namespace SteamGiveawaysBot.Security
{
    public interface ICredentialsEncryptor
    {
        string Encrypt(string value);

        string Decrypt(string hash);
    }
}
