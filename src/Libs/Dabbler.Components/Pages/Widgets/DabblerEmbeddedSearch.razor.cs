// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Dabbler.Components;

/// <summary>
/// A search box scoped to a specific entity. Pressing Enter navigates
/// to the entity's search tab (pre-filtered if search text is provided).
/// </summary>
public partial class DabblerEmbeddedSearch : DabblerComponentBase
{
    private string? _searchText;

    /// <summary>
    /// The entity name this search box is scoped to.
    /// </summary>
    [Parameter, EditorRequired]
    public string EntityName { get; set; } = "";

    /// <summary>
    /// Optional placeholder text for the search input.
    /// </summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>
    /// Raised when the user submits a search, requesting the entity's search tab be opened.
    /// Tuple contains (EntityName, SearchText).
    /// </summary>
    [Parameter]
    public EventCallback<(string EntityName, string? SearchText)> OnSearchSubmitted { get; set; }

    private async Task HandleKeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !string.IsNullOrWhiteSpace(EntityName))
        {
            await OnSearchSubmitted.InvokeAsync((EntityName, _searchText));
        }
    }
}
