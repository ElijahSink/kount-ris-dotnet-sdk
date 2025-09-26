using System.Net.Http;
using Kount.Ris;
using Kount.Ris.Authentication;
using KountRisSdk.Kount.Ris.Authentication;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace KountRisTest.Authentication;

/// <summary>
/// Unit tests for Request constructor behavior, particularly focusing on
/// the authentication provider integration and constructor safety.
/// </summary>
public class RequestConstructorTests
{
    [Fact]
    public void Constructor_InvalidConfig_ThrowsRequestException()
    {
        // Arrange
        var invalidConfig = new Configuration
        {
            MerchantId = null, // Invalid - should cause exception
            URL = "https://test.com",
            ConfigKey = "abcdefghij", // Valid BASE85 characters  
            ConnectTimeout = "5000"
        };

        // Act & Assert
        Assert.Throws<RequestException>(() =>
            new TestableInquiry(checkConfiguration: true, invalidConfig, null!, NullLogger.Instance)
        );
    }

    [Fact]
    public void Constructor_NetworkDown_DoesNotThrow()
    {
        // Arrange - This is the key test: constructor should not make network calls
        var validConfig = new Configuration
        {
            MerchantId = "123456",
            URL = "https://test.com",
            ConfigKey = "abcdefghij", // Valid BASE85 characters
            ConnectTimeout = "5000",
            EnableMigrationMode = "true",
            PaymentsFraudAuthUrl = "https://unreachable-auth-server.com/token", // Unreachable server
            PaymentsFraudApiUrl = "https://api.test.com",
            PaymentsFraudApiKey = "abcdefghij", // Valid BASE85 characters
            ApiKey = "test_api_key" // Provide API key so certificate isn't required
        };

        // Act & Assert - Constructor should succeed even if auth server is unreachable
        var inquiry = new TestableInquiry(checkConfiguration: true, validConfig, null!, NullLogger.Instance);
        Assert.NotNull(inquiry);
    }

    [Fact]
    public void Constructor_WithDI_UsesInjectedProvider()
    {
        // Arrange
        var mockAuthProvider = new MockAuthenticationProvider();
        var validConfig = new Configuration
        {
            MerchantId = "123456",
            URL = "https://test.com",
            ConfigKey = "abcdefghij", // Valid BASE85 characters
            ConnectTimeout = "5000",
            EnableMigrationMode = "true",
            PaymentsFraudAuthUrl = "https://auth.test.com",
            PaymentsFraudApiUrl = "https://api.test.com",
            PaymentsFraudApiKey = "abcdefghij", // Valid BASE85 characters
            ApiKey = "test_api_key" // Provide API key so certificate isn't required
        };

        // Act
        var inquiry = new TestableInquiry(
            checkConfiguration: true,
            validConfig,
            mockAuthProvider,
            NullLogger.Instance
        );

        // Assert
        Assert.NotNull(inquiry);
        Assert.Same(mockAuthProvider, inquiry.GetAuthenticationProvider());
    }

    [Fact]
    public void Constructor_WithNullAuthProvider_CreatesDefaultProvider()
    {
        // Arrange
        var validConfig = new Configuration
        {
            MerchantId = "123456",
            URL = "https://test.com",
            ConfigKey = "abcdefghij", // Valid BASE85 characters
            ConnectTimeout = "5000",
            EnableMigrationMode = "true",
            PaymentsFraudAuthUrl = "https://auth.test.com",
            PaymentsFraudApiUrl = "https://api.test.com",
            PaymentsFraudApiKey = "abcdefghij", // Valid BASE85 characters
            ApiKey = "test_api_key" // Provide API key so certificate isn't required
        };

        // Act
        var inquiry = new TestableInquiry(checkConfiguration: true, validConfig, null!, NullLogger.Instance);

        // Assert
        Assert.NotNull(inquiry);
        Assert.NotNull(inquiry.GetAuthenticationProvider());
        Assert.IsType<DefaultAuthenticationProvider>(inquiry.GetAuthenticationProvider());
    }

    [Fact]
    public void Constructor_NonMigrationMode_DoesNotCreateAuthProvider()
    {
        // Arrange
        var validConfig = new Configuration
        {
            MerchantId = "123456",
            URL = "https://test.com",
            ConfigKey = "abcdefghij", // Valid BASE85 characters
            ConnectTimeout = "5000",
            EnableMigrationMode = "false", // Not in migration mode
            ApiKey = "abcdefghij" // Valid BASE85 characters
        };

        // Act
        var inquiry = new TestableInquiry(checkConfiguration: true, validConfig, null, NullLogger.Instance);

        // Assert
        Assert.NotNull(inquiry);
        Assert.Null(inquiry.GetAuthenticationProvider()); // Should not create auth provider for non-migration mode
    }

