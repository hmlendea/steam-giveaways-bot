using System;

using NuciLog.Core;
using NuciWeb;

using SteamGiveawaysBot.Entities;
using SteamGiveawaysBot.Exceptions;
using SteamGiveawaysBot.Logging;

namespace SteamGiveawaysBot.Processors.GiveawayProcessors
{
    public abstract class GiveawayProcessor : IGiveawayProcessor
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

        protected Giveaway LastProcessedGiveaway;

        protected readonly IWebProcessor webProcessor;
        protected readonly AccountDetails accountDetails;
        protected readonly ILogger logger;

        DateTime LastRewardsChekingDate { get; set; }

        TimeSpan RewardsCheckingInterval => TimeSpan.FromHours(3);

        public GiveawayProcessor(
            IWebProcessor webProcessor,
            AccountDetails accountDetails,
            ILogger logger)
        {
            this.webProcessor = webProcessor;
            this.accountDetails = accountDetails;
            this.logger = logger;

            IsRegistered = true;
        }

        public void Quit()
        {
            webProcessor.Dispose();
        }

        public void Run()
        {
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
                new LogInfo(MyLogInfoKey.Username, accountDetails.Username));

            IsRunning = false;
        }

        public abstract void LogIn();

        public abstract void Register();

        public abstract void ApplyCustomSettings();

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
                    throw new AccountSuspendedException(accountDetails.Username);
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

                webProcessor.Wait(NextCheckTime);
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
                    new LogInfo(MyLogInfoKey.GiveawayId, giveaway.Id),
                    new LogInfo(MyLogInfoKey.Price, giveaway.Price),
                    new LogInfo(MyLogInfoKey.GameTitle, giveaway.GameTitle),
                    new LogInfo(MyLogInfoKey.EndTime, giveawayEndTimestamp));
            }
            catch (UnjoinableGiveawayException ex)
            {
                logger.Warn(
                    MyOperation.JoinGiveaway,
                    OperationStatus.Failure,
                    new LogInfo(MyLogInfoKey.GiveawayId, giveaway.Id),
                    new LogInfo(MyLogInfoKey.Reason, ex.Reason),
                    new LogInfo(MyLogInfoKey.EndTime, giveawayEndTimestamp));
            }
        }
    }
}
