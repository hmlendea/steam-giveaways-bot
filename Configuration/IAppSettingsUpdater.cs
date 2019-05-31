namespace SteamGiveawaysBot.Configuration
{
    public interface IAppSettingsUpdater
    {
        void UpdateSetting(string setting, string value);
    }
}
