using NuciAPI.Responses;

namespace SteamGiveawaysBot.Client.Models
{
    public sealed class SteamAccountResponse : SuccessResponse
    {
        public string Username { get; set; }

        public string Password { get; set; }
    }
}
