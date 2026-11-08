// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using Dabbler.Components.Data;
using Microsoft.AspNetCore.Components;

namespace Dabbler.Components;

/// <summary>
/// A MudBlazor-native filter panel that renders filter editors based on
/// <see cref="FilterColumnDefinition"/> metadata and manages state through
/// <see cref="FilteringCoordinator"/>.
/// </summary>
/// <typeparam name="T">The row data type.</typeparam>
public partial class FilterPanel<T> : ComponentBase, IDisposable
{
    private IReadOnlyList<FilterSummaryItem> _summaryItems = [];
    private IReadOnlyList<FilterColumnDefinition> _filterableColumns = [];
    private string _ariaLabel = "Filter panel";

    /// <summary>The filterable column definitions that drive editor rendering.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<FilterColumnDefinition> Columns { get; set; } = [];

    /// <summary>Raised whenever the composite filter changes, emitting the latest descriptor.</summary>
    [Parameter]
    public EventCallback<CompositeFilterDescriptor?> FilterChanged { get; set; }

    /// <summary>When true, shows the "Add group" control for composing multi-group filters.</summary>
    [Parameter]
    public bool ShowAddGroup { get; set; } = true;

    /// <summary>When true, renders an active-filter chip summary above the editors.</summary>
    [Parameter]
    public bool ShowFilterChips { get; set; } = true;

    /// <summary>The coordinator that owns filter-group state for this panel.</summary>
    public FilteringCoordinator Coordinator { get; } = new();

    /// <summary>Returns the current composite filter, or null when no filters are set.</summary>
    public CompositeFilterDescriptor? GetCompositeFilter() => Coordinator.GetCompositeFilter();

    /// <summary>Returns a human-readable summary of the current filter for display.</summary>
    public string GetFilterSummary()
    {
        return FilterSummaryBuilder.GetSummary(Coordinator.Groups, Columns);
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        Coordinator.Columns = Columns;
        Coordinator.FilterChanged += OnCoordinatorFilterChanged;
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        Coordinator.Columns = Columns;
        _filterableColumns = Columns.Where(c => c.CanFilter).ToList();
        RefreshSummaryItems();
    }

    private void OnValueChanged(int groupIndex, string propertyName, string op, object? value)
    {
        Coordinator.SetFilter(groupIndex, propertyName, op, value);
    }

    private void ClearFilter(string propertyName)
    {
        Coordinator.ClearFilter(propertyName);
    }

    private void ClearAll()
    {
        Coordinator.ClearAll();
    }

    private void AddGroup()
    {
        Coordinator.AddGroup();
        StateHasChanged();
    }

    private void RemoveGroup(FilterGroup group)
    {
        Coordinator.RemoveGroup(group);
        StateHasChanged();
    }

    private void OnCoordinatorFilterChanged()
    {
        RefreshSummaryItems();
        var filter = Coordinator.GetCompositeFilter();
        _ = FilterChanged.InvokeAsync(filter);
        InvokeAsync(StateHasChanged);
    }

    private void RefreshSummaryItems()
    {
        _summaryItems = FilterSummaryBuilder.GetItems(Coordinator.Groups, Columns);
    }

    internal static decimal? ToNullableDecimal(object? value) => value switch
    {
        null => null,
        decimal d => d,
        int i => i,
        double d => (decimal)d,
        float f => (decimal)f,
        long l => l,
        string s when decimal.TryParse(s, out var parsed) => parsed,
        _ => null
    };

    internal static DateTime? ToNullableDateTime(object? value) => value switch
    {
        null => null,
        DateTime dt => dt,
        DateTimeOffset dto => dto.DateTime,
        string s when DateTime.TryParse(s, out var parsed) => parsed,
        _ => null
    };

    internal static bool? ToNullableBool(object? value) => value switch
    {
        null => null,
        bool b => b,
        string s when bool.TryParse(s, out var parsed) => parsed,
        _ => null
    };

    /// <inheritdoc />
    public void Dispose()
    {
        Coordinator.FilterChanged -= OnCoordinatorFilterChanged;
    }
}
