// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using Dabbler.Components.Data.Metadata;
using Microsoft.AspNetCore.Components;

namespace Dabbler.Components.Compat;

/// <summary>
/// The top-level shell component that wires the <see cref="DabblerNavigationPanel"/>
/// and <see cref="DabblerTabbedWorkspace"/> together. Converts navigation events into
/// workspace tab operations, and propagates the active tab's entity/record context
/// back to the navigation panel for relational smart filter updates.
/// </summary>
/// <example>
/// <code>
/// &lt;DabblerShell MetadataProvider="@_metadataProvider"
///               NavigationDataProvider="@_navDataProvider"
///               QueryPageFactory="@CreateQueryPage"
///               DetailFormFactory="@CreateDetailForm" /&gt;
/// </code>
/// </example>
public partial class DabblerShell : DabblerComponentBase
{
    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    private bool ShowTopMenuBar => UsageTrackingProvider is not null && CatalogProvider is not null;
    private string SkipLinkHref
    {
        get
        {
            var relativePath = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
            var pathWithoutFragment = relativePath.Split('#')[0];

            return string.IsNullOrWhiteSpace(pathWithoutFragment)
                ? "#dabbler-main-content"
                : $"/{pathWithoutFragment}#dabbler-main-content";
        }
    }

    // ── Tab state ───────────────────────────────────────────────────
    private readonly List<WorkspaceTab> _tabs = new();
    private string? _activeTabId;

    // ── Focused entity/record for relational filters ────────────────
    private string? _focusedEntityName;
    private object? _focusedRecordKey;

    // ── Nav panel collapse state ────────────────────────────────────
    private bool _isNavCollapsed;

    // ── Component references ────────────────────────────────────────
    private DabblerTabbedWorkspace? _workspace;

    // ── Parameters ───────────────────────────────────────────────────

    /// <summary>
    /// Entity metadata source.
    /// </summary>
    [Parameter, EditorRequired]
    public IEntityMetadataProvider MetadataProvider { get; set; } = null!;

    /// <summary>
    /// Smart filter data source for the navigation panel.
    /// </summary>
    [Parameter, EditorRequired]
    public INavigationDataProvider NavigationDataProvider { get; set; } = null!;

    /// <summary>
    /// Creates a Detail Form <see cref="RenderFragment"/> for a given entity name and record key.
    /// </summary>
    [Parameter, EditorRequired]
    public Func<string, object, RenderFragment> DetailFormFactory { get; set; } = null!;

    /// <summary>
    /// Creates a Search/Query page for a given entity, optionally pre-filtered.
    /// </summary>
    [Parameter, EditorRequired]
    public Func<string, SmartFilterDefinition?, RenderFragment> SearchPageFactory { get; set; } = null!;

    /// <summary>
    /// Creates a new-record Detail Form for a given entity name.
    /// </summary>
    [Parameter, EditorRequired]
    public Func<string, RenderFragment> NewRecordFactory { get; set; } = null!;

    /// <summary>
    /// Creates a report page for a given report name. Optional.
    /// </summary>
    [Parameter]
    public Func<string, RenderFragment>? ReportPageFactory { get; set; }

    /// <summary>
    /// Creates a dashboard page <see cref="RenderFragment"/> for a given
    /// <see cref="DashboardConfiguration"/>. When null, dashboard tabs render
    /// a placeholder message.
    /// </summary>
    [Parameter]
    public Func<DashboardConfiguration, RenderFragment>? DashboardPageFactory { get; set; }

    /// <summary>
    /// Resolves a <see cref="DashboardConfiguration"/> by its identifier.
    /// Required for dashboard tab rendering when a dashboard is opened by ID.
    /// </summary>
    [Parameter]
    public Func<string, DashboardConfiguration?>? DashboardConfigurationResolver { get; set; }

    /// <summary>
    /// Home page configuration controlling the initial workspace tab shown on login.
    /// </summary>
    [Parameter]
    public HomePageConfiguration? HomePageConfiguration { get; set; }

    /// <summary>
    /// The current user's roles, used to resolve per-role home page overrides.
    /// </summary>
    [Parameter]
    public IEnumerable<string> UserRoles { get; set; } = [];

    /// <summary>
    /// Application title displayed in the navigation panel.
    /// </summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>
    /// Maximum number of open workspace tabs.
    /// </summary>
    [Parameter]
    public int MaxTabs { get; set; } = 15;

    /// <summary>
    /// Report items for the navigation panel.
    /// </summary>
    [Parameter]
    public IReadOnlyList<NavigationReportItem> ReportItems { get; set; } = [];

    /// <summary>
    /// Custom empty state content for the workspace.
    /// </summary>
    [Parameter]
    public RenderFragment? EmptyStateTemplate { get; set; }

