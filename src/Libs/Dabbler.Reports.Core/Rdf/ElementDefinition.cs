namespace Dabbler.Reports.Core.Rdf;

/// <summary>
/// Defines a positioned element within a band, such as a text field, static text,
/// line, rectangle, image, or subreport.
/// </summary>
public class ElementDefinition
{
    /// <summary>Gets or sets the element type.</summary>
    public ElementType Type { get; set; }

    /// <summary>Gets or sets the X position in inches relative to the band left edge.</summary>
    public double X { get; set; }

    /// <summary>Gets or sets the Y position in inches relative to the band top edge.</summary>
    public double Y { get; set; }

    /// <summary>Gets or sets the element width in inches.</summary>
    public double Width { get; set; }

    /// <summary>Gets or sets the element height in inches.</summary>
    public double Height { get; set; }

    /// <summary>Gets or sets the style name reference.</summary>
    public string? StyleName { get; set; }

    /// <summary>Gets or sets the field name for simple field binding (shortcut for expression "[FieldName]").</summary>
    public string? FieldName { get; set; }

    /// <summary>Gets or sets the expression for dynamic content (field reference, calculation, aggregate).</summary>
    public string? Expression { get; set; }

    /// <summary>Gets or sets the static text value (for StaticText elements).</summary>
    public string? Value { get; set; }

    /// <summary>Gets or sets the .NET format string override (takes precedence over the field's format string).</summary>
    public string? FormatString { get; set; }

    /// <summary>Gets or sets the image source path or expression (for Image elements).</summary>
    public string? ImageSource { get; set; }

    /// <summary>Gets or sets the image scaling mode (for Image elements).</summary>
    public ImageScaleMode ImageScaleMode { get; set; } = ImageScaleMode.ScaleToFit;

    /// <summary>Gets or sets an expression that must evaluate to true for the element to render.</summary>
    public string? PrintWhenExpression { get; set; }

    /// <summary>Gets or sets the path to a subreport RDF file (for Subreport elements).</summary>
    public string? SubreportPath { get; set; }

    /// <summary>Gets or sets the parameter name-to-expression map for passing to the subreport.</summary>
    public Dictionary<string, string>? SubreportParameters { get; set; }

    /// <summary>Gets or sets the data source name for the subreport (if different from parent).</summary>
    public string? SubreportDataSourceName { get; set; }

    /// <summary>Gets or sets the line direction for Line elements.</summary>
    public LineDirection LineDirection { get; set; } = LineDirection.Horizontal;

    /// <summary>
    /// Gets or sets whether the element height can grow to accommodate wrapped text content.
    /// Only effective for text elements (StaticText, TextField) when the style has TextWrap enabled.
    /// When true, the element and its containing band expand to fit all wrapped text.
    /// </summary>
    public bool CanGrow { get; set; }

    /// <summary>
    /// Gets or sets whether the element height can shrink when text content is shorter than
    /// the defined height. Only effective for text elements (StaticText, TextField) when the
    /// style has TextWrap enabled. When true, the element contracts and the band may shrink
    /// if all elements at the band bottom also shrink.
    /// </summary>
    public bool CanShrink { get; set; }

    /// <summary>
    /// Gets or sets the running total mode for this element. When set to <see cref="RunningTotalMode.OverGroup"/>
    /// or <see cref="RunningTotalMode.OverAll"/>, the element accumulates a running total during layout
    /// composition. Only valid on TextField elements in the Detail band.
    /// </summary>
    public RunningTotalMode RunningTotal { get; set; } = RunningTotalMode.None;

    /// <summary>
    /// Gets or sets whether consecutive duplicate values are hidden for this element.
    /// When true, only the first occurrence of a value is displayed; subsequent rows with
    /// the same formatted value show blank. Only valid on TextField elements in the Detail band.
    /// </summary>
    public bool HideDuplicates { get; set; }
}

/// <summary>
/// Image scaling mode for Image elements.
/// </summary>
public enum ImageScaleMode
{
    /// <summary>Scale proportionally to fit within the element bounds.</summary>
    ScaleToFit,

    /// <summary>Stretch to fill the element bounds (may distort aspect ratio).</summary>
    Stretch,

    /// <summary>Render at actual pixel size.</summary>
    ActualSize,

    /// <summary>Tile the image to fill the element bounds.</summary>
    Tile
}

/// <summary>
/// Direction for Line elements.
/// </summary>
public enum LineDirection
{
    /// <summary>Horizontal line.</summary>
    Horizontal,

    /// <summary>Vertical line.</summary>
    Vertical
}
