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
        static TimeSpan RetryDelay => TimeSpan.FromSeconds(10);

        static AccountDetails account;

        static void Main(string[] args)
        {
            IConfiguration config = LoadConfiguration();

            account = new AccountDetails
            {
                Username = config["steamUsername"],
                Password = config["steamPassword"]
            };

            MailNotifier.RecipientAddress = config["mailRecipientAddress"];
            MailNotifier.SenderAddress = config["mailSenderAddress"];
            MailNotifier.SenderPassword = config["mailSenderPassword"];

            SetUpAccount();

            Console.ReadLine();
        }

        static void SetUpAccount()
        {
            Log.Info($"Setting up the driver");
            IWebDriver driver = SetupDriver();

            SteamProcessor steamProcessor = new SteamProcessor(driver, account);
            Log.Info($"Logging in the Steam account ({account.Username})");
            steamProcessor.LogIn();

            //Log.Info($"Gathering profile information");
            steamProcessor.GatherProfileInfo();

            SteamGiftsProcessor steamGiftsProcessor = new SteamGiftsProcessor(driver, account);
            steamGiftsProcessor.Stopped += steamGiftsProcessor_OnStopped;
            steamGiftsProcessor.Start();
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
            return new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .Build();
        }

        static void steamGiftsProcessor_OnStopped(object sender, EventArgs e)
        {
            Log.Info($"Waiting {RetryDelay.TotalSeconds} seconds to retry");
            Thread.Sleep((int)RetryDelay.TotalMilliseconds);
            SetUpAccount();
        }
    }
}
