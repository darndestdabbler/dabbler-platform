// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using Dabbler.Components.Data;
using Microsoft.AspNetCore.Components;

namespace Dabbler.Components;

/// <summary>
/// A dropdown menu for selecting a filter operator, with optional date preset support.
/// </summary>
public partial class FilterOperatorPopup : ComponentBase
{
    /// <summary>
    /// The operators to display in the menu.
    /// </summary>
    [Parameter]
    public IReadOnlyList<string> AvailableOperators { get; set; } = [];

    /// <summary>
    /// The currently selected operator.
    /// </summary>
    [Parameter]
    public string CurrentOperator { get; set; } = FilterOperators.Equals;

    /// <summary>
    /// Callback invoked when the user selects an operator.
    /// </summary>
    [Parameter]
    public EventCallback<string> OperatorChanged { get; set; }

    /// <summary>
    /// Whether the popup trigger button is disabled.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// Whether to show date preset options below the operators.
    /// </summary>
    [Parameter]
    public bool ShowDatePresets { get; set; }

    /// <summary>
    /// The currently active date preset (if any), for highlighting.
    /// </summary>
    [Parameter]
    public DatePreset? ActiveDatePreset { get; set; }

    /// <summary>
    /// Callback invoked when the user selects a date preset.
    /// </summary>
    [Parameter]
    public EventCallback<DatePreset> DatePresetSelected { get; set; }

    private async Task OnOperatorSelected(string op)
    {
        if (op != CurrentOperator)
        {
            await OperatorChanged.InvokeAsync(op);
        }
    }

    private async Task OnDatePresetSelected(DatePreset preset)
    {
        await DatePresetSelected.InvokeAsync(preset);
    }
}
