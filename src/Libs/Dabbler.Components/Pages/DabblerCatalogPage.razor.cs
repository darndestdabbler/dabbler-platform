// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using Dabbler.Components.Data.Metadata;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Dabbler.Components;

/// <summary>
/// A catalog search page that lists all artifacts of a given type using a search box
/// and data grid. Displayed when the user clicks "All Forms", "All Reports", etc.
/// from the top menu bar. Supports searching, pin/unpin toggling, and row-click navigation.
/// </summary>
public partial class DabblerCatalogPage : DabblerComponentBase
{
    private string? _searchText;
    private bool _isLoading;
    private IReadOnlyList<CatalogGridRow> _rows = [];
    private string _statusText = "";

    // ── Parameters ──────────────────────────────────────────────────

    /// <summary>
    /// The type of artifact this catalog page displays (Form, Query, Report, Dashboard).
    /// </summary>
    [Parameter, EditorRequired]
    public ArtifactType ArtifactType { get; set; }

    /// <summary>
    /// The artifact catalog data source.
    /// </summary>
    [Parameter, EditorRequired]
    public IArtifactCatalogProvider CatalogProvider { get; set; } = null!;

    /// <summary>
    /// The usage tracking data source for pin state and last-used timestamps.
    /// </summary>
    [Parameter, EditorRequired]
    public IUsageTrackingProvider UsageTrackingProvider { get; set; } = null!;

    /// <summary>
    /// The current user identifier for usage queries and pin operations.
    /// </summary>
    [Parameter]
    public string UserId { get; set; } = "";

    /// <summary>
    /// Raised when the user clicks a catalog row, requesting that the artifact be opened.
    /// </summary>
    [Parameter]
    public EventCallback<(ArtifactType ArtifactType, string ArtifactId, string Name)> OnArtifactSelected { get; set; }

    // ── Computed ─────────────────────────────────────────────────────

    private string PageTitle => ArtifactType switch
    {
        ArtifactType.Form => "All Forms",
        ArtifactType.Query => "All Queries",
        ArtifactType.Report => "All Reports",
        ArtifactType.Dashboard => "All Dashboards",
        _ => $"All {ArtifactType}"
    };

    // ── Lifecycle ───────────────────────────────────────────────────

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        await LoadDataAsync();
    }

    // ── Data loading ────────────────────────────────────────────────

    private async Task LoadDataAsync()
    {
        _isLoading = true;
        StateHasChanged();

        try
        {
            // Fetch catalog entries and usage records in parallel
            var catalogTask = string.IsNullOrWhiteSpace(_searchText)
                ? CatalogProvider.GetArtifactsAsync(ArtifactType, UserId)
                : CatalogProvider.SearchArtifactsAsync(ArtifactType, UserId, _searchText);

            var recentTask = UsageTrackingProvider.GetRecentAsync(UserId, ArtifactType, int.MaxValue);
            var pinnedTask = UsageTrackingProvider.GetPinnedAsync(UserId, ArtifactType);

            await Task.WhenAll(catalogTask, recentTask, pinnedTask);

            var catalogEntries = catalogTask.Result;
            var recentRecords = recentTask.Result;
            var pinnedRecords = pinnedTask.Result;

            // Build lookup dictionaries from usage data
            var usageLookup = recentRecords.ToDictionary(r => r.ArtifactId, r => r);
            var pinnedSet = new HashSet<string>(pinnedRecords.Select(r => r.ArtifactId));

            // Merge catalog entries with usage data
            _rows = catalogEntries.Select(entry =>
            {
                usageLookup.TryGetValue(entry.ArtifactId, out var usage);

                return new CatalogGridRow
                {
                    ArtifactId = entry.ArtifactId,
                    ArtifactType = entry.ArtifactType,
                    Name = entry.Name,
                    Description = entry.Description ?? "",
                    Category = entry.Category ?? "",
                    Tags = entry.Tags,
                    LastAccessedAt = usage?.LastAccessedAt,
                    IsPinned = pinnedSet.Contains(entry.ArtifactId)
                };
            }).ToList();

            UpdateStatusText();
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    // ── Event handlers ──────────────────────────────────────────────

    private async Task SearchChangedAsync(string? newValue)
    {
        _searchText = newValue;
        await LoadDataAsync();
    }

    private async Task HandleRowClickAsync(DataGridRowClickEventArgs<CatalogGridRow> args)
    {
        var row = args.Item;
        if (row is null) return;

        await OnArtifactSelected.InvokeAsync((row.ArtifactType, row.ArtifactId, row.Name));
    }

    private async Task TogglePinAsync(CatalogGridRow row)
    {
        var newPinned = !row.IsPinned;

        await UsageTrackingProvider.SetPinnedAsync(UserId, row.ArtifactType, row.ArtifactId, newPinned);

        // Update the local row state
        row.IsPinned = newPinned;
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private void UpdateStatusText()
    {
        _statusText = _rows.Count == 1
            ? "1 artifact"
            : $"{_rows.Count:N0} artifacts";
    }

    // ── Grid row view model ─────────────────────────────────────────

    /// <summary>
    /// View model combining artifact catalog data with usage tracking data
    /// for display in the catalog grid.
    /// </summary>
    public class CatalogGridRow
    {
        /// <summary>Unique identifier for the artifact.</summary>
        public string ArtifactId { get; set; } = "";
        /// <summary>The type of artifact (query, form, report, or dashboard).</summary>
        public ArtifactType ArtifactType { get; set; }
        /// <summary>Display name of the artifact.</summary>
        public string Name { get; set; } = "";
        /// <summary>Human-readable description of the artifact's purpose.</summary>
        public string Description { get; set; } = "";
        /// <summary>Grouping category for organizing artifacts in the catalog.</summary>
        public string Category { get; set; } = "";
        /// <summary>Searchable tags assigned to the artifact.</summary>
        public List<string> Tags { get; set; } = new();
        /// <summary>When the user last opened this artifact, or <see langword="null"/> if never accessed.</summary>
        public DateTime? LastAccessedAt { get; set; }
        /// <summary>Whether the user has pinned this artifact for quick access.</summary>
        public bool IsPinned { get; set; }
    }
}
