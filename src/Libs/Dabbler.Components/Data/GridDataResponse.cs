// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data;

/// <summary>
/// Contains the results of a data fetch operation from an <see cref="IGridDataProvider{T}"/>.
/// </summary>
/// <typeparam name="T">The type of data item.</typeparam>
/// <remarks>
/// This maps directly to MudDataGrid's <c>GridData&lt;T&gt;</c> (same <c>Items</c> + <c>TotalItems</c> shape).
/// </remarks>
public class GridDataResponse<T>
{
    /// <summary>
    /// The items for the current page.
    /// </summary>
    public IReadOnlyList<T> Items { get; init; } = [];

    /// <summary>
    /// The total number of items matching the current filter, across all pages.
    /// </summary>
    /// <remarks>
    /// Used by the pager to calculate the total number of pages.
    /// </remarks>
    public int TotalCount { get; init; }

    /// <summary>
    /// Aggregate data for each group when grouping is active; otherwise <c>null</c>.
    /// </summary>
    public IReadOnlyList<GroupAggregateResult>? GroupAggregates { get; init; }

    /// <summary>
    /// Contiguous item ranges for each group when grouping is active; otherwise <c>null</c>.
    /// </summary>
    public IReadOnlyList<GroupItemRange>? GroupRanges { get; init; }
}
