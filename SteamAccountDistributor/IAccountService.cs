using System.Threading.Tasks;

using SteamGiveawaysBot.SteamAccountDistributor.Models;

namespace SteamGiveawaysBot.SteamAccountDistributor
{
    public interface IAccountService
    {
        Task<SteamAccount> GatherAccount();
    }
}
