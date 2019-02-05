using System;

namespace SteamGiveawaysBot.Service.Models
{
    public sealed class SteamAccountResponse : ResponseBase
    {
        public override bool IsSuccess => true;

        public string Username { get; set; }

        public string Password { get; set; }
    }
}
