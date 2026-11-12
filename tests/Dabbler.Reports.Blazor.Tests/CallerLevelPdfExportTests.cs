using System.Data;
using System.Text;
using Dabbler.Components;
using Dabbler.Reports.Core.Layout;
using Dabbler.Reports.Core.Rdf;
using FluentAssertions;
using UglyToad.PdfPig;
using Xunit;

namespace Dabbler.Reports.Blazor.Tests;

/// <summary>
/// Caller-level coverage for the production PDF export path. The earlier
/// <c>PdfExportTitleRegressionTests</c> exercised the exporter unit with a
/// hard-coded <c>Title</c>/<c>Author</c>, which did not prove that the
/// production caller in <c>DabblerReportPage.ExportToPdfAsync</c> actually
/// supplies values (pdf-info-metadata UAT Finding 1). These tests load the
/// real shipped RDF, drive the factory the production caller now uses, and
/// assert against the literal values the human will see in Adobe Reader's
/// Document Properties dialog.
/// </summary>
public class CallerLevelPdfExportTests
{
    private const string ExpectedTitle = "Orders by Retail Business";
    private const string ExpectedAuthor = "AcmeCoffeeDistributors";
    private const string RdfRelativePath =
        "src/Apps/AcmeCoffeeOnline.Web/Reports/orders-by-customer.rdf.json";
    private const int MultiPageRowCount = 200;

    [Fact]
    public async Task ProductionCaller_PopulatesInfoDictionaryFromRealRdf()
    {
        var report = LoadRealRdf();
        var pages = ComposePages(report, rowCount: 4);
        pages.Should().NotBeEmpty();

        using var stream = new MemoryStream();
        var exporter = ReportPdfExporterFactory.Create(report);
        await exporter.ExportAsync(pages, stream);

        var bytes = stream.ToArray();
        bytes.Should().NotBeEmpty();

        var infoDict = ExtractInfoDictionary(bytes);
        infoDict.Should().Contain($"/Title ({ExpectedTitle})",
            "the production caller must forward ReportMetadata.Name into the PDF /Info dictionary so " +
            "Adobe Reader's Document Properties dialog shows the title (pdf-info-metadata UAT Finding 1)");
        infoDict.Should().Contain($"/Author ({ExpectedAuthor})",
            "the production caller must forward ReportMetadata.Author into the PDF /Info dictionary " +
            "(pdf-info-metadata UAT Finding 1)");
        infoDict.Should().Contain("/CreationDate (D:",
            "DabblerPdf 1.1.0 auto-stages CreationDate at Close(); this test confirms the upgrade is consumed");
    }

    /// <summary>
    /// Finding 2 investigation: the human reported the title heading is
    /// missing from the rendered first page. This test loads the real RDF
    /// (which declares <c>defaultFontFamily: "Segoe UI"</c> and
    /// <c>styleName: "reportTitle"</c> on the title element — neither of
    /// which match DabblerPdf's 14 native fonts), composes a multi-page
    /// document, then uses PdfPig to assert the title is visibly rendered
    /// on physical page 1 and that physical page count still matches the
    /// composer's logical page count.
    /// </summary>
    [Fact]
    public async Task ProductionCaller_RendersTitleHeadingOnFirstPage()
    {
        var report = LoadRealRdf();
        var pages = ComposePages(report, rowCount: MultiPageRowCount);
        pages.Should().HaveCountGreaterThan(1,
            "the regression needs a multi-page document so the first NewPage() boundary is exercised");

        using var stream = new MemoryStream();
        var exporter = ReportPdfExporterFactory.Create(report);
        await exporter.ExportAsync(pages, stream);

        var bytes = stream.ToArray();
        bytes.Should().NotBeEmpty();

        using var pdf = PdfDocument.Open(bytes);
        pdf.NumberOfPages.Should().Be(pages.Count,
            "the exported PDF must produce one physical page per composed logical page");

        var firstPageText = pdf.GetPage(1).Text;
        firstPageText.Should().Contain(ExpectedTitle,
            "Finding 2 was reopened because byte-level presence was not enough; the title must be visibly rendered on physical page 1");
    }

