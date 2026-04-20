using System;
using System.Collections.Generic;
using System.Linq;
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
            IEnumerable<LogInfo> logInfos = [
                new(MyLogInfoKey.SgbUser, botSettings.Username)
            ];

            logger.Info(
                MyOperation.AccountDetailsRetrieval,
                OperationStatus.Started,
                logInfos);

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

                logInfos = logInfos.Append(new(MyLogInfoKey.Username, accountResponse.Username));

                logger.Info(
                    MyOperation.AccountDetailsRetrieval,
                    OperationStatus.Success,
                    logInfos);

                return accountResponse;
            }
            catch (Exception exception)
            {
                logger.Error(
                    MyOperation.AccountDetailsRetrieval,
                    OperationStatus.Failure,
                    exception,
                    logInfos);

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
