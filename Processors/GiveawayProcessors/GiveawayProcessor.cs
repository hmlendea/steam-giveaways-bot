using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

using NuciLog.Core;
using NuciWeb;

using OpenQA.Selenium;

using SteamGiveawaysBot.Entities;
using SteamGiveawaysBot.Exceptions;
using SteamGiveawaysBot.Logging;
using SteamGiveawaysBot.Utils;

namespace SteamGiveawaysBot.Processors.GiveawayProcessors
{
    public abstract class GiveawayProcessor : WebProcessor
    {
        public abstract string ProviderName { get; }

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

        public DateTime NextCheckTime;

        public TimeSpan RechargeWaitDuration => TimeSpan.FromMinutes(30);

        public TimeSpan DefaultWaitDuration => TimeSpan.FromMinutes(5);

        protected AccountDetails account;

        protected Giveaway LastProcessedGiveaway;

        protected ILogger logger;

        DateTime LastRewardsChekingDate { get; set; }

        TimeSpan RewardsCheckingInterval => TimeSpan.FromHours(3);

        public GiveawayProcessor(
            IWebDriver driver,
            AccountDetails account,
            ILogger logger)
            : base(driver)
        {
            this.account = account;
            this.logger = logger;

            IsRegistered = true;
        }

        public void Run()
        {
            //Log.Info($"Starting {GetType().Name} bot for {account.Username}");

            CurrentPoints = MaximumPoints;
            IsRunning = true;

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
        }

        public void Stop()
        {
            logger.Info(
                MyOperation.BotStop,
                new LogInfo(MyLogInfoKey.GiveawayProcessor, GetType().Name),
                new LogInfo(MyLogInfoKey.Username, account.Username));

            IsRunning = false;
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
                    logger.Info(
                        MyOperation.RewardsProcessing,
                        OperationStatus.Started,
                        new LogInfo(MyLogInfoKey.RewardsCount, CurrentRewards));

                    ProcessRewards();

                    LastRewardsChekingDate = DateTime.Now;

                    logger.Debug(MyOperation.RewardsProcessing, OperationStatus.Success);
                }

                if (CurrentPoints >= MinimumPoints)
                {
                    FindAndJoinGiveaway();
                }
                else
                {
                    logger.Info(
                        MyOperation.RechargePoints,
                        new LogInfo(MyLogInfoKey.RechargeDuration, RechargeWaitDuration.TotalMilliseconds),
                        new LogInfo(MyLogInfoKey.MinimumPoints, MinimumPoints),
                        new LogInfo(MyLogInfoKey.CurrentPoints, CurrentPoints));
                    
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
                logger.Warn(
                    MyOperation.JoinGiveaway,
                    OperationStatus.Failure,
                    new LogInfo(MyLogInfoKey.Reason, "Cannot find any giveaway to join"),
                    new LogInfo(MyLogInfoKey.RetryDelay, DefaultWaitDuration.TotalMilliseconds));

                NextCheckTime = DateTime.Now + DefaultWaitDuration;
                return;
            }

            string giveawayEndTimestamp = giveaway.EndTime.ToString("HH:mm");

            LastProcessedGiveaway = giveaway;
            NextCheckTime = giveaway.EndTime.AddSeconds(-5);

            try
            {
                JoinGiveaway(giveaway);
                
                logger.Info(
                    MyOperation.JoinGiveaway,
                    OperationStatus.Success,
                    new LogInfo(MyLogInfoKey.GameTitle, giveaway.GameTitle),
                    new LogInfo(MyLogInfoKey.GiveawayId, giveaway.Id),
                    new LogInfo(MyLogInfoKey.Price, giveaway.Price),
                    new LogInfo(MyLogInfoKey.EndTime, giveawayEndTimestamp));
            }
            catch (UnjoinableGiveawayException ex)
            {
                logger.Warn(
                    MyOperation.JoinGiveaway,
                    OperationStatus.Failure,
                    new LogInfo(MyLogInfoKey.Reason, ex.Reason),
                    new LogInfo(MyLogInfoKey.EndTime, giveawayEndTimestamp));
            }
        }
    }
}
