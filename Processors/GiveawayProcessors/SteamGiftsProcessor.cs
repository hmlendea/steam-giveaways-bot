using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

using SteamGiveawaysBot.Entities;
using SteamGiveawaysBot.Exceptions;
using SteamGiveawaysBot.Utils;

namespace SteamGiveawaysBot.Processors.GiveawayProcessors
{
    public sealed class SteamGiftsProcessor : GiveawayProcessor
    {
        public override string HomePageUrl => "https://www.steamgifts.com";

        public override string RegistrationUrl => HomePageUrl;

        public override string LogInUrl => HomePageUrl;

        public override string RewardsUrl => $"{HomePageUrl}/giveaways/won";

        public override string SettingsUrl => $"{HomePageUrl}/account/settings/giveaways";

        public SteamGiftsProcessor(IWebDriver driver, AccountDetails account)
            : base(driver, account)
        {
            MinimumPoints = 20;
            MaximumPoints = 400;
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
            SelectByValue(countryDropDownSelector, account.Country.SgCode.ToString());
            
            Click(noEmailsRadioButton1Selector);
            Click(noEmailsRadioButton2Selector);

            Click(registerButtonSelector);

            account.IsRegisteredSteamGifts = true;
        }

        public override void LogIn()
        {
            Log.Info($"Logging in {account.Username} on SteamGifts");
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
                By giveawayTitleSelector = By.XPath($"//div[@class='widget-container']/div[2]/div[3]/div[{i}]/div/div/h2/a[@class='giveaway__heading__name']");
                By giveawayEndTimeSelector = By.XPath($"//div[@class='widget-container']/div[2]/div[3]/div[{i}]/div/div/div[1]/div[1]/span");
                By giveawayPriceSelector = By.XPath($"//div[@class='widget-container']/div[2]/div[3]/div[{i}]/div/div/h2/span");

                string giveawayLink = GetHyperlink(giveawayTitleSelector);
                string giveawayEndTimeTimestamp = GetAttribute(giveawayEndTimeSelector, "data-timestamp");
                string priceString = GetElements(giveawayPriceSelector).Last().Text;
                string priceSubstring = priceString.Substring(1).Substring(0, priceString.Length - 3);

                int price = int.Parse(priceSubstring);
                DateTime endTime = DateTimeUtils.FromUnixTime(giveawayEndTimeTimestamp);

                Giveaway giveaway = new Giveaway(giveawayLink, price, endTime);

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

            By errorSelector = By.XPath(@"//div[contains(@class,'sidebar__error') or contains(@class,'sidebar__entry-delete')]");
            By enterButtonSelector = By.ClassName("sidebar__entry-insert");

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

                throw new UnjoinableGiveawayException(giveaway.GiveawayCode, errorText);
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
                
                By receivedStatusSelector = By.XPath($"{rewardItemXpath}/div[4]/div[contains(@class,'table__gift-feedback-awaiting-reply') and contains(@class,'is-hidden')]");
                By keyStatusSelector = By.XPath($"{rewardItemXpath}/div[contains(@class,'table__column--width-medium')]");
                By gameTitleSelector = By.XPath($"{rewardItemXpath}/div[2]/p/a");
                By viewKeyButtonSelector = By.XPath($"{rewardItemXpath}/div[3]/div[@class='view_key_btn']");
                By keyClipboardSelector = By.XPath($"{rewardItemXpath}/div[3]/div/i[1]");
                By receivedRadioSelector = By.XPath($"{rewardItemXpath}/div[4]/div[1]/i");

                if (DoesElementExist(receivedStatusSelector))
                {
                    continue;
                }

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

                string gameTitle = GetText(gameTitleSelector);
                string giveawayLink = GetHyperlink(gameTitleSelector);
                string gameKey = GetAttribute(keyClipboardSelector, "data-clipboard-text");

                MailNotifier.SendMail(account.Username, giveawayLink, gameTitle, gameKey);
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
