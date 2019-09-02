using System.Net.Http;
using System.Threading.Tasks;

using NuciLog.Core;

using SteamGiveawaysBot.Configuration;
using SteamGiveawaysBot.Entities;
using SteamGiveawaysBot.Logging;
using SteamGiveawaysBot.Client.Models;

namespace SteamGiveawaysBot.Client
{
    public sealed class RewardSender : IRewardSender
    {
        readonly HttpClient httpClient;

        readonly BotSettings botSettings;

        readonly IServerCommunicator<RecordRewardRequest> serverCommunicator;

        readonly ILogger logger;

        public RewardSender(
            BotSettings botSettings,
            IServerCommunicator<RecordRewardRequest> serverCommunicator,
            ILogger logger)
        {
            httpClient = new HttpClient();

            this.botSettings = botSettings;
            this.serverCommunicator = serverCommunicator;
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
            string endpoint = BuildRequestUrl(request);

            await serverCommunicator.SendRequest(HttpMethod.Post, endpoint, request);

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
            request.ActivationKey = reward.ActivationKey;

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
                $"&activationKey={request.ActivationKey}";
        }
    }
}
