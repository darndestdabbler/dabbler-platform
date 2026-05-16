# Dabbler.Reports.Export

> Part of the Dabbler Platform. See
> [`docs/platform-overview.md`](../../../docs/platform-overview.md) for
> the platform-level view of how this library fits in.

PDF export for Dabbler Reports. Renders composed report pages to PDF
documents using `DabblerPdf`, with full support for fonts, styles,
images, borders, and multi-page output.

## Audience

Teams already using
[`Dabbler.Reports.Core`](../Dabbler.Reports.Core/README.md) to compose
banded reports who need to deliver those reports as PDF — for email
attachments, archival, or printable downloads — without bringing in a
separate reporting engine.

## Prerequisites and stack constraints

- .NET 10 (current LTS)
- Reference to
  [`Dabbler.Reports.Core`](../Dabbler.Reports.Core/README.md) for the
  composed-pages model
- Reference to `DabblerPdf` for PDF rendering (see warnings below for
  the license implication)

## Install

> **Adoption note:**
> The recommended adoption model for internal projects is **integrated source** under
> `src/Libs/Dabbler.Reports.Export/`. NuGet packages are available;
> to install the package:
> `dotnet add package Dabbler.Reports.Export --prerelease`

## Minimal registration

No DI registration required — `PdfExporter` is constructed directly per
report run.

## Smallest working example

```csharp
// 1. Compose report pages via Dabbler.Reports.Core
var layoutEngine = new BandLayoutEngine(report, textMeasurer);
var pages = layoutEngine.Compose(data);

// 2. Export to PDF
var exporter = new PdfExporter(report)
{
    Title = "Sales Summary",
    Author = "Report System"
};

byte[] pdfBytes = exporter.Export(pages);
File.WriteAllBytes("output.pdf", pdfBytes);
```

The PDF byte stream is `byte[]` — flow it into HTTP responses, file
storage, an email-attachment helper, or
[`Dabbler.Reports.Blazor`](../Dabbler.Reports.Blazor/README.md)'s
download affordance directly.

## Related packages

- [`Dabbler.Reports.Core`](../Dabbler.Reports.Core/README.md) — produces
  the composed-pages input this library consumes.
- [`Dabbler.Reports.Blazor`](../Dabbler.Reports.Blazor/README.md) —
  wires PDF download into the in-browser report lifecycle.

## Warnings and caveats

- **`DabblerPdf` license — non-MIT.** `DabblerPdf` is a Dabbler-authored
  C# port of [OpenPDF](https://github.com/LibrePDF/OpenPDF) and is
  **dual-licensed LGPL-2.1-only OR MPL-2.0** — not MIT. Consumers whose
  distribution policy requires non-copyleft licenses for transitive
  PDF dependencies should review this before adopting. The
  `THIRD-PARTY-NOTICES.txt` file is included in the NuGet package.
- **PDF font subsetting and embedding** is handled by DabblerPdf;
  consumers do not need to register fonts at the .NET level if their
  RDF references standard system fonts. Custom fonts require providing
  the font file path through the report definition.
- **License (this library): MIT** (see
  [LICENSE](https://licenses.nuget.org/MIT)).
