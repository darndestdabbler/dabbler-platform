// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using Microsoft.AspNetCore.Components;

namespace Dabbler.Components;

/// <summary>
/// A titled section containing a list of text links, each navigating to an artifact.
/// Used as a compact navigation widget on dashboard pages.
/// </summary>
public partial class DabblerQuickLinksGroup : DabblerComponentBase
{
    /// <summary>
    /// The group title displayed above the links.
    /// </summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>
    /// The list of quick links to render.
    /// </summary>
    [Parameter]
    public List<QuickLink> Links { get; set; } = new();

    /// <summary>
    /// Raised when a link is clicked.
    /// </summary>
    [Parameter]
    public EventCallback<QuickLink> OnLinkClick { get; set; }
}
