using Client.Core.DelegatingHandlers;
using Client.Core.Entities;
using Client.Core.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using Toolkit.Blazor.Extensions.LocalStorage;
using Toolkit.Cryptography.Entities;
using Toolkit.SignalR.Reactive.Extensions;

namespace Client.Core.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/> to configure application services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds and configures the local storage service with default encryption settings.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <remarks>
    /// Configures local storage with:
    /// - Default passphrase
    /// - Fixed initialization vector
    /// - Salt value
    /// - 1000 key derivation iterations
    /// - 16-byte key length
    /// - SHA384 hashing algorithm
    /// </remarks>
    public static void AddLocalStorage(this IServiceCollection services)
    {
        services.AddLocalStorage(o =>
        {
            o.Passphrase = "123456";
            o.IV = "abcede0123456789";
            o.Salt = "0123456789abcede";
            o.Iterations = 1000;
            o.DesiredKeyLength = 16;
            o.HashMethod = HashAlgo.SHA384;
        });
    }
    
    /// <summary>
    /// Configures the authorization services for the application.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <remarks>
    /// Sets up:
    /// - Custom <see cref="AuthStateProvider"/> as the authentication state provider
    /// - Required options services
    /// - Core authorization services
    /// </remarks>
    public static void AddAuthorization(this IServiceCollection services)
    {
        services.AddScoped<AuthenticationStateProvider, AuthStateProvider>();
        services.AddOptions();
        services.AddAuthorizationCore();
    }
    
    /// <summary>
    /// Configures an authenticated HTTP client for server communication.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="baseAddress">The base address for the HTTP client.</param>
    /// <remarks>
    /// Sets up:
    /// - Authorization handler for automatic token injection
    /// - Named HTTP client with specified base address
    /// - Scoped client instance for dependency injection
    /// </remarks>
    public static void AddAuthHttpClient(this IServiceCollection services, 
        string baseAddress)
    {
        services.AddTransient<AuthorizationDelegatingHandler>();
        
        services.AddHttpClient("Server", client =>
            {
                client.BaseAddress = new Uri(baseAddress);
            })
            .AddHttpMessageHandler<AuthorizationDelegatingHandler>();
        
        services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>()
                .CreateClient("Server"));
    }
    
    /// <summary>
    /// Configures and establishes a SignalR Hub connection with authentication.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="baseAddress">The base address for the Hub connection.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <remarks>
    /// Performs the following setup:
    /// 1. Retrieves access token from local storage
    /// 2. Configures Hub connection with:
    ///    - Automatic token authentication
    ///    - MessagePack protocol for efficient serialization
    ///    - Information-level logging
    ///    - Automatic reconnection
    /// 3. Adds the Hub connection as a singleton service
    /// 4. Configures the reactive pipeline
    /// </remarks>
    public static async Task AddHubConnectionAsync(this IServiceCollection services, string baseAddress)
    {
        var sp = services.BuildServiceProvider();
        var localStorage = sp.GetRequiredService<ILocalStorage>();
        var secret = await localStorage.GetObjectAsync<Secret>(Secret.Key);
        
        var hubConnection = new HubConnectionBuilder()
            .WithUrl(baseAddress + "reactiveTransfer", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(secret?.AccessToken);
            })
            .AddMessagePackProtocol()
            .ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Information))
            .WithAutomaticReconnect()
            .Build();

        services.AddSingleton(hubConnection);
        services.AddReactivePipeline();
    }
}