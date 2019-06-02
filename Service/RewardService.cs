using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

using NuciLog.Core;

using SteamGiveawaysBot.Communication.SteamAccountDistributor.Security;
using SteamGiveawaysBot.Configuration;
using SteamGiveawaysBot.Entities;
using SteamGiveawaysBot.Logging;
using SteamGiveawaysBot.Security;
using SteamGiveawaysBot.Service.Models;

namespace SteamGiveawaysBot.Service
{
    public sealed class RewardService : IRewardService
    {
        readonly HttpClient httpClient;

        readonly BotSettings botSettings;

        readonly SteamSettings steamSettings;

        readonly IHmacEncoder<RecordRewardRequest> requestHmacEncoder;

        readonly ICredentialsEncryptor credentialsEncryptor;

        readonly ILogger logger;

        public RewardService(
            BotSettings botSettings,
            SteamSettings steamSettings,
            IHmacEncoder<RecordRewardRequest> requestEncoder,
            ICredentialsEncryptor credentialsEncryptor,
            ILogger logger)
        {
            httpClient = new HttpClient();

            this.botSettings = botSettings;
            this.steamSettings = steamSettings;
            this.requestHmacEncoder = requestEncoder;
            this.credentialsEncryptor = credentialsEncryptor;
            this.logger = logger;
        }

        public async Task RecordReward(Giveaway giveaway, string activationKey)
        {
            logger.Info(
                MyOperation.RewardRecording,
                OperationStatus.Started,
                new LogInfo(MyLogInfoKey.GiveawayId, giveaway.Id),
                new LogInfo(MyLogInfoKey.SgbUser, botSettings.Username));

            RecordRewardRequest request = BuildRequestObject(giveaway, activationKey);
            string requestUrl = BuildRequestUrl(request);
            await SendRequest(requestUrl);

            logger.Debug(
                MyOperation.RewardRecording,
                OperationStatus.Success,
                new LogInfo(MyLogInfoKey.GiveawayId, giveaway.Id),
                new LogInfo(MyLogInfoKey.SgbUser, botSettings.Username));
        }

        public RecordRewardRequest BuildRequestObject(Giveaway giveaway, string activationKey)
        {
            RecordRewardRequest request = new RecordRewardRequest();
            request.Username = botSettings.Username;
            request.GiveawaysProvider = giveaway.ProviderName;
            request.GiveawayId = giveaway.Id;
            request.SteamUsername = steamSettings.Username;
            request.SteamAppId = giveaway.SteamAppId;
            request.GameTitle = giveaway.GameTitle;
            request.ActivationKey = activationKey;

            string sharedSecretKey = credentialsEncryptor.Decrypt(botSettings.SharedSecretKey);

            request.HmacToken = requestHmacEncoder.GenerateToken(request, sharedSecretKey);

            return request;
        }

        string BuildRequestUrl(RecordRewardRequest request)
        {
            return
                $"{botSettings.Endpoint}/Rewards" +
                $"?username={request.Username}" +
                $"&gaProvider={request.GiveawaysProvider}" +
                $"&gaId={request.GiveawayId}" +
                $"&steamUsername={request.SteamUsername}" +
                $"&steamAppId={request.SteamAppId}" +
                $"&gameTitle={HttpUtility.UrlEncode(request.GameTitle)}" +
                $"&activationKey={request.ActivationKey}" +
                $"&hmac={request.HmacToken}";
        }

        async Task SendRequest(string url)
        {
            Console.WriteLine(url);
            HttpResponseMessage httpResponse = await httpClient.GetAsync(url);
Console.WriteLine("SASA");
            if (!httpResponse.IsSuccessStatusCode)
            {
                // TODO: Handle this case properly
                throw new Exception("Error response");
            }
        }
    }
}
