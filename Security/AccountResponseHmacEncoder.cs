using SteamGiveawaysBot.Service.Models;

namespace SteamGiveawaysBot.Security
{
    public sealed class AccountResponseHmacEncoder : HmacEncoder<AccountResponse>
    {
        public override string GenerateToken(AccountResponse obj, string sharedSecretKey)
        {
            string stringForSigning =
                obj.Username +
                obj.Password +
                obj.IsSuccess;

            return ComputeHmacToken(stringForSigning, sharedSecretKey);
        }
    }
}
