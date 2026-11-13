// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using Microsoft.AspNetCore.Components;

namespace Dabbler.Components.Data;

/// <summary>
/// Context passed to a custom <see cref="FilterColumnDefinition.EditorTemplate"/>.
/// Provides current value, operator, and callbacks to update filter state.
/// </summary>
public class FilterCellContext
{
    /// <summary>
    /// The current filter value, or null if no filter is set.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Callback to update the filter value. Pass null to clear the filter.
    /// </summary>
    public EventCallback<object?> ValueChanged { get; set; }

    /// <summary>
    /// The current filter operator (from <see cref="FilterOperators"/>).
    /// </summary>
    public string Operator { get; set; } = "";

    /// <summary>
    /// Callback to update the filter operator.
    /// </summary>
    public EventCallback<string> OperatorChanged { get; set; }

    /// <summary>
    /// The column definition for this cell.
    /// </summary>
    public FilterColumnDefinition Column { get; set; } = default!;

    /// <summary>
    /// The index of the filter group this cell belongs to.
    /// </summary>
    public int GroupIndex { get; set; }

    /// <summary>
    /// Optional custom serializer for the filter value.
    /// When set, <c>ExportFilter()</c> uses this instead of <c>System.Text.Json</c>.
    /// The return value is stored as-is in <c>FilterPresetEntry.ValueJson</c>.
    /// </summary>
    public Func<object?, string?>? SerializeValue { get; set; }

    /// <summary>
    /// Optional custom deserializer for the filter value.
    /// When set, <c>ImportFilter()</c> uses this to reconstruct the value from the exported string.
    /// Receives the raw string stored by <see cref="SerializeValue"/> (or <c>System.Text.Json</c>).
    /// </summary>
    public Func<string?, object?>? DeserializeValue { get; set; }
}