    private static ReportDefinition LoadRealRdf()
    {
        var rdfPath = ResolveRdfPath();
        var json = File.ReadAllText(rdfPath);
        var result = new RdfParser().TryParse(json);
        result.IsValid.Should().BeTrue(
            $"orders-by-customer.rdf.json must parse cleanly (errors: {FormatErrors(result.Errors)})");
        return result.Definition!;
    }

    private static string FormatErrors(IReadOnlyList<RdfValidationError>? errors)
    {
        if (errors is null || errors.Count == 0) return "none";
        return string.Join("; ", errors.Select(e => e.Message));
    }

    private static string ResolveRdfPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Dabbler.Platform.sln")))
        {
            dir = dir.Parent;
        }

        if (dir is null)
            throw new InvalidOperationException(
                "Could not locate repo root (Dabbler.Platform.sln) walking up from " +
                AppContext.BaseDirectory);

        var rdfPath = Path.Combine(dir.FullName, RdfRelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(rdfPath))
            throw new FileNotFoundException($"Expected shipped RDF at {rdfPath}");

        return rdfPath;
    }

    private static IReadOnlyList<PageLayout> ComposePages(ReportDefinition report, int rowCount)
    {
        var data = BuildData(rowCount);
        var engine = new BandLayoutEngine();
        return engine.ComposePages(report, data);
    }

    private static DataTable BuildData(int rowCount)
    {
        var table = new DataTable();
        table.Columns.Add("CustomerName", typeof(string));
        table.Columns.Add("OrderId", typeof(int));
        table.Columns.Add("OrderDate", typeof(DateTime));
        table.Columns.Add("ShipDate", typeof(DateTime));
        table.Columns.Add("Status", typeof(string));
        table.Columns.Add("ItemCount", typeof(int));
        table.Columns.Add("Total", typeof(decimal));

        var baseDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        for (int i = 0; i < rowCount; i++)
        {
            table.Rows.Add(
                "Downtown Cafe",
                1000 + i,
                baseDate.AddDays(i),
                baseDate.AddDays(i + 2),
                "Shipped",
                3 + i,
                (decimal)(42.50 + i * 5));
        }
        return table;
    }

    private static string ExtractInfoDictionary(byte[] pdfBytes)
    {
        var text = Encoding.Latin1.GetString(pdfBytes);

        int trailerIdx = text.LastIndexOf("trailer", StringComparison.Ordinal);
        if (trailerIdx < 0)
            throw new InvalidDataException("PDF has no trailer section.");

        int infoIdx = text.IndexOf("/Info", trailerIdx, StringComparison.Ordinal);
        if (infoIdx < 0)
            throw new InvalidDataException("PDF trailer does not reference an /Info dictionary.");

        var match = System.Text.RegularExpressions.Regex.Match(
            text.AsSpan(infoIdx, Math.Min(40, text.Length - infoIdx)).ToString(),
            @"/Info\s+(\d+)\s+(\d+)\s+R");
        if (!match.Success)
            throw new InvalidDataException("Could not parse /Info indirect reference.");

        string objectNumber = match.Groups[1].Value;
        string generation = match.Groups[2].Value;

        string objectHeader = $"{objectNumber} {generation} obj";
        int objStart = text.IndexOf(objectHeader, StringComparison.Ordinal);
        if (objStart < 0)
            throw new InvalidDataException($"Could not locate {objectHeader} in PDF.");

        int dictStart = text.IndexOf("<<", objStart, StringComparison.Ordinal);
        int dictEnd = text.IndexOf(">>", dictStart, StringComparison.Ordinal);
        if (dictStart < 0 || dictEnd < 0)
            throw new InvalidDataException($"Could not locate /Info dictionary body for object {objectNumber}.");

        return text.Substring(dictStart + 2, dictEnd - (dictStart + 2));
    }
}
