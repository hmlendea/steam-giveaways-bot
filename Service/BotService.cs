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
        readonly ISteamDbProcessor steamDbProcessor;
        
        readonly BotSettings botSettings;
        readonly DebugSettings debugSettings;
        readonly AccountDetails accountDetails;

        readonly ILogger logger;

        public BotService(
            IAccountGatherer accountGatherer,
            IRewardSender rewardSender,
            IGiveawayProcessor giveawayProcessor,
            ISteamProcessor steamProcessor,
            ISteamDbProcessor steamDbProcessor,
            BotSettings botSettings,
            DebugSettings debugSettings,
            AccountDetails accountDetails,
            ILogger logger)
        {
            this.accountGatherer = accountGatherer;
            this.rewardSender = rewardSender;
            
            this.giveawayProcessor = giveawayProcessor;
            this.steamProcessor = steamProcessor;
            this.steamDbProcessor = steamDbProcessor;

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

            IEnumerable<uint> freeSubIds = steamDbProcessor.GetFreePackagesList();
            
            if (!EnumerableExt.IsNullOrEmpty(freeSubIds))
            {
                steamProcessor.ActivatePackages(freeSubIds);
            }

            IEnumerable<uint> ownedAppIds = steamProcessor.GatherOwnedGames();
            PlayGames(ownedAppIds);

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

        void PlayGames(IEnumerable<uint> appIds)
        {
            int gamesCount = appIds.Count();
            int gamesToPlayCount = (int)((float)gamesCount * 0.50f);

            logger.Info(
                MyOperation.PlayTimeFaking,
                OperationStatus.Started,
                new LogInfo(MyLogInfoKey.Username, accountDetails.Username),
                new LogInfo(MyLogInfoKey.OwnedGamesCount, gamesCount),
                new LogInfo(MyLogInfoKey.GamesCount, gamesToPlayCount));
                
            List<uint> appIdsToPlay = appIds.ToList().Shuffle().Take(gamesToPlayCount).ToList();

            using (SteamClientProcessor steamClientProcessor = new SteamClientProcessor(accountDetails))
            {
                steamClientProcessor.LogIn();
                steamClientProcessor.PlayGames(appIdsToPlay, TimeSpan.FromSeconds(5));
                steamClientProcessor.LogOut();
            }

            logger.Debug(MyOperation.PlayTimeFaking, OperationStatus.Success);
        }
    }
}
