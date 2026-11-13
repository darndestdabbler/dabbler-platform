using Dabbler.Reports.Core.Layout;
using Dabbler.Reports.Core.Rdf;
using Dabbler.Reports.Export;
using FluentAssertions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Graphics.Operations.TextShowing;
using Xunit;

namespace Dabbler.Reports.Blazor.Tests;

/// <summary>
/// Drives <c>PdfPageRenderer</c> through the public <see cref="PdfExporter"/> and reads
/// the text it draws back out of the page's content stream.
/// </summary>
public class PdfPageRendererTests
{
    private const string LongText =
        "The quick brown fox jumps over the lazy dog while the band grows to hold every line " +
        "of this field, and the measurer and the renderer agree on where each line breaks.";

    [Fact]
    public async Task RenderWrappedText_KeepsARunOfSpacesInTheTjOperand()
    {
        var report = Report(new StyleDefinition { Name = "s", TextWrap = true });

        var drawn = await DrawAsync(report, "Total:  42", "s", widthInches: 4, heightInches: 0.5);

        drawn.Should().ContainSingle().Which.Should().Be("Total:  42");
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("Times New Roman", 14.0)]
    public async Task RenderWrappedText_DrawsTheLinesTheMeasurerCounts(string? family, double? size)
    {
        var report = Report(new StyleDefinition { Name = "s", FontFamily = family, FontSize = size, TextWrap = true });
        var measurer = new OpenPdfTextMeasurer(report);
        const double width = 2.0;

        double height = measurer.MeasureTextHeight(LongText, "s", width);
        int measuredLines = (int)Math.Round(height / measurer.MeasureTextHeight("x", "s", width));

        var drawn = await DrawAsync(report, LongText, "s", width, height);

        measuredLines.Should().BeGreaterThan(1);
        drawn.Should().HaveCount(measuredLines);
    }

    private static ReportDefinition Report(StyleDefinition style) => new()
    {
        Metadata = new ReportMetadata { DefaultFontFamily = "Courier New", DefaultFontSize = 9 },
        Styles = { style }
    };

    private static async Task<IReadOnlyList<string?>> DrawAsync(
        ReportDefinition report, string text, string styleName, double widthInches, double heightInches)
    {
        var page = new PageLayout { PageNumber = 1, TotalPages = 1 };
        page.Elements.Add(new LayoutElement
        {
            ElementType = ElementType.TextField,
            X = 1, Y = 1, Width = widthInches, Height = heightInches,
            Text = text, StyleName = styleName
        });

        using var stream = new MemoryStream();
        await new PdfExporter(report).ExportAsync([page], stream);

        using var pdf = PdfDocument.Open(stream.ToArray());
        return pdf.GetPage(1).Operations.OfType<ShowText>().Select(op => op.Text).ToList();
    }
}
