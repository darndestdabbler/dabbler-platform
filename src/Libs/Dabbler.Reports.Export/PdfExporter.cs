using Dabbler.Reports.Core.Export;
using Dabbler.Reports.Core.Layout;
using Dabbler.Reports.Core.Rdf;
using DabblerPdf.Document;
using DabblerPdf.Pdf;
using DabblerPdf.Utilities;

namespace Dabbler.Reports.Export;

/// <summary>
/// Exports composed report pages to PDF format using DabblerPdf.
/// </summary>
/// <example>
/// <code>
/// var exporter = new PdfExporter(reportDefinition)
/// {
///     Title = "Monthly Sales Report",
///     Author = "Report System"
/// };
///
/// byte[] pdfBytes = exporter.Export(composedPages);
/// File.WriteAllBytes("report.pdf", pdfBytes);
/// </code>
/// </example>
public class PdfExporter : IReportExporter
{
    private readonly ReportDefinition _report;

    /// <summary>
    /// Initializes a new <see cref="PdfExporter"/> for the specified report definition.
    /// </summary>
    /// <param name="report">The report definition containing page layout, styles, and metadata.</param>
    public PdfExporter(ReportDefinition report)
    {
        ArgumentNullException.ThrowIfNull(report);
        _report = report;
    }

    /// <summary>
    /// Gets or sets the PDF document title metadata. Defaults to the report name.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the PDF document author metadata.
    /// </summary>
    public string? Author { get; set; }

    /// <inheritdoc />
    public Task ExportAsync(
        IReadOnlyList<PageLayout> pages,
        Stream outputStream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pages);
        ArgumentNullException.ThrowIfNull(outputStream);

        var styleResolver = new PdfStyleResolver(_report);
        var renderer = new PdfPageRenderer(styleResolver, _report.Metadata);

        // Create page size rectangle (DabblerPdf uses bottom-left origin, width × height in points)
        var pageSize = new Rectangle(renderer.PageWidthPoints, renderer.PageHeightPoints);

        using var document = new Document(pageSize);
        var writer = PdfWriter.GetInstance(document, outputStream);
        document.Open();

        if (pages.Count == 0)
        {
            // Write a valid single-page PDF document
            document.NewPage();
            document.Close();
            return Task.CompletedTask;
        }

        for (int i = 0; i < pages.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (i > 0)
                document.NewPage();

            var cb = writer.DirectContent;
            renderer.RenderPage(cb, pages[i]);
        }

        document.Close();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Exports the composed pages to a file.
    /// </summary>
    public async Task ExportToFileAsync(
        IReadOnlyList<PageLayout> pages,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pages);
        ArgumentNullException.ThrowIfNull(filePath);

        using var stream = File.Create(filePath);
        await ExportAsync(pages, stream, cancellationToken);
    }
}
