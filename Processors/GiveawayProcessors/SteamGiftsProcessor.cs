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
            //Log.Info($"Gathering information");
            GoToUrl(HomePageUrl);

            By loginButtonSelector = By.ClassName("nav__sits");
            By pointsSelector = By.ClassName("nav__points");
            By suspensionsButtonSelector = By.XPath(@"//div[contains(@class,'nav__button-container')]/a[@href='/suspensions']");

            if (ElementVisible(loginButtonSelector) || !ElementVisible(pointsSelector))
            {
                IsLoggedIn = false;
                return;
            }

            IsLoggedIn = true;

            CurrentPoints = GetCurrentPoints();
            CurrentRewards = GetCurrentRewards();

            //Log.Info($"Current points: {CurrentPoints}");
            //Log.Info($"Current rewards: {CurrentRewards}");

            if (ElementVisible(suspensionsButtonSelector))
            {
                IsSuspended = true;
            }
        }

        public override void LogIn()
        {
            Log.Info($"Logging in {account.Username} on SteamGifts");
            GoToUrl(LogInUrl);

            Click(By.XPath(@"/html/body/header/nav/div[2]/div/a"));
            Click(By.Id("imageLogin"));

            WaitUntilElementExists(By.ClassName("nav__left-container"), false);

            if (ElementVisible(By.Name("username")))
            {
                Register();
            }
        }

        protected override Giveaway FindGiveaway()
        {
            //Log.Info($"Finding a giveaway to join");
            GoToUrl(HomePageUrl);

            for (int i = 1; i <= 50; i++)
            {
                By giveawayTitleSelector = By.XPath($"//div[@class='widget-container']/div[2]/div[3]/div[{i}]/div/div/h2/a[@class='giveaway__heading__name']");
                By giveawayEndTimeSelector = By.XPath($"//div[@class='widget-container']/div[2]/div[3]/div[{i}]/div/div/div[1]/div[1]/span");

                string giveawayLink = GetHyperlink(giveawayTitleSelector);
                string giveawayEndTimeTimestamp = GetAttribute(giveawayEndTimeSelector, "data-timestamp");

                Giveaway giveaway = new Giveaway(giveawayLink);
                giveaway.EndTime = DateTimeUtils.FromUnixTime(giveawayEndTimeTimestamp);

                if (lastProcessedGiveaway != giveaway)
                {
                    return giveaway;
                }
            }

            return null;
        }

        protected override void ProcessGiveaway(Giveaway giveaway)
        {
            //Log.Info($"Processing giveaway '{giveaway.GiveawayCode}'");
            GoToUrl(giveaway.Url);

            By gameTitleSelector = By.ClassName("featured__heading__medium");
            By priceSelector = By.ClassName("featured__heading__small");
            By errorSelector = By.XPath(@"//div[contains(@class,'sidebar__error') or contains(@class,'sidebar__entry-delete')]");
            By enterButtonSelector = By.ClassName("sidebar__entry-insert");

            string gameTitle = GetText(gameTitleSelector);
            string priceString = GetElements(priceSelector).Last().Text;
            string priceSubstring = priceString.Substring(1).Substring(0, priceString.Length - 3);

            int price = int.Parse(priceSubstring);

            //Log.Info($"Giveaway info retrieved for '{giveaway.GiveawayCode}': \"{gameTitle}\" ({price}P), ends at {endTime}");

            NextCheckTime = giveaway.EndTime;

            if (ElementVisible(errorSelector))
            {
                string errorText = GetText(errorSelector).Trim();;

                if (errorText.Contains("Remove Entry"))
                {
                    errorText = "Already entered";
                }

                Log.Warn($"Cannot enter '{giveaway.GiveawayCode}': \"{errorText}\". Ends at {giveaway.EndTime}");
            }
            else
            {
                Log.Info($"Joining giveawayy '{giveaway.GiveawayCode}'. Ends at {giveaway.EndTime}");
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
                string rewardItemXpath = $"//div[@class='table__row-outer-wrap']/div[{i + 1}]";
                
                By receivedStatusSelector = By.XPath($"{rewardItemXpath}/div[4]/div[contains(@class,'is-hidden')]");
                By keyStatusSelector = By.XPath($"{rewardItemXpath}/div[3]");
                By gameTitleSelector = By.XPath($"{rewardItemXpath}/div[2]/p/a");
                By viewKeyButtonSelector = By.XPath($"{rewardItemXpath}/div[3]/div[@class='view_key_btn']");
                By keySelector = By.XPath($"{rewardItemXpath}/div[3]/div/i[1]");
                By receivedRadioSelector = By.XPath($"{rewardItemXpath}/div[4]/div[1]/i");

                if (ElementVisible(receivedStatusSelector))
                {
                    continue;
                }

                if (ElementVisible(viewKeyButtonSelector))
                {
                    Click(viewKeyButtonSelector);
                    WaitUntilElementExists(keySelector, false);
                }

                if (!ElementVisible(keySelector))
                {
                    string keyStatus = GetText(keyStatusSelector).Trim();

                    if (keyStatus.Length < 6)
                    {
                        continue;
                    }
                }

                string gameTitle = GetText(gameTitleSelector);
                string giveawayLink = GetHyperlink(gameTitleSelector);
                string gameKey = GetAttribute(keySelector, "data-clipboard-text");

                MailNotifier.SendMail(account.Username, giveawayLink, gameTitle, gameKey);
                Click(receivedRadioSelector);
            }
        }

        void Register()
        {
            Log.Info($"Registering user '{account.Username}' on SteamGifts");

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
