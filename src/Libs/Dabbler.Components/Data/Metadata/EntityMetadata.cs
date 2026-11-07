// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data.Metadata;

/// <summary>
/// Describes an entity's structure, display rules, relationships, and navigation behavior.
/// Drives the Navigation Panel, Tabbed Workspace, and Detail Form components.
/// </summary>
/// <example>
/// <code>
/// var customer = new EntityMetadata
/// {
///     EntityName = "Customer",
///     DisplayName = "Customers",
///     DisplayNameSingular = "Customer",
///     PrimaryKeyField = "CustomerId",
///     Fields = new List&lt;FieldMetadata&gt;
///     {
///         new() { FieldName = "CustomerId", DisplayName = "ID", IsPrimaryKey = true },
///         new() { FieldName = "Name", DisplayName = "Name" }
///     }
/// };
/// </code>
/// </example>
public class EntityMetadata
{
    /// <summary>
    /// The programmatic name of the entity (e.g., "Customer").
    /// </summary>
    public string EntityName { get; set; } = "";

    /// <summary>
    /// Plural display name shown in navigation and headers (e.g., "Customers").
    /// </summary>
    public string DisplayName { get; set; } = "";

    /// <summary>
    /// Singular display name used for tab titles and labels (e.g., "Customer").
    /// </summary>
    public string DisplayNameSingular { get; set; } = "";

    /// <summary>
    /// Expression engine template for building a record's display label
    /// (e.g., <c>FirstName + " " + LastName</c>).
    /// </summary>
    public string RecordLabelExpression { get; set; } = "";

    /// <summary>
    /// Optional MudBlazor icon name for the entity in navigation.
    /// </summary>
    public string? IconName { get; set; }

    // ── Column sets ──────────────────────────────────────────

    /// <summary>
    /// All field definitions for this entity.
    /// </summary>
    public List<FieldMetadata> Fields { get; set; } = new();

    /// <summary>
    /// Field names displayed when the entity is shown in a standalone grid.
    /// </summary>
    public List<string> StandaloneColumns { get; set; } = new();

    /// <summary>
    /// Field names displayed in child (parented) grids, typically with the FK column omitted.
    /// </summary>
    public List<string> ParentedColumns { get; set; } = new();

    /// <summary>
    /// Field names used as columns in FilterGrid search views.
    /// </summary>
    public List<string> SearchColumns { get; set; } = new();

    /// <summary>
    /// Field names rendered in the detail form, in display order.
    /// </summary>
    public List<string> DetailFormFields { get; set; } = new();

    // ── Relationships ────────────────────────────────────────

    /// <summary>
    /// Child relationships that appear as tabs in the detail form.
    /// </summary>
    public List<ChildRelationshipMetadata> ChildRelationships { get; set; } = new();

    // ── Smart filters ────────────────────────────────────────

    /// <summary>
    /// Smart filter definitions shown in the navigation panel accordion.
    /// </summary>
    public List<SmartFilterDefinition> SmartFilters { get; set; } = new();

    // ── Navigation ───────────────────────────────────────────

    /// <summary>
    /// Whether this entity appears in the navigation panel.
    /// </summary>
    public bool ShowInNavigation { get; set; } = true;

    /// <summary>
    /// Sort order within the navigation panel.
    /// </summary>
    public int NavigationOrder { get; set; } = 0;

    /// <summary>
    /// Maximum number of record pills shown per smart filter in the navigation panel.
    /// </summary>
    public int SmartFilterTopN { get; set; } = 5;

    // ── Behavior ─────────────────────────────────────────────

    /// <summary>
    /// When true, the entity has no child relationships and may open in a popup.
    /// </summary>
    public bool IsLeafEntity { get; set; } = false;

    /// <summary>
    /// Fields above this count open in a tab instead of a popup for leaf entities.
    /// </summary>
    public int PopupFieldThreshold { get; set; } = 20;
}
