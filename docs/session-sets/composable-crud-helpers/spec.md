# Composable CRUD Helpers Spec

> **Status:** Approved — kicked off 2026-05-13 (Session 1 in progress).
> **Purpose:** Replace the abandoned `ConfigurableMasterDetails<TEntity>`
> composite plan with a set of small, focused, composable helpers that
> the existing CRUD pages can adopt incrementally. Goal: eliminate the
> snapshot / dirty-state / action-row boilerplate that currently lives
> inline in every detail page, and extract the genuinely-good filter
> DSL from the prior plan into a standalone declarative filter panel.
> **Created:** 2026-05-13 (this file). See `spec.md.original` for the
> superseded `ConfigurableMasterDetails<TEntity>` design.
> **Session Set:** `docs/session-sets/composable-crud-helpers/`
> **Prerequisite:** `unified-master-details-composite` (already
> complete, VERIFIED). The existing
> `MasterDetailExpansionShell<TRow,TEntity,TLink>` stays in production
> as the inline-expansion escape hatch for genuinely-master-detail
> pages.
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Session Set Configuration

```yaml
requiresUAT: true
requiresE2E: true
uatScope: full
outsourceMode: first
```

Reasoning: New reusable Blazor primitives that drive every visible
interaction on the migrated pages (Products in Session 1, Orders in
Session 3, Customers in Session 4, Orders + nested OrderItems in
Session 5). Each migration changes UI internals on a production page
the human exercises during normal use. UAT and Playwright coverage
are both required.

---

## Context — why we pivoted

The original `configurable-master-details-composite` spec proposed a
single config-driven composite that drove an entire CRUD page from a
declarative `MasterDetailsConfig<TEntity>` record. At kickoff the
first reference migration (`/products`) revealed mismatches the
composite couldn't host cleanly:

- `/products` uses a separate route-based detail page; the composite
  was inline-expansion only.
- The detail page has page-specific affordances (**Undo**,
  **Toggle Availability**, **Back-to-products**) that don't fit the
  composite's Save / Cancel / Delete action row.
- The Session 1 composite skeleton supported only `NoFilter`, while
  the page has a 6-property `FilterPanel<ProductDto>`.
- Nine existing Playwright tests assert the route-based shape, the
  Back button, the Undo button, the Toggle button, and the filter
  chips. Strict migration would break or `[Skip]` ~6 of them.

The same architectural question routed to GPT 5.4 and Gemini Pro
both returned an unambiguous verdict: **the composite picked the
wrong axis of variation.** Real page variation in this app spans
routing model (inline vs route-based vs modal), action diversity
(Save/Cancel/Delete vs Undo/Toggle/Status-workflow/etc.), and filter
sophistication. A config-driven whole-page composite either accepts
all of those as enums (a config DSL that re-implements the Blazor
renderer) or rejects them (forcing every page into one rigid shape).

The replacement plan favors **composition over configuration**: small
focused helpers each pages adopt à la carte. The genuinely-good idea
from the original spec — a declarative filter DSL — is preserved as a
standalone `<DeclarativeFilterPanel>` in Session 2.

The Phase 1 codebase survey, the routed reviews (GPT 5.4 and Gemini
Pro at $0.044 combined), and my synthesis are recorded in the plan
file at `C:\Users\denmi\.claude\plans\putting-you-in-plan-happy-mochi.md`
in the orchestrator's plan archive.

---

## Goals

1. Ship a reusable `EditSession<T>` helper that captures snapshot /
   dirty-state / Undo / Commit semantics for any DTO, so pages don't
   re-author the field-by-field snapshot logic that today lives inline
   in `ProductDetail.razor` (lines 197–338).
2. Ship a reusable `<DirtyAwareActionRow>` Blazor component with
   Save / Cancel-or-Close / Delete buttons gated on `IsDirty`, a
   `CustomActions` `RenderFragment` slot for page-specific buttons
   (Undo, Toggle Availability, Status workflow, Send invitation), and
   matching `data-testid`s.
3. Ship a reusable `<DeclarativeFilterPanel TEntity="T" Config=… />`
   that renders MudBlazor filter controls from a typed
   `FilterConfig<TEntity>` record (the salvaged filter DSL from the
   original spec). Supports Text, Numeric (single + range), Date,
   Bool, EnumVal, and ForeignKey filter properties; server-side and
   client-side modes; URL query-string persistence for server-side
   state.
