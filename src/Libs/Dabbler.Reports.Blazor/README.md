# Dabbler.Reports.Blazor

> Part of the Dabbler Platform. See
> [`docs/platform-overview.md`](../../../docs/platform-overview.md) for
> the platform-level view of how this library fits in.

Blazor Server UI for the report-running workflow. The supported
contract is parameter prompting, report execution, and rendering the
composed output; PDF export is supporting behavior layered on the
viewer.

## Audience

Teams already using
[`Dabbler.Reports.Core`](../Dabbler.Reports.Core/README.md) +
[`Dabbler.Reports.Export`](../Dabbler.Reports.Export/README.md) who
want the canonical "user picks parameters → report runs → user views
or downloads" lifecycle wired into their Blazor Server app without
re-implementing it page by page.

## Prerequisites and stack constraints

- Blazor Server
- MudBlazor 8.x
- .NET 10 (current LTS)
- Reference to
  [`Dabbler.Reports.Core`](../Dabbler.Reports.Core/README.md) and
  [`Dabbler.Reports.Export`](../Dabbler.Reports.Export/README.md)
- Reference to
  [`Dabbler.Components`](../Dabbler.Components/README.md) for the
  expression engine

## Install

> **Adoption note (Session 5 packaging-and-template-readiness):**
> Supported adoption today is **integrated source** under
> `src/Libs/Dabbler.Reports.Blazor/`. NuGet preview lands at end of
> Session 7; the future install reads
> `dotnet add package Dabbler.Reports.Blazor --prerelease`.

## Minimal registration

```csharp
builder.Services.AddMudServices(); // shared with Dabbler.Components

// Register your IReportDataProvider implementation
builder.Services.AddScoped<IReportDataProvider, YourReportDataProvider>();
```

## Smallest working example

A page that hosts a fully-wired report (parameter prompt → run →
viewer):

```razor
@page "/reports/sales-summary"

<DabblerReportPage RdfPath="reports/sales-summary.rdf.json"
                   DataProvider="@_dataProvider"
                   AutoRunOnLoad="true" />

@code {
    [Inject] private IReportDataProvider _dataProvider { get; set; } = default!;
}
```

Or embed only the viewer when the parameters have been collected
elsewhere:

```razor
<ReportViewer Pages="@_composedPages"
              Report="@_reportDefinition"
              Scale="1.0" />
```

## Supported components

| Component | Description |
|-----------|-------------|
| `DabblerReportPage` | Full report lifecycle: load RDF, prompt parameters, execute query, compose pages, and display the result. |
| `ParameterPromptForm` | Parameter-entry surface for report runs. |
| `ReportViewer` | Render composed report pages with zoom and layout fidelity. |

## Supported workflow themes

- Prompt for parameters before running a report.
- Execute report queries against an `IReportDataProvider`.
- View composed report output in the browser.
- Offer PDF export as supporting behavior (not the main contract story).

## Related packages

- [`Dabbler.Reports.Core`](../Dabbler.Reports.Core/README.md) — report
  engine consumed by `DabblerReportPage`.
- [`Dabbler.Reports.Export`](../Dabbler.Reports.Export/README.md) — PDF
  exporter wired in via the page's download affordance.
- [`Dabbler.Security.Blazor`](../Dabbler.Security.Blazor/README.md) —
  if the report page is access-controlled (typical), combines with the
  `DabblerSecured` wrapper.

## Warnings and caveats

- **DabblerPdf transitive license — non-MIT.** Through
  `Dabbler.Reports.Export`, this library carries a transitive
  dependency on `DabblerPdf` (LGPL-2.1-only OR MPL-2.0). Review
  before adopting if your distribution policy requires non-copyleft
  for PDF dependencies.
- **MudForm's `IsValid` is `true` on first render for empty required
  fields.** If a "Run report" button is gated on `IsValid`, call
  `_form.Validate()` in `OnAfterRenderAsync(firstRender: true)` so the
  gate reflects real form state before user interaction. See
  `docs/planning/lessons-learned.md` → *MudForm Does Not Invalidate
  Required-But-Untouched Fields On Load*.
- **License (this library): MIT** (see
  [LICENSE](https://licenses.nuget.org/MIT)).
