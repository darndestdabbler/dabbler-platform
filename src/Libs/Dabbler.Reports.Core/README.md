# Dabbler.Reports.Core

> Part of the Dabbler Platform. See
> [`docs/platform-overview.md`](../../../docs/platform-overview.md) for
> the platform-level view of how this library fits in.

Banded report layout engine with RDF-based report definitions. Supports
grouping, aggregates, running totals, hide duplicates, subreports,
`PrintWhen` expressions, and `CanGrow` / `CanShrink` layout.

## Audience

Teams needing banded business-report rendering (sales summaries,
order-by-customer, audit listings) where Excel + PivotTable is the
familiar mental model but the deliverable needs to be a typeset PDF or
an in-app interactive viewer. This library is the engine; see sibling
packages for export and Blazor UI.

## Prerequisites and stack constraints

- .NET 10 (current LTS)
- Reference to [`Dabbler.Components`](../Dabbler.Components/README.md)
  for the expression engine
- Reference to `DabblerPdf` for text measurement (see warnings below for
  the license implication)

## Install

> **Adoption note (Session 5 packaging-and-template-readiness):**
> Supported adoption today is **integrated source** under
> `src/Libs/Dabbler.Reports.Core/`. NuGet preview lands at end of
> Session 7; the future install reads
> `dotnet add package Dabbler.Reports.Core --prerelease`.

## Minimal registration

The library has no required DI registration — `RdfParser`,
`ReportDataEngine`, and `BandLayoutEngine` are constructed directly per
report run. If your app routes report data through a shared
abstraction, register your `IReportDataProvider` implementation:

```csharp
builder.Services.AddScoped<IReportDataProvider, YourReportDataProvider>();
```

## Smallest working example

```csharp
// 1. Parse an RDF report definition
var parseResult = RdfParser.Parse(rdfJson);
var report = parseResult.Report;

// 2. Execute the data query
var engine = new ReportDataEngine(dataProvider);
var data = await engine.ExecuteAsync(report, parameterValues);

// 3. Compose pages
var layoutEngine = new BandLayoutEngine(report, textMeasurer);
var pages = layoutEngine.Compose(data);

// The composed pages flow into Dabbler.Reports.Export (PDF) or
// Dabbler.Reports.Blazor (in-browser viewer).
```

## Capabilities at a glance

- **Banded layout** — Detail, group header/footer, page header/footer,
  and report header/footer bands.
- **Grouping and sorting** — Multi-level grouping with aggregate
  summaries.
- **Running totals** — Accumulate values across detail rows or group
  boundaries.
- **Hide duplicates** — Suppress repeated values within groups.
- **Subreports** — Embed child reports within parent report bands.
- **PrintWhen expressions** — Conditionally show/hide elements based on
  data.
- **CanGrow / CanShrink** — Dynamic element sizing based on content.
- **Styles** — Named styles with font, color, alignment, borders, and
  padding.
- **Expression evaluation** — Field references, parameters, built-in
  functions, and aggregate calculations.

## RDF format

Report definitions use a JSON-based format (`.rdf.json`) describing
bands, elements, data sources, parameters, grouping, sorting, and
styles. See [`docs/reports/rdf-schema.md`](../../docs/reports/rdf-schema.md)
for the full schema reference and one worked example.

## Related packages

- [`Dabbler.Reports.Export`](../Dabbler.Reports.Export/README.md) — PDF
  exporter that consumes composed pages from this library.
- [`Dabbler.Reports.Blazor`](../Dabbler.Reports.Blazor/README.md) —
  Blazor UI for the parameter-prompt → run → view lifecycle.
- [`Dabbler.Components`](../Dabbler.Components/README.md) — provides the
  expression engine this library uses for `PrintWhen` and aggregate
  evaluation.

## Warnings and caveats

- **DabblerPdf license — non-MIT.** `DabblerPdf` (this library's text-
  measurement dependency, and the engine `Dabbler.Reports.Export` uses
  for PDF rendering) is a Dabbler-authored C# port of
  [OpenPDF](https://github.com/LibrePDF/OpenPDF) and is
  **dual-licensed LGPL-2.1-only OR MPL-2.0** — not MIT. Consumers whose
  distribution policy requires non-copyleft licenses for transitive
  PDF dependencies should review this before adopting. A
  `THIRD-PARTY-NOTICES.txt` file ships in the package once NuGet
  preview lands (Session 7 deliverable per A7).
- **License (this library): MIT** (see
  [LICENSE](https://licenses.nuget.org/MIT)).
