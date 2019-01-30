using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;

using Microsoft.Extensions.Configuration;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

using SteamGiveawaysBot.Entities;
using SteamGiveawaysBot.Processors;
using SteamGiveawaysBot.Processors.GiveawayProcessors;
using SteamGiveawaysBot.Utils;

namespace SteamGiveawaysBot
{
    public sealed class Program
    {
        static TimeSpan RetryDelay => TimeSpan.FromSeconds(10);

        static void Main(string[] args)
        {
            IConfiguration config = LoadConfiguration();

            AccountDetails account = new AccountDetails
            {
                Username = config["steamUsername"],
                Password = config["steamPassword"]
            };

            MailNotifier.RecipientAddress = config["mailRecipientAddress"];
            MailNotifier.SenderAddress = config["mailSenderAddress"];
            MailNotifier.SenderPassword = config["mailSenderPassword"];

            Bot bot = new Bot(account);
            
            try
            {
                bot.Run();
            }
            catch (Exception ex)
            {
                Log.Error($"{ex.Message}\n{ex.StackTrace}");

                Log.Info($"Waiting {RetryDelay.TotalSeconds} seconds to retry");
                Thread.Sleep((int)RetryDelay.TotalMilliseconds);
            }
        }

        static IConfiguration LoadConfiguration()
        {
            return new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .Build();
        }
    }
}
