using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using NuciLog.Core;
using NuciWeb;

using OpenQA.Selenium;

using SteamGiveawaysBot.Client;
using SteamGiveawaysBot.Entities;
using SteamGiveawaysBot.Exceptions;
using SteamGiveawaysBot.Logging;
using SteamGiveawaysBot.Utils;

namespace SteamGiveawaysBot.Processors.GiveawayProcessors
{
    public sealed class SteamGiftsProcessor : GiveawayProcessor
    {
        public override string ProviderName => "SteamGifts";

        public override string HomePageUrl => "https://www.steamgifts.com";

        public override string RegistrationUrl => HomePageUrl;

        public override string LogInUrl => HomePageUrl;

        public override string RewardsUrl => $"{HomePageUrl}/giveaways/won";

        public override string SettingsUrl => $"{HomePageUrl}/account/settings/giveaways";

        IRewardSender rewardSender;

        public SteamGiftsProcessor(
            IWebProcessor webProcessor,
            IRewardSender rewardSender,
            AccountDetails accountDetails,
            ILogger logger)
            : base(webProcessor, accountDetails, logger)
        {
            MinimumPoints = 20;
            MaximumPoints = 400;

            this.rewardSender = rewardSender;
        }

        public override void LogIn()
        {
            logger.Info(
                MyOperation.SteamGiftsLogIn,
                OperationStatus.Started,
                new LogInfo(MyLogInfoKey.Username,  accountDetails.Username));

            webProcessor.GoToUrl(LogInUrl);

            By privacyScreenshotSelector = By.ClassName("form__screenshot");
            By usernameTextBoxSelector = By.XPath(@"//form/div[1]/div[2]/input");
            By pointsSelector = By.ClassName("nav__points");

            if (webProcessor.IsElementVisible(pointsSelector))
            {
                IsLoggedIn = true;
                logger.Debug(
                    MyOperation.SteamGiftsLogIn,
                    OperationStatus.Success,
                    "Already logged in");

                return;
            }

            AcceptCookies();

            webProcessor.Click(By.XPath(@"/html/body/header/nav/div[2]/div/a"));
            webProcessor.Click(By.Id("imageLogin"));

            webProcessor.WaitForElementToExist(By.ClassName("nav__left-container"), false);

            if (webProcessor.DoesAnyElementExist(
                privacyScreenshotSelector,
                usernameTextBoxSelector))
            {
                IsRegistered = false;
                IsLoggedIn = false;

                if (webProcessor.IsElementVisible(privacyScreenshotSelector))
                {
                    webProcessor.Wait(TimeSpan.FromSeconds(30));
                }

                throw new AccountNotRegisteredException(accountDetails.Username);
            }

            IsLoggedIn = true;

            logger.Debug(MyOperation.SteamGiftsLogIn, OperationStatus.Success);
        }

        protected override void GatherInfo()
        {
            webProcessor.GoToUrl(HomePageUrl);

            By loginButtonSelector = By.ClassName("nav__sits");
            By pointsSelector = By.ClassName("nav__points");
            By suspensionsButtonSelector = By.XPath(@"//div[contains(@class,'nav__button-container')]/a[@href='/suspensions']");

            webProcessor.WaitForAnyElementToExist(loginButtonSelector, pointsSelector);

            if (webProcessor.DoesElementExist(loginButtonSelector))
            {
                IsLoggedIn = false;
                return;
            }

            IsLoggedIn = true;

            CurrentPoints = GatherCurrentPoints();
            CurrentRewards = GatherCurrentRewards();

            if (webProcessor.DoesElementExist(suspensionsButtonSelector))
            {
                IsSuspended = true;
            }
        }

        protected override Giveaway FindGiveaway()
        {
            webProcessor.GoToUrl(HomePageUrl);

            for (int i = 1; i <= 50; i++)
            {
                string baseXpath = "//div[@class='page__heading']/following-sibling::div/div[@class='giveaway__row-outer-wrap']/..";
                   
                By giveawayTitleSelector = By.XPath($"{baseXpath}/div[{i}]/div/div/h2/a[@class='giveaway__heading__name']");
                By giveawayEndTimeSelector = By.XPath($"{baseXpath}/div[{i}]/div/div/div[1]/div[1]/span");
                By giveawayPriceSelector = By.XPath($"{baseXpath}/div[{i}]/div/div/h2/span");

                string giveawayLink = webProcessor.GetHyperlink(giveawayTitleSelector);
                string endTimeSelector = webProcessor.GetAttribute(giveawayEndTimeSelector, "data-timestamp");
                string gameTitle = webProcessor.GetText(giveawayTitleSelector);
                string priceString = webProcessor.GetTextOfMany(giveawayPriceSelector).Last();
                string priceSubstring = priceString.Substring(1).Substring(0, priceString.Length - 3);

                int price = int.Parse(priceSubstring);
                DateTime endTime = DateTimeUtils.FromUnixTime(endTimeSelector);

                Giveaway giveaway = new Giveaway(ProviderName, giveawayLink, gameTitle, price, endTime);

                if (LastProcessedGiveaway != giveaway)
                {
                    return giveaway;
                }
            }

            return null;
        }

