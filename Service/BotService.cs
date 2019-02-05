using System;
using System.Net;
using System.Threading;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

using SteamGiveawaysBot.Configuration;
using SteamGiveawaysBot.Entities;
using SteamGiveawaysBot.Exceptions;
using SteamGiveawaysBot.Logging;
using SteamGiveawaysBot.Processors;
using SteamGiveawaysBot.Processors.GiveawayProcessors;
using SteamGiveawaysBot.Utils;

namespace SteamGiveawaysBot.Service
{
    public class BotService : IBotService
    {
        public event EventHandler Started;

        public event EventHandler Stopped;

        public bool IsRunning { get; private set; }

        readonly DebugSettings debugSettings;

        readonly SteamSettings steamSettings;

        readonly IMailNotifier mailNotifier;

        readonly ILogger logger;

        readonly AccountDetails account;
        
        TimeSpan ConnectionWaitTime => TimeSpan.FromMinutes(15);

        public BotService(
            DebugSettings debugSettings,
            SteamSettings steamSettings,
            IMailNotifier mailNotifier,
            ILogger logger)
        {
            this.debugSettings = debugSettings;
            this.steamSettings = steamSettings;
            this.mailNotifier = mailNotifier;
            this.logger = logger;

            account = new AccountDetails
            {
                Username = steamSettings.Username,
                Password = steamSettings.Password
            };
        }

        public void Run()
        {
            IsRunning = true;
            OnStarted(new EventArgs());

            RunLoop();
            OnStopped(new EventArgs());
        }

        void RunLoop()
        {
            while (IsRunning)
            {
                IWebDriver driver = SetupDriver();

                try
                {
                    if (!IsInternetConnected())
                    {
                        logger.Error($"No internet connection. Waiting {ConnectionWaitTime.TotalMinutes} minutes before retrying");
                        Thread.Sleep((int)ConnectionWaitTime.TotalMilliseconds);
                    }

                    if (IsInternetConnected())
                    {
                        DoStuff(driver);
                    }
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
            
            steamProcessor.Close();

            steamGiftsProcessor.Run();
            steamGiftsProcessor.Stop();
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

        bool IsInternetConnected()
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

        void OnStarted(EventArgs e) => Started?.Invoke(this, e);
        void OnStopped(EventArgs e) => Stopped?.Invoke(this, e);
    }
}
