namespace SteamGiveawaysBot.Processors.GiveawayProcessors
{
    public interface IGiveawayProcessor
    {
        void Run();

        void Stop();

        void LogIn();
    }
}
