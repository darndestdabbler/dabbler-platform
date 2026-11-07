// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data.Metadata;

/// <summary>
/// Defines a dashboard (or switchboard) page and its layout grid.
/// A switchboard is simply a dashboard whose widgets are navigational rather than analytical.
/// </summary>
public class DashboardConfiguration
{
    /// <summary>
    /// Unique identifier for this dashboard.
    /// </summary>
    public string DashboardId { get; set; } = "";

    /// <summary>
    /// Display name shown in the menu bar and workspace tab title.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Optional description of the dashboard's purpose.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Number of columns in the CSS Grid layout (e.g., 3, 4, 6).
    /// </summary>
    public int ColumnCount { get; set; } = 3;

    /// <summary>
    /// Whether this is a platform-provided dashboard that cannot be deleted by users.
    /// </summary>
    public bool IsSystemDashboard { get; set; } = false;

    // ── Widget placements ────────────────────────────────────

    /// <summary>
    /// The widgets placed on this dashboard, defining their type, position, and configuration.
    /// </summary>
    public List<WidgetPlacement> Widgets { get; set; } = new();
}
