using Dabbler.Components;
using Dabbler.Reports.Core.Layout;
using Dabbler.Reports.Core.Rdf;
using FluentAssertions;
using UglyToad.PdfPig;
using Xunit;

namespace Dabbler.Reports.Blazor.Tests;

public class ReportPdfExporterFactoryTests
{
    [Fact]
    public async Task Create_DrawsWithTheGivenFontsAndKeepsTheMetadata()
    {
        const string text = "Съешь же ещё";
        var path = Path.Combine(AppContext.BaseDirectory, "Fonts", "LiberationSans-Regular.ttf");
        var report = new ReportDefinition
        {
            Metadata = new ReportMetadata
            {
                Name = "Unicode",
                Author = "Dabbler",
                DefaultFontFamily = "Liberation Sans"
            }
        };
        var page = new PageLayout { PageNumber = 1, TotalPages = 1 };
        page.Elements.Add(new LayoutElement
        {
            ElementType = ElementType.TextField,
            X = 1, Y = 1, Width = 6, Height = 0.5,
            Text = text
        });

        var exporter = ReportPdfExporterFactory.Create(report, new ReportFonts().Register("Liberation Sans", path));
        using var stream = new MemoryStream();
        await exporter.ExportAsync([page], stream);

        exporter.Title.Should().Be("Unicode");
        exporter.Author.Should().Be("Dabbler");
        using var pdf = PdfDocument.Open(stream.ToArray());
        string.Concat(pdf.GetPage(1).Letters.Select(l => l.Value).Where(v => !string.IsNullOrWhiteSpace(v)))
            .Should().Be(text.Replace(" ", ""));
    }
}
