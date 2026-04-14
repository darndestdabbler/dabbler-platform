// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using Dabbler.Components.Data;
using Microsoft.AspNetCore.Components;

namespace Dabbler.Components;

/// <summary>
/// Container for multiple <see cref="DabblerFilterRow"/> components, separated by OR labels.
/// Supports adding and removing OR groups.
/// </summary>
public partial class DabblerFilterRowPanel : ComponentBase
{
    /// <summary>
    /// The column definitions shared by all filter rows.
    /// </summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<FilterColumnDefinition> Columns { get; set; } = [];

    /// <summary>
    /// The coordinator managing filter state.
    /// </summary>
    [Parameter, EditorRequired]
    public FilteringCoordinator Coordinator { get; set; } = null!;

    /// <summary>
    /// Whether to show the "+ Add OR filter row" button. Defaults to true.
    /// </summary>
    [Parameter]
    public bool ShowAddOrRow { get; set; } = true;

    /// <summary>
    /// The visual layout mode. Passed through to child <see cref="DabblerFilterRow"/> components.
    /// </summary>
    [Parameter]
    public FilterGridLayout Layout { get; set; } = FilterGridLayout.Horizontal;

    /// <summary>
    /// When true, validation errors are shown on all cells regardless of user interaction.
    /// </summary>
    [Parameter]
    public bool ShowValidation { get; set; }

    private bool IsVertical => Layout == FilterGridLayout.Vertical;

    private string? HeaderGridStyle
    {
        get
        {
            if (IsVertical || Columns.Count == 0)
                return null;

            if (Columns.Any(c => c.Width is not null))
            {
                var template = string.Join(" ", Columns.Select(c => c.Width ?? "1fr"));
                return $"display: grid; grid-template-columns: {template}; gap: 8px; align-items: center;";
            }

            return null;
        }
    }

    private void AddOrGroup()
    {
        Coordinator.AddGroup();
        StateHasChanged();
    }

    private void RemoveGroup(int groupIndex)
    {
        var group = Coordinator.Groups.Groups.ElementAtOrDefault(groupIndex);
        if (group is not null)
        {
            Coordinator.RemoveGroup(group);
            StateHasChanged();
        }
    }
}
