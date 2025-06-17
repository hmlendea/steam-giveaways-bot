using System.Text.Json.Serialization;
using NuciAPI.Requests;

namespace SteamGiveawaysBot.Client.Models
{
    public sealed class SteamAccountRequest : Request
    {
        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("gaProvider")]
        public string GiveawaysProvider { get; set; }
    }
}
