// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data.Metadata;

/// <summary>
/// Defines a smart filter that appears in the navigation panel's accordion section for an entity.
/// Smart filters return top-N record pills based on temporal, relational, audit, or custom criteria.
/// </summary>
public class SmartFilterDefinition
{
    /// <summary>
    /// Display name shown in the navigation panel (e.g., "since yesterday").
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// The category of this smart filter, determining which fields are used for filtering.
    /// </summary>
    public SmartFilterCategory Category { get; set; }

    /// <summary>
    /// Sort order within the entity's smart filter list.
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// Whether this filter is active. Disabled filters are not shown in the navigation panel.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    // ── Temporal filters ─────────────────────────────────────

    /// <summary>
    /// The date/time field to filter on (e.g., "CreatedDate", "OrderDate").
    /// </summary>
    public string? DateFieldName { get; set; }

    /// <summary>
    /// Relative date range expression: "1d", "7d", "30d", "thisWeek", "thisMonth".
    /// </summary>
    public string? RelativeDateRange { get; set; }

    // ── Relational filters ───────────────────────────────────

    /// <summary>
    /// The related entity name for context-sensitive filtering (e.g., "Customer").
    /// </summary>
    public string? RelatedEntityName { get; set; }

    /// <summary>
    /// The FK field on this entity that references the related entity.
    /// </summary>
    public string? ForeignKeyFieldName { get; set; }

    // ── Audit filters ────────────────────────────────────────

    /// <summary>
    /// The audit field to filter on (e.g., "ModifiedBy", "CreatedDate").
    /// </summary>
    public string? AuditFieldName { get; set; }

    /// <summary>
    /// The field containing the user identifier for audit filtering.
    /// </summary>
    public string? AuditUserFieldName { get; set; }

    // ── Custom filters ───────────────────────────────────────

    /// <summary>
    /// An expression engine expression for custom filter logic.
    /// </summary>
    public string? FilterExpression { get; set; }
}
