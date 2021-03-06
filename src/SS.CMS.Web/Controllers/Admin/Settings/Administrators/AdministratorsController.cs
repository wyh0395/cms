﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Datory;
using Microsoft.AspNetCore.Mvc;
using SS.CMS.Abstractions;
using SS.CMS.Abstractions.Dto.Request;
using SS.CMS.Abstractions.Dto.Result;
using SS.CMS.Core;
using SS.CMS.Core.Office;
using SS.CMS.Framework;
using SS.CMS.Web.Extensions;

namespace SS.CMS.Web.Controllers.Admin.Settings.Administrators
{
    [Route("admin/settings/administrators")]
    public partial class AdministratorsController : ControllerBase
    {
        private const string Route = "";
        private const string RoutePermissions = "permissions/{adminId:int}";
        private const string RouteLock = "actions/lock";
        private const string RouteUnLock = "actions/unLock";
        private const string RouteImport = "actions/import";
        private const string RouteExport = "actions/export";

        private readonly IAuthManager _authManager;

        public AdministratorsController(IAuthManager authManager)
        {
            _authManager = authManager;
        }

        [HttpGet, Route(Route)]
        public async Task<ActionResult<GetResult>> GetConfig([FromQuery]GetRequest request)
        {
            var auth = await _authManager.GetAdminAsync();
            if (!auth.IsAdminLoggin ||
                !await auth.AdminPermissions.HasSystemPermissionsAsync(Constants.AppPermissions.SettingsAdministrators))
            {
                return Unauthorized();
            }

            var roles = new List<KeyValuePair<string, string>>();

            var roleNameList = await auth.AdminPermissions.IsSuperAdminAsync() ? await DataProvider.RoleRepository.GetRoleNameListAsync() : await DataProvider.RoleRepository.GetRoleNameListByCreatorUserNameAsync(auth.AdminName);

            var predefinedRoles = TranslateUtils.GetEnums<PredefinedRole>();
            foreach (var predefinedRole in predefinedRoles)
            {
                roles.Add(new KeyValuePair<string, string>(predefinedRole.GetValue(), predefinedRole.GetDisplayName()));
            }
            foreach (var roleName in roleNameList)
            {
                if (!roles.Any(x => StringUtils.EqualsIgnoreCase(x.Key, roleName)))
                {
                    roles.Add(new KeyValuePair<string, string>(roleName, roleName));
                }
            }

            var isSuperAdmin = await auth.AdminPermissions.IsSuperAdminAsync();
            var creatorUserName = isSuperAdmin ? string.Empty : auth.AdminName;
            var count = await DataProvider.AdministratorRepository.GetCountAsync(creatorUserName, request.Role, request.LastActivityDate, request.Keyword);
            var administrators = await DataProvider.AdministratorRepository.GetAdministratorsAsync(creatorUserName, request.Role, request.Order, request.LastActivityDate, request.Keyword, request.Offset, request.Limit);
            var admins = new List<Admin>();
            foreach (var administratorInfo in administrators)
            {
                admins.Add(new Admin
                {
                    Id = administratorInfo.Id,
                    AvatarUrl = administratorInfo.AvatarUrl,
                    UserName = administratorInfo.UserName,
                    DisplayName = string.IsNullOrEmpty(administratorInfo.DisplayName)
                        ? administratorInfo.UserName
                        : administratorInfo.DisplayName,
                    Mobile = administratorInfo.Mobile,
                    LastActivityDate = administratorInfo.LastActivityDate,
                    CountOfLogin = administratorInfo.CountOfLogin,
                    Locked = administratorInfo.Locked,
                    Roles = await DataProvider.AdministratorRepository.GetRolesAsync(administratorInfo.UserName)
                });
            }

            return new GetResult
            {
                Administrators = admins,
                Count = count,
                Roles = roles,
                IsSuperAdmin = await auth.AdminPermissions.IsSuperAdminAsync(),
                AdminId = auth.AdminId
            };
        }

