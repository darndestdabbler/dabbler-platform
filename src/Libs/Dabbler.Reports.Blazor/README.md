# Dabbler.Reports.Blazor

> Part of the Dabbler Platform. See
> [`docs/platform-overview.md`](../../../docs/platform-overview.md) for
> the platform-level view of how this library fits in.

Blazor report UI for the retained reporting workflow. The supported contract
centers on prompting for parameters, executing a report, and rendering the
composed output.

## Supported Core

| Component | Description |
|-----------|-------------|
| `DabblerReportPage` | Full report lifecycle: load RDF, prompt parameters, execute query, compose pages, and display the result |
| `ParameterPromptForm` | Parameter-entry surface for report runs |
| `ReportViewer` | Render composed report pages with zoom and layout fidelity |

## Quick Start

```razor
<DabblerReportPage RdfPath="reports/sales-summary.rdf.json"
                   DataProvider="@dataProvider"
                   AutoRunOnLoad="true" />
```

Or embed just the viewer:

```razor
<ReportViewer Pages="@composedPages"
              Report="@reportDefinition"
              Scale="1.0" />
```

## Supported Workflow Themes

- prompt for parameters before running a report
- execute report queries against a report data provider
- view composed report output in the browser
- keep PDF/export support as supporting behavior, not the main contract story

## Dependencies

- `Dabbler.Reports.Core` (report engine)
- `Dabbler.Reports.Export` (PDF export)
- `Dabbler.Components` (base components, expression engine)
- [MudBlazor](https://mudblazor.com/) 8.x
- .NET 10.0

## License

[MIT](https://licenses.nuget.org/MIT)
