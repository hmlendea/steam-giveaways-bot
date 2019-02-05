using System.Threading.Tasks;

using SteamGiveawaysBot.Entities;

namespace SteamGiveawaysBot.Service
{
    public interface IAccountService
    {
        Task<AccountDetails> GatherAccount();
    }
}
