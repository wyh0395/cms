﻿using System;
using System.Threading.Tasks;
using Datory;
using Mono.Options;
using SS.CMS.Abstractions;
using SS.CMS.Cli.Core;
using SS.CMS.Core;
using SS.CMS.Framework;
using Microsoft.Extensions.DependencyInjection;

namespace SS.CMS.Cli.Services
{
    public class InstallJob
    {
        public const string CommandName = "install";

        public static async Task Execute(IJobContext context)
        {
            var application = CliUtils.Provider.GetService<InstallJob>();
            await application.RunAsync(context);
        }

        private string _configFile;
        private string _userName;
        private string _password;
        private bool _isHelp;

        private readonly OptionSet _options;

        public InstallJob()
        {
            _options = new OptionSet {
                { "c|config-file=", "指定配置文件Web.config路径或文件名",
                    v => _configFile = v },
                { "u|userName=", "超级管理员用户名",
                    v => _userName = v },
                { "p|password=", "超级管理员密码",
                    v => _password = v },
                { "h|help",  "命令说明",
                    v => _isHelp = v != null }
            };
        }

        public static void PrintUsage()
        {
            Console.WriteLine("系统安装: siteserver install");
            var job = new InstallJob();
            job._options.WriteOptionDescriptions(Console.Out);
            Console.WriteLine();
        }

        public async Task RunAsync(IJobContext context)
        {
            if (!CliUtils.ParseArgs(_options, context.Args)) return;

            if (_isHelp)
            {
                PrintUsage();
                return;
            }

            var webConfigPath = CliUtils.GetWebConfigPath(_configFile);
            if (!FileUtils.IsFileExists(webConfigPath))
            {
                await CliUtils.PrintErrorAsync($"系统配置文件不存在：{webConfigPath}！");
                return;
            }

            if (string.IsNullOrEmpty(WebConfigUtils.ConnectionString))
            {
                await CliUtils.PrintErrorAsync($"{webConfigPath} 中数据库连接字符串 connectionString 未设置");
                return;
            }

            if (string.IsNullOrEmpty(_userName))
            {
                await CliUtils.PrintErrorAsync("未设置参数管理员用户名：{userName} ！");
                return;
            }

            if (string.IsNullOrEmpty(_password))
            {
                await CliUtils.PrintErrorAsync("未设置参数管理员密码：{password} ！");
                return;
            }

            if (_password.Length < 6)
            {
                await CliUtils.PrintErrorAsync("管理员密码必须大于6位 ！");
                return;
            }

            if (!PasswordRestrictionUtils.IsValid(_password, PasswordRestriction.LetterAndDigit.GetValue()))
            {
                await CliUtils.PrintErrorAsync($"管理员密码不符合规则，请包含{PasswordRestriction.LetterAndDigit.GetDisplayName()}");
                return;
            }

            WebConfigUtils.Load(CliUtils.PhysicalApplicationPath, webConfigPath);

            await Console.Out.WriteLineAsync($"数据库类型: {WebConfigUtils.DatabaseType.GetValue()}");
            await Console.Out.WriteLineAsync($"连接字符串: {WebConfigUtils.ConnectionString}");
            await Console.Out.WriteLineAsync($"系统文件夹: {CliUtils.PhysicalApplicationPath}");

            var (isConnectionWorks, errorMessage) = await WebConfigUtils.Database.IsConnectionWorksAsync();
            if (!isConnectionWorks)
            {
                await CliUtils.PrintErrorAsync($"数据库连接错误：{errorMessage}");
                return;
            }

            if (!await DataProvider.ConfigRepository.IsNeedInstallAsync())
            {
                await CliUtils.PrintErrorAsync("系统已安装在 web.config 指定的数据库中，命令执行失败");
                return;
            }

            WebConfigUtils.UpdateWebConfig(WebConfigUtils.IsProtectData, WebConfigUtils.DatabaseType, WebConfigUtils.ConnectionString, WebConfigUtils.RedisConnectionString, WebConfigUtils.AdminDirectory, WebConfigUtils.HomeDirectory, StringUtils.GetShortGuid(), false);

            DataProvider.Reset();

            await DataProvider.DatabaseRepository.InstallDatabaseAsync(_userName, _password);

            await Console.Out.WriteLineAsync("恭喜，系统安装成功！");
        }
    }
}
