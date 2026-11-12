using Dabbler.Reports.Core.Layout;
using Dabbler.Reports.Core.Rdf;
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

    private static OpenPdfTextMeasurer Measurer(double defaultFontSize, StyleDefinition style) =>
        new(new ReportDefinition
        {
            Metadata = new ReportMetadata { DefaultFontSize = defaultFontSize },
            Styles = { style }
        });
}
