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
        public bool IsSuccess { get; private set; }
        public Dictionary<string, string> Headers { get; private set; }
        public string ErrorMessage { get; private set; }

        private AuthResult()
        {
        }

        public static AuthResult Success(Dictionary<string, string> headers)
        {
            return new AuthResult
            {
                IsSuccess = true,
                Headers = headers ?? new Dictionary<string, string>(),
                ErrorMessage = null
            };
        }

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