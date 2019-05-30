using System.IO;
using System.Reflection;

namespace SteamGiveawaysBot.Launcher
{
    public static class ApplicationInfo
    {
        const string VersionFileName = "version.txt";
        const string PlatformFileName = "platform.txt";
        const string SgbDirectoryName = "sgb_app";

        static string rootDirectory;

        /// <summary>
        /// The application directory.
        /// </summary>
        public static string ApplicationDirectory
        {
            get
            {
                if (rootDirectory == null)
                {
                    rootDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                }

                return rootDirectory;
            }
        }

        public static string SteamGiveawaysBotDirectory => Path.Combine(ApplicationDirectory, SgbDirectoryName);

        public static string SteamGiveawaysBotVersion
        {
            get => GetFileContent(VersionFileName);
            set => File.WriteAllText(VersionFileName, value);
        }

        public static string Platform => GetFileContent(PlatformFileName);

        static string GetFileContent(string fileName)
        {
            string filePath = Path.Combine(ApplicationDirectory, fileName);
            return File.ReadAllText(filePath);
        }
    }
}
