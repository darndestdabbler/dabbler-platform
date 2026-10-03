// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data;

/// <summary>
/// Abstraction over a data source that provides items for a combo box or autocomplete component.
/// </summary>
/// <typeparam name="T">The type of item returned by the provider.</typeparam>
/// <remarks>
/// <para>
/// Implementations may fetch from a local collection, a REST API, a gRPC service, or any other source.
/// </para>
/// <para>
/// To use with <c>MudAutocomplete&lt;T&gt;</c>, pass <see cref="SearchAsync"/> as the <c>SearchFunc</c> parameter.
/// To use with <c>MudSelect&lt;T&gt;</c>, pre-load items via <see cref="SearchAsync"/> with <c>null</c> search text.
/// </para>
/// </remarks>
public interface IComboBoxItemProvider<T>
{
    /// <summary>
    /// Searches for items matching the specified search text.
    /// </summary>
    /// <param name="searchText">
    /// The text the user has typed.  May be <c>null</c> or empty to return default or initial items.
    /// </param>
    /// <param name="maxResults">
    /// The maximum number of results to return.  Implementations should respect this limit.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A list of items matching the search criteria.</returns>
    Task<IReadOnlyList<T>> SearchAsync(string? searchText, int maxResults, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves items for a batch of keys.
    /// </summary>
    /// <param name="keys">
    /// The key values to look up (e.g., foreign key IDs).
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A list of items corresponding to the provided keys.  Items not found are omitted.
    /// </returns>
    /// <remarks>
    /// This is used to resolve the display text for values that contain only a foreign key
    /// without fetching the entire item list (e.g., rendering a selected value in a closed combo box).
    /// </remarks>
    Task<IReadOnlyList<T>> GetByKeysAsync(IReadOnlyList<object> keys, CancellationToken cancellationToken = default);
}
