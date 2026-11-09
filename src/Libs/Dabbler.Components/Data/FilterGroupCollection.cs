// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data;

/// <summary>
/// A mutable collection of filter groups following the Access-style pattern:
/// filters within a group are ANDed; groups are ORed.
/// </summary>
/// <remarks>
/// <para>
/// Each <see cref="FilterGroup"/> represents one filter row in the UI. The
/// <see cref="ToCompositeFilterDescriptor"/> method bridges to the recursive
/// <see cref="CompositeFilterDescriptor"/> that <c>LocalDataProvider</c> evaluates.
/// </para>
/// <para>
/// This class also supports standalone use for report parameter input, where a single
/// group serves as a structured form.
/// </para>
/// </remarks>
public class FilterGroupCollection
{
    private readonly List<FilterGroup> _groups = [];

    /// <summary>
    /// Initializes a new instance with a single empty group.
    /// </summary>
    public FilterGroupCollection()
    {
        _groups.Add(new FilterGroup());
    }

    /// <summary>
    /// The filter groups. Each group represents one "row" of AND-combined filters.
    /// Multiple groups are OR-combined.
    /// </summary>
    public IReadOnlyList<FilterGroup> Groups => _groups;

    /// <summary>
    /// Whether any group contains at least one filter with a non-null value
    /// (or a unary operator like IsEmpty/IsNotEmpty).
    /// </summary>
    public bool HasFilters => _groups.Any(g => g.HasFilters);

    /// <summary>
    /// Adds a new empty filter group and returns it.
    /// </summary>
    public FilterGroup AddGroup()
    {
        var group = new FilterGroup();
        _groups.Add(group);
        return group;
    }

    /// <summary>
    /// Removes a filter group.
    /// </summary>
    /// <returns>True if the group was found and removed.</returns>
    public bool RemoveGroup(FilterGroup group)
    {
        if (_groups.Count <= 1)
            return false; // Always keep at least one group

        return _groups.Remove(group);
    }

    /// <summary>
    /// Clears all groups and resets to a single empty group.
    /// </summary>
    public void Clear()
    {
        _groups.Clear();
        _groups.Add(new FilterGroup());
    }

    /// <summary>
    /// Clears filters for a specific property across all groups.
    /// </summary>
    public void ClearFiltersForProperty(string propertyName)
    {
        foreach (var group in _groups)
        {
            group.RemoveFilter(propertyName);
        }
    }

    /// <summary>
    /// Whether any group has a filter for the specified property.
    /// </summary>
    public bool HasFilterForProperty(string propertyName) =>
        _groups.Any(g => g.HasFilter(propertyName));

    /// <summary>
    /// Converts this collection to a <see cref="CompositeFilterDescriptor"/> for evaluation
    /// by <c>LocalDataProvider</c>.
    /// </summary>
    /// <returns>
    /// A composite descriptor, or <c>null</c> if there are no active filters.
    /// </returns>
    /// <remarks>
    /// Produces: top-level OR node containing one AND child per group.
    /// Single-group case is flattened to a single AND node (no unnecessary OR wrapper).
    /// </remarks>
    public CompositeFilterDescriptor? ToCompositeFilterDescriptor()
    {
        var activeGroups = _groups
            .Select(g => g.ToAndDescriptor())
            .Where(d => d is not null)
            .Cast<CompositeFilterDescriptor>()
            .ToList();

        return activeGroups.Count switch
        {
            0 => null,
            1 => activeGroups[0], // Single group: just AND, no OR wrapper
            _ => new CompositeFilterDescriptor
            {
                Logic = CompositeFilterLogic.Or,
                Children = activeGroups,
            }
        };
    }
}

