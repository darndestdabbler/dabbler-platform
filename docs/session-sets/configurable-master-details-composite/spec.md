# Configurable Master-Details Composite Spec

> **Status:** **DRAFT — awaiting human review before kickoff.**
> **Purpose:** Build `ConfigurableMasterDetails<TEntity>`, a higher-level
> composite that wraps `MasterDetailExpansionShell<TRow,TEntity,TLink>` and
> drives the entire CRUD surface (header, filter, grid, details action row)
> from a single declarative `MasterDetailsConfig<TEntity>` value. Goal:
> cover ~90% of admin/data-entry CRUD pages with one component, leaving the
> existing slot-based shell as the escape hatch for the remaining ~10%.
> **Created:** 2026-05-12
> **Session Set:** `docs/session-sets/configurable-master-details-composite/`
> **Prerequisite:** `unified-master-details-composite` is complete (this set
> consumes its `MasterDetailExpansionShell`, `PickOrAddDialog`, and
> `DetailsContext` contracts).
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Session Set Configuration

```yaml
requiresUAT: true
requiresE2E: true
uatScope: full
outsourceMode: first
```

Reasoning: A composite that drives end-to-end CRUD across many entity types
has both interaction-contract concerns (covered by Playwright) and
visual-polish concerns (covered by human UAT). Each session that ships a
new capability (filter type, nav mode, action-row button, nested 1:N)
needs eyes on real pages, not just unit assertions on the component.

---

## Context

The newly-generic `MasterDetailExpansionShell<TRow, TEntity, TLink>` (from
the `unified-master-details-composite` set) is a slot-based primitive: every
consumer hand-authors `FilterContent`, `GridContent`, and `DetailsContent`
fragments and wires up the callbacks themselves. That flexibility is the
right primitive for the 10% of pages that need something unusual, but it
forces page authors (human or AI) to re-derive the same patterns
(server-side filtering, FK dropdowns with "(All)" sentinel, sortable
paging, "+ New" affordance, save/cancel/delete action row with
dirty-state + confirmation) on every CRUD page.

This set adds a higher-level `ConfigurableMasterDetails<TEntity>` that
consumes a `MasterDetailsConfig<TEntity>` record and translates it into
slot fills against the existing shell. Page authors write a config object
and a hand-written (or AI-written) `MudForm` body for the details surface;
everything else is generated.

Nested 1:N children are first-class: a `DetailsConfig.FormContent` can
embed another `ConfigurableMasterDetails<TChildEntity>` bound to a parent
collection. The parent's `SaveAsync` is the only server commit — child
saves stash mutations onto the parent's in-memory collection, and EF
handles cascade insert/update/delete on the parent's save round-trip.

M:N (with or without join-payload) is **explicitly out of scope**. The
existing `PickOrAddDialog` from the prior set already covers M:N pickers
launched from a parent form; this set does not replace or wrap that
pattern.

## Goals

1. `MasterDetailsConfig<TEntity>` (a `record`) declaratively captures
   header summary, filter type + properties, grid columns + ordering +
   navigation, and details save / delete / new-entity behavior. All
   property selectors use `Expression<Func<TEntity, …>>` so refactor
   tooling and compile-time type-checking work.
2. `ConfigurableMasterDetails<TEntity>` renders the entire surface from
   a config. Internally it wraps the existing
   `MasterDetailExpansionShell<TEntity, TEntity, object>` and fills its
   slots — the shell stays the lower-level primitive and consumers can
   still drop down to it for the 10% case.
3. **Filter** supports server-side / client-side / none modes and these
   property types: `Text`, `Numeric`, `Date`, `Bool`, `EnumVal`,
   `ForeignKey<TForeign>` (rendered as a `MudSelect` with an `(All)`
   sentinel at the top of the dropdown). FK lookup data comes from a
   consumer-supplied `LookupSource<TForeign>` (static list or async
   loader) — **no DI-by-type magic, no reflection over EF metadata**.
4. **Grid** supports paging (default) and infinite-scroll navigation, a
   default order-by expression, configurable width/height, and an
   optional **"+ New…" sentinel row** pinned at the top that, when
   clicked, opens the Details surface in Add mode.
