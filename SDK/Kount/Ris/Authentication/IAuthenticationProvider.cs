//-----------------------------------------------------------------------
// <copyright file="IAuthenticationProvider.cs" company="Equifax Inc">
//     Copyright 2025 Equifax. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using KountRisSdk.Kount.Ris.Authentication;

namespace Kount.Ris.Authentication
{
    /// <summary>
    /// Handles authentication token management for API requests.
    /// </summary>
    public interface IAuthenticationProvider
    {
        /// <summary>
        /// Gets authentication headers for API requests.
        /// </summary>
        /// <param name="forceRefresh">Forces token refresh even if current token is valid</param>
        /// <returns>AuthResult containing success with headers or failure with error message</returns>
        AuthResult GetAuthenticationHeaders(bool forceRefresh = false);

        /// <summary>
        /// Invalidates the cached token, forcing refresh on next request.
        /// </summary>
        void InvalidateToken();
    }
}