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
