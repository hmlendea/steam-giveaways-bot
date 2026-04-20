using System.Collections.Generic;
using System.Security.Authentication;

using NuciLog.Core;
using NuciWeb.Steam;
using NuciWeb.Steam.Models;

using SteamGiveawaysBot.Client;
using SteamGiveawaysBot.Client.Models;
using SteamGiveawaysBot.Logging;
using SteamGiveawaysBot.Processors.GiveawayProcessors;

namespace SteamGiveawaysBot.Service
{
    public class BotService(
        IAccountGatherer accountGatherer,
        IGiveawayProcessor giveawayProcessor,
        ISteamProcessor steamProcessor,
        SteamAccount steamAccount,
        ILogger logger) : IBotService
    {
        public void Run()
        {
            GatherTheCredentials();
            LogIntoSteam();
            JoinGiveaways();
        }

        void GatherTheCredentials()
        {
            // TODO: Broken async
            GetSteamAccountResponse accountResponse = accountGatherer.GatherAccount().Result;

            steamAccount.Username = accountResponse.Username;
            steamAccount.Password = accountResponse.Password;
        }

        void LogIntoSteam()
        {
            IEnumerable<LogInfo> logInfos =
            [
                new(MyLogInfoKey.Username, steamAccount.Username)
            ];

            logger.Info(
                MyOperation.SteamLogIn,
                OperationStatus.Started,
                logInfos);

            try
            {
                steamProcessor.LogIn(steamAccount);
                steamProcessor.VisitChat();
            }
            catch (AuthenticationException exception)
            {
                logger.Error(
                    MyOperation.SteamLogIn,
                    OperationStatus.Failure,
                    exception,
                    logInfos);
            }

            logger.Info(
                MyOperation.SteamLogIn,
                OperationStatus.Success,
                logInfos);
        }

        void JoinGiveaways()
        {
            giveawayProcessor.LogIn();
            giveawayProcessor.Run();
            giveawayProcessor.Stop();
        }
    }
}
