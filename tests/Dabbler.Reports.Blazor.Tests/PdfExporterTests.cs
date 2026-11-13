using Dabbler.Reports.Core.Layout;
using Dabbler.Reports.Core.Rdf;
using Dabbler.Reports.Export;
using FluentAssertions;
using UglyToad.PdfPig;
using Xunit;

namespace Dabbler.Reports.Blazor.Tests;

public class PdfExporterTests
{
    private static readonly string LiberationSans =
        Path.Combine(AppContext.BaseDirectory, "Fonts", "LiberationSans-Regular.ttf");

    [Theory]
    [InlineData("Zażółć gęślą jaźń")]
    [InlineData("Съешь же ещё этих булок")]
    [InlineData("Ξεσκεπάζω την ψυχοφθόρα")]
    public async Task ExportAsync_DrawsUnicodeTextInARegisteredFamily(string text)
    {
        var report = new ReportDefinition
        {
            Styles = { new StyleDefinition { Name = "s", FontFamily = "Liberation Sans", FontSize = 12 } }
        };
        var page = new PageLayout { PageNumber = 1, TotalPages = 1 };
        page.Elements.Add(new LayoutElement
        {
            ElementType = ElementType.TextField,
            X = 1, Y = 1, Width = 6, Height = 0.5,
            Text = text, StyleName = "s"
        });

        using var stream = new MemoryStream();
        await new PdfExporter(report, new ReportFonts().Register("Liberation Sans", LiberationSans))
            .ExportAsync([page], stream);

        using var pdf = PdfDocument.Open(stream.ToArray());
        var extracted = string.Concat(pdf.GetPage(1).Letters.Select(l => l.Value));
        WithoutSpaces(extracted).Should().Be(WithoutSpaces(text));
    }

    [Fact]
    public async Task ExportAsync_DrawsAnUnregisteredFamilyInTheBase14()
    {
        var report = new ReportDefinition
        {
            Styles = { new StyleDefinition { Name = "s", FontFamily = "Arial" } }
        };
        var page = new PageLayout { PageNumber = 1, TotalPages = 1 };
        page.Elements.Add(new LayoutElement
        {
            ElementType = ElementType.TextField,
            X = 1, Y = 1, Width = 6, Height = 0.5,
            Text = "Total", StyleName = "s"
        });

        using var stream = new MemoryStream();
        await new PdfExporter(report, new ReportFonts().Register("Liberation Sans", LiberationSans))
            .ExportAsync([page], stream);

        using var pdf = PdfDocument.Open(stream.ToArray());
        pdf.GetPage(1).Letters.Select(l => l.FontName).Distinct()
            .Should().ContainSingle().Which.Should().Contain("Helvetica");
    }

    private static string WithoutSpaces(string s) => string.Concat(s.Where(c => !char.IsWhiteSpace(c)));
}