    /// <summary>
    /// Provides pinned and recent/frequent artifact data for the top menu bar dropdowns.
    /// When null, the top menu bar is not rendered.
    /// </summary>
    [Parameter]
    public IUsageTrackingProvider? UsageTrackingProvider { get; set; }

    /// <summary>
    /// Provides artifact catalog data for the top menu bar dropdowns.
    /// When null, the top menu bar is not rendered.
    /// </summary>
    [Parameter]
    public IArtifactCatalogProvider? CatalogProvider { get; set; }

    /// <summary>
    /// The current user's identifier, passed to the top menu bar for usage tracking.
    /// </summary>
    [Parameter]
    public string UserId { get; set; } = "";

    /// <summary>
    /// Optional content for the right side of the menu bar (user profile, settings, etc.).
    /// </summary>
    [Parameter]
    public RenderFragment? MenuBarTrailingContent { get; set; }

    // ── Navigation event handlers ────────────────────────────────────

    private async Task HandleRecordSelectedAsync((string EntityName, object PrimaryKey) args)
    {
        var meta = MetadataProvider.GetEntityMetadata(args.EntityName);
        var title = meta?.DisplayNameSingular ?? args.EntityName;

        var tab = new WorkspaceTab
        {
            TabType = WorkspaceTabType.Detail,
            EntityName = args.EntityName,
            RecordKey = args.PrimaryKey,
            Title = $"{title}: {args.PrimaryKey}",
            Content = DetailFormFactory(args.EntityName, args.PrimaryKey)
        };

        if (_workspace is not null)
        {
            await _workspace.OpenTabAsync(tab);
        }
    }

    private async Task HandleSearchRequestedAsync(
        (string EntityName, SmartFilterDefinition? PreFilter) args)
    {
        var meta = MetadataProvider.GetEntityMetadata(args.EntityName);
        var title = $"{meta?.DisplayName ?? args.EntityName} Search";

        var tab = new WorkspaceTab
        {
            TabType = WorkspaceTabType.Search,
            EntityName = args.EntityName,
            Title = title,
            Content = SearchPageFactory(args.EntityName, args.PreFilter)
        };

        if (_workspace is not null)
        {
            await _workspace.OpenTabAsync(tab);
        }
    }

    private async Task HandleNewRequestedAsync(string entityName)
    {
        var meta = MetadataProvider.GetEntityMetadata(entityName);
        var title = $"New {meta?.DisplayNameSingular ?? entityName}";

        var tab = new WorkspaceTab
        {
            TabType = WorkspaceTabType.Detail,
            EntityName = entityName,
            Title = title,
            IsDirty = true,
            Content = NewRecordFactory(entityName)
        };

        if (_workspace is not null)
        {
            await _workspace.OpenTabAsync(tab);
        }
    }

    private async Task HandleReportSelectedAsync(string reportName)
    {
        if (ReportPageFactory is null) return;

        var tab = new WorkspaceTab
        {
            TabType = WorkspaceTabType.Report,
            Title = reportName,
            Content = ReportPageFactory(reportName)
        };

        if (_workspace is not null)
        {
            await _workspace.OpenTabAsync(tab);
        }
    }

    // ── Entity navigator event handler ─────────────────────────────────

    private async Task HandleEntitySelectedAsync(string entityName)
    {
        await HandleSearchRequestedAsync((entityName, null));
    }

    // ── Menu bar event handlers ───────────────────────────────────────

    private async Task HandleMenuArtifactSelectedAsync(
        (ArtifactType ArtifactType, string ArtifactId, string Name) args)
    {
        // Track the access for future dropdown population
        if (UsageTrackingProvider is not null)
        {
            await UsageTrackingProvider.TrackAccessAsync(
                UserId, args.ArtifactType, args.ArtifactId);
        }

        // Map artifact type to the appropriate tab type and factory
        switch (args.ArtifactType)
        {
            case ArtifactType.Form:
                // Open the entity's search tab via the existing search flow
                await HandleSearchRequestedAsync((args.ArtifactId, null));
                break;

            case ArtifactType.Query:
                await HandleSearchRequestedAsync((args.ArtifactId, null));
                break;

            case ArtifactType.Report:
                await HandleReportSelectedAsync(args.ArtifactId);
                break;

            case ArtifactType.Dashboard:
                await OpenDashboardTabAsync(args.ArtifactId, args.Name);
                break;
        }
    }

    private async Task HandleCatalogRequestedAsync(ArtifactType artifactType)
    {
        var label = artifactType switch
        {
            ArtifactType.Form => "All Forms",
            ArtifactType.Query => "All Queries",
            ArtifactType.Report => "All Reports",
            ArtifactType.Dashboard => "All Dashboards",
            _ => $"All {artifactType}"
        };

        var tab = new WorkspaceTab
        {
            TabType = WorkspaceTabType.Catalog,
            Title = label,
            Content = BuildCatalogContent(artifactType),
            State =
            {
                ["CatalogType"] = artifactType
            }
        };

        if (_workspace is not null)
        {
            await _workspace.OpenTabAsync(tab);
        }
    }

