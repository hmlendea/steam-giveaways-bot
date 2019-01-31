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

        public event EventHandler Started;

        public event EventHandler Stopped;

        public TimeSpan TimerInterval
        {
            get { return TimeSpan.FromMilliseconds(timer.Interval); }
            set { UpdateTimerInterval(value); }
        }

        public TimeSpan RechargeInterval => TimeSpan.FromMinutes(30);

        protected TimeSpan DefaultInterval => TimeSpan.FromSeconds(5);

        protected AccountDetails account;

        Timer timer;

        DateTime LastRewardsChekingDate { get; set; }

        TimeSpan RewardsCheckingInterval => TimeSpan.FromHours(3);

        public GiveawayProcessor(IWebDriver driver, AccountDetails account)
            : base(driver)
        {
            this.account = account;

            SetUp();
        }

        public void Start()
        {
            //Log.Info($"Starting {GetType().Name} bot for {account.Username}");
            IsRunning = true;
            OnStarted(new EventArgs());

            try
            {
                GatherInfo();

                if (!IsLoggedIn)
                {
                    LogIn();
                }

                timer.Enabled = true;
                timer.Start();
            }
            catch (Exception ex)
            {
                Log.Error($"{ex.Message}\n{ex.StackTrace}");
                Stop();
                throw;
            }
        }

        public void Stop()
        {
            Log.Info($"Stopping {GetType().Name} bot for {account.Username}");

            IsRunning = false;
            timer.Enabled = false;
            timer.Stop();

            OnStopped(new EventArgs());
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

        void SetUp()
        {
            LastRewardsChekingDate = DateTime.Now.AddYears(-10);

            timer = new Timer(DefaultInterval.TotalMilliseconds);
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

        void OnStarted(EventArgs e) => Started?.Invoke(this, e);
        void OnStopped(EventArgs e) => Stopped?.Invoke(this, e);

        void timer_OnTimedEvent(object source, ElapsedEventArgs e)
        {
            try
            {
                GatherInfo();

                if (CurrentRewards > 0)
                {
                    if (LastRewardsChekingDate.Add(RewardsCheckingInterval) < DateTime.Now)
                    {
                        ProcessRewards();
                    }
                }

                JoinGiveaway();
            }
            catch (Exception ex)
            {
                Log.Error($"{ex.Message}");
                Stop();
                throw;
            }
        }
    }
}
