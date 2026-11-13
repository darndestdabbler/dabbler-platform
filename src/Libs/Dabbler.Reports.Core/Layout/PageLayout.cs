namespace Dabbler.Reports.Core.Layout;

/// <summary>
/// Represents a single composed page of a report, containing
/// a list of positioned elements ready for rendering.
/// </summary>
public class PageLayout
{
    /// <summary>Gets or sets the page number (1-based).</summary>
    public int PageNumber { get; set; }

    /// <summary>Gets or sets the total page count (set after all pages are composed).</summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Gets the band background regions on this page. These are drawn before
    /// elements to provide section-level or alternating-row coloring.
    /// </summary>
    public List<LayoutBandBackground> BandBackgrounds { get; } = [];

    /// <summary>Gets the positioned elements on this page.</summary>
    public List<LayoutElement> Elements { get; } = [];
}
