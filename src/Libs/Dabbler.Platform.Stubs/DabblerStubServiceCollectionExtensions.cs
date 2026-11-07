using Microsoft.Extensions.DependencyInjection;

namespace Dabbler.Platform.Stubs;

/// <summary>
/// DI helpers for wiring the full platform stub surface into a host or bUnit context.
/// </summary>
public static class DabblerStubServiceCollectionExtensions
{
    /// <summary>
    /// Registers the full platform stub surface — metadata, navigation, catalog, usage-tracking,
    /// and report providers — for a stub-backed host.
    /// Entity-specific grid/combo providers must still be registered individually via
    /// <see cref="StubsComponentsServiceCollectionExtensions.AddStubGridDataProvider{T}"/> and
    /// <see cref="StubsComponentsServiceCollectionExtensions.AddStubComboBoxProvider{T}"/>.
    /// </summary>
    public static IServiceCollection AddDabblerPlatformStubs(
        this IServiceCollection services,
        StubPlatformDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(definition);

        services.AddSingleton(definition);

        // Components sub-package helpers
        services.AddStubEntityMetadataProvider(definition.Entities);
        services.AddStubNavigationDataProvider(definition.Navigation);

        // Reports sub-package helpers (conditional on non-empty collections)
        if (definition.CatalogEntries.Count > 0)
            services.AddStubArtifactCatalogProvider(definition.CatalogEntries);

        if (definition.UsageRecords.Count > 0)
            services.AddStubUsageTrackingProvider(definition.UsageRecords);

        return services;
    }
}
