namespace Dabbler.Reports.Core.Rdf;

/// <summary>
/// Named style definition for report elements including font, color,
/// alignment, border, and padding properties.
/// </summary>
public class StyleDefinition
{
    /// <summary>Gets or sets the style name for referencing from elements.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the font family name.</summary>
    public string? FontFamily { get; set; }

    /// <summary>Gets or sets the font size in points.</summary>
    public double? FontSize { get; set; }

    /// <summary>Gets or sets whether the font is bold.</summary>
    public bool? IsBold { get; set; }

    /// <summary>Gets or sets whether the font is italic.</summary>
    public bool? IsItalic { get; set; }

    /// <summary>Gets or sets whether the font has underline decoration.</summary>
    public bool? IsUnderline { get; set; }

    /// <summary>Gets or sets the foreground color (hex, e.g., "#000000").</summary>
    public string? ForegroundColor { get; set; }

    /// <summary>Gets or sets the background color (hex, e.g., "#FFFFFF").</summary>
    public string? BackgroundColor { get; set; }

    /// <summary>Gets or sets the horizontal text alignment.</summary>
    public HorizontalTextAlignment? HorizontalAlignment { get; set; }

    /// <summary>Gets or sets the vertical text alignment.</summary>
    public VerticalTextAlignment? VerticalAlignment { get; set; }

    /// <summary>Gets or sets whether text wraps within element bounds.</summary>
    public bool? TextWrap { get; set; }

    /// <summary>Gets or sets the shorthand border for all four sides (e.g., "1px solid #000000"). Individual sides override.</summary>
    public string? Border { get; set; }

    /// <summary>Gets or sets the top border specification.</summary>
    public string? BorderTop { get; set; }

    /// <summary>Gets or sets the bottom border specification.</summary>
    public string? BorderBottom { get; set; }

    /// <summary>Gets or sets the left border specification.</summary>
    public string? BorderLeft { get; set; }

    /// <summary>Gets or sets the right border specification.</summary>
    public string? BorderRight { get; set; }

    /// <summary>Gets or sets the shorthand padding in inches for all four sides. Individual sides override.</summary>
    public double? Padding { get; set; }

    /// <summary>Gets or sets the top padding in inches.</summary>
    public double? PaddingTop { get; set; }

    /// <summary>Gets or sets the bottom padding in inches.</summary>
    public double? PaddingBottom { get; set; }

    /// <summary>Gets or sets the left padding in inches.</summary>
    public double? PaddingLeft { get; set; }

    /// <summary>Gets or sets the right padding in inches.</summary>
    public double? PaddingRight { get; set; }
}

/// <summary>
/// Horizontal text alignment within an element.
/// </summary>
public enum HorizontalTextAlignment
{
    /// <summary>Align text to the left.</summary>
    Left,

    /// <summary>Center text horizontally.</summary>
    Center,

    /// <summary>Align text to the right.</summary>
    Right
}

/// <summary>
/// Vertical text alignment within an element.
/// </summary>
public enum VerticalTextAlignment
{
    /// <summary>Align text to the top.</summary>
    Top,

    /// <summary>Align text to the vertical middle.</summary>
    Middle,

    /// <summary>Align text to the bottom.</summary>
    Bottom
}
