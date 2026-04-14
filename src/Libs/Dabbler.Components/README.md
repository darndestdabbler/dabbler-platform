# Dabbler.Components

Blazor UI component library for notes-driven data workflows. The supported
default surface centers on query/list pages, detail editing, shell/navigation,
dashboard landing pages, and lookup flows that the current app and audit both
prove.

The current supported contract is defined in
`docs/planning/notes-driven-supported-library-surface.md`.

## Supported Core

| Surface | Notes-driven role |
|-----------|-------------|
| `DabblerQueryPage<T>` | Standard query/list page that composes the retained filter and grid behaviors |
| `DabblerDataGrid<T>` | Core search-result grid for sorting, paging, row selection, and CRUD-style workflows |
| `DabblerFilterGrid<T>` | QBE filter surface with row-based AND/OR logic and English summary output |
| `DabblerComboBox<T>` | Searchable, multi-column lookup with cascading coordination |
| `DabblerMultiComboBox<T>` | Retained multiselect lookup surface while the notes-driven combo contract stays compatibility-safe |
| `DabblerDetailForm<T>` | Validated single-record form editing |
| `DabblerRecordPopup<T>` | Modal record display/edit flow |
| `DabblerShell` | Top menu, left-nav, pinned-item, workspace, and runtime-provider shell composition |
| `DabblerDashboardPage` | Core landing-page/dashboard surface |
| `ExpressionEngine` | Support infrastructure for expression-based query/report behavior |

## Quick Start

```bash
dotnet add package Dabbler.Components
```

Register services in `Program.cs`:

```csharp
builder.Services.AddMudServices();
```

Compose a standard query page:

```razor
<DabblerQueryPage T="Customer"
                  Title="Customers"
                  FilterColumns="@filterColumns"
                  DataProvider="@dataProvider">
    <GridColumns>
        <PropertyColumn Property="x => x.Name" Title="Name" />
        <PropertyColumn Property="x => x.City" Title="City" />
    </GridColumns>
</DabblerQueryPage>
```

## Supported Workflow Themes

- QBE-style filtering with row-based AND/OR behavior
- query/list composition built from filter + result grid
- searchable lookup selection with cascading coordination
- retained multiselect lookup selection via `DabblerMultiComboBox<T>`
- validated detail editing and popup flows
- reusable shell/navigation and landing-page composition
- expression support used by higher-level query/report workflows

## Advanced Or Non-Core

These capabilities remain available for compatibility, but they are not the
primary supported default contract and are intentionally de-emphasized in the
current public story:

- grid grouping, aggregates, clipboard helpers, CSV export, column pinning,
  and detail expansion
- filter quick filter, presets, expression line output, and column toggles
- combo add-new flows and grouping headers
- favorites/recent-items dashboard widgets as default platform behavior

Opt into clipboard/CSV helpers on `DabblerQueryPage<T>` only when a workflow
explicitly needs them. The default query-page toolbar now stays focused on the
retained search/list path.

`DabblerCatalogPage` is currently a retirement candidate rather than a
first-choice reusable surface.

## Dependencies

- [MudBlazor](https://mudblazor.com/) 8.x
- .NET 10.0

## License

[MIT](https://licenses.nuget.org/MIT)
