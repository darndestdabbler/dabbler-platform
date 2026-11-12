using Dabbler.Reports.Core.Rdf;
using DabblerPdf.Text;

namespace Dabbler.Reports.Core.Layout;

/// <summary>
/// Resolves the font and size a report element's text is measured and drawn in:
/// the style's own values, then the report's <see cref="ReportMetadata"/> defaults,
/// then the Base 14 mapping. Shared by <see cref="OpenPdfTextMeasurer"/> and the
/// PDF renderer so both see the same font.
/// </summary>
internal sealed class ReportFontResolver
{
    private readonly ReportMetadata _metadata;
    private readonly Dictionary<string, BaseFont> _fontCache = new();

    public ReportFontResolver(ReportMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        _metadata = metadata;
    }

    /// <summary>Gets the Base 14 font for the given style.</summary>
    public BaseFont GetFont(StyleDefinition? style)
    {
        string baseFontName = MapToBaseFontName(
            style?.FontFamily ?? _metadata.DefaultFontFamily,
            style?.IsBold == true,
            style?.IsItalic == true);

        if (!_fontCache.TryGetValue(baseFontName, out var font))
        {
            font = BaseFont.CreateFont(baseFontName, BaseFont.WinAnsiEncoding, false);
            _fontCache[baseFontName] = font;
        }

        return font;
    }

    /// <summary>Gets the font size in points for the given style.</summary>
    public double GetFontSize(StyleDefinition? style) =>
        style?.FontSize ?? _metadata.DefaultFontSize;

    /// <summary>
    /// Maps a font family and weight to one of the 14 standard PDF font names.
    /// </summary>
    internal static string MapToBaseFontName(string? fontFamily, bool bold, bool italic)
    {
        return NormalizeFontFamily(fontFamily) switch
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

        return fontFamily.Trim().ToLowerInvariant() switch
        {
            "times" or "times new roman" or "times-roman" or "serif" => "times",
            "courier" or "courier new" or "monospace" => "courier",
            _ => "helvetica", // Arial, Helvetica, sans-serif, and anything unknown
        };
    }
}
