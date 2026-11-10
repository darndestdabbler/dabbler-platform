namespace Dabbler.Reports.Core.Layout;

/// <summary>
/// Represents a filled rectangular region behind band elements on a composed page.
/// Band backgrounds are rendered before elements, providing section-level or
/// alternating-row coloring.
/// </summary>
public class LayoutBandBackground
{
    /// <summary>Gets or sets the X position in inches from the page left edge.</summary>
    public double X { get; set; }

    /// <summary>Gets or sets the Y position in inches from the page top edge.</summary>
    public double Y { get; set; }

    /// <summary>Gets or sets the width in inches (typically the printable width).</summary>
    public double Width { get; set; }

    /// <summary>Gets or sets the height in inches (the effective band height).</summary>
    public double Height { get; set; }

    /// <summary>Gets or sets the fill color as a hex string (e.g., "#F2F2F2").</summary>
    public string Color { get; set; } = string.Empty;
}
