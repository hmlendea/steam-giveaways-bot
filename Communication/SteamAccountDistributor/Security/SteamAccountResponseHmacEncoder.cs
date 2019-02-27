using SteamGiveawaysBot.Communication.SteamAccountDistributor.Models;

namespace SteamGiveawaysBot.Communication.SteamAccountDistributor.Security
{
    public sealed class SteamAccountResponseHmacEncoder : HmacEncoder<SteamAccountResponse>
    {
        public override string GenerateToken(SteamAccountResponse obj, string sharedSecretKey)
        {
            string stringForSigning =
                obj.Username +
                obj.Password +
                obj.IsSuccess;

            return ComputeHmacToken(stringForSigning, sharedSecretKey);
        }
    }
}
