﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Datory;
using SS.CMS.Abstractions;
using SS.CMS.Framework;
using InputType = SS.CMS.Abstractions.InputType;
using TemplateType = SS.CMS.Abstractions.TemplateType;

namespace SS.CMS.Core
{
    public static class PathUtility
    {
        public static async Task<string> GetSitePathAsync(Site site)
        {
            return await GetSitePathAsync(site, null);
        }

        public static async Task<string> GetSitePathAsync(Site site, params string[] paths)
        {
            string sitePath;
            if (site.ParentId == 0)
            {
                sitePath = PathUtils.Combine(WebConfigUtils.PhysicalApplicationPath, site.Root ? string.Empty : site.SiteDir);
            }
            else
            {
                var parent = await DataProvider.SiteRepository.GetAsync(site.ParentId);
                sitePath = await GetSitePathAsync(parent, site.SiteDir);
            }

            if (paths == null || paths.Length <= 0) return sitePath;

            foreach (var t in paths)
            {
                var path = t?.Replace(Constants.PageSeparatorChar, PathUtils.SeparatorChar).Trim(PathUtils.SeparatorChar) ?? string.Empty;
                sitePath = PathUtils.Combine(sitePath, path);
            }
            return sitePath;
        }

        public static async Task<string> GetSitePathAsync(int siteId, params string[] paths)
        {
            var site = await DataProvider.SiteRepository.GetAsync(siteId);
            return await GetSitePathAsync(site, paths);
        }

        public static async Task<string> GetIndexPageFilePathAsync(Site site, string createFileFullName, bool root, int currentPageIndex)
        {
            if (string.IsNullOrEmpty(createFileFullName))
            {
                createFileFullName = "@/index.html";
            }
            if (root)
            {
                if (createFileFullName.StartsWith("@"))
                {
                    createFileFullName = "~" + createFileFullName.Substring(1);
                }
                else if (!createFileFullName.StartsWith("~"))
                {
                    createFileFullName = "~" + createFileFullName;
                }
            }
            else
            {
                if (!createFileFullName.StartsWith("~") && !createFileFullName.StartsWith("@"))
                {
                    createFileFullName = "@" + createFileFullName;
                }
            }

            var filePath = await MapPathAsync(site, createFileFullName);

            if (currentPageIndex != 0)
            {
                string appendix = $"_{currentPageIndex + 1}";
                var fileName = PathUtils.GetFileNameWithoutExtension(filePath) + appendix + PathUtils.GetExtension(filePath);
                filePath = PathUtils.Combine(DirectoryUtils.GetDirectoryPath(filePath), fileName);
            }

            return filePath;
        }

        public static string GetBackupFilePath(Site site, BackupType backupType)
        {
            var extention = ".zip";
            var siteName = site.SiteDir;
            if (!string.IsNullOrEmpty(siteName))
            {
                siteName += "_";
            }
            if (backupType == BackupType.Templates)
            {
                extention = ".xml";
            }
            return PathUtils.Combine(PhysicalSiteFilesPath, DirectoryUtils.SiteFiles.BackupFiles, site.SiteDir, DateTime.Now.ToString("yyyy-MM"), backupType.GetValue() + "_" + siteName + DateTime.Now.ToString("yyyy-MM-dd-HH-mm") + extention);
        }

        public static async Task<string> GetUploadDirectoryPathAsync(Site site, string fileExtension)
        {
            return await GetUploadDirectoryPathAsync(site, DateTime.Now, fileExtension);
        }

        public static async Task<string> GetUploadDirectoryPathAsync(Site site, DateTime datetime, string fileExtension)
        {
            var uploadDateFormatString = site.FileUploadDateFormatString;
            var uploadDirectoryName = site.FileUploadDirectoryName;

            if (IsImageExtensionAllowed(site, fileExtension))
            {
                uploadDateFormatString = site.ImageUploadDateFormatString;
                uploadDirectoryName = site.ImageUploadDirectoryName;
            }
            else if (IsVideoExtensionAllowed(site, fileExtension))
            {
                uploadDateFormatString = site.VideoUploadDateFormatString;
                uploadDirectoryName = site.VideoUploadDirectoryName;
            }

            string directoryPath;
            var dateFormatType = uploadDateFormatString;
            var sitePath = await GetSitePathAsync(site);
            if (dateFormatType == DateFormatType.Year)
            {
                directoryPath = PathUtils.Combine(sitePath, uploadDirectoryName, datetime.Year.ToString());
            }
            else if (dateFormatType == DateFormatType.Day)
            {
                directoryPath = PathUtils.Combine(sitePath, uploadDirectoryName, datetime.Year.ToString(), datetime.Month.ToString(), datetime.Day.ToString());
            }
            else
            {
                directoryPath = PathUtils.Combine(sitePath, uploadDirectoryName, datetime.Year.ToString(), datetime.Month.ToString());
            }
            DirectoryUtils.CreateDirectoryIfNotExists(directoryPath);
            return directoryPath;
        }

        public static async Task<string> GetUploadDirectoryPathAsync(Site site, UploadType uploadType)
        {
            return await GetUploadDirectoryPathAsync(site, DateTime.Now, uploadType);
        }

