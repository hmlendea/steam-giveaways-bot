using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using SteamGiveawaysBot.Utils;

namespace SteamGiveawaysBot.Configuration
{
    public sealed class AppSettingsUpdater : IAppSettingsUpdater
    {
        public void UpdateSetting(string settingPath, string value)
        {
            string[] levels = settingPath.Split('.');

            if (levels.Length < 1)
            {
                throw new ArgumentException("Invalid setting path");
            }

            string appSettingsJson = File.ReadAllText(ApplicationPaths.AppSettingsFile);
            JObject appSettings = JObject.Parse(appSettingsJson);
            JToken token = appSettings.SelectToken(settingPath);

            if (token == null)
            {
                throw new ArgumentException("Invalid settings path");
            }

            if (token.ToString() == value)
            {
                return;
            }
            
            token.Replace(value);
            File.WriteAllText(ApplicationPaths.AppSettingsFile, appSettings.ToString());
        }
    }
}
