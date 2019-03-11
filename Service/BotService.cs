using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

using SteamGiveawaysBot.Communication.Notifications;
using SteamGiveawaysBot.Configuration;
using SteamGiveawaysBot.Entities;
using SteamGiveawaysBot.Exceptions;
using SteamGiveawaysBot.Logging;
using SteamGiveawaysBot.Processors;
using SteamGiveawaysBot.Processors.GiveawayProcessors;
using SteamGiveawaysBot.Security;

namespace SteamGiveawaysBot.Service
{
    public class BotService : IBotService
    {
        public bool IsRunning { get; private set; }

        readonly DebugSettings debugSettings;

        readonly SteamSettings steamSettings;

        readonly ICredentialsEncryptor credentialsEncryptor;

        readonly IMailNotifier mailNotifier;

        readonly ILogger logger;

        readonly AccountDetails account;

        public BotService(
            DebugSettings debugSettings,
            SteamSettings steamSettings,
            ICredentialsEncryptor credentialsEncryptor,
            IMailNotifier mailNotifier,
            ILogger logger)
        {
            this.debugSettings = debugSettings;
            this.steamSettings = steamSettings;
            this.credentialsEncryptor = credentialsEncryptor;
            this.mailNotifier = mailNotifier;
            this.logger = logger;

            account = new AccountDetails
            {
                Username = steamSettings.Username,
                Password = credentialsEncryptor.Decrypt(steamSettings.Password)
            };
        }

        public void Run()
        {
            IsRunning = true;

            RunLoop();
        }

        void RunLoop()
        {
            while (IsRunning)
            {
                IWebDriver driver = SetupDriver();

                try
                {
                    DoStuff(driver);
                }
                catch (AccountSuspendedException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    if (debugSettings.IsCrashScreenshotEnabled)
                    {
                        Screenshot crashScreenshot = ((ITakesScreenshot) driver).GetScreenshot();
                        crashScreenshot.SaveAsFile(debugSettings.CrashScreenshotFilePath);
                    }
                    
                    driver.Quit();
                    logger.Error($"{ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        void DoStuff(IWebDriver driver)
        {
            SteamProcessor steamProcessor = new SteamProcessor(driver, account, logger);
            steamProcessor.LogIn();
            steamProcessor.VisitChat();

            IEnumerable<uint> ownedAppIds = steamProcessor.GatherOwnedGames();
            PlayGames(ownedAppIds);

            SteamGiftsProcessor steamGiftsProcessor =
                new SteamGiftsProcessor(driver, account, mailNotifier, logger);

            try
            {
                steamGiftsProcessor.LogIn();
            }
            catch (AccountNotRegisteredException)
            {
                steamProcessor.GatherProfileInfo();
                steamProcessor.UpdatePrivacySettings();
                
                steamGiftsProcessor.Register();
                steamGiftsProcessor.ApplyCustomSettings();
            }
            
            steamProcessor.Dispose();

            steamGiftsProcessor.Run();
            steamGiftsProcessor.Stop();
            steamGiftsProcessor.Dispose();
        }

        void PlayGames(IEnumerable<uint> appIds)
        {
            int gamesCount = appIds.Count();
            logger.Info($"Playing {gamesCount} games on Steam for {account.Username}");

            using (SteamClientProcessor steamClientProcessor = new SteamClientProcessor(account))
            {
                steamClientProcessor.LogIn();
                steamClientProcessor.PlayGames(appIds, TimeSpan.FromSeconds(5));
                steamClientProcessor.LogOut();
            }
        }

        IWebDriver SetupDriver()
        {
            ChromeOptions options = new ChromeOptions();
            options.PageLoadStrategy = PageLoadStrategy.None;
            options.AddArgument("--silent");
			options.AddArgument("--disable-translate");
			options.AddArgument("--disable-infobars");
            options.AddArgument("--disable-extensions");

            if (debugSettings.IsHeadless)
            {
                options.AddArgument("--headless");
                options.AddArgument("--disable-gpu");
                options.AddArgument("--window-size=1920,1080");
                options.AddArgument("--start-maximized");
                options.AddArgument("--blink-settings=imagesEnabled=false");
            }

            ChromeDriverService service = ChromeDriverService.CreateDefaultService();
            service.SuppressInitialDiagnosticInformation = true;
            service.HideCommandPromptWindow = true;

            IWebDriver driver = new ChromeDriver(service, options);
            driver.Manage().Window.Maximize();

            return driver;
        }
    }
}
