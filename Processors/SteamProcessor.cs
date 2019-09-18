using System;
using System.Collections.Generic;
using System.Linq;

using NuciExtensions;
using NuciLog.Core;
using NuciWeb;

using OpenQA.Selenium;

using SteamGiveawaysBot.Entities;
using SteamGiveawaysBot.Logging;

namespace SteamGiveawaysBot.Processors
{
    public sealed class SteamProcessor : ISteamProcessor
    {
        const int PackagesActivationLimit = 50;

        public string HomePageUrl => "https://store.steampowered.com";
        public string CommunityUrl => "https://steamcommunity.com";
        public string LoginUrl => $"{HomePageUrl}/login/?redir=&redir_ssl=1";
        public string AccountUrl => $"{HomePageUrl}/account/";
        public string ChatUrl => $"{CommunityUrl}/chat/";
        public string LicensesUrl => $"{AccountUrl}/licenses/";
        public string AddFreeLicenseUrlFormat => HomePageUrl + "/checkout/addfreelicense/{0}";

        readonly AccountDetails account;
        readonly ILogger logger;

        readonly WebProcessor webProcessor;

        string ProfileUrl => $"https://steamcommunity.com/id/{account.Username}";
        string EditProfileUrl => $"https://steamcommunity.com/id/{account.Username}/edit";
        string PrivacySettingsUrl => $"{ProfileUrl}/edit/settings";
        string BadgesUrl => $"{ProfileUrl}/badges";
        string GamesUrl => $"{ProfileUrl}/games/?tab=all";

        public SteamProcessor(
            IWebDriver driver,
            AccountDetails account,
            ILogger logger)
        {
            this.account = account;
            this.logger = logger;

            webProcessor = new WebProcessor(driver);
        }

        public void Quit()
        {
            webProcessor.Dispose();
        }

        public void LogIn()
        {
            logger.Info(
                MyOperation.SteamLogIn,
                OperationStatus.Started,
                new LogInfo(MyLogInfoKey.Username, account.Username));

            webProcessor.GoToUrl(LoginUrl);

            By usernameSelector = By.Id("input_username");
            By passwordSelector = By.Id("input_password");
            By avatarSelector = By.XPath(@"//a[contains(@class,'playerAvatar')]");

            webProcessor.SetText(usernameSelector, account.Username);
            webProcessor.SetText(passwordSelector, account.Password);
            
            webProcessor.Click(By.XPath(@"//*[@id='login_btn_signin']/button"));
            
            webProcessor.WaitForElementToExist(avatarSelector);

            logger.Debug(
                MyOperation.SteamLogIn,
                OperationStatus.Success);
        }

        public void VisitChat()
        {
            webProcessor.GoToUrl(ChatUrl);

            By avatarSelector = By.ClassName("currentUserAvatar");
            webProcessor.WaitForElementToExist(avatarSelector);
        }
        
        public void GatherProfileInformation()
        {
            webProcessor.GoToUrl(AccountUrl);

            By emailAddressSelector = By.XPath(@"//*[@id='main_content']/div[2]/div[4]/div[1]/div[2]/div[1]/span[2]");
            By displayNameSelector = By.ClassName("actual_persona_name");
            By realNameSelector = By.XPath("//div[contains(@class,'header_real_name')]/bdi");

            account.EmailAddress = webProcessor.GetText(emailAddressSelector);

            webProcessor.GoToUrl(account.SteamProfileUrl);

            string name = webProcessor.GetText(realNameSelector);;
            
            account.DisplayName = webProcessor.GetText(displayNameSelector);
            account.PersonalName = new PersonalName(name);
            account.Country = GetCountry();
        }

        public void UpdatePrivacySettings()
        {
            webProcessor.GoToUrl(PrivacySettingsUrl);

            By gameDetailsDropDownSelector = By.XPath(@"/html/body/div[1]/div[7]/div[3]/div/div[2]/div/div[1]/div[3]/div/div[6]/div[1]/div");
            By gameDetailsPublicOptionSelector = By.XPath(@"/html/body/div[4]/div/div/div[1]");

            webProcessor.Click(gameDetailsDropDownSelector);
            webProcessor.Click(gameDetailsPublicOptionSelector);
        }

        public IEnumerable<uint> GatherOwnedGames()
        {
            webProcessor.GoToUrl(GamesUrl);

            By gameRowSelector = By.ClassName("gameListRow");
            
            IEnumerable<string> gameRowsIds = webProcessor.GetIdOfMany(gameRowSelector);
            List<uint> appIds = new List<uint>();

            foreach (string gameRowId in gameRowsIds)
            {
                string appIdText = gameRowId.Substring(5);
                uint appId = uint.Parse(appIdText);

                appIds.Add(appId);
            }

            return appIds;
        }

        public void ActivatePackages(IEnumerable<uint> subIds)
        {
            logger.Info(
                MyOperation.PackageActivation,
                OperationStatus.Started);

            int packageCountToActivate = Math.Min(PackagesActivationLimit, subIds.Count());
            int activatedPackagesCount = 0;

            for (int i = 0; i < packageCountToActivate; i++)
            {
                uint subId = subIds.GetRandomElement();
                bool wasActivated = ActivatePackage(subId);

                if (wasActivated)
                {
                    activatedPackagesCount += 1;
                }
            }

            logger.Debug(
                MyOperation.PackageActivation,
                OperationStatus.Success,
                new LogInfo(MyLogInfoKey.GamesCount, activatedPackagesCount));
        }

        bool TryActivatePackage(uint package)
        {
            try
            {
                return ActivatePackage(package);
            }
            catch
            {
                return false;
            }
        }

        bool ActivatePackage(uint package)
        {
            webProcessor.GoToUrl(LicensesUrl);

            By pageContentSelector = By.ClassName("page_content");
            By lastGameTitleSelector = By.XPath(@"//table[@class='account_table']/tbody/tr[2]/td[2]");

            webProcessor.WaitForElementToExist(pageContentSelector);

            string activationEndpoint = string.Format(AddFreeLicenseUrlFormat, package);
            string activationScript = $"jQuery.post('{activationEndpoint}',{{ajax:true,sessionid:g_sessionID}})";
            string previousGameTitle = string.Empty;
            string currentGameTitle = string.Empty;

            if (webProcessor.DoesElementExist(lastGameTitleSelector))
            {
                previousGameTitle = webProcessor.GetText(lastGameTitleSelector);
            }

            webProcessor.ExecuteScript(activationScript);
            webProcessor.Wait(250);

            for (int checkCount = 0; checkCount < 2; checkCount++)
            {
                webProcessor.Refresh();
                webProcessor.WaitForElementToExist(pageContentSelector);

                if (webProcessor.DoesElementExist(lastGameTitleSelector))
                {
                    currentGameTitle = webProcessor.GetText(lastGameTitleSelector);
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

            string countryFlagUrl = webProcessor.GetSource(countryFlagSelector);
            string[] countryFlagUrlFields = countryFlagUrl.Split('/');

            string countryCode = countryFlagUrlFields[countryFlagUrlFields.Length - 1].Substring(0, 2);

            return Country.FromSteamCode(countryCode);
        }
    }
}
