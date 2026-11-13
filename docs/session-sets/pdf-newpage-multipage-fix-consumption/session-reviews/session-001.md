# Verification Round 1

VERIFIED

### ISSUES_FOUND

#### Issue 1: Redundant PDF parsing logic can be replaced by `PdfPig` an API. (Low Severity)

**â†’ Location:**
`tests/Dabbler.Reports.Blazor.Tests/CallerLevelPdfExportTests.cs`

**â†’ Fix:**
The `ProductionCaller_PopulatesInfoDictionaryFromRealRdf` test uses a complex, brittle custom method (`ExtractInfoDictionary`) to parse the PDF's `/Info` dictionary from raw bytes. Since `PdfPig` is already a dependency in this test project, this custom logic should be replaced with `PdfPig`'s built-in metadata parsing capabilities for simplicity, robustness, and maintainability.

```csharp
// In tests/Dabbler.Reports.Blazor.Tests/CallerLevelPdfExportTests.cs

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

    using var pdf = PdfDocument.Open(bytes);
    pdf.Information.Title.Should().Be(ExpectedTitle,
        "the production caller must forward ReportMetadata.Name into the PDF /Info dictionary so " +
        "Adobe Reader's Document Properties dialog shows the title (pdf-info-metadata UAT Finding 1)");
    pdf.Information.Author.Should().Be(ExpectedAuthor,
        "the production caller must forward ReportMetadata.Author into the PDF /Info dictionary " +
        "(pdf-info-metadata UAT Finding 1)");
    pdf.Information.CreationDate.Should().NotBeNull(
        "DabblerPdf 1.1.0 auto-stages CreationDate at Close(); this test confirms the upgrade is consumed");
}

// ... remove the private static string ExtractInfoDictionary(byte[] pdfBytes) method
```
