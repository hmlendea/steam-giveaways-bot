using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Threading.Tasks;

using Newtonsoft.Json;
using NuciLog.Core;

using SteamGiveawaysBot;
using SteamGiveawaysBot.Entities;
using SteamGiveawaysBot.Service.Models;
using SteamGiveawaysBot.Security;
using SteamGiveawaysBot.Configuration;
using SteamGiveawaysBot.Logging;

namespace SteamGiveawaysBot.Service
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

        public async Task<SteamAccount> GatherAccount()
        {
            logger.Info(
                MyOperation.AccountDetailsRetrieval,
                OperationStatus.Started,
                new LogInfo(MyLogInfoKey.SgbUser, botSettings.Username));

            AccountRequest request = BuildRequestObject();
            string endpoint = BuildRequestUrl(request);
            AccountResponse response = await serverCommunicator.SendRequest(HttpMethod.Get, endpoint, request);

            // TODO: Do the mapping in a dedicated place
            SteamAccount account = new SteamAccount
            {
                Username = response.Username,
                Password = response.Password
            };

            logger.Debug(
                MyOperation.AccountDetailsRetrieval,
                OperationStatus.Success,
                "Account details retrieved successfully",
                new LogInfo(MyLogInfoKey.Username, account.Username));

            return account;
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
