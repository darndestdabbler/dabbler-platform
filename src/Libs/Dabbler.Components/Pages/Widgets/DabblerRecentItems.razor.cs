// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using System.ComponentModel;
using Dabbler.Components.Data.Metadata;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Dabbler.Components;

/// <summary>
/// Auto-populated list of the user's most recently accessed records.
/// Clicking an item navigates to its detail tab.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public partial class DabblerRecentItems : DabblerComponentBase
{
    private bool _isLoading;
    private IReadOnlyList<ArtifactUsageRecord> _items = [];

    /// <summary>
    /// Provides recent access data.
    /// </summary>
    [Parameter]
    public IUsageTrackingProvider? UsageTrackingProvider { get; set; }

    /// <summary>
    /// The current user identifier.
    /// </summary>
    [Parameter]
    public string UserId { get; set; } = "";

    /// <summary>
    /// Maximum number of recent items to display.
    /// </summary>
    [Parameter]
    public int MaxItems { get; set; } = 7;

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
            // Collect recent items across all artifact types
            var tasks = Enum.GetValues<ArtifactType>()
                .Select(t => UsageTrackingProvider.GetRecentAsync(UserId, t, MaxItems));

            var results = await Task.WhenAll(tasks);

            _items = results
                .SelectMany(r => r)
                .OrderByDescending(r => r.LastAccessedAt)
                .Take(MaxItems)
                .ToList();
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private static string GetArtifactIcon(ArtifactType type) => type switch
    {
        ArtifactType.Form => Icons.Material.Filled.Description,
        ArtifactType.Query => Icons.Material.Filled.TableChart,
        ArtifactType.Report => Icons.Material.Filled.Assessment,
        ArtifactType.Dashboard => Icons.Material.Filled.Dashboard,
        _ => Icons.Material.Filled.Article
    };
}
