// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data;

/// <summary>
/// Represents a single active filter condition in a human-readable form.
/// Used by <see cref="FilterSummaryBuilder.GetItems"/>.
/// </summary>
public class FilterSummaryItem
{
    /// <summary>The column header (display name) for this filter.</summary>
    public string ColumnHeader { get; set; } = "";

    /// <summary>The property name this filter applies to.</summary>
    public string PropertyName { get; set; } = "";

    /// <summary>The human-readable operator label (e.g., "Contains", "Equals").</summary>
    public string OperatorLabel { get; set; } = "";

    /// <summary>The formatted filter value as a string (empty for unary operators).</summary>
    public string FormattedValue { get; set; } = "";

    /// <summary>
    /// The OR-group index (0-based). Use -1 for the quick filter row,
    /// which is not part of any explicit OR group.
    /// </summary>
    public int GroupIndex { get; set; }
}
