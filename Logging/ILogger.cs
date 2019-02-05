using System;
using System.IO;

namespace SteamGiveawaysBot.Logging
{
    public interface ILogger
    {
        void Info(string message);

        void Warn(string message);

        void Error(string message);

        void Fatal(string message);
    }
}
