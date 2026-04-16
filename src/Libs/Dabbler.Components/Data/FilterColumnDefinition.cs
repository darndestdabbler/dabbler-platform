// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using Microsoft.AspNetCore.Components;

namespace Dabbler.Components.Data;

/// <summary>
/// Defines the configuration for a single column in a <see cref="Dabbler.Components.FilterPanel{T}"/>.
/// </summary>
/// <remarks>
/// Separates filter metadata from the grid column definition. Each instance describes
/// how the filter cell for one property should be rendered and what operators are available.
/// </remarks>
public class FilterColumnDefinition
{
    /// <summary>
    /// Display text shown as the column header or label.
    /// </summary>
    public string? Header { get; set; }

    /// <summary>
    /// The property name to filter on. Must match <see cref="FilterDescriptor.PropertyName"/>.
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// CSS width for the column (e.g., "200px", "1fr"). Defaults to null (auto).
    /// </summary>
    public string? Width { get; set; }

    /// <summary>
    /// Whether this column is filterable. When false, the cell is disabled.
    /// </summary>
    public bool CanFilter { get; set; } = true;

    /// <summary>
    /// The type of editor to display in the filter cell.
    /// </summary>
    public FilterEditorType EditorType { get; set; } = FilterEditorType.Text;

    /// <summary>
    /// The filter operators available for this column. When null, defaults are
    /// determined by <see cref="FilterOperatorInfo.GetDefaultOperators"/>.
    /// </summary>
    public IReadOnlyList<string>? AvailableOperators { get; set; }

    /// <summary>
    /// The default operator when the user starts typing. Defaults to
    /// <see cref="FilterOperators.Contains"/> for text or <see cref="FilterOperators.Equals"/> for others.
    /// </summary>
    public string? DefaultOperator { get; set; }

    /// <summary>
    /// Whether to show the operator selection button. Defaults to true.
    /// </summary>
    public bool ShowOperator { get; set; } = true;

    /// <summary>
    /// Static items source for <see cref="FilterEditorType.ComboBox"/> columns.
    /// </summary>
    public IEnumerable<object>? ItemsSource { get; set; }

    /// <summary>
    /// Default filter value to pre-populate.
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// Whether this filter is required for validation (e.g., report parameters).
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Placeholder text shown in the editor when empty.
    /// </summary>
    public string? Placeholder { get; set; }

    /// <summary>
    /// The <see cref="PropertyName"/> of a parent column this column depends on.
    /// When the parent column's value changes, this column's value is automatically cleared.
    /// Used for cascading filter dependencies (e.g., Category → SubCategory).
    /// </summary>
    public string? DependsOn { get; set; }

    /// <summary>
    /// Optional custom editor template. When set, replaces the built-in editor for this column.
    /// The <see cref="FilterCellContext"/> argument provides value and operator bindings and callbacks.
    /// </summary>
    public RenderFragment<FilterCellContext>? EditorTemplate { get; set; }

    /// <summary>
    /// Optional custom serializer for the filter value, used when <see cref="EditorTemplate"/> is set.
    /// When provided, <c>ExportFilter()</c> calls this instead of <c>System.Text.Json</c>.
    /// The returned string is stored verbatim in the preset entry.
    /// </summary>
    public Func<object?, string?>? SerializeValue { get; set; }

    /// <summary>
    /// Optional custom deserializer for the filter value, used when <see cref="EditorTemplate"/> is set.
    /// When provided, <c>ImportFilter()</c> calls this to reconstruct the value from the preset entry string.
    /// </summary>
    public Func<string?, object?>? DeserializeValue { get; set; }

    /// <summary>
    /// Validation rules evaluated against the filter value for this column.
    /// When null, no additional validation is performed beyond <see cref="IsRequired"/>.
    /// Use <see cref="FilterValidation"/> factory methods to build common rules.
    /// </summary>
    public IReadOnlyList<FilterValidationRule>? ValidationRules { get; set; }

    /// <summary>
    /// Gets the effective available operators, falling back to defaults for the editor type.
    /// </summary>
    public IReadOnlyList<string> EffectiveOperators =>
        AvailableOperators ?? FilterOperatorInfo.GetDefaultOperators(EditorType);

    /// <summary>
    /// Gets the effective default operator, falling back to the first available operator.
    /// </summary>
    public string EffectiveDefaultOperator =>
        DefaultOperator ?? FilterOperatorInfo.GetDefaultOperator(EditorType);
}
