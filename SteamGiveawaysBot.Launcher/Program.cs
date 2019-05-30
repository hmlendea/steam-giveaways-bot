using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime;

namespace SteamGiveawaysBot.Launcher
{
    class Program
    {
        static string[] fileNameCandidates = {
            $"{nameof(SteamGiveawaysBot)}",
            $"{nameof(SteamGiveawaysBot)}.exe",
            $"{nameof(SteamGiveawaysBot)}.dll" };

        static void Main(string[] args)
        {
            string appExePath = GetApplicationExecutablePath();
            Process process = new Process();
            process.StartInfo.CreateNoWindow = true;

            if (appExePath.EndsWith(".dll"))
            {
                process.StartInfo.FileName = "dotnet";
                process.StartInfo.Arguments = appExePath;
            }
            else
            {
                process.StartInfo.FileName = appExePath;
            }

            process.Start();
        }

        static string GetApplicationExecutablePath()
        {
            string appDirPath = GetApplicationDirectoryPath();
            string appExePath = string.Empty;

            foreach (string fileNameCandidate in fileNameCandidates)
            {
                string filePath = Path.Combine(appDirPath, fileNameCandidate);

                if (File.Exists(filePath))
                {
                    return filePath;
                }
            }

            return null;
        }

        static string GetApplicationDirectoryPath()
        {
            Assembly thisAssembly = Assembly.GetExecutingAssembly();
            return Path.GetDirectoryName(thisAssembly.Location);
        }
    }
}
