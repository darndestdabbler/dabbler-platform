using Dabbler.Reports.Core.Rdf;

namespace Dabbler.Reports.Core.Layout;

/// <summary>
/// A positioned element within a composed page, with absolute coordinates
/// and resolved content ready for rendering.
/// </summary>
public class LayoutElement
{
    /// <summary>Gets or sets the X position in inches from the page left edge.</summary>
    public double X { get; set; }

    /// <summary>Gets or sets the Y position in inches from the page top edge.</summary>
    public double Y { get; set; }

    /// <summary>Gets or sets the element width in inches.</summary>
    public double Width { get; set; }

    /// <summary>Gets or sets the element height in inches.</summary>
    public double Height { get; set; }

    /// <summary>Gets or sets the resolved text content (for text elements).</summary>
    public string? Text { get; set; }

    /// <summary>Gets or sets the style name reference.</summary>
    public string? StyleName { get; set; }

    /// <summary>Gets or sets the element type from the original definition.</summary>
    public ElementType ElementType { get; set; }

    /// <summary>Gets or sets the resolved image file path (for Image elements).</summary>
    public string? ImageSourcePath { get; set; }

    /// <summary>Gets or sets the raw image data from a data field (for Image elements with BLOB sources).</summary>
    public byte[]? ImageData { get; set; }

    /// <summary>Gets or sets the image scaling mode (for Image elements).</summary>
    public ImageScaleMode ImageScaleMode { get; set; }

    /// <summary>
    /// Gets the child elements for subreport elements. These are the composed
    /// elements from the subreport, positioned relative to this element's origin.
    /// </summary>
    public List<LayoutElement>? ChildElements { get; set; }

    /// <summary>
    /// Gets or sets the actual rendered height of this element in inches.
    /// For subreport elements, this may exceed <see cref="Height"/> when the
    /// subreport content is taller than the defined element bounds.
    /// </summary>
    public double ActualHeight { get; set; }
}
