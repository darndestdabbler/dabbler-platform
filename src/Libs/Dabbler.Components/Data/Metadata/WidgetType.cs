// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data.Metadata;

/// <summary>
/// Identifies the kind of widget placed on a <see cref="DashboardConfiguration"/>.
/// </summary>
public enum WidgetType
{
    // ── Data widgets ─────────────────────────────────────────

    /// <summary>
    /// Displays a single aggregate value (count, sum, average) with an optional trend indicator.
    /// </summary>
    SummaryCard,

    /// <summary>
    /// Bar, line, pie, or donut chart bound to a query result set.
    /// </summary>
    Chart,

    /// <summary>
    /// An embedded query/report page in compact mode with reduced row height and fewer columns.
    /// </summary>
    CompactGrid,

    // ── Navigation widgets ───────────────────────────────────

    /// <summary>
    /// A large clickable card with icon, label, optional subtitle, and optional badge count.
    /// Navigates to a Form, Query, Report, or Dashboard.
    /// </summary>
    NavigationCard,

    /// <summary>
    /// A titled section containing a list of text links, each navigating to an artifact.
    /// </summary>
    QuickLinksGroup,

    /// <summary>
    /// Auto-populated list of the user's most recently accessed records.
    /// </summary>
    RecentItems,

    /// <summary>
    /// User-curated list of pinned records and artifacts.
    /// </summary>
    Favorites,

    // ── Composite / hybrid widgets ───────────────────────────

    /// <summary>
    /// A compact grid pre-filtered to show items requiring action, with urgency indicators.
    /// </summary>
    AttentionList,

    /// <summary>
    /// A search box scoped to a specific entity; selecting a result navigates to its detail tab.
    /// </summary>
    EmbeddedSearch
}
