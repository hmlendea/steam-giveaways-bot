using System;

namespace SteamGiveawaysBot.Service.Models
{
    public sealed class ErrorResponse
    {
        public bool IsSuccess { get; set; }

        public string ErrorMessage { get; set; }
    }
}
