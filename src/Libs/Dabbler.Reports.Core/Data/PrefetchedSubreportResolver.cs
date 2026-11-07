using System.Data;
using Dabbler.Reports.Core.Rdf;

namespace Dabbler.Reports.Core.Data;

/// <summary>
/// An <see cref="ISubreportResolver"/> backed by a pre-fetched dictionary of
/// subreport definitions and data. Used when subreport data has been loaded
/// asynchronously before layout composition begins.
/// </summary>
/// <remarks>
/// <para>This resolver performs no I/O — all data must be provided upfront.
/// Use <see cref="SubreportDataPrefetcher"/> to build the dictionary
/// automatically from RDF files and an <see cref="IReportDataProvider"/>.</para>
/// </remarks>
public class PrefetchedSubreportResolver : ISubreportResolver
{
    private readonly IReadOnlyDictionary<string, (ReportDefinition Report, DataTable Data)> _subreports;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrefetchedSubreportResolver"/> class.
    /// </summary>
    /// <param name="subreports">
    /// A dictionary mapping subreport paths to their parsed definitions and query results.
    /// Keys are compared case-insensitively.
    /// </param>
    public PrefetchedSubreportResolver(
        IReadOnlyDictionary<string, (ReportDefinition Report, DataTable Data)> subreports)
    {
        ArgumentNullException.ThrowIfNull(subreports);
        _subreports = subreports;
    }

    /// <inheritdoc />
    public (ReportDefinition Report, DataTable Data) Resolve(
        string subreportPath,
        IReadOnlyDictionary<string, object?> parameters)
    {
        ArgumentNullException.ThrowIfNull(subreportPath);

        if (_subreports.TryGetValue(subreportPath, out var result))
            return result;

        // Try case-insensitive fallback if the dictionary doesn't already use
        // a case-insensitive comparer.
        foreach (var kvp in _subreports)
        {
            if (string.Equals(kvp.Key, subreportPath, StringComparison.OrdinalIgnoreCase))
                return kvp.Value;
        }

        var available = string.Join(", ", _subreports.Keys.Select(k => $"'{k}'"));
        throw new InvalidOperationException(
            $"Subreport path '{subreportPath}' was not found in the pre-fetched data. " +
            $"Available paths: [{available}].");
    }
}
