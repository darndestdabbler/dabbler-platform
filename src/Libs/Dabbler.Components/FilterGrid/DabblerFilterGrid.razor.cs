// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using System.Text.Json;
using Dabbler.Components.Data;
using Microsoft.AspNetCore.Components;

namespace Dabbler.Components;

/// <summary>
/// A standalone filter input component that can be used with a DataGrid or as a
/// report parameter form.
/// </summary>
/// <remarks>
/// <para>
/// Composes <see cref="DabblerFilterRowPanel"/> and <see cref="DabblerFilterStatusBar"/>
/// with a <see cref="FilteringCoordinator"/> to provide a complete filter UI.
/// </para>
/// <para>
/// For DataGrid filtering, connect via <see cref="FilterChanged"/> to update the
/// <see cref="GridDataRequest.CompositeFilter"/> on the data provider.
/// </para>
/// <para>
/// For report parameters, use <see cref="GetCellValue"/>, <see cref="SetCellValue"/>,
/// and <see cref="GetAllCellValues"/> to read/write parameter values. Set
/// <see cref="ShowAddOrRow"/> to false and <see cref="ShowStatusBar"/> to false
/// for a clean form layout.
/// </para>
/// </remarks>
/// <typeparam name="T">The row data type (used for type consistency with DataGrid).</typeparam>
/// <example>
/// <code>
/// &lt;DabblerFilterGrid T="Order" FieldMetadata="@_fields"
///                     FilterChanged="@OnFilterChanged" /&gt;
///
/// @code {
///     private void OnFilterChanged(CompositeFilterDescriptor filter)
///     {
///         // Apply the filter to a data provider or query
///     }
/// }
/// </code>
/// </example>
public partial class DabblerFilterGrid<T> : DabblerComponentBase, IDisposable
{
    private readonly FilteringCoordinator _coordinator = new();
    private bool _showValidation;
    private HashSet<string>? _internalVisibleColumns;
    private IReadOnlyList<string>? _lastVisibleColumnsParam;
    private string? _expressionLineOverride;
    private string? _filterAnnouncement;

    /// <summary>
    /// The column definitions for the filter grid.
    /// </summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<FilterColumnDefinition> Columns { get; set; } = [];

    /// <summary>
    /// The visual layout of the filter grid. Defaults to <see cref="FilterGridLayout.Horizontal"/>.
    /// Use <see cref="FilterGridLayout.Vertical"/> for report parameter forms.
    /// </summary>
    [Parameter]
    public FilterGridLayout Layout { get; set; } = FilterGridLayout.Horizontal;

    /// <summary>
    /// Whether to show the filter status bar (active filter chips). Defaults to true.
    /// </summary>
    [Parameter]
    public bool ShowStatusBar { get; set; } = true;

    /// <summary>
    /// Whether to show the "+ Add OR filter row" button. Defaults to true.
    /// </summary>
    [Parameter]
    public bool ShowAddOrRow { get; set; } = true;

    /// <summary>
    /// Callback invoked when the filter state changes. Provides the composite filter
    /// descriptor for the data layer, or null if no filters are active.
    /// </summary>
    [Parameter]
    public EventCallback<CompositeFilterDescriptor?> FilterChanged { get; set; }

    /// <summary>
    /// Callback invoked when the <see cref="IsValid"/> state changes.
    /// </summary>
    [Parameter]
    public EventCallback<bool> ValidationChanged { get; set; }

    /// <summary>
    /// Whether to show a quick-filter search input above the structured filter rows.
    /// Defaults to false.
    /// </summary>
    [Parameter]
    public bool ShowQuickFilter { get; set; } = false;

    /// <summary>
    /// Column property names to include in the quick filter search.
    /// When null, all text-compatible columns are searched.
    /// </summary>
    [Parameter]
    public IEnumerable<string>? QuickFilterColumns { get; set; }

    /// <summary>
    /// Placeholder text for the quick filter input. Defaults to "Search…".
    /// </summary>
    [Parameter]
    public string? QuickFilterPlaceholder { get; set; } = "Search\u2026";

    /// <summary>
    /// Debounce interval in milliseconds for the quick filter input. Defaults to 300.
    /// </summary>
    [Parameter]
    public int QuickFilterDebounce { get; set; } = 300;

