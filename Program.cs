using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using Microsoft.Extensions.Configuration;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

using SteamGiveawaysBot.Entities;
using SteamGiveawaysBot.Processors;
using SteamGiveawaysBot.Processors.GiveawayProcessors;
using SteamGiveawaysBot.Utils;

namespace SteamGiveawaysBot
{
    public sealed class Program
    {
        static void Main(string[] args)
        {
            IConfiguration config = LoadConfiguration();

            AccountDetails account = new AccountDetails
            {
                Username = config["steamUsername"],
                Password = config["steamPassword"]
            };

            MailNotifier.RecipientAddress = config["mailRecipientAddress"];
            MailNotifier.SenderAddress = config["mailSenderAddress"];
            MailNotifier.SenderPassword = config["mailSenderPassword"];

            SetUpAccount(account);
        }

        static void SetUpAccount(AccountDetails account)
        {
            Log.Info($"Setting up the driver");
            IWebDriver driver = SetupDriver();

            SteamProcessor steamProcessor = new SteamProcessor(driver, account);
            Log.Info($"Logging in the Steam account ({account.Username})");
            steamProcessor.LogIn();

            Log.Info($"Gathering profile information");
            steamProcessor.GatherProfileInfo();

            SteamGiftsProcessor steamGiftsProcessor = new SteamGiftsProcessor(driver, account);
            Log.Info($"Logging in the SteamGifts account");
            steamGiftsProcessor.LogIn();
            
            Log.Info($"Starting the SteamGifts bot");
            steamGiftsProcessor.Start();
            
            while (true)
            {
                string cmd = Console.ReadLine().ToLower();

                if (cmd == "exit")
                {
                    steamGiftsProcessor.Stop();
                    Environment.Exit((int)ExitCodes.Success);
                }
            }
        }

        static IWebDriver SetupDriver()
        {
            ChromeOptions options = new ChromeOptions();
            options.PageLoadStrategy = PageLoadStrategy.None;
            options.AddArgument("headless");
            options.AddArgument("--silent");

            ChromeDriverService service = ChromeDriverService.CreateDefaultService();
            service.SuppressInitialDiagnosticInformation = true;
            service.HideCommandPromptWindow = true;

            IWebDriver driver = new ChromeDriver(service, options);
            driver.Manage().Window.Maximize();

            return driver;
        }

        static IConfiguration LoadConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");
 
            return new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .Build();
        }
    }
}
