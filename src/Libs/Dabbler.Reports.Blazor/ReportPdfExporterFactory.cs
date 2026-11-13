// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using Dabbler.Reports.Core.Layout;
using Dabbler.Reports.Core.Rdf;
using Dabbler.Reports.Export;

namespace Dabbler.Components;

/// <summary>
/// Factory that constructs a <see cref="PdfExporter"/> pre-configured with
/// the report definition's metadata. Centralises the "build exporter from
/// report definition" step so the production caller in
/// <see cref="DabblerReportPage.ExportToPdfAsync"/> and regression tests
/// share the same Title / Author wiring.
/// </summary>
public static class ReportPdfExporterFactory
{
    /// <summary>
    /// Creates a <see cref="PdfExporter"/> whose <c>Title</c> and
    /// <c>Author</c> are populated from the report definition's
    /// <see cref="ReportMetadata.Name"/> and
    /// <see cref="ReportMetadata.Author"/>. Empty metadata values flow
    /// through unchanged; <see cref="PdfExporter.ExportAsync"/> uses its
    /// own <c>IsNullOrWhiteSpace</c> guards before emitting <c>/Title</c>
    /// or <c>/Author</c> entries into the PDF <c>/Info</c> dictionary.
    /// </summary>
    /// <param name="reportDefinition">The parsed RDF.</param>
    /// <returns>A configured <see cref="PdfExporter"/> ready to export.</returns>
    public static PdfExporter Create(ReportDefinition reportDefinition) =>
        Create(reportDefinition, null);

    /// <summary>
    /// Creates a <see cref="PdfExporter"/> as <see cref="Create(ReportDefinition)"/> does, which
    /// draws a family registered in <paramref name="fonts"/> with its embedded TrueType file.
    /// </summary>
    /// <param name="reportDefinition">The parsed RDF.</param>
    /// <param name="fonts">The registered TrueType families, or null for the Base 14 fonts only.</param>
    /// <returns>A configured <see cref="PdfExporter"/> ready to export.</returns>
    public static PdfExporter Create(ReportDefinition reportDefinition, ReportFonts? fonts)
    {
        ArgumentNullException.ThrowIfNull(reportDefinition);
        return new PdfExporter(reportDefinition, fonts)
        {
            Title = reportDefinition.Metadata.Name,
            Author = reportDefinition.Metadata.Author,
        };
    }
}
