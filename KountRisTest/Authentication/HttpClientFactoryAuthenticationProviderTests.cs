using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Kount.Ris.Authentication;
using KountRisSdk.Kount.Ris.Authentication;
using Xunit;

namespace KountRisTest.Authentication;

/// <summary>
/// Unit tests for DefaultAuthenticationProvider using IHttpClientFactory.
/// Tests the dependency injection mode with proper HttpClient lifecycle management.
/// </summary>
public class HttpClientFactoryAuthenticationProviderTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHandler;
    private readonly MockHttpClientFactory _mockFactory;

    public HttpClientFactoryAuthenticationProviderTests()
    {
        _mockHandler = new MockHttpMessageHandler();
        _mockFactory = new MockHttpClientFactory(_mockHandler);
    }


    [Fact]
    public void Constructor_WithNullFactory_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new DefaultAuthenticationProvider("http://auth.test", "key123", (IHttpClientFactory)null!)
        );
    }

    [Fact]
    public void GetAuthenticationHeaders_ValidToken_ReturnsSuccess()
    {
        // Arrange
        _mockHandler.SetupResponse(
            200,
            "{\"access_token\":\"factory_token\",\"token_type\":\"Bearer\",\"expires_in\":3600}"
        );

        var provider = new DefaultAuthenticationProvider("http://auth.test", "key123", _mockFactory);

        // Act
        var result = provider.GetAuthenticationHeaders();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Headers);
        Assert.Equal("Bearer factory_token", result.Headers["Authorization"]);
    }

    [Fact]
    public void GetAuthenticationHeaders_NetworkFailure_ReturnsError()
    {
        // Arrange
        _mockHandler.SetupException(new HttpRequestException("Factory network unavailable"));

        var provider = new DefaultAuthenticationProvider("http://auth.test", "key123", _mockFactory);

        // Act
        var result = provider.GetAuthenticationHeaders();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Network error during token refresh", result.ErrorMessage);
        Assert.Contains("Factory network unavailable", result.ErrorMessage);
    }

    [Fact]
    public void GetAuthenticationHeaders_CachedToken_CreatesNewClientEachTime()
    {
        // Arrange
        _mockHandler.SetupResponse(
            200,
            "{\"access_token\":\"factory_cached_token\",\"token_type\":\"Bearer\",\"expires_in\":3600}"
        );

        var provider = new DefaultAuthenticationProvider("http://auth.test", "key123", _mockFactory);

        // Act - First call to cache the token
        var result1 = provider.GetAuthenticationHeaders();

        // Act - Second call should use cached token but still create new client for potential future calls
        var result2 = provider.GetAuthenticationHeaders();

        // Assert
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.Equal("Bearer factory_cached_token", result1.Headers["Authorization"]);
        Assert.Equal("Bearer factory_cached_token", result2.Headers["Authorization"]);

        // Verify factory was called at least once (for the initial token fetch)
        Assert.True(_mockFactory.CreateClientCallCount >= 1);
    }

    [Fact]
    public void GetAuthenticationHeaders_ForceRefresh_CreatesNewClient()
    {
        // Arrange
        _mockHandler.SetupResponse(
            200,
            "{\"access_token\":\"factory_first_token\",\"token_type\":\"Bearer\",\"expires_in\":3600}"
        );

        var provider = new DefaultAuthenticationProvider("http://auth.test", "key123", _mockFactory);

        // Act - First call
        var result1 = provider.GetAuthenticationHeaders();
        var initialCallCount = _mockFactory.CreateClientCallCount;

        // Set up different response for refresh
        _mockHandler.SetupResponse(
            200,
            "{\"access_token\":\"factory_refreshed_token\",\"token_type\":\"Bearer\",\"expires_in\":3600}"
        );

        // Act - Force refresh
        var result2 = provider.GetAuthenticationHeaders(forceRefresh: true);

        // Assert
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.Equal("Bearer factory_first_token", result1.Headers["Authorization"]);
        Assert.Equal("Bearer factory_refreshed_token", result2.Headers["Authorization"]);

        // Verify factory was called again for refresh
        Assert.True(_mockFactory.CreateClientCallCount > initialCallCount);
    }

    [Fact]
    public async Task GetAuthenticationHeaders_ConcurrentCalls_ThreadSafe()
    {
        // Arrange
        _mockHandler.SetupResponse(
            200,
            "{\"access_token\":\"factory_thread_safe_token\",\"token_type\":\"Bearer\",\"expires_in\":3600}"
        );

        var provider = new DefaultAuthenticationProvider("http://auth.test", "key123", _mockFactory);
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
            Assert.Equal("Bearer factory_thread_safe_token", result.Headers["Authorization"]);
        }
    }


    public void Dispose()
    {
        _mockHandler?.Dispose();
    }

    /// <summary>
    /// Mock implementation of IHttpClientFactory for testing.
    /// Tracks calls and returns HttpClient instances with configured mock handler.
    /// </summary>
    private class MockHttpClientFactory(MockHttpMessageHandler handler) : IHttpClientFactory
    {
        public int CreateClientCallCount { get; private set; }

        public HttpClient CreateClient(string? name = null)
        {
            CreateClientCallCount++;
            return new HttpClient(handler);
        }
    }
}