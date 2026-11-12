# Dabbler.Components

> Part of the Dabbler Platform. See
> [`docs/platform-overview.md`](../../../docs/platform-overview.md) for
> the platform-level view of how this library fits in.

MudBlazor-native CRUD primitives for Blazor Server: declarative QBE
filter panel, dirty-tracking edit session, master/detail expansion
shell, and the underlying expression-engine + data-provider
abstractions. Composition-over-configuration — pages declare their own
MudDataGrid columns and MudForm fields.

## Audience

Teams building Blazor Server CRUD applications who want a tightly
focused set of composable helpers (filter panel, dirty tracking, action
row, master-detail shell) instead of a config-driven page generator.
The helpers are à-la-carte: each page wires only the helpers it needs.

## Prerequisites and stack constraints

- Blazor Server (Blazor WebAssembly is out of scope today)
- MudBlazor 8.x
- .NET 10 (current LTS)
- Dependency-injected access to your `IGridDataProvider<T>` implementations

## Install

> **Adoption note:**
> The recommended adoption model for internal projects is **integrated source** —
> add a `<ProjectReference>` from your app csproj to
> `src/Libs/Dabbler.Components/`. NuGet packages are also available
> (current preview release `0.1.0-preview.1`).
> To install via NuGet:
> `dotnet add package Dabbler.Components --prerelease`

## Minimal registration

There is no library-wide DI extension — `Dabbler.Components` types are
used directly by the pages that need them. The only requirement is
MudBlazor services:

```csharp
builder.Services.AddMudServices();
```

If a page consumes a grid-data primitive, register the concrete provider
for that page's entity:

```csharp
builder.Services.AddScoped<IGridDataProvider<Customer>, CustomerGridDataProvider>();
```

## Smallest working example

A query page that combines `DeclarativeFilterPanel<TEntity>` with a
`MudDataGrid<T>` server-data callback:

```razor
@page "/customers"

<DeclarativeFilterPanel TEntity="Customer"
                        FilterColumns="@_filterColumns"
                        Coordinator="@_coordinator" />

<MudDataGrid T="Customer"
             Bordered="true"
             ServerData="@_dataProvider.ToServerDataFunc(_coordinator.GetCompositeFilter)">
    <Columns>
        <PropertyColumn Property="x => x.Name" Title="Name" />
        <PropertyColumn Property="x => x.City" Title="City" />
    </Columns>
</MudDataGrid>

@code {
    [Inject] private IGridDataProvider<Customer> _dataProvider { get; set; } = default!;
    private readonly FilteringCoordinator _coordinator = new();
    private readonly List<FilterColumnDefinition> _filterColumns = new()
    {
        new() { Field = nameof(Customer.Name), Caption = "Name" },
        new() { Field = nameof(Customer.City), Caption = "City" },
    };
}
```

Detail pages additionally compose `EditSession<T>` with
`<DirtyAwareActionRow>` for save/cancel/delete; see
`docs/platform-overview.md` § "CRUD Form Conventions" for the form
wiring pattern.

## Active core

The library's surface, organized by role:

| Surface | Role |
|---------|------|
| `DeclarativeFilterPanel<TEntity>` | Catalog-friendly QBE filter panel with AND/OR group logic, operator selection, presets, and active filter chips. Default for new query pages. |
| `EditSession<T>` | Dirty-tracking snapshot for a detail-page record (NotifyChanged / Commit / CommitFrom / Cancel). Pairs with `<DirtyAwareActionRow>`. |
| `<DirtyAwareActionRow>` | Save / Cancel / Delete action row that reads `EditSession<T>` state, with a `CustomActions` slot for page-specific buttons. |
| `<MasterDetailExpansionShell>` | Generic master/detail composite (Add-Only, Search+Update, Pick-or-Add, M:N-with-payload) in `Dabbler.Components.MasterDetail`, selected via capability parameters (`ShowGrid`, `ShowLinkData`, `TargetEditability`). An opt-in Add button above the grid (`ShowAddAction`, labelled by `AddLabel`) resets the selection and raises `OnAddRequested`. `PickOrAddDialog` hosts it in a `MudDialog`. |
| `FilterPanel<T>` | Original MudBlazor-native QBE filter panel; retained for legacy callers. New pages prefer `DeclarativeFilterPanel<TEntity>`. |
| `FilteringCoordinator` | Manages filter state, group lifecycle, and change notification. |
| `FilterColumnDefinition` | Defines filterable columns with types, operators, and editor metadata. |
| `FilterGroupCollection` / `FilterGroup` | AND/OR group model for composable filter expressions. |
| `CompositeFilterDescriptor` / `FilterDescriptor` | Filter expression descriptors consumed by the expression engine. |
| `FilterSummaryBuilder` | Generates human-readable filter summary text. |
| `IGridDataProvider<T>` | Data provider abstraction with `ToServerDataFunc` for MudDataGrid ServerData callbacks. |

## Related packages

- [`Dabbler.Security.Blazor`](../Dabbler.Security.Blazor/README.md) —
  combines with the helpers above for restriction-aware CRUD pages
  (column hiding, row scoping, control disablement).
- [`Dabbler.Reports.Blazor`](../Dabbler.Reports.Blazor/README.md) —
  consumes the expression engine for report parameter prompts.
- [`Dabbler.Platform.Stubs`](../Dabbler.Platform.Stubs/README.md) —
  provides in-memory `StubGridDataProvider<T>` and friends for bUnit
  tests and demo hosts.

## Warnings and caveats

- **The library focuses on à-la-carte helpers, not a metadata DSL.**
  The abandoned `MasterDetailsConfig<TEntity>` config-DSL prototype is
  not coming back; see `docs/planning/project-guidance.md` →
  *Composition over configuration*.
- **MudBlazor 8.x `data-testid` lands on the `<input>`, not the
  wrapper.** Playwright tests should use `GetByTestId("foo")` directly
  rather than chaining a `.Locator("input")` after.
- **`MudTextField ReadOnly="true"` does NOT set the HTML `readonly`
  attribute** in MudBlazor 8.x. Tests asserting read-only state must
  use behavior-level proxies (e.g., the toggle-button text), not
  `Not.ToBeEditableAsync`.
- **License: MIT** (see [LICENSE](https://licenses.nuget.org/MIT)).

## If this library stops being maintained

This package is MIT-licensed, and its source is in
[`src/Libs/Dabbler.Components`](https://github.com/darndestdabbler/dabbler-platform/tree/master/src/Libs/Dabbler.Components).
If it stops being maintained, copy that source into your application,
replace the package reference with a project reference to your copy, and
own it from there. Keep the MIT license notice with the copied code.
