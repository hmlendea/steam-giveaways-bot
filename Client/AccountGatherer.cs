using System;
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

            try
            {
                var response = await serverCommunicator.SendRequest(HttpMethod.Get, $"{botSettings.ServerUrl}/SteamAccount/{request.Username}", request);

                logger.Debug(
                    MyOperation.AccountDetailsRetrieval,
                    OperationStatus.Success,
                    new LogInfo(MyLogInfoKey.Username, response.Username));

                return response;
            }
            catch (Exception ex)
            {
                logger.Error(
                    MyOperation.AccountDetailsRetrieval,
                    OperationStatus.Failure,
                    ex,
                    new LogInfo(MyLogInfoKey.Username, request.Username));

                throw;
            }
        }

        SteamAccountRequest BuildRequestObject() => new()
        {
            Username = botSettings.Username,
            GiveawaysProvider = "SteamGifts"
        };
    }
}
