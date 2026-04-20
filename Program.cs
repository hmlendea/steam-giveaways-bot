using System;
using System.Threading;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NuciAPI.Client;
using NuciCLI.Arguments;
using NuciLog;
using NuciLog.Core;
using NuciWeb.Automation;
using NuciWeb.Automation.Selenium;
using NuciWeb.HTTP;
using NuciWeb.Steam;
using NuciWeb.Steam.Models;

using OpenQA.Selenium;

using SteamGiveawaysBot.Client;
using SteamGiveawaysBot.Configuration;
using SteamGiveawaysBot.Logging;
using SteamGiveawaysBot.Processors.GiveawayProcessors;
using SteamGiveawaysBot.Service;

namespace SteamGiveawaysBot
{
    public sealed class Program
    {
        static readonly string[] UsernameOptions = ["-u", "--user", "--username"];
        static readonly string[] SecretKeyOptions = ["-k", "--ssk", "--key", "--secretkey", "--sharedsecretkey"];

        static TimeSpan RetryDelay => TimeSpan.FromMinutes(5);

        static BotSettings botSettings;
        static DebugSettings debugSettings;
        static SteamAccount steamAccount;
        static IWebDriver webDriver;

        static ILogger logger;

        static TimeSpan ConnectionWaitTime => TimeSpan.FromMinutes(5);

        static void Main(string[] args)
        {
            botSettings = new BotSettings();
            debugSettings = new DebugSettings();
            steamAccount = new SteamAccount();

            IConfiguration config = LoadConfiguration();
            config.Bind(nameof(BotSettings), botSettings);
            config.Bind(nameof(DebugSettings), debugSettings);

            LoadBotCredentials(args);
            webDriver = WebDriverInitialiser.InitialiseAvailableWebDriver(debugSettings.IsDebugMode);

            IServiceProvider serviceProvider = new ServiceCollection()
                .AddNuciLoggerSettings(config)
                .AddSingleton(botSettings)
                .AddSingleton(debugSettings)
                .AddSingleton(steamAccount)
                .AddSingleton<ILogger, NuciLogger>()
                .AddSingleton<IAccountGatherer, AccountGatherer>()
                .AddSingleton<IRewardSender, RewardSender>()
                .AddSingleton(webDriver)
                .AddTransient<IWebProcessor, SeleniumWebProcessor>()
                .AddSingleton<IGiveawayProcessor, SteamGiftsProcessor>()
                .AddSingleton<ISteamProcessor, SteamProcessor>()
                .AddSingleton<IBotService, BotService>()
                .AddTransient<INuciApiClient>(provider => new NuciApiClient(botSettings.ServerUrl))
                .BuildServiceProvider();

            logger = serviceProvider.GetService<ILogger>();
            logger.Info(Operation.StartUp, $"Application started");

            while (true)
            {
                try
                {
                    RunLoop(serviceProvider);
                }
                catch (AggregateException ex)
                {
                    foreach (Exception innerException in ex.InnerExceptions)
                    {
                        logger.Fatal(Operation.Unknown, OperationStatus.Failure, innerException);
                    }
                }
                catch (Exception ex)
                {
                    logger.Fatal(Operation.Unknown, OperationStatus.Failure, ex);
                }

                SaveCrashScreenshot();

                logger.Info(
                    MyOperation.CrashRecovery,
                    new LogInfo(MyLogInfoKey.RetryDelay, RetryDelay.TotalMilliseconds));

                Thread.Sleep((int)RetryDelay.TotalMilliseconds);
            }
        }

        static void LoadBotCredentials(string[] args)
        {
            ArgumentParser parser = new();

            foreach (string usernameOption in UsernameOptions)
            {
                parser.AddArgument(usernameOption, string.Empty, false, null);
            }

            foreach (string secretKeyOption in SecretKeyOptions)
            {
                parser.AddArgument(secretKeyOption, string.Empty, false, null);
            }

            ArgumentsCollection parsedArgs = parser.ParseArgs(args);

            if (TryGetOptionValue(parsedArgs, UsernameOptions, out string username))
            {
                botSettings.Username = username;
            }

            if (TryGetOptionValue(parsedArgs, SecretKeyOptions, out string sharedSecretKey))
            {
                botSettings.SharedSecretKey = sharedSecretKey;
            }
        }

        static bool TryGetOptionValue(ArgumentsCollection parsedArgs, string[] options, out string value)
        {
            foreach (string option in options)
            {
                if (parsedArgs.Has(option))
                {
                    value = parsedArgs.Get<string>(option);

                    return true;
                }
            }

            value = null;

            return false;
        }

        static void RunLoop(IServiceProvider serviceProvider)
        {
            WaitForInternet();

            IBotService botService = serviceProvider.GetService<IBotService>();
            botService.Run();
        }

        static void WaitForInternet()
        {
            logger.Info(
                MyOperation.ConnectivityTest,
                OperationStatus.Started,
                "Waiting for internet access");

            NetworkUtils.WaitForInternetAccess(TimeSpan.FromDays(1));
        }

        static IConfiguration LoadConfiguration() => new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", true, true)
            .Build();

        static void SaveCrashScreenshot()
        {
            if (!debugSettings.IsCrashScreenshotEnabled)
            {
                return;
            }

            string filePath = debugSettings.CrashScreenshotFileName;

            ((ITakesScreenshot)webDriver)
                .GetScreenshot()
                .SaveAsFile(filePath);
        }
    }
}
