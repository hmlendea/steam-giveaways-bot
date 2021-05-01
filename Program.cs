using System;
using System.IO;
using System.Net;
using System.Threading;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NuciCLI;
using NuciLog;
using NuciLog.Configuration;
using NuciLog.Core;
using NuciSecurity.HMAC;
using NuciWeb;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

using SteamGiveawaysBot.Client;
using SteamGiveawaysBot.Client.Models;
using SteamGiveawaysBot.Client.Security;
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
        static string[] UsernameOptions = { "-u", "--user", "--username" };
        static string[] SecretKeyOptions = { "-k", "--ssk", "--key", "--secretkey", "--sharedsecretkey" };

        static TimeSpan RetryDelay => TimeSpan.FromMinutes(5);

        static BotSettings botSettings;
        static DebugSettings debugSettings;
        static NuciLoggerSettings loggingSettings;
        static AccountDetails accountDetails;
        static IWebDriver webDriver;

        static ILogger logger;
        
        static TimeSpan ConnectionWaitTime => TimeSpan.FromMinutes(5);

        static void Main(string[] args)
        {
            botSettings = new BotSettings();
            debugSettings = new DebugSettings();
            loggingSettings = new NuciLoggerSettings();
            accountDetails = new AccountDetails();

            IConfiguration config = LoadConfiguration();
            config.Bind(nameof(BotSettings), botSettings);
            config.Bind(nameof(DebugSettings), debugSettings);
            config.Bind(nameof(NuciLoggerSettings), loggingSettings);

            LoadBotCredentials(args);
            webDriver = SetupWebDriver();

            IServiceProvider serviceProvider = new ServiceCollection()
                .AddSingleton(botSettings)
                .AddSingleton(debugSettings)
                .AddSingleton(loggingSettings)
                .AddSingleton(accountDetails)
                .AddSingleton<ILogger, NuciLogger>()
                .AddSingleton<IHmacEncoder<AccountRequest>, AccountRequestHmacEncoder>()
                .AddSingleton<IHmacEncoder<AccountResponse>, AccountResponseHmacEncoder>()
                .AddSingleton<IHmacEncoder<RecordRewardRequest>, RecordRewardRequestHmacEncoder>()
                .AddSingleton<IServerCommunicator<AccountRequest, AccountResponse>, ServerCommunicator<AccountRequest, AccountResponse>>()
                .AddSingleton<IServerCommunicator<RecordRewardRequest>, ServerCommunicator<RecordRewardRequest>>()
                .AddSingleton<IAccountGatherer, AccountGatherer>()
                .AddSingleton<IRewardSender, RewardSender>()
                .AddSingleton(webDriver)
                .AddTransient<IWebProcessor, WebProcessor>()
                .AddSingleton<IGiveawayProcessor, SteamGiftsProcessor>()
                .AddSingleton<ISteamProcessor, SteamProcessor>()
                .AddSingleton<IBotService, BotService>()
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
            if (CliArgumentsReader.HasOption(args, UsernameOptions))
            {
                botSettings.Username = CliArgumentsReader.GetOptionValue(args, UsernameOptions);
            }

            if (CliArgumentsReader.HasOption(args, SecretKeyOptions))
            {
                botSettings.SharedSecretKey = CliArgumentsReader.GetOptionValue(args, SecretKeyOptions);
            }
        }

        static void RunLoop(IServiceProvider serviceProvider)
        {
            WaitForInternet();

            IBotService botService = serviceProvider.GetService<IBotService>();
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

        static IWebDriver SetupWebDriver()
        {
            ChromeOptions options = new ChromeOptions();
            options.PageLoadStrategy = PageLoadStrategy.None;
            options.AddArgument("--silent");
            options.AddArgument("--no-sandbox");
			options.AddArgument("--disable-translate");
			options.AddArgument("--disable-infobars");

            if (debugSettings.IsHeadless)
            {
                options.AddArgument("--headless");
                options.AddArgument("--disable-gpu");
                options.AddArgument("--window-size=1920,1080");
                options.AddArgument("--start-maximized");
                options.AddArgument("--blink-settings=imagesEnabled=false");
                options.AddUserProfilePreference("profile.default_content_setting_values.images", 2);
            }

            ChromeDriverService service = ChromeDriverService.CreateDefaultService();
            service.SuppressInitialDiagnosticInformation = true;
            service.HideCommandPromptWindow = true;

            IWebDriver driver = new ChromeDriver(service, options, TimeSpan.FromSeconds(botSettings.PageLoadTimeout));
            IJavaScriptExecutor scriptExecutor = (IJavaScriptExecutor)driver;
            string userAgent = (string)scriptExecutor.ExecuteScript("return navigator.userAgent;");

            if (userAgent.Contains("Headless"))
            {
                userAgent = userAgent.Replace("Headless", "");
                options.AddArgument($"--user-agent={userAgent}");

                driver.Quit();
                driver = new ChromeDriver(service, options);
            }

            driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(botSettings.PageLoadTimeout);
            driver.Manage().Window.Maximize();

            return driver;
        }

        static void SaveCrashScreenshot()
        {
            if (!debugSettings.IsCrashScreenshotEnabled)
            {
                return;
            }

            string directory = Path.GetDirectoryName(loggingSettings.LogFilePath);
            string filePath = Path.Combine(directory, debugSettings.CrashScreenshotFileName);

            ((ITakesScreenshot)webDriver)
                .GetScreenshot()
                .SaveAsFile(filePath);
        }
    }
}
