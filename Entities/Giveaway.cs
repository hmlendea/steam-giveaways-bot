using System;

namespace SteamGiveawaysBot.Entities
{
    public sealed class Giveaway : IEquatable<Giveaway>
    {
        public string Url { get; }

        public string GiveawayCode
        {
            get
            {
                string[] fields = Url.Split('/');
                return fields[fields.Length - 2];
            }
        }

        public string SteamAppUrl => $"https://store.steampowered.com/app/{SteamAppId}";

        public string SteamAppId
        {
            get
            {
                string[] fields = Url.Split('/');
                return fields[fields.Length - 1];
            }
        }

        public string GameTitle { get; }

        public int Price { get; }

        public DateTime EndTime { get; }

        public Giveaway(string url, string gameTitle, int price, DateTime endTime)
        {
            Url = url;
            GameTitle = gameTitle;
            Price = price;
            EndTime = endTime;
        }
        
        /// <summary>
        /// Determines whether the specified <see cref="Giveaway"/> is equal to the current <see cref="Giveaway"/>.
        /// </summary>
        /// <param name="other">The <see cref="Giveaway"/> to compare with the current <see cref="Giveaway"/>.</param>
        /// <returns><c>true</c> if the specified <see cref="Giveaway"/> is equal to the current
        /// <see cref="Giveaway"/>; otherwise, <c>false</c>.</returns>
        public bool Equals(Giveaway other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return string.Equals(Url, other.Url);
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to the current <see cref="Giveaway"/>.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with the current <see cref="Giveaway"/>.</param>
        /// <returns><c>true</c> if the specified <see cref="object"/> is equal to the current
        /// <see cref="Giveaway"/>; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((Giveaway)obj);
        }

        public static bool operator == (Giveaway me, Giveaway other)
        {
            if (me is null)
            {
                return other is null;
            }

            return me.Equals(other);
        }

        public static bool operator != (Giveaway me, Giveaway other)
        {
            return !(me == other);
        }

        /// <summary>
        /// Serves as a hash function for a <see cref="Giveaway"/> object.
        /// </summary>
        /// <returns>A hash code for this instance that is suitable for use in hashing algorithms and data structures such as a
        /// hash table.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((Url != null ? Url.GetHashCode() : 0) * 246);
            }
        }
    }
}