4. Migrate `/products` (Products list + detail) onto the helpers
   without changing user-visible behavior. All 9 existing
   `ProductsWorkflowTests.cs` Playwright tests remain green without
   modification. The migration replaces the inline snapshot/undo
   logic in `ProductDetail.razor` and the bespoke
   `FilterPanel<ProductDto>` in `ProductList.razor`.
5. Migrate `/orders` and `/customers` onto the same helpers, surfacing
   Status workflow buttons (`Mark Shipped`, etc.) and read-only
   sub-grids in the `CustomActions` slot.
6. Migrate the Orders → OrderItems nested 1:N edit flow using the
   existing `MasterDetailExpansionShell` + `<DirtyAwareActionRow>`
   inside the nested grid's row form. **No new composite required.**
7. bUnit + Playwright parity for every new helper. The helpers are
   not allowed to grow without coverage at both levels.
8. UAT checklist + change log + Step 9 reorganization review on the
   final session.

## Non-goals

- **`ConfigurableMasterDetails<TEntity>` and `MasterDetailsConfig<TEntity>`
  are explicitly discarded.** The original spec (preserved at
  `spec.md.original`) is the breadcrumb; do not re-introduce the
  whole-page composite under any new name.
- **No reflection-driven `GridConfig` / `ColumnConfig`.** Pages
  hand-author their `MudDataGrid<T>` directly. The helpers do not
  generate grids.
- **No auto-rendered form fields.** Pages hand-author their
  `MudForm` directly. The helpers only own action-row state and
  filter rendering.
- **No M:N coverage.** The existing `PickOrAddDialog` from the prior
  set already handles M:N pickers; this set does not wrap or replace
  it.
- **No deprecation of `MasterDetailExpansionShell`.** It remains the
  inline-expansion primitive for pages that genuinely want
  expand-below-the-grid editing.
- **Keyboard navigation.** Deferred to a follow-up set, as in the
  prior one.

## Architecture context

Components touched (non-exhaustive):

- `src/Libs/Dabbler.Components/MasterDetail/EditSession.cs` — new.
  Generic snapshot-and-dirty helper.
- `src/Libs/Dabbler.Components/MasterDetail/DirtyAwareActionRow.razor`
  — new. Save / Cancel-or-Close / Delete action row with
  `CustomActions` slot.
- `src/Libs/Dabbler.Components/MasterDetail/DeclarativeFilterPanel.razor`
  — new. Renders filter controls from a typed `FilterConfig<TEntity>`.
- `src/Libs/Dabbler.Components/MasterDetail/FilterConfig.cs` — new.
  Discriminated-union filter config records (Text / Numeric / Date /
  Bool / EnumVal / ForeignKey).
- `src/Libs/Dabbler.Components/MasterDetail/LookupSource.cs` — new.
  `LookupSource<TForeign>` discriminated union (Static / AsyncLoader).
- `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Products/ProductDetail.razor`
  — modified Session 1.
- `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Products/ProductList.razor`
  — modified Session 2.
- `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Orders/*` — modified Session 3.
- `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Customers/*` — modified Session 4.
- `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Orders/OrderDetail.razor`
  — modified Session 5 (nested 1:N).
- `tests/Dabbler.Components.Tests/MasterDetail/**` — new. bUnit
  coverage for each helper.
- `tests/AcmeCoffeeOnline.Playwright/**` — pre-existing tests must
  stay green; new tests added for filter persistence and Orders 1:N.

Reuse:

- `MasterDetailExpansionShell<TRow,TEntity,TLink>` and
  `DetailsContext<TEntity>` (from `unified-master-details-composite`)
  for pages that genuinely want inline expansion. The nested 1:N
  Session 5 work uses the shell directly.
- `MudDataGrid`, `MudSelect`, `MudTextField`, `MudDatePicker`,
  `MudSwitch`, `MudDialog`, `MudForm` from MudBlazor 8.x.
- `IDialogService` for Cancel-when-dirty and Delete confirmations.

### Decisions confirmed with the human (do not re-litigate)

- **Composition over configuration.** Each helper is a focused
  primitive that pages adopt à la carte. No whole-page composite.
- **Slug renamed** from `configurable-master-details-composite` to
  `composable-crud-helpers` on 2026-05-13. Original spec preserved
  at `spec.md.original`.
