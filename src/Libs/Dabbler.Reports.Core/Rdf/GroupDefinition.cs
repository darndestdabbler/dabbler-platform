namespace Dabbler.Reports.Core.Rdf;

/// <summary>
/// Defines a grouping level including the group field or expression, sort order,
/// references to header/footer bands, and page break behavior.
/// </summary>
public class GroupDefinition
{
    /// <summary>Gets or sets the field name to group by. Either this or <see cref="GroupExpression"/> must be set.</summary>
    public string? FieldName { get; set; }

    /// <summary>
    /// Gets or sets the expression to group by (e.g., "Left([FirstName], 1)").
    /// Used when grouping by a derived value. Either this or <see cref="FieldName"/> must be set.
    /// </summary>
    public string? GroupExpression { get; set; }

    /// <summary>Gets or sets the sort direction within the group.</summary>
    public SortDirection SortDirection { get; set; } = SortDirection.Ascending;

    /// <summary>Gets or sets the name of the group header band.</summary>
    public string? HeaderBandName { get; set; }

    /// <summary>Gets or sets the name of the group footer band.</summary>
    public string? FooterBandName { get; set; }

    /// <summary>Gets or sets whether the engine attempts to keep the group header and first detail row on the same page.</summary>
    public bool KeepTogether { get; set; }

    /// <summary>Gets or sets whether aggregates reset at this group boundary.</summary>
    public bool ResetAggregates { get; set; } = true;

    /// <summary>Gets or sets whether a page break is inserted before each group (except the first).</summary>
    public bool PageBreakBefore { get; set; }

    /// <summary>Gets or sets whether a page break is inserted after each group.</summary>
    public bool PageBreakAfter { get; set; }
}