    /// <summary>
    /// When true, <see cref="FilterChanged"/> is suppressed while <see cref="IsValid"/> is false.
    /// The filter state still updates internally; the callback is only withheld.
    /// </summary>
    [Parameter]
    public bool PreventFilterWhenInvalid { get; set; }

    /// <summary>
    /// When true, renders a styled expression line below the filter rows that displays
    /// a plain-text summary of the active filters. Updates in real-time as filters change.
    /// When validation errors exist, the expression line shows error messages instead.
    /// Defaults to false.
    /// </summary>
    [Parameter]
    public bool ShowExpressionLine { get; set; }

    /// <summary>
    /// When true, renders a column visibility toggle button that lets the user
    /// show or hide individual filter columns. Defaults to false.
    /// </summary>
    [Parameter]
    public bool ShowColumnToggle { get; set; }

    /// <summary>
    /// The property names of currently visible columns. When null, all columns are visible.
    /// Changes to this parameter are reflected in the column toggle menu.
    /// </summary>
    [Parameter]
    public IReadOnlyList<string>? VisibleColumns { get; set; }

    /// <summary>
    /// Raised when the set of visible columns changes via the column toggle menu.
    /// The argument is the new list of visible property names.
    /// </summary>
    [Parameter]
    public EventCallback<IReadOnlyList<string>> VisibleColumnsChanged { get; set; }

    /// <summary>
    /// Whether all required columns have values in the first group.
    /// </summary>
    public bool IsValid => CheckIsValid();

    /// <summary>
    /// Whether any filters are currently active.
    /// </summary>
    public bool HasActiveFilters => _coordinator.HasFilters;

    /// <summary>
    /// The text currently displayed in the expression line.
    /// Returns the override text (e.g., validation errors) when set,
    /// otherwise the plain-text filter summary, or a placeholder when empty.
    /// </summary>
    internal string ExpressionLineText
    {
        get
        {
            if (_expressionLineOverride is not null)
                return _expressionLineOverride;

            var summary = GetFilterSummary();
            return summary.Length > 0 ? summary : "(no active filters)";
        }
    }

    /// <summary>
    /// The internal coordinator (exposed for testing).
    /// </summary>
    internal FilteringCoordinator Coordinator => _coordinator;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _coordinator.FilterChanged += OnCoordinatorFilterChanged;
        _coordinator.Columns = Columns;
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _coordinator.Columns = Columns;

