# Refresh response for `src/Libs/Dabbler.Components/README.md`

- Model: gemini-pro
- Input tokens: 1514
- Output tokens: 676
- Cost: $0.0087

---

```markdown
# Dabbler.Components

Blazor UI component library providing reusable filtering, expression engine,
and data provider abstractions for MudBlazor-native application pages.

After the MudBlazor-native rebuild, application pages use MudBlazor components
directly (MudDataGrid, MudForm, MudLayout, MudDialog, etc.) with explicit page
composition rather than heavyweight wrapper abstractions.

## Active Core

| Surface | Role |
|---------|------|
| `DeclarativeFilterPanel<T>` | MudBlazor-native QBE filter panel with AND/OR group logic, operator selection, presets, and active filter chips |
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

Register Dabbler and MudBlazor services in `Program.cs`:

```csharp
builder.Services.AddDabblerComponents();
builder.Services.AddMudServices();
```

Use `DeclarativeFilterPanel<T>` with `MudDataGrid<T>` for query pages:

```razor
<DeclarativeFilterPanel T="Customer"
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
```

---
**Verification (gpt-5-4-mini, openai):** ISSUES_FOUND

**Issue 1** [? / ?]: S FOUND

The revision is mostly aligned with current platform truth, especially the switch from `FilterPanel<T>` to `DeclarativeFilterPanel<T>`. However, there are two material concerns:

1. **`AddDabblerComponents()` is unverified and may be incorrect**
   - The updated Quick Start now tells consumers to call `builder.Services.AddDabblerComponents();`.
   - Nothing in the provided platform truth confirms that this public DI extension exists or is required.
   - If the package does not actually expose that method, this is a consumer-facing documentation bug.

2. **The README still omits key MudBlazor-native composable helpers**
   - Platform truth explicitly calls out `EditSession<T>`, `DirtyAwareActionRow`, and `DeclarativeFilterPanel` as the current CRUD composition model.
   - The doc mentions filtering and data-provider abstractions, but does not surface the broader composable CRUD story.
   - If those helpers live in `Dabbler.Components`, they should be called out in the “Active Core” section for completeness.

Everything else looks broadly consistent: no obvious stale sibling-repo references remain, and the `DeclarativeFilterPanel<T>` rename is the right direction.
