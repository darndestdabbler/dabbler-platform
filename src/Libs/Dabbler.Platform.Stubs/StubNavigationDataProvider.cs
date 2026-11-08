using Dabbler.Components.Data.Metadata;

namespace Dabbler.Platform.Stubs;

/// <summary>
/// In-memory navigation provider for shell and smart-filter demos.
/// </summary>
public sealed class StubNavigationDataProvider : INavigationDataProvider
{
    private readonly StubNavigationDataset _dataset;

    /// <summary>
    /// Initializes a new provider with a navigation dataset.
    /// </summary>
    public StubNavigationDataProvider(StubNavigationDataset dataset)
    {
        _dataset = dataset ?? throw new ArgumentNullException(nameof(dataset));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<NavigationRecord>> GetSmartFilterRecordsAsync(
        string entityName,
        SmartFilterDefinition filter,
        int topN,
        object? context = null)
    {
        var records = _dataset.GetRecords(entityName, filter.Name);
        return Task.FromResult<IReadOnlyList<NavigationRecord>>(records.Take(topN).ToArray());
    }

    /// <inheritdoc />
    public Task<int> GetSmartFilterCountAsync(
        string entityName,
        SmartFilterDefinition filter,
        object? context = null)
    {
        return Task.FromResult(_dataset.GetRecords(entityName, filter.Name).Count);
    }
}
