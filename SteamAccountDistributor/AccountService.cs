using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Threading.Tasks;

using Newtonsoft.Json;

using SteamGiveawaysBot;
using SteamGiveawaysBot.Configuration;
using SteamGiveawaysBot.Entities;
using SteamGiveawaysBot.Logging;
using SteamGiveawaysBot.SteamAccountDistributor.Models;
using SteamGiveawaysBot.SteamAccountDistributor.Security;

namespace SteamGiveawaysBot.SteamAccountDistributor
{
    public sealed class AccountService : IAccountService
    {
        readonly HttpClient httpClient;

        readonly AccountDistributorSettings accountDistributorSettings;

        readonly IHmacEncoder<SteamAccountRequest> requestHmacEncoder;

        readonly IHmacEncoder<SteamAccountResponse> responseHmacEncoder;

        readonly SteamSettings steamSettings;

        readonly ILogger logger;

        public AccountService(
            AccountDistributorSettings accountDistributorSettings,
            IHmacEncoder<SteamAccountRequest> requestEncoder,
            IHmacEncoder<SteamAccountResponse> responseEncoder,
            SteamSettings steamSettings,
            ILogger logger)
        {
            httpClient = new HttpClient();

            this.accountDistributorSettings = accountDistributorSettings;
            this.requestHmacEncoder = requestEncoder;
            this.responseHmacEncoder = responseEncoder;
            this.steamSettings = steamSettings;
            this.logger = logger;
        }

        public async Task<AccountDetails> GatherAccount()
        {
            logger.Info($"Requesting account details for SGB user '{accountDistributorSettings.Username}'");

            SteamAccountRequest request = BuildRequestObject();

            string endpoint = accountDistributorSettings.Endpoint +
                $"/{request.Username}" +
                $"?gaProvider=SteamGifts" + // TODO: Hardcoded
                $"&hmac={request.HmacToken}";
            
            HttpResponseMessage httpResponse = await httpClient.GetAsync(endpoint);

            if (!httpResponse.IsSuccessStatusCode)
            {
                // TODO: Handle this case properly
                throw new Exception("Error response");
            }
            
            SteamAccountResponse response = await DeserialiseResponse(httpResponse);
            ValdiateRespone(response, accountDistributorSettings.SharedSecretKey);

            // TODO: Do the mapping in a dedicated place
            AccountDetails account = new AccountDetails
            {
                Username = response.Username,
                Password = response.Password
            };

            return account;
        }

        SteamAccountRequest BuildRequestObject()
        {
            SteamAccountRequest request = new SteamAccountRequest();
            request.Username = accountDistributorSettings.Username;
            request.GiveawaysProvider = "SteamGifts";

            request.HmacToken = requestHmacEncoder.GenerateToken(
                request,
                accountDistributorSettings.SharedSecretKey);

            return request;
        }

        async Task<SteamAccountResponse> DeserialiseResponse(HttpResponseMessage httpResponse)
        {
            string responseString = await httpResponse.Content.ReadAsStringAsync();
            SteamAccountResponse response = JsonConvert.DeserializeObject<SteamAccountResponse>(responseString);

            return response;
        }

        void ValdiateRespone(SteamAccountResponse response, string sharedSecretKey)
        {
            if (!responseHmacEncoder.IsTokenValid(response.HmacToken, response, sharedSecretKey))
            {
                throw new AuthenticationException("The response HMAC token is not valid");
            }
        }
    }
}
