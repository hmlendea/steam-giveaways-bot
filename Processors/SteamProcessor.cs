using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using NuciExtensions;
using NuciLog.Core;
using NuciWeb;

using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

using SteamGiveawaysBot.Entities;
using SteamGiveawaysBot.Logging;
using SteamGiveawaysBot.Utils;

namespace SteamGiveawaysBot.Processors
{
    public sealed class SteamProcessor : WebProcessor
    {
        public string HomePageUrl => "https://store.steampowered.com";
        public string CommunityUrl => "https://steamcommunity.com";
        public string LoginUrl => $"{HomePageUrl}/login/?redir=&redir_ssl=1";
        public string AccountUrl => $"{HomePageUrl}/account/";
        public string ChatUrl => $"{CommunityUrl}/chat/";
        public string LicensesUrl => $"{AccountUrl}/licenses/";

        AccountDetails account;
        ILogger logger;

        string ProfileUrl => $"https://steamcommunity.com/id/{account.Username}";
        string EditProfileUrl => $"https://steamcommunity.com/id/{account.Username}/edit";
        string PrivacySettingsUrl => $"{ProfileUrl}/edit/settings";
        string BadgesUrl => $"{ProfileUrl}/badges";
        string GamesUrl => $"{ProfileUrl}/games/?tab=all";

        public SteamProcessor(
            IWebDriver driver,
            AccountDetails account,
            ILogger logger)
            : base(driver)
        {
            this.account = account;
            this.logger = logger;
        }

        public void LogIn()
        {
            logger.Info(
                MyOperation.SteamLogIn,
                OperationStatus.Started,
                new LogInfo(MyLogInfoKey.Username, account.Username));

            GoToUrl(LoginUrl);

            By usernameSelector = By.Id("input_username");
            By passwordSelector = By.Id("input_password");
            By avatarSelector = By.XPath(@"//a[contains(@class,'playerAvatar')]");

            SetText(usernameSelector, account.Username);
            SetText(passwordSelector, account.Password);
            
            Click(By.XPath(@"//*[@id='login_btn_signin']/button"));
            
            WaitForElementToExist(avatarSelector);

            logger.Debug(
                MyOperation.SteamGiftsLogIn,
                OperationStatus.Success);
        }

        public void VisitChat()
        {
            GoToUrl(ChatUrl);

            By avatarSelector = By.ClassName("currentUserAvatar");
            WaitForElementToExist(avatarSelector);
        }
        
        public void GatherProfileInformation()
        {
            GoToUrl(AccountUrl);

            By emailAddressSelector = By.XPath(@"//*[@id='main_content']/div[2]/div[4]/div[1]/div[2]/div[1]/span[2]");
            By displayNameSelector = By.ClassName("actual_persona_name");
            By realNameSelector = By.XPath("//div[contains(@class,'header_real_name')]/bdi");

            account.EmailAddress = GetText(emailAddressSelector);

            GoToUrl(account.SteamProfileUrl);

            string name = GetText(realNameSelector);;
            
            account.DisplayName = GetText(displayNameSelector);
            account.PersonalName = new PersonalName(name);
            account.Country = GetCountry();
        }

        public void UpdatePrivacySettings()
        {
            GoToUrl(PrivacySettingsUrl);

            By gameDetailsDropDownSelector = By.XPath(@"/html/body/div[1]/div[7]/div[3]/div/div[2]/div/div[1]/div[3]/div/div[6]/div[1]/div");
            By gameDetailsPublicOptionSelector = By.XPath(@"/html/body/div[4]/div/div/div[1]");

            Click(gameDetailsDropDownSelector);
            Click(gameDetailsPublicOptionSelector);
        }

        public IEnumerable<uint> GatherOwnedGames()
        {
            GoToUrl(GamesUrl);

            By gameRowSelector = By.ClassName("gameListRow");
            
            IEnumerable<string> gameRowsIds = GetIdOfMany(gameRowSelector);
            List<uint> appIds = new List<uint>();

            foreach (string gameRowId in gameRowsIds)
            {
                string appIdText = gameRowId.Substring(5);
                uint appId = uint.Parse(appIdText);

                appIds.Add(appId);
            }

            return appIds;
        }

        public void ActivatePackages(IEnumerable<string> subIds)
        {
            const int packageCountToActivate = 50;

            logger.Info(
                MyOperation.PackageActivation,
                OperationStatus.Started);

            int activatedPackagesCount = 0;

            for (int i = 0; i < packageCountToActivate; i++)
            {
                string subId = subIds.GetRandomElement();
                bool wasActivated = ActivatePackage(subId);

                if (!wasActivated)
                {
                    logger.Info(
                        MyOperation.PackageActivation,
                        OperationStatus.Failure,
                        new LogInfo(MyLogInfoKey.SubId, subId));
                    
                    continue;
                }

                activatedPackagesCount += 1;
            }

            logger.Debug(
                MyOperation.PackageActivation,
                OperationStatus.Success,
                new LogInfo(MyLogInfoKey.GamesCount, activatedPackagesCount));
        }

        bool ActivatePackage(string package)
        {
            GoToUrl(LicensesUrl);

            By pageContentSelector = By.ClassName("page_content");
            By lastGameTitleSelector = By.XPath(@"//table[@class='account_table']/tbody/tr[2]/td[2]");

            WaitForElementToExist(pageContentSelector);

            string activationScript = $"jQuery.post('//store.steampowered.com/checkout/addfreelicense',{{action:'add_to_cart',sessionid:g_sessionID,subid:{package}}})";
            string previousGameTitle = string.Empty;
            string currentGameTitle = string.Empty;

            if (DoesElementExist(lastGameTitleSelector))
            {
                previousGameTitle = GetText(lastGameTitleSelector);
            }

            for (int attempt = 0; attempt < 3; attempt++)
            {
                ExecuteScript(activationScript);

                Refresh();
                WaitForElementToExist(pageContentSelector);

                if (DoesElementExist(lastGameTitleSelector))
                {
                    currentGameTitle = GetText(lastGameTitleSelector);
                }

                if (currentGameTitle != previousGameTitle)
                {
                    return true;
                }
            }

            return false;
        }

        Country GetCountry()
        {
            By countryFlagSelector = By.ClassName("profile_flag");

            string countryFlagUrl = GetSource(countryFlagSelector);
            string[] countryFlagUrlFields = countryFlagUrl.Split('/');

            string countryCode = countryFlagUrlFields[countryFlagUrlFields.Length - 1].Substring(0, 2);

            return Country.FromSteamCode(countryCode);
        }
    }
}