    [Fact]
    public void Constructor_WithHttpClientFactory_UsesFactoryProvider()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();
        var mockFactory = new MockHttpClientFactory(mockHandler);
        var validConfig = new Configuration
        {
            MerchantId = "123456",
            URL = "https://test.com",
            ConfigKey = "abcdefghij", // Valid BASE85 characters
            ConnectTimeout = "5000",
            EnableMigrationMode = "true",
            PaymentsFraudAuthUrl = "https://auth.test.com",
            PaymentsFraudApiUrl = "https://api.test.com",
            PaymentsFraudApiKey = "abcdefghij", // Valid BASE85 characters
            ApiKey = "test_api_key" // Provide API key so certificate isn't required
        };

        // Act
        var inquiry = new TestableInquiry(
            checkConfiguration: true,
            validConfig,
            null!,
            mockFactory,
            NullLogger.Instance
        );

        // Assert
        Assert.NotNull(inquiry);
        Assert.NotNull(inquiry.GetAuthenticationProvider());
        Assert.IsType<DefaultAuthenticationProvider>(inquiry.GetAuthenticationProvider());
    }

    [Fact]
    public void Constructor_WithBothAuthProviderAndFactory_PrefersAuthProvider()
    {
        // Arrange
        var mockAuthProvider = new MockAuthenticationProvider();
        var mockHandler = new MockHttpMessageHandler();
        var mockFactory = new MockHttpClientFactory(mockHandler);
        var validConfig = new Configuration
        {
            MerchantId = "123456",
            URL = "https://test.com",
            ConfigKey = "abcdefghij", // Valid BASE85 characters
            ConnectTimeout = "5000",
            EnableMigrationMode = "true",
            PaymentsFraudAuthUrl = "https://auth.test.com",
            PaymentsFraudApiUrl = "https://api.test.com",
            PaymentsFraudApiKey = "abcdefghij", // Valid BASE85 characters
            ApiKey = "test_api_key" // Provide API key so certificate isn't required
        };

        // Act
        var inquiry = new TestableInquiry(
            checkConfiguration: true,
            validConfig,
            mockAuthProvider,
            mockFactory,
            NullLogger.Instance
        );

        // Assert
        Assert.NotNull(inquiry);
        Assert.Same(mockAuthProvider, inquiry.GetAuthenticationProvider()); // Should prefer injected auth provider
    }

    /// <summary>
    /// Mock authentication provider for testing dependency injection.
    /// </summary>
    private class MockAuthenticationProvider : IAuthenticationProvider
    {
        public AuthResult GetAuthenticationHeaders(bool forceRefresh = false)
        {
            return AuthResult.Success(
                new System.Collections.Generic.Dictionary<string, string>
                {
                    ["Authorization"] = "Bearer mock_token"
                }
            );
        }

        public void InvalidateToken()
        {
            // Mock implementation
        }
    }

    /// <summary>
    /// Mock implementation of IHttpClientFactory for testing.
    /// </summary>
    private class MockHttpClientFactory(MockHttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string? name = null)
        {
            return new HttpClient(handler);
        }
    }

    /// <summary>
    /// Testable version of Inquiry that exposes the authentication provider for verification.
    /// </summary>
    private class TestableInquiry : Inquiry
    {
        public TestableInquiry(
            bool checkConfiguration,
            Configuration configuration,
            IAuthenticationProvider authenticationProvider,
            Microsoft.Extensions.Logging.ILogger logger
        )
            : base(checkConfiguration, configuration, authenticationProvider, logger) { }

        public TestableInquiry(
            bool checkConfiguration,
            Configuration configuration,
            IAuthenticationProvider authenticationProvider,
            IHttpClientFactory httpClientFactory,
            Microsoft.Extensions.Logging.ILogger logger
        )
            : base(checkConfiguration, configuration, authenticationProvider, httpClientFactory, logger) { }

        public IAuthenticationProvider GetAuthenticationProvider()
        {
            // Use reflection to access the private field for testing
            var field = typeof(Request).GetField(
                "_authenticationProvider",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            return (IAuthenticationProvider)field?.GetValue(this);
        }
    }
}