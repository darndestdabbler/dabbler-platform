// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data;

/// <summary>
/// A serializable snapshot of all filter groups in a
/// <see cref="Dabbler.Components.DabblerFilterGrid{T}"/>.
/// </summary>
/// <remarks>
/// Produced by <c>DabblerFilterGrid.ExportFilter()</c> and consumed by
/// <c>DabblerFilterGrid.ImportFilter()</c>. The format is JSON-friendly
/// (all values are stored as JSON strings with type metadata).
/// </remarks>
public class FilterPresetState
{
    /// <summary>
    /// One entry per OR group. Groups are AND-within, OR-between, matching
    /// the semantics of <see cref="FilterGroupCollection"/>.
    /// </summary>
    public List<FilterPresetGroup> Groups { get; set; } = new();
}

/// <summary>
/// A serializable snapshot of one AND filter group.
/// </summary>
public class FilterPresetGroup
{
    /// <summary>
    /// Filter entries keyed by property name. Keys ending in <c>__RangeEnd</c>
    /// are internal date-range end values produced by date presets.
    /// </summary>
    public Dictionary<string, FilterPresetEntry> Entries { get; set; } = new();
}

/// <summary>
/// A serializable snapshot of a single filter entry (operator + value).
/// </summary>
public class FilterPresetEntry
{
    /// <summary>The filter operator string (from <see cref="FilterOperators"/>).</summary>
    public string Operator { get; set; } = "";

    /// <summary>
    /// The filter value serialized as a JSON string.
    /// Null when <see cref="Operator"/> is a unary operator (IsEmpty / IsNotEmpty).
    /// </summary>
    public string? ValueJson { get; set; }

    /// <summary>
    /// The assembly-qualified type name of the original value, used for deserialization.
    /// Null when <see cref="ValueJson"/> is null.
    /// </summary>
    public string? ValueTypeName { get; set; }

    /// <summary>
    /// When set, the entry was created by a named date preset (e.g., "ThisMonth").
    /// On import, the preset is re-resolved to the current date range rather than
    /// restoring the hardcoded dates from <see cref="ValueJson"/>.
    /// Corresponds to a <see cref="DatePreset"/> enum name.
    /// </summary>
    public string? DatePresetName { get; set; }
}
