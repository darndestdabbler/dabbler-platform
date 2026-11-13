namespace Dabbler.Reports.Core.Layout;

/// <summary>
/// Abstraction for measuring rendered text height. Used by <see cref="BandLayoutEngine"/>
/// to compute effective band heights for CanGrow/CanShrink elements.
/// </summary>
public interface ITextMeasurer
{
    /// <summary>
    /// Measures the height of the given text when rendered with the specified style
    /// and constrained to the given width.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="styleName">The style name to use for font metrics (null for default).</param>
    /// <param name="widthInches">The available width in inches for text wrapping.</param>
    /// <returns>The measured text height in inches.</returns>
    double MeasureTextHeight(string text, string? styleName, double widthInches);
}
