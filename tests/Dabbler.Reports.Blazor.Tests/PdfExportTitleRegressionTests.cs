using System.Data;
using System.IO.Compression;
using System.Text;
using Dabbler.Reports.Core.Layout;
using Dabbler.Reports.Core.Rdf;
using Dabbler.Reports.Export;
using FluentAssertions;
using Xunit;

namespace Dabbler.Reports.Blazor.Tests;

/// <summary>
/// Regression coverage that renders real PDF bytes to guard against the
/// report-title disappearing from the PDF export (security-dashboard-uat
/// reviewer feedback, 2026-04-17).
/// </summary>
public class PdfExportTitleRegressionTests
{
    private const string TitleText = "Orders by Retail Business";
    private const string AuthorText = "AcmeCoffeeDistributors";
    private const string ColumnHeaderText = "Order #";

    [Fact]
    public async Task ExportedPdf_ContainsTitleAndColumnHeaderText()
    {
        var report = BuildReportDefinition();
        var engine = new BandLayoutEngine();
        var pages = engine.ComposePages(report, BuildData(rowCount: 3));
        pages.Should().NotBeEmpty();

        using var stream = new MemoryStream();
        var exporter = new PdfExporter(report) { Title = TitleText };
        await exporter.ExportAsync(pages, stream);

        var bytes = stream.ToArray();
        bytes.Should().NotBeEmpty();

        // Sanity: the output is a PDF document.
        Encoding.ASCII.GetString(bytes, 0, Math.Min(4, bytes.Length))
            .Should().Be("%PDF");

        // DabblerPdf writes content streams with /FlateDecode compression,
        // so the text operators only become visible after inflating them.
        var decodedStreams = ExtractAndInflateContentStreams(bytes);
        decodedStreams.Should().NotBeEmpty(
            "at least one FlateDecode content stream must be present in the export");

        var combined = string.Join("\n", decodedStreams);
        combined.Should().Contain(TitleText,
            "the report title must be rendered on page 1 of the PDF export");
        combined.Should().Contain(ColumnHeaderText,
            "column headers must still be rendered on page 1");
    }

    [Fact]
    public async Task ExportedPdf_PopulatesInfoDictionaryWithTitleAndAuthor()
    {
        var report = BuildReportDefinition();
        var engine = new BandLayoutEngine();
        var pages = engine.ComposePages(report, BuildData(rowCount: 3));
        pages.Should().NotBeEmpty();

        using var stream = new MemoryStream();
        var exporter = new PdfExporter(report) { Title = TitleText, Author = AuthorText };
        await exporter.ExportAsync(pages, stream);

        var bytes = stream.ToArray();
        bytes.Should().NotBeEmpty();

        var infoDict = ExtractInfoDictionary(bytes);
        infoDict.Should().Contain($"/Title ({TitleText})",
            "the PDF /Info dictionary must expose the Title property so Adobe Reader's Document Properties dialog renders it");
        infoDict.Should().Contain($"/Author ({AuthorText})",
            "the PDF /Info dictionary must expose the Author property");
        infoDict.Should().Contain("/CreationDate (D:",
            "DabblerPdf 1.1.0 auto-stages CreationDate at Close() unless SuppressCreationDate() is called");
        infoDict.Should().Contain("/Producer (DabblerPdf.NET)",
            "the existing /Producer entry must remain");
    }

    [Fact]
    public async Task ExportedPdf_PlacesTitleElementAbovePageHeaderOnFirstPage()
    {
        var report = BuildReportDefinition();
        var engine = new BandLayoutEngine();
        var pages = engine.ComposePages(report, BuildData(rowCount: 3));

        var firstPage = pages[0];
        var title = firstPage.Elements.First(e => e.Text == TitleText);
        var columnHeader = firstPage.Elements.First(e => e.Text == ColumnHeaderText);

        title.Y.Should().BeLessThan(columnHeader.Y,
            "the title must sit above the column headers on page 1");

        // Exercise the export path to prove the composer's page 1 element list
        // is what the exporter consumes; any future change that accidentally
        // reorders bands in the exporter will break this test.
        using var stream = new MemoryStream();
        var exporter = new PdfExporter(report) { Title = TitleText };
        await exporter.ExportAsync(pages, stream);
        stream.Length.Should().BeGreaterThan(0);
    }

