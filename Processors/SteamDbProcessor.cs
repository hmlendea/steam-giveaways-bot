using System;
using System.Collections.Generic;
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

        public IEnumerable<string> GetFreePackagesList()
        {
            logger.Info(
                MyOperation.FreePackagesRetrieval,
                OperationStatus.Started);

            GoToUrl(FreePackagesListUrl);

            By freePackagesBoxSelector = By.Id("freepackages");
            By packageSelector = By.ClassName("package");

            WaitForElementToBeVisible(freePackagesBoxSelector);
            WaitForElementToExist(packageSelector);

            Wait(TimeSpan.FromSeconds(3));
            
            string pageSource = GetPageSource();
            IEnumerable<string> subIds = GetSubIdsFromSource(pageSource);

            logger.Debug(
                MyOperation.FreePackagesRetrieval,
                OperationStatus.Success,
                new LogInfo(MyLogInfoKey.GamesCount, subIds.Count()));
            
            return subIds;
        }

        IEnumerable<string> GetSubIdsFromSource(string pageSource)
        {
            List<string> lines = pageSource.Split('\n').ToList();
            List<string> subids = new List<string>();

            foreach (string line in lines)
            {
                if (!line.Contains("data-subid"))
                {
                    continue;
                }

                string[] lineSplit = line.Split(' ');
                string subidField = lineSplit[2];

                string[] subidFieldSplit = subidField.Split('=');
                string subid = subidFieldSplit[1].Replace("\"", "");

                subids.Add(subid);
            }

            return subids;
        }
    }
}
