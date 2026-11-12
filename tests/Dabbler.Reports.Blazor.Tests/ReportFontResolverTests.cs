using Dabbler.Reports.Core.Layout;
using Dabbler.Reports.Core.Rdf;
using DabblerPdf.Text;
using FluentAssertions;
using Xunit;

namespace Dabbler.Reports.Blazor.Tests;

public class ReportFontResolverTests
{
    private static readonly ReportMetadata Metadata = new()
    {
        DefaultFontFamily = "Courier New",
        DefaultFontSize = 9
    };

    [Fact]
    public void GetFont_UsesTheStylesFamilyAndWeight()
    {
        var resolver = new ReportFontResolver(Metadata);

        var font = resolver.GetFont(new StyleDefinition { Name = "s", FontFamily = "Times New Roman", IsBold = true });

        font.PostscriptName.Should().Be(BaseFont.TimesBold);
    }

    [Fact]
    public void GetFont_FallsBackToTheReportDefaultFamily()
    {
        var resolver = new ReportFontResolver(Metadata);

        resolver.GetFont(new StyleDefinition { Name = "s", IsItalic = true }).PostscriptName
            .Should().Be(BaseFont.CourierOblique);
        resolver.GetFont(null).PostscriptName.Should().Be(BaseFont.Courier);
    }

    [Fact]
    public void GetFont_MapsAFamilyOutsideTheBase14ToHelvetica()
    {
        var resolver = new ReportFontResolver(new ReportMetadata { DefaultFontFamily = "Segoe UI" });

        resolver.GetFont(null).PostscriptName.Should().Be(BaseFont.Helvetica);
    }

    [Fact]
    public void GetFontSize_UsesTheStylesSizeThenTheReportDefault()
    {
        var resolver = new ReportFontResolver(Metadata);

        resolver.GetFontSize(new StyleDefinition { Name = "s", FontSize = 14 }).Should().Be(14);
        resolver.GetFontSize(new StyleDefinition { Name = "s" }).Should().Be(9);
        resolver.GetFontSize(null).Should().Be(9);
    }

    [Theory]
    [InlineData("times", false, false, BaseFont.TimesRoman)]
    [InlineData("Courier New", true, true, BaseFont.CourierBoldOblique)]
    [InlineData("Arial", true, false, BaseFont.HelveticaBold)]
    [InlineData(null, false, false, BaseFont.Helvetica)]
    public void MapToBaseFontName_MapsToTheBase14(string? family, bool bold, bool italic, string expected)
    {
        ReportFontResolver.MapToBaseFontName(family, bold, italic).Should().Be(expected);
    }
}
