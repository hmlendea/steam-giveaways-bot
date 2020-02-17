using System.IO;
using System.Reflection;

namespace SteamGiveawaysBot.Utils
{
    public sealed class ApplicationPaths
    {
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

        public static string AppSettingsFile => Path.Combine(ApplicationDirectory, "appsettings.json");

        public static string DataDirectory => Path.Combine(ApplicationDirectory, "Data");
    }
}
