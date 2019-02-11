using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SteamGiveawaysBot.Security
{
    public sealed class CredentialsEncryptor : ICredentialsEncryptor
    {
        public string Encrypt(string value)
        {
            byte[] data = Encoding.UTF8.GetBytes(value);
            string cypher = Convert.ToBase64String(data);

            return cypher;
        }

        public string Decrypt(string cypher)
        {
            if (string.IsNullOrWhiteSpace(cypher))
            {
                return null;
            }
            
            byte[] valueBytes = Convert.FromBase64String(cypher);
            string value = Encoding.UTF8.GetString(valueBytes);

            return value;
        }
    }
}
