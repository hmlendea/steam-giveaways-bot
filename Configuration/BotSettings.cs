namespace SteamGiveawaysBot.Configuration
{
    public sealed class BotSettings
    {
        public bool IsEnabled { get; set; }

        public string Endpoint { get; set; }

        public string Username { get; set; }

        public string SharedSecretKey { get; set; }
    }
}
