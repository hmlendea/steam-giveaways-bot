namespace SteamGiveawaysBot.Entities
{
    public sealed class Reward
    {
        public string GiveawaysProvider { get; set; }

        public string GiveawayUrl { get; set; }

        public string GiveawayId
        {
            get
            {
                string[] fields = GiveawayUrl.Split('/');
                return fields[fields.Length - 2];
            }
        }

        public string WinnerUsername { get; set; }

        public string SteamAppId { get; set; }

        public string GameTitle { get; set; }

        public string ActivationKey { get; set; }
    }
}
