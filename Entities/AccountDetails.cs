using System;

namespace SteamGiveawaysBot.Entities
{
    public sealed class AccountDetails
    {
        string username;
        string displayName;

        public string Username
        {
            get { return username.ToLower(); }
            set { username = value; }
        }

        public string DisplayName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(displayName))
                {
                    return username;
                }

                return displayName;
            }
            set { displayName = value; }
        }

        public string Password { get; set; }

        public string EmailAddress { get; set; }

        public Country Country { get; set; }

        public PersonalName PersonalName { get; set; }

        public string SteamProfileUrl => $"https://steamcommunity.com/id/{Username}";

        public string SteamGiftsProfileUrl => $"https://www.steamgifts.com/user/{Username}";

        public decimal SteamGiftsValue { get; set; }

        public bool IsRegisteredSteamGifts { get; set; }

        public bool IsRegisteredIndieGala { get; set; }

        public DateTime Birthday { get; set; }

        public AccountDetails()
        {
            PersonalName = new PersonalName();
        }
    }
}
