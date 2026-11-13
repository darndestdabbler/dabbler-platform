using System.Data;
using Dabbler.Reports.Core.Rdf;

namespace Dabbler.Reports.Core.Data;

/// <summary>
/// Resolves subreport definitions and data at runtime. Implementations load
/// the subreport RDF file, execute its data query with the given parameters,
/// and return the parsed definition and result set.
/// </summary>
/// <remarks>
/// <para>This interface decouples the layout engine from file I/O and data access.
/// The consuming application provides an implementation that knows how to locate
/// RDF files and execute queries (e.g., via gRPC, EF Core, or in-memory test data).</para>
/// <para>For unit testing, use a dictionary-based implementation that maps subreport
/// paths to pre-built (ReportDefinition, DataTable) pairs.</para>
/// </remarks>
public interface ISubreportResolver
{
    /// <summary>
    /// Resolves a subreport by loading its definition and executing its data query.
    /// </summary>
    /// <param name="subreportPath">
    /// The path to the subreport RDF file, as specified in the parent element's
    /// <see cref="ElementDefinition.SubreportPath"/>.
    /// </param>
    /// <param name="parameters">
    /// Parameter values to pass to the subreport, with expressions already evaluated
    /// in the parent report's context.
    /// </param>
    /// <returns>
    /// A tuple containing the parsed <see cref="ReportDefinition"/> and the query
    /// result <see cref="DataTable"/> for the subreport.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="subreportPath"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">The subreport could not be resolved.</exception>
    (ReportDefinition Report, DataTable Data) Resolve(
        string subreportPath,
        IReadOnlyDictionary<string, object?> parameters);
}
