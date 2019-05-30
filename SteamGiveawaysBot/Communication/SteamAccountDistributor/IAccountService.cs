using System.Threading.Tasks;

using SteamGiveawaysBot.Communication.SteamAccountDistributor.Models;

namespace SteamGiveawaysBot.Communication.SteamAccountDistributor
{
    public interface IAccountService
    {
        Task<SteamAccount> GatherAccount();
    }
}
