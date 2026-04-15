using Dabbler.Components.Data;
using Dabbler.Components.Data.Metadata;
using Dabbler.Reports.Core.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dabbler.Platform.Stubs;

/// <summary>
/// DI helpers for wiring the supported stub platform surface into a host.
/// </summary>
public static class DabblerStubServiceCollectionExtensions
{
    /// <summary>
    /// Registers metadata, navigation, catalog, and usage-tracking stubs for a host.
    /// </summary>
    public static IServiceCollection AddDabblerPlatformStubs(
        this IServiceCollection services,
        StubPlatformDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(definition);

        services.AddSingleton(definition);
        services.AddSingleton<StubEntityMetadataProvider>(_ => new StubEntityMetadataProvider(definition.Entities));
        services.AddSingleton<IEntityMetadataProvider>(sp => sp.GetRequiredService<StubEntityMetadataProvider>());
        services.AddSingleton<StubNavigationDataProvider>(_ => new StubNavigationDataProvider(definition.Navigation));
        services.AddSingleton<INavigationDataProvider>(sp => sp.GetRequiredService<StubNavigationDataProvider>());

        if (definition.CatalogEntries.Count > 0)
        {
            services.AddSingleton<StubArtifactCatalogProvider>(
                _ => new StubArtifactCatalogProvider(definition.CatalogEntries));
            services.AddSingleton<IArtifactCatalogProvider>(
                sp => sp.GetRequiredService<StubArtifactCatalogProvider>());
        }

        if (definition.UsageRecords.Count > 0)
        {
            services.AddSingleton<StubUsageTrackingProvider>(
                _ => new StubUsageTrackingProvider(definition.UsageRecords));
            services.AddSingleton<IUsageTrackingProvider>(
                sp => sp.GetRequiredService<StubUsageTrackingProvider>());
        }

        return services;
    }

    /// <summary>
    /// Registers a supported in-memory grid provider for <typeparamref name="T"/>.
    /// </summary>
    public static IServiceCollection AddStubGridDataProvider<T>(
        this IServiceCollection services,
        IEnumerable<T> items,
        Action<StubGridDataProvider<T>>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(items);

        var provider = new StubGridDataProvider<T>(items);
        configure?.Invoke(provider);
        services.AddSingleton(provider);
        services.AddSingleton<IGridDataProvider<T>>(sp => sp.GetRequiredService<StubGridDataProvider<T>>());
        return services;
    }

    /// <summary>
    /// Registers a supported in-memory combo-box provider for <typeparamref name="T"/>.
    /// </summary>
    public static IServiceCollection AddStubComboBoxProvider<T>(
        this IServiceCollection services,
        IEnumerable<T> items,
        Func<T, string>? displaySelector = null,
        Func<T, object>? keySelector = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(items);

        services.AddSingleton<IComboBoxItemProvider<T>>(
            _ => new StubComboBoxItemProvider<T>(items, displaySelector, keySelector));
        return services;
    }

    /// <summary>
    /// Registers a supported in-memory report provider.
    /// </summary>
    public static IServiceCollection AddStubReportDataProvider(
        this IServiceCollection services,
        Action<StubReportDataProvider> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var provider = new StubReportDataProvider();
        configure(provider);
        services.AddSingleton(provider);
        services.AddSingleton<IReportDataProvider>(sp => sp.GetRequiredService<StubReportDataProvider>());
        return services;
    }

    /// <summary>
    /// Registers a stub authentication-state provider for tests or demo hosts.
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