        public static async Task<string> GetUploadDirectoryPathAsync(Site site, DateTime datetime, UploadType uploadType)
        {
            var uploadDateFormatString = DateFormatType.Month;
            var uploadDirectoryName = string.Empty;

            if (uploadType == UploadType.Image)
            {
                uploadDateFormatString = site.ImageUploadDateFormatString;
                uploadDirectoryName = site.ImageUploadDirectoryName;
            }
            else if (uploadType == UploadType.Audio)
            {
                uploadDateFormatString = site.AudioUploadDateFormatString;
                uploadDirectoryName = site.AudioUploadDirectoryName;
            }
            else if (uploadType == UploadType.Video)
            {
                uploadDateFormatString = site.VideoUploadDateFormatString;
                uploadDirectoryName = site.VideoUploadDirectoryName;
            }
            else if (uploadType == UploadType.File)
            {
                uploadDateFormatString = site.FileUploadDateFormatString;
                uploadDirectoryName = site.FileUploadDirectoryName;
            }
            else if (uploadType == UploadType.Special)
            {
                uploadDateFormatString = site.FileUploadDateFormatString;
                uploadDirectoryName = "/special";
            }

            string directoryPath;
            var dateFormatType = uploadDateFormatString;
            var sitePath = await GetSitePathAsync(site);
            if (dateFormatType == DateFormatType.Year)
            {
                directoryPath = PathUtils.Combine(sitePath, uploadDirectoryName, datetime.Year.ToString());
            }
            else if (dateFormatType == DateFormatType.Day)
            {
                directoryPath = PathUtils.Combine(sitePath, uploadDirectoryName, datetime.Year.ToString(), datetime.Month.ToString(), datetime.Day.ToString());
            }
            else
            {
                directoryPath = PathUtils.Combine(sitePath, uploadDirectoryName, datetime.Year.ToString(), datetime.Month.ToString());
            }
            DirectoryUtils.CreateDirectoryIfNotExists(directoryPath);
            return directoryPath;
        }

        public static string GetUploadFileName(Site site, string filePath)
        {
            var fileExtension = PathUtils.GetExtension(filePath);

            var isUploadChangeFileName = site.IsFileUploadChangeFileName;
            if (IsImageExtensionAllowed(site, fileExtension))
            {
                isUploadChangeFileName = site.IsImageUploadChangeFileName;
            }
            else if (IsVideoExtensionAllowed(site, fileExtension))
            {
                isUploadChangeFileName = site.IsVideoUploadChangeFileName;
            }

            return GetUploadFileName(filePath, isUploadChangeFileName);
        }

        public static string GetUploadFileName(string filePath, bool isUploadChangeFileName)
        {
            if (isUploadChangeFileName)
            {
                return $"{StringUtils.GetShortGuid(false)}{PathUtils.GetExtension(filePath)}";
            }

            var fileName = PathUtils.GetFileNameWithoutExtension(filePath);
            fileName = PathUtils.GetSafeFilename(fileName);
            return $"{fileName}{PathUtils.GetExtension(filePath)}";
        }

        public static async Task<Site> GetSiteAsync(string path)
        {
            var directoryPath = DirectoryUtils.GetDirectoryPath(path).ToLower().Trim(' ', '/', '\\');
            var applicationPath = WebConfigUtils.PhysicalApplicationPath.ToLower().Trim(' ', '/', '\\');
            var directoryDir = StringUtils.ReplaceStartsWith(directoryPath, applicationPath, string.Empty).Trim(' ', '/', '\\');
            if (directoryDir == string.Empty) return null;

            var siteList = await DataProvider.SiteRepository.GetSiteListAsync();

            Site headquarter = null;
            foreach (var site in siteList)
            {
                if (site.Root)
                {
                    headquarter = site;
                }
                else
                {
                    if (StringUtils.Contains(directoryDir, site.SiteDir.ToLower()))
                    {
                        return site;
                    }
                }
            }

            return headquarter;
        }

        public static async Task<string> GetSiteDirAsync(string path)
        {
            var siteDir = string.Empty;
            var directoryPath = DirectoryUtils.GetDirectoryPath(path).ToLower().Trim(' ', '/', '\\');
            var applicationPath = WebConfigUtils.PhysicalApplicationPath.ToLower().Trim(' ', '/', '\\');
            var directoryDir = StringUtils.ReplaceStartsWith(directoryPath, applicationPath, string.Empty).Trim(' ', '/', '\\');
            if (directoryDir == string.Empty)
            {
                return string.Empty;
            }

            var siteList = await DataProvider.SiteRepository.GetSiteListAsync();
            foreach (var site in siteList)
            {
                if (site?.Root!= false) continue;

                if (StringUtils.Contains(directoryDir, site.SiteDir.ToLower()))
                {
                    siteDir = site.SiteDir;
                }
            }

            return string.IsNullOrWhiteSpace(siteDir) ? siteDir : PathUtils.GetDirectoryName(siteDir, false);
        }

