using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NuciLog;
using NuciLog.Core;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

using SteamGiveawaysBot.Configuration;
using SteamGiveawaysBot.Entities;
using SteamGiveawaysBot.Exceptions;
using SteamGiveawaysBot.Logging;
using SteamGiveawaysBot.Processors;
using SteamGiveawaysBot.Processors.GiveawayProcessors;
using SteamGiveawaysBot.Security;
using SteamGiveawaysBot.Service;
using SteamGiveawaysBot.Service.Models;

namespace SteamGiveawaysBot
{
    public sealed class Program
    {
        static TimeSpan RetryDelay => TimeSpan.FromSeconds(10);

        static BotSettings botSettings;
        static DebugSettings debugSettings;

        static ILogger logger;
        
        static TimeSpan ConnectionWaitTime => TimeSpan.FromMinutes(5);

        static void Main(string[] args)
        {
            IConfiguration config = LoadConfiguration();

            botSettings = new BotSettings();
            debugSettings = new DebugSettings();

            config.Bind(nameof(botSettings), botSettings);
            config.Bind(nameof(DebugSettings), debugSettings);

            IServiceProvider serviceProvider = new ServiceCollection()
                .AddSingleton(botSettings)
                .AddSingleton(debugSettings)
                .AddSingleton<IHmacEncoder<SteamAccountRequest>, SteamAccountRequestHmacEncoder>()
                .AddSingleton<IHmacEncoder<SteamAccountResponse>, SteamAccountResponseHmacEncoder>()
                .AddSingleton<IHmacEncoder<RecordRewardRequest>, RecordRewardRequestHmacEncoder>()
                .AddSingleton<ICredentialsEncryptor, CredentialsEncryptor>()
                .AddSingleton<ILogger, NuciLogger>()
                .AddSingleton<ISteamAccountGatherer, SteamAccountGatherer>()
                .AddSingleton<IRewardSender, RewardSender>()
                .AddSingleton<IBotService, BotService>()
                .BuildServiceProvider();

            logger = serviceProvider.GetService<ILogger>();
            logger.SetSourceContext<Program>();
            logger.Info(Operation.StartUp, $"Application started");

            while (true)
            {
                try
                {
                    RunLoop(serviceProvider);
                }
                catch (Exception ex)
                {
                    logger.Fatal(Operation.Unknown, OperationStatus.Failure, ex);
                    break;
                }
            }

            logger.Info(Operation.ShutDown, $"Application stopped");
        }

        static void RunLoop(IServiceProvider serviceProvider)
        {
            WaitForInternet();

            ISteamAccountGatherer steamAccountGatherer = serviceProvider.GetService<ISteamAccountGatherer>();
            SteamAccount steamAccount = steamAccountGatherer.GatherAccount().Result; // TODO: Broken async

            IRewardSender rewardSender = serviceProvider.GetService<IRewardSender>();
            ILogger logger = serviceProvider.GetService<ILogger>();
            logger.SetSourceContext<BotService>();
            
            IBotService botService = new BotService(
                steamAccount,
                debugSettings,
                rewardSender,
                logger);

            botService.Run();
        }

        static void WaitForInternet()
        {
            bool isConnected = false;

            while (!isConnected)
            {
                try
                {
                    using (var client = new WebClient())
                    using (client.OpenRead("http://clients3.google.com/generate_204"))
                    {
                        isConnected = true;
                        return;
                    }
                }
                catch
                {
                    isConnected = false;
                }

                logger.Error(
                    MyOperation.ConnectivityTest,
                    OperationStatus.Failure,
                    new LogInfo(MyLogInfoKey.RetryDelay, ConnectionWaitTime.TotalMilliseconds));

                Thread.Sleep((int)ConnectionWaitTime.TotalMilliseconds);
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
