using System;

namespace SteamGiveawaysBot.Service
{
    public interface IBotService
    {
        bool IsRunning { get; }

        event EventHandler Started;

        event EventHandler Stopped;

        void Run();
    }
}
