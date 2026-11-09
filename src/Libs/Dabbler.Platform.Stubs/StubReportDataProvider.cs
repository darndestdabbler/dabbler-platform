using System.Data;
using Dabbler.Reports.Core.Data;

namespace Dabbler.Platform.Stubs;

/// <summary>
/// In-memory report-data provider for report pages, prompt lookups, and stub hosts.
/// </summary>
public sealed class StubReportDataProvider : IReportDataProvider
{
    private readonly Dictionary<string, Func<IReadOnlyDictionary<string, object?>, DataTable>> _queryResults =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Func<IReadOnlyDictionary<string, object?>, DataTable>> _lookupResults =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers a static query result for an exact query string.
    /// </summary>
    public StubReportDataProvider AddQueryResult(string query, DataTable table)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        ArgumentNullException.ThrowIfNull(table);

        _queryResults[query] = _ => table.Copy();
        return this;
    }

    /// <summary>
    /// Registers a dynamic query result factory for an exact query string.
    /// </summary>
    public StubReportDataProvider AddQueryResult(
        string query,
        Func<IReadOnlyDictionary<string, object?>, DataTable> factory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        ArgumentNullException.ThrowIfNull(factory);

        _queryResults[query] = factory;
        return this;
    }

    /// <summary>
    /// Registers a static lookup result for an exact query string.
    /// </summary>
    public StubReportDataProvider AddLookupResult(string query, DataTable table)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        ArgumentNullException.ThrowIfNull(table);

        _lookupResults[query] = _ => table.Copy();
        return this;
    }

    /// <summary>
    /// Registers a dynamic lookup result factory for an exact query string.
    /// </summary>
    public StubReportDataProvider AddLookupResult(
        string query,
        Func<IReadOnlyDictionary<string, object?>, DataTable> factory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        ArgumentNullException.ThrowIfNull(factory);

        _lookupResults[query] = factory;
        return this;
    }

    /// <inheritdoc />
    public Task<DataTable> ExecuteQueryAsync(
        string query,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Resolve(query, parameters, _queryResults, "query"));
    }

    /// <inheritdoc />
    public Task<DataTable> ExecuteLookupAsync(
        string query,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Resolve(query, parameters ?? EmptyParameters.Instance, _lookupResults, "lookup"));
    }

    private static DataTable Resolve(
        string query,
        IReadOnlyDictionary<string, object?> parameters,
        IReadOnlyDictionary<string, Func<IReadOnlyDictionary<string, object?>, DataTable>> registry,
        string kind)
    {
        if (!registry.TryGetValue(query, out var factory))
        {
            throw new InvalidOperationException($"No stub {kind} result was registered for: {query}");
        }

        return factory(parameters);
    }

    private sealed class EmptyParameters : IReadOnlyDictionary<string, object?>
    {
        public static EmptyParameters Instance { get; } = new();

        public IEnumerable<string> Keys => [];
        public IEnumerable<object?> Values => [];
        public int Count => 0;
        public object? this[string key] => throw new KeyNotFoundException(key);

        public bool ContainsKey(string key) => false;
        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() =>
            Enumerable.Empty<KeyValuePair<string, object?>>().GetEnumerator();
        public bool TryGetValue(string key, out object? value)
        {
            value = null;
            return false;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