        [HttpGet, Route(RoutePermissions)]
        public async Task<ActionResult<GetPermissionsResult>> GetPermissions(int adminId)
        {
            var auth = await _authManager.GetAdminAsync();
            if (!auth.IsAdminLoggin ||
                !await auth.AdminPermissions.HasSystemPermissionsAsync(Constants.AppPermissions.SettingsAdministrators))
            {
                return Unauthorized();
            }

            if (!await auth.AdminPermissions.IsSuperAdminAsync())
            {
                return Unauthorized();
            }

            var roles = await DataProvider.RoleRepository.GetRoleNameListAsync();
            roles = roles.Where(x => !DataProvider.RoleRepository.IsPredefinedRole(x)).ToList();
            var allSites = await DataProvider.SiteRepository.GetSiteListAsync();

            var adminInfo = await DataProvider.AdministratorRepository.GetByUserIdAsync(adminId);
            var adminRoles = await DataProvider.AdministratorsInRolesRepository.GetRolesForUserAsync(adminInfo.UserName);
            string adminLevel;
            var checkedSites = new List<int>();
            var checkedRoles = new List<string>();
            if (DataProvider.RoleRepository.IsConsoleAdministrator(adminRoles))
            {
                adminLevel = "SuperAdmin";
            }
            else if (DataProvider.RoleRepository.IsSystemAdministrator(adminRoles))
            {
                adminLevel = "SiteAdmin";
                checkedSites = adminInfo.SiteIds;
            }
            else
            {
                adminLevel = "Admin";
                foreach (var role in roles)
                {
                    if (!checkedRoles.Contains(role) && !DataProvider.RoleRepository.IsPredefinedRole(role) && adminRoles.Contains(role))
                    {
                        checkedRoles.Add(role);
                    }
                }
            }

            return new GetPermissionsResult
            {
                Roles = roles,
                AllSites = allSites,
                AdminLevel = adminLevel,
                CheckedSites = checkedSites,
                CheckedRoles = checkedRoles
            };
        }

        [HttpPost, Route(RoutePermissions)]
        public async Task<ActionResult<SavePermissionsResult>> SavePermissions([FromRoute]int adminId, [FromBody]SavePermissionsRequest request)
        {
            var auth = await _authManager.GetAdminAsync();
            if (!auth.IsAdminLoggin ||
                !await auth.AdminPermissions.HasSystemPermissionsAsync(Constants.AppPermissions.SettingsAdministrators))
            {
                return Unauthorized();
            }

            if (!await auth.AdminPermissions.IsSuperAdminAsync())
            {
                return Unauthorized();
            }

            var adminInfo = await DataProvider.AdministratorRepository.GetByUserIdAsync(adminId);

            await DataProvider.AdministratorsInRolesRepository.RemoveUserAsync(adminInfo.UserName);
            if (request.AdminLevel == "SuperAdmin")
            {
                await DataProvider.AdministratorRepository.AddUserToRoleAsync(adminInfo.UserName, PredefinedRole.ConsoleAdministrator.GetValue());
            }
            else if (request.AdminLevel == "SiteAdmin")
            {
                await DataProvider.AdministratorRepository.AddUserToRoleAsync(adminInfo.UserName, PredefinedRole.SystemAdministrator.GetValue());
            }
            else
            {
                await DataProvider.AdministratorRepository.AddUserToRoleAsync(adminInfo.UserName, PredefinedRole.Administrator.GetValue());
                await DataProvider.AdministratorRepository.AddUserToRolesAsync(adminInfo.UserName,  request.CheckedRoles.ToArray());
            }

            await DataProvider.AdministratorRepository.UpdateSiteIdsAsync(adminInfo,
                request.AdminLevel == "SiteAdmin"
                    ? request.CheckedSites
                    : new List<int>());

            CacheUtils.ClearAll();

            await auth.AddAdminLogAsync("设置管理员权限", $"管理员:{adminInfo.UserName}");

            return new SavePermissionsResult
            {
                Roles = await DataProvider.AdministratorRepository.GetRolesAsync(adminInfo.UserName)
            };
        }

        [HttpDelete, Route(Route)]
        public async Task<ActionResult<BoolResult>> Delete([FromBody]IdRequest request)
        {
            var auth = await _authManager.GetAdminAsync();
            if (!auth.IsAdminLoggin ||
                !await auth.AdminPermissions.HasSystemPermissionsAsync(Constants.AppPermissions.SettingsAdministrators))
            {
                return Unauthorized();
            }

            var adminInfo = await DataProvider.AdministratorRepository.GetByUserIdAsync(request.Id);
            await DataProvider.AdministratorsInRolesRepository.RemoveUserAsync(adminInfo.UserName);
            await DataProvider.AdministratorRepository.DeleteAsync(adminInfo.Id);

            await auth.AddAdminLogAsync("删除管理员", $"管理员:{adminInfo.UserName}");

            return new BoolResult
            {
                Value = true
            };
        }

