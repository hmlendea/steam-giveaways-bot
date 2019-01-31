using System;
using System.IO;

namespace SteamGiveawaysBot.Utils
{
    public static class Log
    {
        public static string LogFilePath { get; set; }

        public static void Info(string message)
        {
            WriteLine(message, "INFO");
        }

        public static void Warn(string message)
        {
            WriteLine(message, "WARN");
        }

        public static void Error(string message)
        {
            WriteLine(message, "ERROR");
        }
        
        static void WriteLine(string message, string level)
        {
            string timeStamp = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff");
            string logLine = $"[{timeStamp}] {level}: {message}";

            Console.WriteLine(logLine);
            
            if (!string.IsNullOrWhiteSpace(LogFilePath))
            {
                File.AppendAllText(LogFilePath ,logLine + Environment.NewLine);
            }
        }
    }
}
