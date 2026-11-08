using System.Data;
using Dabbler.Reports.Core.Layout;
using Dabbler.Reports.Core.Rdf;

namespace Dabbler.Reports.Core.Data;

/// <summary>
/// Binds parameter values to a report query and executes it via an
/// <see cref="IReportDataProvider"/>. Handles optional parameter logic
/// and conditional WHERE clause construction.
/// </summary>
public class ReportDataEngine
{
    private readonly IReportDataProvider _dataProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportDataEngine"/> class.
    /// </summary>
    /// <param name="dataProvider">The data provider for query execution.</param>
    public ReportDataEngine(IReportDataProvider dataProvider)
    {
        ArgumentNullException.ThrowIfNull(dataProvider);
        _dataProvider = dataProvider;
    }

    /// <summary>
    /// Executes the report query with the given parameter values.
    /// </summary>
    /// <param name="dataSource">The data source definition from the RDF.</param>
    /// <param name="parameterValues">The resolved parameter values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="DataTable"/> containing the report data.</returns>
    public Task<DataTable> ExecuteReportQueryAsync(
        DataSourceDefinition dataSource,
        IReadOnlyList<ParameterValue> parameterValues,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dataSource);
        ArgumentNullException.ThrowIfNull(parameterValues);

        var parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var pv in parameterValues)
        {
            parameters[pv.Name] = pv.Value;
        }

        return _dataProvider.ExecuteQueryAsync(dataSource.Query, parameters, cancellationToken);
    }

    /// <summary>
    /// Composes a report with full subreport support. Handles subreport data
    /// pre-fetching asynchronously, then runs the synchronous layout engine.
    /// </summary>
    /// <param name="report">The parsed root report definition.</param>
    /// <param name="data">The root report's data.</param>
    /// <param name="parameters">Resolved parameter values for the root report.</param>
    /// <param name="subreportRdfLoader">
    /// A function that loads subreport RDF JSON content given a path.
    /// Required if the report contains subreport elements. Pass <c>null</c> if
    /// no subreports are expected.
    /// </param>
    /// <param name="dataProviderFactory">
    /// A factory that returns an <see cref="IReportDataProvider"/> for a given
    /// connection name. Required if the report contains subreport elements.
    /// If <c>null</c>, the constructor-provided data provider is used for all connections.
    /// </param>
    /// <param name="fallbackResolver">
    /// Optional fallback resolver for subreports with row-dependent parameters
    /// that cannot be pre-fetched. If provided and needed, it is chained after
    /// the pre-fetched resolver via <see cref="CompositeSubreportResolver"/>.
    /// </param>
    /// <param name="textMeasurer">
    /// Optional text measurer for computing actual text height when elements have
    /// <see cref="ElementDefinition.CanGrow"/> or <see cref="ElementDefinition.CanShrink"/> enabled.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of composed pages ready for rendering.</returns>
    public async Task<IReadOnlyList<PageLayout>> ComposeReportAsync(
        ReportDefinition report,
        DataTable data,
        Dictionary<string, object?>? parameters = null,
        Func<string, string>? subreportRdfLoader = null,
        Func<string, IReportDataProvider>? dataProviderFactory = null,
        ISubreportResolver? fallbackResolver = null,
        ITextMeasurer? textMeasurer = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(data);

        ISubreportResolver? resolver = null;

        // Check if there are any subreport elements
        bool hasSubreports = report.Bands.Any(
            b => b.Elements.Any(e => e.Type == ElementType.Subreport));

        if (hasSubreports && subreportRdfLoader is not null)
        {
            // Use constructor provider as default if no factory provided
            var factory = dataProviderFactory ?? (_ => _dataProvider);

            var prefetcher = new SubreportDataPrefetcher(
                subreportRdfLoader, factory);

            var prefetchResult = await prefetcher.PrefetchAsync(
                report, parameters, cancellationToken).ConfigureAwait(false);

            resolver = prefetchResult.SkippedPaths.Count > 0 && fallbackResolver is not null
                ? new CompositeSubreportResolver(prefetchResult.Resolver, fallbackResolver)
                : prefetchResult.Resolver;
        }
        else if (fallbackResolver is not null)
        {
            resolver = fallbackResolver;
        }

        var engine = new BandLayoutEngine();
        return engine.ComposePages(report, data, parameters, resolver, textMeasurer);
    }
}
