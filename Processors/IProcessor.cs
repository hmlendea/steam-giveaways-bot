namespace SteamGiveawaysBot.Processors
{
    public interface IProcessor
    {
        string Name { get; }

        void Close();
    }
}