- **`/products` and `/products/{id}` routes stay.** The migration
  replaces internals, not routing. Existing Playwright tests stay
  green without modification through Sessions 1–2.
- **Page-specific affordances (Undo, Toggle Availability, Status
  workflow) live in the `CustomActions` slot.** The action row's
  primary buttons are Save / Cancel-or-Close / Delete only.
- **Filter DSL preserved.** `FilterConfig<TEntity>` /
  `FilterProperty<TEntity>` / `LookupSource<TForeign>` from the
  original spec ship as standalone types in Session 2 inside the
  filter panel — without being part of a whole-page composite.
- Property selectors are typed `Expression<Func<TEntity, …>>` (same
  as the original spec; compile-time safety beats stringly typing).
- Server-side filter state persists to URL query string;
  client-side filter does not. (Same as original spec.)
- Existing `MasterDetailExpansionShell` is the inline-expansion
  escape hatch and is not deprecated.

### Type sketch

```csharp
public sealed class EditSession<T> where T : class
{
    public T Live { get; }
    public bool IsDirty { get; }
    public void NotifyChanged();   // called by the form on field change
    public void Undo();             // restore Live to snapshot
    public void Commit();           // freeze current Live as the new baseline
    public void CommitFrom(T fresh); // overwrite Live + freeze (post-save)
}

public sealed record FilterConfig<TEntity>(
    FilterMode Mode,
    IReadOnlyList<FilterProperty<TEntity>> Properties);

public enum FilterMode { ServerSide, ClientSide }

public abstract record FilterProperty<TEntity>
{
    public required Expression<Func<TEntity, object?>> Property { get; init; }
    public string? Label { get; init; }

    public sealed record Text     : FilterProperty<TEntity>;
    public sealed record Numeric  : FilterProperty<TEntity> { public bool AsRange { get; init; } }
    public sealed record Date     : FilterProperty<TEntity> { public bool AsRange { get; init; } }
    public sealed record Bool     : FilterProperty<TEntity>;
    public sealed record EnumVal  : FilterProperty<TEntity>;
    public sealed record ForeignKey<TForeign> : FilterProperty<TEntity>
    {
        public required LookupSource<TForeign> Lookup { get; init; }
        public required Expression<Func<TForeign, object>> DisplayProperty { get; init; }
    }
}

public abstract record LookupSource<TForeign>
{
    public sealed record Static(IReadOnlyList<TForeign> Items) : LookupSource<TForeign>;
    public sealed record AsyncLoader(Func<CancellationToken, Task<IReadOnlyList<TForeign>>> Load) : LookupSource<TForeign>;
}
```

### Form-component conventions (durable rules)

Promoted from the original spec; these rules apply to every form
hosted by `<DirtyAwareActionRow>`:

1. Form fields bind via `Value="@_record.Field"` +
   `ValueChanged="@((T v) => { _record.Field = v; _session.NotifyChanged(); })"`.
   **Never** use `@bind-Value` — it bypasses dirty tracking.
2. Required fields get `Required="true"` + `RequiredError`.
3. Layout uses `MudGrid` + `MudItem` with project breakpoints
   (`xs="12" md="6"`).
4. **No** Save / Cancel / Delete buttons in the form — the action
   row is owned by `<DirtyAwareActionRow>`.
5. Page-specific buttons (Undo, Toggle Availability, Status
   workflow) go in the action row's `CustomActions` slot.
6. Nested grid forms render `<DirtyAwareActionRow>` per row form
   inside the nested `MasterDetailExpansionShell`.

These conventions are codified here and ship into
`docs/planning/project-guidance.md` in Session 6's Step 9
reorganization review.

---

## Session Plan

### Session 1 of 6: `EditSession<T>` + `<DirtyAwareActionRow>` + Products migration

**Steps:**

1. Read planning docs (`docs/planning/project-guidance.md`,
   `docs/planning/lessons-learned.md`,
   `docs/planning/session-set-authoring-guide.md`).
2. Author `ai-assignment.md` via `route(task_type="analysis")`.
3. **Build `EditSession<T>`** (`src/Libs/Dabbler.Components/MasterDetail/EditSession.cs`).
   JSON-snapshot-based; exposes `Live`, `IsDirty`, `NotifyChanged()`,
   `Undo()`, `Commit()`, `CommitFrom(fresh)`.
