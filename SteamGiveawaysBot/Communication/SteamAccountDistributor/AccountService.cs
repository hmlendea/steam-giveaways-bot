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
using SteamGiveawaysBot.Communication.SteamAccountDistributor.Models;
using SteamGiveawaysBot.Communication.SteamAccountDistributor.Security;
using SteamGiveawaysBot.Configuration;
using SteamGiveawaysBot.Logging;
using SteamGiveawaysBot.Security;

namespace SteamGiveawaysBot.Communication.SteamAccountDistributor
{
    public sealed class AccountService : IAccountService
    {
        readonly HttpClient httpClient;

        readonly AccountDistributorSettings accountDistributorSettings;

        readonly IHmacEncoder<SteamAccountRequest> requestHmacEncoder;

        readonly IHmacEncoder<SteamAccountResponse> responseHmacEncoder;

        readonly ICredentialsEncryptor credentialsEncryptor;

        readonly ILogger logger;

        public AccountService(
            AccountDistributorSettings accountDistributorSettings,
            IHmacEncoder<SteamAccountRequest> requestEncoder,
            IHmacEncoder<SteamAccountResponse> responseEncoder,
            ICredentialsEncryptor credentialsEncryptor,
            ILogger logger)
        {
            httpClient = new HttpClient();

            this.accountDistributorSettings = accountDistributorSettings;
            this.requestHmacEncoder = requestEncoder;
            this.responseHmacEncoder = responseEncoder;
            this.credentialsEncryptor = credentialsEncryptor;
            this.logger = logger;
        }

        public async Task<SteamAccount> GatherAccount()
        {
            logger.Info(
                MyOperation.AccountDetailsRetrieval,
                OperationStatus.Started,
                new LogInfo(MyLogInfoKey.SgbUser, accountDistributorSettings.Username));

            string sharedSecretKey = credentialsEncryptor.Decrypt(accountDistributorSettings.SharedSecretKey);

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
            ValdiateRespone(response, sharedSecretKey);

            string passwordCypher = credentialsEncryptor.Encrypt(response.Password);

            // TODO: Do the mapping in a dedicated place
            SteamAccount account = new SteamAccount
            {
                Username = response.Username,
                Password = passwordCypher
            };

            logger.Debug(MyOperation.AccountDetailsRetrieval, OperationStatus.Success);

            return account;
        }

        SteamAccountRequest BuildRequestObject()
        {
            SteamAccountRequest request = new SteamAccountRequest();
            request.Username = accountDistributorSettings.Username;
            request.GiveawaysProvider = "SteamGifts";

            string sharedSecretKey = credentialsEncryptor.Decrypt(accountDistributorSettings.SharedSecretKey);

            request.HmacToken = requestHmacEncoder.GenerateToken(request, sharedSecretKey);

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
