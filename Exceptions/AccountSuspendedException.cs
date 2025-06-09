using System;

namespace SteamGiveawaysBot.Exceptions
{
    /// <summary>
    /// Account suspended exception.
    /// </summary>
    public class AccountSuspendedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AccountSuspendedException"/> class.
        /// </summary>
        /// <param name="username">Account username.</param>
        public AccountSuspendedException(string username)
            : base($"The '{username}' account is suspended") { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AccountSuspendedException"/> class.
        /// </summary>
        /// <param name="username">Account username.</param>
        /// <param name="innerException">Inner exception.</param>
        public AccountSuspendedException(string username, Exception innerException)
            : base($"The '{username}' account is suspended", innerException) { }
    }
}
