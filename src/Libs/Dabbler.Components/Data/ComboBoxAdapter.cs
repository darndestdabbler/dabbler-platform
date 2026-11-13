// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data;

/// <summary>
/// Adapts an <see cref="IComboBoxItemProvider{T}"/> to the delegate signatures
/// expected by <c>MudAutocomplete&lt;T&gt;</c>.
/// </summary>
/// <typeparam name="T">The type of item provided.</typeparam>
public sealed class ComboBoxAdapter<T>
{
    private readonly IComboBoxItemProvider<T> _provider;

    /// <summary>
    /// Gets or sets the maximum number of results returned by <see cref="SearchFunc"/>.
    /// </summary>
    public int MaxResults { get; set; } = 20;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComboBoxAdapter{T}"/> class.
    /// </summary>
    /// <param name="provider">The item provider to adapt.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="provider"/> is null.</exception>
    public ComboBoxAdapter(IComboBoxItemProvider<T> provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _provider = provider;
    }

    /// <summary>
    /// A delegate compatible with <c>MudAutocomplete&lt;T&gt;.SearchFunc</c>.
    /// </summary>
    public async Task<IEnumerable<T>> SearchFunc(string? searchText, CancellationToken cancellationToken)
    {
        return await _provider.SearchAsync(searchText, MaxResults, cancellationToken);
    }

    /// <summary>
    /// Resolves items by key values. Use this to set the initial selection when only
    /// a foreign key is available (e.g., loading a form with a saved CustomerId).
    /// </summary>
    /// <param name="keys">The key values to look up.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The resolved items, omitting any keys not found.</returns>
    public Task<IReadOnlyList<T>> GetByKeysAsync(IReadOnlyList<object> keys, CancellationToken cancellationToken = default)
    {
        return _provider.GetByKeysAsync(keys, cancellationToken);
    }
}
