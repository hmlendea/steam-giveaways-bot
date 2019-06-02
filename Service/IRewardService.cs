using System.Threading.Tasks;

using SteamGiveawaysBot.Entities;

namespace SteamGiveawaysBot.Service
{
    public interface IRewardService
    {
        Task RecordReward(Giveaway giveaway, string activationKey);
    }
}
