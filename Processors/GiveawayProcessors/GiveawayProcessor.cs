using System;
using System.Collections;
using System.Collections.Generic;
using NuciLog.Core;
using NuciWeb.Automation;
using NuciWeb.Steam.Models;

using SteamGiveawaysBot.Entities;
using SteamGiveawaysBot.Exceptions;
using SteamGiveawaysBot.Logging;

namespace SteamGiveawaysBot.Processors.GiveawayProcessors
{
    public abstract class GiveawayProcessor(
        IWebProcessor webProcessor,
        SteamAccount steamAccount,
        ILogger logger) : IGiveawayProcessor
    {
        public abstract string ProviderName { get; }

        public abstract string HomePageUrl { get; }

        public abstract string RegistrationUrl { get; }

        public abstract string LogInUrl { get; }

        public abstract string RewardsUrl { get; }

        public abstract string SettingsUrl { get; }

        public bool IsRunning { get; private set; }

        public bool IsLoggedIn { get; protected set; }

        public bool IsRegistered { get; protected set; } = true;

        public bool IsSuspended { get; protected set; }

        public int CurrentPoints { get; protected set; }

        public int MinimumPoints { get; protected set; }

        public int MaximumPoints { get; protected set; }

        public int CurrentRewards { get; protected set; }

        public DateTime NextCheckTime;

        public static TimeSpan RechargeWaitDuration => TimeSpan.FromMinutes(30);

        public static TimeSpan DefaultWaitDuration => TimeSpan.FromMinutes(5);

        protected Giveaway LastProcessedGiveaway;

        protected readonly IWebProcessor webProcessor = webProcessor;
        protected readonly SteamAccount steamAccount = steamAccount;
        protected readonly ILogger logger = logger;

        DateTime LastRewardsChekingDate { get; set; }

        static TimeSpan RewardsCheckingInterval => TimeSpan.FromHours(3);

        public void Run()
        {
            CurrentPoints = MaximumPoints;
            IsRunning = true;

            GatherInfo();

            if (!IsLoggedIn)
            {
                LogIn();
            }

            RunLoop();
        }

        public void Stop()
        {
            logger.Info(
                MyOperation.BotStop,
                new LogInfo(MyLogInfoKey.GiveawayProcessor, GetType().Name),
                new LogInfo(MyLogInfoKey.Username, steamAccount.Username));

            IsRunning = false;
        }

        public abstract void LogIn();

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
                    throw new AccountSuspendedException(steamAccount.Username);
                }

                if (CurrentRewards > 0 && DateTime.Now > LastRewardsChekingDate + RewardsCheckingInterval)
                {
                    IEnumerable<LogInfo> logInfos = [
                        new(MyLogInfoKey.RewardsCount, CurrentRewards)
                    ];

                    logger.Info(
                        MyOperation.RewardsProcessing,
                        OperationStatus.Started,
                        logInfos);

                    ProcessRewards();

                    LastRewardsChekingDate = DateTime.Now;

                    logger.Info(
                        MyOperation.RewardsProcessing,
                        OperationStatus.Success,
                        logInfos);
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

            IEnumerable<LogInfo> logInfos = [
                new(MyLogInfoKey.GiveawayId, giveaway.Id),
                new(MyLogInfoKey.EndTime, giveawayEndTimestamp)
            ];

            try
            {
                JoinGiveaway(giveaway);

                logger.Info(
                    MyOperation.JoinGiveaway,
                    OperationStatus.Success,
                    logInfos,
                    new LogInfo(MyLogInfoKey.Price, giveaway.Price),
                    new LogInfo(MyLogInfoKey.GameTitle, giveaway.GameTitle));
            }
            catch (UnjoinableGiveawayException ex)
            {
                logger.Warn(
                    MyOperation.JoinGiveaway,
                    OperationStatus.Failure,
                    logInfos,
                    new LogInfo(MyLogInfoKey.Reason, ex.Reason));
            }
        }
    }
}
