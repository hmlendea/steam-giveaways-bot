namespace SteamGiveawaysBot.Service.Models
{
    public sealed class AccountRequest
    {
        public string Username { get; set; }

        public string GiveawaysProvider { get; set; }
    }
}
