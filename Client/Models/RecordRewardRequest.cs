using System.Text.Json.Serialization;
using NuciAPI.Requests;
using NuciSecurity.HMAC;

namespace SteamGiveawaysBot.Client.Models
{
    public sealed class RecordRewardRequest : NuciApiRequest
    {
        [HmacOrder(1)]
        [JsonPropertyName("username")]
        public string Username { get; set; }

        [HmacOrder(2)]
        [JsonPropertyName("gaProvider")]
        public string GiveawaysProvider { get; set; }

        [HmacOrder(3)]
        [JsonPropertyName("gaId")]
        public string GiveawayId { get; set; }

        [HmacOrder(4)]
        [JsonPropertyName("steamUsername")]
        public string SteamUsername { get; set; }

        [HmacOrder(5)]
        [JsonPropertyName("steamAppId")]
        public string SteamAppId { get; set; }

        [HmacOrder(6)]
        [JsonPropertyName("key")]
        public string ActivationKey { get; set; }
    }
}
