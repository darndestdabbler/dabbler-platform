// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data;

/// <summary>
/// Provides display labels and abbreviations for filter operators, and default
/// operator sets per <see cref="FilterEditorType"/>.
/// </summary>
public static class FilterOperatorInfo
{
    private static readonly Dictionary<string, (string Abbreviation, string Label)> OperatorDisplay = new()
    {
        [FilterOperators.Equals] = ("=", "Equals"),
        [FilterOperators.Contains] = ("\u2283", "Contains"),           // ⊃
        [FilterOperators.StartsWith] = ("A..", "Starts With"),
        [FilterOperators.EndsWith] = ("..Z", "Ends With"),
        [FilterOperators.GreaterThan] = (">", "Greater Than"),
        [FilterOperators.LessThan] = ("<", "Less Than"),
        [FilterOperators.GreaterThanOrEqual] = ("\u2265", "Greater or Equal"),  // ≥
        [FilterOperators.LessThanOrEqual] = ("\u2264", "Less or Equal"),        // ≤
        [FilterOperators.IsEmpty] = ("\u2205", "Is Empty"),            // ∅
        [FilterOperators.IsNotEmpty] = ("\u2260\u2205", "Is Not Empty"), // ≠∅
        [FilterOperators.In] = ("\u2208", "In"),                       // ∈
    };

    private static readonly IReadOnlyList<string> TextOperators =
    [
        FilterOperators.Contains,
        FilterOperators.Equals,
        FilterOperators.StartsWith,
        FilterOperators.EndsWith,
        FilterOperators.IsEmpty,
        FilterOperators.IsNotEmpty,
    ];

    private static readonly IReadOnlyList<string> NumericOperators =
    [
        FilterOperators.Equals,
        FilterOperators.GreaterThan,
        FilterOperators.LessThan,
        FilterOperators.GreaterThanOrEqual,
        FilterOperators.LessThanOrEqual,
        FilterOperators.IsEmpty,
        FilterOperators.IsNotEmpty,
    ];

    private static readonly IReadOnlyList<string> DateOperators =
    [
        FilterOperators.Equals,
        FilterOperators.GreaterThan,
        FilterOperators.LessThan,
        FilterOperators.GreaterThanOrEqual,
        FilterOperators.LessThanOrEqual,
        FilterOperators.IsEmpty,
        FilterOperators.IsNotEmpty,
    ];

    private static readonly IReadOnlyList<string> CheckBoxOperators =
    [
        FilterOperators.Equals,
    ];

    private static readonly IReadOnlyList<string> ComboBoxOperators =
    [
        FilterOperators.Equals,
        FilterOperators.IsEmpty,
        FilterOperators.IsNotEmpty,
    ];

    /// <summary>
    /// Gets the short abbreviation for a filter operator (e.g., "=" for Equals, "⊃" for Contains).
    /// </summary>
    public static string GetAbbreviation(string op) =>
        OperatorDisplay.TryGetValue(op, out var info) ? info.Abbreviation : op;

    /// <summary>
    /// Gets the display label for a filter operator (e.g., "Equals", "Contains").
    /// </summary>
    public static string GetLabel(string op) =>
        OperatorDisplay.TryGetValue(op, out var info) ? info.Label : op;

    /// <summary>
    /// Gets the default set of operators for a given editor type.
    /// </summary>
    public static IReadOnlyList<string> GetDefaultOperators(FilterEditorType editorType) =>
        editorType switch
        {
            FilterEditorType.Text => TextOperators,
            FilterEditorType.NumericText => NumericOperators,
            FilterEditorType.Date => DateOperators,
            FilterEditorType.CheckBox => CheckBoxOperators,
            FilterEditorType.ComboBox => ComboBoxOperators,
            _ => TextOperators,
        };

    /// <summary>
    /// Gets the default operator for a given editor type.
    /// </summary>
    public static string GetDefaultOperator(FilterEditorType editorType) =>
        editorType switch
        {
            FilterEditorType.Text => FilterOperators.Contains,
            FilterEditorType.NumericText => FilterOperators.Equals,
            FilterEditorType.Date => FilterOperators.Equals,
            FilterEditorType.CheckBox => FilterOperators.Equals,
            FilterEditorType.ComboBox => FilterOperators.Equals,
            _ => FilterOperators.Contains,
        };

    /// <summary>
    /// Formats a filter for display (e.g., "Name contains 'John'").
    /// </summary>
    public static string FormatFilter(string? header, string op, object? value)
    {
        var label = GetAbbreviation(op);
        var displayHeader = header ?? "?";

        if (op == FilterOperators.IsEmpty)
            return $"{displayHeader} is empty";
        if (op == FilterOperators.IsNotEmpty)
            return $"{displayHeader} is not empty";

        var displayValue = value switch
        {
            null => "",
            string s when s.Length > 20 => s[..20] + "\u2026", // …
            _ => value.ToString() ?? ""
        };

        return $"{displayHeader} {label} {displayValue}".TrimEnd();
    }
}
