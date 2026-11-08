namespace Dabbler.Reports.Core.Data;

/// <summary>
/// The result of a <see cref="SubreportDataPrefetcher.PrefetchAsync"/> operation,
/// containing the pre-fetched resolver and a list of subreport paths that were
/// skipped because their parameters depend on the current data row.
/// </summary>
public class SubreportPrefetchResult
{
    /// <summary>
    /// Gets the resolver containing all successfully pre-fetched subreport data.
    /// </summary>
    public PrefetchedSubreportResolver Resolver { get; }

    /// <summary>
    /// Gets the list of subreport paths that were skipped because their parameter
    /// expressions reference row-level fields (e.g., <c>[FieldName]</c>) that
    /// cannot be evaluated until layout composition iterates through data rows.
    /// </summary>
    /// <remarks>
    /// If this list is non-empty, provide a fallback <see cref="ISubreportResolver"/>
    /// (e.g., via <see cref="CompositeSubreportResolver"/>) to handle these paths
    /// during layout composition.
    /// </remarks>
    public IReadOnlyList<string> SkippedPaths { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SubreportPrefetchResult"/> class.
    /// </summary>
    /// <param name="resolver">The pre-fetched resolver.</param>
    /// <param name="skippedPaths">Paths that were skipped due to row-dependent parameters.</param>
    public SubreportPrefetchResult(
        PrefetchedSubreportResolver resolver,
        IReadOnlyList<string> skippedPaths)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(skippedPaths);
        Resolver = resolver;
        SkippedPaths = skippedPaths;
    }
}
