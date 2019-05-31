using System;

using NuciExtensions;

namespace SteamGiveawaysBot.Entities
{
    public sealed class PersonalName
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string FullName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(LastName))
                {
                    return $"{FirstName} {LastName}";
                }

                return FirstName;
            }
        }

        public PersonalName()
        {
            FirstName = string.Empty;
            LastName = string.Empty;
        }

        public PersonalName(string name)
        {
            if (name.Contains(' '))
            {
                string[] split = name.Split(' ');
                FirstName = split[0].ToTitleCase();
                LastName = split[split.Length - 1].ToTitleCase();
            }
            else if (!string.IsNullOrWhiteSpace(name))
            {
                FirstName = name.ToTitleCase();
                LastName = string.Empty;
            }
            else
            {
                FirstName = string.Empty;
                LastName = string.Empty;
            }
        }

        public PersonalName(string firstName, string lastName)
        {
            FirstName = firstName;
            LastName = lastName;
        }
    }
}
