// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using System.ComponentModel;
using Dabbler.Components.Data.Metadata;
using Microsoft.AspNetCore.Components;

namespace Dabbler.Components;

/// <summary>
/// User-curated list of pinned (favorite) records and artifacts.
/// Clicking an item navigates to its detail tab.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public partial class DabblerFavorites : DabblerComponentBase
{
    private bool _isLoading;
    private IReadOnlyList<ArtifactUsageRecord> _items = [];

    /// <summary>
    /// Provides pinned artifact data.
    /// </summary>
    [Parameter]
    public IUsageTrackingProvider? UsageTrackingProvider { get; set; }

    /// <summary>
    /// The current user identifier.
    /// </summary>
    [Parameter]
    public string UserId { get; set; } = "";

    /// <summary>
    /// Raised when an item is clicked.
    /// </summary>
    [Parameter]
    public EventCallback<ArtifactUsageRecord> OnItemClick { get; set; }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (UsageTrackingProvider is null) return;

        _isLoading = true;
        StateHasChanged();

        try
        {
            // Collect pinned items across all artifact types
            var tasks = Enum.GetValues<ArtifactType>()
                .Select(t => UsageTrackingProvider.GetPinnedAsync(UserId, t));

            var results = await Task.WhenAll(tasks);

            _items = results
                .SelectMany(r => r)
                .OrderBy(r => r.ArtifactType)
                .ThenBy(r => r.ArtifactId)
                .ToList();
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }
}
