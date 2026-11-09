using System.Data;
using System.Text.RegularExpressions;
using Dabbler.Reports.Core.Layout;
using Dabbler.Reports.Core.Rdf;

namespace Dabbler.Reports.Core.Data;

/// <summary>
/// Scans a report definition for subreport elements, loads their RDF files,
/// executes their data queries via <see cref="IReportDataProvider"/>, and
/// returns a <see cref="PrefetchedSubreportResolver"/> ready for use with
/// <see cref="BandLayoutEngine.ComposePages(ReportDefinition, DataTable, Dictionary{string, object?}?, ISubreportResolver?)"/>.
/// </summary>
/// <remarks>
/// <para>This class solves the sync/async mismatch between
/// <see cref="ISubreportResolver.Resolve"/> (synchronous, called during layout)
/// and <see cref="IReportDataProvider.ExecuteQueryAsync"/> (asynchronous).
/// All data is fetched asynchronously before layout begins, then served
/// synchronously from a dictionary during composition.</para>
/// <para>Subreports whose parameter expressions reference row-level fields
/// (e.g., <c>[CategoryId]</c>) cannot be pre-fetched because the field values
/// are unknown until the layout engine iterates data rows. These paths are
/// reported in <see cref="SubreportPrefetchResult.SkippedPaths"/>; provide a
/// fallback <see cref="ISubreportResolver"/> via <see cref="CompositeSubreportResolver"/>
/// for those cases.</para>
/// </remarks>
public class SubreportDataPrefetcher
{
    private static readonly Regex FieldRefPattern = new(@"\[(\w+)\]", RegexOptions.Compiled);

    private readonly Func<string, string> _rdfLoader;
    private readonly Func<string, IReportDataProvider> _dataProviderFactory;
    private readonly RdfParser _parser;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubreportDataPrefetcher"/> class.
    /// </summary>
    /// <param name="rdfLoader">
    /// A function that loads subreport RDF JSON content given a subreport path.
    /// For file-system loading, use <c>path => File.ReadAllText(Path.Combine(baseDir, path))</c>.
    /// </param>
    /// <param name="dataProviderFactory">
    /// A factory that returns an <see cref="IReportDataProvider"/> for a given connection name
    /// (from <see cref="DataSourceDefinition.ConnectionName"/>).
    /// </param>
    /// <param name="parser">
    /// Optional <see cref="RdfParser"/> instance. If <c>null</c>, a default instance is created.
    /// </param>
    public SubreportDataPrefetcher(
        Func<string, string> rdfLoader,
        Func<string, IReportDataProvider> dataProviderFactory,
        RdfParser? parser = null)
    {
        ArgumentNullException.ThrowIfNull(rdfLoader);
        ArgumentNullException.ThrowIfNull(dataProviderFactory);

        _rdfLoader = rdfLoader;
        _dataProviderFactory = dataProviderFactory;
        _parser = parser ?? new RdfParser();
    }

    /// <summary>
    /// Scans the report definition for subreport elements, loads their RDF files,
    /// and fetches their data. Subreports with row-dependent parameters are skipped.
    /// </summary>
    /// <param name="rootReport">The root report definition to scan.</param>
    /// <param name="parentParameters">
    /// The resolved parameter values for the root report, used to evaluate
    /// static subreport parameter expressions like <c>{@StartDate}</c>.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A <see cref="SubreportPrefetchResult"/> containing the pre-fetched resolver
    /// and a list of skipped paths.
    /// </returns>
    public async Task<SubreportPrefetchResult> PrefetchAsync(
        ReportDefinition rootReport,
        IReadOnlyDictionary<string, object?>? parentParameters = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rootReport);

        var subreports = new Dictionary<string, (ReportDefinition, DataTable)>(
            StringComparer.OrdinalIgnoreCase);
        var skippedPaths = new List<string>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await ScanAndFetchAsync(
            rootReport, parentParameters, subreports, skippedPaths,
            visited, depth: 0, cancellationToken).ConfigureAwait(false);

