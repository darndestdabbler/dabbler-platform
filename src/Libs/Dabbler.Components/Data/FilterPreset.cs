// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data;

/// <summary>
/// A saved, named snapshot of a <see cref="Dabbler.Components.DabblerFilterGrid{T}"/> filter state.
/// </summary>
/// <remarks>
/// Presets are consumer-managed: the component raises <see cref="Dabbler.Components.DabblerFilterPresetSelector{T}.OnSavePreset"/>
/// and <see cref="Dabbler.Components.DabblerFilterPresetSelector{T}.OnDeletePreset"/> so the host
/// application handles persistence (database, local storage, etc.).
/// </remarks>
public class FilterPreset
{
    /// <summary>Display name for the preset.</summary>
    public string Name { get; set; } = "";

    /// <summary>Optional description or notes.</summary>
    public string? Description { get; set; }

    /// <summary>The serialized filter state.</summary>
    public FilterPresetState FilterState { get; set; } = new();

    /// <summary>UTC date the preset was created.</summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>UTC date the preset was last applied.</summary>
    public DateTime LastUsedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Column visibility snapshot. Keys are property names; values indicate whether the
    /// column is visible. Null means all columns are visible (Feature 5 integration).
    /// </summary>
    public Dictionary<string, bool>? ColumnVisibility { get; set; }
}
