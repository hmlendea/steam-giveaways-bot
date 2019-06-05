using SteamGiveawaysBot.Service.Models;

namespace SteamGiveawaysBot.Security
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
