// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using Dabbler.Components.Data;
using Microsoft.AspNetCore.Components;

namespace Dabbler.Components;

/// <summary>
/// Renders one horizontal row of filter cells representing a single AND group.
/// Uses CSS grid for column alignment when widths are specified.
/// </summary>
public partial class DabblerFilterRow : ComponentBase
{
    /// <summary>
    /// The column definitions for this filter row.
    /// </summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<FilterColumnDefinition> Columns { get; set; } = [];

    /// <summary>
    /// The index of the filter group this row represents.
    /// </summary>
    [Parameter]
    public int GroupIndex { get; set; }

    /// <summary>
    /// The coordinator to pass to child cells.
    /// </summary>
    [Parameter]
    public FilteringCoordinator? Coordinator { get; set; }

    /// <summary>
    /// The visual layout mode.
    /// </summary>
    [Parameter]
    public FilterGridLayout Layout { get; set; } = FilterGridLayout.Horizontal;

    /// <summary>
    /// When true, validation errors are shown on all cells regardless of user interaction.
    /// </summary>
    [Parameter]
    public bool ShowValidation { get; set; }

    private bool IsVertical => Layout == FilterGridLayout.Vertical;

    /// <summary>
    /// Gets the CSS grid-template-columns style if any column has an explicit width.
    /// Falls back to equal-width flex when no widths are specified.
    /// In vertical mode, returns null (each cell is its own row).
    /// </summary>
    private string? GridTemplateStyle
    {
        get
        {
            if (IsVertical || Columns.Count == 0)
                return null;

            // If any column has an explicit width, switch to CSS grid layout
            if (Columns.Any(c => c.Width is not null))
            {
                var template = string.Join(" ", Columns.Select(c => c.Width ?? "1fr"));
                return $"display: grid; grid-template-columns: {template}; gap: 8px; align-items: center;";
            }

            return null; // Use default flex layout from CSS
        }
    }
}
