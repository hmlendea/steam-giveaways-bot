using System.Threading.Tasks;

using SteamGiveawaysBot.Entities;

namespace SteamGiveawaysBot.SteamAccountDistributor
{
    public interface IAccountService
    {
        Task<AccountDetails> GatherAccount();
    }
}
