using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using NuciExtensions;
using NuciLog.Core;
using NuciWeb;
using NuciWeb.Automation;
using NuciWeb.Steam.Models;

using SteamGiveawaysBot.Client;
using SteamGiveawaysBot.Entities;
using SteamGiveawaysBot.Exceptions;
using SteamGiveawaysBot.Logging;

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

        readonly IRewardSender rewardSender;

        public SteamGiftsProcessor(
            IWebProcessor webProcessor,
            IRewardSender rewardSender,
            SteamAccount steamAccount,
            ILogger logger)
            : base(webProcessor, steamAccount, logger)
        {
            MinimumPoints = 20;
            MaximumPoints = 400;

            this.rewardSender = rewardSender;
            this.webProcessor.RetryOnDomFailure = true;
        }

        public override void LogIn()
        {
            IEnumerable<LogInfo> logInfos =
            [
                new(MyLogInfoKey.Username, steamAccount.Username)
            ];

            logger.Info(
                MyOperation.SteamGiftsLogIn,
                OperationStatus.Started,
                logInfos);

            webProcessor.GoToUrl(LogInUrl);

            string privacyScreenshotSelector = Select.ByClass("form__screenshot");
            string usernameTextBoxSelector = Select.ByXPath(@"//form/div[1]/div[2]/input");
            string pointsSelector = Select.ByClass("nav__points");

            if (webProcessor.IsElementVisible(pointsSelector))
            {
                IsLoggedIn = true;
                logger.Info(
                    MyOperation.SteamGiftsLogIn,
                    OperationStatus.Success,
                    "Already logged in");

                return;
            }

            AcceptCookies();

            webProcessor.Click(Select.ByXPath(@"/html/body/header/nav/div[2]/div/a"));
            webProcessor.Click(Select.ById("imageLogin"));

            webProcessor.WaitForElementToExist(Select.ByClass("nav__left-container"));

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

                throw new AccountNotRegisteredException(steamAccount.Username);
            }

            IsLoggedIn = true;

            logger.Info(
                MyOperation.SteamGiftsLogIn,
                OperationStatus.Success,
                logInfos);
        }

        protected override void GatherInfo()
        {
            webProcessor.GoToUrl(HomePageUrl);

            string loginButtonSelector = Select.ByClass("nav__sits");
            string pointsSelector = Select.ByClass("nav__points");
            string suspensionsButtonSelector = Select.ByXPath(@"//div[contains(@class,'nav__button-container')]/a[@href='/suspensions']");

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

                string giveawayTitleSelector = Select.ByXPath($"{baseXpath}/div[{i}]/div/div/h2/a[@class='giveaway__heading__name']");
                string giveawayEndTimeSelector = Select.ByXPath($"{baseXpath}/div[{i}]/div/div/div[1]/div/span");
                string giveawayPriceSelector = Select.ByXPath($"{baseXpath}/div[{i}]/div/div/h2/span[last()]");

                string giveawayLink = webProcessor.GetHyperlink(giveawayTitleSelector);
                string endTimeSelector = webProcessor.GetAttribute(giveawayEndTimeSelector, "data-timestamp");
                string gameTitle = webProcessor.GetText(giveawayTitleSelector);
                string priceString = webProcessor.GetTextOfMany(giveawayPriceSelector).Last();
                string priceSubstring = priceString[1..][..(priceString.Length - 3)];

                int price = int.Parse(priceSubstring);
                DateTime endTime = DateTimeExtensions.FromUnixTime(endTimeSelector).ToLocalTime();

                Giveaway giveaway = new(ProviderName, giveawayLink, gameTitle, price, endTime);

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

            string enterButtonSelector = Select.ByClass("sidebar__entry-insert");
            string errorDivSelector = Select.ByXPath(@"//div[contains(@class,'sidebar__error') or contains(@class,'sidebar__entry-delete')]");
            string errorASelector = Select.ByXPath(@"//a[contains(@class,'sidebar__error') or contains(@class,'sidebar__entry-delete')]");

            webProcessor.WaitForAnyElementToBeVisible(enterButtonSelector, errorDivSelector);

            if (!webProcessor.IsElementVisible(enterButtonSelector))
            {
                string errorText = "Giveaway ended";

                if (webProcessor.IsElementVisible(errorDivSelector))
                {
                    errorText = webProcessor.GetText(errorDivSelector).Trim();

                    if (errorText.Contains("Remove Entry"))
                    {
                        errorText = "Already entered";
                    }
                }
                else if (webProcessor.IsElementVisible(errorASelector))
                {
                    errorText = webProcessor.GetText(errorASelector).Trim();

                    if (errorText.Contains("Sync Required"))
                    {
                        errorText = "Steam profile not public";
                    }
                }

                throw new UnjoinableGiveawayException(giveaway, errorText);
            }

            webProcessor.Click(enterButtonSelector);
        }

        protected override void ProcessRewards()
        {
            webProcessor.GoToUrl(RewardsUrl);

            for (int i = 0; i < 10; i++)
            {
                string rewardItemXpath = $"//div[@class='table__rows']/div[{i + 1}]/div[1]";

                if (!webProcessor.DoesElementExist(Select.ByXPath(rewardItemXpath)))
                {
                    break;
                }

                string keyStatusSelector = Select.ByXPath($"{rewardItemXpath}/div[contains(@class,'table__column--width-medium')]");
                string gameTitleSelector = Select.ByXPath($"{rewardItemXpath}/div[2]/p/a");
                string gameThumbnailSelector = Select.ByXPath($"{rewardItemXpath}/div[1]/a");
                string viewKeyButtonSelector = Select.ByXPath($"{rewardItemXpath}/div[3]/div[@class='view_key_btn']");
                string keyClipboardSelector = Select.ByXPath($"{rewardItemXpath}/div[3]/div/i[1]");
                string receivedRadioSelector = Select.ByXPath($"{rewardItemXpath}/div[4]/div[1]/i");

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

                rewardSender.RecordReward(new()
                {
                    GiveawaysProvider = ProviderName,
                    GiveawayId = webProcessor.GetHyperlink(gameTitleSelector).Split('/')[^2],
                    WinnerUsername = steamAccount.Username,
                    SteamAppId = Regex.Match(webProcessor.GetAttribute(gameThumbnailSelector, "style"), ".*apps\\/([0-9]*)\\/.*").Groups[1].Value,
                    ActivationKey = webProcessor.GetAttribute(keyClipboardSelector, "data-clipboard-text")
                });

                if (webProcessor.IsElementVisible(receivedRadioSelector))
                {
                    webProcessor.Click(receivedRadioSelector);
                }
            }
        }

        void AcceptCookies()
        {
            string cookieOverlaySelector = Select.ByXPath(@"//div[contains(@class,'qc-cmp-cleanslate')]");
            string moreOptionsButtonSelector = Select.ByXPath(@"//div[contains(@class,'qc-cmp2-summary-buttons')]/button[1]");
            string agreeSelectedButtonSelector = Select.ByXPath(@"//div[contains(@class,'qc-cmp2-buttons-desktop')]/button[1]");

            webProcessor.WaitForAnyElementToBeVisible(
                cookieOverlaySelector,
                moreOptionsButtonSelector);

            logger.Info(
                MyOperation.SteamGiftsLogIn,
                OperationStatus.InProgress,
                "Accepting the cookies");

            if (webProcessor.IsElementVisible(moreOptionsButtonSelector))
            {
                webProcessor.Click(moreOptionsButtonSelector);
                webProcessor.Click(agreeSelectedButtonSelector);
            }
        }

        int GatherCurrentPoints()
        {
            string pointsText = webProcessor.GetText(Select.ByClass("nav__points"));

            if (string.IsNullOrWhiteSpace(pointsText))
            {
                throw new FormatException("Cannot parse the current points count");
            }

            return int.Parse(pointsText);
        }

        int GatherCurrentRewards()
        {
            string rewardsText = webProcessor.GetText(Select.ByXPath(@"/html/body/header/nav/div[2]/div[2]/a"));

            if (string.IsNullOrWhiteSpace(rewardsText))
            {
                return 0;
            }

            return int.Parse(rewardsText);
        }
    }
}
