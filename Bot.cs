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

            if (!IsInternetConnected())
            {
                Log.Error($"No internet connection. Waiting {ConnectionWaitTime.TotalMinutes} minutes before retrying");
                Thread.Sleep((int)ConnectionWaitTime.TotalMilliseconds);
            }

            if (IsInternetConnected())
            {
                try
                {

                    DoStuff();
                }
                catch (Exception ex)
                {
                    Log.Error($"{ex.Message}\n{ex.StackTrace}");
                    OnStopped(new EventArgs());
                }
            }
        }

        void DoStuff()
        {
            Log.Info($"Setting up the driver");
            IWebDriver driver = SetupDriver();

            SteamProcessor steamProcessor = new SteamProcessor(driver, account);
            SteamGiftsProcessor steamGiftsProcessor = new SteamGiftsProcessor(driver, account);

            try
            {
                Log.Info($"Logging in {account.Username} on Steam");
                steamProcessor.LogIn();
                steamProcessor.GatherProfileInfo();

                steamGiftsProcessor.Run();
            }
            catch
            {
                steamProcessor.Close();
                steamGiftsProcessor.Close();
                driver.Close();
                throw;
            }
        }

        IWebDriver SetupDriver()
        {
            ChromeOptions options = new ChromeOptions();
            options.PageLoadStrategy = PageLoadStrategy.None;
            //options.AddArgument("headless");
            options.AddArgument("--silent");

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
