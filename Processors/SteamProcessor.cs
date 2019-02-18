using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using NuciWeb.Processors;
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
            logger.Info($"Logging in {account.Username} on Steam");
            GoToUrl(LoginUrl);

            By usernameSelector = By.Id("input_username");
            By passwordSelector = By.Id("input_password");
            By avatarSelector = By.XPath(@"//a[contains(@class,'playerAvatar')]");

            SetText(usernameSelector, account.Username);
            SetText(passwordSelector, account.Password);
            
            Click(By.XPath(@"//*[@id='login_btn_signin']/button"));
            
            WaitForElementToExist(avatarSelector);
        }

        public void VisitChat()
        {
            GoToUrl(ChatUrl);

            By avatarSelector = By.ClassName("currentUserAvatar");
            WaitForElementToExist(avatarSelector);
        }

        public void GatherProfileInfo()
        {
            GoToUrl(AccountUrl);

            By emailAddressSelector = By.XPath(@"//*[@id='main_content']/div[2]/div[4]/div[1]/div[2]/div[1]/span[2]");
            By displayNameSelector = By.ClassName("actual_persona_name");
            By realNameSelector = By.XPath("//div[contains(@class,'header_real_name')]/bdi");

            account.EmailAddress = GetText(emailAddressSelector);

            GoToUrl(account.SteamProfileUrl);
            
            account.DisplayName = GetText(displayNameSelector);
            account.FirstName = GetText(realNameSelector);
            account.Country = GetCountry();

            if (account.FirstName.Contains(' '))
            {
                string[] splitName = account.FirstName.Split(' ');
                
                account.FirstName = splitName[0];
                account.LastName = splitName[1];
            }
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
            List<IWebElement> gameRows = GetElements(gameRowSelector);

            List<uint> appIds = new List<uint>();

            foreach (IWebElement element in gameRows)
            {
                string elementId = element.GetAttribute("id");
                string appIdText = elementId.Substring(5);
                uint appId = uint.Parse(appIdText);

                appIds.Add(appId);
            }

            return appIds;
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
