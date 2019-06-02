using SteamGiveawaysBot.Service.Models;

namespace SteamGiveawaysBot.Security
{
    public sealed class SteamAccountRequestHmacEncoder : HmacEncoder<SteamAccountRequest>
    {
        public override string GenerateToken(SteamAccountRequest obj, string sharedSecretKey)
        {
            string stringForSigning =
                obj.Username +
                obj.GiveawaysProvider;

            string hmacToken = ComputeHmacToken(stringForSigning, sharedSecretKey);

            return hmacToken;
        }
    }
}
