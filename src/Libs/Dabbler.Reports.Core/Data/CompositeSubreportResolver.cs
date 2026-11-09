using System.Data;
using Dabbler.Reports.Core.Rdf;

namespace Dabbler.Reports.Core.Data;

/// <summary>
/// Chains multiple <see cref="ISubreportResolver"/> instances, trying each in order
/// until one succeeds. If all resolvers fail, the last exception is rethrown.
/// </summary>
/// <remarks>
/// <para>Use this to combine a <see cref="PrefetchedSubreportResolver"/> (for
/// static-parameter subreports) with a custom application resolver (for
/// row-dependent subreports that require on-demand data fetching).</para>
/// </remarks>
public class CompositeSubreportResolver : ISubreportResolver
{
    private readonly ISubreportResolver[] _resolvers;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeSubreportResolver"/> class.
    /// </summary>
    /// <param name="resolvers">
    /// One or more resolvers to try in order. The first successful result is returned.
    /// </param>
    /// <exception cref="ArgumentException"><paramref name="resolvers"/> is empty.</exception>
    public CompositeSubreportResolver(params ISubreportResolver[] resolvers)
    {
        ArgumentNullException.ThrowIfNull(resolvers);
        if (resolvers.Length == 0)
            throw new ArgumentException("At least one resolver must be provided.", nameof(resolvers));

        _resolvers = resolvers;
    }

    /// <inheritdoc />
    public (ReportDefinition Report, DataTable Data) Resolve(
        string subreportPath,
        IReadOnlyDictionary<string, object?> parameters)
    {
        Exception? lastException = null;

        foreach (var resolver in _resolvers)
        {
            try
            {
                return resolver.Resolve(subreportPath, parameters);
            }
            catch (Exception ex)
            {
                lastException = ex;
            }
        }

        throw lastException!;
    }
}
