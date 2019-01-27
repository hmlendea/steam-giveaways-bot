using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

using SteamGiveawaysBot.Entities;
using SteamGiveawaysBot.Utils;

namespace SteamGiveawaysBot.Processors.GiveawayProcessors
{
    public sealed class SteamGiftsProcessor : GiveawayProcessor
    {
        public override string HomePageUrl => "https://www.steamgifts.com/";

        public override string LogInUrl => "https://www.steamgifts.com/";

        public override int MinimumPoints => 20;

        public override int MaximumPoints => 400;

        int unclaimedRewards;

        public SteamGiftsProcessor(IWebDriver driver, AccountDetails account)
            : base(driver, account)
        {
        }

        public override void LogIn()
        {
            GoToUrl(LogInUrl);

            Click(By.XPath(@"/html/body/header/nav/div[2]/div/a"));
            Click(By.Id("imageLogin"));

            WaitUntilElementExists(By.ClassName("nav__left-container"));

            if (ElementVisible(By.Name("username")))
            {
                Register();
            }
        }

        public override void GetGiveaway()
        {
            int currentPoints = GetCurrentPoints();
            int currentRewards = GetCurrentRewards();

            // TODO: Improve this - currently if a gift is claimed and another one immediately won,
            // it won't notice the change.
            if (currentRewards > 0 && currentRewards > unclaimedRewards)
            {
                Log.Info($"$Unclaimed rewards ({currentRewards})");
            }
        }

        void Register()
        {
            SetText(By.Name("username"), account.Username);
            SetText(By.Name("email"), account.EmailAddress);
            SelectByValue(By.Name("country"), account.Country.SgCode.ToString());

            // Say no to emails
            Click(By.XPath(@"//form/div[4]/div[2]/div[1]/div[2]/i[1]"));
            Click(By.XPath(@"//form/div[5]/div[2]/div[1]/div[2]/i[1]"));

            // Complete registration
            Click(By.XPath(@"//form/div[6]/div[1]"));
        }

        int GetCurrentPoints()
        {
            string pointsString = GetText(By.ClassName("nav__points"));

            return int.Parse(pointsString);
        }

        int GetCurrentRewards()
        {
            string rewardsString = GetText(By.XPath(@"/html/body/header/nav/div[2]/div[2]/a"));

            if (string.IsNullOrWhiteSpace(rewardsString))
            {
                return 0;
            }

            return int.Parse(rewardsString);
        }
    }
}
