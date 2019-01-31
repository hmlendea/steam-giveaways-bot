using System;
using System.Net;
using System.Net.Mail;

namespace SteamGiveawaysBot.Utils
{
    public sealed class MailNotifier
    {
        public static string RecipientAddress { get; set; }

        public static string SenderAddress { get; set; }

        public static string SenderPassword { get; set; }

        public static void SendMail(string username, string giveawayLink, string gameTitle, string gameKey)
        {
            SmtpClient smtp = new SmtpClient();
            smtp.Host = "smtp.gmail.com";
            smtp.Port = 587;
            smtp.EnableSsl = true;
            smtp.UseDefaultCredentials = false;
            smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtp.Credentials = new NetworkCredential(SenderAddress, SenderPassword);

            using (MailMessage mail = new MailMessage())
            {
                mail.From = new MailAddress(SenderAddress, $"SteamGiveawaysBot");
                mail.Subject = $"SGB - '{gameTitle}' key won by {username}";
                mail.Body =
                    $"You have won a giveaway for '{gameTitle}': {giveawayLink}" + Environment.NewLine +
                    $"Here is the key: {gameKey}";
                mail.To.Add(RecipientAddress);

                smtp.Send(mail);
            }
        }
    }
}