5. **Add-new-row affordance** uses a dedicated icon column rendered as
   the first visible column when `ShowAddNewRow=true`:
   - New-row icon: `Icons.Material.Filled.AddCircle` (or `+`) with a
     tooltip "Add new {EntityLabel}".
   - Existing-row icon: `Icons.Material.Filled.Edit` (pencil) with a
     tooltip "Edit {EntityLabel}".
   - The new row's remaining cells display ghost text (italic, muted,
     "Add new …").
   - Clicking either the icon or anywhere on the row opens Details.
6. **Details action row** is owned by `ConfigurableMasterDetails`, not
   the form. The form contributes only the `MudForm` body (fields).
   Buttons:
   - **Save** — enabled when the entity is dirty (driven by the
     existing `DetailsContext.IsDirty`).
   - **Close / Cancel** — when not dirty, button reads "Close" and
     simply collapses the panel. When dirty, button reads "Cancel" and
     opens an `IDialogService` confirmation ("Discard unsaved
     changes?") before discarding.
   - **Delete** — visible when `DeleteAsync` is non-null. Enabled when
     the `CanDelete` predicate returns true (or always, if no predicate
     was provided). Click opens an `IDialogService` confirmation
     (text from `DeleteConfirmation(entity)` or a generic default).
7. **Filter persistence** — server-side filter state is serialized to
   the URL's query string. Reload / back-button restores the filter.
   Client-side filtering uses in-memory state and does **not** persist
   across reloads.
8. **Nested 1:N support** — the parent's `DetailsConfig.FormContent`
   may include `<ConfigurableMasterDetails TEntity="TChild" Config="@_childConfig" />`.
   The child's `SaveAsync` mutates the parent entity's in-memory
   collection only; the parent's `SaveAsync` is the sole server commit
   (EF cascade handles insert / update / delete of children).
9. **Reference migrations** prove the component on real pages: Session 1
   migrates `Products` (simplest CRUD), Session 5 migrates `Orders` with
   a nested `OrderItems` child collection.
10. **bUnit + Playwright** parity for every shipped capability — the
    component is not allowed to grow a feature without coverage at both
    levels.

## Non-goals

- **Auto-rendered form fields.** The `FormContent` `RenderFragment` is
  always hand- or AI-authored. Auto-generating fields from model
  metadata is fragile and is **explicitly out of scope** for this set.
- **M:N relationships.** Covered by the existing `PickOrAddDialog` from
  the prior set. This set does not wrap, replace, or extend that
  pattern.
- **Soft-delete UI decisions.** `DeleteAsync` is opaque to the
  component — the REST service layer behind it decides hard vs. soft
  delete. The UI just calls `DeleteAsync(entity, ct)` and reflects the
  result.
- **DI-by-type FK resolution.** Every FK filter property carries its
  own explicit `LookupSource<TForeign>`. No service-locator pattern.
- **EF metadata reflection.** No runtime probing of `IModel` or
  `DbContext` to discover relationships. All config is explicit.
- **Keyboard navigation.** Deferred to a follow-up set, as in the prior
  one.

## Architecture Context

Components touched (non-exhaustive):

- `<Dabbler.Components or new lib>/MasterDetail/ConfigurableMasterDetails.razor`
  — the new top-level component. **Final library location TBD** during
  Session 1 — the existing `MasterDetailExpansionShell` lives in
  `Dabbler.Security.Blazor`, but a generic-CRUD composite has no
  security dependency and probably belongs in `Dabbler.Components` (or
  a new `Dabbler.MasterDetail` lib).
- `<…>/MasterDetail/MasterDetailsConfig.cs` — the config record types
  (sketched below).
- `<…>/MasterDetail/LookupSource.cs` — discriminated union for FK data
  sources.
- `<…>/MasterDetail/FilterRenderer.razor` — internal helper that turns a
  `FilterConfig<TEntity>` into the shell's `FilterContent` slot.
- `<…>/MasterDetail/GridRenderer.razor` — internal helper that turns a
  `GridConfig<TEntity>` into the shell's `GridContent` slot, including
  the icon column, "+ New…" sentinel row, paging, and infinite-scroll.
- `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Products/ProductsPage.razor`
  — first reference migration (Session 1).
- `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Orders/OrdersPage.razor`
  and `OrderDetail.razor` — second reference migration with nested 1:N
  (Session 5).
- `tests/Dabbler.<…>.Tests/**` — bUnit coverage for the renderer
  helpers + the public `ConfigurableMasterDetails` API.
- `tests/AcmeCoffeeOnline.Playwright/**` — end-to-end coverage for each
  reference migration.

Reuse:

- `MasterDetailExpansionShell<TRow,TEntity,TLink>` (from the prior set).
  The new component is a wrapper, not a rewrite.
- `DetailsContext<TEntity>` cascading-value contract (from the prior
  set). The action row reads `IsDirty` and `IsTargetReadOnly` from it.
- `MudDataGrid`, `MudSelect`, `MudTextField`, `MudDatePicker`,
  `MudSwitch`, `MudDialog`, `MudForm` from MudBlazor. No custom grid
  or form surfaces.
- `IDialogService` for Cancel-when-dirty and Delete confirmations.
  Pattern mirrors `AddUserTypeRoleDialog` + the existing
  `CustomerPickerDialog` work.

### Decisions confirmed with the human (do not re-litigate)

- Property selectors are typed: `Expression<Func<TEntity, …>>`. Not
  string property names. (Compile-time safety + refactor support
  beats flexibility for AI-written configs.)
- FK lookup data comes from a consumer-supplied `LookupSource<TForeign>`
  per FK property. Not DI-by-type, not EF metadata.
- Form fields are hand- or AI-written `MudForm` content. Not
  auto-generated. The form **never** renders Save/Cancel/Delete
  buttons — those belong to `ConfigurableMasterDetails`.
- "+ New…" affordance is a pinned readonly row inside the same
  `MudDataGrid` (option B from earlier discussion), distinguished by an
  icon column (AddCircle for new, Edit/pencil for existing). Clicking
  anywhere on the new row opens Details in Add mode.
- Nested 1:N saves are EF-cascade: the parent's `SaveAsync` is the
  sole server commit. Child `SaveAsync` mutates the parent's in-memory
  collection.
- `DeleteAsync` is opaque — the REST layer decides soft vs. hard
  delete. The UI does not branch on it.
- Server-side filter state persists to the URL's query string;
  client-side filtering does not persist.
- M:N is out of scope. Use `PickOrAddDialog` from the prior set.

### Type sketch (final shapes to be confirmed in Session 1)

```csharp
public sealed record MasterDetailsConfig<TEntity>
{
    public required Expression<Func<TEntity, string>> SelectedRecordSummary { get; init; }
    public string ExpandedHeaderText { get; init; } = "Results";
    public string EntityLabel { get; init; } = "record";  // used in tooltips

    public FilterConfig<TEntity> Filter { get; init; } = FilterConfig<TEntity>.None;
    public required GridConfig<TEntity> Grid { get; init; }
    public required DetailsConfig<TEntity> Details { get; init; }
}

public abstract record FilterConfig<TEntity>
{
    public static FilterConfig<TEntity> None { get; } = new NoFilter();
    public sealed record NoFilter : FilterConfig<TEntity>;
    public sealed record ServerSide(IReadOnlyList<FilterProperty<TEntity>> Properties) : FilterConfig<TEntity>;
    public sealed record ClientSide(IReadOnlyList<FilterProperty<TEntity>> Properties) : FilterConfig<TEntity>;
}

public abstract record FilterProperty<TEntity>
{
    public required Expression<Func<TEntity, object?>> Property { get; init; }
    public string? Label { get; init; }

    public sealed record Text    : FilterProperty<TEntity>;
    public sealed record Numeric : FilterProperty<TEntity>;
    public sealed record Date    : FilterProperty<TEntity>;
    public sealed record Bool    : FilterProperty<TEntity>;
    public sealed record EnumVal : FilterProperty<TEntity>;
    public sealed record ForeignKey<TForeign> : FilterProperty<TEntity>
    {
        public required LookupSource<TForeign> Lookup { get; init; }
        public required Expression<Func<TForeign, object>> DisplayProperty { get; init; }
    }
}

public sealed record GridConfig<TEntity>
{
    public required IReadOnlyList<ColumnConfig<TEntity>> Columns { get; init; }
    public Expression<Func<TEntity, object>>? DefaultOrderBy { get; init; }
    public bool DefaultOrderDescending { get; init; }
    public NavigationConfig Navigation { get; init; } = new NavigationConfig.Paging(25);
    public bool ShowAddNewRow { get; init; } = true;
    public string? Width { get; init; }
    public string? Height { get; init; }
}

public sealed record ColumnConfig<TEntity>
{
    public required Expression<Func<TEntity, object?>> Property { get; init; }
    public string? Label { get; init; }
    public string? Format { get; init; }
    public bool Sortable { get; init; } = true;
}

public abstract record NavigationConfig
{
    public sealed record Paging(int PageSize) : NavigationConfig;
    public sealed record InfiniteScroll(int PageSize) : NavigationConfig;
}

public sealed record DetailsConfig<TEntity>
{
    public required RenderFragment FormContent { get; init; }
    public required Func<TEntity, CancellationToken, Task<TEntity>> SaveAsync { get; init; }
    public Func<TEntity, CancellationToken, Task>? DeleteAsync { get; init; }
    public Func<TEntity, bool>? CanDelete { get; init; }
    public Func<TEntity>? NewEntityFactory { get; init; }
    public Func<TEntity, string>? DeleteConfirmation { get; init; }
}

public abstract record LookupSource<TForeign>
{
    public sealed record Static(IReadOnlyList<TForeign> Items) : LookupSource<TForeign>;
    public sealed record AsyncLoader(Func<CancellationToken, Task<IReadOnlyList<TForeign>>> Load) : LookupSource<TForeign>;
}
```

### Convention: how `FormContent` is authored

A details form is a Razor component that receives the entity via the
cascading `DetailsContext<TEntity>` and follows seven rules:

1. Consume the cascading value: `[CascadingParameter] private DetailsContext<TEntity> Ctx { get; set; } = default!;`
2. Bind via `Value="@Ctx.Entity.Field" ValueChanged="@((T v) => Set(() => Ctx.Entity.Field = v))"`. **Never** use `@bind-Value` (it bypasses `SetDirty()`).
3. Forward `ReadOnly="@Ctx.IsTargetReadOnly"` on every field so the
   shell's `TargetEditability` modes work.
4. Required fields get `Required="true"` + `RequiredError`.
5. Layout uses `MudGrid` + `MudItem` with project breakpoints (`xs="12" md="6"`).
6. **No** Save / Cancel / Delete buttons in the form — the action row
   is owned by `ConfigurableMasterDetails`.
7. Nested 1:N child collections render `<ConfigurableMasterDetails TEntity="TChild" Config="@_childConfig" />`
   inside a `MudTabPanel` (or directly below the main fields). Each
   level owns its own dirty state. The parent's `SaveAsync` is the
   sole server commit; child saves only mutate the parent's
   in-memory collection.

These conventions are codified in this spec and in
`docs/planning/project-guidance.md` (added in Session 6's Step 9
reorganization review).

---

## Session Plan

### Session 1 of 6: Config types + minimal `ConfigurableMasterDetails` + Products migration

**Steps:**

1. Read planning docs (`docs/planning/project-guidance.md`,
   `docs/planning/lessons-learned.md`).
2. **Decide the host library** (`Dabbler.Components` vs. a new
   `Dabbler.MasterDetail` lib). Route the decision via `route(task_type="analysis")`.
3. Create the config record types (`MasterDetailsConfig`, `FilterConfig`,
   `GridConfig`, `ColumnConfig`, `NavigationConfig`, `DetailsConfig`,
   `LookupSource`) in the chosen library.
4. Create `ConfigurableMasterDetails<TEntity>` that:
   - Wraps `MasterDetailExpansionShell<TEntity, TEntity, object>`.
   - Renders **only** `FilterConfig.NoFilter`, `NavigationConfig.Paging`,
     **text-only** columns, and the default action row (Save / Close;
     no Delete, no "+ New", no FK).
   - Wires up `SaveAsync` with `OperationCanceledException` on null
     return (preserves dirty state on validation failure).
5. **Reference migration: Products.** Migrate
   `AcmeCoffeeOnline.Web/Components/Pages/Products/ProductsPage.razor`
   from its current bespoke composition to a `MasterDetailsConfig<ProductDto>` +
   `ConfigurableMasterDetails<ProductDto>` invocation. Hand-authored
   `ProductDetailsForm.razor` follows the seven conventions above.
6. **bUnit coverage** (`route(task_type="test-generation")`):
   - `ConfigurableMasterDetails` renders header/grid/details from a
     minimal config.
   - `SelectedRecordSummary` expression drives the collapsed header.
   - Save button is disabled until `Ctx.SetDirty()` is called.
   - Cancel-when-not-dirty closes without confirmation; cancel-when-dirty
     opens the confirmation dialog (verify dialog rendered).
7. **Playwright coverage** for the migrated Products page:
   - List loads with seeded data.
   - Edit a row, confirm Save is enabled, Save, confirm DB round-trip.
   - Edit a row, click Cancel — confirmation dialog appears, accept,
     confirm form discarded.
8. **Build and test.**
9. **Cross-provider verification.**
10. **Commit, push, notify.**

**Creates:** Config record types; `ConfigurableMasterDetails`;
`ProductDetailsForm`; bUnit + Playwright coverage.
**Touches:** `ProductsPage.razor` (full rewrite to the new pattern).
**Ends with:** Products page is functionally identical to pre-migration,
runs on the new composite, build + tests green.
**Progress keys:** `session-1/read-planning`, `session-1/lib-decision`,
`session-1/config-types`, `session-1/composite-skeleton`,
`session-1/products-migration`, `session-1/bunit-tests`,
`session-1/playwright`, `session-1/build-and-test`,
`session-1/verification`, `session-1/commit-push-notify`.

---

### Session 2 of 6: Filter — all property types except FK

**Steps:**

1. Read planning docs.
2. **Filter renderer.** Add `FilterConfig.ServerSide` + `FilterConfig.ClientSide`
   rendering for the five non-FK filter property types: `Text` (MudTextField),
   `Numeric` (MudNumericField), `Date` (MudDatePicker range),
   `Bool` (MudSwitch with three states: true / false / unset),
   `EnumVal` (MudSelect populated from the enum type).
3. **Server-side filter persistence.** Filter state serializes to the URL's
   query string. Reload restores. Client-side filtering uses in-memory state
   only (no query-string write).
4. **Wire filter onto the Products page** so at least one filter property of
   each non-FK type is exercised in a real page.
5. **bUnit coverage:**
   - Each filter property type renders the correct MudBlazor control.
   - `ClientSide` filter applies an in-memory predicate to the grid.
   - `ServerSide` filter reissues the grid's data request with the new state.
   - Query-string round-trip: setting filter → URL update;
     navigating to a URL with query string → filter populated.
6. **Playwright coverage** on Products:
   - Type in the text filter → grid narrows.
   - Reload → filter and grid state restored from URL.
   - Clear filter → grid restored to full list.
7. **Build and test.**
8. **Cross-provider verification.**
9. **Commit, push, notify.**

**Creates:** Filter renderer + query-string adapter; bUnit + Playwright tests.
**Touches:** `ConfigurableMasterDetails` (filter slot fill);
`ProductsPage.razor` config (adds filter properties).
**Ends with:** Five filter property types ship with server- + client-side
modes; query-string persistence for server-side works.
**Progress keys:** `session-2/read-planning`, `session-2/filter-renderer`,
`session-2/filter-persistence`, `session-2/products-filter-wiring`,
`session-2/bunit-tests`, `session-2/playwright`,
`session-2/build-and-test`, `session-2/verification`,
`session-2/commit-push-notify`.

---

### Session 3 of 6: FK filter + "+ New…" sentinel row

**Steps:**

1. Read planning docs.
2. **FK filter property.** Add `FilterProperty.ForeignKey<TForeign>`
   rendering as a `MudSelect` with an "(All)" sentinel at the top.
   Reads data from the consumer-supplied `LookupSource<TForeign>`
   (static or async). Server-side mode submits the selected FK as a
   filter value; client-side mode applies it as an in-memory predicate.
3. **"+ New…" sentinel row.** When `GridConfig.ShowAddNewRow=true`,
   render a pinned readonly row at the top of the data grid with:
   - First column: `AddCircle` icon, tooltip "Add new {EntityLabel}".
   - Remaining cells: ghost text (italic + muted), "Add new …".
   - Existing rows get an `Edit` (pencil) icon in the same first
     column, tooltip "Edit {EntityLabel}".
   - Click anywhere on the new row → opens Details in Add mode using
     `DetailsConfig.NewEntityFactory()` (or `Activator.CreateInstance`
     fallback if no factory). The expansion panel auto-expands the
     details surface and the row sentinel remains visible at the top
     of the grid.
4. **Wire FK filter and "+ New" onto Products** (Product belongs to a
   Category — exercise the FK filter; "+ New" enables Add-mode flow).
5. **bUnit coverage:**
   - FK filter renders MudSelect with "(All)" sentinel + entries from
     the LookupSource.
   - Selecting the FK filter applies the predicate.
   - "+ New…" row renders with the correct icon and tooltip; existing
     rows show pencil icon.
   - Click on "+ New…" opens Details with a fresh entity from
     `NewEntityFactory`.
6. **Playwright coverage:**
   - Pick a category from the FK filter dropdown → grid narrows.
   - Click "+ New…" row → details opens with empty form → fill +
     save → new product appears in grid → verified via API round-trip.
7. **Build and test.**
8. **Cross-provider verification.**
9. **Commit, push, notify.**

**Creates:** FK renderer; "+ New…" sentinel row; tests.
**Touches:** `ConfigurableMasterDetails` (FK + sentinel-row paths);
`ProductsPage.razor` config (adds FK filter, exercises "+ New").
**Progress keys:** `session-3/read-planning`, `session-3/fk-filter`,
`session-3/add-new-row`, `session-3/products-wiring`,
`session-3/bunit-tests`, `session-3/playwright`,
`session-3/build-and-test`, `session-3/verification`,
`session-3/commit-push-notify`.

---

### Session 4 of 6: Infinite-scroll nav + full action row (Delete + confirmations)

**Steps:**

1. Read planning docs.
2. **Infinite-scroll navigation.** Add `NavigationConfig.InfiniteScroll(PageSize)`
   rendering: the grid loads pages on a sentinel scroll into view.
   Tested via Playwright by scrolling to the sentinel.
3. **Full action row:**
   - **Save** — already in place (Session 1). Re-verify dirty-state
     gating still works.
   - **Close / Cancel toggle** — already in place (Session 1). Re-verify
     dirty-state-driven label flip and confirmation dialog.
   - **Delete** — new. Visible when `DeleteAsync` is non-null. Enabled
     when `CanDelete(entity)` returns true (or always, if `CanDelete`
     is null). Click opens an `IDialogService` confirmation with text
     from `DeleteConfirmation(entity)` (or generic default). Accept →
     call `DeleteAsync` → on success, collapse details and refresh
     grid; on failure (exception or `HttpRequestException`), keep
     details open and surface a snackbar error.
4. **Wire Delete onto Products** (`CanDelete = p => p.IsActive == false`
   as the example permission rule).
5. **Try `InfiniteScroll` on Products** (or a fresh demo page if Products
   is the wrong shape).
6. **bUnit coverage:**
   - InfiniteScroll renders the sentinel and the data-loaded predicate.
   - Delete button visibility honors `DeleteAsync` null-ness;
     enabled-state honors `CanDelete`.
   - Click Delete → confirmation dialog → accept → `DeleteAsync` called
     once with the entity → details collapsed.
   - Decline confirmation → details remain open, entity unchanged.
7. **Playwright coverage:**
   - Scroll the grid to load more rows (InfiniteScroll).
   - Click Delete on a deletable row → confirm → row removed from grid
     and from DB (verified via API).
   - Click Delete on a non-deletable row → button disabled (no
     confirmation dialog).
8. **Build and test.**
9. **Cross-provider verification.**
10. **Commit, push, notify.**

**Creates:** InfiniteScroll renderer; Delete button + dialog wiring;
tests.
**Touches:** `ConfigurableMasterDetails` (nav-mode branch, action-row
extension); `ProductsPage.razor` config.
**Progress keys:** `session-4/read-planning`, `session-4/infinite-scroll`,
`session-4/delete-button`, `session-4/products-wiring`,
`session-4/bunit-tests`, `session-4/playwright`,
`session-4/build-and-test`, `session-4/verification`,
`session-4/commit-push-notify`.

---

### Session 5 of 6: Nested 1:N — Orders → OrderItems migration

**Steps:**

1. Read planning docs.
2. **Confirm the nested-save model** in code: a child
   `ConfigurableMasterDetails<TChild>` is bound to a parent entity's
   in-memory collection. The child's `SaveAsync` mutates the parent's
   collection (add / update / mark-deleted) and returns. The parent's
   `SaveAsync` is the sole server commit; EF cascade handles
   insert/update/delete based on the tracker state of the parent
   graph at commit time.
3. **Migrate `OrdersPage.razor` + `OrderDetail.razor`** to
   `MasterDetailsConfig<OrderDto>` with a nested
   `MasterDetailsConfig<OrderItemDto>` for the items collection. The
   nested composite renders inside a `MudTabPanel` ("Items") in the
   order's `FormContent`.
4. **Keep the existing "Pick customer" PickOrAddDialog flow** wired on
   OrderDetail's customer field — this set does **not** rework M:N or
   pick-or-add patterns. It coexists with the new composite.
5. **bUnit coverage:**
   - Nested child renders independently with its own dirty state.
   - Child Save mutates the parent's in-memory collection without
     calling the parent's `SaveAsync`.
   - Parent Save commits the full graph via the parent's `SaveAsync`.
   - Child Delete (in-memory) removes the row from the parent's
     collection; subsequent parent Save deletes from DB via EF cascade.
6. **Playwright coverage:**
   - Open an existing order → switch to Items tab → add an item via
     "+ New…" → save the order → verify the item exists in DB.
   - Delete an item → save the order → verify the item is gone from DB.
   - Edit an item → save the order → verify the change is persisted.
7. **Build and test (full Playwright suite + DB integration).**
8. **Cross-provider verification.**
9. **Commit, push, notify.**

**Creates:** Nested-1:N runtime contract; bUnit + Playwright coverage.
**Touches:** `OrdersPage.razor`, `OrderDetail.razor` (rewritten on the
new composite); possibly `OrderDetailsForm.razor` (extracted from the
current order header).
**Progress keys:** `session-5/read-planning`,
`session-5/nested-save-model`, `session-5/orders-migration`,
`session-5/bunit-tests`, `session-5/playwright`,
`session-5/build-and-test`, `session-5/verification`,
`session-5/commit-push-notify`.

---

### Session 6 of 6: UAT checklist + coverage review + change log + reorganization

**Steps:**

1. Read planning docs.
2. **Generate UAT checklist** via `route(task_type="uat-script-author")`.
   File:
   `docs/session-sets/configurable-master-details-composite/configurable-master-details-composite-uat-checklist.json`.
   Schema:
   `https://github.com/darndestdabbler/uat-checklist-editor/blob/main/checklist-schema.json`.
   Items cover:
   - Filter — each of six property types — narrows the grid correctly.
   - Server-side filter persists via URL.
   - "+ New…" row opens Details in Add mode; Save creates a new entity.
   - Edit a row → Save → updated entity persists.
   - Delete with `CanDelete=false` → button disabled.
   - Delete with `CanDelete=true` → confirmation → row removed from DB.
   - Cancel-when-dirty → confirmation → form discarded.
   - Cancel-when-not-dirty → closes without confirmation.
   - Nested 1:N: add / edit / delete child items via the child
     composite, save the parent, all changes persist.
3. **Run W2 runner** on the checklist.
4. **Build + full test suite (including Playwright).**
5. **Cross-provider verification.**
6. **Generate change-log.md** via `route(task_type="summarization", max_tier=1)`.
7. **Cost report.**
8. **Commit, push, notify.**
9. **Step 9 reorganization review** — scan `docs/planning/project-guidance.md`
   + `docs/planning/lessons-learned.md` for promotion candidates. Likely
   candidates:
   - "Form-component conventions (cascading `DetailsContext`, no
     `@bind-Value`, action row owned by composite)" as a Convention.
   - "Config-driven composites with typed `Expression<>` property
     selectors" as a Principle.
   - "Nested 1:N uses in-memory mutation + EF cascade; child composites
     do not commit independently" as a Lesson.
   If no candidate qualifies, output the exact "no changes recommended"
   sentence. Accepted promotions ship in a **separate follow-up commit**.