        [HttpPost, Route(RouteLock)]
        public async Task<ActionResult<BoolResult>> Lock([FromBody]IdRequest request)
        {
            var auth = await _authManager.GetAdminAsync();
            if (!auth.IsAdminLoggin ||
                !await auth.AdminPermissions.HasSystemPermissionsAsync(Constants.AppPermissions.SettingsAdministrators))
            {
                return Unauthorized();
            }

            var adminInfo = await DataProvider.AdministratorRepository.GetByUserIdAsync(request.Id);

            await DataProvider.AdministratorRepository.LockAsync(new List<string>
            {
                adminInfo.UserName
            });

            await auth.AddAdminLogAsync("锁定管理员", $"管理员:{adminInfo.UserName}");

            return new BoolResult
            {
                Value = true
            };
        }

        [HttpPost, Route(RouteUnLock)]
        public async Task<ActionResult<BoolResult>> UnLock([FromBody]IdRequest request)
        {
            var auth = await _authManager.GetAdminAsync();
            if (!auth.IsAdminLoggin ||
                !await auth.AdminPermissions.HasSystemPermissionsAsync(Constants.AppPermissions.SettingsAdministrators))
            {
                return Unauthorized();
            }

            var adminInfo = await DataProvider.AdministratorRepository.GetByUserIdAsync(request.Id);

            await DataProvider.AdministratorRepository.UnLockAsync(new List<string>
            {
                adminInfo.UserName
            });

            await auth.AddAdminLogAsync("解锁管理员", $"管理员:{adminInfo.UserName}");

            return new BoolResult
            {
                Value = true
            };
        }

        [HttpPost, Route(RouteImport)]
        public async Task<ActionResult<ImportResult>> Import([FromBody]ImportRequest request)
        {
            var auth = await _authManager.GetAdminAsync();
            if (!auth.IsAdminLoggin ||
                !await auth.AdminPermissions.HasSystemPermissionsAsync(Constants.AppPermissions.SettingsAdministrators))
            {
                return Unauthorized();
            }

            if (request.File == null)
            {
                return this.Error("请选择有效的文件上传");
            }

            var fileName = Path.GetFileName(request.File.FileName);

            var sExt = PathUtils.GetExtension(fileName);
            if (!StringUtils.EqualsIgnoreCase(sExt, ".xlsx"))
            {
                return this.Error("导入文件为Excel格式，请选择有效的文件上传");
            }

            var filePath = PathUtility.GetTemporaryFilesPath(fileName);
            DirectoryUtils.CreateDirectoryIfNotExists(filePath);
            request.File.CopyTo(new FileStream(filePath, FileMode.Create));

            var errorMessage = string.Empty;
            var success = 0;
            var failure = 0;

            var sheet = ExcelUtils.GetDataTable(filePath);
            if (sheet != null)
            {
                for (var i = 1; i < sheet.Rows.Count; i++) //行
                {
                    if (i == 1) continue;

                    var row = sheet.Rows[i];

                    var userName = row[0].ToString().Trim();
                    var password = row[1].ToString().Trim();
                    var displayName = row[2].ToString().Trim();
                    var mobile = row[3].ToString().Trim();
                    var email = row[4].ToString().Trim();

                    if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password))
                    {
                        var (isValid, message) = await DataProvider.AdministratorRepository.InsertAsync(new Administrator
                        {
                            UserName = userName,
                            DisplayName = displayName,
                            Mobile = mobile,
                            Email = email
                        }, password);
                        if (!isValid)
                        {
                            failure++;
                            errorMessage = message;
                        }
                        else
                        {
                            success++;
                        }
                    }
                    else
                    {
                        failure++;
                    }
                }
            }

            return new ImportResult
            {
                Value = true,
                Success = success,
                Failure = failure,
                ErrorMessage = errorMessage
            };
        }

        [HttpPost, Route(RouteExport)]
        public async Task<ActionResult<StringResult>> Export()
        {
            var auth = await _authManager.GetAdminAsync();
            if (!auth.IsAdminLoggin ||
                !await auth.AdminPermissions.HasSystemPermissionsAsync(Constants.AppPermissions.SettingsAdministrators))
            {
                return Unauthorized();
            }

            const string fileName = "administrators.csv";
            var filePath = PathUtility.GetTemporaryFilesPath(fileName);

            await ExcelObject.CreateExcelFileForAdministratorsAsync(filePath);
            var downloadUrl = PageUtils.GetRootUrlByPhysicalPath(filePath);

            return new StringResult
            {
                Value = downloadUrl
            };
        }
    }
}