/// <summary>
/// A single filter group representing one row of AND-combined filters.
/// </summary>
public class FilterGroup
{
    private readonly Dictionary<string, FilterEntry> _entries = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The active filter entries in this group, keyed by property name.
    /// Excludes internal range-end entries.
    /// </summary>
    public IReadOnlyDictionary<string, FilterEntry> Entries =>
        _entries.Where(e => !e.Key.EndsWith("__RangeEnd"))
                .ToDictionary(e => e.Key, e => e.Value, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// All filter entries in this group, including internal date-range end entries
    /// (keys ending in <c>__RangeEnd</c>). Used by preset serialization.
    /// </summary>
    internal IReadOnlyDictionary<string, FilterEntry> AllEntries => _entries;

    /// <summary>
    /// Whether this group has any active filters (excludes internal range-end entries).
    /// </summary>
    public bool HasFilters => _entries.Any(e => !e.Key.EndsWith("__RangeEnd"));

    /// <summary>
    /// Sets or replaces a filter for the specified property.
    /// </summary>
    /// <remarks>
    /// For unary operators (<see cref="FilterOperators.IsEmpty"/>, <see cref="FilterOperators.IsNotEmpty"/>),
    /// <paramref name="value"/> may be null.
    /// </remarks>
    public void SetFilter(string propertyName, string op, object? value)
    {
        // For non-unary operators, a null/empty value means "no filter"
        if (!IsUnaryOperator(op) && IsEffectivelyEmpty(value))
        {
            _entries.Remove(propertyName);
            return;
        }

        // Preserve DatePresetName if the existing entry has one and the operator matches
        string? existingPreset = null;
        if (_entries.TryGetValue(propertyName, out var existing)
            && existing.DatePresetName is not null
            && existing.Operator == op)
        {
            existingPreset = existing.DatePresetName;
        }

        _entries[propertyName] = new FilterEntry(op, value, existingPreset);
    }

    /// <summary>
    /// Removes the filter for the specified property (including any date range end).
    /// </summary>
    public bool RemoveFilter(string propertyName)
    {
        _entries.Remove($"{propertyName}__RangeEnd");
        return _entries.Remove(propertyName);
    }

    /// <summary>
    /// Whether this group has a filter for the specified property.
    /// </summary>
    public bool HasFilter(string propertyName) =>
        _entries.ContainsKey(propertyName);

    /// <summary>
    /// Gets the filter entry for a property, or null if not set.
    /// </summary>
    public FilterEntry? GetFilter(string propertyName) =>
        _entries.TryGetValue(propertyName, out var entry) ? entry : null;

    /// <summary>
    /// Sets the end value for a date range filter (used by date presets).
    /// This creates a second entry keyed as "PropertyName__RangeEnd".
    /// </summary>
    public void SetDateRangeEnd(string propertyName, DateTime end)
    {
        _entries[$"{propertyName}__RangeEnd"] = new FilterEntry(FilterOperators.LessThanOrEqual, end);
    }

    /// <summary>
    /// Sets a filter entry produced by a named date preset, recording the preset name
    /// so it can be serialized by <c>ExportFilter</c> and re-resolved on import.
    /// </summary>
    internal void SetFilterWithPresetName(string propertyName, string op, object? value, string presetName)
    {
        _entries[propertyName] = new FilterEntry(op, value, presetName);
    }

    /// <summary>
    /// Gets the active date preset label for a property, if the filter was set by a preset.
    /// </summary>
    public bool HasDateRange(string propertyName) =>
        _entries.ContainsKey($"{propertyName}__RangeEnd");

    /// <summary>
    /// Clears all filters in this group.
    /// </summary>
    public void Clear() => _entries.Clear();

    /// <summary>
    /// Converts this group to an AND-combined <see cref="CompositeFilterDescriptor"/>.
    /// </summary>
    /// <returns>A descriptor, or null if this group has no filters.</returns>
    internal CompositeFilterDescriptor? ToAndDescriptor()
    {
        if (!HasFilters)
            return null;

        var filters = _entries
            .Select(kvp =>
            {
                // Range-end entries use "Property__RangeEnd" key — extract real property name
                var propName = kvp.Key.EndsWith("__RangeEnd")
                    ? kvp.Key[..kvp.Key.IndexOf("__RangeEnd")]
                    : kvp.Key;
                return new FilterDescriptor(propName, kvp.Value.Operator, kvp.Value.Value);
            })
            .ToList();

        return new CompositeFilterDescriptor
        {
            Logic = CompositeFilterLogic.And,
            Filters = filters,
        };
    }

    private static bool IsUnaryOperator(string op) =>
        op == FilterOperators.IsEmpty || op == FilterOperators.IsNotEmpty;

    private static bool IsEffectivelyEmpty(object? value) =>
        value is null or string { Length: 0 };
}

/// <summary>
/// A single filter entry within a <see cref="FilterGroup"/>.
/// </summary>
/// <param name="Operator">The filter operator string (from <see cref="FilterOperators"/>).</param>
/// <param name="Value">The filter value.</param>
/// <param name="DatePresetName">
/// When set, indicates this entry was created by a named date preset (e.g., "ThisMonth").
/// Preserved through export so <c>ImportFilter</c> can re-resolve the preset to the current
/// date range rather than restoring hardcoded dates.
/// </param>
public record FilterEntry(string Operator, object? Value, string? DatePresetName = null);