        public static async Task<int> GetCurrentSiteIdAsync()
        {
            int siteId;
            var siteIdList = await DataProvider.SiteRepository.GetSiteIdListAsync();
            if (siteIdList.Any())
            {
                siteId = siteIdList.First();
            }
            else
            {
                siteId = await DataProvider.SiteRepository.GetIdByIsRootAsync();
            }
            return siteId;
        }

        public static string AddVirtualToPath(string path)
        {
            var resolvedPath = path;
            if (!string.IsNullOrEmpty(path))
            {
                path = path.Replace("../", string.Empty);
                if (!path.StartsWith("@") && !path.StartsWith("~"))
                {
                    resolvedPath = "@" + path;
                }
            }
            return resolvedPath;
        }

        public static async Task<string> MapPathAsync(Site site, string virtualPath)
        {
            var resolvedPath = virtualPath;
            if (string.IsNullOrEmpty(virtualPath))
            {
                virtualPath = "@";
            }
            if (!virtualPath.StartsWith("@") && !virtualPath.StartsWith("~"))
            {
                virtualPath = "@" + virtualPath;
            }
            if (!virtualPath.StartsWith("@")) return WebUtils.MapPath(resolvedPath);

            if (site != null)
            {
                return await GetSitePathAsync(site, virtualPath.Substring(1));
            }
            return WebUtils.MapPath(resolvedPath);
        }

        public static async Task<string> MapPathAsync(Site site, string virtualPath, bool isCopyToSite)
        {
            if (!isCopyToSite) return await MapPathAsync(site, virtualPath);

            var resolvedPath = virtualPath;
            if (string.IsNullOrEmpty(virtualPath))
            {
                virtualPath = "@";
            }
            if (!virtualPath.StartsWith("@") && !virtualPath.StartsWith("~"))
            {
                virtualPath = "@" + virtualPath;
            }
            if (!virtualPath.StartsWith("@")) return WebUtils.MapPath(resolvedPath);

            if (site != null)
            {
                return await GetSitePathAsync(site, virtualPath.Substring(1));
            }
            return WebUtils.MapPath(resolvedPath);
        }

        public static string MapPath(string directoryPath, string virtualPath)
        {
            var resolvedPath = virtualPath;
            if (string.IsNullOrEmpty(virtualPath))
            {
                virtualPath = "@";
            }
            if (!virtualPath.StartsWith("@") && !virtualPath.StartsWith("~"))
            {
                virtualPath = "@" + virtualPath;
            }
            if (virtualPath.StartsWith("@"))
            {
                if (string.IsNullOrEmpty(directoryPath))
                {
                    resolvedPath = string.Concat("~", virtualPath.Substring(1));
                }
                else
                {
                    return PageUtils.Combine(directoryPath, virtualPath.Substring(1));
                }
            }
            return WebUtils.MapPath(resolvedPath);
        }

        //将编辑器中图片上传至本机
        public static async Task<string> SaveImageAsync(Site site, string content)
        {
            var originalImageSrcs = RegexUtils.GetOriginalImageSrcs(content);
            foreach (var originalImageSrc in originalImageSrcs)
            {
                if (!PageUtils.IsProtocolUrl(originalImageSrc) ||
                    StringUtils.StartsWithIgnoreCase(originalImageSrc, PageUtils.ApplicationPath) ||
                    StringUtils.StartsWithIgnoreCase(originalImageSrc, await site.GetWebUrlAsync()))
                    continue;
                var fileExtName = PageUtils.GetExtensionFromUrl(originalImageSrc);
                if (!FileUtils.IsImageOrPlayer(fileExtName)) continue;

                var fileName = GetUploadFileName(site, originalImageSrc);
                var directoryPath = await GetUploadDirectoryPathAsync(site, fileExtName);
                var filePath = PathUtils.Combine(directoryPath, fileName);

                try
                {
                    if (!FileUtils.IsFileExists(filePath))
                    {
                        WebClientUtils.SaveRemoteFileToLocal(originalImageSrc, filePath);
                        if (FileUtils.IsImage(PathUtils.GetExtension(fileName)))
                        {
                            await FileUtility.AddWaterMarkAsync(site, filePath);
                        }
                    }
                    var fileUrl = await PageUtility.GetSiteUrlByPhysicalPathAsync(site, filePath, true);
                    content = content.Replace(originalImageSrc, fileUrl);
                }
                catch
                {
                    // ignored
                }
            }
            return content;
        }

        public static string GetTemporaryFilesPath(string relatedPath)
        {
            relatedPath = PathUtils.RemoveParentPath(relatedPath);
            return PathUtils.Combine(WebConfigUtils.PhysicalApplicationPath, DirectoryUtils.SiteFiles.DirectoryName, DirectoryUtils.SiteFiles.TemporaryFiles, relatedPath);
        }

        public static string GetSiteTemplatesPath(string relatedPath)
        {
            relatedPath = PathUtils.RemoveParentPath(relatedPath);
            return PathUtils.Combine(WebConfigUtils.PhysicalApplicationPath, DirectoryUtils.SiteFiles.DirectoryName, DirectoryUtils.SiteTemplates.DirectoryName, relatedPath);
        }

        public static string GetSiteTemplateMetadataPath(string siteTemplatePath, string relatedPath)
        {
            relatedPath = PathUtils.RemoveParentPath(relatedPath);
            return PathUtils.Combine(siteTemplatePath, DirectoryUtils.SiteTemplates.SiteTemplateMetadata, relatedPath);
        }

