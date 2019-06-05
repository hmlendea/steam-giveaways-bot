namespace SteamGiveawaysBot.Service.Models
{
    public sealed class AccountRequest : Request
    {
        public string Username { get; set; }

        public string GiveawaysProvider { get; set; }
    }
}
