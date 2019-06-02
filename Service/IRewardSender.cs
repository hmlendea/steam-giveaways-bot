using System.Threading.Tasks;

using SteamGiveawaysBot.Entities;

namespace SteamGiveawaysBot.Service
{
    public interface IRewardSender
    {
        Task RecordReward(Reward reward);
    }
}
