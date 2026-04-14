// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using Microsoft.AspNetCore.Components;

namespace Dabbler.Components;

/// <summary>
/// A large clickable card with an icon, label, optional subtitle, and optional badge count.
/// Used as a navigation widget on dashboard/switchboard pages.
/// </summary>
public partial class DabblerNavigationCard : DabblerComponentBase
{
    /// <summary>
    /// The primary label displayed on the card.
    /// </summary>
    [Parameter, EditorRequired]
    public string Label { get; set; } = "";

    /// <summary>
    /// Optional MudBlazor icon string (e.g., <c>Icons.Material.Filled.People</c>).
    /// </summary>
    [Parameter]
    public string? Icon { get; set; }

    /// <summary>
    /// Optional subtitle displayed below the label (e.g., "1,247 active").
    /// </summary>
    [Parameter]
    public string? Subtitle { get; set; }

    /// <summary>
    /// Optional badge text displayed in the corner (e.g., "12 overdue").
    /// </summary>
    [Parameter]
    public string? BadgeText { get; set; }

    /// <summary>
    /// Optional accent color for the card (CSS color value, e.g., "#4A90D9").
    /// </summary>
    [Parameter]
    public string? Color { get; set; }

    /// <summary>
    /// Raised when the card is clicked.
    /// </summary>
    [Parameter]
    public EventCallback OnClick { get; set; }

    private string? CardStyle
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(Style))
                parts.Add(Style);
            if (!string.IsNullOrWhiteSpace(Color))
                parts.Add($"--dabbler-card-accent: {Color}");
            return parts.Count > 0 ? string.Join("; ", parts) : null;
        }
    }
}
