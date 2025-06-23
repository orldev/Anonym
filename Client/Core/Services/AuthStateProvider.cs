using System.Net.Http.Json;
using System.Security.Claims;
using Client.Core.Entities;
using Microsoft.AspNetCore.Components.Authorization;
using Toolkit.Blazor.Extensions.LocalStorage;

namespace Client.Core.Services;

/// <summary>
/// Provides authentication state by validating stored credentials against the authentication server.
/// Implements <see cref="AuthenticationStateProvider"/> for Blazor authentication integration.
/// </summary>
/// <remarks>
/// This provider:
/// - Retrieves credentials from secure local storage
/// - Implements strict expiration policies
/// - Validates tokens with the authentication server
/// - Follows security best practices (fail-secure, defense in depth)
/// - Handles all error cases gracefully
/// </remarks>
public class AuthStateProvider(ILocalStorage localStorage, HttpClient httpClient) 
    : AuthenticationStateProvider
{

    /// <summary>
    /// Gets the current authentication state asynchronously.
    /// </summary>
    /// <returns>
    /// A <see cref="Task"/> representing the authentication state, which contains
    /// a <see cref="ClaimsPrincipal"/> with user claims if authenticated.
    /// </returns>
    /// <remarks>
    /// The authentication flow:
    /// 1. Retrieves secret from local storage
    /// 2. Validates secret lifecycle (expiration)
    /// 3. For valid secrets, verifies token with authentication server
    /// 4. Returns appropriate authentication state
    /// 
    /// Security Features:
    /// - Automatic storage clearing for expired temporary secrets
    /// - Fail-secure design (returns anonymous state on any failure)
    /// - Comprehensive error handling
    /// </remarks>
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var secret = await localStorage.GetObjectAsync<Secret>(Secret.Key);
    
        // Fail fast if no secret exists
        if (secret is null)
        {
            return await Task.FromResult(MaskEmptyAsAuthenticationState());
        }

        switch (secret.Lifecycle)
        {
            // Handles expiration of temporary secrets according to security best practices
            case Lifecycle.Temporary when DateTime.UtcNow > secret.Expiration:
                // Defense in depth: Clear entire storage rather than just the secret
                await localStorage.ClearAsync();
            
                // Fail secure: Return anonymous state rather than throwing
                return await Task.FromResult(MaskEmptyAsAuthenticationState());
            
            // Handles expiration of boundless secrets according to security best practices
            case Lifecycle.Boundless when DateTime.UtcNow > secret.Expiration:
                // Fail secure: Return anonymous state rather than throwing
                return await Task.FromResult(MaskEmptyAsAuthenticationState());
            
            default:
                try
                {
                    // Validate token with authentication server
                    var response = await httpClient.PostAsJsonAsync("authentication/validate", secret.AccessToken);
        
                    // Return anonymous if validation request fails
                    if (!response.IsSuccessStatusCode)
                    {
                        return await Task.FromResult(MaskEmptyAsAuthenticationState());
                    }
        
                    // Parse validation result
                    var isValid = await response.Content.ReadFromJsonAsync<bool>();
        
                    // Return anonymous if token is invalid
                    if (!isValid) 
                    {
                        return await Task.FromResult(MaskEmptyAsAuthenticationState());
                    }
        
                    // Create authenticated state for valid user
                    var authUser = MarkUserAsAuthenticated(secret.UserIdentifier);
                    return new AuthenticationState(authUser);
                }
                catch
                {
                    // Safely handle all exceptions (network errors, etc.)
                    return await Task.FromResult(MaskEmptyAsAuthenticationState());
                }
        }
    }

    /// <summary>
    /// Creates an anonymous authentication state.
    /// </summary>
    /// <returns>An unauthenticated <see cref="AuthenticationState"/>.</returns>
    /// <remarks>
    /// Used as the fail-secure state when:
    /// - No secret exists
    /// - Secret is expired
    /// - Token validation fails
    /// - Any error occurs
    /// </remarks>
    private static AuthenticationState MaskEmptyAsAuthenticationState()
    {
        var identity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(identity);
        return new AuthenticationState(principal);
    }
    
    /// <summary>
    /// Creates an authenticated principal and notifies listeners of the state change.
    /// </summary>
    /// <param name="userIdentifier">The unique identifier of the authenticated user.</param>
    /// <returns>An authenticated <see cref="ClaimsPrincipal"/>.</returns>
    /// <remarks>
    /// - Creates a principal with NameIdentifier claim
    /// - Uses "jwt" authentication type
    /// - Notifies all authentication state subscribers
    /// </remarks>
    private ClaimsPrincipal MarkUserAsAuthenticated(string userIdentifier)
    {
        var authUser = new ClaimsPrincipal(new ClaimsIdentity(
            new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userIdentifier),
            }, "jwt"));

        var authState = Task.FromResult(new AuthenticationState(authUser));

        NotifyAuthenticationStateChanged(authState);
        return authUser;
    }
}