**Creates:** UAT checklist; `change-log.md`; possibly
`project-guidance.md` / `lessons-learned.md` edits (separate commit).
**Progress keys:** `session-6/read-planning`,
`session-6/uat-checklist`, `session-6/uat-runner`,
`session-6/build-and-test`, `session-6/verification`,
`session-6/change-log`, `session-6/cost-report`,
`session-6/commit-push-notify`, `session-6/reorganization`.

---

## Exit criteria

- Build green on `Dabbler.Platform.sln`.
- All unit, bUnit, and Playwright tests pass (or documented in
  `FlakyPlaywrightCollection` with rationale).
- W2 runner returns a deterministic pass on the W0-compiled
  Playwright tests for every UAT item.
- `ConfigurableMasterDetails<TEntity>` ships with:
  - 6 filter property types (Text, Numeric, Date, Bool, EnumVal, FK).
  - 2 navigation modes (Paging, InfiniteScroll).
  - Full action row (Save / Close-Cancel / Delete with confirmations).
  - "+ New…" sentinel row with icon column.
  - URL query-string persistence for server-side filters.
- Products and Orders pages run on the new composite without regression
  versus pre-migration behavior. Existing PickOrAddDialog wiring on
  OrderDetail's customer field still works.
- Nested 1:N (Orders → OrderItems) demonstrates the EF-cascade save
  model end-to-end.
