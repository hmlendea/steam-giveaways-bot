using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

using NuciLog.Core;

using SteamGiveawaysBot.Configuration;
using SteamGiveawaysBot.Entities;
using SteamGiveawaysBot.Logging;
using SteamGiveawaysBot.Security;
using SteamGiveawaysBot.Service.Models;

namespace SteamGiveawaysBot.Service
{
    public sealed class RewardSender : IRewardSender
    {
        readonly HttpClient httpClient;

        readonly BotSettings botSettings;

        readonly IHmacEncoder<RecordRewardRequest> requestHmacEncoder;

        readonly ICredentialsEncryptor credentialsEncryptor;

        readonly ILogger logger;

        public RewardSender(
            BotSettings botSettings,
            IHmacEncoder<RecordRewardRequest> requestEncoder,
            ICredentialsEncryptor credentialsEncryptor,
            ILogger logger)
        {
            httpClient = new HttpClient();

            this.botSettings = botSettings;
            this.requestHmacEncoder = requestEncoder;
            this.credentialsEncryptor = credentialsEncryptor;
            this.logger = logger;
        }

        public async Task RecordReward(Reward reward)
        {
            logger.Info(
                MyOperation.RewardRecording,
                OperationStatus.Started,
                new LogInfo(MyLogInfoKey.GiveawayId, reward.GiveawayId),
                new LogInfo(MyLogInfoKey.SgbUser, botSettings.Username));

            RecordRewardRequest request = BuildRequestObject(reward);
            string requestUrl = BuildRequestUrl(request);
            await SendRequest(requestUrl);

            logger.Debug(
                MyOperation.RewardRecording,
                OperationStatus.Success,
                new LogInfo(MyLogInfoKey.GiveawayId, reward.GiveawayId),
                new LogInfo(MyLogInfoKey.SgbUser, botSettings.Username));
        }

        public RecordRewardRequest BuildRequestObject(Reward reward)
        {
            RecordRewardRequest request = new RecordRewardRequest();
            request.Username = botSettings.Username;
            request.GiveawaysProvider = reward.GiveawaysProvider;
            request.GiveawayId = reward.GiveawayId;
            request.SteamUsername = reward.WinnerUsername;
            request.SteamAppId = reward.SteamAppId;
            request.GameTitle = reward.GameTitle;
            request.ActivationKey = reward.ActivationKey;

            string sharedSecretKey = credentialsEncryptor.Decrypt(botSettings.SharedSecretKey);

            request.HmacToken = requestHmacEncoder.GenerateToken(request, sharedSecretKey);

            return request;
        }

        string BuildRequestUrl(RecordRewardRequest request)
        {
            return
                $"{botSettings.ServerUrl}/Rewards" +
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
            HttpResponseMessage httpResponse = await httpClient.PostAsync(url, null);

            if (!httpResponse.IsSuccessStatusCode)
            {
                // TODO: Handle this case properly
                throw new Exception("Error response");
            }
        }
    }
}
