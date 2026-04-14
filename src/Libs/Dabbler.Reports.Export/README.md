# Dabbler.Reports.Export

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
- `DabblerPdf` (PDF rendering)
- .NET 10.0

## License

[MIT](https://licenses.nuget.org/MIT)
