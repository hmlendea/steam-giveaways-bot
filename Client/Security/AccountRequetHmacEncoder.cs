using NuciSecurity.HMAC;

using SteamGiveawaysBot.Client.Models;

namespace SteamGiveawaysBot.Client.Security
{
    public sealed class AccountRequestHmacEncoder : HmacEncoder<AccountRequest>
    {
        public override string GenerateToken(AccountRequest obj, string sharedSecretKey)
        {
            string stringForSigning =
                obj.Username +
                obj.GiveawaysProvider;

            string hmacToken = ComputeHmacToken(stringForSigning, sharedSecretKey);

            return hmacToken;
        }
    }
}
