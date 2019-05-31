using System;

namespace SteamGiveawaysBot.Utils
{
    public static class DateTimeUtils
    {
        public static DateTime FromUnixTime(string unixTimestamp) => FromUnixTime(double.Parse(unixTimestamp));
        public static DateTime FromUnixTime(double unixTime)
        {

            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

            dateTime = dateTime.AddSeconds(unixTime).ToLocalTime();

            return dateTime;
        }
    }
}