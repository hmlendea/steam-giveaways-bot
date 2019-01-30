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

        public override string RewardsUrl => "https://www.steamgifts.com/giveaways/won";

        int unclaimedRewards;
        Giveaway lastProcessedGiveaway;

        public SteamGiftsProcessor(IWebDriver driver, AccountDetails account)
            : base(driver, account)
        {
            MinimumPoints = 20;
            MaximumPoints = 400;
        }

        public override void GatherInfo()
        {
            Log.Info($"Gathering information");
            GoToUrl(HomePageUrl);

            By loginButtonSelector = By.ClassName("nav__sits");
            By suspensionsButtonSelector = By.XPath(@"//div[contains(@class,'nav__button-container')]/a[@href='/suspensions']");

            if (ElementVisible(loginButtonSelector))
            {
                IsLoggedIn = false;
                return;
            }

            IsLoggedIn = true;

            CurrentPoints = GetCurrentPoints();
            CurrentRewards = GetCurrentRewards();

            Log.Info($"Current points: {CurrentPoints}");
            Log.Info($"Current rewards: {CurrentRewards}");

            if (ElementVisible(suspensionsButtonSelector))
            {
                IsSuspended = true;

                throw new Exception("This account is suspended");
            }

            if (CurrentPoints < MinimumPoints)
            {
                TimerInterval = RechargeInterval;

                return;
            }
            else
            {
                TimerInterval = DefaultInterval;
            }
        }

        public override void LogIn()
        {
            Log.Info($"Logging in {account.Username} on SteamGifts");
            GoToUrl(LogInUrl);

            Click(By.XPath(@"/html/body/header/nav/div[2]/div/a"));
            Click(By.Id("imageLogin"));

            WaitUntilElementExists(By.ClassName("nav__left-container"));

            if (ElementVisible(By.Name("username")))
            {
                Register();
            }
        }

        protected override Giveaway FindGiveaway()
        {
            Log.Info($"Finding a giveaway to join");
            GoToUrl(HomePageUrl);

            By giveawaySelector = By.XPath(@"//div[@class='widget-container']/div[2]/div[3]/div/div/div/h2/a");

            foreach (IWebElement element in GetElements(giveawaySelector))
            {
                Giveaway giveaway = new Giveaway(element.GetAttribute("href"));

                if (lastProcessedGiveaway != giveaway)
                {
                    return giveaway;
                }
            }

            return null;
        }

        protected override void ProcessGiveaway(Giveaway giveaway)
        {
            Log.Info($"Processing giveaway '{giveaway.GiveawayCode}'");
            GoToUrl(giveaway.Url);

            By gameTitleSelector = By.ClassName("featured__heading__medium");
            By priceSelector = By.ClassName("featured__heading__small");
            By remainingTimeSelector = By.XPath(@"//div[@class='featured__summary']/div[2]/div[1]/span");
            By errorSelector = By.XPath(@"//div[contains(@class,'sidebar__error') or contains(@class,'sidebar__entry-delete')]");
            By enterButtonSelector = By.ClassName("sidebar__entry-insert");

            string gameTitle = GetText(gameTitleSelector);
            string priceString = GetElements(priceSelector).Last().Text;
            string endTimeUnixTimestamp = GetAttribute(remainingTimeSelector, "data-timestamp");
            string priceSubstring = priceString.Substring(1).Substring(0, priceString.Length - 3);

            int price = int.Parse(priceSubstring);

            DateTime endTime = DateTimeUtils.FromUnixTime(endTimeUnixTimestamp);

            Log.Info($"Giveaway info retrieved for '{giveaway.GiveawayCode}': \"{gameTitle}\" ({price}P), ends at {endTime}");

            if (endTime > DateTime.Now)
            {
                TimerInterval = endTime - DateTime.Now;
            }

            if (ElementVisible(errorSelector))
            {
                string errorText = GetText(errorSelector).Trim();;

                if (errorText.Contains("Remove Entry"))
                {
                    errorText = "Already entered";
                }

                Log.Warn($"Cannot enter '{giveaway.GiveawayCode}': \"{errorText}\"");
            }
            else
            {
                Log.Info($"Joining giveawayy '{giveaway.GiveawayCode}'");
                Click(enterButtonSelector);
            }

            lastProcessedGiveaway = giveaway;
        }

        protected override void ProcessRewards()
        {
            Log.Info($"Processing the rewards ({CurrentRewards})");
            GoToUrl(RewardsUrl);

            By rewardItemSelector = By.ClassName("table__row-inner-wrap");

            List<IWebElement> rewardItems = GetElements(rewardItemSelector);

            for (int i = 0; i < rewardItems.Count; i++)
            {
                By unreceivedRadioSelector = By.XPath($"//div[@class='table__row-outer-wrap']/div[{i}]/div[4]/div[1]/i[contains(@class,'fa-circle-o')]");
                By keySelector = By.XPath($"//div[@class='table__row-outer-wrap']/div[{1}]/div[3]");
                By gameTitleSelector = By.XPath($"//div[@class='table__row-outer-wrap']/div[{i}]/div[2]/p/a");
                By receivedRadioSelector = By.XPath($"//div[@class='table__row-outer-wrap']/div[{i}]/div[4]/div[1]/i");

                if (!ElementVisible(unreceivedRadioSelector))
                {
                    continue;
                }

                string gameKey = GetText(keySelector).Trim();

                if (gameKey.Length < 6)
                {
                    continue;
                }

                string gameTitle = GetText(gameTitleSelector);
                string giveawayLink = GetHyperlink(gameTitleSelector);

                MailNotifier.SendMail(account.Username, giveawayLink, gameTitle, gameKey);
                Click(receivedRadioSelector);
            }
        }

        void Register()
        {
            Log.Info($"Registering user '{account.Username}'");

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
