# Dabbler.Components

Blazor UI component library for data-driven applications. Built for migrating Microsoft Access applications to the web, Dabbler.Components provides a rich set of components for filtering, data grids, navigation, and expression evaluation.

## Components

| Component | Description |
|-----------|-------------|
| `DabblerDataGrid<T>` | Data grid with sorting, QBE-style filtering, row grouping, inline editing, detail expansion, and keyboard navigation |
| `DabblerFilterGrid<T>` | Standalone query-by-example filter UI with AND/OR groups, operator selection, and saved presets |
| `DabblerComboBox<T>` | Typed combo box with search, multi-column display, grouping, and add-new support |
| `DabblerMultiComboBox<T>` | Multi-select variant of the combo box |
| `DabblerShell` | Master/detail navigation shell with menu bar, tree navigation, tabbed workspace, and detail forms |
| `DabblerQueryPage<T>` | Pre-built page combining a filter grid and data grid for query-style workflows |
| `DabblerDashboardPage` | Configurable dashboard with navigation cards, quick links, and embedded search |
| `DabblerCatalogPage` | Artifact browser for listing and navigating to entities |
| `DabblerDetailForm<T>` | Detail/form view for editing a single record |
| `DabblerRecordPopup<T>` | Popup dialog for viewing related records |
| `ExpressionEngine` | Evaluator for report-style expressions with fields, parameters, aggregates, and conditionals |

## Quick Start

```bash
dotnet add package Dabbler.Components
```

Register services in `Program.cs`:

```csharp
builder.Services.AddMudServices();
```

Render a data grid:

```razor
<DabblerDataGrid T="Customer"
                 DataProvider="@dataProvider"
                 EntityMetadata="@metadata" />
```

## Key Features

- **QBE-style filtering** &mdash; AND/OR filter groups with operator selection, date presets, and saved filter configurations
- **Row grouping** &mdash; Group by one or more columns with aggregate summaries (Sum, Count, Avg, Min, Max)
- **Keyboard navigation** &mdash; Full keyboard support across grids, combo boxes, and navigation panels
- **Expression engine** &mdash; Evaluate expressions referencing fields, parameters, and aggregate functions
- **Navigation shell** &mdash; Tabbed workspace with hierarchical navigation and metadata-driven UI
- **Many-to-many support** &mdash; Built-in handling for M:M relationship editing
- **CSV export** &mdash; Export grid data to CSV files
- **Accessibility** &mdash; WCAG-compliant with ARIA attributes, keyboard navigation, and screen reader support

## Dependencies

- [MudBlazor](https://mudblazor.com/) 8.x
- .NET 10.0

## License

[MIT](https://licenses.nuget.org/MIT)
