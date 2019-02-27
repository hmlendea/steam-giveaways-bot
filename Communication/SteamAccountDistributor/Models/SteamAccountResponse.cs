using System;

namespace SteamGiveawaysBot.Communication.SteamAccountDistributor.Models
{
    public sealed class SteamAccountResponse
    {
        public bool IsSuccess => true;

        public string Username { get; set; }

        public string Password { get; set; }

        public string HmacToken { get; set; }
    }
}
