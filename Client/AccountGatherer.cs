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
        IServerCommunicator<SteamAccountRequest, SteamAccountResponse> serverCommunicator,
        ILogger logger) : IAccountGatherer
    {
        public async Task<SteamAccountResponse> GatherAccount()
        {
            logger.Info(
                MyOperation.AccountDetailsRetrieval,
                OperationStatus.Started,
                new LogInfo(MyLogInfoKey.SgbUser, botSettings.Username));

            SteamAccountRequest request = BuildRequestObject();
            string endpoint = BuildRequestUrl(request);
            SteamAccountResponse response = await serverCommunicator.SendRequest(HttpMethod.Get, endpoint, request);

            // TODO: Do the mapping in a dedicated place

            logger.Debug(
                MyOperation.AccountDetailsRetrieval,
                OperationStatus.Success,
                "Account details retrieved successfully",
                new LogInfo(MyLogInfoKey.Username, response.Username));

            return response;
        }

        SteamAccountRequest BuildRequestObject() => new()
        {
            Username = botSettings.Username,
            GiveawaysProvider = "SteamGifts"
        };

        string BuildRequestUrl(SteamAccountRequest request) =>
            $"{botSettings.ServerUrl}/SteamAccount/{request.Username}" +
            $"?gaProvider=SteamGifts"; // TODO: Hardcoded
    }
}
