using System;

namespace SteamGiveawaysBot.Service
{
    public interface IBotService
    {
        bool IsRunning { get; }

        void Run();
    }
}
