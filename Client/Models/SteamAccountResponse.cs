using System.Text.Json.Serialization;
using NuciAPI.Responses;

namespace SteamGiveawaysBot.Client.Models
{
    public sealed class SteamAccountResponse : SuccessResponse
    {
        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("password")]
        public string Password { get; set; }
    }
}