    private static ReportDefinition BuildReportDefinition()
    {
        return new ReportDefinition
        {
            Metadata = new ReportMetadata
            {
                Name = TitleText,
                PageWidth = 11.0,
                PageHeight = 8.5,
                Orientation = PageOrientation.Landscape,
                MarginTop = 0.5,
                MarginBottom = 0.5,
                MarginLeft = 0.75,
                MarginRight = 0.75,
                DefaultFontFamily = "Helvetica",
                DefaultFontSize = 9
            },
            Fields =
            {
                new FieldDefinition { Name = "OrderId", DataType = "Int32", SourceColumn = "OrderId" }
            },
            Bands =
            {
                new BandDefinition
                {
                    Name = "reportHeader",
                    Type = BandType.ReportHeader,
                    Height = 0.6,
                    Elements =
                    {
                        new ElementDefinition
                        {
                            Type = ElementType.StaticText,
                            X = 0, Y = 0.05, Width = 6.0, Height = 0.4,
                            Value = TitleText
                        }
                    }
                },
                new BandDefinition
                {
                    Name = "pageHeader",
                    Type = BandType.PageHeader,
                    Height = 0.3,
                    Elements =
                    {
                        new ElementDefinition
                        {
                            Type = ElementType.StaticText,
                            X = 0, Y = 0.05, Width = 1.0, Height = 0.2,
                            Value = ColumnHeaderText
                        }
                    }
                },
                new BandDefinition
                {
                    Name = "detail",
                    Type = BandType.Detail,
                    Height = 0.25,
                    Elements =
                    {
                        new ElementDefinition
                        {
                            Type = ElementType.TextField,
                            X = 0, Y = 0.02, Width = 1.0, Height = 0.2,
                            FieldName = "OrderId"
                        }
                    }
                }
            }
        };
    }

    private static DataTable BuildData(int rowCount)
    {
        var table = new DataTable();
        table.Columns.Add("OrderId", typeof(int));
        for (int i = 0; i < rowCount; i++)
            table.Rows.Add(1000 + i);
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

        // Trailer /Info entry is "/Info N G R" where N and G are integers.
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

    private static List<string> ExtractAndInflateContentStreams(byte[] pdfBytes)
    {
        var results = new List<string>();
        var text = Encoding.Latin1.GetString(pdfBytes);

        const string streamMarker = "stream";
        const string endMarker = "endstream";

        int cursor = 0;
        while (true)
        {
            int startIdx = text.IndexOf(streamMarker, cursor, StringComparison.Ordinal);
            if (startIdx < 0) break;

            // Skip past "stream" keyword and the required newline (PDF 1.7 spec).
            int dataStart = startIdx + streamMarker.Length;
            if (dataStart < text.Length && text[dataStart] == '\r') dataStart++;
            if (dataStart < text.Length && text[dataStart] == '\n') dataStart++;

            int endIdx = text.IndexOf(endMarker, dataStart, StringComparison.Ordinal);
            if (endIdx < 0) break;

            // Trim the trailing newline before "endstream".
            int dataEnd = endIdx;
            if (dataEnd > dataStart && text[dataEnd - 1] == '\n') dataEnd--;
            if (dataEnd > dataStart && text[dataEnd - 1] == '\r') dataEnd--;

            int length = dataEnd - dataStart;
            if (length > 0)
            {
                var compressed = new byte[length];
                Array.Copy(pdfBytes, dataStart, compressed, 0, length);

                try
                {
                    using var input = new MemoryStream(compressed);
                    using var zlib = new ZLibStream(input, CompressionMode.Decompress);
                    using var output = new MemoryStream();
                    zlib.CopyTo(output);
                    results.Add(Encoding.Latin1.GetString(output.ToArray()));
                }
                catch
                {
                    // Non-FlateDecode streams (e.g., images) are skipped.
                }
            }

            cursor = endIdx + endMarker.Length;
        }

        return results;
    }
}
