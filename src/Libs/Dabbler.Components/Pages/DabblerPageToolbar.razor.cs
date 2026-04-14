// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using Microsoft.AspNetCore.Components;

namespace Dabbler.Components;

/// <summary>
/// Internal toolbar component shared by page-level components.
/// Renders a horizontal bar with status text, custom content, and action buttons.
/// </summary>
public partial class DabblerPageToolbar : DabblerComponentBase
{
    /// <summary>
    /// Left-side status display text (e.g., record count or loading indicator).
    /// </summary>
    [Parameter]
    public string? StatusText { get; set; }

    /// <summary>
    /// When true, shows a progress indicator.
    /// </summary>
    [Parameter]
    public bool IsLoading { get; set; }

    /// <summary>
    /// Custom toolbar content rendered in the center area.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Right-side action buttons.
    /// </summary>
    [Parameter]
    public RenderFragment? ActionButtons { get; set; }
}
