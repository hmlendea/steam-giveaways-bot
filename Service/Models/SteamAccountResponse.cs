using System;

namespace SteamGiveawaysBot.Service.Models
{
    public sealed class SteamAccountResponse
    {
        public bool IsSuccess => true;

        public string Username { get; set; }

        public string Password { get; set; }

        public string HmacToken { get; set; }
    }
}