4. **Build `<DirtyAwareActionRow>`**
   (`src/Libs/Dabbler.Components/MasterDetail/DirtyAwareActionRow.razor`).
   Parameters: `IsDirty`, `Saving`, `OnSave`, `OnCancel` (or
   `OnClose`), `DeleteAsync?`, `CanDelete?`, `DeleteConfirmation?`,
   `SaveLabel`, `CloseLabel`, `CancelLabel`, `DeleteLabel`,
   `CustomActions` slot. `data-testid` defaults: `save-button`,
   `cancel-button`, `close-button`, `delete-button`.
5. **Migrate `ProductDetail.razor`** to use both helpers.
   - Replace the manual `_snapshot` / `UpdateDirtyState()` logic
     (lines 197–338) with `_session = new EditSession<ProductDto>(_record)`.
   - Replace the toolbar (`MudStack` with Save / Undo / Toggle
     Availability) with `<DirtyAwareActionRow>`. Undo + Toggle
     Availability go into `CustomActions` (they keep their existing
     `data-testid`s so Playwright stays green).
   - Form-field `ValueChanged` handlers call `_session.NotifyChanged()`.
   - On save, `_session.CommitFrom(updated)`.
6. **bUnit coverage** for the two new helpers
   (`route(task_type="test-generation")`):
   - `EditSession<T>` — initial `IsDirty=false`, mutating Live and
     calling `NotifyChanged` flips it true, `Undo` restores fields
     and flips false, `Commit` freezes the new baseline.
   - `<DirtyAwareActionRow>` — Save button disabled when not dirty,
     enabled when dirty; Close label flips to Cancel when dirty;
     Delete button visibility honors `DeleteAsync` null-ness; click
     Delete pops the `IDialogService` confirmation; `CustomActions`
     slot renders.
7. **No Playwright regression.** The 9 existing
   `ProductsWorkflowTests.cs` tests must pass without modification.
8. **Build and test.**
9. **Cross-provider verification.**
10. **Commit, push, notify.**

**Creates:** `EditSession.cs`, `DirtyAwareActionRow.razor`, bUnit
tests for both.
**Touches:** `ProductDetail.razor` (full internals rewrite; UI
unchanged).
**Ends with:** `/products/{id}` UI behavior identical to
pre-migration; new helpers exist and have bUnit coverage; existing
Playwright suite green.
**Progress keys:** `session-1/read-planning`,
`session-1/ai-assignment`, `session-1/edit-session`,
`session-1/action-row`, `session-1/products-migration`,
`session-1/bunit-tests`, `session-1/playwright-regression`,
`session-1/build-and-test`, `session-1/verification`,
`session-1/commit-push-notify`.

---

### Session 2 of 6: `<DeclarativeFilterPanel>` + Products filter migration

**Steps:**

1. Read planning docs.
2. **Build the filter records** (`FilterConfig.cs`, `LookupSource.cs`).
3. **Build `<DeclarativeFilterPanel TEntity="T">`** in
   `src/Libs/Dabbler.Components/MasterDetail/DeclarativeFilterPanel.razor`.
   Renders: `Text` → `MudTextField`; `Numeric` → `MudNumericField`
   (plus a paired min/max when `AsRange=true`); `Date` →
   `MudDatePicker` (plus a paired range when `AsRange=true`);
   `Bool` → tri-state `MudToggleGroup` (true / false / unset);
   `EnumVal` → `MudSelect` populated from the enum type;
   `ForeignKey<TForeign>` → `MudSelect` with "(All)" sentinel,
   populated from the consumer-supplied `LookupSource<TForeign>`.
4. **Server-side filter persistence.** Filter state serializes to
   the URL query string. Reload restores. Client-side filtering
   does not write to the URL.
5. **Wire onto `ProductList.razor`.** Replace the bespoke
   `FilterPanel<ProductDto>` with `<DeclarativeFilterPanel>` whose
   `Config` exercises Text (Name), three Enum (RoastLevel /
   CaffeineContent / GrindType), Bool (IsAvailable), and Numeric
   range (UnitPrice min/max) — the six properties the page has
   today. Existing filter behavior preserved.
6. **bUnit coverage** for each filter property type and for
   query-string round-trip.
7. **Playwright coverage** for the migrated filter (the two
   pre-existing filter tests must stay green; one new test for
   query-string reload restore).
8. **Build and test.**
9. **Cross-provider verification.**
10. **Commit, push, notify.**

