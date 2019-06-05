using System;

namespace SteamGiveawaysBot.Service.Models
{
    public sealed class AccountResponse : Response
    {
        public string Username { get; set; }

        public string Password { get; set; }
    }
}
