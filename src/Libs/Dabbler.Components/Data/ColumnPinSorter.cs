// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data;

/// <summary>
/// Provides helpers for re-ordering <see cref="GridColumnInfo"/> collections to match
/// the visual column order when column pinning is active.
/// </summary>
/// <remarks>
/// Visual order is: pinned-left columns first (in original order), then unpinned columns
/// (in original order), then pinned-right columns (in original order).
/// This order is used for clipboard copy and CSV export so that the exported data
/// matches what the user sees on screen.
/// </remarks>
public static class ColumnPinSorter
{
    /// <summary>
    /// Returns the columns in visual display order:
    /// pinned-left → unpinned → pinned-right.
    /// Columns that share the same pin group retain their original relative order.
    /// </summary>
    /// <param name="columns">The original column list.</param>
    /// <returns>
    /// A new list with columns reordered to match the visual layout.
    /// If no columns are pinned, returns a copy of the original list in its original order.
    /// </returns>
    public static IReadOnlyList<GridColumnInfo> GetVisualOrder(IReadOnlyList<GridColumnInfo> columns)
    {
        ArgumentNullException.ThrowIfNull(columns);

        // Fast path: if no columns are pinned, return a copy of the original list
        if (columns.All(c => c.PinPosition == ColumnPinPosition.None))
            return columns.ToList();

        var left     = columns.Where(c => c.PinPosition == ColumnPinPosition.Left);
        var unpinned = columns.Where(c => c.PinPosition == ColumnPinPosition.None);
        var right    = columns.Where(c => c.PinPosition == ColumnPinPosition.Right);

        return left.Concat(unpinned).Concat(right).ToList();
    }
}
