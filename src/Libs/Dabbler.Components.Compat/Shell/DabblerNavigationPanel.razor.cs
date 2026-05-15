// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using Dabbler.Components.Data.Metadata;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Dabbler.Components.Compat;

/// <summary>
/// A navigation panel with a dual-section layout: an entity list (top) for quick entity access
/// and a contextual pill area (bottom) showing smart filter pills for the currently focused entity.
/// Replaces the previous accordion-based layout. Driven by <see cref="IEntityMetadataProvider"/>
/// and <see cref="INavigationDataProvider"/>.
/// </summary>
public partial class DabblerNavigationPanel : DabblerComponentBase
{
    private sealed class SmartFilterData
    {
        public IReadOnlyList<NavigationRecord> Records { get; set; } = [];
        public int TotalCount { get; set; }
    }

    private sealed class RelatedEntityGroup
    {
        public string EntityName { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string? IconName { get; set; }
        public Dictionary<string, SmartFilterData> Filters { get; set; } = new();
    }

    // ── Injected services ────────────────────────────────────────
    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;

    // ── Internal state ───────────────────────────────────────────
    private IReadOnlyList<EntityMetadata> _navigableEntities = [];
    private IReadOnlyList<EntityMetadata> _allEntities = [];
    private string _entitySearchFilter = "";
    private readonly Dictionary<string, SmartFilterData> _ownFilters = new();
    private readonly List<RelatedEntityGroup> _relatedGroups = new();
    private bool _isLoadingFilters;
    private IJSObjectReference? _jsModule;
    private DotNetObjectReference<DabblerNavigationPanel>? _dotNetRef;
    private ElementReference _dividerRef;
    private ElementReference _contentRef;
    private bool _jsInitPending;
    private string? _previousFocusedEntityName;
    private object? _previousFocusedRecordKey;

    // ── Parameters ───────────────────────────────────────────────

    /// <summary>
    /// Source of entity metadata for building the entity list.
    /// </summary>
    [Parameter, EditorRequired]
    public IEntityMetadataProvider MetadataProvider { get; set; } = null!;

    /// <summary>
    /// Data source for smart filter queries (top-N records and counts).
    /// </summary>
    [Parameter, EditorRequired]
    public INavigationDataProvider NavigationDataProvider { get; set; } = null!;

    /// <summary>
    /// Flat report items shown in the navigation panel.
    /// </summary>
    [Parameter]
    public IReadOnlyList<NavigationReportItem> ReportItems { get; set; } = [];

    /// <summary>
    /// Application title displayed at the top of the panel.
    /// </summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>
    /// Custom title area content. Takes precedence over <see cref="Title"/>.
    /// </summary>
    [Parameter]
    public RenderFragment? TitleTemplate { get; set; }

    /// <summary>
    /// Fired when a record pill is clicked in the contextual pill area.
    /// </summary>
    [Parameter]
    public EventCallback<(string EntityName, object PrimaryKey)> OnRecordSelected { get; set; }

    /// <summary>
    /// Fired when an "all(N)" link is clicked in the pill area.
    /// </summary>
    [Parameter]
    public EventCallback<(string EntityName, SmartFilterDefinition? PreFilter)> OnSearchRequested { get; set; }

    /// <summary>
    /// Fired when a new-record action is triggered. Provides the entity name.
    /// </summary>
    [Parameter]
    public EventCallback<string> OnNewRequested { get; set; }

    /// <summary>
    /// Fired when an entity is clicked in the entity list.
    /// Opens the entity's Search Tab in the workspace.
    /// </summary>
    [Parameter]
    public EventCallback<string> OnEntitySelected { get; set; }

    /// <summary>
    /// Fired when a report item is clicked in the navigation panel.
    /// </summary>
    [Parameter]
    public EventCallback<string> OnReportSelected { get; set; }

    /// <summary>
    /// Whether the panel is collapsed to a narrow icon strip.
    /// </summary>
    [Parameter]
    public bool IsCollapsed { get; set; }

    /// <summary>
    /// Two-way binding callback for <see cref="IsCollapsed"/>.
    /// </summary>
    [Parameter]
    public EventCallback<bool> IsCollapsedChanged { get; set; }

