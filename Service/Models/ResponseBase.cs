using System;

namespace SteamGiveawaysBot.Service.Models
{
    public abstract class ResponseBase
    {
        public abstract bool IsSuccess { get; }
    }
}
