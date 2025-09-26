//-----------------------------------------------------------------------
// <copyright file="AuthResult.cs" company="Equifax Inc">
//     Copyright 2025 Equifax. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;

namespace KountRisSdk.Kount.Ris.Authentication
{
    /// <summary>
    /// Result of an authentication operation.
    /// </summary>
    public class AuthResult
    {
        /// <summary>
        /// Gets a value indicating whether the authentication was successful.
        /// </summary>
        public bool IsSuccess { get; private set; }

        /// <summary>
        /// Gets the authentication headers if successful, otherwise null.
        /// </summary>
        public Dictionary<string, string> Headers { get; private set; }

        /// <summary>
        /// Gets the error message if authentication failed, otherwise null.
        /// </summary>
        public string ErrorMessage { get; private set; }

        private AuthResult()
        {
        }

        /// <summary>
        /// Creates a successful authentication result with the provided headers.
        /// </summary>
        /// <param name="headers">The authentication headers to include in requests</param>
        /// <returns>A successful AuthResult</returns>
        public static AuthResult Success(Dictionary<string, string> headers)
        {
            return new AuthResult
            {
                IsSuccess = true,
                Headers = headers ?? new Dictionary<string, string>(),
                ErrorMessage = null
            };
        }

        /// <summary>
        /// Creates a failed authentication result with the provided error message.
        /// </summary>
        /// <param name="errorMessage">The error message describing what went wrong</param>
        /// <returns>A failed AuthResult</returns>
        public static AuthResult Failure(string errorMessage)
        {
            return new AuthResult
            {
                IsSuccess = false,
                Headers = null,
                ErrorMessage = string.IsNullOrEmpty(errorMessage) ? "Unknown authentication error" : errorMessage
            };
        }
    }
}