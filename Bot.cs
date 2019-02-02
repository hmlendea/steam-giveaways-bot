using System;
using System.Net;
using System.Threading;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

using SteamGiveawaysBot.Entities;
using SteamGiveawaysBot.Exceptions;
using SteamGiveawaysBot.Processors;
using SteamGiveawaysBot.Processors.GiveawayProcessors;
using SteamGiveawaysBot.Utils;

namespace SteamGiveawaysBot
{
    public class Bot
    {
        public event EventHandler Started;

        public event EventHandler Stopped;

        public bool IsRunning { get; private set; }

        public bool IsHeadless { get; set; }

        AccountDetails account;
        TimeSpan ConnectionWaitTime => TimeSpan.FromMinutes(15);

        public Bot(AccountDetails account)
        {
            this.account = account;

            IsHeadless = true;
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
                        Log.Error($"No internet connection. Waiting {ConnectionWaitTime.TotalMinutes} minutes before retrying");
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
                    Screenshot crashScreenshot = ((ITakesScreenshot) driver).GetScreenshot();
                    crashScreenshot.SaveAsFile(Log.LogFilePath + ".png"); // TODO: Proper location

                    driver.Quit();
                    Log.Error($"{ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        void DoStuff(IWebDriver driver)
        {
            SteamProcessor steamProcessor = new SteamProcessor(driver, account);
            steamProcessor.LogIn();

            SteamGiftsProcessor steamGiftsProcessor = new SteamGiftsProcessor(driver, account);

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

            if (IsHeadless)
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