        var resolver = new PrefetchedSubreportResolver(subreports);
        return new SubreportPrefetchResult(resolver, skippedPaths);
    }

    private async Task ScanAndFetchAsync(
        ReportDefinition report,
        IReadOnlyDictionary<string, object?>? parameters,
        Dictionary<string, (ReportDefinition, DataTable)> subreports,
        List<string> skippedPaths,
        HashSet<string> visited,
        int depth,
        CancellationToken cancellationToken)
    {
        if (depth >= BandLayoutEngine.MaxSubreportDepth)
            return;

        var subreportElements = FindSubreportElements(report);
        if (subreportElements.Count == 0)
            return;

        var fetchTasks = new List<Task>();

        foreach (var element in subreportElements)
        {
            var path = element.SubreportPath!;

            if (!visited.Add(path))
                continue; // Already processed this path

            // Check if any parameter expression is row-dependent
            if (HasRowDependentParameters(element.SubreportParameters))
            {
                // Load and parse the definition so it's available in the resolver,
                // but use an empty DataTable since we can't fetch the right data
                try
                {
                    var json = _rdfLoader(path);
                    var subDef = _parser.Parse(json);
                    subreports[path] = (subDef, new DataTable());
                    skippedPaths.Add(path);

                    // Still scan for nested subreports within the skipped definition
                    await ScanAndFetchAsync(
                        subDef, null, subreports, skippedPaths,
                        visited, depth + 1, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    skippedPaths.Add(path);
                }

                continue;
            }

            // Static parameters — we can pre-fetch
            fetchTasks.Add(FetchSubreportAsync(
                element, parameters, subreports, skippedPaths,
                visited, depth, cancellationToken));
        }

        if (fetchTasks.Count > 0)
            await Task.WhenAll(fetchTasks).ConfigureAwait(false);
    }

    private async Task FetchSubreportAsync(
        ElementDefinition element,
        IReadOnlyDictionary<string, object?>? parentParameters,
        Dictionary<string, (ReportDefinition, DataTable)> subreports,
        List<string> skippedPaths,
        HashSet<string> visited,
        int depth,
        CancellationToken cancellationToken)
    {
        var path = element.SubreportPath!;

        try
        {
            // Load and parse the subreport definition
            var json = _rdfLoader(path);
            var subReport = _parser.Parse(json);

            // Determine which data source to use
            var dataSource = ResolveDataSource(subReport, element.SubreportDataSourceName);

            // Build query parameters from static expressions
            var queryParams = EvaluateStaticParameters(
                element.SubreportParameters, parentParameters);

            // Execute the query
            var provider = _dataProviderFactory(dataSource.ConnectionName);
            var data = await provider.ExecuteQueryAsync(
                dataSource.Query, queryParams, cancellationToken).ConfigureAwait(false);

            lock (subreports)
            {
                subreports[path] = (subReport, data);
            }

            // Recursively scan for nested subreports
            await ScanAndFetchAsync(
                subReport, queryParams, subreports, skippedPaths,
                visited, depth + 1, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            lock (skippedPaths)
            {
                if (!skippedPaths.Contains(path))
                    skippedPaths.Add(path);
            }
        }
    }

    /// <summary>
    /// Finds all subreport elements across all bands in a report definition.
    /// </summary>
    private static List<ElementDefinition> FindSubreportElements(ReportDefinition report)
    {
        var result = new List<ElementDefinition>();

        foreach (var band in report.Bands)
        {
            foreach (var element in band.Elements)
            {
                if (element.Type == ElementType.Subreport
                    && !string.IsNullOrWhiteSpace(element.SubreportPath))
                {
                    result.Add(element);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Determines whether any parameter expression references a data row field
    /// (e.g., <c>[FieldName]</c>), which cannot be evaluated at prefetch time.
    /// </summary>
    private static bool HasRowDependentParameters(
        Dictionary<string, string>? parameterExpressions)
    {
        if (parameterExpressions is null || parameterExpressions.Count == 0)
            return false;

        foreach (var expression in parameterExpressions.Values)
        {
            if (FieldRefPattern.IsMatch(expression))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Resolves the data source for a subreport, using the specified name or
    /// falling back to the first data source in the subreport's definition.
    /// </summary>
    private static DataSourceDefinition ResolveDataSource(
        ReportDefinition subReport, string? dataSourceName)
    {
        if (subReport.DataSources.Count == 0)
            throw new InvalidOperationException(
                "Subreport has no data sources defined.");

        if (string.IsNullOrWhiteSpace(dataSourceName))
            return subReport.DataSources[0];

        var ds = subReport.DataSources.Find(
            d => string.Equals(d.Name, dataSourceName, StringComparison.OrdinalIgnoreCase));

        return ds ?? throw new InvalidOperationException(
            $"Data source '{dataSourceName}' not found in subreport. " +
            $"Available: {string.Join(", ", subReport.DataSources.Select(d => d.Name))}.");
    }

    /// <summary>
    /// Evaluates static parameter expressions (literal values and parent parameter
    /// references like <c>{@ParamName}</c>) into query parameter values.
    /// </summary>
    private static Dictionary<string, object?> EvaluateStaticParameters(
        Dictionary<string, string>? parameterExpressions,
        IReadOnlyDictionary<string, object?>? parentParameters)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        if (parameterExpressions is null)
            return result;

        foreach (var (name, expression) in parameterExpressions)
        {
            result[name] = EvaluateStaticExpression(expression, parentParameters);
        }

        return result;
    }

    /// <summary>
    /// Evaluates a single static expression. Handles <c>{@ParamName}</c> references
    /// and literal values. Does not handle field references or complex expressions.
    /// </summary>
    private static object? EvaluateStaticExpression(
        string expression, IReadOnlyDictionary<string, object?>? parentParameters)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return null;

        var trimmed = expression.Trim();

        // Parameter reference: {@ParamName}
        if (trimmed.StartsWith("{@") && trimmed.EndsWith("}"))
        {
            var paramName = trimmed[2..^1];
            if (parentParameters is not null
                && parentParameters.TryGetValue(paramName, out var value))
            {
                return value;
            }
            return null;
        }

        // Quoted string literal
        if ((trimmed.StartsWith('"') && trimmed.EndsWith('"'))
            || (trimmed.StartsWith('\'') && trimmed.EndsWith('\'')))
        {
            return trimmed[1..^1];
        }

        // Numeric literal
        if (int.TryParse(trimmed, out var intVal))
            return intVal;
        if (double.TryParse(trimmed, out var dblVal))
            return dblVal;

        // Return as string literal
        return trimmed;
    }
}
