using Dabbler.Components.Data.Metadata;
using Dabbler.Reports.Core.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dabbler.Platform.Stubs;

/// <summary>
/// DI helpers for wiring Dabbler.Reports stub providers into a host or bUnit context.
/// </summary>
public static class StubsReportsServiceCollectionExtensions
{
    /// <summary>
    /// Registers an in-memory report-data provider.
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
    /// Registers an in-memory artifact-catalog provider.
    /// </summary>
    public static IServiceCollection AddStubArtifactCatalogProvider(
        this IServiceCollection services,
        IEnumerable<ArtifactCatalogEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(entries);

        services.TryAddSingleton<StubArtifactCatalogProvider>(_ => new StubArtifactCatalogProvider(entries));
        services.TryAddSingleton<IArtifactCatalogProvider>(
            sp => sp.GetRequiredService<StubArtifactCatalogProvider>());
        return services;
    }

    /// <summary>
    /// Registers an in-memory usage-tracking provider.
    /// </summary>
    public static IServiceCollection AddStubUsageTrackingProvider(
        this IServiceCollection services,
        IEnumerable<ArtifactUsageRecord>? records = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<StubUsageTrackingProvider>(_ => new StubUsageTrackingProvider(records));
        services.TryAddSingleton<IUsageTrackingProvider>(
            sp => sp.GetRequiredService<StubUsageTrackingProvider>());
        return services;
    }
}
