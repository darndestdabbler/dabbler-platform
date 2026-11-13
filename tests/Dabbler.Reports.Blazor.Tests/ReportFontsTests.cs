using Dabbler.Reports.Core.Layout;
using FluentAssertions;
using Xunit;

namespace Dabbler.Reports.Blazor.Tests;

public class ReportFontsTests
{
    private static readonly string Regular =
        Path.Combine(AppContext.BaseDirectory, "Fonts", "LiberationSans-Regular.ttf");

    // Any existing file stands in for a styled face: GetFontFile only chooses a path.
    private static readonly string Other =
        Path.Combine(AppContext.BaseDirectory, "Fonts", "LICENSE");

    [Fact]
    public void Register_MatchesTheFamilyCaseInsensitively()
    {
        var fonts = new ReportFonts().Register("Liberation Sans", Regular);

        fonts.GetFontFile("LIBERATION SANS", bold: false, italic: false).Should().Be(Regular);
        fonts.GetFontFile(" liberation sans ", bold: false, italic: false).Should().Be(Regular);
    }

    [Fact]
    public void Register_AgainReplacesTheFamilysFiles()
    {
        var fonts = new ReportFonts()
            .Register("Liberation Sans", Regular, boldPath: Other)
            .Register("Liberation Sans", Regular);

        fonts.GetFontFile("Liberation Sans", bold: true, italic: false).Should().Be(Regular);
    }

    [Fact]
    public void Register_RejectsAMissingFileOrAnEmptyName()
    {
        var fonts = new ReportFonts();
        var missing = Path.Combine(AppContext.BaseDirectory, "Fonts", "missing.ttf");

        fonts.Invoking(f => f.Register("Liberation Sans", missing)).Should().Throw<FileNotFoundException>();
        fonts.Invoking(f => f.Register("Liberation Sans", Regular, italicPath: missing))
            .Should().Throw<FileNotFoundException>();
        fonts.Invoking(f => f.Register(" ", Regular)).Should().Throw<ArgumentException>();
        fonts.Invoking(f => f.Register("Liberation Sans", "")).Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void GetFontFile_ChoosesTheFileForTheWeightAndSlant(bool bold, bool italic)
    {
        var fonts = new ReportFonts().Register(
            "Liberation Sans", Regular,
            boldPath: bold && !italic ? Other : null,
            italicPath: italic && !bold ? Other : null,
            boldItalicPath: bold && italic ? Other : null);

        fonts.GetFontFile("Liberation Sans", bold, italic).Should().Be(Other);
        fonts.GetFontFile("Liberation Sans", bold: false, italic: false).Should().Be(Regular);
    }

    [Fact]
    public void GetFontFile_FallsBackToTheRegularFile()
    {
        var fonts = new ReportFonts().Register("Liberation Sans", Regular, boldPath: Other);

        fonts.GetFontFile("Liberation Sans", bold: true, italic: true).Should().Be(Regular);
        fonts.GetFontFile("Liberation Sans", bold: false, italic: true).Should().Be(Regular);
    }

    [Theory]
    [InlineData("Helvetica")]
    [InlineData(null)]
    [InlineData("")]
    public void GetFontFile_IsNullForAnUnregisteredFamily(string? family)
    {
        var fonts = new ReportFonts().Register("Liberation Sans", Regular);

        fonts.GetFontFile(family, bold: false, italic: false).Should().BeNull();
    }
}
