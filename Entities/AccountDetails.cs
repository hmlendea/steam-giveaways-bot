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

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string FullName => $"{FirstName} {LastName}";

        public string SteamProfileUrl => $"https://steamcommunity.com/id/{Username}";

        public decimal SteamGiftsValue { get; set; }

        public bool IsRegisteredSteamGifts { get; set; }

        public bool IsRegisteredIndieGala { get; set; }

        public DateTime Birthday { get; set; }

        public override string ToString()
        {
            return
                $"{Username}:{Password} ({EmailAddress}). " +
                $"{FirstName} {LastName} from {Country}. " +
                $"Born: {Birthday.ToString("yyyy/MM/dd")}.";
        }
    }
}
