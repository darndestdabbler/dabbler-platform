namespace Dabbler.Components.MasterDetail;

/// <summary>
/// Discriminated union describing how the <see cref="FilterProperty{TEntity}.ForeignKey{TForeign}"/>
/// filter should load its lookup list.
/// </summary>
public abstract record LookupSource<TForeign>
{
    /// <summary>Items supplied inline at construction time (no async loading needed).</summary>
    /// <param name="Items">The complete list of foreign-key items.</param>
    public sealed record Static(IReadOnlyList<TForeign> Items) : LookupSource<TForeign>;

    /// <summary>Items loaded on demand via an async delegate (e.g., an HTTP call).</summary>
    /// <param name="Load">Async factory that returns the lookup list.</param>
    public sealed record AsyncLoader(
        Func<CancellationToken, Task<IReadOnlyList<TForeign>>> Load) : LookupSource<TForeign>;
}