    /// <summary>
    /// Entity name of the currently focused workspace tab.
    /// Drives the contextual pill area content.
    /// </summary>
    [Parameter]
    public string? FocusedEntityName { get; set; }

    /// <summary>
    /// Primary key of the focused record (for relational filters).
    /// </summary>
    [Parameter]
    public object? FocusedRecordKey { get; set; }

    /// <summary>
    /// Panel width when expanded.
    /// </summary>
    [Parameter]
    public string Width { get; set; } = "280px";

    /// <summary>
    /// Percentage of the panel content area allocated to the entity list (top section).
    /// The pill area takes the remainder. Value is clamped to [15, 85].
    /// </summary>
    [Parameter]
    public double SplitPercent { get; set; } = 40;

    /// <summary>
    /// Callback fired when the user finishes dragging the divider.
    /// Provides the new split percentage for persistence.
    /// </summary>
    [Parameter]
    public EventCallback<double> SplitRatioChanged { get; set; }

    // ── Computed ─────────────────────────────────────────────────

    private IEnumerable<EntityMetadata> FilteredEntities =>
        string.IsNullOrWhiteSpace(_entitySearchFilter)
            ? _navigableEntities
            : _navigableEntities.Where(e =>
                e.DisplayName.Contains(_entitySearchFilter, StringComparison.OrdinalIgnoreCase));

    private EntityMetadata? FocusedEntity =>
        string.IsNullOrEmpty(FocusedEntityName)
            ? null
            : _navigableEntities.FirstOrDefault(e =>
                string.Equals(e.EntityName, FocusedEntityName, StringComparison.OrdinalIgnoreCase));

    // ── Lifecycle ────────────────────────────────────────────────

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _navigableEntities = MetadataProvider.GetNavigableEntities();
        _allEntities = MetadataProvider.GetAllEntities();
        _jsInitPending = true;
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsModule = await JSRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./_content/Dabbler.Components/dabbler-nav-panel.js");
        }

