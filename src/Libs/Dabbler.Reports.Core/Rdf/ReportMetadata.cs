namespace Dabbler.Reports.Core.Rdf;

/// <summary>
/// Report-level metadata including title, page dimensions, and margins.
/// </summary>
public class ReportMetadata
{
    /// <summary>Gets or sets the report name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the report description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the report author.</summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>Gets or sets the RDF schema version.</summary>
    public string Version { get; set; } = "1.0";

    /// <summary>Gets or sets the page width in inches.</summary>
    public double PageWidth { get; set; } = 8.5;

    /// <summary>Gets or sets the page height in inches.</summary>
    public double PageHeight { get; set; } = 11.0;

    /// <summary>Gets or sets the page orientation.</summary>
    public PageOrientation Orientation { get; set; } = PageOrientation.Portrait;

    /// <summary>Gets or sets the top margin in inches.</summary>
    public double MarginTop { get; set; } = 1.0;

    /// <summary>Gets or sets the bottom margin in inches.</summary>
    public double MarginBottom { get; set; } = 1.0;

    /// <summary>Gets or sets the left margin in inches.</summary>
    public double MarginLeft { get; set; } = 1.0;

    /// <summary>Gets or sets the right margin in inches.</summary>
    public double MarginRight { get; set; } = 1.0;

    /// <summary>Gets or sets the default font family for elements without a style.</summary>
    public string DefaultFontFamily { get; set; } = "Segoe UI";

    /// <summary>Gets or sets the default font size in points for elements without a style.</summary>
    public double DefaultFontSize { get; set; } = 10;
}

/// <summary>
/// Page orientation for report output.
/// </summary>
public enum PageOrientation
{
    /// <summary>Portrait orientation (taller than wide).</summary>
    Portrait,

    /// <summary>Landscape orientation (wider than tall).</summary>
    Landscape
}
