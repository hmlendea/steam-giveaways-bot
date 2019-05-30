using System.IO;
using System.IO.Compression;
using System.Net;

namespace SteamGiveawaysBot.Launcher
{
    public static class ApplicationUpdater
    {
        const string LatestVersionArchiveUrlFormat = "http://hori.go.ro/sgb/sgb_{0}_{1}.zip";
        const string LatestVersionStringUrl = "http://hori.go.ro/sgb/version";

        public static string GetLatestVersionString()
        {
            using (WebClient client = new WebClient())
            {
                return client.DownloadString(LatestVersionStringUrl);
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
            string url = string
                .Format(LatestVersionArchiveUrlFormat, version, platform)
                .Replace(" ", "")
                .Replace("\r", "")
                .Replace("\n", "");

            System.Console.WriteLine(url);
            System.Console.ReadKey();

            using (WebClient client = new WebClient())
            {
                client.DownloadFile(url, filePath);
            }
        }
    }
}
