using System.Threading.Tasks;

using SteamGiveawaysBot.Client.Models;

namespace SteamGiveawaysBot.Client
{
    public interface IAccountGatherer
    {
        Task<GetSteamAccountResponse> GatherAccount();
    }
}