        // Sync column visibility when the parameter reference changes.
        if (!ReferenceEquals(VisibleColumns, _lastVisibleColumnsParam))
        {
            _lastVisibleColumnsParam = VisibleColumns;
            _internalVisibleColumns = VisibleColumns is not null
                ? new HashSet<string>(VisibleColumns, StringComparer.OrdinalIgnoreCase)
                : null;
        }
    }

    /// <summary>
    /// Returns only the columns that are currently visible (respects the column toggle).
    /// </summary>
    private IReadOnlyList<FilterColumnDefinition> EffectiveColumns =>
        _internalVisibleColumns is null
            ? Columns
            : Columns.Where(c => _internalVisibleColumns.Contains(c.PropertyName)).ToList();

    /// <summary>
    /// Returns true if the column with the given property name is currently visible.
    /// </summary>
    private bool IsColumnVisible(string propertyName) =>
        _internalVisibleColumns is null || _internalVisibleColumns.Contains(propertyName);

    /// <summary>
    /// Toggles the visibility of the column with the given property name.
    /// Hides the column and clears its filter when unchecked; shows it when checked.
    /// Raises <see cref="VisibleColumnsChanged"/> after the change.
    /// </summary>
    private async Task ToggleColumnAsync(string propertyName)
    {
        if (_internalVisibleColumns is null)
        {
            // Start from all-visible, then remove this one.
            _internalVisibleColumns = new HashSet<string>(
                Columns.Select(c => c.PropertyName),
                StringComparer.OrdinalIgnoreCase);
        }

        if (_internalVisibleColumns.Contains(propertyName))
        {
            _internalVisibleColumns.Remove(propertyName);
            _coordinator.ClearFilter(propertyName);
        }
        else
        {
            _internalVisibleColumns.Add(propertyName);
            // If all columns are now visible, revert to null (all visible).
            if (_internalVisibleColumns.Count >= Columns.Count)
                _internalVisibleColumns = null;
        }

        if (VisibleColumnsChanged.HasDelegate)
        {
            var visibleList = _internalVisibleColumns is null
                ? (IReadOnlyList<string>)Columns.Select(c => c.PropertyName).ToList()
                : _internalVisibleColumns.ToList();
            await VisibleColumnsChanged.InvokeAsync(visibleList);
        }
    }

    /// <summary>
    /// Returns a dictionary of column property names to their current visibility state.
    /// Returns null when all columns are visible (no explicit override).
    /// Used by <see cref="DabblerFilterPresetSelector{T}"/> when saving presets.
    /// </summary>
    internal Dictionary<string, bool>? GetColumnVisibilityForPreset()
    {
        if (_internalVisibleColumns is null)
            return null;

        return Columns.ToDictionary(
            c => c.PropertyName,
            c => _internalVisibleColumns.Contains(c.PropertyName));
    }

    /// <summary>
    /// Applies a column visibility map (e.g., restored from a <see cref="Data.FilterPreset"/>).
    /// Columns mapped to false are hidden and their filters are cleared.
    /// Pass null to show all columns.
    /// </summary>
    internal void ApplyColumnVisibility(Dictionary<string, bool>? visibility)
    {
        if (visibility is null)
        {
            _internalVisibleColumns = null;
            return;
        }

        _internalVisibleColumns = new HashSet<string>(
            visibility.Where(kv => kv.Value).Select(kv => kv.Key),
            StringComparer.OrdinalIgnoreCase);

        foreach (var kv in visibility.Where(kv => !kv.Value))
            _coordinator.ClearFilter(kv.Key);
    }

    /// <summary>
    /// Programmatically clears filters for a specific property.
    /// </summary>
    public void ClearFilter(string propertyName)
    {
        _coordinator.ClearFilter(propertyName);
    }

    /// <summary>
    /// Clears all filters and resets to a single empty row.
    /// </summary>
    public void ClearAllFilters()
    {
        _coordinator.ClearAll();
    }

    /// <summary>
    /// Returns a plain-text summary of all currently active filters, suitable for
    /// report headers and audit logs. Returns an empty string when no filters are active.
    /// </summary>
    public string GetFilterSummary() =>
        FilterSummaryBuilder.GetSummary(
            _coordinator.Groups, Columns, _coordinator.QuickFilterText);

    /// <summary>
    /// Returns a formatted summary of all currently active filters in the requested format.
    /// Returns an empty string when no filters are active.
    /// </summary>
    public string GetFilterSummary(FilterSummaryFormat format) =>
        FilterSummaryBuilder.GetSummary(
            _coordinator.Groups, Columns, format, _coordinator.QuickFilterText);

    /// <summary>
    /// Returns a structured list of active filter conditions for custom rendering.
    /// Quick-filter entries are included with <see cref="FilterSummaryItem.GroupIndex"/> = -1.
    /// </summary>
    public IReadOnlyList<FilterSummaryItem> GetFilterSummaryItems() =>
        FilterSummaryBuilder.GetItems(
            _coordinator.Groups, Columns, _coordinator.QuickFilterText);

    /// <summary>
    /// Gets the current filter value for a property from the first group.
    /// </summary>
    public object? GetCellValue(string propertyName)
    {
        var group = _coordinator.Groups.Groups.FirstOrDefault();
        return group?.GetFilter(propertyName)?.Value;
    }

    /// <summary>
    /// Sets a filter value for a property in the first group using the default operator.
    /// </summary>
    public void SetCellValue(string propertyName, object? value)
    {
        var colDef = Columns.FirstOrDefault(c =>
            string.Equals(c.PropertyName, propertyName, StringComparison.OrdinalIgnoreCase));
        var op = colDef?.EffectiveDefaultOperator ?? FilterOperators.Equals;
        _coordinator.SetFilter(0, propertyName, op, value);
    }

    /// <summary>
    /// Gets all current filter values from the first group as a dictionary.
    /// </summary>
    public Dictionary<string, object?> GetAllCellValues()
    {
        var group = _coordinator.Groups.Groups.FirstOrDefault();
        if (group is null)
            return new Dictionary<string, object?>();

        return group.Entries.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Value,
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Exports the current filter state to a serializable <see cref="FilterPresetState"/> DTO.
    /// </summary>
    /// <remarks>
    /// All filter group entries — including internal date-range end entries — are included
    /// so that <see cref="ImportFilter"/> can restore the state exactly.
    /// Values are serialized via <c>System.Text.Json</c> with the original type name stored
    /// alongside so deserialization can reconstruct the correct CLR type.
    /// </remarks>
    public FilterPresetState ExportFilter()
    {
        var state = new FilterPresetState();

        foreach (var group in _coordinator.Groups.Groups)
        {
            var presetGroup = new FilterPresetGroup();
            foreach (var kvp in group.AllEntries)
            {
                var entry = new FilterPresetEntry
                {
                    Operator = kvp.Value.Operator,
                    DatePresetName = kvp.Value.DatePresetName,
                };
                if (kvp.Value.Value is not null)
                {
                    // For range-end keys (e.g. "OrderDate__RangeEnd"), look up the base property.
                    var baseProp = kvp.Key.EndsWith("__RangeEnd", StringComparison.Ordinal)
                        ? kvp.Key[..kvp.Key.IndexOf("__RangeEnd", StringComparison.Ordinal)]
                        : kvp.Key;
                    var colDef = Columns.FirstOrDefault(c =>
                        string.Equals(c.PropertyName, baseProp, StringComparison.OrdinalIgnoreCase));

                    if (colDef?.SerializeValue is not null)
                    {
                        entry.ValueJson = colDef.SerializeValue(kvp.Value.Value);
                        entry.ValueTypeName = "__custom";
                    }
                    else
                    {
                        entry.ValueJson = JsonSerializer.Serialize(kvp.Value.Value, kvp.Value.Value.GetType());
                        entry.ValueTypeName = kvp.Value.Value.GetType().AssemblyQualifiedName;
                    }
                }
                presetGroup.Entries[kvp.Key] = entry;
            }
            state.Groups.Add(presetGroup);
        }

        return state;
    }

    /// <summary>
    /// Restores filter state from a <see cref="FilterPresetState"/> previously produced by
    /// <see cref="ExportFilter"/>, then raises <see cref="FilterChanged"/>.
    /// </summary>
    /// <param name="state">The preset state to restore.</param>
    /// <remarks>
    /// Clears the current filter state before restoring. Groups beyond the first are added
    /// directly to <see cref="FilterGroupCollection"/> to avoid spurious intermediate
    /// <see cref="FilterChanged"/> events; a single event is raised at the end.
    /// </remarks>
    public void ImportFilter(FilterPresetState state)
    {
        // ClearAll resets to one empty group and fires FilterChanged once (cleared state).
        _coordinator.ClearAll();

        for (int i = 0; i < state.Groups.Count; i++)
        {
            FilterGroup group;
            if (i == 0)
            {
                group = _coordinator.Groups.Groups[0];
            }
            else
            {
                // Add directly to the collection to avoid an event per group.
                group = _coordinator.Groups.AddGroup();
            }

            RestoreGroupEntries(group, state.Groups[i]);
        }

        _coordinator.RaiseFilterChanged();
    }

    private void RestoreGroupEntries(FilterGroup group, FilterPresetGroup presetGroup)
    {
        var colLookup = Columns.ToDictionary(c => c.PropertyName, c => c, StringComparer.OrdinalIgnoreCase);

        // Pre-compute which property names are handled by named date presets so their
        // __RangeEnd entries are not restored from stored values (the preset re-resolves them).
        var datePresetProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in presetGroup.Entries)
        {
            if (!kvp.Key.EndsWith("__RangeEnd", StringComparison.Ordinal)
                && kvp.Value.DatePresetName is not null)
            {
                datePresetProperties.Add(kvp.Key);
            }
        }

        foreach (var kvp in presetGroup.Entries)
        {
            if (kvp.Key.EndsWith("__RangeEnd", StringComparison.Ordinal))
            {
                // Skip __RangeEnd for properties whose date preset will re-resolve the range.
                var propName = kvp.Key[..kvp.Key.IndexOf("__RangeEnd", StringComparison.Ordinal)];
                if (datePresetProperties.Contains(propName))
                    continue;

                colLookup.TryGetValue(propName, out var rangeColDef);
                if (DeserializePresetValue(kvp.Value, rangeColDef) is DateTime endDate)
                    group.SetDateRangeEnd(propName, endDate);

                continue;
            }

            // Named date preset: re-resolve to current date range.
            if (kvp.Value.DatePresetName is not null
                && Enum.TryParse<DatePreset>(kvp.Value.DatePresetName, out var preset))
            {
                var (start, end) = DatePresetHelper.GetRange(preset);
                if (start == end)
                {
                    group.SetFilter(kvp.Key, FilterOperators.Equals, start);
                }
                else
                {
                    group.SetFilter(kvp.Key, FilterOperators.GreaterThanOrEqual, start);
                    group.SetDateRangeEnd(kvp.Key, end);
                }
                continue;
            }

            // Standard entry.
            colLookup.TryGetValue(kvp.Key, out var colDef);
            group.SetFilter(kvp.Key, kvp.Value.Operator, DeserializePresetValue(kvp.Value, colDef));
        }
    }

    private static object? DeserializePresetValue(FilterPresetEntry entry, FilterColumnDefinition? colDef)
    {
        if (entry.ValueJson is null)
            return null;

        // Custom serialization path: use the column's deserializer when the value was exported via SerializeValue.
        if (entry.ValueTypeName == "__custom" && colDef?.DeserializeValue is not null)
            return colDef.DeserializeValue(entry.ValueJson);

        if (entry.ValueTypeName is null)
            return null;

        var type = Type.GetType(entry.ValueTypeName);
        if (type is null)
            return null;

        return JsonSerializer.Deserialize(entry.ValueJson, type);
    }

    private async void OnCoordinatorFilterChanged()
    {
        await InvokeAsync(async () =>
        {
            _expressionLineOverride = CollectValidationErrors();
            var valid = IsValid;

            if (FilterChanged.HasDelegate && (!PreventFilterWhenInvalid || valid))
                await FilterChanged.InvokeAsync(_coordinator.GetCompositeFilter());

            if (ValidationChanged.HasDelegate)
                await ValidationChanged.InvokeAsync(valid);

            // Announce filter state change to screen readers
            if (_expressionLineOverride is not null)
                _filterAnnouncement = _expressionLineOverride;
            else if (_coordinator.HasFilters)
                _filterAnnouncement = $"Filter applied. {GetFilterSummary()}";
            else
                _filterAnnouncement = "Filters cleared";

            StateHasChanged();
        });
    }

    /// <summary>
    /// Collects validation error messages from all groups/columns that have active filter
    /// values failing their validation rules. Returns a semicolon-separated string of
    /// errors, or null when all active filters are valid.
    /// </summary>
    private string? CollectValidationErrors()
    {
        var errors = new List<string>();

        foreach (var group in _coordinator.Groups.Groups)
        {
            foreach (var col in Columns)
            {
                if (col.ValidationRules is null) continue;

                var entry = group.GetFilter(col.PropertyName);
                if (entry is null) continue; // No value set — skip

                foreach (var rule in col.ValidationRules)
                {
                    if (rule.ValidateFunc is not null && !rule.ValidateFunc(entry.Value, col.PropertyName))
                    {
                        var header = col.Header ?? col.PropertyName;
                        var msg = rule.ErrorMessage ?? "Invalid value.";
                        errors.Add($"{header}: {msg}");
                        break; // First error per column per group
                    }
                }
            }
        }

        return errors.Count > 0 ? string.Join("; ", errors) : null;
    }

    /// <summary>
    /// Forces validation to run on all cells (including untouched ones), making error messages
    /// visible. Returns the current <see cref="IsValid"/> state.
    /// </summary>
    public bool Validate()
    {
        _showValidation = true;
        _expressionLineOverride = CollectValidationErrors();
        _ = InvokeAsync(StateHasChanged);
        return IsValid;
    }

    private void OnQuickFilterChanged(string? text)
    {
        _coordinator.SetQuickFilter(text, QuickFilterColumns);
    }

    private bool CheckIsValid()
    {
        // Check IsRequired columns in the first group.
        var group = _coordinator.Groups.Groups.FirstOrDefault();
        if (group is null) return true;

        var hasRequired = Columns
            .Where(c => c.IsRequired)
            .All(c => group.HasFilter(c.PropertyName));

        if (!hasRequired) return false;

        // Check ValidationRules against current coordinator values for all groups.
        foreach (var (g, _) in _coordinator.Groups.Groups.Select((g, i) => (g, i)))
        {
            foreach (var col in Columns)
            {
                if (col.ValidationRules is null) continue;
                var value = g.GetFilter(col.PropertyName)?.Value;
                foreach (var rule in col.ValidationRules)
                {
                    if (rule.ValidateFunc is not null && !rule.ValidateFunc(value, col.PropertyName))
                        return false;
                }
            }
        }

        return true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _coordinator.FilterChanged -= OnCoordinatorFilterChanged;
    }
}
