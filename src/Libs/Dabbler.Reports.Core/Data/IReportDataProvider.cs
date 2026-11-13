using System.Data;

namespace Dabbler.Reports.Core.Data;

/// <summary>
/// Abstraction for executing parameterized report queries against a data source.
/// Implementations handle connection management, parameter binding, and result retrieval.
/// </summary>
public interface IReportDataProvider
{
    /// <summary>
    /// Executes a parameterized query and returns the results as a <see cref="DataTable"/>.
    /// </summary>
    /// <param name="query">The SQL query with named parameter placeholders.</param>
    /// <param name="parameters">The parameter values to bind to the query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="DataTable"/> containing the query results.</returns>
    Task<DataTable> ExecuteQueryAsync(
        string query,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a lookup query for populating parameter choice lists.
    /// </summary>
    /// <param name="query">The SQL lookup query.</param>
    /// <param name="parameters">Optional parameter values for cascading lookups.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="DataTable"/> containing the lookup results.</returns>
    Task<DataTable> ExecuteLookupAsync(
        string query,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default);
}
