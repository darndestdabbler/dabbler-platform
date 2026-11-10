// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data.Metadata;

/// <summary>
/// Categorizes smart filters to determine which configuration fields are relevant.
/// </summary>
public enum SmartFilterCategory
{
    /// <summary>
    /// Filters based on a date/time field and a relative date range.
    /// Uses <see cref="SmartFilterDefinition.DateFieldName"/> and
    /// <see cref="SmartFilterDefinition.RelativeDateRange"/>.
    /// </summary>
    Temporal,

    /// <summary>
    /// Filters based on a relationship to the currently focused record.
    /// Uses <see cref="SmartFilterDefinition.RelatedEntityName"/> and
    /// <see cref="SmartFilterDefinition.ForeignKeyFieldName"/>.
    /// </summary>
    Relational,

    /// <summary>
    /// Filters based on audit trail fields (e.g., last modified by current user).
    /// Uses <see cref="SmartFilterDefinition.AuditFieldName"/> and
    /// <see cref="SmartFilterDefinition.AuditUserFieldName"/>.
    /// </summary>
    Audit,

    /// <summary>
    /// Filters using a custom expression engine expression.
    /// Uses <see cref="SmartFilterDefinition.FilterExpression"/>.
    /// </summary>
    Custom
}