        public static bool IsSystemFile(string fileName)
        {
            if (StringUtils.EqualsIgnoreCase(fileName, "Web.config")
                || StringUtils.EqualsIgnoreCase(fileName, "Global.asax")
                || StringUtils.EqualsIgnoreCase(fileName, "robots.txt"))
            {
                return true;
            }
            return false;
        }

        public static bool IsSystemFileForChangeSiteType(string fileName)
        {
            if (StringUtils.EqualsIgnoreCase(fileName, "Web.config")
                || StringUtils.EqualsIgnoreCase(fileName, "Global.asax")
                || StringUtils.EqualsIgnoreCase(fileName, "robots.txt")
                || StringUtils.EqualsIgnoreCase(fileName, "packages.config")
                || StringUtils.EqualsIgnoreCase(fileName, "version.txt"))
            {
                return true;
            }
            return false;
        }

        public static class ChannelFilePathRules
        {
            private const string ChannelId = "{@channelId}";
            private const string Year = "{@year}";
            private const string Month = "{@month}";
            private const string Day = "{@day}";
            private const string Hour = "{@hour}";
            private const string Minute = "{@minute}";
            private const string Second = "{@second}";
            private const string Sequence = "{@sequence}";
            private const string ParentRule = "{@parentRule}";
            private const string ChannelName = "{@channelName}";
            private const string LowerChannelName = "{@lowerChannelName}";
            private const string ChannelIndex = "{@channelIndex}";
            private const string LowerChannelIndex = "{@lowerChannelIndex}";

            public static string DefaultRule = "/channels/{@channelId}.html";
            public static string DefaultDirectoryName = "/channels/";
            public static string DefaultRegexString = "/channels/(?<channelId>[^_]*)_?(?<pageIndex>[^_]*)";

            public static async Task<Dictionary<string, string>> GetDictionaryAsync(Site site, int channelId)
            {
                var dictionary = new Dictionary<string, string>
                {
                    {ChannelId, "栏目ID"},
                    {Year, "年份"},
                    {Month, "月份"},
                    {Day, "日期"},
                    {Hour, "小时"},
                    {Minute, "分钟"},
                    {Second, "秒钟"},
                    {Sequence, "顺序数"},
                    {ParentRule, "父级命名规则"},
                    {ChannelName, "栏目名称"},
                    {LowerChannelName, "栏目名称(小写)"},
                    {ChannelIndex, "栏目索引"},
                    {LowerChannelIndex, "栏目索引(小写)"}
                };

                var channelInfo = await DataProvider.ChannelRepository.GetAsync(channelId);
                var styleInfoList = await DataProvider.TableStyleRepository.GetChannelStyleListAsync(channelInfo);
                foreach (var styleInfo in styleInfoList)
                {
                    if (styleInfo.InputType == InputType.Text)
                    {
                        dictionary.Add($@"{{@{StringUtils.LowerFirst(styleInfo.AttributeName)}}}", styleInfo.DisplayName);
                        dictionary.Add($@"{{@lower{styleInfo.AttributeName}}}", styleInfo.DisplayName + "(小写)");
                    }
                }

                return dictionary;
            }

            public static async Task<string> ParseAsync(Site site, int channelId)
            {
                var channelFilePathRule = await GetChannelFilePathRuleAsync(site, channelId);
                var filePath = await ParseChannelPathAsync(site, channelId, channelFilePathRule);
                return filePath;
            }

