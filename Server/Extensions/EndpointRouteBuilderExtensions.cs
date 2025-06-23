using Toolkit.SignalR.Reactive;

namespace Server.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IEndpointRouteBuilder"/> to map reactive transfer hubs.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the ReactiveTransferHub to the specified path "/reactiveTransfer" and requires authorization.
    /// </summary>
    /// <param name="app">The endpoint route builder to add the route to.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="app"/> is null.</exception>
    /// <remarks>
    /// This extension method configures a SignalR hub endpoint at the path "/reactiveTransfer"
    /// using the <see cref="ReactiveTransferHub"/> and enforces authorization requirements
    /// for all connections to this hub.
    /// </remarks>
    public static void MapReactiveTransferHub(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.MapHub<ReactiveTransferHub>("/reactiveTransfer")
            .RequireAuthorization();
    }
}