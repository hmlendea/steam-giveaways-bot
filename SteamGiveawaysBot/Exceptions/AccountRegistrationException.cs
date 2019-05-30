using System;

namespace SteamGiveawaysBot.Exceptions
{
    /// <summary>
    /// Account registration exception.
    /// </summary>
    public class AccountRegistrationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AccountRegistrationException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public AccountRegistrationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AccountRegistrationException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        public AccountRegistrationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
