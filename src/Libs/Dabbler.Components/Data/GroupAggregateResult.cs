// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data;

/// <summary>
/// Contains aggregate data for a single group produced during grouping.
/// </summary>
public class GroupAggregateResult
{
    /// <summary>
    /// The group key value (null for items with null group property).
    /// </summary>
    public object? Key { get; init; }

    /// <summary>
    /// The number of items in this group.
    /// </summary>
    public int Count { get; init; }

    /// <summary>
    /// Named aggregate values for numeric properties (e.g., "Salary_Sum", "Salary_Average").
    /// </summary>
    public Dictionary<string, object?> Aggregates { get; init; } = new();

    /// <summary>
    /// The zero-based grouping level (0 = top-level group).
    /// </summary>
    public int Level { get; init; }
}
