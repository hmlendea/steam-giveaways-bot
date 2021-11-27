using System.Security.Authentication;

using NuciLog.Core;
using NuciWeb.Steam;
using NuciWeb.Steam.Models;

using SteamGiveawaysBot.Client;
using SteamGiveawaysBot.Client.Models;
using SteamGiveawaysBot.Configuration;
using SteamGiveawaysBot.Logging;
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
        readonly SteamAccount steamAccount;

        readonly ILogger logger;

        public BotService(
            IAccountGatherer accountGatherer,
            IRewardSender rewardSender,
            IGiveawayProcessor giveawayProcessor,
            ISteamProcessor steamProcessor,
            BotSettings botSettings,
            DebugSettings debugSettings,
            SteamAccount steamAccount,
            ILogger logger)
        {
            this.accountGatherer = accountGatherer;
            this.rewardSender = rewardSender;
            
            this.giveawayProcessor = giveawayProcessor;
            this.steamProcessor = steamProcessor;

            this.botSettings = botSettings;
            this.debugSettings = debugSettings;
            this.steamAccount = steamAccount;

            this.logger = logger;
        }

        public void Run()
        {
            GatherTheCredentials();
            LogIntoSteam();
            JoinGiveaways();
        }

        void GatherTheCredentials()
        {
            // TODO: Broken async
            AccountResponse accountResponse = accountGatherer.GatherAccount().Result;

            steamAccount.Username = accountResponse.Username;
            steamAccount.Password = accountResponse.Password;
        }

        void LogIntoSteam()
        {
            logger.Info(
                MyOperation.SteamLogIn,
                OperationStatus.Started,
                new LogInfo(MyLogInfoKey.Username, steamAccount.Username));

            try
            {
                steamProcessor.LogIn(steamAccount);
                steamProcessor.VisitChat();
            }
            catch (AuthenticationException ex)
            {
                logger.Error(
                    MyOperation.SteamLogIn,
                    OperationStatus.Failure,
                    ex,
                    new LogInfo(MyLogInfoKey.Username, steamAccount.Username));
            }

            logger.Debug(
                MyOperation.SteamLogIn,
                OperationStatus.Success,
                new LogInfo(MyLogInfoKey.Username, steamAccount.Username));
        }

        void JoinGiveaways()
        {
            giveawayProcessor.LogIn();
            giveawayProcessor.Run();
            giveawayProcessor.Stop();
        }
    }
}
