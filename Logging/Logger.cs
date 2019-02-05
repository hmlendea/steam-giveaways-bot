using System;
using System.IO;

using SteamGiveawaysBot.Configuration;

namespace SteamGiveawaysBot.Logging
{
    public class Logger : ILogger
    {
        readonly DebugSettings debugSettings;

        public Logger(DebugSettings debugSettings)
        {
            this.debugSettings = debugSettings;
        }

        public void Info(string message)
        {
            WriteLine(message, "INFO ");
        }

        public void Warn(string message)
        {
            WriteLine(message, "WARN ");
        }

        public void Error(string message)
        {
            WriteLine(message, "ERROR");
        }

        public void Fatal(string message)
        {
            WriteLine(message, "FATAL");
        }
        
        void WriteLine(string message, string level)
        {
            string timeStamp = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff");
            string logLine = $"[{timeStamp}] {level}: {message}";

            Console.WriteLine(logLine);
            
            if (debugSettings.IsLoggingEnabled)
            {
                File.AppendAllText(debugSettings.LogFilePath ,logLine + Environment.NewLine);
            }
        }
    }
}
