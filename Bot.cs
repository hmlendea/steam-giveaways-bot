using System;
using System.Net;
using System.Threading;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

using SteamGiveawaysBot.Entities;
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

        AccountDetails account;
        TimeSpan ConnectionWaitTime => TimeSpan.FromMinutes(15);

        public Bot(AccountDetails account)
        {
            this.account = account;
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
                try
                {
                    if (!IsInternetConnected())
                    {
                        Log.Error($"No internet connection. Waiting {ConnectionWaitTime.TotalMinutes} minutes before retrying");
                        Thread.Sleep((int)ConnectionWaitTime.TotalMilliseconds);
                    }

                    if (IsInternetConnected())
                    {
                        DoStuff();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"{ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        void DoStuff()
        {
            IWebDriver driver = SetupDriver();

            try
            {
                SteamProcessor steamProcessor = new SteamProcessor(driver, account);
                Log.Info($"Logging in {account.Username} on Steam");
                steamProcessor.LogIn();
                steamProcessor.GatherProfileInfo();
                steamProcessor.Close();

                SteamGiftsProcessor steamGiftsProcessor = new SteamGiftsProcessor(driver, account);
                steamGiftsProcessor.Run();
                steamGiftsProcessor.Stop();
            }
            catch
            {
                driver.Quit();
                throw;
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
            options.AddArgument("--start-maximized");
            options.AddArgument("--headless");
            options.AddArgument("--disable-gpu");
			options.AddArgument("--window-size=1920,1080");

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
