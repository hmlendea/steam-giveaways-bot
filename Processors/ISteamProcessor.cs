using System.Collections.Generic;

namespace SteamGiveawaysBot.Processors
{
    public interface ISteamProcessor
    {
        void LogIn();

        void VisitChat();
    }
}
