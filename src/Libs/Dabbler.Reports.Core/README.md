# Dabbler.Reports.Core

> Part of the Dabbler Platform. See
> [`docs/platform-overview.md`](../../../docs/platform-overview.md) for
> the platform-level view of how this library fits in.

Banded report layout engine with RDF-based report definitions. Supports grouping, aggregates, running totals, hide duplicates, subreports, PrintWhen expressions, and CanGrow/CanShrink layout.

## Capabilities

- **Banded layout** &mdash; Detail, group header/footer, page header/footer, and report header/footer bands
- **Grouping and sorting** &mdash; Multi-level grouping with aggregate summaries
- **Running totals** &mdash; Accumulate values across detail rows or group boundaries
- **Hide duplicates** &mdash; Suppress repeated values within groups
- **Subreports** &mdash; Embed child reports within parent report bands
- **PrintWhen expressions** &mdash; Conditionally show/hide elements based on data
- **CanGrow / CanShrink** &mdash; Dynamic element sizing based on content
- **Styles** &mdash; Named styles with font, color, alignment, borders, and padding
- **Expression evaluation** &mdash; Field references, parameters, built-in functions, and aggregate calculations

## Quick Start

```csharp
// Parse an RDF report definition
var parseResult = RdfParser.Parse(rdfJson);
var report = parseResult.Report;

// Execute the data query
var engine = new ReportDataEngine(dataProvider);
var data = await engine.ExecuteAsync(report, parameterValues);

// Compose pages
var layoutEngine = new BandLayoutEngine(report, textMeasurer);
var pages = layoutEngine.Compose(data);
```

## RDF Format

Report definitions use a JSON-based format (`.rdf.json`) that describes bands, elements, data sources, parameters, grouping, sorting, and styles. See the repository documentation for the full RDF schema reference.

## Dependencies

- `Dabbler.Components` (expression engine)
- `DabblerPdf` (text measurement) — Dabbler-authored C# port of
  [OpenPDF](https://github.com/LibrePDF/OpenPDF), ships as a NuGet
  package from the sibling
  [`darndestdabbler/dabbler-pdf`](https://github.com/darndestdabbler/dabbler-pdf)
  repository. **Dual-licensed LGPL-2.1-only OR MPL-2.0** (not MIT).
- .NET 10.0

## License

[MIT](https://licenses.nuget.org/MIT)
