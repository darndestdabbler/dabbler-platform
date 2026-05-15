# Dabbler.Reports.Export

> Part of the Dabbler Platform. See
> [`docs/platform-overview.md`](../../../docs/platform-overview.md) for
> the platform-level view of how this library fits in.

PDF export for Dabbler Reports. Renders composed report pages to PDF documents using the DabblerPdf library, with full support for fonts, styles, images, borders, and multi-page output.

## Quick Start

```csharp
// Compose report pages first
var layoutEngine = new BandLayoutEngine(report, textMeasurer);
var pages = layoutEngine.Compose(data);

// Export to PDF
var exporter = new PdfExporter(report)
{
    Title = "Sales Summary",
    Author = "Report System"
};

byte[] pdfBytes = exporter.Export(pages);
File.WriteAllBytes("output.pdf", pdfBytes);
```

## Dependencies

- `Dabbler.Reports.Core` (report definitions, layout engine)
- `DabblerPdf` (PDF rendering) — Dabbler-authored C# port of
  [OpenPDF](https://github.com/LibrePDF/OpenPDF), ships as a NuGet
  package from the sibling
  [`darndestdabbler/dabbler-pdf`](https://github.com/darndestdabbler/dabbler-pdf)
  repository. **Dual-licensed LGPL-2.1-only OR MPL-2.0** (not MIT).
- .NET 10.0

## License

[MIT](https://licenses.nuget.org/MIT)
