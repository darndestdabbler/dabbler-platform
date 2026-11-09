namespace Dabbler.Reports.Core.Rdf;

/// <summary>
/// Maps a query result column to a named report field with formatting.
/// </summary>
public class FieldDefinition
{
    /// <summary>Gets or sets the field name used in expressions and band elements.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the .NET data type (e.g., "String", "Decimal", "DateTime").</summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>Gets or sets the source column name from the query result.</summary>
    public string SourceColumn { get; set; } = string.Empty;

    /// <summary>Gets or sets the format string for display (e.g., "C2", "d", "N0").</summary>
    public string? FormatString { get; set; }
}
