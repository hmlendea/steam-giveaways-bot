using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

using NuciLog.Core;
using NuciWeb;

using OpenQA.Selenium;

using SteamGiveawaysBot.Entities;
using SteamGiveawaysBot.Exceptions;
using SteamGiveawaysBot.Logging;
using SteamGiveawaysBot.Service;
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
            IWebDriver driver,
            AccountDetails account,
            IRewardSender rewardSender,
            ILogger logger)
            : base(driver, account, logger)
        {
            MinimumPoints = 20;
            MaximumPoints = 400;

            this.rewardSender = rewardSender;
        }

        public override void Register()
        {
            GoToUrl(RegistrationUrl);

            By signInButton1Selector = By.XPath(@"//div[@class='nav__right-container']/div/a");
            By signInButton2Selector = By.Id("imageLogin");
            By registerButtonSelector = By.XPath(@"//div[contains(@class, 'form__submit-button')]");
            By privacyScreenshotSelector = By.ClassName("form__screenshot");

            By usernameTextBoxSelector = By.XPath(@"//form/div[1]/div[2]/input");
            By emailTextBoxSelector = By.XPath(@"//form/div[2]/div[2]/input");
            By countryDropDownSelector = By.XPath(@"//select[@name='country']");
            By noEmailsRadioButton1Selector = By.XPath(@"//form/div[4]/div[2]/div[1]/div[2]/i[1]");
            By noEmailsRadioButton2Selector = By.XPath(@"//form/div[5]/div[2]/div[1]/div[2]/i[1]");

            if (!IsElementVisible(usernameTextBoxSelector))
            {
                Click(signInButton1Selector);
                Click(signInButton2Selector);
            }
            
            if (IsElementVisible(privacyScreenshotSelector))
            {
                throw new AccountRegistrationException("Cannot register on SteamGifts due to invalid Steam privacy settings");
            }

            SetText(usernameTextBoxSelector, account.Username);
            SetText(emailTextBoxSelector, account.EmailAddress);
            SelectOptionByValue(countryDropDownSelector, account.Country.SgCode.ToString());
            
            Click(noEmailsRadioButton1Selector);
            Click(noEmailsRadioButton2Selector);

            Click(registerButtonSelector);

            account.IsRegisteredSteamGifts = true;
        }

        public override void LogIn()
        {
            logger.Info(
                MyOperation.SteamGiftsLogIn,
                OperationStatus.Started,
                new LogInfo(MyLogInfoKey.Username,  account.Username));

            GoToUrl(LogInUrl);

            By privacyScreenshotSelector = By.ClassName("form__screenshot");
            By usernameTextBoxSelector = By.XPath(@"//form/div[1]/div[2]/input");

            Click(By.XPath(@"/html/body/header/nav/div[2]/div/a"));
            Click(By.Id("imageLogin"));

            WaitForElementToExist(By.ClassName("nav__left-container"), false);

            if (DoesAnyElementExist(privacyScreenshotSelector, usernameTextBoxSelector))
            {
                IsRegistered = false;
                IsLoggedIn = false;

                if (IsElementVisible(privacyScreenshotSelector))
                {
                    Wait(TimeSpan.FromSeconds(30));
                }

                throw new AccountNotRegisteredException(account.Username);
            }

            IsLoggedIn = true;

            logger.Debug(MyOperation.SteamGiftsLogIn, OperationStatus.Success);
        }

        public void ApplyCustomSettings()
        {
            GoToUrl(SettingsUrl);

            By hideHighLevelGiveawaysSelector = By.XPath(@"/html/body/div[1]/div/div/div[2]/form/div/div[4]/div[2]/div/div[1]/i[1]");
            By saveButtonSelector = By.XPath(@"//div[contains(@class,'form__submit-button')]");

            UpdateCheckbox(hideHighLevelGiveawaysSelector, true);

            Click(saveButtonSelector);
        }

        protected override void GatherInfo()
        {
            GoToUrl(HomePageUrl);

            By loginButtonSelector = By.ClassName("nav__sits");
            By pointsSelector = By.ClassName("nav__points");
            By suspensionsButtonSelector = By.XPath(@"//div[contains(@class,'nav__button-container')]/a[@href='/suspensions']");

            WaitForAnyElementToExist(loginButtonSelector, pointsSelector);

            if (DoesElementExist(loginButtonSelector))
            {
                IsLoggedIn = false;
                return;
            }

            IsLoggedIn = true;

            CurrentPoints = GatherCurrentPoints();
            CurrentRewards = GatherCurrentRewards();

            if (DoesElementExist(suspensionsButtonSelector))
            {
                IsSuspended = true;
            }
        }

        protected override Giveaway FindGiveaway()
        {
            GoToUrl(HomePageUrl);

            for (int i = 1; i <= 50; i++)
            {
                string baseXpath = "//div[@class='page__inner-wrap']/div[@class='widget-container']/div[2]";
                int baseXpathFindingsCount = GetElements(By.XPath($"{baseXpath}/div")).Count;

                // TODO: A more proper fix
                if (baseXpathFindingsCount == 7)
                {
                    baseXpath += "/div[2]";
                }
                else
                {
                    baseXpath += "/div[3]";
                }

                By giveawayTitleSelector = By.XPath($"{baseXpath}/div[{i}]/div/div/h2/a[@class='giveaway__heading__name']");
                By giveawayEndTimeSelector = By.XPath($"{baseXpath}/div[{i}]/div/div/div[1]/div[1]/span");
                By giveawayPriceSelector = By.XPath($"{baseXpath}/div[{i}]/div/div/h2/span");

                string giveawayLink = GetHyperlink(giveawayTitleSelector);
                string endTimeSelector = GetAttribute(giveawayEndTimeSelector, "data-timestamp");
                string gameTitle = GetText(giveawayTitleSelector);
                string priceString = GetTextOfMany(giveawayPriceSelector).Last();
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
            GoToUrl(giveaway.Url);

            By enterButtonSelector = By.ClassName("sidebar__entry-insert");
            By errorSelector = By.XPath(@"//div[contains(@class,'sidebar__error') or contains(@class,'sidebar__entry-delete')]");

            WaitForAnyElementToBeVisible(TimeSpan.FromSeconds(3), enterButtonSelector, errorSelector);

            if (!IsElementVisible(enterButtonSelector))
            {
                string errorText = "Giveaway ended";
                
                if (IsElementVisible(errorSelector))
                {
                    errorText = GetText(errorSelector).Trim();

                    if (errorText.Contains("Remove Entry"))
                    {
                        errorText = "Already entered";
                    }
                }

                throw new UnjoinableGiveawayException(giveaway, errorText);
            }

            Click(enterButtonSelector);
        }

        protected override void ProcessRewards()
        {
            GoToUrl(RewardsUrl);

            By rewardItemSelector = By.ClassName("table__row-inner-wrap");

            List<IWebElement> rewardItems = GetElements(rewardItemSelector);

            for (int i = 0; i < rewardItems.Count; i++)
            {
                string rewardItemXpath = $"//div[@class='table__rows']/div[{i + 1}]/div[1]";
                
                By keyStatusSelector = By.XPath($"{rewardItemXpath}/div[contains(@class,'table__column--width-medium')]");
                By gameTitleSelector = By.XPath($"{rewardItemXpath}/div[2]/p/a");
                By gameThumbnailSelector = By.XPath($"{rewardItemXpath}/div[1]/a");
                By viewKeyButtonSelector = By.XPath($"{rewardItemXpath}/div[3]/div[@class='view_key_btn']");
                By keyClipboardSelector = By.XPath($"{rewardItemXpath}/div[3]/div/i[1]");
                By receivedRadioSelector = By.XPath($"{rewardItemXpath}/div[4]/div[1]/i");

                if (DoesElementExist(viewKeyButtonSelector))
                {
                    Click(viewKeyButtonSelector);
                    WaitForElementToExist(keyClipboardSelector);
                }

                if (!DoesElementExist(keyClipboardSelector))
                {
                    string keyStatus = GetText(keyStatusSelector).Trim();

                    if (keyStatus.Length == 1)
                    {
                        continue;
                    }
                }

                const string appIdPattern = ".*apps\\/([0-9]*)\\/.*";
                string thumbnailStyle = GetAttribute(gameThumbnailSelector, "style");

                string giveawayUrl = GetHyperlink(gameTitleSelector);
                string[] giveawayUrlFields = giveawayUrl.Split('/');

                Reward reward = new Reward();
                reward.GiveawaysProvider = ProviderName;
                reward.GiveawayId  = giveawayUrlFields[giveawayUrlFields.Length - 2];
                reward.WinnerUsername = account.Username;
                reward.SteamAppId = Regex.Match(thumbnailStyle, appIdPattern).Groups[1].Value;
                reward.ActivationKey = GetAttribute(keyClipboardSelector, "data-clipboard-text");

                rewardSender.RecordReward(reward);
                Click(receivedRadioSelector);
            }
        }

        int GatherCurrentPoints()
        {
            By pointsSelector = By.ClassName("nav__points");
            string pointsText = GetText(pointsSelector);

            if (string.IsNullOrWhiteSpace(pointsText))
            {
                throw new FormatException("Cannot parse the current points count");
            }

            return int.Parse(pointsText);
        }

        int GatherCurrentRewards()
        {
            By rewardsSelector = By.XPath(@"/html/body/header/nav/div[2]/div[2]/a");
            string rewardsText = GetText(rewardsSelector);

            if (string.IsNullOrWhiteSpace(rewardsText))
            {
                return 0;
            }

            return int.Parse(rewardsText);
        }
    }
}
