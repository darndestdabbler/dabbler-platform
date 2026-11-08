// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data.Metadata;

/// <summary>
/// Defines a widget's type, position, and configuration within a
/// <see cref="DashboardConfiguration"/> grid layout.
/// </summary>
public class WidgetPlacement
{
    /// <summary>
    /// Unique identifier for this placement.
    /// </summary>
    public string PlacementId { get; set; } = "";

    /// <summary>
    /// Identifier of the parent <see cref="DashboardConfiguration"/> this widget belongs to.
    /// </summary>
    public string DashboardId { get; set; } = "";

    // ── Grid position ────────────────────────────────────────

    /// <summary>
    /// Row position in the dashboard layout grid (1-based).
    /// </summary>
    public int GridRow { get; set; } = 1;

    /// <summary>
    /// Column position in the dashboard layout grid (1-based).
    /// </summary>
    public int GridColumn { get; set; } = 1;

    /// <summary>
    /// Number of columns the widget spans (default 1).
    /// </summary>
    public int ColSpan { get; set; } = 1;

    /// <summary>
    /// Number of rows the widget spans (default 1).
    /// </summary>
    public int RowSpan { get; set; } = 1;

    // ── Widget identity ──────────────────────────────────────

    /// <summary>
    /// The type of widget to render at this position.
    /// </summary>
    public WidgetType WidgetType { get; set; }

    /// <summary>
    /// Widget-type-specific configuration serialized as JSON.
    /// Structure varies by <see cref="WidgetType"/> (e.g., label, icon, navigation target,
    /// query binding, color, badge query).
    /// </summary>
    public string ConfigJson { get; set; } = "{}";

    /// <summary>
    /// Ordering hint when multiple widgets occupy the same grid cell.
    /// </summary>
    public int SortOrder { get; set; } = 0;
}
