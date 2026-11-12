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
- MudBlazor 9.x
- .NET 10 (current LTS)
- Dependency-injected access to your `IGridDataProvider<T>` implementations

## Install

> **Adoption note:**
> The recommended adoption model for internal projects is **integrated source** —
> add a `<ProjectReference>` from your app csproj to
> `src/Libs/Dabbler.Components/`. NuGet packages are also available.
> To install via NuGet:
> `dotnet add package Dabbler.Components`

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
`MudDataGrid<T>` server-data callback. The panel takes its filterable
properties from `Config` (a `FilterConfig<TEntity>`) and raises
`FilterChanged` with the current `CompositeFilterDescriptor`; the page
keeps that descriptor and reloads the grid:

```razor
@page "/customers"
@using Dabbler.Components.Data
@using Dabbler.Components.MasterDetail
@using MudBlazor

<DeclarativeFilterPanel TEntity="Customer"
                        Config="_filterConfig"
                        FilterChanged="@OnFilterChangedAsync" />

<MudDataGrid T="Customer"
             @ref="_grid"
             Bordered="true"
             ServerData="@_serverData">
    <Columns>
        <PropertyColumn Property="x => x.Name" Title="Name" />
        <PropertyColumn Property="x => x.City" Title="City" />
    </Columns>
</MudDataGrid>

@code {
    [Inject] private IGridDataProvider<Customer> DataProvider { get; set; } = default!;

    private static readonly FilterConfig<Customer> _filterConfig = new(
        FilterMode.ServerSide,
        [
            new FilterProperty<Customer>.Text { Property = c => (object?)c.Name, Label = "Name" },
            new FilterProperty<Customer>.Text { Property = c => (object?)c.City, Label = "City" },
        ]);

    private MudDataGrid<Customer>? _grid;
    private CompositeFilterDescriptor? _filter;
    private Func<GridState<Customer>, CancellationToken, Task<GridData<Customer>>> _serverData = default!;

    protected override void OnInitialized() =>
        _serverData = DataProvider.ToServerDataFunc(() => _filter);

    private async Task OnFilterChangedAsync(CompositeFilterDescriptor? filter)
    {
        _filter = filter;
        if (_grid is not null)
            await _grid.ReloadServerData();
    }
}
```

`FilterMode.ServerSide` also writes the filter state to the URL query
string, so a reload restores it; `FilterMode.ClientSide` keeps it
transient. `AcmeCoffeeOnline.Web`'s `CustomerList.razor` is the full
version of this page (status text, debounce, row navigation).

Detail pages additionally compose `EditSession<T>` with
`<DirtyAwareActionRow>` for save/cancel/delete; see
`docs/platform-overview.md` § "CRUD Form Conventions" for the form
wiring pattern.

## `EditSession<T>` semantics

`EditSession<T>` tracks one record on a detail page. What it does, exactly:

- **`Live` is the instance passed to the constructor, not a copy.** The
  session holds that reference for its whole lifetime and never replaces
  it; the form mutates it directly, and any other code holding the same
  reference sees every edit as it happens.
- **`NotifyChanged()` serialises the whole object on every call.** It
  writes `Live` to JSON with `System.Text.Json` and compares the result
  with the snapshot taken by the constructor or the last `Commit`. Pages
  call it from each field's `ValueChanged`, so the cost of serialising
  the record is paid per change; `IsDirty` is cached between calls.
- **`Undo()` and `CommitFrom(fresh)` copy public properties that have a
  getter and a setter, and nothing else.** They reflect over the public
  instance properties of `typeof(T)` that can be read and written
  (init-only setters included) and are not indexers. Fields, get-only
  properties (a get-only collection among them) and properties declared
  only on a derived type are not touched. The copy is shallow: a
  reference-typed property is assigned, not cloned. `Undo()` copies from a
  deserialised copy of the snapshot; `CommitFrom(fresh)` copies from
  `fresh` and then takes a new snapshot.
- **`Commit()`** takes a new snapshot of `Live` and clears `IsDirty`.

The shape it is built for is a flat DTO of public read/write scalar,
string, enum and nullable properties. Do not pass an EF entity with
tracked navigation properties.

## Downloading a file

`DabblerDownloadExtensions.DownloadFileAsync` saves bytes in the browser
as a named file through the `dabbler-download.js` module this package
ships:

```csharp
@inject IJSRuntime Js

await Js.DownloadFileAsync("orders.csv", csvBytes, "text/csv");
```

The content crosses the circuit as one base64 string in a single JS
interop call (about a third larger than the bytes, and held in memory on
both the server and the browser). That suits a report or an export of a
few megabytes; a large file is better served from an HTTP endpoint the
browser downloads directly.

## Active core

The library's surface, organized by role:

| Surface | Role |
|---------|------|
| `DeclarativeFilterPanel<TEntity>` | Catalog-friendly QBE filter panel with AND/OR group logic, operator selection, presets, and active filter chips. Default for new query pages. |
| `EditSession<T>` | Dirty-tracking snapshot for a detail-page record (NotifyChanged / Commit / CommitFrom / Undo). Pairs with `<DirtyAwareActionRow>`. |
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
- **On a `MudTextField`, `data-testid` lands on the `<input>`, not the
  wrapper** (MudBlazor 9.x; checked against 9.12.0). Playwright tests
  should use `GetByTestId("foo")` directly rather than chaining a
  `.Locator("input")` after.
- **License: MIT** (see [LICENSE](https://licenses.nuget.org/MIT)).

## If this library stops being maintained

This package is MIT-licensed, and its source is in
[`src/Libs/Dabbler.Components`](https://github.com/darndestdabbler/dabbler-platform/tree/master/src/Libs/Dabbler.Components).
If it stops being maintained, copy that source into your application,
replace the package reference with a project reference to your copy, and
own it from there. Keep the MIT license notice with the copied code.
