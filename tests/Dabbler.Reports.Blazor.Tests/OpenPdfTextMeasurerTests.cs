using Dabbler.Reports.Core.Layout;
using Dabbler.Reports.Core.Rdf;
using DabblerPdf.Text;
using FluentAssertions;
using Xunit;

namespace Dabbler.Reports.Blazor.Tests;

public class OpenPdfTextMeasurerTests
{
    [Fact]
    public void MeasureTextHeight_MeasuresAStyleWithNoSizeAtTheReportsDefaultSize()
    {
        var unsized = Measurer(defaultFontSize: 20, new StyleDefinition { Name = "s" });
        var sizedAtDefault = Measurer(defaultFontSize: 20, new StyleDefinition { Name = "s", FontSize = 20 });
        var sizedAtTwelve = Measurer(defaultFontSize: 20, new StyleDefinition { Name = "s", FontSize = 12 });

        double height = unsized.MeasureTextHeight("x", "s", 5);

        height.Should().Be(sizedAtDefault.MeasureTextHeight("x", "s", 5));
        height.Should().BeGreaterThan(sizedAtTwelve.MeasureTextHeight("x", "s", 5));
    }

    [Fact]
    public void MeasureTextHeight_BreaksAWordWiderThanTheField()
    {
        var measurer = Measurer(defaultFontSize: 10, new StyleDefinition { Name = "s" });

        double oneLine = measurer.MeasureTextHeight("x", "s", 0.5);

        measurer.MeasureTextHeight(new string('W', 40), "s", 0.5).Should().BeGreaterThan(oneLine);
    }

    [Fact]
    public void MeasureTextHeight_MeasuresARegisteredFamilyWithItsTrueTypeFont()
    {
        const string text = "Zażółć gęślą jaźń. Съешь же ещё этих мягких булок. Ξεσκεπάζω την ψυχοφθόρα βδελυγμία.";
        var path = Path.Combine(AppContext.BaseDirectory, "Fonts", "LiberationSans-Regular.ttf");
        var style = new StyleDefinition { Name = "s", FontFamily = "Liberation Sans", FontSize = 10, Padding = 0.05 };
        var measurer = new OpenPdfTextMeasurer(
            new ReportDefinition { Styles = { style } },
            new ReportFonts().Register("Liberation Sans", path));
        const double width = 2.0;

        double height = measurer.MeasureTextHeight(text, "s", width);

        var font = BaseFont.CreateFont(path, BaseFont.IdentityH, true);
        double leading = font.GetFontDescriptor(BaseFont.ASCENT, 10) - font.GetFontDescriptor(BaseFont.DESCENT, 10);
        double widthPoints = (width - 0.1) * 72;
        TextLayout.CountLines(text, font, 10, widthPoints).Should().BeGreaterThan(1);
        height.Should().BeApproximately(
            TextLayout.MeasureHeight(text, font, 10, widthPoints, leading) / 72 + 0.1, 1e-9);
    }

    private static OpenPdfTextMeasurer Measurer(double defaultFontSize, StyleDefinition style) =>
        new(new ReportDefinition
        {
            Metadata = new ReportMetadata { DefaultFontSize = defaultFontSize },
            Styles = { style }
        });
}