- UAT checklist exists and covers every shipped capability.
- Change log committed; notification sent.

## Risk notes

- **Library placement decision.** `Dabbler.Components` vs. a new
  `Dabbler.MasterDetail` lib affects every downstream consumer. Session 1
  routes this decision via `analysis` — do not skip the routing.
- **`Expression<>` parsing for property names.** Translating an
  `Expression<Func<TEntity, object?>>` into a property name (for the URL
  query string, for column accessors, for `OrderBy`) requires careful
  visitor code. Use a single helper (`ExpressionHelpers.GetMemberName`)
  and unit-test it independently of the composite.
- **Query-string round-tripping** for filter state has edge cases
  (whitespace, URL-encoded enums, null vs. absent). Round-trip tests
  must cover each property type plus a "navigate to URL with garbage
  query" case (graceful fallback to default state).
- **`InfiniteScroll` flake risk.** Browser scroll-into-view tests are a
  known Playwright flake source. Use `WaitForSelectorAsync` on the
  sentinel-loaded marker, not arbitrary timeouts. If a test proves
  unstable, follow the `feedback_fix_broken_tests_no_carry_forward`
  discipline.
- **Nested 1:N save semantics with EF cascade.** EF's cascade behavior
  depends on the relationship's `OnDelete` configuration and on whether
  child entities are tracked. The Session 5 bUnit + Playwright tests
  must cover insert / update / delete each in isolation **and** mixed
  in a single parent save, against a real DB (not mocks).
- **Form-author drift.** The seven `FormContent` conventions are easy
  to violate (especially the `@bind-Value` trap). Session 6's Step 9
  reorganization review should land them in
  `docs/planning/project-guidance.md` so AI authors see them as
  durable rules, not one-off spec text.
- **Pinned "+ New…" row inside `MudDataGrid`.** MudBlazor doesn't ship
  a first-class "frozen row" affordance. Implementation likely needs
  to prepend the sentinel as the first item in the `Items` collection
  and detect it in the row template — fine for paging mode but needs
  careful interaction with InfiniteScroll (the sentinel must not be
  treated as data). Spike this in Session 3 before committing to the
  approach.
