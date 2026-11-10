using Dabbler.Components.Data.Metadata;

namespace Dabbler.Platform.Stubs;

/// <summary>
/// In-memory record store for stub navigation results.
/// </summary>
public sealed class StubNavigationDataset
{
    private readonly Dictionary<string, Dictionary<string, IReadOnlyList<NavigationRecord>>> _records =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Adds or replaces records for a given entity and smart-filter name.
    /// </summary>
    public StubNavigationDataset AddRecords(
        string entityName,
        string filterName,
        IEnumerable<NavigationRecord> records)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);
        ArgumentException.ThrowIfNullOrWhiteSpace(filterName);
        ArgumentNullException.ThrowIfNull(records);

        if (!_records.TryGetValue(entityName, out var byFilter))
        {
            byFilter = new Dictionary<string, IReadOnlyList<NavigationRecord>>(StringComparer.OrdinalIgnoreCase);
            _records[entityName] = byFilter;
        }

        byFilter[filterName] = records.ToArray();
        return this;
    }

    /// <summary>
    /// Resolves records for a given entity and smart-filter name.
    /// </summary>
    public IReadOnlyList<NavigationRecord> GetRecords(string entityName, string filterName)
    {
        if (_records.TryGetValue(entityName, out var byFilter)
            && byFilter.TryGetValue(filterName, out var records))
        {
            return records;
        }

        return [];
    }
}
