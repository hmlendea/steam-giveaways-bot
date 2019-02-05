using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

using Newtonsoft.Json;

using SteamGiveawaysBot;
using SteamGiveawaysBot.Configuration;
using SteamGiveawaysBot.Entities;
using SteamGiveawaysBot.Logging;
using SteamGiveawaysBot.Service.Models;

namespace SteamGiveawaysBot.Service
{
    public sealed class AccountService : IAccountService
    {
        readonly HttpClient httpClient;

        readonly AccountDistributorSettings accountDistributorSettings;

        readonly SteamSettings steamSettings;

        readonly ILogger logger;

        public AccountService(
            AccountDistributorSettings accountDistributorSettings,
            SteamSettings steamSettings,
            ILogger logger)
        {
            httpClient = new HttpClient();

            this.accountDistributorSettings = accountDistributorSettings;
            this.steamSettings = steamSettings;
            this.logger = logger;
        }

        public async Task<AccountDetails> GatherAccount()
        {
            logger.Info($"Requesting account details for SGB user '{accountDistributorSettings.Username}'");

            string endpoint = accountDistributorSettings.Endpoint +
                $"/{accountDistributorSettings.Username}" +
                $"?gaProvider=SteamGifts" + // TODO: Hardcoded
                $"&password={accountDistributorSettings.Password}";
            
            AccountDetails account = await RequestAccountDetails(endpoint);

            if (account == null)
            {
                logger.Info($"Unable to receive account details. Using default ones instead");

                account = new AccountDetails
                {
                    Username = steamSettings.Username,
                    Password = steamSettings.Password
                };
            }

            return account;
        }

        async Task<AccountDetails> RequestAccountDetails(string endpoint)
        {
            try
            {
                HttpResponseMessage httpResponse = await httpClient.GetAsync(endpoint);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    return null;
                }
                
                SteamAccountResponse response = await DeserialiseResponse(httpResponse);

                // TODO: Do the mapping in a dedicated place
                AccountDetails account = new AccountDetails
                {
                    Username = response.Username,
                    Password = response.Password
                };

                return account;
            }
            catch
            {
                return null;
            }
        }

        async Task<SteamAccountResponse> DeserialiseResponse(HttpResponseMessage httpResponse)
        {
            string responseString = await httpResponse.Content.ReadAsStringAsync();
            SteamAccountResponse response = JsonConvert.DeserializeObject<SteamAccountResponse>(responseString);

            return response;
        }
    }
}
