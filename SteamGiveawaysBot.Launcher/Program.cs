using System;
using System.Diagnostics;
using System.IO;
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
            CheckForUpdates();
            LaunchApplication();
        }

        static void LaunchApplication()
        {
            string appExePath = GetApplicationExecutablePath();

            if (string.IsNullOrWhiteSpace(appExePath))
            {
                return;
            }

            Process process = new Process();
            process.StartInfo.CreateNoWindow = true;

            if (appExePath.EndsWith(".dll"))
            {
                process.StartInfo.FileName = "dotnet";
                process.StartInfo.Arguments = $"\"{appExePath}\"";
            }
            else
            {
                process.StartInfo.FileName = appExePath;
            }

            process.Start();
        }

        static string GetApplicationExecutablePath()
        {
            foreach (string fileNameCandidate in fileNameCandidates)
            {
                string filePath = Path.Combine(ApplicationInfo.SteamGiveawaysBotDirectory, fileNameCandidate);

                if (File.Exists(filePath))
                {
                    return filePath;
                }
            }

            return null;
        }

        static void CheckForUpdates()
        {
            try
            {
                string latestVersion = ApplicationUpdater.GetLatestVersionString();

                if (!Directory.Exists(ApplicationInfo.SteamGiveawaysBotDirectory) ||
                    string.Compare(latestVersion, ApplicationInfo.SteamGiveawaysBotVersion) > 0)
                {
                    ApplicationUpdater.UpdateApplication(latestVersion);
                }
            }
            catch
            {
                Console.WriteLine("Error checking for updates");
            }
        }
    }
}
