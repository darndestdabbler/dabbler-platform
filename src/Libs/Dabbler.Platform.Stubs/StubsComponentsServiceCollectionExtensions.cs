using Dabbler.Components.Data;
using Dabbler.Components.Data.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dabbler.Platform.Stubs;

/// <summary>
/// DI helpers for wiring Dabbler.Components stub providers into a host or bUnit context.
/// </summary>
public static class StubsComponentsServiceCollectionExtensions
{
    /// <summary>
    /// Registers an in-memory grid-data provider for <typeparamref name="T"/>.
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
    /// Registers an in-memory combo-box provider for <typeparamref name="T"/>.
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
    /// Registers an in-memory entity-metadata provider.
    /// </summary>
    public static IServiceCollection AddStubEntityMetadataProvider(
        this IServiceCollection services,
        IEnumerable<EntityMetadata> entities)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(entities);

        services.TryAddSingleton<StubEntityMetadataProvider>(_ => new StubEntityMetadataProvider(entities));
        services.TryAddSingleton<IEntityMetadataProvider>(
            sp => sp.GetRequiredService<StubEntityMetadataProvider>());
        return services;
    }

    /// <summary>
    /// Registers an in-memory navigation provider.
    /// </summary>
    public static IServiceCollection AddStubNavigationDataProvider(
        this IServiceCollection services,
        StubNavigationDataset dataset)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(dataset);

        services.TryAddSingleton<StubNavigationDataProvider>(_ => new StubNavigationDataProvider(dataset));
        services.TryAddSingleton<INavigationDataProvider>(
            sp => sp.GetRequiredService<StubNavigationDataProvider>());
        return services;
    }
}
