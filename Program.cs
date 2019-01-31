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

        static Bot bot;

        static void Main(string[] args)
        {
            Log.Info($"Application started");

            IConfiguration config = LoadConfiguration();

            AccountDetails account = new AccountDetails
            {
                Username = config["steamUsername"],
                Password = config["steamPassword"]
            };

            Log.LogFilePath = config["logFilePath"];

            MailNotifier.RecipientAddress = config["mailRecipientAddress"];
            MailNotifier.SenderAddress = config["mailSenderAddress"];
            MailNotifier.SenderPassword = config["mailSenderPassword"];

            try
            {
                bot = new Bot(account);
                bot.Stopped += bot_OnStopped;
                bot.Run();

                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Log.Error($"Fatality: {ex.Message}");
            }
        }

        static IConfiguration LoadConfiguration()
        {
            return new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .Build();
        }

        static void bot_OnStopped(object sender, EventArgs e)
        {
            Log.Info($"Waiting {RetryDelay.TotalSeconds} seconds to retry");
            Thread.Sleep((int)RetryDelay.TotalMilliseconds);

            bot.Run();
        }
    }
}
