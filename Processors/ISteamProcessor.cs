using System.Collections.Generic;

namespace SteamGiveawaysBot.Processors
{
    public interface ISteamProcessor
    {
        void Quit();

        void LogIn();

        void VisitChat();

        void GatherProfileInformation();

        void UpdatePrivacySettings();

        IEnumerable<uint> GatherOwnedGames();

        void ActivatePackages(IEnumerable<uint> subIds);
    }
}
