using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace KountRisTest.Authentication;

/// <summary>
/// Mock HTTP message handler for testing authentication scenarios.
/// Allows setting up responses and exceptions for controlled testing.
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    private HttpResponseMessage _response;
    private Exception _exception;

    /// <summary>
    /// Sets up a mock HTTP response with the specified status code and content.
    /// </summary>
    /// <param name="statusCode">HTTP status code to return</param>
    /// <param name="content">Response content</param>
    public void SetupResponse(int statusCode, string content)
    {
        _response = new HttpResponseMessage((HttpStatusCode)statusCode)
        {
            Content = new StringContent(content)
        };
        _exception = null;
    }

    /// <summary>
    /// Sets up the mock to throw an exception when SendAsync is called.
    /// </summary>
    /// <param name="exception">Exception to throw</param>
    public void SetupException(Exception exception)
    {
        _exception = exception;
        _response = null;
    }

    /// <summary>
    /// Mock implementation of SendAsync that returns the configured response or throws exception.
    /// </summary>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_exception != null)
            throw _exception;
                
        if (_response != null)
            return Task.FromResult(_response);
                
        // Default response if nothing configured
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"access_token\":\"test_token\",\"token_type\":\"Bearer\",\"expires_in\":3600}")
        });
    }

    /// <summary>
    /// Disposes resources used by the mock handler.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _response?.Dispose();
        }
        base.Dispose(disposing);
    }
}