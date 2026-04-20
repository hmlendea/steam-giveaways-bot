using System.Net.Http;
using System.Threading.Tasks;

using NuciLog.Core;

using SteamGiveawaysBot.Configuration;
using SteamGiveawaysBot.Entities;
using SteamGiveawaysBot.Logging;
using SteamGiveawaysBot.Client.Models;
using System;
using NuciAPI.Client;
using NuciAPI.Responses;
using System.Collections.Generic;

namespace SteamGiveawaysBot.Client
{
    public sealed class RewardSender(
        BotSettings botSettings,
        INuciApiClient server,
        ILogger logger) : IRewardSender
    {
        public async Task RecordReward(Reward reward)
        {
            IEnumerable<LogInfo> logInfos = [
                new(MyLogInfoKey.GiveawayId, reward.GiveawayId),
                new(MyLogInfoKey.SgbUser, botSettings.Username)
            ];

            logger.Info(
                MyOperation.RewardRecording,
                OperationStatus.Started,
                logInfos);

            RecordRewardRequest request = BuildRequestObject(reward);
            try
            {
                await server.SendRequestAsync<RecordRewardRequest, NuciApiSuccessResponse>(
                    HttpMethod.Post,
                    request,
                    new()
                    {
                        ClientId = $"{nameof(SteamGiveawaysBot)}.{Environment.MachineName}.{botSettings.Username}",
                        BearerToken = botSettings.ServerApiKey,
                        HmacSharedSecretKey = botSettings.SharedSecretKey,
                    },
                    "Rewards");

                logger.Info(
                    MyOperation.RewardRecording,
                    OperationStatus.Success,
                    logInfos);
            }
            catch (Exception ex)
            {
                logger.Error(
                    MyOperation.RewardRecording,
                    OperationStatus.Failure,
                    ex,
                    logInfos);

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
