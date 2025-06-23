using System.IO.Compression;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.SignalR;
using Toolkit.Authentication.JwtBearer;
using Toolkit.SignalR.Reactive.Extensions;

namespace Server.Extensions;

/// <summary>
/// Adds and configures SignalR services with reactive transfer capabilities
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures SignalR with reactive transfer support including authentication,
    /// message packing, compression, and caching
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <param name="configureHub">Optional action to configure HubOptions</param>
    /// <param name="configureJwt">Optional action to configure JWT options</param>
    /// <remarks>
    /// Includes:
    /// - JWT authentication with SignalR WebSocket support
    /// - MessagePack protocol for binary serialization
    /// - Response compression for binary streams
    /// - Memory cache configuration
    /// </remarks>
    public static void AddReactiveTransferSignalR(
        this IServiceCollection services, 
        IConfiguration configuration,
        Action<HubOptions>? configureHub = null,
        Action<JwtBearerOptions>? configureJwt = null)
    {
        // Configure JWT authentication for SignalR
        services.AddAuthJwtBearer(configuration, options =>
        {
            // Allow token in query string for WebSocket connections
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && 
                        path.StartsWithSegments("/reactiveTransfer"))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };
            
            // Apply additional JWT configuration if provided
            configureJwt?.Invoke(options);
        });
        
        // Configure SignalR with MessagePack protocol
        services.AddSignalR(options =>
        {
            // Default configuration optimized for binary transfers
            options.MaximumReceiveMessageSize = 1024 * 92;  // 92KB
            options.StreamBufferCapacity = 100;  // Number of chunks to buffer
            options.EnableDetailedErrors = true;  // Better error messages
            
            // Apply additional hub configuration if provided
            configureHub?.Invoke(options);
        }).AddMessagePackProtocol();
        
        
        // Add response compression for binary streams
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat([
                "application/wasm", // For .wasm files
                "application/octet-stream" // For .dll files
            ]);
        });
        
        // Configure Brotli compression level
        services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
        });

        // Add caching service with configuration
        services.AddCacheService(configuration);
    }
}