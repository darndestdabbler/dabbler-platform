// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data.Metadata;

/// <summary>
/// Data provider contract for navigation-specific queries.
/// Returns smart filter top-N records and counts for the navigation panel.
/// </summary>
public interface INavigationDataProvider
{
    /// <summary>
    /// Returns top-N records matching a smart filter, projected as
    /// (<see cref="NavigationRecord.PrimaryKeyValue"/>, <see cref="NavigationRecord.RecordLabel"/>) pairs.
    /// </summary>
    /// <param name="entityName">The entity to query.</param>
    /// <param name="filter">The smart filter definition to apply.</param>
    /// <param name="topN">Maximum number of records to return.</param>
    /// <param name="context">Optional context value (e.g., the current parent record PK for relational filters).</param>
    /// <returns>An ordered list of matching navigation records.</returns>
    Task<IReadOnlyList<NavigationRecord>> GetSmartFilterRecordsAsync(
        string entityName,
        SmartFilterDefinition filter,
        int topN,
        object? context = null);

    /// <summary>
    /// Returns the total count of records matching a smart filter.
    /// </summary>
    /// <param name="entityName">The entity to query.</param>
    /// <param name="filter">The smart filter definition to apply.</param>
    /// <param name="context">Optional context value (e.g., the current parent record PK for relational filters).</param>
    /// <returns>The total matching record count.</returns>
    Task<int> GetSmartFilterCountAsync(
        string entityName,
        SmartFilterDefinition filter,
        object? context = null);
}
