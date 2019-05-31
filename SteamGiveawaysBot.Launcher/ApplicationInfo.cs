using System.IO;
using System.Reflection;
using System.Runtime;

namespace SteamGiveawaysBot.Launcher
{
    public static class ApplicationInfo
    {
        const string VersionFileName = "version.txt";
        const string PlatformFileName = "platform.txt";
        const string SgbDirectoryName = "sgb_app";

        static string rootDirectory;

        public static string LauncherName = nameof(SteamGiveawaysBot.Launcher);

        public static string ApplicationName = nameof(SteamGiveawaysBot);

        /// <summary>
        /// The application directory.
        /// </summary>
        public static string LauncherDirectory
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

        public static string ApplicationDirectory => Path.Combine(LauncherDirectory, SgbDirectoryName);

        public static string ApplicationVersion
        {
            get => GetFileValue(VersionFileName);
            set => WriteFileContent(VersionFileName, value);
        }

        public static string Platform => GetFileValue(PlatformFileName, "win-x64");

        static string GetFileValue(string fileName)
            => GetFileValue(fileName, null);

        static string GetFileValue(string fileName, string defaultValue)
        {
            string fileContent = GetFileContent(fileName);
            string firstValue = string.Empty;

            foreach (char c in fileContent ?? string.Empty)
            {
                if (char.IsWhiteSpace(c) ||
                    c == '\r' ||
                    c == '\n')
                {
                    break;
                }

                firstValue += c;
            }

            if (string.IsNullOrWhiteSpace(firstValue))
            {
                return defaultValue;
            }

            return firstValue;
        }

        static string GetFileContent(string fileName)
        {
            string filePath = Path.Combine(LauncherDirectory, fileName);

            if (File.Exists(filePath))
            {
                return File.ReadAllText(filePath);
            }

            return null;
        }

        static void WriteFileContent(string fileName, string fileContent)
        {
            string filePath = Path.Combine(LauncherDirectory, fileName);
            File.WriteAllText(filePath, fileContent);
        }
    }
}
