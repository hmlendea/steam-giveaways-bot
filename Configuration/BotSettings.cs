namespace SteamGiveawaysBot.Configuration
{
    public sealed class BotSettings
    {
        public string ServerUrl { get; set; }

        public string ServerApiKey { get; set; }

        public string Username { get; set; }

        public string SharedSecretKey { get; set; }

        public int PageLoadTimeout { get; set; }
    }
}
