using NuciLog.Core;

namespace SteamGiveawaysBot.Logging
{
    public sealed class MyLogInfoKey : LogInfoKey
    {
        MyLogInfoKey(string name) : base(name) { }

        public static LogInfoKey GiveawayProcessor => new MyLogInfoKey(nameof(GiveawayProcessor));

        public static LogInfoKey SgbUser => new MyLogInfoKey(nameof(SgbUser));

        public static LogInfoKey Username => new MyLogInfoKey(nameof(Username));

        public static LogInfoKey RewardsCount => new MyLogInfoKey(nameof(RewardsCount));

        public static LogInfoKey RechargeDuration => new MyLogInfoKey(nameof(RechargeDuration));

        public static LogInfoKey RetryDelay => new MyLogInfoKey(nameof(RetryDelay));

        public static LogInfoKey MinimumPoints => new MyLogInfoKey(nameof(MinimumPoints));

        public static LogInfoKey CurrentPoints => new MyLogInfoKey(nameof(CurrentPoints));

        public static LogInfoKey GameTitle => new MyLogInfoKey(nameof(GameTitle));

        public static LogInfoKey GiveawayId => new MyLogInfoKey(nameof(GiveawayId));

        public static LogInfoKey Price => new MyLogInfoKey(nameof(Price));

        public static LogInfoKey EndTime => new MyLogInfoKey(nameof(EndTime));

        public static LogInfoKey Reason => new MyLogInfoKey(nameof(Reason));

        public static LogInfoKey GamesCount => new MyLogInfoKey(nameof(GamesCount));

        public static LogInfoKey OwnedGamesCount => new MyLogInfoKey(nameof(OwnedGamesCount));

        public static LogInfoKey SubId => new MyLogInfoKey(nameof(SubId));
    }
}