        protected override void JoinGiveaway(Giveaway giveaway)
        {
            webProcessor.GoToUrl(giveaway.Url);

            By enterButtonSelector = By.ClassName("sidebar__entry-insert");
            By errorSelector = By.XPath(@"//div[contains(@class,'sidebar__error') or contains(@class,'sidebar__entry-delete')]");

            webProcessor.WaitForAnyElementToBeVisible(TimeSpan.FromSeconds(3), enterButtonSelector, errorSelector);

            if (!webProcessor.IsElementVisible(enterButtonSelector))
            {
                string errorText = "Giveaway ended";
                
                if (webProcessor.IsElementVisible(errorSelector))
                {
                    errorText = webProcessor.GetText(errorSelector).Trim();

                    if (errorText.Contains("Remove Entry"))
                    {
                        errorText = "Already entered";
                    }
                }

                throw new UnjoinableGiveawayException(giveaway, errorText);
            }

            webProcessor.Click(enterButtonSelector);
        }

        protected override void ProcessRewards()
        {
            webProcessor.GoToUrl(RewardsUrl);

            By rewardItemSelector = By.ClassName("table__row-inner-wrap");

            IEnumerable<IWebElement> rewardItems = webProcessor.GetElements(rewardItemSelector);

            for (int i = 0; i < rewardItems.Count(); i++)
            {
                string rewardItemXpath = $"//div[@class='table__rows']/div[{i + 1}]/div[1]";
                
                By keyStatusSelector = By.XPath($"{rewardItemXpath}/div[contains(@class,'table__column--width-medium')]");
                By gameTitleSelector = By.XPath($"{rewardItemXpath}/div[2]/p/a");
                By gameThumbnailSelector = By.XPath($"{rewardItemXpath}/div[1]/a");
                By viewKeyButtonSelector = By.XPath($"{rewardItemXpath}/div[3]/div[@class='view_key_btn']");
                By keyClipboardSelector = By.XPath($"{rewardItemXpath}/div[3]/div/i[1]");
                By receivedRadioSelector = By.XPath($"{rewardItemXpath}/div[4]/div[1]/i");

                if (webProcessor.DoesElementExist(viewKeyButtonSelector))
                {
                    webProcessor.Click(viewKeyButtonSelector);
                    webProcessor.WaitForElementToExist(keyClipboardSelector);
                }

                if (!webProcessor.DoesElementExist(keyClipboardSelector))
                {
                    string keyStatus = webProcessor.GetText(keyStatusSelector).Trim();

                    if (keyStatus.Length <= 3)
                    {
                        continue;
                    }
                }

                const string appIdPattern = ".*apps\\/([0-9]*)\\/.*";
                string thumbnailStyle = webProcessor.GetAttribute(gameThumbnailSelector, "style");

                string giveawayUrl = webProcessor.GetHyperlink(gameTitleSelector);
                string[] giveawayUrlFields = giveawayUrl.Split('/');

                Reward reward = new Reward();
                reward.GiveawaysProvider = ProviderName;
                reward.GiveawayId  = giveawayUrlFields[giveawayUrlFields.Length - 2];
                reward.WinnerUsername = accountDetails.Username;
                reward.SteamAppId = Regex.Match(thumbnailStyle, appIdPattern).Groups[1].Value;
                reward.ActivationKey = webProcessor.GetAttribute(keyClipboardSelector, "data-clipboard-text");

                rewardSender.RecordReward(reward);

                if (webProcessor.IsElementVisible(receivedRadioSelector))
                {
                    webProcessor.Click(receivedRadioSelector);
                }
            }
        }

        void AcceptCookies()
        {
            By cookieOverlaySelector = By.XPath(@"//div[contains(@class,'qc-cmp-cleanslate')]");
            By moreOptionsButtonSelector = By.XPath(@"//div[contains(@class,'qc-cmp2-summary-buttons')]/button[1]");
            By agreeSelectedButtonSelector = By.XPath(@"//div[contains(@class,'qc-cmp2-buttons-desktop')]/button[1]");

            webProcessor.WaitForAnyElementToBeVisible(
                cookieOverlaySelector,
                moreOptionsButtonSelector);
                
            logger.Debug(
                MyOperation.SteamGiftsLogIn,
                OperationStatus.InProgress,
                "Accepting the cookies");

            webProcessor.Click(moreOptionsButtonSelector);
            webProcessor.Click(agreeSelectedButtonSelector);
        }

        int GatherCurrentPoints()
        {
            By pointsSelector = By.ClassName("nav__points");
            string pointsText = webProcessor.GetText(pointsSelector);

            if (string.IsNullOrWhiteSpace(pointsText))
            {
                throw new FormatException("Cannot parse the current points count");
            }

            return int.Parse(pointsText);
        }

        int GatherCurrentRewards()
        {
            By rewardsSelector = By.XPath(@"/html/body/header/nav/div[2]/div[2]/a");
            string rewardsText = webProcessor.GetText(rewardsSelector);

            if (string.IsNullOrWhiteSpace(rewardsText))
            {
                return 0;
            }

            return int.Parse(rewardsText);
        }
    }
}
