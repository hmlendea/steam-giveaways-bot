using System;
using System.Net;
using System.Net.Mail;

using SteamGiveawaysBot.Configuration;
using SteamGiveawaysBot.Entities;
using SteamGiveawaysBot.Security;

namespace SteamGiveawaysBot.Utils
{
    public sealed class GmailMailNotifier : MailNotifier
    {
        public GmailMailNotifier(
            MailSettings mailSettings,
            ICredentialsEncryptor credentialsEncryptor)
            : base(mailSettings, credentialsEncryptor)
        {
        }
        
        protected override SmtpClient BuildSmtpClient(NetworkCredential senderCredentials)
        {
            SmtpClient client = new SmtpClient();
            client.Host = "smtp.gmail.com";
            client.Port = 587;
            client.EnableSsl = true;
            client.UseDefaultCredentials = false;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.Credentials = senderCredentials;

            return client;
        }
    }
}
