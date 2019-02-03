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

        public abstract string RegistrationUrl { get; }

        public abstract string LogInUrl { get; }

        public abstract string RewardsUrl { get; }

        public abstract string SettingsUrl { get; }

        public bool IsRunning { get; private set; }

        public bool IsLoggedIn { get; protected set; }

        public bool IsRegistered { get; protected set; }

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

        protected Giveaway LastProcessedGiveaway;

        DateTime LastRewardsChekingDate { get; set; }

        TimeSpan RewardsCheckingInterval => TimeSpan.FromHours(3);

        public GiveawayProcessor(IWebDriver driver, AccountDetails account)
            : base(driver)
        {
            this.account = account;

            IsRegistered = true;
        }

        public void Run()
        {
            //Log.Info($"Starting {GetType().Name} bot for {account.Username}");

            CurrentPoints = MaximumPoints;
            IsRunning = true;
            OnStarted(new EventArgs());

            GatherInfo();

            if (!IsLoggedIn)
            {
                LogIn();
            }

            if (!IsRegistered)
            {
                Register();
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

        public abstract void LogIn();

        public abstract void Register();

        protected abstract void GatherInfo();
        
        protected abstract Giveaway FindGiveaway();
        
        protected abstract void JoinGiveaway(Giveaway giveaway);

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

                if (CurrentRewards > 0 && DateTime.Now > LastRewardsChekingDate + RewardsCheckingInterval)
                {
                    Log.Info($"Processing the rewards ({CurrentRewards})");
                    ProcessRewards();

                    LastRewardsChekingDate = DateTime.Now;
                }

                if (CurrentPoints >= MinimumPoints)
                {
                    FindAndJoinGiveaway();
                }
                else
                {
                    Log.Info($"Waiting {RechargeWaitDuration.TotalMinutes} minutes to recharge the points to at least {MinimumPoints} (Currently {CurrentPoints}/{MaximumPoints})");
                    
                    NextCheckTime = DateTime.Now + RechargeWaitDuration;
                }

                Wait(NextCheckTime);
            }
        }

        void FindAndJoinGiveaway()
        {
            Giveaway giveaway = FindGiveaway();

            if (giveaway is null)
            {
                Log.Warn($"Cannot find any giveaway to join. Waiting {DefaultWaitDuration.TotalMinutes} minuts");
                NextCheckTime = DateTime.Now + DefaultWaitDuration;
                return;
            }

            string giveawayEndTimestamp = giveaway.EndTime.ToString("HH:mm");

            LastProcessedGiveaway = giveaway;
            NextCheckTime = giveaway.EndTime.AddSeconds(-5);

            try
            {
                JoinGiveaway(giveaway);
                Log.Info($"Joined a giveaway for '{giveaway.GameTitle}' ({giveaway.GiveawayCode}) with {giveaway.Price} points. Ends: {giveawayEndTimestamp}");
            }
            catch (UnjoinableGiveawayException ex)
            {
                Log.Warn($"{ex.Message}. Ends: {giveawayEndTimestamp}");
            }
        }

        void OnStarted(EventArgs e) => Started?.Invoke(this, e);
        void OnStopped(EventArgs e) => Stopped?.Invoke(this, e);
    }
}
