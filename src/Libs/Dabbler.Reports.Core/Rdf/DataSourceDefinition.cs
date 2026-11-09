namespace Dabbler.Reports.Core.Rdf;

/// <summary>
/// Defines the data source for a report, including the connection reference and SQL query.
/// </summary>
public class DataSourceDefinition
{
    /// <summary>Gets or sets the unique name for referencing this data source (e.g., "main").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the connection string name (resolved at runtime, never embedded).</summary>
    public string ConnectionName { get; set; } = string.Empty;

    /// <summary>Gets or sets the data provider type (e.g., "SqlServer", "OleDb").</summary>
    public string ProviderType { get; set; } = string.Empty;

    /// <summary>Gets or sets the SQL query with named parameter placeholders (e.g., @StartDate).</summary>
    public string Query { get; set; } = string.Empty;
}
