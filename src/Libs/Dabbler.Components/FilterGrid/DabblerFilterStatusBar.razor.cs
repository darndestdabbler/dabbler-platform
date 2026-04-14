// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using Dabbler.Components.Data;
using Microsoft.AspNetCore.Components;

namespace Dabbler.Components;

/// <summary>
/// Displays active filter state as chips with individual clear buttons and a "Clear All" action.
/// </summary>
public partial class DabblerFilterStatusBar : ComponentBase, IDisposable
{
    /// <summary>
    /// The coordinator managing filter state.
    /// </summary>
    [Parameter, EditorRequired]
    public FilteringCoordinator Coordinator { get; set; } = null!;

    /// <summary>
    /// The column definitions for header lookup.
    /// </summary>
    [Parameter]
    public IReadOnlyList<FilterColumnDefinition> Columns { get; set; } = [];

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        Coordinator.FilterChanged += OnFilterChanged;
    }

    private string GetHeader(string propertyName) =>
        Columns.FirstOrDefault(c =>
            string.Equals(c.PropertyName, propertyName, StringComparison.OrdinalIgnoreCase))
            ?.Header ?? propertyName;

    private void OnChipClosed(string propertyName)
    {
        Coordinator.ClearFilter(propertyName);
    }

    private void OnQuickFilterChipClosed()
    {
        Coordinator.ClearQuickFilter();
    }

    private void OnClearAllClicked()
    {
        Coordinator.ClearAll();
    }

    private bool _hasChipsBefore(int groupIndex)
    {
        for (int i = 0; i < groupIndex; i++)
        {
            if (Coordinator.Groups.Groups[i].HasFilters)
                return true;
        }
        return false;
    }

    private void OnFilterChanged()
    {
        InvokeAsync(StateHasChanged);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Coordinator.FilterChanged -= OnFilterChanged;
    }
}
