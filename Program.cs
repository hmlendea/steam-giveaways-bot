using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

using SteamGiveawaysBot.Configuration;
using SteamGiveawaysBot.Entities;
using SteamGiveawaysBot.Logging;
using SteamGiveawaysBot.Processors;
using SteamGiveawaysBot.Processors.GiveawayProcessors;
using SteamGiveawaysBot.Service;
using SteamGiveawaysBot.Utils;

namespace SteamGiveawaysBot
{
    public sealed class Program
    {
        static TimeSpan RetryDelay => TimeSpan.FromSeconds(10);

        static BotService bot;

        static ILogger logger;

        static void Main(string[] args)
        {
            IConfiguration config = LoadConfiguration();

            DebugSettings debugSettings = new DebugSettings();
            MailSettings mailSettings = new MailSettings();
            SteamSettings steamSettings = new SteamSettings();

            config.Bind(nameof(DebugSettings), debugSettings);
            config.Bind(nameof(MailSettings), mailSettings);
            config.Bind(nameof(SteamSettings), steamSettings);
            
            var serviceProvider = new ServiceCollection()
                .AddSingleton(debugSettings)
                .AddSingleton(mailSettings)
                .AddSingleton(steamSettings)
                .AddSingleton<IMailNotifier, GmailMailNotifier>()
                .AddSingleton<ILogger, Logger>()
                .AddSingleton<IBotService, BotService>()
                .BuildServiceProvider();

            logger = serviceProvider.GetService<ILogger>();
            logger.Info($"Application started");

            try
            {
                IBotService bot = serviceProvider.GetService<IBotService>();
                bot.Stopped += bot_OnStopped;
                bot.Run();

                Console.ReadLine();
            }
            catch (Exception ex)
            {
                logger.Fatal($"Fatal exception: {ex.Message}");
            }

            logger.Info($"Application stopped");
        }

        static IConfiguration LoadConfiguration()
        {
            return new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .Build();
        }

        static void bot_OnStopped(object sender, EventArgs e)
        {
            logger.Info($"Waiting {RetryDelay.TotalSeconds} seconds to retry");
            Thread.Sleep((int)RetryDelay.TotalMilliseconds);

            bot.Run();
        }
    }
}
