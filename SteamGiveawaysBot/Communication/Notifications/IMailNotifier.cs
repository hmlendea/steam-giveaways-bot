using System;
using System.Net;
using System.Net.Mail;

using SteamGiveawaysBot.Entities;

namespace SteamGiveawaysBot.Communication.Notifications
{
    public interface IMailNotifier
    {
        void SendMail(string username, string gameTitle, string giveawayLink, string gameKey);
    }
}
