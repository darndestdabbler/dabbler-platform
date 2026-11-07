// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using Dabbler.Components.Data.Metadata;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace Dabbler.Components.Compat;

/// <summary>
/// Horizontal top menu bar providing catalog-style access to application artifacts:
/// Forms, Queries, Reports, and Dashboards. Each menu item opens a dropdown with
/// pinned items, recently/frequently used items, and an "All [Type]" link to the
/// full catalog page.
/// </summary>
public partial class DabblerTopMenuBar : DabblerComponentBase
{
    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;

    // ── Dropdown state ──────────────────────────────────────────────
    private ArtifactType? _openDropdown;
    private bool _isHamburgerOpen;
    private bool _preventMenuBarDefault;

    // ── Dropdown data cache ─────────────────────────────────────────
    private IReadOnlyList<ArtifactUsageRecord> _pinnedItems = [];
    private IReadOnlyList<ArtifactUsageRecord> _recentItems = [];
    private IReadOnlyList<ArtifactCatalogEntry> _allItems = [];
    private bool _isDropdownLoading;

    // ── Parameters ──────────────────────────────────────────────────

    /// <summary>
    /// Provides pinned and recently/frequently used artifact data for dropdown sections.
    /// </summary>
    [Parameter, EditorRequired]
    public IUsageTrackingProvider UsageTrackingProvider { get; set; } = null!;

    /// <summary>
    /// Provides the full artifact catalog for dropdown population and catalog page navigation.
    /// </summary>
    [Parameter, EditorRequired]
    public IArtifactCatalogProvider CatalogProvider { get; set; } = null!;

    /// <summary>
    /// The current user's identifier, used to query usage tracking and catalog data.
    /// </summary>
    [Parameter]
    public string UserId { get; set; } = "";

    /// <summary>
    /// Application title or logo content displayed at the left of the menu bar.
    /// </summary>
    [Parameter]
    public string? AppTitle { get; set; }

    /// <summary>
    /// Optional custom content for the app logo/title area.
    /// </summary>
    [Parameter]
    public RenderFragment? AppTitleTemplate { get; set; }

    /// <summary>
    /// Optional content for the right side of the menu bar (user profile, settings, etc.).
    /// </summary>
    [Parameter]
    public RenderFragment? TrailingContent { get; set; }

    /// <summary>
    /// Raised when the user clicks an artifact in a dropdown. The shell converts this
    /// to a workspace tab open operation.
    /// </summary>
    [Parameter]
    public EventCallback<(ArtifactType ArtifactType, string ArtifactId, string Name)> OnArtifactSelected { get; set; }

    /// <summary>
    /// Raised when the user clicks "All [Type]" at the bottom of a dropdown,
    /// requesting a full catalog search page for that artifact type.
    /// </summary>
    [Parameter]
    public EventCallback<ArtifactType> OnCatalogRequested { get; set; }

    // ── Menu definition ─────────────────────────────────────────────

    private static readonly (ArtifactType Type, string Label, string Icon)[] MenuItems =
    [
        (ArtifactType.Form, "Forms", Icons.Material.Outlined.Description),
        (ArtifactType.Query, "Queries", Icons.Material.Outlined.TableChart),
        (ArtifactType.Report, "Reports", Icons.Material.Outlined.Assessment),
        (ArtifactType.Dashboard, "Dashboards", Icons.Material.Outlined.Dashboard),
    ];

    // ── Menu bar keyboard navigation (ARIA Menu Bar pattern) ─────────

