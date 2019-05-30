using System;
using System.Net;
using System.Net.Mail;

using SteamGiveawaysBot.Configuration;
using SteamGiveawaysBot.Entities;
using SteamGiveawaysBot.Security;

namespace SteamGiveawaysBot.Communication.Notifications
{
    public abstract class MailNotifier : IMailNotifier
    {
        readonly MailSettings mailSettings;

        readonly ICredentialsEncryptor credentialsEncryptor;

        public MailNotifier(
            MailSettings mailSettings,
            ICredentialsEncryptor credentialsEncryptor)
        {
            this.mailSettings = mailSettings;
            this.credentialsEncryptor = credentialsEncryptor;
        }

        public void SendMail(
            string username,
            string gameTitle,
            string giveawayLink,
            string gameKey)
        {
            string senderPassword = credentialsEncryptor.Decrypt(mailSettings.SenderPassword);

            MailAddress senderAddress = new MailAddress(mailSettings.SenderAddress, "SteamGiveawaysBot");
            NetworkCredential senderCredentials = new NetworkCredential(mailSettings.SenderAddress, senderPassword);
            SmtpClient client = BuildSmtpClient(senderCredentials);

            using (MailMessage mail = new MailMessage())
            {
                mail.From = senderAddress;
                mail.Subject = $"SGB - '{gameTitle}' key won by {username}";
                mail.Body =
                    $"You have won a giveaway for '{gameTitle}': {giveawayLink}" + Environment.NewLine +
                    $"Here is the key: {gameKey}";
                mail.To.Add(mailSettings.RecipientAddress);

                client.Send(mail);
            }
        }

        protected abstract SmtpClient BuildSmtpClient(NetworkCredential senderCredentials);
    }
}
