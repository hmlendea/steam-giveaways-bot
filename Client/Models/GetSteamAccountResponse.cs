using System.Text.Json.Serialization;
using NuciAPI.Responses;
using NuciSecurity.HMAC;

namespace SteamGiveawaysBot.Client.Models
{
    public sealed class GetSteamAccountResponse : NuciApiSuccessResponse
    {
        [HmacOrder(1)]
        [JsonPropertyName("username")]
        public string Username { get; set; }

        [HmacOrder(2)]
        [JsonPropertyName("password")]
        public string Password { get; set; }
    }
}
