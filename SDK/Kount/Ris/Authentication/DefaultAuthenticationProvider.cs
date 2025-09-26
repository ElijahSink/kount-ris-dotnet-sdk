//-----------------------------------------------------------------------
// <copyright file="DefaultAuthenticationProvider.cs" company="Equifax Inc">
//     Copyright 2025 Equifax. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using Kount.Ris;
using Kount.Ris.Authentication;

namespace KountRisSdk.Kount.Ris.Authentication
{
    /// <summary>
    /// Default authentication provider for Kount Payments Fraud API.
    /// </summary>
    public class DefaultAuthenticationProvider : IAuthenticationProvider, IDisposable
    {
        private readonly object _lock = new object();
        private readonly string _authUrl;
        private readonly string _apiKey;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly HttpClient _staticHttpClient;
        private readonly bool _ownsStaticClient;
        private readonly bool _useFactory;

        private BearerAuthResponse _cachedToken;
        private DateTimeOffset _tokenExpiration;

        public DefaultAuthenticationProvider(string authUrl, string apiKey, HttpClient httpClient = null)
        {
            _authUrl = authUrl ?? throw new ArgumentNullException(nameof(authUrl));
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _staticHttpClient = httpClient ?? new HttpClient();
            _ownsStaticClient = httpClient == null;
            _useFactory = false;
            _tokenExpiration = DateTimeOffset.MinValue;
        }

        public DefaultAuthenticationProvider(string authUrl, string apiKey, IHttpClientFactory httpClientFactory)
        {
            _authUrl = authUrl ?? throw new ArgumentNullException(nameof(authUrl));
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _useFactory = true;
            _ownsStaticClient = false;
            _tokenExpiration = DateTimeOffset.MinValue;
        }

        public AuthResult GetAuthenticationHeaders(bool forceRefresh = false)
        {
            lock (_lock)
            {
                if (forceRefresh || _tokenExpiration <= DateTimeOffset.Now)
                {
                    var refreshResult = RefreshToken();
                    if (!refreshResult.IsSuccess)
                        return refreshResult;
                }

                return AuthResult.Success(
                    new Dictionary<string, string>
                    {
                        ["Authorization"] = $"{_cachedToken.TokenType} {_cachedToken.AccessToken}"
                    }
                );
            }
        }

        public void InvalidateToken()
        {
            lock (_lock)
            {
                _tokenExpiration = DateTimeOffset.MinValue;
                _cachedToken = null;
            }
        }

        private AuthResult RefreshToken()
        {
            try
            {
                var tokenUrl = _authUrl + "?grant_type=client_credentials&scope=k1_integration_api";

                if (_useFactory)
                {
                    using (var httpClient = _httpClientFactory.CreateClient())
                    {
                        return ExecuteTokenRequest(httpClient, tokenUrl);
                    }
                }

                return ExecuteTokenRequest(_staticHttpClient, tokenUrl);
            }
            catch (Exception ex)
            {
                return AuthResult.Failure($"Token refresh failed: {ex.Message}");
            }
        }

        private AuthResult ExecuteTokenRequest(HttpClient httpClient, string tokenUrl)
        {
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl))
                {
                    request.Content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>());
                    request.Headers.Add("Authorization", "Basic " + _apiKey);

                    using (var response = httpClient.SendAsync(request).Result)
                    {
                        response.EnsureSuccessStatusCode();
                        var responseString = response.Content.ReadAsStringAsync().Result;

                        var authResponse = JsonSerializer.Deserialize<BearerAuthResponse>(responseString);
                        if (authResponse?.ExpiresIn > 0)
                        {
                            _cachedToken = authResponse;
                            _tokenExpiration = DateTimeOffset.Now.AddSeconds(authResponse.ExpiresIn - 30);
                            return AuthResult.Success(null);
                        }

                        return AuthResult.Failure("Invalid token response: missing or invalid expiration");
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                return AuthResult.Failure($"Network error during token refresh: {ex.Message}");
            }
            catch (JsonException ex)
            {
                return AuthResult.Failure($"Failed to parse token response: {ex.Message}");
            }
            catch (AggregateException ex) when (ex.InnerException is HttpRequestException)
            {
                return AuthResult.Failure($"Network error during token refresh: {ex.InnerException.Message}");
            }
            catch (Exception ex)
            {
                return AuthResult.Failure($"Token refresh failed: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_ownsStaticClient) _staticHttpClient?.Dispose();
        }
    }
}