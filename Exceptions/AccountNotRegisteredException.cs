using System;

namespace SteamGiveawaysBot.Exceptions
{
    /// <summary>
    /// Account not registered exception.
    /// </summary>
    public class AccountNotRegisteredException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AccountNotRegisteredException"/> class.
        /// </summary>
        /// <param name="username">The account username.</param>
        public AccountNotRegisteredException(string username)
            : base($"The '{username}' account is not registered")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AccountNotRegisteredException"/> class.
        /// </summary>
        /// <param name="username">The account username.</param>
        /// <param name="innerException">The inner exception.</param>
        public AccountNotRegisteredException(string username, Exception innerException)
            : base($"The '{username}' account is not registered", innerException)
        {
        }
    }
}
