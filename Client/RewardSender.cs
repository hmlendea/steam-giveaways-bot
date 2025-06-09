using System.Net.Http;
using System.Threading.Tasks;

using NuciLog.Core;

using SteamGiveawaysBot.Configuration;
using SteamGiveawaysBot.Entities;
using SteamGiveawaysBot.Logging;
using SteamGiveawaysBot.Client.Models;

namespace SteamGiveawaysBot.Client
{
    public sealed class RewardSender(
        BotSettings botSettings,
        IServerCommunicator<RecordRewardRequest> serverCommunicator,
        ILogger logger) : IRewardSender
    {
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

        public RecordRewardRequest BuildRequestObject(Reward reward) => new()
        {
            Username = botSettings.Username,
            GiveawaysProvider = reward.GiveawaysProvider,
            GiveawayId = reward.GiveawayId,
            SteamUsername = reward.WinnerUsername,
            SteamAppId = reward.SteamAppId,
            ActivationKey = reward.ActivationKey
        };

        string BuildRequestUrl(RecordRewardRequest request) =>
            $"{botSettings.ServerUrl}/Rewards" +
            $"?username={request.Username}" +
            $"&gaProvider={request.GiveawaysProvider}" +
            $"&gaId={request.GiveawayId}" +
            $"&steamUsername={request.SteamUsername}" +
            $"&steamAppId={request.SteamAppId}" +
            $"&activationKey={request.ActivationKey}";
    }
}
