using System.Net.Http;
using System.Threading.Tasks;

using NuciLog.Core;

using SteamGiveawaysBot.Configuration;
using SteamGiveawaysBot.Entities;
using SteamGiveawaysBot.Logging;
using SteamGiveawaysBot.Client.Models;
using System;

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
            try
            {
                await serverCommunicator.SendRequest(HttpMethod.Post, $"{botSettings.ServerUrl}/Rewards", request);

                logger.Debug(
                    MyOperation.RewardRecording,
                    OperationStatus.Success,
                    new LogInfo(MyLogInfoKey.GiveawayId, reward.GiveawayId),
                    new LogInfo(MyLogInfoKey.SgbUser, botSettings.Username));
            }
            catch (Exception ex)
            {
                logger.Error(
                    MyOperation.RewardRecording,
                    OperationStatus.Failure,
                    ex,
                    new LogInfo(MyLogInfoKey.GiveawayId, reward.GiveawayId),
                    new LogInfo(MyLogInfoKey.SgbUser, botSettings.Username));

                throw;
            }
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
    }
}
