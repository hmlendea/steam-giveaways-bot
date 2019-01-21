using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

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
            AccountDetails account = new AccountDetails
            {
                Username = args[0],
                Password = args[1]
            };

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
        }

        static IWebDriver SetupDriver()
        {
            ChromeOptions options = new ChromeOptions();
            options.PageLoadStrategy = PageLoadStrategy.None;
            options.AddArgument("--silent");

            ChromeDriverService service = ChromeDriverService.CreateDefaultService();
            service.SuppressInitialDiagnosticInformation = true;
            service.HideCommandPromptWindow = true;

            IWebDriver driver = new ChromeDriver(service, options);
            driver.Manage().Window.Maximize();

            return driver;
        }
    }
}
