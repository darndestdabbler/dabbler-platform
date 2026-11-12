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
> `dotnet add package Dabbler.Reports.Export`

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
- **Fonts.** By default every family maps to one of the PDF Base 14
  fonts: Times, Times New Roman and serif to Times; Courier, Courier New
  and monospace to Courier; anything else (Arial, Segoe UI, ...) to
  Helvetica. The Base 14 fonts are not embedded and draw Windows-1252
  text only, so Polish, Cyrillic, Greek or CJK characters print as `?`.
  To print such text, register a TrueType family in code (no package
  ships a font file, so bring your own `.ttf` files) and name it as a
  style's `fontFamily` or the report's `defaultFontFamily`:

  ```csharp
  // Blazor: DabblerReportPage measures and exports with it.
  builder.Services.AddDabblerReportFonts(fonts =>
      fonts.Register("Noto Sans", "fonts/NotoSans-Regular.ttf",
                     boldPath: "fonts/NotoSans-Bold.ttf"));

  // Directly: give the same ReportFonts to the measurer and the exporter.
  var fonts = new ReportFonts().Register("Noto Sans", "fonts/NotoSans-Regular.ttf");
  var measurer = new OpenPdfTextMeasurer(report, fonts);
  var exporter = new PdfExporter(report, fonts);
  ```

  Family names match case-insensitively. A registered family is embedded
  as a subset and draws every character its font has a glyph for. A
  bold, italic or bold-italic style uses the file registered for it,
  else the regular file. What it does not do: no synthetic bold or
  italic, no text shaping (complex scripts and ligatures are drawn glyph
  by glyph) and no right-to-left layout, nothing beyond U+FFFF (such
  characters draw as the font's missing glyph), and no fallback from one
  font to another for a character the font lacks.
- **License (this library): MIT** (see
  [LICENSE](https://licenses.nuget.org/MIT)).

## If this library stops being maintained

This package is MIT-licensed, and its source is in
[`src/Libs/Dabbler.Reports.Export`](https://github.com/darndestdabbler/dabbler-platform/tree/master/src/Libs/Dabbler.Reports.Export).
If it stops being maintained, copy that source into your application,
replace the package reference with a project reference to your copy, and
own it from there. Keep the MIT license notice with the copied code.
