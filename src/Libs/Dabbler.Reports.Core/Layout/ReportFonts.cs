namespace Dabbler.Reports.Core.Layout;

/// <summary>
/// TrueType font families a report can name in a style's or the report's default
/// font family. A registered family is embedded as a subset and draws every character
/// its font has a glyph for; any other family maps to one of the Base 14 PDF fonts,
/// which draw Windows-1252 text only.
/// </summary>
/// <example>
/// <code>
/// var fonts = new ReportFonts()
///     .Register("Noto Sans", "fonts/NotoSans-Regular.ttf", boldPath: "fonts/NotoSans-Bold.ttf");
/// </code>
/// </example>
public sealed class ReportFonts
{
    private readonly Dictionary<string, FontFiles> _families = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers a family name (case-insensitive) against a regular <c>.ttf</c> file and,
    /// optionally, its bold, italic and bold-italic files. A style whose weight and slant
    /// have no file of their own uses the regular file; no bold or italic is synthesized.
    /// Registering a family again replaces its files.
    /// </summary>
    /// <param name="family">The family name a style or the report's default family names.</param>
    /// <param name="regularPath">Path of the regular <c>.ttf</c> file.</param>
    /// <param name="boldPath">Path of the bold <c>.ttf</c> file, if any.</param>
    /// <param name="italicPath">Path of the italic <c>.ttf</c> file, if any.</param>
    /// <param name="boldItalicPath">Path of the bold-italic <c>.ttf</c> file, if any.</param>
    /// <returns>This instance, so registrations can be chained.</returns>
    /// <exception cref="FileNotFoundException">A given file does not exist.</exception>
    public ReportFonts Register(
        string family,
        string regularPath,
        string? boldPath = null,
        string? italicPath = null,
        string? boldItalicPath = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(family);
        ArgumentException.ThrowIfNullOrWhiteSpace(regularPath);

        foreach (var path in new[] { regularPath, boldPath, italicPath, boldItalicPath })
        {
            if (path is not null && !File.Exists(path))
                throw new FileNotFoundException($"Font file for family '{family}' not found.", path);
        }

        _families[family.Trim()] = new FontFiles(regularPath, boldPath, italicPath, boldItalicPath);
        return this;
    }

    /// <summary>
    /// Gets the file a family draws the given weight and slant with: the file registered
    /// for them, else the family's regular file; null when the family is not registered.
    /// </summary>
    /// <param name="family">The family name, matched case-insensitively.</param>
    /// <param name="bold">Whether the text is bold.</param>
    /// <param name="italic">Whether the text is italic.</param>
    public string? GetFontFile(string? family, bool bold, bool italic)
    {
        if (string.IsNullOrWhiteSpace(family) || !_families.TryGetValue(family.Trim(), out var files))
            return null;

        string? styled = (bold, italic) switch
        {
            (true, true) => files.BoldItalic,
            (true, false) => files.Bold,
            (false, true) => files.Italic,
            _ => null,
        };

        return styled ?? files.Regular;
    }

    private sealed record FontFiles(string Regular, string? Bold, string? Italic, string? BoldItalic);
}
