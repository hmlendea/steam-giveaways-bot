using System;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace SteamGiveawaysBot.Launcher
{
    public static class ApplicationUpdater
    {
        const string LatestVersionFileNameFormat = "sgb_{0}_{1}.zip";
        const string LatestVersionArchiveUrlFormat = "http://hori.go.ro/sgb/{0}";
        const string LatestVersionStringUrl = "http://hori.go.ro/sgb/version";

        public static void CheckForUpdates()
        {
            Console.WriteLine("Checking for updates");

            try
            {
                string latestVersion = ApplicationUpdater.GetLatestVersionString();

                if (!Directory.Exists(ApplicationInfo.SteamGiveawaysBotDirectory) ||
                    string.Compare(latestVersion, ApplicationInfo.SteamGiveawaysBotVersion) > 0)
                {
                    ApplicationUpdater.UpdateApplication(latestVersion);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking for updates: {ex.Message}");
            }
        }

        public static string GetLatestVersionString()
        {
            using (WebClient client = new WebClient())
            {
                return client
                    .DownloadString(LatestVersionStringUrl)
                    .Replace(" ", "")
                    .Replace("\r", "")
                    .Replace("\n", "");;
            }
        }

        public static void UpdateApplication(string version)
        {
            string archivePath = Path.Combine(ApplicationInfo.ApplicationDirectory, "sgb.zip");
            
            DownloadSgb(archivePath, version, ApplicationInfo.Platform);

            if (Directory.Exists(ApplicationInfo.SteamGiveawaysBotDirectory))
            {
                Directory.Delete(ApplicationInfo.SteamGiveawaysBotDirectory, true);
            }

            Directory.CreateDirectory(ApplicationInfo.SteamGiveawaysBotDirectory);

            ZipFile.ExtractToDirectory(archivePath, ApplicationInfo.SteamGiveawaysBotDirectory);

            File.Delete(archivePath);
            ApplicationInfo.SteamGiveawaysBotVersion = version;
        }

        public static void DownloadSgb(string filePath, string version, string platform)
        {
            string archiveName = string.Format(LatestVersionFileNameFormat, version, platform);
            string url = string.Format(LatestVersionArchiveUrlFormat, archiveName);
            
            Console.WriteLine($"Downloading \"{archiveName}\"");

            using (WebClient client = new WebClient())
            {
                client.DownloadFile(url, filePath);
            }
        }
    }
}
