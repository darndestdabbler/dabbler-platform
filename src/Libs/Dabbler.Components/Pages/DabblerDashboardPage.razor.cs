// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using System.Text.Json;
using Dabbler.Components.Data.Metadata;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Dabbler.Components;

/// <summary>
/// A dashboard page that renders a CSS Grid of widgets based on a
/// <see cref="DashboardConfiguration"/>. Supports navigation widgets
/// (NavigationCard, QuickLinksGroup, RecentItems, Favorites, EmbeddedSearch)
/// with analytical widgets deferred to Phase 3.
/// </summary>
public partial class DabblerDashboardPage : DabblerComponentBase
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // ── Parameters ──────────────────────────────────────────────────

    /// <summary>
    /// The dashboard configuration defining the grid layout and widget placements.
    /// </summary>
    [Parameter, EditorRequired]
    public DashboardConfiguration Configuration { get; set; } = null!;

    /// <summary>
    /// Provides pinned and recent artifact data for RecentItems and Favorites widgets.
    /// </summary>
    [Parameter]
    public IUsageTrackingProvider? UsageTrackingProvider { get; set; }

    /// <summary>
    /// The current user identifier for usage queries.
    /// </summary>
    [Parameter]
    public string UserId { get; set; } = "";

    /// <summary>
    /// Raised when a navigation widget requests opening an artifact (form, report, dashboard).
    /// </summary>
    [Parameter]
    public EventCallback<(ArtifactType ArtifactType, string ArtifactId, string Name)> OnArtifactSelected { get; set; }

    /// <summary>
    /// Raised when a widget requests opening a record's detail tab.
    /// </summary>
    [Parameter]
    public EventCallback<(string EntityName, object PrimaryKey)> OnRecordSelected { get; set; }

    /// <summary>
    /// Raised when a widget requests opening an entity's search tab,
    /// optionally with a search text for pre-filtering.
    /// </summary>
    [Parameter]
    public EventCallback<(string EntityName, string? SearchText)> OnSearchRequested { get; set; }

    // ── Widget rendering ────────────────────────────────────────────

    private RenderFragment RenderWidget(WidgetPlacement placement) => builder =>
    {
        switch (placement.WidgetType)
        {
            case WidgetType.NavigationCard:
                var navConfig = Deserialize<NavigationCardConfig>(placement.ConfigJson);
                builder.OpenComponent<DabblerNavigationCard>(0);
                builder.AddAttribute(1, nameof(DabblerNavigationCard.Label), navConfig?.Label ?? "");
                builder.AddAttribute(2, nameof(DabblerNavigationCard.Icon), navConfig?.Icon);
                builder.AddAttribute(3, nameof(DabblerNavigationCard.Subtitle), navConfig?.Subtitle);
                builder.AddAttribute(4, nameof(DabblerNavigationCard.BadgeText), navConfig?.BadgeText);
                builder.AddAttribute(5, nameof(DabblerNavigationCard.Color), navConfig?.Color);
                builder.AddAttribute(6, nameof(DabblerNavigationCard.OnClick),
                    EventCallback.Factory.Create(this, () => HandleNavigationCardClickAsync(navConfig)));
                builder.CloseComponent();
                break;

            case WidgetType.QuickLinksGroup:
                var linksConfig = Deserialize<QuickLinksGroupConfig>(placement.ConfigJson);
                builder.OpenComponent<DabblerQuickLinksGroup>(0);
                builder.AddAttribute(1, nameof(DabblerQuickLinksGroup.Title), linksConfig?.Title ?? "");
                builder.AddAttribute(2, nameof(DabblerQuickLinksGroup.Links), linksConfig?.Links ?? []);
                builder.AddAttribute(3, nameof(DabblerQuickLinksGroup.OnLinkClick),
                    EventCallback.Factory.Create<QuickLink>(this, HandleQuickLinkClickAsync));
                builder.CloseComponent();
                break;

            case WidgetType.RecentItems:
                if (UsageTrackingProvider is null)
                {
                    RenderCompatibilityWidgetPlaceholder(
                        builder,
                        widgetName: "RecentItems",
                        message: "Recent items is a compatibility-only dashboard widget that requires usage tracking.");
                    break;
                }

                builder.OpenComponent<DabblerRecentItems>(0);
                builder.AddAttribute(1, nameof(DabblerRecentItems.UsageTrackingProvider), UsageTrackingProvider);
                builder.AddAttribute(2, nameof(DabblerRecentItems.UserId), UserId);
                builder.AddAttribute(3, nameof(DabblerRecentItems.OnItemClick),
                    EventCallback.Factory.Create<ArtifactUsageRecord>(this, HandleRecentItemClickAsync));
                builder.CloseComponent();
                break;

            case WidgetType.Favorites:
                if (UsageTrackingProvider is null)
                {
                    RenderCompatibilityWidgetPlaceholder(
                        builder,
                        widgetName: "Favorites",
                        message: "Favorites is a compatibility-only dashboard widget that requires usage tracking.");
                    break;
                }

                builder.OpenComponent<DabblerFavorites>(0);
                builder.AddAttribute(1, nameof(DabblerFavorites.UsageTrackingProvider), UsageTrackingProvider);
                builder.AddAttribute(2, nameof(DabblerFavorites.UserId), UserId);
                builder.AddAttribute(3, nameof(DabblerFavorites.OnItemClick),
                    EventCallback.Factory.Create<ArtifactUsageRecord>(this, HandleFavoriteClickAsync));
                builder.CloseComponent();
                break;

            case WidgetType.EmbeddedSearch:
                var searchConfig = Deserialize<EmbeddedSearchConfig>(placement.ConfigJson);
                builder.OpenComponent<DabblerEmbeddedSearch>(0);
                builder.AddAttribute(1, nameof(DabblerEmbeddedSearch.EntityName), searchConfig?.EntityName ?? "");
                builder.AddAttribute(2, nameof(DabblerEmbeddedSearch.Placeholder), searchConfig?.Placeholder);
                builder.AddAttribute(3, nameof(DabblerEmbeddedSearch.OnSearchSubmitted),
                    EventCallback.Factory.Create<(string, string?)>(this,
                        args => OnSearchRequested.InvokeAsync(args)));
                builder.CloseComponent();
                break;

            default:
                // Data widgets (SummaryCard, Chart, CompactGrid, AttentionList) are Phase 3
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "dabbler-widget-placeholder");
                builder.AddContent(2, $"{placement.WidgetType} (coming soon)");
                builder.CloseElement();
                break;
        }
    };

    // ── Navigation handlers ─────────────────────────────────────────

    private async Task HandleNavigationCardClickAsync(NavigationCardConfig? config)
    {
        if (config?.NavigateTo is null) return;

        var nav = config.NavigateTo;
        if (Enum.TryParse<ArtifactType>(nav.Type, true, out var artifactType))
        {
            await OnArtifactSelected.InvokeAsync((artifactType, nav.EntityId ?? "", config.Label ?? ""));
        }
    }

    private async Task HandleQuickLinkClickAsync(QuickLink link)
    {
        if (Enum.TryParse<ArtifactType>(link.Type, true, out var artifactType))
        {
            await OnArtifactSelected.InvokeAsync((artifactType, link.ArtifactId, link.Label));
        }
    }

    private async Task HandleRecentItemClickAsync(ArtifactUsageRecord record)
    {
        await OnArtifactSelected.InvokeAsync((record.ArtifactType, record.ArtifactId, record.ArtifactId));
    }

    private async Task HandleFavoriteClickAsync(ArtifactUsageRecord record)
    {
        await OnArtifactSelected.InvokeAsync((record.ArtifactType, record.ArtifactId, record.ArtifactId));
    }

    // ── JSON helpers ────────────────────────────────────────────────

    private static T? Deserialize<T>(string json) where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private static void RenderCompatibilityWidgetPlaceholder(
        RenderTreeBuilder builder,
        string widgetName,
        string message)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "dabbler-widget-placeholder dabbler-widget-compatibility");
        builder.AddAttribute(2, "data-compatibility-widget", widgetName);
        builder.AddContent(3, message);
        builder.CloseElement();
    }

    // ── Widget configuration models ─────────────────────────────────

    /// <summary>
    /// Configuration for a <see cref="WidgetType.NavigationCard"/> widget.
    /// </summary>
    public class NavigationCardConfig
    {
        /// <summary>Card title displayed to the user.</summary>
        public string? Label { get; set; }
        /// <summary>MudBlazor icon name shown on the card.</summary>
        public string? Icon { get; set; }
        /// <summary>Secondary text displayed beneath the label.</summary>
        public string? Subtitle { get; set; }
        /// <summary>Optional badge text (e.g., a count) shown on the card.</summary>
        public string? BadgeText { get; set; }
        /// <summary>MudBlazor color name for the card accent.</summary>
        public string? Color { get; set; }
        /// <summary>Where to navigate when the card is clicked.</summary>
        public NavigationTarget? NavigateTo { get; set; }
    }

    /// <summary>
    /// Describes the navigation destination for a card or link click.
    /// </summary>
    public class NavigationTarget
    {
        /// <summary>Target type (e.g., "entity", "report", "dashboard").</summary>
        public string Type { get; set; } = "";
        /// <summary>Optional entity or artifact identifier for the destination.</summary>
        public string? EntityId { get; set; }
    }

    /// <summary>
    /// Configuration for a <see cref="WidgetType.QuickLinksGroup"/> widget.
    /// </summary>
    public class QuickLinksGroupConfig
    {
        /// <summary>Group heading displayed above the links.</summary>
        public string? Title { get; set; }
        /// <summary>The links in this group.</summary>
        public List<QuickLink> Links { get; set; } = new();
    }

    /// <summary>
    /// Configuration for a <see cref="WidgetType.EmbeddedSearch"/> widget.
    /// </summary>
    public class EmbeddedSearchConfig
    {
        /// <summary>The entity to search against when the user types in the search box.</summary>
        public string? EntityName { get; set; }
        /// <summary>Watermark text shown in the empty search input.</summary>
        public string? Placeholder { get; set; }
    }
}

/// <summary>
/// Represents a single link in a <see cref="WidgetType.QuickLinksGroup"/> widget.
/// </summary>
public class QuickLink
{
    /// <summary>Display text for the link.</summary>
    public string Label { get; set; } = "";
    /// <summary>Link type (e.g., "query", "form", "report").</summary>
    public string Type { get; set; } = "";
    /// <summary>Identifier of the artifact to open when the link is clicked.</summary>
    public string ArtifactId { get; set; } = "";
}
