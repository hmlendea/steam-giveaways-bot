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
    public abstract class GiveawayProcessor : WebProcessor
    {
        public abstract string LogInUrl { get; }

        public abstract int MinimumPoints { get; }

        public abstract int MaximumPoints { get; }

        protected AccountDetails account;

        public GiveawayProcessor(IWebDriver driver, AccountDetails account)
            : base(driver)
        {
            this.account = account;
        }

        public abstract void LogIn();
    }
}
