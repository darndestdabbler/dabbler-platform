# Dabbler.Reports.Blazor

Blazor components for rendering and interacting with Dabbler Reports. Provides a report viewer for displaying composed pages, a parameter prompt form, and full-lifecycle report pages with filtering, zoom, and PDF export.

## Components

| Component | Description |
|-----------|-------------|
| `ReportViewer` | Renders composed report pages as styled HTML with RDF style resolution and configurable zoom |
| `ParameterPromptForm` | Dynamic form for report parameter entry with text, date, combo box, multi-select, checkbox, and numeric controls |
| `DabblerReportPage` | Full report lifecycle: load RDF, prompt parameters, execute query, compose pages, view, and export to PDF |
| `DabblerQueryReportPage<T>` | Report page variant with integrated data grid and filtering alongside the report view |

## Quick Start

```razor
<DabblerReportPage RdfPath="reports/sales-summary.rdf.json"
                   DataProvider="@dataProvider"
                   AutoRunOnLoad="true"
                   ShowPdfExport="true" />
```

Or embed just the viewer:

```razor
<ReportViewer Pages="@composedPages"
              Report="@reportDefinition"
              Scale="1.0" />
```

## Dependencies

- `Dabbler.Reports.Core` (report engine)
- `Dabbler.Reports.Export` (PDF export)
- `Dabbler.Components` (base components, expression engine)
- [MudBlazor](https://mudblazor.com/) 8.x
- .NET 10.0

## License

[MIT](https://licenses.nuget.org/MIT)
