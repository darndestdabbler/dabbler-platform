// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data;

/// <summary>
/// Manages filter state for a <see cref="Dabbler.Components.FilterPanel{T}"/>,
/// coordinating between the filter UI and the data layer.
/// </summary>
/// <remarks>
/// Owns a <see cref="FilterGroupCollection"/> and raises <see cref="FilterChanged"/>
/// when any mutation occurs. Consumers (like <c>FilterPanel</c>) subscribe to
/// this event to push the updated <see cref="CompositeFilterDescriptor"/> to the data provider.
/// </remarks>
public class FilteringCoordinator
{
    /// <summary>
    /// The underlying filter group collection.
    /// </summary>
    public FilterGroupCollection Groups { get; } = new();

    /// <summary>
    /// Whether any filters are currently active.
    /// </summary>
    public bool HasFilters => Groups.HasFilters;

    /// <summary>
    /// Raised when the filter state changes. Subscribers should re-query data.
    /// </summary>
    public event Action? FilterChanged;

    /// <summary>
    /// The column definitions, used for cascading dependency resolution.
    /// Set by the owning <c>FilterPanel</c>.
    /// </summary>
    public IReadOnlyList<FilterColumnDefinition>? Columns { get; set; }

    /// <summary>
    /// Sets or updates a filter in the specified group.
    /// </summary>
    public void SetFilter(int groupIndex, string propertyName, string op, object? value)
    {
        if (groupIndex < 0 || groupIndex >= Groups.Groups.Count)
            return;

        Groups.Groups[groupIndex].SetFilter(propertyName, op, value);
        ProcessCascadingDependencies(propertyName);
        OnFilterChanged();
    }

    /// <summary>
    /// Applies a date preset for a property, creating a between filter (>= start AND &lt;= end).
    /// Replaces any existing filter for that property in the specified group.
    /// </summary>
    public void ApplyDatePreset(int groupIndex, string propertyName, DatePreset preset, DateTime? referenceDate = null)
    {
        if (groupIndex < 0 || groupIndex >= Groups.Groups.Count)
            return;

        var (start, end) = DatePresetHelper.GetRange(preset, referenceDate);
        var group = Groups.Groups[groupIndex];

        // Remove existing filter for this property
        group.RemoveFilter(propertyName);

        // For "Today" (single day), use Equals
        if (start == end)
        {
            group.SetFilterWithPresetName(propertyName, FilterOperators.Equals, start, preset.ToString());
        }
        else
        {
            // For ranges, we store as a DateRangeFilter entry
            group.SetFilterWithPresetName(propertyName, FilterOperators.GreaterThanOrEqual, start, preset.ToString());
            group.SetDateRangeEnd(propertyName, end);
        }

        ProcessCascadingDependencies(propertyName);
        OnFilterChanged();
    }

    /// <summary>
    /// Clears filters for a property across all groups.
    /// </summary>
    public void ClearFilter(string propertyName)
    {
        Groups.ClearFiltersForProperty(propertyName);
        OnFilterChanged();
    }

    /// <summary>
    /// Clears all filters (including quick filter) and resets to a single empty group.
    /// </summary>
    public void ClearAll()
    {
        Groups.Clear();
        QuickFilterText = null;
        QuickFilterColumns = null;
        OnFilterChanged();
    }

    /// <summary>
    /// Adds a new OR group.
    /// </summary>
    /// <returns>The new group.</returns>
    public FilterGroup AddGroup()
    {
        var group = Groups.AddGroup();
        OnFilterChanged();
        return group;
    }

    /// <summary>
    /// Removes an OR group.
    /// </summary>
    public void RemoveGroup(FilterGroup group)
    {
        if (Groups.RemoveGroup(group))
            OnFilterChanged();
    }

    /// <summary>
    /// Whether any group has a filter for the specified property.
    /// </summary>
    public bool HasFilterForProperty(string propertyName) =>
        Groups.HasFilterForProperty(propertyName);

    /// <summary>
    /// The current quick filter text, or null if no quick filter is active.
    /// </summary>
    public string? QuickFilterText { get; private set; }

    /// <summary>
    /// The column property names to include in the quick filter search.
    /// When null, all text-compatible columns are included.
    /// </summary>
    public IEnumerable<string>? QuickFilterColumns { get; private set; }

    /// <summary>
    /// Sets the quick filter text and column scope, then raises <see cref="FilterChanged"/>.
    /// </summary>
    public void SetQuickFilter(string? text, IEnumerable<string>? columns = null)
    {
        QuickFilterText = string.IsNullOrEmpty(text) ? null : text;
        QuickFilterColumns = columns;
        OnFilterChanged();
    }

    /// <summary>
    /// Clears the quick filter and raises <see cref="FilterChanged"/>.
    /// </summary>
    public void ClearQuickFilter()
    {
        QuickFilterText = null;
        QuickFilterColumns = null;
        OnFilterChanged();
    }

    /// <summary>
    /// Gets the composite filter descriptor for the data layer, or null if no filters are active.
    /// ANDs the quick filter (OR across columns) with the group-based composite.
    /// </summary>
    public CompositeFilterDescriptor? GetCompositeFilter()
    {
        var groupsFilter = Groups.ToCompositeFilterDescriptor();
        var quickFilter = BuildQuickFilterDescriptor();

        if (groupsFilter is null && quickFilter is null)
            return null;

        if (groupsFilter is null)
            return quickFilter;

        if (quickFilter is null)
            return groupsFilter;

        // AND the two together
        return new CompositeFilterDescriptor
        {
            Logic = CompositeFilterLogic.And,
            Children = [groupsFilter, quickFilter],
        };
    }

    private CompositeFilterDescriptor? BuildQuickFilterDescriptor()
    {
        if (string.IsNullOrEmpty(QuickFilterText) || Columns is null)
            return null;

        IEnumerable<FilterColumnDefinition> cols;
        if (QuickFilterColumns is not null)
        {
            var scope = new HashSet<string>(QuickFilterColumns, StringComparer.OrdinalIgnoreCase);
            cols = Columns.Where(c => scope.Contains(c.PropertyName));
        }
        else
        {
            // Default: all text-compatible columns
            cols = Columns.Where(c =>
                c.EditorType is FilterEditorType.Text or FilterEditorType.ComboBox);
        }

        var filters = cols
            .Select(c => new FilterDescriptor(c.PropertyName, FilterOperators.Contains, QuickFilterText))
            .ToList();

        if (filters.Count == 0)
            return null;

        if (filters.Count == 1)
            return new CompositeFilterDescriptor
            {
                Logic = CompositeFilterLogic.And,
                Filters = filters,
            };

        return new CompositeFilterDescriptor
        {
            Logic = CompositeFilterLogic.Or,
            Filters = filters,
        };
    }

    /// <summary>
    /// Clears filters for all columns that depend on the specified property.
    /// </summary>
    private void ProcessCascadingDependencies(string changedPropertyName)
    {
        if (Columns is null) return;

        foreach (var col in Columns)
        {
            if (string.Equals(col.DependsOn, changedPropertyName, StringComparison.OrdinalIgnoreCase))
            {
                Groups.ClearFiltersForProperty(col.PropertyName);
            }
        }
    }

    /// <summary>
    /// Raises <see cref="FilterChanged"/> without mutating state.
    /// Used after bulk state restoration.
    /// </summary>
    internal void RaiseFilterChanged() => OnFilterChanged();

    private void OnFilterChanged() => FilterChanged?.Invoke();
}
