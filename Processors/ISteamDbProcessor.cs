using System.Collections.Generic;

namespace SteamGiveawaysBot.Processors
{
    public interface ISteamDbProcessor
    {
        IEnumerable<uint> GetFreePackagesList();
    }
}
