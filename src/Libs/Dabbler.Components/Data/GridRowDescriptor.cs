// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data;

/// <summary>
/// Represents a single visual row in a grouped data grid — either a group header or a data row.
/// </summary>
/// <typeparam name="T">The type of data item.</typeparam>
public class GridRowDescriptor<T>
{
    /// <summary>Whether this row is a group header or a data row.</summary>
    public GridRowType RowType { get; init; }

    // ── Group header properties ───────────────────────────────────────

    /// <summary>The group key value. Populated when <see cref="RowType"/> is <see cref="GridRowType.GroupHeader"/>.</summary>
    public object? GroupKey { get; init; }

    /// <summary>The zero-based grouping level. Populated when <see cref="RowType"/> is <see cref="GridRowType.GroupHeader"/>.</summary>
    public int GroupLevel { get; init; }

    /// <summary>The number of data items in this group. Populated when <see cref="RowType"/> is <see cref="GridRowType.GroupHeader"/>.</summary>
    public int GroupItemCount { get; init; }

    /// <summary>
    /// Named aggregate values for this group (e.g., "Salary_Sum"). Populated when
    /// <see cref="RowType"/> is <see cref="GridRowType.GroupHeader"/> and aggregates were requested.
    /// </summary>
    public Dictionary<string, object?>? Aggregates { get; init; }

    /// <summary>Whether this group header row is currently expanded. Mutable so the grid can update it on toggle.</summary>
    public bool IsExpanded { get; set; }

    // ── Data row properties ───────────────────────────────────────────

    /// <summary>The data item. Populated when <see cref="RowType"/> is <see cref="GridRowType.Data"/>.</summary>
    public T? DataItem { get; init; }

    /// <summary>The zero-based index of <see cref="DataItem"/> within the response Items list.</summary>
    public int DataIndex { get; init; }
}
