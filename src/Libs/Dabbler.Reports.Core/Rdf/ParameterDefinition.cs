namespace Dabbler.Reports.Core.Rdf;

/// <summary>
/// Declares a report parameter including its data type, UI control type,
/// prompt text, validation rules, and optional lookup query for populating choice lists.
/// </summary>
public class ParameterDefinition
{
    /// <summary>Gets or sets the parameter identifier used in SQL query binding (e.g., "StartDate").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the .NET data type (e.g., "DateTime", "Int32", "String").</summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>Gets or sets the input control type to generate for the parameter prompt.</summary>
    public ParameterControlType ControlType { get; set; } = ParameterControlType.TextBox;

    /// <summary>Gets or sets the label displayed to the user.</summary>
    public string PromptText { get; set; } = string.Empty;

    /// <summary>Gets or sets the default value (static or dynamic expression like "Today", "FirstOfMonth").</summary>
    public string? DefaultValue { get; set; }

    /// <summary>Gets or sets whether the parameter is required for report execution.</summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>Gets or sets validation rules (min/max, regex, date range constraints).</summary>
    public ParameterValidation? Validation { get; set; }

    /// <summary>Gets or sets the SQL query for populating ComboBox/MultiSelectList choice lists.</summary>
    public string? LookupQuery { get; set; }

    /// <summary>Gets or sets the display column from the lookup query results.</summary>
    public string? LookupDisplayField { get; set; }

    /// <summary>Gets or sets the value column from the lookup query results.</summary>
    public string? LookupValueField { get; set; }

    /// <summary>Gets or sets the parent parameter name for cascading parameter dependencies.</summary>
    public string? DependsOn { get; set; }

    /// <summary>Gets or sets the display order in the parameter prompt form.</summary>
    public int SortOrder { get; set; }
}

/// <summary>
/// Control type to generate for a report parameter prompt.
/// </summary>
public enum ParameterControlType
{
    /// <summary>Free-form text input.</summary>
    TextBox,

    /// <summary>Date picker control.</summary>
    DatePicker,

    /// <summary>Dropdown with lookup-populated choices.</summary>
    ComboBox,

    /// <summary>Multi-selection list with lookup-populated choices.</summary>
    MultiSelectList,

    /// <summary>Boolean checkbox.</summary>
    CheckBox,

    /// <summary>Numeric input with up/down buttons.</summary>
    NumericUpDown
}

/// <summary>
/// Validation rules for a report parameter.
/// </summary>
public class ParameterValidation
{
    /// <summary>Gets or sets the minimum value (for numeric/date parameters).</summary>
    public string? MinValue { get; set; }

    /// <summary>Gets or sets the maximum value (for numeric/date parameters).</summary>
    public string? MaxValue { get; set; }

    /// <summary>Gets or sets a regex pattern for text validation.</summary>
    public string? Pattern { get; set; }
}
