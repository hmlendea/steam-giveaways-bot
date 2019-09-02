using System.Threading.Tasks;

using SteamGiveawaysBot.Entities;

namespace SteamGiveawaysBot.Client
{
    public interface IRewardSender
    {
        Task RecordReward(Reward reward);
    }
}