            //递归处理
            private static async Task<string> ParseChannelPathAsync(Site site, int channelId, string channelFilePathRule)
            {
                var filePath = channelFilePathRule.Trim();
                const string regex = "(?<element>{@[^}]+})";
                var elements = RegexUtils.GetContents("element", regex, filePath);
                Channel node = null;

                foreach (var element in elements)
                {
                    var value = string.Empty;
                    
                    if (StringUtils.EqualsIgnoreCase(element, ChannelId))
                    {
                        value = channelId.ToString();
                    }
                    else if (StringUtils.EqualsIgnoreCase(element, Year))
                    {
                        if (node == null) node = await DataProvider.ChannelRepository.GetAsync(channelId);
                        if (node.AddDate.HasValue)
                        {
                            value = node.AddDate.Value.Year.ToString();
                        }
                    }
                    else if (StringUtils.EqualsIgnoreCase(element, Month))
                    {
                        if (node == null) node = await DataProvider.ChannelRepository.GetAsync(channelId);
                        if (node.AddDate.HasValue)
                        {
                            value = node.AddDate.Value.Month.ToString();
                        }
                    }
                    else if (StringUtils.EqualsIgnoreCase(element, Day))
                    {
                        if (node == null) node = await DataProvider.ChannelRepository.GetAsync(channelId);
                        if (node.AddDate.HasValue)
                        {
                            value = node.AddDate.Value.Day.ToString();
                        }
                    }
                    else if (StringUtils.EqualsIgnoreCase(element, Hour))
                    {
                        if (node == null) node = await DataProvider.ChannelRepository.GetAsync(channelId);
                        if (node.AddDate.HasValue)
                        {
                            value = node.AddDate.Value.Hour.ToString();
                        }
                    }
                    else if (StringUtils.EqualsIgnoreCase(element, Minute))
                    {
                        if (node == null) node = await DataProvider.ChannelRepository.GetAsync(channelId);
                        if (node.AddDate.HasValue)
                        {
                            value = node.AddDate.Value.Minute.ToString();
                        }
                    }
                    else if (StringUtils.EqualsIgnoreCase(element, Second))
                    {
                        if (node == null) node = await DataProvider.ChannelRepository.GetAsync(channelId);
                        if (node.AddDate.HasValue)
                        {
                            value = node.AddDate.Value.Second.ToString();
                        }
                    }
                    else if (StringUtils.EqualsIgnoreCase(element, Sequence))
                    {
                        value = (await DataProvider.ChannelRepository.GetSequenceAsync(site.Id, channelId)).ToString();
                    }
                    else if (StringUtils.EqualsIgnoreCase(element, ParentRule))
                    {
                        if (node == null) node = await DataProvider.ChannelRepository.GetAsync(channelId);
                        var parentInfo = await DataProvider.ChannelRepository.GetAsync(node.ParentId);
                        if (parentInfo != null)
                        {
                            var parentRule = await GetChannelFilePathRuleAsync(site, parentInfo.Id);
                            value = DirectoryUtils.GetDirectoryPath(await ParseChannelPathAsync(site, parentInfo.Id, parentRule)).Replace("\\", "/");
                        }
                    }
                    else if (StringUtils.EqualsIgnoreCase(element, ChannelName))
                    {
                        if (node == null) node = await DataProvider.ChannelRepository.GetAsync(channelId);
                        value = node.ChannelName;
                    }
                    else if (StringUtils.EqualsIgnoreCase(element, LowerChannelName))
                    {
                        if (node == null) node = await DataProvider.ChannelRepository.GetAsync(channelId);
                        value = node.ChannelName.ToLower();
                    }
                    else if (StringUtils.EqualsIgnoreCase(element, LowerChannelIndex))
                    {
                        if (node == null) node = await DataProvider.ChannelRepository.GetAsync(channelId);
                        value = node.IndexName.ToLower();
                    }
                    else
                    {
                        if (node == null) node = await DataProvider.ChannelRepository.GetAsync(channelId);
                        var attributeName = element.Replace("{@", string.Empty).Replace("}", string.Empty);

                        var isLower = false;
                        if (StringUtils.StartsWithIgnoreCase(attributeName, "lower"))
                        {
                            isLower = true;
                            attributeName = attributeName.Substring(5);
                        }

                        value = node.Get<string>(attributeName);

                        if (isLower)
                        {
                            value = value.ToLower();
                        }
                    }

                    filePath = filePath.Replace(element, value);
                }

                if (!filePath.Contains("//")) return filePath;

                filePath = Regex.Replace(filePath, @"(/)\1{2,}", "/");
                filePath = Regex.Replace(filePath, @"//", "/");
                return filePath;
            }
        }

        public class ContentFilePathRules
        {
            private ContentFilePathRules()
            {
            }

            private const string ChannelId = "{@channelId}";
            private const string ContentId = "{@contentId}";
            private const string Year = "{@year}";
            private const string Month = "{@month}";
            private const string Day = "{@day}";
            private const string Hour = "{@hour}";
            private const string Minute = "{@minute}";
            private const string Second = "{@second}";
            private const string Sequence = "{@sequence}";
            private const string ParentRule = "{@parentRule}";
            private const string ChannelName = "{@channelName}";
            private const string LowerChannelName = "{@lowerChannelName}";
            private const string ChannelIndex = "{@channelIndex}";
            private const string LowerChannelIndex = "{@lowerChannelIndex}";

            public const string DefaultRule = "/contents/{@channelId}/{@contentId}.html";
            public const string DefaultDirectoryName = "/contents/";
            public const string DefaultRegexString = "/contents/(?<channelId>[^/]*)/(?<contentId>[^/]*)_?(?<pageIndex>[^_]*)";

            public static async Task<Dictionary<string, string>> GetDictionaryAsync(Site site, int channelId)
            {
                var dictionary = new Dictionary<string, string>
                {
                    {ChannelId, "栏目ID"},
                    {ContentId, "内容ID"},
                    {Year, "年份"},
                    {Month, "月份"},
                    {Day, "日期"},
                    {Hour, "小时"},
                    {Minute, "分钟"},
                    {Second, "秒钟"},
                    {Sequence, "顺序数"},
                    {ParentRule, "父级命名规则"},
                    {ChannelName, "栏目名称"},
                    {LowerChannelName, "栏目名称(小写)"},
                    {ChannelIndex, "栏目索引"},
                    {LowerChannelIndex, "栏目索引(小写)"}
                };

                var channelInfo = await DataProvider.ChannelRepository.GetAsync(channelId);
                var tableName = await DataProvider.ChannelRepository.GetTableNameAsync(site, channelInfo);
                var styleInfoList = await DataProvider.TableStyleRepository.GetContentStyleListAsync(channelInfo, tableName);
                foreach (var styleInfo in styleInfoList)
                {
                    if (styleInfo.InputType == InputType.Text)
                    {
                        dictionary.Add($@"{{@{StringUtils.LowerFirst(styleInfo.AttributeName)}}}", styleInfo.DisplayName);
                        dictionary.Add($@"{{@lower{styleInfo.AttributeName}}}", styleInfo.DisplayName + "(小写)");
                    }
                }

                return dictionary;
            }

