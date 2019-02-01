using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

using SteamGiveawaysBot.Entities;
using SteamGiveawaysBot.Exceptions;
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

        public DateTime NextCheckTime;

        public TimeSpan RechargeWaitDuration => TimeSpan.FromMinutes(30);

        public TimeSpan DefaultWaitDuration => TimeSpan.FromMinutes(5);

        protected AccountDetails account;

        DateTime LastRewardsChekingDate { get; set; }

        TimeSpan RewardsCheckingInterval => TimeSpan.FromHours(3);

        public GiveawayProcessor(IWebDriver driver, AccountDetails account)
            : base(driver)
        {
            this.account = account;
        }

        public void Run()
        {
            //Log.Info($"Starting {GetType().Name} bot for {account.Username}");

            IsRunning = true;
            OnStarted(new EventArgs());

            GatherInfo();

            if (!IsLoggedIn)
            {
                LogIn();
            }

            RunLoop();
            OnStopped(new EventArgs());
        }

        public void Stop()
        {
            Log.Info($"Stopping {GetType().Name} bot for {account.Username}");

            IsRunning = false;

            OnStopped(new EventArgs());
        }

        public abstract void GatherInfo();

        public abstract void LogIn();
        
        protected abstract Giveaway FindGiveaway();
        
        protected abstract void ProcessGiveaway(Giveaway giveaway);

        protected abstract void ProcessRewards();

        void RunLoop()
        {
            while (IsRunning)
            {
                GatherInfo();

                if (IsSuspended)
                {
                    throw new AccountSuspendedException(account.Username);
                }

                if (CurrentRewards > 0)
                {
                    if (LastRewardsChekingDate.Add(RewardsCheckingInterval) < DateTime.Now)
                    {
                        ProcessRewards();
                    }
                }

                if (CurrentPoints >= MinimumPoints)
                {
                    Giveaway giveaway = FindGiveaway();
                    ProcessGiveaway(giveaway);
                }
                else
                {
                    Log.Info($"Waiting {RechargeWaitDuration.TotalMinutes} minutes to recharge points ({CurrentPoints}/{MaximumPoints}, Min: {MinimumPoints})");
                    
                    NextCheckTime = DateTime.Now + RechargeWaitDuration;
                }

                if (NextCheckTime > DateTime.Now)
                {
                    TimeSpan waitTime = NextCheckTime - DateTime.Now;

                    Wait(waitTime);
                }
            }
        }

        void OnStarted(EventArgs e) => Started?.Invoke(this, e);
        void OnStopped(EventArgs e) => Stopped?.Invoke(this, e);
    }
}
