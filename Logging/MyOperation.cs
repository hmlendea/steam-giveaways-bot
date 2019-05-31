using NuciLog.Core;

namespace SteamGiveawaysBot.Logging
{
    public sealed class MyOperation : Operation
    {
        MyOperation(string name)
            : base(name)
        {
            
        }

        public static Operation ConfigurationUpdate => new MyOperation(nameof(ConfigurationUpdate));

        public static Operation ConnectivityTest => new MyOperation(nameof(ConnectivityTest));

        public static Operation BotStart => new MyOperation(nameof(BotStart));

        public static Operation BotStop => new MyOperation(nameof(BotStop));

        public static Operation RewardsProcessing => new MyOperation(nameof(RewardsProcessing));

        public static Operation AccountDetailsRetrieval => new MyOperation(nameof(AccountDetailsRetrieval));

        public static Operation RechargePoints => new MyOperation(nameof(RechargePoints));

        public static Operation JoinGiveaway => new MyOperation(nameof(JoinGiveaway));

        public static Operation SteamLogIn => new MyOperation(nameof(SteamLogIn));

        public static Operation SteamGiftsLogIn => new MyOperation(nameof(SteamGiftsLogIn));

        public static Operation PlayTimeFaking => new MyOperation(nameof(PlayTimeFaking));
    }
}