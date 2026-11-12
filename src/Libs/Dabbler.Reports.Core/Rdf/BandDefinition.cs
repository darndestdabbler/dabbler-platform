namespace Dabbler.Reports.Core.Rdf;

/// <summary>
/// Defines a band (section) within the report layout, such as a page header,
/// detail band, or group footer. Contains positioned elements.
/// </summary>
public class BandDefinition
{
    /// <summary>Gets or sets the unique name of this band.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the band type.</summary>
    public BandType Type { get; set; }

    /// <summary>Gets or sets the band height in inches.</summary>
    public double Height { get; set; }

    /// <summary>
    /// Gets or sets the group name for <see cref="BandType.GroupHeader"/> and
    /// <see cref="BandType.GroupFooter"/> bands. Links to a grouping entry by
    /// <see cref="GroupDefinition.FieldName"/> or <see cref="GroupDefinition.GroupExpression"/>.
    /// </summary>
    public string? GroupName { get; set; }

    /// <summary>Gets or sets an expression that must evaluate to true for the band to render.</summary>
    public string? PrintWhenExpression { get; set; }

    /// <summary>Gets or sets whether a page break is inserted before this band.</summary>
    public bool PageBreakBefore { get; set; }

    /// <summary>Gets or sets whether a page break is inserted after this band.</summary>
    public bool PageBreakAfter { get; set; }

    /// <summary>
    /// Gets or sets the background color for this band as a hex string (e.g., "#F2F2F2").
    /// When set, a filled rectangle of this color is drawn behind all elements in the band.
    /// For Detail bands with <see cref="AlternateBackgroundColor"/> also set, this color
    /// is used for even-indexed rows (0, 2, 4, ...).
    /// </summary>
    public string? BackgroundColor { get; set; }

    /// <summary>
    /// Gets or sets the alternate background color for Detail bands as a hex string.
    /// When set on a Detail band, odd-indexed rows (1, 3, 5, ...) use this color while
    /// even-indexed rows use <see cref="BackgroundColor"/>. This produces "green bar"
    /// or alternating-row coloring. Ignored on non-Detail bands.
    /// Row alternation is continuous across the entire report (does not reset at group boundaries).
    /// </summary>
    public string? AlternateBackgroundColor { get; set; }

    /// <summary>Gets or sets the elements positioned within this band.</summary>
    public List<ElementDefinition> Elements { get; set; } = [];
}
