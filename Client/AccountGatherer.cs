using System.Net.Http;
using System.Threading.Tasks;

using NuciLog.Core;

using SteamGiveawaysBot.Client.Models;
using SteamGiveawaysBot.Configuration;
using SteamGiveawaysBot.Logging;

namespace SteamGiveawaysBot.Client
{
    public sealed class AccountGatherer : IAccountGatherer
    {
        readonly BotSettings botSettings;

        readonly IServerCommunicator<AccountRequest, AccountResponse> serverCommunicator;

        readonly ILogger logger;

        public AccountGatherer(
            BotSettings botSettings,
            IServerCommunicator<AccountRequest, AccountResponse> serverCommunicator,
            ILogger logger)
        {
            this.botSettings = botSettings;
            this.serverCommunicator = serverCommunicator;
            this.logger = logger;
        }

        public async Task<AccountResponse> GatherAccount()
        {
            logger.Info(
                MyOperation.AccountDetailsRetrieval,
                OperationStatus.Started,
                new LogInfo(MyLogInfoKey.SgbUser, botSettings.Username));

            AccountRequest request = BuildRequestObject();
            string endpoint = BuildRequestUrl(request);
            AccountResponse response = await serverCommunicator.SendRequest(HttpMethod.Get, endpoint, request);

            // TODO: Do the mapping in a dedicated place

            logger.Debug(
                MyOperation.AccountDetailsRetrieval,
                OperationStatus.Success,
                "Account details retrieved successfully",
                new LogInfo(MyLogInfoKey.Username, response.Username));

            return response;
        }

        AccountRequest BuildRequestObject()
        {
            AccountRequest request = new AccountRequest();
            request.Username = botSettings.Username;
            request.GiveawaysProvider = "SteamGifts";

            return request;
        }

        string BuildRequestUrl(AccountRequest request)
        {
            return
                $"{botSettings.ServerUrl}/SteamAccount/{request.Username}" +
                $"?gaProvider=SteamGifts"; // TODO: Hardcoded
        }
    }
}
