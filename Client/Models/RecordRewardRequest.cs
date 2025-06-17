using System.Text.Json.Serialization;
using NuciAPI.Requests;

namespace SteamGiveawaysBot.Client.Models
{
    public sealed class RecordRewardRequest : Request
    {
        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("gaProvider")]
        public string GiveawaysProvider { get; set; }

        [JsonPropertyName("gaId")]
        public string GiveawayId { get; set; }

        [JsonPropertyName("steamUsername")]
        public string SteamUsername { get; set; }

        [JsonPropertyName("steamAppId")]
        public string SteamAppId { get; set; }

        [JsonPropertyName("key")]
        public string ActivationKey { get; set; }
    }
}
