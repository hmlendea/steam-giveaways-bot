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

namespace SteamGiveawaysBot.Processors
{
    public sealed class SteamProcessor : WebProcessor
    {
        const string LoginUrl = "https://store.steampowered.com/login/?redir=account%2F&redir_ssl=1";
        const string AccountUrl = "https://store.steampowered.com/account/";

        AccountDetails account;

        string ProfileUrl => $"https://steamcommunity.com/id/{account.Username}";
        string EditProfileUrl => $"https://steamcommunity.com/id/{account.Username}/edit";
        string PrivacySettingsUrl => $"{ProfileUrl}/edit/settings";
        string BadgesUrl => $"{ProfileUrl}/badges";

        public SteamProcessor(IWebDriver driver, AccountDetails account)
            : base(driver)
        {
            this.account = account;
        }

        public void LogIn()
        {
            Log.Info($"Logging in {account.Username} on Steam");
            GoToUrl(LoginUrl);

            By avatarSelector = By.XPath(@"//a[contains(@class,'playerAvatar')]");

            SetText(By.Id("input_username"), account.Username);
            SetText(By.Id("input_password"), account.Password);
            
            Click(By.XPath(@"//*[@id='login_btn_signin']/button"));
            
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
