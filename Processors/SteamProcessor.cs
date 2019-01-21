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
        const string LoginUrl = "https://store.steampowered.com/login/?redir=&redir_ssl=1";
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
            GoToUrl(LoginUrl);

            SendKeys(By.Id("input_username"), account.Username);
            SendKeys(By.Id("input_password"), account.Password);

            Wait(DefaultWait);
            
            Click(By.XPath(@"//*[@id='login_btn_signin']/button"));
        }

        public void GatherProfileInfo()
        {
            GoToUrl(AccountUrl);

            account.EmailAddress = GetElement(By.XPath(@"//*[@id='main_content']/div[2]/div[4]/div[1]/div[2]/div[1]/span[2]")).Text;

            Click(By.XPath(@"//*[@id='global_actions']/a"));
            Click(By.XPath(@"//div[@class='profile_header_actions']/a"));
            
            account.DisplayName = GetElement(By.Id("personaName")).Text;
            account.FirstName = GetElement(By.Id("real_name")).Text;
            account.Country = Country.FromSteamCode(GetSelectValue(By.Id("country")));
        }
    }
}
