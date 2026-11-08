using Dabbler.Testing.Blazor.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dabbler.Testing.Blazor.Hosting;

/// <summary>
/// Extension methods for registering action tracking services.
/// </summary>
public static class TrackedActionsServiceCollectionExtensions
{
    /// <summary>
    /// Registers the tracked actions configuration and services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTrackedActions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<TrackedActionsOptions>(
            configuration.GetSection("Testing:ActionTracking"));
        return services;
    }
}
