using System.Net.Http;
using System.Threading.Tasks;

using NuciLog.Core;

using SteamGiveawaysBot.Client.Models;
using SteamGiveawaysBot.Configuration;
using SteamGiveawaysBot.Logging;

namespace SteamGiveawaysBot.Client
{
    public sealed class AccountGatherer(
        BotSettings botSettings,
        IServerCommunicator<AccountRequest, AccountResponse> serverCommunicator,
        ILogger logger) : IAccountGatherer
    {
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

        AccountRequest BuildRequestObject() => new()
        {
            Username = botSettings.Username,
            GiveawaysProvider = "SteamGifts"
        };

        string BuildRequestUrl(AccountRequest request) =>
            $"{botSettings.ServerUrl}/SteamAccount/{request.Username}" +
            $"?gaProvider=SteamGifts"; // TODO: Hardcoded
    }
}
