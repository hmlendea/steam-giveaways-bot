using System;

namespace SteamGiveawaysBot.Service.Models
{
    public abstract class Request
    {
        public string HmacToken { get; set; }
    }
}
