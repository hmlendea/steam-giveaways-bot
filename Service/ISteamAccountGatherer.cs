using System.Threading.Tasks;

using SteamGiveawaysBot.Service.Models;

namespace SteamGiveawaysBot.Service
{
    public interface ISteamAccountGatherer
    {
        Task<SteamAccount> GatherAccount();
    }
}
