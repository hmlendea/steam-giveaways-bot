namespace SteamGiveawaysBot.Processors.GiveawayProcessors
{
    public interface IGiveawayProcessor
    {
        void Quit();

        void Run();

        void Stop();

        void LogIn();

        void Register();

        void ApplyCustomSettings();
    }
}
