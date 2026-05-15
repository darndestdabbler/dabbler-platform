# Dabbler.Components

Blazor UI component library providing reusable filtering, expression engine,
and data provider abstractions for MudBlazor-native application pages.

After the MudBlazor-native rebuild, application pages use MudBlazor components
directly (MudDataGrid, MudForm, MudLayout, MudDialog, etc.) with explicit page
composition rather than heavyweight wrapper abstractions.

## Active Core

### CRUD master/details composition

The library ships a small set of à-la-carte helpers that pages wire up
independently rather than being generated from a config object. Each
helper does one thing and is composed by the page itself.

| Surface | Role |
|---------|------|
| `DeclarativeFilterPanel<TEntity>` | Catalog-friendly QBE filter panel with AND/OR group logic, operator selection, presets, and active filter chips. The current default for new query pages. |
| `EditSession<T>` | Dirty-tracking snapshot for a detail-page record (NotifyChanged / Commit / CommitFrom / Cancel). Pairs with `<DirtyAwareActionRow>`. |
| `<DirtyAwareActionRow>` | Save / Cancel / Delete action row that reads `EditSession<T>` state, with a `CustomActions` slot for page-specific buttons. |
| `<MasterDetailExpansionShell>` | Generic master-detail composite (Add-Only, Search+Update, Pick-or-Add, M:N-with-payload) selected via capability parameters (`ShowGrid`, `ShowLinkData`, `TargetEditability`). |
| `FilterPanel<T>` | Original MudBlazor-native QBE filter panel; retained for legacy callers. New pages prefer `DeclarativeFilterPanel<TEntity>`. |

### Filter and data-provider primitives

| Surface | Role |
|---------|------|
| `FilteringCoordinator` | Manages filter state, group lifecycle, and change notification |
| `FilterColumnDefinition` | Defines filterable columns with types, operators, and editor metadata |
| `FilterGroupCollection` / `FilterGroup` | AND/OR group model for composable filter expressions |
| `CompositeFilterDescriptor` / `FilterDescriptor` | Filter expression descriptors consumed by the expression engine |
| `FilterSummaryBuilder` | Generates human-readable filter summary text |
| `ExpressionEngine` | Expression-based query and report support infrastructure |
| `IGridDataProvider<T>` | Data provider abstraction with `ToServerDataFunc` for MudDataGrid ServerData callbacks |

## Compatibility Surfaces

These components support the `PlatformStubDemo` scenario and are not used by
the main application, which has moved to MudBlazor-native layout and routing:

| Surface | Role |
|---------|------|
| `DabblerShell` | Left-nav + tabbed-workspace shell for stub and demo app modes |
| `DabblerNavigationPanel` | Entity/report navigation panel composed into DabblerShell |
| `DabblerTabbedWorkspace` | In-shell tab manager for search, detail, report, and dashboard content |
| `DabblerTopMenuBar` | Optional artifact-browsing menu bar when catalog/usage providers are supplied |
| `DabblerCatalogPage` | Compatibility-only artifact catalog page used by DabblerShell |

## Quick Start

Register MudBlazor services in `Program.cs` (Dabbler.Components types
have no project-wide DI extension — components and helpers are used
directly by the pages that need them):

```csharp
builder.Services.AddMudServices();
```

Use `DeclarativeFilterPanel<TEntity>` with `MudDataGrid<T>` for query pages:

```razor
<DeclarativeFilterPanel TEntity="Customer"
                        FilterColumns="@filterColumns"
                        Coordinator="@coordinator" />

<MudDataGrid T="Customer"
             ServerData="@dataProvider.ToServerDataFunc(coordinator.GetCompositeFilter)">
    <Columns>
        <PropertyColumn Property="x => x.Name" Title="Name" />
        <PropertyColumn Property="x => x.City" Title="City" />
    </Columns>
</MudDataGrid>
```

## Dependencies

- [MudBlazor](https://mudblazor.com/) 8.x
- .NET 10.0

## License

[MIT](https://licenses.nuget.org/MIT)
