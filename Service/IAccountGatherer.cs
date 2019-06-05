using System.Threading.Tasks;

using SteamGiveawaysBot.Service.Models;

namespace SteamGiveawaysBot.Service
{
    public interface IAccountGatherer
    {
        Task<SteamAccount> GatherAccount();
    }
}