        if (_jsInitPending && !IsCollapsed && _jsModule is not null)
        {
            _jsInitPending = false;
            _dotNetRef?.Dispose();
            _dotNetRef = DotNetObjectReference.Create(this);
            await _jsModule.InvokeVoidAsync(
                "initDrag", _dividerRef, _contentRef, _dotNetRef, SplitPercent);
        }
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (!string.Equals(_previousFocusedEntityName, FocusedEntityName, StringComparison.OrdinalIgnoreCase)
            || !Equals(_previousFocusedRecordKey, FocusedRecordKey))
        {
            _previousFocusedEntityName = FocusedEntityName;
            _previousFocusedRecordKey = FocusedRecordKey;
            await LoadFocusedEntityFiltersAsync();
        }
    }

    // ── Smart filter loading for focused entity ──────────────────

    private async Task LoadFocusedEntityFiltersAsync()
    {
        _ownFilters.Clear();
        _relatedGroups.Clear();

        var focusedEntity = FocusedEntity;
        if (focusedEntity is null)
        {
            StateHasChanged();
            return;
        }

        _isLoadingFilters = true;
        StateHasChanged();

        try
        {
            // 1. Load non-relational filters (temporal, audit, custom) for the focused entity
            var ownNonRelational = focusedEntity.SmartFilters
                .Where(f => f.IsEnabled && f.Category != SmartFilterCategory.Relational)
                .OrderBy(f => f.DisplayOrder)
                .ToList();

            foreach (var filter in ownNonRelational)
            {
                var records = await NavigationDataProvider.GetSmartFilterRecordsAsync(
                    focusedEntity.EntityName, filter, focusedEntity.SmartFilterTopN);
                var count = await NavigationDataProvider.GetSmartFilterCountAsync(
                    focusedEntity.EntityName, filter);

                _ownFilters[filter.Name] = new SmartFilterData
                {
                    Records = records,
                    TotalCount = count
                };
            }

            // 2. Scan all entities for relational filters that point TO the focused entity.
            //    E.g., if focused on Customer, find Order's relational filter where
            //    RelatedEntityName == "Customer" — then load Order pills for this customer.
            //    Skip when no specific record is focused (FocusedRecordKey is null),
            //    because relational queries need a parent key to filter by.
            if (FocusedRecordKey is null)
            {
                return;
            }

            foreach (var otherEntity in _allEntities)
            {
                if (string.Equals(otherEntity.EntityName, focusedEntity.EntityName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var relationalFilters = otherEntity.SmartFilters
                    .Where(f => f.IsEnabled
                        && f.Category == SmartFilterCategory.Relational
                        && string.Equals(f.RelatedEntityName, FocusedEntityName,
                            StringComparison.OrdinalIgnoreCase))
                    .OrderBy(f => f.DisplayOrder)
                    .ToList();

                if (relationalFilters.Count == 0) continue;

                var group = new RelatedEntityGroup
                {
                    EntityName = otherEntity.EntityName,
                    DisplayName = otherEntity.DisplayName,
                    IconName = otherEntity.IconName
                };

                foreach (var filter in relationalFilters)
                {
                    var records = await NavigationDataProvider.GetSmartFilterRecordsAsync(
                        otherEntity.EntityName, filter, otherEntity.SmartFilterTopN,
                        FocusedRecordKey);
                    var count = await NavigationDataProvider.GetSmartFilterCountAsync(
                        otherEntity.EntityName, filter,
                        FocusedRecordKey);

                    group.Filters[filter.Name] = new SmartFilterData
                    {
                        Records = records,
                        TotalCount = count
                    };
                }

                if (group.Filters.Count > 0)
                {
                    _relatedGroups.Add(group);
                }
            }
        }
        finally
        {
            _isLoadingFilters = false;
            StateHasChanged();
        }
    }

    // ── Event handlers ───────────────────────────────────────────

    private async Task HandleEntityClickAsync(string entityName)
    {
        await OnEntitySelected.InvokeAsync(entityName);
    }

    private async Task HandleRecordClickAsync(string entityName, object primaryKey)
    {
        await OnRecordSelected.InvokeAsync((entityName, primaryKey));
    }

    private async Task HandleSearchClickAsync(string entityName, SmartFilterDefinition? preFilter = null)
    {
        await OnSearchRequested.InvokeAsync((entityName, preFilter));
    }

    private async Task HandleReportClickAsync(string reportName)
    {
        await OnReportSelected.InvokeAsync(reportName);
    }

    private async Task ToggleCollapseAsync()
    {
        var wasCollapsed = IsCollapsed;
        IsCollapsed = !IsCollapsed;
        await IsCollapsedChanged.InvokeAsync(IsCollapsed);

        // Re-initialize JS drag behavior when expanding from collapsed state
        if (wasCollapsed && !IsCollapsed)
        {
            _jsInitPending = true;
        }
    }

    // ── JS callback for divider drag end ─────────────────────────

    /// <summary>
    /// Called from JavaScript when the user finishes dragging the section divider.
    /// </summary>
    [JSInvokable]
    public async Task OnDividerDragEnd(double newPercent)
    {
        SplitPercent = Math.Clamp(newPercent, 15, 85);
        await SplitRatioChanged.InvokeAsync(SplitPercent);
        StateHasChanged();
    }

    // ── Helpers ──────────────────────────────────────────────────

    private SmartFilterData? GetOwnFilterData(string filterName)
    {
        return _ownFilters.TryGetValue(filterName, out var data) ? data : null;
    }

    private bool IsEntityFocused(EntityMetadata entity)
    {
        return string.Equals(entity.EntityName, FocusedEntityName, StringComparison.OrdinalIgnoreCase);
    }

    private string GetPanelCssClass()
    {
        var css = "dabbler-nav-panel";
        if (IsCollapsed)
            css += " dabbler-nav-panel-collapsed";
        return css;
    }

    // ── Disposal ─────────────────────────────────────────────────

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        if (_jsModule is not null)
        {
            try
            {
                await _jsModule.InvokeVoidAsync("dispose", _dividerRef);
                await _jsModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // Circuit disconnected — safe to ignore
            }
        }

        _dotNetRef?.Dispose();
    }
}
