using System;
using System.Net.Http;
using System.Threading.Tasks;
using NuciAPI.Client;
using NuciAPI.Responses;
using NuciLog.Core;
using SteamGiveawaysBot.Client.Models;
using SteamGiveawaysBot.Configuration;
using SteamGiveawaysBot.Logging;

namespace SteamGiveawaysBot.Client
{
    public sealed class AccountGatherer(
        BotSettings botSettings,
        INuciApiClient server,
        ILogger logger) : IAccountGatherer
    {
        public async Task<GetSteamAccountResponse> GatherAccount()
        {
            logger.Info(
                MyOperation.AccountDetailsRetrieval,
                OperationStatus.Started,
                new LogInfo(MyLogInfoKey.SgbUser, botSettings.Username));

            GetSteamAccountRequest request = BuildRequestObject();

            try
            {
                NuciApiResponse response = await server.SendRequestAsync<GetSteamAccountRequest, GetSteamAccountResponse>(
                    HttpMethod.Get,
                    request,
                    new()
                    {
                        ClientId = $"{nameof(SteamGiveawaysBot)}.{Environment.MachineName}.{botSettings.Username}",
                        BearerToken = botSettings.ServerApiKey,
                        HmacSharedSecretKey = botSettings.SharedSecretKey,
                    },
                    $"SteamAccount/{request.Username}");

                if (!response.IsSuccessful)
                {
                    throw new HttpRequestException(response.Message);
                }

                GetSteamAccountResponse accountResponse = response as GetSteamAccountResponse;

                logger.Debug(
                    MyOperation.AccountDetailsRetrieval,
                    OperationStatus.Success,
                    new LogInfo(MyLogInfoKey.Username, accountResponse.Username));

                return accountResponse;
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

        GetSteamAccountRequest BuildRequestObject() => new()
        {
            Username = botSettings.Username,
            GiveawaysProvider = "SteamGifts"
        };
    }
}
