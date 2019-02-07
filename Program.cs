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

using SteamGiveawaysBot.Configuration;
using SteamGiveawaysBot.Entities;
using SteamGiveawaysBot.Exceptions;
using SteamGiveawaysBot.Logging;
using SteamGiveawaysBot.Processors;
using SteamGiveawaysBot.Processors.GiveawayProcessors;
using SteamGiveawaysBot.Service;
using SteamGiveawaysBot.SteamAccountDistributor;
using SteamGiveawaysBot.SteamAccountDistributor.Models;
using SteamGiveawaysBot.SteamAccountDistributor.Security;
using SteamGiveawaysBot.Utils;

namespace SteamGiveawaysBot
{
    public sealed class Program
    {
        static TimeSpan RetryDelay => TimeSpan.FromSeconds(10);

        static AccountDistributorSettings accountDistributorSettings;
        static DebugSettings debugSettings;
        static MailSettings mailSettings;
        static SteamSettings steamSettings;

        static ILogger logger;
        
        static TimeSpan ConnectionWaitTime => TimeSpan.FromMinutes(15);

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
                .AddSingleton<IHmacEncoder<SteamAccountRequest>, SteamAccountRequestHmacEncoder>()
                .AddSingleton<IHmacEncoder<SteamAccountResponse>, SteamAccountResponseHmacEncoder>()
                .AddSingleton<IMailNotifier, GmailMailNotifier>()
                .AddSingleton<ILogger, Logger>()
                .AddSingleton<IAccountService, AccountService>()
                .AddSingleton<IBotService, BotService>()
                .BuildServiceProvider();

            logger = serviceProvider.GetService<ILogger>();

            logger.Info($"Application started");

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
            if (!IsInternetConnected())
            {
                logger.Error($"No internet connection. Waiting {ConnectionWaitTime.TotalMinutes} minutes before retrying");
                Thread.Sleep((int)ConnectionWaitTime.TotalMilliseconds);
            }

            if (IsInternetConnected())
            {
                if (accountDistributorSettings.IsEnabled)
                {
                    UpdateAccountDetails(serviceProvider);
                }

                IBotService botService = serviceProvider.GetService<IBotService>();

                botService.Run();
            }
        }

        static bool IsInternetConnected()
        {
            try
            {
                using (var client = new WebClient())
                using (client.OpenRead("http://clients3.google.com/generate_204"))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        static void UpdateAccountDetails(IServiceProvider serviceProvider)
        {
            try
            {
                string oldUsername = steamSettings.Username;
                string oldPassword = steamSettings.Password;

                IAccountService accountService = serviceProvider.GetService<IAccountService>();
                AccountDetails account = accountService.GatherAccount().Result; // TODO: Broken async

                steamSettings.Username = account.Username;
                steamSettings.Password = account.Password;

                string appsettings = File.ReadAllText(ApplicationPaths.AppSettingsFile)
                    .Replace(oldUsername, account.Username)
                    .Replace(oldPassword, oldPassword);
                
                File.WriteAllText(ApplicationPaths.AppSettingsFile, appsettings);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + Environment.NewLine + ex.StackTrace);
                logger.Info($"Unable to receive account details. Using default ones instead");
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
