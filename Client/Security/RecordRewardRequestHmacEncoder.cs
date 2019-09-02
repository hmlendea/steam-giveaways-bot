using NuciSecurity.HMAC;

using SteamGiveawaysBot.Client.Models;

namespace SteamGiveawaysBot.Client.Security
{
    public sealed class RecordRewardRequestHmacEncoder : HmacEncoder<RecordRewardRequest>
    {
        public override string GenerateToken(RecordRewardRequest obj, string sharedSecretKey)
        {
            string stringForSigning =
                obj.Username +
                obj.GiveawaysProvider +
                obj.GiveawayId +
                obj.SteamUsername +
                obj.SteamAppId +
                obj.ActivationKey;

            string hmacToken = ComputeHmacToken(stringForSigning, sharedSecretKey);

            return hmacToken;
        }
    }
}
