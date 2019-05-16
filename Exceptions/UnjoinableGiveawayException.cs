using System;

using SteamGiveawaysBot.Entities;

namespace SteamGiveawaysBot.Exceptions
{
    /// <summary>
    /// Unjoinable giveaway exception.
    /// </summary>
    public class UnjoinableGiveawayException : Exception
    {
        public string Reason { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnjoinableGiveawayException"/> class.
        /// </summary>
        /// <param name="giveaway">The giveaway.</param>
        /// <param name="reason">The reason why the giveaway cannot be joined.</param>
        public UnjoinableGiveawayException(Giveaway giveaway, string reason)
            : base($"The giveaway for '{giveaway.GameTitle}' ('{giveaway.GiveawayCode}') cannot be joined. Reason: '{reason}'")
        {
            Reason = reason;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnjoinableGiveawayException"/> class.
        /// </summary>
        /// <param name="giveawayCode">The giveaway code.</param>
        /// <param name="reason">The reason why the giveaway cannot be joined.</param>
        /// <param name="innerException">The inner exception.</param>
        public UnjoinableGiveawayException(string giveawayCode, string reason, Exception innerException)
            : base($"The '{giveawayCode}' cannot be joined ({reason})", innerException)
        {
            Reason = reason;
        }
    }
}
