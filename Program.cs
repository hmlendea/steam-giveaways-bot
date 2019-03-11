using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

using SteamGiveawaysBot.Communication.Notifications;
using SteamGiveawaysBot.Communication.SteamAccountDistributor;
using SteamGiveawaysBot.Communication.SteamAccountDistributor.Models;
using SteamGiveawaysBot.Communication.SteamAccountDistributor.Security;
using SteamGiveawaysBot.Configuration;
using SteamGiveawaysBot.Exceptions;
using SteamGiveawaysBot.Logging;
using SteamGiveawaysBot.Processors;
using SteamGiveawaysBot.Processors.GiveawayProcessors;
using SteamGiveawaysBot.Security;
using SteamGiveawaysBot.Service;

namespace SteamGiveawaysBot
{
    public sealed class Program
    {
        static TimeSpan RetryDelay => TimeSpan.FromSeconds(10);

        static AccountDistributorSettings accountDistributorSettings;
        static DebugSettings debugSettings;
        static MailSettings mailSettings;
        static SteamSettings steamSettings;

        static IAppSettingsUpdater appSettingsUpdater;

        static ILogger logger;
        
        static TimeSpan ConnectionWaitTime => TimeSpan.FromMinutes(5);

        static void Main(string[] args)
        {
            IConfiguration config = LoadConfiguration();

            accountDistributorSettings = new AccountDistributorSettings();
            debugSettings = new DebugSettings();
            mailSettings = new MailSettings();
            steamSettings = new SteamSettings();

            config.Bind(nameof(accountDistributorSettings), accountDistributorSettings);
            config.Bind(nameof(DebugSettings), debugSettings);
            config.Bind(nameof(MailSettings), mailSettings);
            config.Bind(nameof(SteamSettings), steamSettings);
            
            IServiceProvider serviceProvider = new ServiceCollection()
                .AddSingleton(accountDistributorSettings)
                .AddSingleton(debugSettings)
                .AddSingleton(mailSettings)
                .AddSingleton(steamSettings)
                .AddSingleton<IAppSettingsUpdater, AppSettingsUpdater>()
                .AddSingleton<IHmacEncoder<SteamAccountRequest>, SteamAccountRequestHmacEncoder>()
                .AddSingleton<IHmacEncoder<SteamAccountResponse>, SteamAccountResponseHmacEncoder>()
                .AddSingleton<ICredentialsEncryptor, CredentialsEncryptor>()
                .AddSingleton<IMailNotifier, GmailMailNotifier>()
                .AddSingleton<ILogger, Logger>()
                .AddSingleton<IAccountService, AccountService>()
                .AddSingleton<IBotService, BotService>()
                .BuildServiceProvider();

            logger = serviceProvider.GetService<ILogger>();
            logger.Info($"Application started");

            appSettingsUpdater = serviceProvider.GetService<IAppSettingsUpdater>();

            while (true)
            {
                try
                {
                    RunLoop(serviceProvider);
                }
                catch (Exception ex)
                {
                    logger.Fatal($"Fatal exception: {ex.Message}");
                    break;
                }
            }

            logger.Info($"Application stopped");
        }

        static void RunLoop(IServiceProvider serviceProvider)
        {
            WaitForInternet();

            if (accountDistributorSettings.IsEnabled)
            {
                UpdateAccountDetails(serviceProvider);
            }

            IBotService botService = serviceProvider.GetService<IBotService>();

            botService.Run();
        }

        static void WaitForInternet()
        {
            bool isConnected = false;

            while (!isConnected)
            {
                try
                {
                    using (var client = new WebClient())
                    using (client.OpenRead("http://clients3.google.com/generate_204"))
                    {
                        isConnected = true;
                        return;
                    }
                }
                catch
                {
                    isConnected = false;
                }

                logger.Error($"No internet connection. Waiting {ConnectionWaitTime.TotalMinutes} minutes before retrying");
                Thread.Sleep((int)ConnectionWaitTime.TotalMilliseconds);
            }
        }

        static void UpdateAccountDetails(IServiceProvider serviceProvider)
        {
            try
            {
                IAccountService accountService = serviceProvider.GetService<IAccountService>();
                SteamAccount account = accountService.GatherAccount().Result; // TODO: Broken async

                appSettingsUpdater.UpdateSetting("steamSettings.username", account.Username);
                appSettingsUpdater.UpdateSetting("steamSettings.password", account.Password);

                steamSettings.Username = account.Username;
                steamSettings.Password = account.Password;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + Environment.NewLine + ex.StackTrace);
                logger.Info($"Unable to update account details. Using default ones instead");
            }
        }
        
        static IConfiguration LoadConfiguration()
        {
            return new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .Build();
        }
    }
}