            public static async Task<string> ParseAsync(Site site, int channelId, int contentId)
            {
                var contentFilePathRule = await GetContentFilePathRuleAsync(site, channelId);
                var contentInfo = await DataProvider.ContentRepository.GetAsync(site, channelId, contentId);
                var filePath = await ParseContentPathAsync(site, channelId, contentInfo, contentFilePathRule);
                return filePath;
            }

            public static async Task<string> ParseAsync(Site site, int channelId, Content content)
            {
                var contentFilePathRule = await GetContentFilePathRuleAsync(site, channelId);
                var filePath = await ParseContentPathAsync(site, channelId, content, contentFilePathRule);
                return filePath;
            }

            private static async Task<string> ParseContentPathAsync(Site site, int channelId, Content content, string contentFilePathRule)
            {
                var filePath = contentFilePathRule.Trim();
                var regex = "(?<element>{@[^}]+})";
                var elements = RegexUtils.GetContents("element", regex, filePath);
                var addDate = content.AddDate;
                var contentId = content.Id;
                foreach (var element in elements)
                {
                    var value = string.Empty;

                    if (StringUtils.EqualsIgnoreCase(element, ChannelId))
                    {
                        value = channelId.ToString();
                    }
                    else if (StringUtils.EqualsIgnoreCase(element, ContentId))
                    {
                        value = contentId.ToString();
                    }
                    else if (StringUtils.EqualsIgnoreCase(element, Sequence))
                    {
                        var tableName = await DataProvider.ChannelRepository.GetTableNameAsync(site, channelId);
                        value = Convert.ToString(await DataProvider.ContentRepository.GetSequenceAsync(tableName, site.Id, channelId, contentId));
                    }
                    else if (StringUtils.EqualsIgnoreCase(element, ParentRule))//继承父级设置 20151113 sessionliang
                    {
                        var nodeInfo = await DataProvider.ChannelRepository.GetAsync(channelId);
                        var parentInfo = await DataProvider.ChannelRepository.GetAsync(nodeInfo.ParentId);
                        if (parentInfo != null)
                        {
                            var parentRule = await GetContentFilePathRuleAsync(site, parentInfo.Id);
                            value = DirectoryUtils.GetDirectoryPath(await ParseContentPathAsync(site, parentInfo.Id, content, parentRule)).Replace("\\", "/");
                        }
                    }
                    else if (StringUtils.EqualsIgnoreCase(element, ChannelName))
                    {
                        var nodeInfo = await DataProvider.ChannelRepository.GetAsync(channelId);
                        if (nodeInfo != null)
                        {
                            value = nodeInfo.ChannelName;
                        }
                    }
                    else if (StringUtils.EqualsIgnoreCase(element, LowerChannelName))
                    {
                        var nodeInfo = await DataProvider.ChannelRepository.GetAsync(channelId);
                        if (nodeInfo != null)
                        {
                            value = nodeInfo.ChannelName.ToLower();
                        }
                    }
                    else if (StringUtils.EqualsIgnoreCase(element, ChannelIndex))
                    {
                        var nodeInfo = await DataProvider.ChannelRepository.GetAsync(channelId);
                        if (nodeInfo != null)
                        {
                            value = nodeInfo.IndexName;
                        }
                    }
                    else if (StringUtils.EqualsIgnoreCase(element, LowerChannelIndex))
                    {
                        var nodeInfo = await DataProvider.ChannelRepository.GetAsync(channelId);
                        if (nodeInfo != null)
                        {
                            value = nodeInfo.IndexName.ToLower();
                        }
                    }
                    else if (StringUtils.EqualsIgnoreCase(element, Year) || StringUtils.EqualsIgnoreCase(element, Month) || StringUtils.EqualsIgnoreCase(element, Day) || StringUtils.EqualsIgnoreCase(element, Hour) || StringUtils.EqualsIgnoreCase(element, Minute) || StringUtils.EqualsIgnoreCase(element, Second))
                    {
                        if (StringUtils.EqualsIgnoreCase(element, Year))
                        {
                            if (addDate.HasValue)
                            {
                                value = addDate.Value.Year.ToString();
                            }
                        }
                        else if (StringUtils.EqualsIgnoreCase(element, Month))
                        {
                            if (addDate.HasValue)
                            {
                                value = addDate.Value.Month.ToString("D2");
                            }

                            //value = addDate.ToString("MM");
                        }
                        else if (StringUtils.EqualsIgnoreCase(element, Day))
                        {
                            if (addDate.HasValue)
                            {
                                value = addDate.Value.Day.ToString("D2");
                            }

                            //value = addDate.ToString("dd");
                        }
                        else if (StringUtils.EqualsIgnoreCase(element, Hour))
                        {
                            if (addDate.HasValue)
                            {
                                value = addDate.Value.Hour.ToString();
                            }
                        }
                        else if (StringUtils.EqualsIgnoreCase(element, Minute))
                        {
                            if (addDate.HasValue)
                            {
                                value = addDate.Value.Minute.ToString();
                            }
                        }
                        else if (StringUtils.EqualsIgnoreCase(element, Second))
                        {
                            if (addDate.HasValue)
                            {
                                value = addDate.Value.Second.ToString();
                            }
                        }
                    }
                    else
                    {
                        var attributeName = element.Replace("{@", string.Empty).Replace("}", string.Empty);

                        var isLower = false;
                        if (StringUtils.StartsWithIgnoreCase(attributeName, "lower"))
                        {
                            isLower = true;
                            attributeName = attributeName.Substring(5);
                        }

                        value = content.Get<string>(attributeName);
                        if (isLower)
                        {
                            value = value.ToLower();
                        }
                    }

                    value = WebUtils.HtmlDecode(value);

                    filePath = filePath.Replace(element, value);
                }

                if (filePath.Contains("//"))
                {
                    filePath = Regex.Replace(filePath, @"(/)\1{2,}", "/");
                    filePath = filePath.Replace("//", "/");
                }

                if (filePath.Contains("("))
                {
                    regex = @"(?<element>\([^\)]+\))";
                    elements = RegexUtils.GetContents("element", regex, filePath);
                    foreach (var element in elements)
                    {
                        if (!element.Contains("|")) continue;

                        var value = element.Replace("(", string.Empty).Replace(")", string.Empty);
                        var value1 = value.Split('|')[0];
                        var value2 = value.Split('|')[1];
                        value = value1 + value2;

                        if (!string.IsNullOrEmpty(value1) && !string.IsNullOrEmpty(value1))
                        {
                            value = value1;
                        }

                        filePath = filePath.Replace(element, value);
                    }
                }
                return filePath;
            }
        }

