using System.Globalization;
using Dabbler.Reports.Core.Rdf;
using DabblerPdf.Document;
using DabblerPdf.Text;
using DabblerPdf.Utilities;

namespace Dabbler.Reports.Export;

/// <summary>
/// Resolves <see cref="StyleDefinition"/> properties into DabblerPdf types
/// (BaseFont, BaseColor) for PDF rendering. Caches resolved objects to
/// avoid repeated allocation during page rendering.
/// </summary>
internal sealed class PdfStyleResolver
{
    private readonly Dictionary<string, StyleDefinition> _styles;
    private readonly ReportMetadata _metadata;
    private readonly Dictionary<string, BaseFont> _fontCache = new();
    private readonly Dictionary<string, BaseColor> _colorCache = new();

    public PdfStyleResolver(ReportDefinition report)
    {
        ArgumentNullException.ThrowIfNull(report);
        _metadata = report.Metadata;
        _styles = new Dictionary<string, StyleDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var style in report.Styles)
        {
            _styles[style.Name] = style;
        }
    }

    public StyleDefinition? FindStyle(string? styleName)
    {
        if (styleName is null) return null;
        _styles.TryGetValue(styleName, out var style);
        return style;
    }

    /// <summary>
    /// Gets the BaseFont for the given style, mapping to one of the 14 standard PDF fonts.
    /// </summary>
    public BaseFont GetFont(StyleDefinition? style)
    {
        string baseFontName = OpenPdfTextMeasurerFontMapping(style);

        if (!_fontCache.TryGetValue(baseFontName, out var font))
        {
            font = BaseFont.CreateFont(baseFontName, BaseFont.WinAnsiEncoding, false);
            _fontCache[baseFontName] = font;
        }

        return font;
    }

    /// <summary>
    /// Gets the effective font size for a style, falling back to metadata default.
    /// </summary>
    public double GetFontSize(StyleDefinition? style)
    {
        return style?.FontSize ?? _metadata.DefaultFontSize;
    }

    /// <summary>
    /// Parses a hex color string into a BaseColor, or returns null if invalid.
    /// </summary>
    public BaseColor? GetColor(string? hexColor)
    {
        if (hexColor is null) return null;

        if (_colorCache.TryGetValue(hexColor, out var cached))
            return cached;

        try
        {
            var color = ParseHexColor(hexColor);
            _colorCache[hexColor] = color;
            return color;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parses a CSS-like border specification (e.g., "1px solid #000000").
    /// Returns (width, dashPattern, color) or null if invalid.
    /// </summary>
    public (double Width, double[]? DashPattern, BaseColor Color)? ParseBorder(string? borderSpec)
    {
        if (string.IsNullOrWhiteSpace(borderSpec)) return null;

        try
        {
            var parts = borderSpec.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return null;

            var widthStr = parts[0];
            if (widthStr.EndsWith("px", StringComparison.OrdinalIgnoreCase))
                widthStr = widthStr[..^2];
            if (!double.TryParse(widthStr, CultureInfo.InvariantCulture, out var width))
                return null;

            var colorStr = parts.Length >= 3 ? parts[2] : parts[1];
            var color = GetColor(colorStr);
            if (color is null) return null;

            double[]? dashPattern = null;
            if (parts.Length >= 3)
            {
                dashPattern = parts[1].ToLowerInvariant() switch
                {
                    "dashed" => [6, 3],
                    "dotted" => [1, 2],
                    _ => null // solid
                };
            }

            return (width, dashPattern, color);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the effective padding for a style in PDF points.
    /// </summary>
    public static (double Left, double Top, double Right, double Bottom) GetPaddingPoints(StyleDefinition? style)
    {
        if (style is null) return (0, 0, 0, 0);

        var basePad = style.Padding ?? 0;
        return (
            CoordinateTranslator.InchesToPoints(style.PaddingLeft ?? basePad),
            CoordinateTranslator.InchesToPoints(style.PaddingTop ?? basePad),
            CoordinateTranslator.InchesToPoints(style.PaddingRight ?? basePad),
            CoordinateTranslator.InchesToPoints(style.PaddingBottom ?? basePad)
        );
    }

    /// <summary>
    /// Maps a style's font properties to one of the 14 standard PDF base font names.
    /// Reuses the same mapping logic as OpenPdfTextMeasurer.
    /// </summary>
    private string OpenPdfTextMeasurerFontMapping(StyleDefinition? style)
    {
        return Dabbler.Reports.Core.Layout.OpenPdfTextMeasurer.MapToBaseFontName(
            style is not null
                ? new StyleDefinition
                {
                    FontFamily = style.FontFamily ?? _metadata.DefaultFontFamily,
                    IsBold = style.IsBold,
                    IsItalic = style.IsItalic
                }
                : new StyleDefinition
                {
                    FontFamily = _metadata.DefaultFontFamily
                });
    }

    internal static BaseColor ParseHexColor(string hexColor)
    {
        var hex = hexColor.TrimStart('#');

        return hex.Length switch
        {
            6 => new BaseColor(
                byte.Parse(hex[0..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                byte.Parse(hex[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                byte.Parse(hex[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture)),
            8 => new BaseColor(
                byte.Parse(hex[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                byte.Parse(hex[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                byte.Parse(hex[6..8], NumberStyles.HexNumber, CultureInfo.InvariantCulture)),
            _ => throw new FormatException($"Invalid hex color: {hexColor}")
        };
    }
}
