// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data.Metadata;

/// <summary>
/// Describes a parent-to-child relationship, including many-to-many junction entity configuration.
/// Used to render child tabs in the detail form.
/// </summary>
public class ChildRelationshipMetadata
{
    /// <summary>
    /// Display name for the child tab (e.g., "Orders").
    /// </summary>
    public string RelationshipName { get; set; } = "";

    /// <summary>
    /// The entity name of the child (e.g., "Order").
    /// </summary>
    public string ChildEntityName { get; set; } = "";

    /// <summary>
    /// Foreign key field on the child entity that references the parent.
    /// </summary>
    public string ForeignKeyField { get; set; } = "";

    /// <summary>
    /// Sort order for child tabs in the detail form.
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// Whether this relationship appears as a child tab. When false, the relationship
    /// exists in metadata but is not rendered.
    /// </summary>
    public bool ShowAsTab { get; set; } = true;

    // ── Many-to-Many configuration ───────────────────────────

    /// <summary>
    /// When true, the child entity is a junction table for a many-to-many relationship.
    /// </summary>
    public bool IsJunctionEntity { get; set; } = false;

    /// <summary>
    /// The entity on the far side of the M:M relationship (e.g., "Product").
    /// </summary>
    public string? FarSideEntityName { get; set; }

    /// <summary>
    /// The FK field on the junction entity that points to the far-side entity.
    /// </summary>
    public string? FarSideForeignKeyField { get; set; }

    /// <summary>
    /// Junction table columns to display in the merged-view grid (e.g., Quantity, Role).
    /// </summary>
    public List<string>? JunctionColumns { get; set; }

    /// <summary>
    /// Far-side entity columns to display in the merged-view grid (e.g., ProductName, Price).
    /// </summary>
    public List<string>? FarSideColumns { get; set; }

    // ── Navigation behavior ──────────────────────────────────

    /// <summary>
    /// Explicit click action override. When null, the default popup-vs-tab rules apply.
    /// </summary>
    public ChildClickAction? ClickAction { get; set; }
}
