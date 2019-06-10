namespace SteamGiveawaysBot.Entities
{
    public sealed class Reward
    {
        public string GiveawaysProvider { get; set; }

        public string GiveawayId { get; set; }

        public string WinnerUsername { get; set; }

        public string SteamAppId { get; set; }

        public string ActivationKey { get; set; }
    }
}
