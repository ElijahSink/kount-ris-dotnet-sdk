using System.Collections.Generic;
using Kount.Ris.Authentication;
using KountRisSdk.Kount.Ris.Authentication;
using Xunit;

namespace KountRisTest.Authentication
{
    /// <summary>
    /// Unit tests for the AuthResult class.
    /// </summary>
    public class AuthResultTests
    {
        [Fact]
        public void Success_WithHeaders_ReturnsSuccessResult()
        {
            // Arrange
            var headers = new Dictionary<string, string>
            {
                ["Authorization"] = "Bearer test_token"
            };

            // Act
            var result = AuthResult.Success(headers);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Headers);
            Assert.Equal("Bearer test_token", result.Headers["Authorization"]);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public void Success_WithNullHeaders_ReturnsSuccessWithEmptyHeaders()
        {
            // Act
            var result = AuthResult.Success(null);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Headers);
            Assert.Empty(result.Headers);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public void Failure_WithErrorMessage_ReturnsFailureResult()
        {
            // Arrange
            var errorMessage = "Authentication failed";

            // Act
            var result = AuthResult.Failure(errorMessage);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Null(result.Headers);
            Assert.Equal("Authentication failed", result.ErrorMessage);
        }

        [Fact]
        public void Failure_WithNullErrorMessage_ReturnsFailureWithDefaultMessage()
        {
            // Act
            var result = AuthResult.Failure(null);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Null(result.Headers);
            Assert.Equal("Unknown authentication error", result.ErrorMessage);
        }

        [Fact]
        public void Failure_WithEmptyErrorMessage_ReturnsFailureWithDefaultMessage()
        {
            // Act
            var result = AuthResult.Failure("");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Null(result.Headers);
            Assert.Equal("Unknown authentication error", result.ErrorMessage);
        }
    }
}