using System.Net.Http.Headers;
using Client.Core.Entities;
using Toolkit.Blazor.Extensions.LocalStorage;

namespace Client.Core.DelegatingHandlers;

/// <summary>
/// A custom <see cref="DelegatingHandler"/> that adds authorization headers to HTTP requests.
/// </summary>
/// <remarks>
/// This handler:
/// - Retrieves an access token from local storage
/// - Adds it as a Bearer token to the Authorization header
/// - Ensures the request succeeds (throws on failure)
/// - Should be registered in the HTTP client pipeline
/// </remarks>
public class AuthorizationDelegatingHandler(ILocalStorage localStorage)
    : DelegatingHandler
{
    /// <summary>
    /// Sends the HTTP request with authorization headers.
    /// </summary>
    /// <param name="request">The HTTP request message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The HTTP response message.</returns>
    /// <exception cref="HttpRequestException">
    /// Thrown when the response status code indicates failure.
    /// </exception>
    /// <remarks>
    /// The method:
    /// 1. Retrieves the secret from local storage using <see cref="Secret.Key"/>
    /// 2. Adds the access token as a Bearer token
    /// 3. Sends the request
    /// 4. Validates the response was successful
    /// </remarks>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Retrieve the secret from local storage
        var secret = await localStorage.GetObjectAsync<Secret>(Secret.Key, cancellationToken);
        
        // Add authorization header if token exists
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            secret?.AccessToken);

        // Send the request
        var httpResponseMessage = await base.SendAsync(
            request,
            cancellationToken);

        // Ensure the request succeeded
        httpResponseMessage.EnsureSuccessStatusCode();

        return httpResponseMessage;
    }
}