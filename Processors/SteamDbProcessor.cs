using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;

using NuciLog.Core;
using NuciWeb;

using OpenQA.Selenium;

using SteamGiveawaysBot.Logging;
using SteamGiveawaysBot.Utils;

namespace SteamGiveawaysBot.Processors
{
    public sealed class SteamDbProcessor : WebProcessor
    {
        public string HomePageUrl => "https://steamdb.info";
        public string FreePackagesListUrl => $"{HomePageUrl}/freepackages";

        ILogger logger;
        
        public SteamDbProcessor(
            IWebDriver driver,
            ILogger logger)
            : base(driver)
        {
            this.logger = logger;
        }

        public IEnumerable<uint> GetFreePackagesList()
        {
            logger.Info(
                MyOperation.FreePackagesRetrieval,
                OperationStatus.Started);

            GoToUrl(FreePackagesListUrl);

            IEnumerable<uint> subIds = GetFreePackagesFromChromeDriverSource();

            if (!subIds.Any())
            {
                subIds = GetFreePackagesFromWebClientSource();
            }

            logger.Debug(
                MyOperation.FreePackagesRetrieval,
                OperationStatus.Success,
                new LogInfo(MyLogInfoKey.GamesCount, subIds.Count()));
            
            return subIds;
        }

        IEnumerable<uint> GetFreePackagesFromChromeDriverSource()
        {
            By captchaSelector = By.Id("recaptcha_widget");
            By freePackagesBoxSelector = By.Id("freepackages");
            By packageSelector = By.ClassName("package");

            WaitForAnyElementToBeVisible(freePackagesBoxSelector, captchaSelector);

            if (DoesElementExist(captchaSelector))
            {
                logger.Debug(
                    MyOperation.FreePackagesRetrieval,
                    OperationStatus.Failure,
                    new LogInfo(MyLogInfoKey.Reason, "Captcha input required"));

                return new List<uint>();
            }

            WaitForElementToExist(packageSelector);
            Wait(TimeSpan.FromSeconds(3));
            
            string pageSource = GetPageSource();
            IEnumerable<uint> subIds = GetSubIdsFromSource(pageSource);

            return subIds;
        }

        IEnumerable<uint> GetFreePackagesFromWebClientSource()
        {
            using (WebClient client = new WebClient())
            {
                string html = client.DownloadString(FreePackagesListUrl);
                return GetSubIdsFromSource(html);
            }
        }

        IEnumerable<uint> GetSubIdsFromSource(string pageSource)
        {
            List<string> lines = pageSource.Split('\n').ToList();
            List<uint> subIds = new List<uint>();

            foreach (string line in lines)
            {
                if (!line.Contains("data-subid"))
                {
                    continue;
                }

                string[] lineSplit = line.Split(' ');
                string subIdField = lineSplit[2];

                string[] subIdFieldSplit = subIdField.Split('=');
                string subId = subIdFieldSplit[1].Replace("\"", "");

                subIds.Add(uint.Parse(subId));
            }

            return subIds;
        }
    }
}
