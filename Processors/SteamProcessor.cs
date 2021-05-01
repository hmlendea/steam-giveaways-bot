using System;
using System.Security.Authentication;

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

        readonly IWebProcessor webProcessor;
        readonly AccountDetails account;
        readonly ILogger logger;

        string ProfileUrl => $"https://steamcommunity.com/id/{account.Username}";
        string EditProfileUrl => $"{ProfileUrl}/edit";
        string PrivacySettingsUrl => $"{EditProfileUrl}/settings";
        string BadgesUrl => $"{ProfileUrl}/badges";
        string GamesUrl => $"{ProfileUrl}/games/?tab=all";

        public SteamProcessor(
            IWebProcessor webProcessor,
            AccountDetails account,
            ILogger logger)
        {
            this.webProcessor = webProcessor;
            this.account = account;
            this.logger = logger;
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
            By captchaInputSelector = By.Id("input_captcha");
            By logInButtonSelector = By.XPath(@"//*[@id='login_btn_signin']/button");
            By errorBoxSelector = By.Id("error_display");
            By steamGuardCodeInputSelector = By.Id("twofactorcode_entry");
            By avatarSelector = By.XPath(@"//a[contains(@class,'playerAvatar')]");

            if (webProcessor.IsElementVisible(avatarSelector))
            {
                logger.Debug(
                    MyOperation.SteamLogIn,
                    OperationStatus.Success,
                    "Already logged in");

                return;
            }
            
            if (webProcessor.IsElementVisible(captchaInputSelector))
            {
                ThrowLogInException("Captcha input required");
            }

            webProcessor.SetText(usernameSelector, account.Username);
            webProcessor.SetText(passwordSelector, account.Password);
            
            webProcessor.Click(logInButtonSelector);
            webProcessor.WaitForAnyElementToBeVisible(
                steamGuardCodeInputSelector,
                errorBoxSelector,
                avatarSelector);

            if (webProcessor.IsElementVisible(steamGuardCodeInputSelector))
            {
                ThrowLogInException("SteamGuard input required");
            }
            
            if (webProcessor.IsElementVisible(errorBoxSelector))
            {
                string errorMessage = webProcessor.GetText(errorBoxSelector);

                logger.Error(
                    MyOperation.SteamLogIn,
                    OperationStatus.Failure,
                    "Log in failed");

                throw new AuthenticationException(errorMessage);
            }

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
        
        void ThrowLogInException(string message)
        {
            Exception ex = new AuthenticationException(message);

            logger.Error(
                MyOperation.SteamLogIn,
                OperationStatus.Failure,
                ex,
                new LogInfo(MyLogInfoKey.Username, account.Username));

            throw ex;
        }
    }
}