**Creates:** `FilterConfig.cs`, `LookupSource.cs`,
`DeclarativeFilterPanel.razor`, bUnit + Playwright tests.
**Touches:** `ProductList.razor` (replaces FilterPanel; behavior
preserved).
**Progress keys:** `session-2/read-planning`,
`session-2/filter-records`, `session-2/filter-panel`,
`session-2/filter-persistence`, `session-2/products-wiring`,
`session-2/bunit-tests`, `session-2/playwright`,
`session-2/build-and-test`, `session-2/verification`,
`session-2/commit-push-notify`.

---

### Session 3 of 6: Orders migration

**Steps:**

1. Read planning docs.
2. Migrate `OrderList.razor` to `<DeclarativeFilterPanel>` (Status
   enum, CustomerName text, OrderDate range, Total range).
3. Migrate `OrderDetail.razor` to `EditSession<OrderDto>` +
   `<DirtyAwareActionRow>`. Status workflow buttons
   (`Mark Shipped`, `Mark Delivered`, etc.) go in `CustomActions`.
4. **bUnit coverage** for the migrated detail + filter wiring.
5. **Playwright coverage** for status workflow transitions (use the
   `CustomActions` testids).
6. **Build and test.**
7. **Cross-provider verification.**
8. **Commit, push, notify.**

**Creates:** bUnit + Playwright tests.
**Touches:** `OrderList.razor`, `OrderDetail.razor`.
**Progress keys:** `session-3/read-planning`,
`session-3/orders-migration`, `session-3/status-actions`,
`session-3/bunit-tests`, `session-3/playwright`,
`session-3/build-and-test`, `session-3/verification`,
`session-3/commit-push-notify`.

---

### Session 4 of 6: Customers migration + helper hardening

**Steps:**

1. Read planning docs.
2. Migrate `CustomerList.razor` to `<DeclarativeFilterPanel>`.
3. Migrate `CustomerDetail.razor` to `EditSession<CustomerDto>` +
   `<DirtyAwareActionRow>`. The read-only related-orders sub-grid
   stays as a `CustomActions` adjacent block (it's display, not an
   action — alternatively render it below the action row).
4. **Extract a `<ConfirmDeleteButton>` helper** if the
   `IDialogService` + `DeleteAsync` pattern has proven repetitive
   enough across Sessions 1–3 to justify it. If not, defer.
5. **bUnit + Playwright** coverage.
6. **Build and test.**
7. **Cross-provider verification.**
8. **Commit, push, notify.**

**Creates:** Possibly `ConfirmDeleteButton.razor` + tests; otherwise
just tests.
**Touches:** `CustomerList.razor`, `CustomerDetail.razor`.
**Progress keys:** `session-4/read-planning`,
`session-4/customers-migration`,
`session-4/optional-confirm-delete-extract`,
`session-4/bunit-tests`, `session-4/playwright`,
`session-4/build-and-test`, `session-4/verification`,
`session-4/commit-push-notify`.

---

### Session 5 of 6: Nested 1:N — Orders → OrderItems

**Steps:**

1. Read planning docs.
2. **Use the existing `MasterDetailExpansionShell` for the nested
   grid** (the shell is already production-tested for this shape).
   The row form inside the nested grid uses `<DirtyAwareActionRow>`
   for its action row. EF cascade handles insert/update/delete on
   the parent save.
3. **Migrate `OrderDetail.razor`** to host the nested OrderItems
   shell. The order's `<DirtyAwareActionRow>` is the sole server
   commit; child Save mutates the parent's in-memory collection.
4. **bUnit coverage** for the nested save semantics (add / edit /
   delete a child, verify parent collection state, verify parent
   `OnSave` commits the full graph).
5. **Playwright coverage** end-to-end with a real DB:
   - Add an item via the nested shell's "+ Add" affordance; save
     the order; verify the item exists in the DB.
   - Delete an item; save the order; verify the item is removed.
   - Edit an item; save the order; verify the change is persisted.
6. **Build and test (full Playwright suite).**
7. **Cross-provider verification.**
8. **Commit, push, notify.**

**Creates:** bUnit + Playwright tests for nested-save semantics.
**Touches:** `OrderDetail.razor` (extends to host nested shell).
**Progress keys:** `session-5/read-planning`,
`session-5/nested-shell-wiring`, `session-5/nested-save-semantics`,
`session-5/bunit-tests`, `session-5/playwright`,
`session-5/build-and-test`, `session-5/verification`,
`session-5/commit-push-notify`.