    private RenderFragment BuildCatalogContent(ArtifactType artifactType) => builder =>
    {
#pragma warning disable CS0618
        builder.OpenComponent<DabblerCatalogPage>(0);
        builder.AddAttribute(1, nameof(DabblerCatalogPage.ArtifactType), artifactType);
        builder.AddAttribute(2, nameof(DabblerCatalogPage.CatalogProvider), CatalogProvider!);
        builder.AddAttribute(3, nameof(DabblerCatalogPage.UsageTrackingProvider), UsageTrackingProvider!);
        builder.AddAttribute(4, nameof(DabblerCatalogPage.UserId), UserId);
        builder.AddAttribute(5, nameof(DabblerCatalogPage.OnArtifactSelected),
            EventCallback.Factory.Create<(ArtifactType, string, string)>(this, HandleMenuArtifactSelectedAsync));
        builder.CloseComponent();
#pragma warning restore CS0618
    };

    // ── Home page routing ──────────────────────────────────────────────

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await OpenHomePageAsync();
    }

    private async Task OpenHomePageAsync()
    {
        if (HomePageConfiguration is null) return;

        var target = HomePageConfiguration.ResolveHomePage(UserRoles);
        if (string.IsNullOrWhiteSpace(target)) return;

        var parts = target.Split(':', 2);
        if (parts.Length != 2) return;

        var type = parts[0];
        var id = parts[1];

        switch (type)
        {
            case "Dashboard":
                await OpenDashboardTabAsync(id, null);
                break;

            case "Form":
                await HandleSearchRequestedAsync((id, null));
                break;
        }
    }

    // ── Dashboard tab helper ────────────────────────────────────────

    private async Task OpenDashboardTabAsync(string dashboardId, string? title)
    {
        var config = DashboardConfigurationResolver?.Invoke(dashboardId);

        var dashTab = new WorkspaceTab
        {
            TabType = WorkspaceTabType.Dashboard,
            Title = title ?? config?.Name ?? dashboardId,
            Content = BuildDashboardContent(config),
            State = { ["DashboardId"] = dashboardId }
        };

        if (_workspace is not null)
        {
            await _workspace.OpenTabAsync(dashTab);
        }
    }

    private RenderFragment BuildDashboardContent(DashboardConfiguration? config) => builder =>
    {
        if (DashboardPageFactory is not null && config is not null)
        {
            builder.AddContent(0, DashboardPageFactory(config));
        }
        else
        {
            // DabblerDashboardPage was removed during the MudBlazor-native rebuild.
            // App-level dashboards now use explicit MudBlazor composition via route-based pages.
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "dabbler-dashboard-placeholder");
            builder.AddContent(2, "Dashboard rendering requires a DashboardPageFactory parameter.");
            builder.CloseElement();
        }
    };

    // ── Workspace tab event handlers ─────────────────────────────────

    private void HandleTabClose(WorkspaceTab tab)
    {
        _tabs.Remove(tab);

        if (_activeTabId == tab.Id)
        {
            _activeTabId = _tabs.LastOrDefault()?.Id;
            UpdateFocusedEntity();
        }

        StateHasChanged();
    }

    private void HandleTabOpen(WorkspaceTab tab)
    {
        _tabs.Add(tab);
        _activeTabId = tab.Id;
        UpdateFocusedEntity();
        StateHasChanged();
    }

    private void HandleTabReorder((int OldIndex, int NewIndex) args)
    {
        if (args.OldIndex < 0 || args.OldIndex >= _tabs.Count
            || args.NewIndex < 0 || args.NewIndex >= _tabs.Count)
            return;

        var tab = _tabs[args.OldIndex];
        _tabs.RemoveAt(args.OldIndex);
        _tabs.Insert(args.NewIndex, tab);
        StateHasChanged();
    }

    private Task HandleActiveTabChangedAsync(string? tabId)
    {
        _activeTabId = tabId;
        UpdateFocusedEntity();
        StateHasChanged();
        return Task.CompletedTask;
    }

    private void HandleDirtyTabCloseConfirmed(WorkspaceTab tab)
    {
        // The workspace already handles the close; nothing extra needed here.
    }

    // ── Focus propagation ────────────────────────────────────────────

    private void UpdateFocusedEntity()
    {
        var activeTab = _tabs.FirstOrDefault(t => t.Id == _activeTabId);

        if (activeTab is { TabType: WorkspaceTabType.Detail })
        {
            _focusedEntityName = activeTab.EntityName;
            _focusedRecordKey = activeTab.RecordKey;
        }
        else
        {
            _focusedEntityName = activeTab?.EntityName;
            _focusedRecordKey = null;
        }
    }
}