    private async Task HandleMenuBarKeyDownAsync(KeyboardEventArgs e)
    {
        // Find which menu item is currently open (or default to 0)
        var currentIndex = _openDropdown is not null
            ? Array.FindIndex(MenuItems, m => m.Type == _openDropdown.Value)
            : 0;

        switch (e.Key)
        {
            case "ArrowRight":
                _preventMenuBarDefault = true;
                var nextIndex = (currentIndex + 1) % MenuItems.Length;
                if (_openDropdown is not null)
                {
                    await ToggleDropdownAsync(MenuItems[nextIndex].Type);
                }
                await JSRuntime.InvokeVoidAsync("eval",
                    $"document.getElementById('menu-item-{nextIndex}')?.focus()");
                break;

            case "ArrowLeft":
                _preventMenuBarDefault = true;
                var prevIndex = (currentIndex - 1 + MenuItems.Length) % MenuItems.Length;
                if (_openDropdown is not null)
                {
                    await ToggleDropdownAsync(MenuItems[prevIndex].Type);
                }
                await JSRuntime.InvokeVoidAsync("eval",
                    $"document.getElementById('menu-item-{prevIndex}')?.focus()");
                break;

            case "ArrowDown":
                _preventMenuBarDefault = true;
                if (_openDropdown is null)
                {
                    await ToggleDropdownAsync(MenuItems[currentIndex].Type);
                }
                // Focus the first item in the dropdown
                await JSRuntime.InvokeVoidAsync("eval",
                    "document.querySelector('.dabbler-menu-dropdown button[role=menuitem]')?.focus()");
                break;

            case "Escape":
                _preventMenuBarDefault = true;
                if (_openDropdown is not null)
                {
                    CloseDropdown();
                    await JSRuntime.InvokeVoidAsync("eval",
                        $"document.getElementById('menu-item-{currentIndex}')?.focus()");
                }
                break;

            default:
                _preventMenuBarDefault = false;
                break;
        }
    }

    // ── Dropdown toggle ─────────────────────────────────────────────

    private async Task ToggleDropdownAsync(ArtifactType type)
    {
        if (_openDropdown == type)
        {
            CloseDropdown();
            return;
        }

        _openDropdown = type;
        _isHamburgerOpen = false;
        await LoadDropdownDataAsync(type);
    }

    private void CloseDropdown()
    {
        _openDropdown = null;
        _isHamburgerOpen = false;
    }

    private void ToggleHamburger()
    {
        _isHamburgerOpen = !_isHamburgerOpen;
        if (_isHamburgerOpen)
        {
            _openDropdown = null;
        }
    }

    // ── Data loading ────────────────────────────────────────────────

    private async Task LoadDropdownDataAsync(ArtifactType type)
    {
        _isDropdownLoading = true;
        StateHasChanged();

        try
        {
            var pinnedTask = UsageTrackingProvider.GetPinnedAsync(UserId, type);
            var recentTask = UsageTrackingProvider.GetRecentAsync(UserId, type);
            var allTask = CatalogProvider.GetArtifactsAsync(type, UserId);

            await Task.WhenAll(pinnedTask, recentTask, allTask);

            _pinnedItems = pinnedTask.Result;
            _recentItems = recentTask.Result;
            _allItems = allTask.Result;
        }
        finally
        {
            _isDropdownLoading = false;
            StateHasChanged();
        }
    }

    // ── Item click handlers ─────────────────────────────────────────

    private async Task HandleArtifactClickAsync(ArtifactType type, string artifactId, string name)
    {
        CloseDropdown();
        await OnArtifactSelected.InvokeAsync((type, artifactId, name));
    }

    private async Task HandleCatalogClickAsync(ArtifactType type)
    {
        CloseDropdown();
        await OnCatalogRequested.InvokeAsync(type);
    }

    // ── Helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Resolves a display name for a usage record by matching it against the loaded catalog.
    /// Falls back to the ArtifactId if no catalog entry is found.
    /// </summary>
    private string GetDisplayName(ArtifactUsageRecord record)
    {
        var entry = _allItems.FirstOrDefault(e => e.ArtifactId == record.ArtifactId);
        return entry?.Name ?? record.ArtifactId;
    }

    /// <summary>
    /// Returns catalog entries that are not in the pinned or recent lists,
    /// for the empty-state fallback (new user with no usage history).
    /// </summary>
    private bool HasUsageData => _pinnedItems.Count > 0 || _recentItems.Count > 0;
}