        public static async Task<string> GetChannelFilePathRuleAsync(Site site, int channelId)
        {
            var channelFilePathRule = await GetChannelFilePathRuleAsync(site.Id, channelId);
            if (string.IsNullOrEmpty(channelFilePathRule))
            {
                channelFilePathRule = site.ChannelFilePathRule;

                if (string.IsNullOrEmpty(channelFilePathRule))
                {
                    channelFilePathRule = ChannelFilePathRules.DefaultRule;
                }
            }
            return channelFilePathRule;
        }

        private static async Task<string> GetChannelFilePathRuleAsync(int siteId, int channelId)
        {
            if (channelId == 0) return string.Empty;
            var nodeInfo = await DataProvider.ChannelRepository.GetAsync(channelId);
            if (nodeInfo == null) return string.Empty;

            var filePathRule = nodeInfo.ChannelFilePathRule;
            if (string.IsNullOrEmpty(filePathRule) && nodeInfo.ParentId != 0)
            {
                filePathRule = await GetChannelFilePathRuleAsync(siteId, nodeInfo.ParentId);
            }

            return filePathRule;
        }

        public static async Task<string> GetContentFilePathRuleAsync(Site site, int channelId)
        {
            var contentFilePathRule = await GetContentFilePathRuleAsync(site.Id, channelId);
            if (string.IsNullOrEmpty(contentFilePathRule))
            {
                contentFilePathRule = site.ContentFilePathRule;

                if (string.IsNullOrEmpty(contentFilePathRule))
                {
                    contentFilePathRule = ContentFilePathRules.DefaultRule;
                }
            }
            return contentFilePathRule;
        }

        private static async Task<string> GetContentFilePathRuleAsync(int siteId, int channelId)
        {
            if (channelId == 0) return string.Empty;
            var nodeInfo = await DataProvider.ChannelRepository.GetAsync(channelId);
            if (nodeInfo == null) return string.Empty;

            var filePathRule = nodeInfo.ContentFilePathRule;
            if (string.IsNullOrEmpty(filePathRule) && nodeInfo.ParentId != 0)
            {
                filePathRule = await GetContentFilePathRuleAsync(siteId, nodeInfo.ParentId);
            }

            return filePathRule;
        }

        public static async Task<string> GetChannelPageFilePathAsync(Site site, int channelId, int currentPageIndex)
        {
            var nodeInfo = await DataProvider.ChannelRepository.GetAsync(channelId);
            if (nodeInfo.ParentId == 0)
            {
                var templateInfo = await DataProvider.TemplateRepository.GetDefaultTemplateAsync(site.Id, TemplateType.IndexPageTemplate);
                return await GetIndexPageFilePathAsync(site, templateInfo.CreatedFileFullName, site.Root, currentPageIndex);
            }
            var filePath = nodeInfo.FilePath;

            if (string.IsNullOrEmpty(filePath))
            {
                filePath = await ChannelFilePathRules.ParseAsync(site, channelId);
            }

            filePath = await MapPathAsync(site, filePath);// PathUtils.Combine(sitePath, filePath);
            if (PathUtils.IsDirectoryPath(filePath))
            {
                filePath = PathUtils.Combine(filePath, channelId + ".html");
            }
            DirectoryUtils.CreateDirectoryIfNotExists(filePath);

            if (currentPageIndex != 0)
            {
                string appendix = $"_{(currentPageIndex + 1)}";
                var fileName = PathUtils.GetFileNameWithoutExtension(filePath) + appendix + PathUtils.GetExtension(filePath);
                filePath = PathUtils.Combine(DirectoryUtils.GetDirectoryPath(filePath), fileName);
            }

            return filePath;
        }

