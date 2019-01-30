using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

using SteamGiveawaysBot.Entities;
using SteamGiveawaysBot.Utils;

namespace SteamGiveawaysBot.Processors.GiveawayProcessors
{
    public abstract class GiveawayProcessor : WebProcessor
    {
        public abstract string HomePageUrl { get; }

        public abstract string LogInUrl { get; }

        public abstract string RewardsUrl { get; }

        public bool IsRunning { get; private set; }

        public bool IsLoggedIn { get; protected set; }

        public bool IsSuspended { get; protected set; }

        public int CurrentPoints { get; protected set; }

        public int MinimumPoints { get; protected set; }

        public int MaximumPoints { get; protected set; }

        public int CurrentRewards { get; protected set; }

        public TimeSpan TimerInterval
        {
            get { return TimeSpan.FromMilliseconds(timer.Interval); }
            set { UpdateTimerInterval(value); }
        }

        public TimeSpan RechargeInterval => TimeSpan.FromMinutes(30);

        protected TimeSpan DefaultInterval => TimeSpan.FromSeconds(5);

        protected AccountDetails account;

        Timer timer;

        public GiveawayProcessor(IWebDriver driver, AccountDetails account)
            : base(driver)
        {
            this.account = account;

            SetTimer();
        }

        public void Start()
        {
            Log.Info($"Starting {GetType().Name} bot for {account.Username}");
            IsRunning = true;

            GatherInfo();

            if (!IsLoggedIn)
            {
                LogIn();
            }

            if (CurrentRewards > 0)
            {
                ProcessRewards();
            }

            timer.Enabled = true;
        }

        public void Stop()
        {
            Log.Info($"Stopping {GetType().Name} bot for {account.Username}");

            IsRunning = false;
            timer.Enabled = false;
        }

        public void JoinGiveaway()
        {
            Giveaway giveaway = FindGiveaway();
            ProcessGiveaway(giveaway);
        }

        public abstract void GatherInfo();

        public abstract void LogIn();
        
        protected abstract Giveaway FindGiveaway();
        
        protected abstract void ProcessGiveaway(Giveaway giveaway);

        protected abstract void ProcessRewards();

        void SetTimer()
        {
            timer = new Timer(10000);
            timer.Elapsed += timer_OnTimedEvent;
            timer.AutoReset = true;
            timer.Enabled = false;
        }

        void UpdateTimerInterval(TimeSpan interval)
        {
            int min = (int)(Math.Max(
                interval.TotalMilliseconds - 10000,
                interval.TotalMilliseconds - (int)(interval.TotalMilliseconds * 0.2)));

            int max = (int)(Math.Min(
                interval.TotalMilliseconds + 10000,
                interval.TotalMilliseconds + (int)(interval.TotalMilliseconds * 0.2)));

            timer.Interval = Random.Next(min, max);
        }

        void timer_OnTimedEvent(object source, ElapsedEventArgs e)
        {
            JoinGiveaway();
        }
    }
}
