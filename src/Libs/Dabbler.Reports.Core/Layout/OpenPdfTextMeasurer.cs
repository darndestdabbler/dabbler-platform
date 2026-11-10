using Dabbler.Reports.Core.Rdf;
using DabblerPdf.Text;

namespace Dabbler.Reports.Core.Layout;

/// <summary>
/// Measures text height using DabblerPdf font metrics for accurate CanGrow/CanShrink support.
/// Maps report <see cref="StyleDefinition"/> font properties to DabblerPdf <see cref="BaseFont"/>
/// instances for character-level width measurement and line-height computation.
/// </summary>
public class OpenPdfTextMeasurer : ITextMeasurer
{
    private const double DefaultFontSize = 12.0;
    private const double PointsPerInch = 72.0;

    private readonly Dictionary<string, StyleDefinition> _styles;
    private readonly Dictionary<string, BaseFont> _fontCache = new();

    /// <summary>
    /// Creates a new text measurer using the styles from the given report definition.
    /// </summary>
    /// <param name="styles">The named style definitions from the report.</param>
    public OpenPdfTextMeasurer(IEnumerable<StyleDefinition> styles)
    {
        _styles = styles.ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Creates a new text measurer using the styles from the given report definition.
    /// </summary>
    public OpenPdfTextMeasurer(ReportDefinition report)
        : this(report.Styles)
    {
    }

    /// <inheritdoc />
    public double MeasureTextHeight(string text, string? styleName, double widthInches)
    {
        if (string.IsNullOrEmpty(text) || widthInches <= 0)
            return 0;

        var style = ResolveStyle(styleName);
        var font = GetOrCreateFont(style);
        double fontSize = style?.FontSize ?? DefaultFontSize;

        double paddingLeft = GetEffectivePaddingLeft(style);
        double paddingRight = GetEffectivePaddingRight(style);
        double paddingTop = GetEffectivePaddingTop(style);
        double paddingBottom = GetEffectivePaddingBottom(style);

        double availableWidthPoints = (widthInches - paddingLeft - paddingRight) * PointsPerInch;
        if (availableWidthPoints <= 0)
            return 0;

        int lineCount = CountWrappedLines(text, font, fontSize, availableWidthPoints);
        double lineHeightPoints = GetLineHeight(font, fontSize);
        double textHeightInches = (lineCount * lineHeightPoints) / PointsPerInch;

        return textHeightInches + paddingTop + paddingBottom;
    }

    private int CountWrappedLines(string text, BaseFont font, double fontSize, double availableWidthPoints)
    {
        // Split on hard line breaks first
        var paragraphs = text.Split('\n');
        int totalLines = 0;

        foreach (var paragraph in paragraphs)
        {
            if (paragraph.Length == 0)
            {
                totalLines++;
                continue;
            }

            totalLines += WrapParagraph(paragraph, font, fontSize, availableWidthPoints);
        }

        return totalLines;
    }

    private int WrapParagraph(string paragraph, BaseFont font, double fontSize, double availableWidthPoints)
    {
        // Word-wrap: split on spaces, accumulate widths
        var words = paragraph.Split(' ');
        int lines = 1;
        double currentLineWidth = 0;
        double spaceWidth = font.GetWidthPoint(" ", fontSize);

        for (int i = 0; i < words.Length; i++)
        {
            string word = words[i];
            if (word.Length == 0)
                continue;

            double wordWidth = font.GetWidthPoint(word, fontSize);

            if (currentLineWidth == 0)
            {
                // First word on the line — always place it even if it overflows
                currentLineWidth = wordWidth;
            }
            else
            {
                double candidateWidth = currentLineWidth + spaceWidth + wordWidth;
                if (candidateWidth > availableWidthPoints)
                {
                    // Wrap to next line
                    lines++;
                    currentLineWidth = wordWidth;
                }
                else
                {
                    currentLineWidth = candidateWidth;
                }
            }
        }

        return lines;
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

    private BaseFont GetOrCreateFont(StyleDefinition? style)
    {
        string baseFontName = MapToBaseFontName(style);

        if (!_fontCache.TryGetValue(baseFontName, out var font))
        {
            font = BaseFont.CreateFont(baseFontName, BaseFont.WinAnsiEncoding, false);
            _fontCache[baseFontName] = font;
        }

        return font;
    }

    /// <summary>
    /// Maps a style's font properties to one of the 14 standard PDF font names.
    /// </summary>
    internal static string MapToBaseFontName(StyleDefinition? style)
    {
        if (style is null)
            return BaseFont.Helvetica;

        string family = NormalizeFontFamily(style.FontFamily);
        bool bold = style.IsBold == true;
        bool italic = style.IsItalic == true;

        return family switch
        {
            "times" => (bold, italic) switch
            {
                (true, true) => BaseFont.TimesBoldItalic,
                (true, false) => BaseFont.TimesBold,
                (false, true) => BaseFont.TimesItalic,
                _ => BaseFont.TimesRoman,
            },
            "courier" => (bold, italic) switch
            {
                (true, true) => BaseFont.CourierBoldOblique,
                (true, false) => BaseFont.CourierBold,
                (false, true) => BaseFont.CourierOblique,
                _ => BaseFont.Courier,
            },
            _ => (bold, italic) switch // Helvetica family (default)
            {
                (true, true) => BaseFont.HelveticaBoldOblique,
                (true, false) => BaseFont.HelveticaBold,
                (false, true) => BaseFont.HelveticaOblique,
                _ => BaseFont.Helvetica,
            },
        };
    }

    private static string NormalizeFontFamily(string? fontFamily)
    {
        if (string.IsNullOrWhiteSpace(fontFamily))
            return "helvetica";

        string lower = fontFamily.Trim().ToLowerInvariant();

        // Map common font family names to PDF standard families
        return lower switch
        {
            "times" or "times new roman" or "times-roman" or "serif" => "times",
            "courier" or "courier new" or "monospace" => "courier",
            _ => "helvetica", // Arial, Helvetica, sans-serif, and anything unknown
        };
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