        public static async Task<string> GetContentPageFilePathAsync(Site site, int channelId, int contentId, int currentPageIndex)
        {
            var contentInfo = await DataProvider.ContentRepository.GetAsync(site, channelId, contentId);
            return await GetContentPageFilePathAsync(site, channelId, contentInfo, currentPageIndex);
        }

        public static async Task<string> GetContentPageFilePathAsync(Site site, int channelId, Content content, int currentPageIndex)
        {
            var filePath = await ContentFilePathRules.ParseAsync(site, channelId, content);

            filePath = await MapPathAsync(site, filePath);
            if (PathUtils.IsDirectoryPath(filePath))
            {
                filePath = PathUtils.Combine(filePath, content.Id + ".html");
            }
            DirectoryUtils.CreateDirectoryIfNotExists(filePath);

            if (currentPageIndex == 0) return filePath;

            string appendix = $"_{currentPageIndex + 1}";
            var fileName = PathUtils.GetFileNameWithoutExtension(filePath) + appendix + PathUtils.GetExtension(filePath);
            filePath = PathUtils.Combine(DirectoryUtils.GetDirectoryPath(filePath), fileName);

            return filePath;
        }

        public static bool IsImageExtensionAllowed(Site site, string fileExtention)
        {
            return PathUtils.IsFileExtensionAllowed(site.ImageUploadTypeCollection, fileExtention);
        }

        public static bool IsImageSizeAllowed(Site site, long contentLength)
        {
            return contentLength <= site.ImageUploadTypeMaxSize * 1024;
        }

        public static bool IsVideoExtensionAllowed(Site site, string fileExtention)
        {
            return PathUtils.IsFileExtensionAllowed(site.VideoUploadTypeCollection, fileExtention);
        }

        public static bool IsVideoSizeAllowed(Site site, int contentLength)
        {
            return contentLength <= site.VideoUploadTypeMaxSize * 1024;
        }

        public static bool IsFileExtensionAllowed(Site site, string fileExtention)
        {
            var typeCollection = site.FileUploadTypeCollection + "," + site.ImageUploadTypeCollection + "," + site.VideoUploadTypeCollection;
            return PathUtils.IsFileExtensionAllowed(typeCollection, fileExtention);
        }

        public static bool IsFileSizeAllowed(Site site, int contentLength)
        {
            return contentLength <= site.FileUploadTypeMaxSize * 1024;
        }

        public static bool IsUploadExtensionAllowed(UploadType uploadType, Site site, string fileExtention)
        {
            if (uploadType == UploadType.Image)
            {
                return IsImageExtensionAllowed(site, fileExtention);
            }
            else if (uploadType == UploadType.Video)
            {
                return IsVideoExtensionAllowed(site, fileExtention);
            }
            else if (uploadType == UploadType.File)
            {
                return IsFileExtensionAllowed(site, fileExtention);
            }
            return false;
        }

        public static bool IsUploadSizeAllowed(UploadType uploadType, Site site, int contentLength)
        {
            switch (uploadType)
            {
                case UploadType.Image:
                    return IsImageSizeAllowed(site, contentLength);
                case UploadType.Video:
                    return IsVideoSizeAllowed(site, contentLength);
                case UploadType.File:
                    return IsFileSizeAllowed(site, contentLength);
            }
            return false;
        }

        public static string GetBinDirectoryPath(string relatedPath)
        {
            relatedPath = PathUtils.RemoveParentPath(relatedPath);
            return PathUtils.Combine(WebConfigUtils.PhysicalApplicationPath, DirectoryUtils.Bin.DirectoryName, relatedPath);
        }

        public static string GetAdminDirectoryPath(string relatedPath)
        {
            relatedPath = PathUtils.RemoveParentPath(relatedPath);
            return PathUtils.Combine(WebConfigUtils.PhysicalApplicationPath, WebConfigUtils.AdminDirectory, relatedPath);
        }

        public static string GetHomeDirectoryPath(string relatedPath)
        {
            relatedPath = PathUtils.RemoveParentPath(relatedPath);
            return PathUtils.Combine(WebConfigUtils.PhysicalApplicationPath, WebConfigUtils.HomeDirectory, relatedPath);
        }

        public static string PhysicalSiteServerPath => PathUtils.Combine(WebConfigUtils.PhysicalApplicationPath, Constants.AdminRootDirectory);

        public static string PhysicalSiteFilesPath => PathUtils.Combine(WebConfigUtils.PhysicalApplicationPath, DirectoryUtils.SiteFiles.DirectoryName);

        public static string GetLibraryFilePath(string virtualUrl)
        {
            return PathUtils.Combine(WebConfigUtils.PhysicalApplicationPath, virtualUrl);
        }
    }
}
