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

namespace SteamGiveawaysBot.Processors.GiveawayProcessors
{
    public sealed class SteamGiftsProcessor : GiveawayProcessor
    {
        public override string LogInUrl => "https://www.steamgifts.com/";

        public override int MinimumPoints => 20;

        public override int MaximumPoints => 400;

        public SteamGiftsProcessor(IWebDriver driver, AccountDetails account)
            : base(driver, account)
        {
        }

        public override void LogIn()
        {
            GoToUrl(LogInUrl);

            Click(By.XPath(@"/html/body/header/nav/div[2]/div/a"));
            Click(By.Id("imageLogin"));

            WaitUntilElementExists(By.ClassName("nav__left-container"));

            if (ElementVisible(By.Name("username")))
            {
                Register();
            }
        }

        void Register()
        {
            SendKeys(By.Name("username"), account.Username);
            SendKeys(By.Name("email"), account.EmailAddress);
            SelectByValue(By.Name("country"), account.Country.SgCode.ToString());

            // Say no to emails
            Click(By.XPath(@"//form/div[4]/div[2]/div[1]/div[2]/i[2]"));
            Click(By.XPath(@"//form/div[5]/div[2]/div[1]/div[2]"));

            // Complete registration
            Click(By.XPath(@"//form/div[6]/div[1]"));
        }
    }
}
