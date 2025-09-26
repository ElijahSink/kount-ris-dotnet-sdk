using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Kount.Ris.Authentication;
using KountRisSdk.Kount.Ris.Authentication;
using Xunit;

namespace KountRisTest.Authentication
{
    /// <summary>
    /// Unit tests for DefaultAuthenticationProvider.
    /// Tests token caching, refresh logic, error handling, and thread safety.
    /// </summary>
    public class DefaultAuthenticationProviderTests : IDisposable
    {
        private readonly MockHttpMessageHandler _mockHandler;
        private readonly HttpClient _mockHttpClient;

        public DefaultAuthenticationProviderTests()
        {
            _mockHandler = new MockHttpMessageHandler();
            _mockHttpClient = new HttpClient(_mockHandler);
        }


        [Fact]
        public void Constructor_WithNullAuthUrl_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new DefaultAuthenticationProvider(null, "key123", _mockHttpClient)
            );
        }

        [Fact]
        public void Constructor_WithNullApiKey_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new DefaultAuthenticationProvider("http://auth.test", null, _mockHttpClient)
            );
        }

        [Fact]
        public void GetAuthenticationHeaders_ValidToken_ReturnsSuccess()
        {
            // Arrange
            _mockHandler.SetupResponse(
                200,
                "{\"access_token\":\"abc123\",\"token_type\":\"Bearer\",\"expires_in\":3600}"
            );

            var provider = new DefaultAuthenticationProvider("http://auth.test", "key123", _mockHttpClient);

            // Act
            var result = provider.GetAuthenticationHeaders();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Headers);
            Assert.Equal("Bearer abc123", result.Headers["Authorization"]);
        }

        [Fact]
        public void GetAuthenticationHeaders_NetworkFailure_ReturnsError()
        {
            // Arrange
            _mockHandler.SetupException(new HttpRequestException("Network unavailable"));

            var provider = new DefaultAuthenticationProvider("http://auth.test", "key123", _mockHttpClient);

            // Act
            var result = provider.GetAuthenticationHeaders();

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Network error during token refresh", result.ErrorMessage);
            Assert.Contains("Network unavailable", result.ErrorMessage);
        }

        [Fact]
        public void GetAuthenticationHeaders_InvalidJsonResponse_ReturnsError()
        {
            // Arrange
            _mockHandler.SetupResponse(200, "invalid json");

            var provider = new DefaultAuthenticationProvider("http://auth.test", "key123", _mockHttpClient);

            // Act
            var result = provider.GetAuthenticationHeaders();

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Failed to parse token response", result.ErrorMessage);
        }

        [Fact]
        public void GetAuthenticationHeaders_HttpErrorStatus_ReturnsError()
        {
            // Arrange
            _mockHandler.SetupResponse(401, "Unauthorized");

            var provider = new DefaultAuthenticationProvider("http://auth.test", "key123", _mockHttpClient);

            // Act
            var result = provider.GetAuthenticationHeaders();

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Network error during token refresh", result.ErrorMessage);
        }

        [Fact]
        public void GetAuthenticationHeaders_InvalidTokenResponse_ReturnsError()
        {
            // Arrange - response missing expires_in
            _mockHandler.SetupResponse(
                200,
                "{\"access_token\":\"abc123\",\"token_type\":\"Bearer\"}"
            );

            var provider = new DefaultAuthenticationProvider("http://auth.test", "key123", _mockHttpClient);

            // Act
            var result = provider.GetAuthenticationHeaders();

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Invalid token response", result.ErrorMessage);
        }

        [Fact]
        public void GetAuthenticationHeaders_ZeroExpiresIn_ReturnsError()
        {
            // Arrange
            _mockHandler.SetupResponse(
                200,
                "{\"access_token\":\"abc123\",\"token_type\":\"Bearer\",\"expires_in\":0}"
            );

            var provider = new DefaultAuthenticationProvider("http://auth.test", "key123", _mockHttpClient);

            // Act
            var result = provider.GetAuthenticationHeaders();

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Invalid token response", result.ErrorMessage);
        }

        [Fact]
        public void GetAuthenticationHeaders_CachedToken_ReturnsWithoutNetworkCall()
        {
            // Arrange
            _mockHandler.SetupResponse(
                200,
                "{\"access_token\":\"cached_token\",\"token_type\":\"Bearer\",\"expires_in\":3600}"
            );

            var provider = new DefaultAuthenticationProvider("http://auth.test", "key123", _mockHttpClient);

            // Act - First call to cache the token
            var result1 = provider.GetAuthenticationHeaders();

            // Set up handler to fail on subsequent calls
            _mockHandler.SetupException(new HttpRequestException("Should not be called"));

            // Act - Second call should use cached token
            var result2 = provider.GetAuthenticationHeaders();

            // Assert
            Assert.True(result1.IsSuccess);
            Assert.True(result2.IsSuccess);
            Assert.Equal("Bearer cached_token", result2.Headers["Authorization"]);
        }

        [Fact]
        public void GetAuthenticationHeaders_ForceRefresh_RefreshesToken()
        {
            // Arrange
            _mockHandler.SetupResponse(
                200,
                "{\"access_token\":\"first_token\",\"token_type\":\"Bearer\",\"expires_in\":3600}"
            );

            var provider = new DefaultAuthenticationProvider("http://auth.test", "key123", _mockHttpClient);

            // Act - First call
            var result1 = provider.GetAuthenticationHeaders();

            // Set up different response for refresh
            _mockHandler.SetupResponse(
                200,
                "{\"access_token\":\"refreshed_token\",\"token_type\":\"Bearer\",\"expires_in\":3600}"
            );

            // Act - Force refresh
            var result2 = provider.GetAuthenticationHeaders(forceRefresh: true);

            // Assert
            Assert.True(result1.IsSuccess);
            Assert.True(result2.IsSuccess);
            Assert.Equal("Bearer first_token", result1.Headers["Authorization"]);
            Assert.Equal("Bearer refreshed_token", result2.Headers["Authorization"]);
        }

        [Fact]
        public void InvalidateToken_AfterCaching_ForcesRefreshOnNextCall()
        {
            // Arrange
            _mockHandler.SetupResponse(
                200,
                "{\"access_token\":\"first_token\",\"token_type\":\"Bearer\",\"expires_in\":3600}"
            );

            var provider = new DefaultAuthenticationProvider("http://auth.test", "key123", _mockHttpClient);

            // Act - First call to cache token
            var result1 = provider.GetAuthenticationHeaders();

            // Invalidate the token
            provider.InvalidateToken();

            // Set up different response for next call
            _mockHandler.SetupResponse(
                200,
                "{\"access_token\":\"new_token\",\"token_type\":\"Bearer\",\"expires_in\":3600}"
            );

            // Act - Should refresh due to invalidation
            var result2 = provider.GetAuthenticationHeaders();

            // Assert
            Assert.True(result1.IsSuccess);
            Assert.True(result2.IsSuccess);
            Assert.Equal("Bearer first_token", result1.Headers["Authorization"]);
            Assert.Equal("Bearer new_token", result2.Headers["Authorization"]);
        }

        [Fact]
        public async Task GetAuthenticationHeaders_ConcurrentCalls_ThreadSafe()
        {
            // Arrange
            _mockHandler.SetupResponse(
                200,
                "{\"access_token\":\"thread_safe_token\",\"token_type\":\"Bearer\",\"expires_in\":3600}"
            );

            var provider = new DefaultAuthenticationProvider("http://auth.test", "key123", _mockHttpClient);
            var tasks = new List<Task<AuthResult>>();
            const int concurrentCalls = 10;

            // Act - Make multiple concurrent calls
            for (var i = 0; i < concurrentCalls; i++)
            {
                tasks.Add(Task.Run(() => provider.GetAuthenticationHeaders()));
            }

            var results = await Task.WhenAll(tasks.ToArray());

            // Assert - All calls should succeed with the same token
            foreach (var result in results)
            {
                Assert.True(result.IsSuccess);
                Assert.Equal("Bearer thread_safe_token", result.Headers["Authorization"]);
            }
        }

        public void Dispose()
        {
            _mockHttpClient?.Dispose();
            _mockHandler?.Dispose();
        }
    }
}