using System;

namespace SteamGiveawaysBot.Service.Models
{
    public sealed class ErrorResponse : Response
    {
        public string ErrorMessage { get; set; }
    }
}
