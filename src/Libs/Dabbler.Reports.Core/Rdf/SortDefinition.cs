namespace Dabbler.Reports.Core.Rdf;

/// <summary>
/// Defines a sort field and direction for report data ordering.
/// </summary>
public class SortDefinition
{
    /// <summary>Gets or sets the field name to sort by.</summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>Gets or sets the sort direction.</summary>
    public SortDirection Direction { get; set; } = SortDirection.Ascending;
}

/// <summary>
/// Sort direction for report data ordering.
/// </summary>
public enum SortDirection
{
    /// <summary>Sort ascending (A-Z, 0-9, earliest-latest).</summary>
    Ascending,

    /// <summary>Sort descending (Z-A, 9-0, latest-earliest).</summary>
    Descending
}