---

### Session 6 of 6: UAT checklist + change log + Step 9 reorganization

**Steps:**

1. Read planning docs.
2. **Generate UAT checklist** via `route(task_type="uat-script-author")`.
   File:
   `docs/session-sets/composable-crud-helpers/composable-crud-helpers-uat-checklist.json`.
   Schema:
   `https://github.com/darndestdabbler/uat-checklist-editor/blob/main/checklist-schema.json`.
   Items cover:
   - Save enabled when a field is edited (Products, Orders,
     Customers).
   - Save disabled after successful save.
   - Undo restores fields and disables Save (Products).
   - Toggle Availability flips state and persists (Products).
   - Status workflow buttons transition correctly (Orders).
   - Cancel-when-dirty pops the confirmation dialog (every page).
   - Filter persistence across reload (Products + Orders +
     Customers — server-side).
   - Nested OrderItems: add / edit / delete via the nested grid
     and save the parent.
3. **W2 runner** on the checklist.
4. **Build + full test suite.**
5. **Cross-provider verification.**
6. **Generate `change-log.md`** via
   `route(task_type="summarization", max_tier=1)`.
7. **Cost report.**
8. **Commit, push, notify.**
9. **Step 9 reorganization review.** Promote durable items:
   - "Form fields use `Value`/`ValueChanged` not `@bind-Value`;
     `<DirtyAwareActionRow>` owns the action row" as a Convention.
   - "Composition over configuration for CRUD page composites —
     prefer focused helpers over whole-page config DSLs" as a
     Principle, citing the routed-second-opinion convergence as
     the second context (the first was the abandoned composite
     itself).
   - "`EditSession<T>` for any page that maintains a dirty
     snapshot" as a Lesson if the second use case has emerged.

**Creates:** UAT checklist; `change-log.md`; possibly
`project-guidance.md` / `lessons-learned.md` edits (separate
commit).
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
- Three helper primitives ship: `EditSession<T>`,
  `<DirtyAwareActionRow>`, `<DeclarativeFilterPanel>` (and possibly
  `<ConfirmDeleteButton>` if extracted in Session 4).
- Products, Orders, and Customers detail pages run on the helpers.
  Orders demonstrates Status workflow buttons; Customers
  demonstrates a related-entity sub-grid.
- Orders → OrderItems nested 1:N edit flow uses the existing
  `MasterDetailExpansionShell` + `<DirtyAwareActionRow>` with no
  new composite required.
- UAT checklist exists and covers every shipped capability.
- Change log committed; notification sent.

## Risk notes

- **JSON-snapshot dirty detection.** `EditSession<T>` uses
  `JsonSerializer.Serialize` to snapshot the entity and to detect
  changes. This handles most DTOs cleanly but is sensitive to
  property-ordering quirks across BCL versions and to decimal /
  date round-tripping. Round-trip tests for each migrated DTO
  exercise add / edit / undo. If a DTO fails round-trip, the
  fallback is the explicit equality-delegate constructor.
- **Reflection-driven `OverwriteFrom` on Undo.** Iterates public
  read-write properties on `T` and copies values. Safe for DTOs;
  do not pass entity classes with EF-tracked nav properties.
- **Filter URL query-string edge cases.** Whitespace, URL-encoded
  enums, null vs absent — round-trip tests cover each property
  type plus a "navigate to URL with garbage query" graceful-fallback
  case.
- **Status workflow button discipline.** Orders' status buttons in
  `CustomActions` are page-specific. Watch for the temptation to
  promote them into the helper as a "WorkflowConfig" — that's
  exactly the over-broad abstraction this set is reacting against.
  Keep `CustomActions` opaque to the helper.
- **Nested 1:N save flake risk.** Playwright tests for the nested
  shell must use the watermark synchronization pattern
  (`WAIT FOR ACTIONS QUIET` after every nested interaction) per
  `docs/planning/project-guidance.md` → Testing Conventions.
- **Existing shell coupling to `Dabbler.Security.Blazor`.** The
  shell currently lives under that library. Session 5's nested
  usage may want a copy or relocation. Decide in-session — don't
  move the shell out of `Dabbler.Security.Blazor` without a
  routed analysis.
