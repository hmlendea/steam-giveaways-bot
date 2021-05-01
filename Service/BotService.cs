using System;
using System.Collections.Generic;
using System.Linq;

using NuciExtensions;
using NuciLog.Core;

using SteamGiveawaysBot.Client;
using SteamGiveawaysBot.Client.Models;
using SteamGiveawaysBot.Configuration;
using SteamGiveawaysBot.Entities;
using SteamGiveawaysBot.Exceptions;
using SteamGiveawaysBot.Logging;
using SteamGiveawaysBot.Processors;
using SteamGiveawaysBot.Processors.GiveawayProcessors;

namespace SteamGiveawaysBot.Service
{
    public class BotService : IBotService
    {
        readonly IAccountGatherer accountGatherer;
        readonly IRewardSender rewardSender;
        
        readonly IGiveawayProcessor giveawayProcessor;
        readonly ISteamProcessor steamProcessor;
        
        readonly BotSettings botSettings;
        readonly DebugSettings debugSettings;
        readonly AccountDetails accountDetails;

        readonly ILogger logger;

        public BotService(
            IAccountGatherer accountGatherer,
            IRewardSender rewardSender,
            IGiveawayProcessor giveawayProcessor,
            ISteamProcessor steamProcessor,
            BotSettings botSettings,
            DebugSettings debugSettings,
            AccountDetails accountDetails,
            ILogger logger)
        {
            this.accountGatherer = accountGatherer;
            this.rewardSender = rewardSender;
            
            this.giveawayProcessor = giveawayProcessor;
            this.steamProcessor = steamProcessor;

            this.botSettings = botSettings;
            this.debugSettings = debugSettings;
            this.accountDetails = accountDetails;

            this.logger = logger;
        }

        public void Run()
        {
            try
            {
                // TODO: Broken async
                AccountResponse accountResponse = accountGatherer.GatherAccount().Result;

                this.accountDetails.Username = accountResponse.Username;
                this.accountDetails.Password = accountResponse.Password;

                DoStuff();
            }
            catch (AccountSuspendedException)
            {
                throw;
            }
            catch (AggregateException ex)
            {
                foreach (Exception innerException in ex.InnerExceptions)
                {
                    logger.Error(Operation.Unknown, OperationStatus.Failure, innerException);
                }
            }
            catch (Exception ex)
            {
                logger.Error(Operation.Unknown, OperationStatus.Failure, ex);
            }
        }

        void DoStuff()
        {
            steamProcessor.LogIn();
            steamProcessor.VisitChat();

            try
            {
                giveawayProcessor.LogIn();
            }
            catch (AccountNotRegisteredException)
            {
                steamProcessor.GatherProfileInformation();
                steamProcessor.UpdatePrivacySettings();
                
                giveawayProcessor.Register();
                giveawayProcessor.ApplyCustomSettings();
            }
            
            giveawayProcessor.Run();
            giveawayProcessor.Stop();
        }
    }
}
