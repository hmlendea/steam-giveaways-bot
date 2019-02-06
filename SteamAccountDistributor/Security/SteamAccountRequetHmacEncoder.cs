using SteamGiveawaysBot.SteamAccountDistributor.Models;

namespace SteamGiveawaysBot.SteamAccountDistributor.Security
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
