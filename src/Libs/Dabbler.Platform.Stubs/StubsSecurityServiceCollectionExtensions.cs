using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dabbler.Platform.Stubs;

/// <summary>
/// DI helpers for wiring a stub authentication-state provider into a host or bUnit context.
/// </summary>
public static class StubsSecurityServiceCollectionExtensions
{
    /// <summary>
    /// Registers a mutable in-memory authentication-state provider for tests or demo hosts.
    /// </summary>
    public static IServiceCollection AddStubAuthentication(
        this IServiceCollection services,
        StubUserContext userContext)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(userContext);

        services.TryAddScoped<StubAuthenticationStateProvider>(_ => new StubAuthenticationStateProvider(userContext));
        services.TryAddScoped<AuthenticationStateProvider>(
            sp => sp.GetRequiredService<StubAuthenticationStateProvider>());
        services.AddAuthorizationCore();
        return services;
    }
}
