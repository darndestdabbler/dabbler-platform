using Dabbler.Reports.Core.Rdf;
using DabblerPdf.Text;

namespace Dabbler.Reports.Core.Layout;

/// <summary>
/// Measures text height using DabblerPdf font metrics for accurate CanGrow/CanShrink support.
/// Resolves fonts and sizes exactly as the PDF renderer does (style, then report metadata,
/// then the Base 14 mapping) and wraps with <see cref="TextLayout"/>, so a measured field
/// holds the lines that are drawn in it.
/// </summary>
public class OpenPdfTextMeasurer : ITextMeasurer
{
    private const double PointsPerInch = 72.0;

    private readonly Dictionary<string, StyleDefinition> _styles;
    private readonly ReportFontResolver _fonts;

    /// <summary>
    /// Creates a new text measurer using the styles from the given report definition.
    /// A style with no font size or family falls back to the <see cref="ReportMetadata"/> defaults.
    /// </summary>
    /// <param name="styles">The named style definitions from the report.</param>
    public OpenPdfTextMeasurer(IEnumerable<StyleDefinition> styles)
        : this(styles, new ReportMetadata(), null)
    {
    }

    /// <summary>
    /// Creates a new text measurer using the styles and metadata defaults of the given report definition.
    /// </summary>
    public OpenPdfTextMeasurer(ReportDefinition report)
        : this(report, null)
    {
    }

    /// <summary>
    /// Creates a new text measurer using the styles and metadata defaults of the given report
    /// definition, measuring a family registered in <paramref name="fonts"/> with its TrueType file.
    /// </summary>
    /// <param name="report">The report definition.</param>
    /// <param name="fonts">The registered TrueType families, or null for the Base 14 fonts only.</param>
    public OpenPdfTextMeasurer(ReportDefinition report, ReportFonts? fonts)
        : this(report.Styles, report.Metadata, fonts)
    {
    }

    private OpenPdfTextMeasurer(IEnumerable<StyleDefinition> styles, ReportMetadata metadata, ReportFonts? fonts)
    {
        _styles = styles.ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);
        _fonts = new ReportFontResolver(metadata, fonts);
    }

    /// <inheritdoc />
    public double MeasureTextHeight(string text, string? styleName, double widthInches)
    {
        if (string.IsNullOrEmpty(text) || widthInches <= 0)
            return 0;

        var style = ResolveStyle(styleName);
        var font = _fonts.GetFont(style);
        double fontSize = _fonts.GetFontSize(style);

        double paddingLeft = GetEffectivePaddingLeft(style);
        double paddingRight = GetEffectivePaddingRight(style);
        double paddingTop = GetEffectivePaddingTop(style);
        double paddingBottom = GetEffectivePaddingBottom(style);

        double availableWidthPoints = (widthInches - paddingLeft - paddingRight) * PointsPerInch;
        if (availableWidthPoints <= 0)
            return 0;

        int lineCount = TextLayout.CountLines(text, font, fontSize, availableWidthPoints);
        double lineHeightPoints = GetLineHeight(font, fontSize);
        double textHeightInches = (lineCount * lineHeightPoints) / PointsPerInch;

        return textHeightInches + paddingTop + paddingBottom;
    }

    private static double GetLineHeight(BaseFont font, double fontSize)
    {
        double ascent = font.GetFontDescriptor(BaseFont.ASCENT, fontSize);
        double descent = font.GetFontDescriptor(BaseFont.DESCENT, fontSize);
        // Descent is negative, so ascent - descent gives the full line height
        return ascent - descent;
    }

    private StyleDefinition? ResolveStyle(string? styleName)
    {
        if (styleName is null)
            return null;

        _styles.TryGetValue(styleName, out var style);
        return style;
    }

    private static double GetEffectivePaddingLeft(StyleDefinition? style)
    {
        if (style is null) return 0;
        return style.PaddingLeft ?? style.Padding ?? 0;
    }

    private static double GetEffectivePaddingRight(StyleDefinition? style)
    {
        if (style is null) return 0;
        return style.PaddingRight ?? style.Padding ?? 0;
    }

    private static double GetEffectivePaddingTop(StyleDefinition? style)
    {
        if (style is null) return 0;
        return style.PaddingTop ?? style.Padding ?? 0;
    }

    private static double GetEffectivePaddingBottom(StyleDefinition? style)
    {
        if (style is null) return 0;
        return style.PaddingBottom ?? style.Padding ?? 0;
    }
}
