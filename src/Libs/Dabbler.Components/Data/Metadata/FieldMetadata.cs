// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data.Metadata;

/// <summary>
/// Describes a single field on an entity, including data type, display label, and editor hints.
/// </summary>
public class FieldMetadata
{
    /// <summary>
    /// The property name on the data model (e.g., "FirstName").
    /// </summary>
    public string FieldName { get; set; } = "";

    /// <summary>
    /// Human-readable label shown in forms and column headers (e.g., "First Name").
    /// </summary>
    public string DisplayLabel { get; set; } = "";

    /// <summary>
    /// CLR type name: "string", "int", "decimal", "DateTime", "bool", etc.
    /// </summary>
    public string DataType { get; set; } = "string";

    /// <summary>
    /// Whether the field must have a value before saving.
    /// </summary>
    public bool IsRequired { get; set; } = false;

    /// <summary>
    /// Whether the field is read-only in the detail form.
    /// </summary>
    public bool IsReadOnly { get; set; } = false;

    /// <summary>
    /// Whether this field is part of the entity's primary key.
    /// </summary>
    public bool IsPrimaryKey { get; set; } = false;

    /// <summary>
    /// Whether this field is a foreign key to another entity.
    /// </summary>
    public bool IsForeignKey { get; set; } = false;

    /// <summary>
    /// The target entity name when <see cref="IsForeignKey"/> is true.
    /// </summary>
    public string? ForeignKeyEntity { get; set; }

    /// <summary>
    /// Optional format string for display (e.g., "C2" for currency, "d" for short date).
    /// </summary>
    public string? FormatString { get; set; }

    /// <summary>
    /// Optional default value for new records (as a string; parsed at runtime).
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Sort order for display within forms and grids.
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// Which column in the detail form layout (0, 1, 2, etc.). Null uses automatic placement.
    /// </summary>
    public int? DetailFormColumn { get; set; }

    /// <summary>
    /// Explicit editor type override: "text", "combobox", "checkbox", "date", "numeric".
    /// When null, the editor is inferred from <see cref="DataType"/>.
    /// </summary>
    public string? EditorType { get; set; }
}
