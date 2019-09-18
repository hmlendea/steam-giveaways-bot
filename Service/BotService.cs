using System;
using System.Collections.Generic;
using System.Linq;

using NuciExtensions;
using NuciLog.Core;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

using SteamGiveawaysBot.Client;
using SteamGiveawaysBot.Client.Models;
using SteamGiveawaysBot.Configuration;
using SteamGiveawaysBot.Entities;
using SteamGiveawaysBot.Exceptions;
using SteamGiveawaysBot.Logging;
using SteamGiveawaysBot.Processors;
using SteamGiveawaysBot.Processors.GiveawayProcessors;
using SteamGiveawaysBot.Utils;

namespace SteamGiveawaysBot.Service
{
    public class BotService : IBotService
    {
        readonly IAccountGatherer accountGatherer;
        readonly IRewardSender rewardSender;
        readonly IWebDriver webDriver;
        
        readonly BotSettings botSettings;
        readonly DebugSettings debugSettings;

        readonly ILogger logger;

        readonly AccountDetails account;

        public BotService(
            IAccountGatherer accountGatherer,
            IRewardSender rewardSender,
            IWebDriver webDriver,
            BotSettings botSettings,
            DebugSettings debugSettings,
            ILogger logger)
        {
            this.accountGatherer = accountGatherer;
            this.rewardSender = rewardSender;
            this.webDriver = webDriver;

            this.botSettings = botSettings;
            this.debugSettings = debugSettings;

            this.logger = logger;

            // TODO: Broken async
            AccountResponse accountResponse = accountGatherer.GatherAccount().Result;

            account = new AccountDetails
            {
                Username = accountResponse.Username,
                Password = accountResponse.Password
            };
        }

        public void Run()
        {
            try
            {
                DoStuff();
            }
            catch (AccountSuspendedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.Error(Operation.Unknown, OperationStatus.Failure, ex);
            }
        }

        void DoStuff()
        {
            ISteamProcessor steamProcessor = new SteamProcessor(webDriver, account, logger);
            steamProcessor.LogIn();
            steamProcessor.VisitChat();

            ISteamDbProcessor steamDbProcessor = new SteamDbProcessor(webDriver, logger);
            IEnumerable<uint> freeSubIds = steamDbProcessor.GetFreePackagesList();
            
            if (!EnumerableExt.IsNullOrEmpty(freeSubIds))
            {
                steamProcessor.ActivatePackages(freeSubIds);
            }

            IEnumerable<uint> ownedAppIds = steamProcessor.GatherOwnedGames();
            PlayGames(ownedAppIds);

            IGiveawayProcessor steamGiftsProcessor =
                new SteamGiftsProcessor(webDriver, account, rewardSender, logger);

            try
            {
                steamGiftsProcessor.LogIn();
            }
            catch (AccountNotRegisteredException)
            {
                steamProcessor.GatherProfileInformation();
                steamProcessor.UpdatePrivacySettings();
                
                steamGiftsProcessor.Register();
                steamGiftsProcessor.ApplyCustomSettings();
            }
            
            steamProcessor.Quit();

            steamGiftsProcessor.Run();
            steamGiftsProcessor.Stop();
            steamGiftsProcessor.Quit();
        }

        void PlayGames(IEnumerable<uint> appIds)
        {
            int gamesCount = appIds.Count();
            int gamesToPlayCount = (int)((float)gamesCount * 0.50f);

            logger.Info(
                MyOperation.PlayTimeFaking,
                OperationStatus.Started,
                new LogInfo(MyLogInfoKey.Username, account.Username),
                new LogInfo(MyLogInfoKey.OwnedGamesCount, gamesCount),
                new LogInfo(MyLogInfoKey.GamesCount, gamesToPlayCount));
                
            List<uint> appIdsToPlay = appIds.ToList().Shuffle().Take(gamesToPlayCount).ToList();

            using (SteamClientProcessor steamClientProcessor = new SteamClientProcessor(account))
            {
                steamClientProcessor.LogIn();
                steamClientProcessor.PlayGames(appIdsToPlay, TimeSpan.FromSeconds(5));
                steamClientProcessor.LogOut();
            }

            logger.Debug(MyOperation.PlayTimeFaking, OperationStatus.Success);
        }
    }
}
