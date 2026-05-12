# Unified Master-Details Composite Spec

> **Purpose:** Evolve `MasterDetailExpansionShell` into a single composite
> control that drives four distinct data-entry workflows (Add-Only;
> Search+Update; Pick-or-Add; Pick/Update-then-Use or Add-then-Use for
> many-to-many with join-table payload), backed by bUnit and Playwright
> coverage, with a per-set human-UAT checklist proving each scenario works
> end-to-end before sign-off.
> **Created:** 2026-04-23
> **Session Set:** `docs/session-sets/unified-master-details-composite/`
> **Plan:** `C:\Users\denmi\.claude\plans\three-common-and-one-shiny-lemur.md`
> **Prerequisite:** Role administration foundations set is complete (shell
> exists at its current single-purpose shape; migration here depends on that
> starting point).
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Session Set Configuration

```yaml
requiresUAT: true
requiresE2E: true
uatScope: full
outsourceMode: first
```

Reasoning: A composite UI control driving four distinct data-entry
workflows (Add-Only, Search+Update, Pick-or-Add, M2M-with-payload)
cannot be visually verified by automated tests alone — each workflow
needs human eyes confirming the layout, keyboard flow, and visual
polish look correct end-to-end. Playwright coverage handles the
interaction contracts and regression-detection; UAT exercises the
humans-actually-using-this verification dimension that automated
tests can't reach.

---

## Context

Four data-entry workflows are served today by bespoke page compositions
or by the existing `MasterDetailExpansionShell` — none of which covers
all four:

1. **Add Only.** Data-entry clerk repeats form-after-form. Wants max
   real estate, no grid chrome, forward-only keyboard navigation
   (*keyboard work is explicitly deferred to a follow-up set*).
2. **Search + Update.** Reviewer or clerk finds a record and edits it.
   This matches today's `MasterDetailExpansionShell` behavior.
3. **Pick-or-Add.** Parent form has a related record (e.g., Order
   needs a Customer). User searches existing; if found, links it;
   otherwise adds a new one and links it.
4. **Pick-then-AddAssocData or Add-then-Use for M2M with join-table
   payload.** Same as (3), but the relationship carries association
   data (start date, role-in-relationship, quantity, etc.). Two
   subscenarios:
   - **4a:** target entity may be edited alongside the link data.
   - **4b:** target entity must remain unchanged; only link data is
     editable. May optionally expose a toggle to unlock target edits
     when needed.

The same read-only-with-toggle pattern also serves a Scenario 3
variant: user drills into a candidate record to inspect fields before
deciding to link; target stays read-only unless the user chooses to
edit before linking.

The goal is one composite shell that drives all four, unified around
three independent knobs: *is a grid shown?*, *is the target editable?*,
and *is there link-data?* Plus a thin `PickOrAddDialog` wrapper for
the cases that render inside a parent form's modal.

## Goals

1. `MasterDetailExpansionShell<TRow, TEntity, TLink>` is generic and
   parameterized to drive Scenarios 1, 2, 3, and 4 from one
   component. Scenarios 3 and 4 are wrapped in `PickOrAddDialog` for
   modal invocation from parent forms.
2. FilterPanel moves inside the expansion panel (collapsing frees
   vertical real estate for the form).
3. `ShowGrid=false` drops the expansion panel entirely for
   Scenario 1.
4. Link-data fields render in their own visually-distinct "Link Data"
   card when `ShowLinkData=true`.
5. Target entity fields honor `TargetEditability` (`Editable` /
   `ReadOnly` / `ReadOnlyWithToggle`); the toggle renders an "Edit
   record" button when applicable and flips field bindings through a
   DetailsContext flag.
6. Action button relabels from "Use" → "Save and Use" once any field
   (target or link) becomes dirty; "Save" applies in Scenarios 1/2.
   Toggling `ReadOnlyWithToggle` alone does NOT flip the label.
7. `RoleMasterDetailsTab` and any other admin tabs currently wrapping
   the shell are migrated to the new slot pattern without behavior
   regression.
8. Every functional UAT checklist item has matching Playwright
   coverage via the W0 compile step (under the DSL pipeline; the
   retired `uat-coverage-review` AI gate is no longer used). The
   W2 runner pass is the coverage proof.
9. Human UAT proves each of the four scenarios works end-to-end
   before the set closes.

## Non-goals

- **Keyboard navigation** (Enter-moves-next-field for Scenario 1).
  Deferred to a follow-up set.
- **Full migration audit** of every existing page to the new
  composite. This set covers the shell, `RoleMasterDetailsTab` as the
  primary reference migration, and any admin tabs that would break
  build if left on the old surface. Other callers migrate as a
  follow-on.
- **New entity types.** Scenarios 3 and 4 wire against existing
  entities (e.g., Order → Customer for Scenario 3; OrderProductLine
  or similar for Scenario 4). If no suitable M2M-with-payload exists,
  build the minimum demo page the set needs rather than grafting a
  new domain entity.

## Architecture Context

Components touched (non-exhaustive):

- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Shared/MasterDetailExpansionShell.razor`
  (rewritten as generic).
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Shared/MasterDetailShellContexts.cs`
  (new — context records, enums, result types).
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Shared/PickOrAddDialog.razor`
  (new — generic dialog host).
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Role/RoleMasterDetailsTab.razor`
  (migrated to new slots).
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Role/RoleMasterGrid.razor`
  (stripped to pure grid fragment).
- Other admin tabs that wrap the shell (determine in-session via
  Explore; expected candidates: Organization, Users, Default Masks).
- `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/**` — one or more
  demo pages for Scenarios 1, 3, 4a, 4b.
- `tests/Dabbler.Security.Blazor.Tests/**` — bUnit coverage for shell
  parameter behavior.
- `tests/AcmeCoffeeOnline.Playwright/**` — end-to-end coverage for
  each scenario.

Reuse:

- `MudExpansionPanels`, `MudDataGrid`, `MudDialog`, `MudForm` from
  MudBlazor. No custom panel, grid, or dialog surfaces added.
- Existing `IDialogService.ShowAsync<T>()` pattern from
  `AddUserTypeRoleDialog` and `OrderNotePopup`.
- Existing `FilterPanel` component (moves inside the shell but is not
  modified).
- Existing `MudForm.IsTouched` / `FieldChanged` signals drive dirty
  state via `DetailsContext.SetDirty()` / `LinkContext.SetLinkDirty()`.

### Decisions confirmed with the human (do not re-litigate)

- Link-data fields render in a visually-distinct card, NOT merged
  into the target form.
- "Use" lives in two places at once: row-wise "Use" button in each
  grid row AND "Save and Use" primary button in the form's action
  row. Never in the collapsed panel header.
- Scenario 4b supports both `ReadOnly` (target locked) and
  `ReadOnlyWithToggle` (target unlocks on demand).
- Scenario 3 drill-in with read-only target uses the same
  `ReadOnlyWithToggle` mode as 4b.
- Keyboard work is out of scope for this set.
- No separate association-payload form component. The composite
  holds a single target form card plus an optional link-data card;
  the save handler splits into multiple EF writes behind the scenes.

---

## Session Plan

### Session 1 of 5: Generify the shell + migrate `RoleMasterDetailsTab` (Scenario 2 baseline)

**Steps:**

1. Read planning docs (`docs/planning/project-guidance.md`,
   `docs/planning/lessons-learned.md`).
2. **Pre-flight Explore.** Audit `src/Libs/Dabbler.Security.Blazor/**`
   for every consumer of `MasterDetailExpansionShell`. Record the
   list — each must be migrated in this session to keep the build
   green.
3. **Rewrite the shell** as
   `MasterDetailExpansionShell<TRow, TEntity, TLink>`. New parameters:
   `ShowGrid` (default `true`), `ShowAddAction` (default `true`),
   `ShowRowUseAction` (default `false`), `AllowLinkOnly` (default
   `false`), `Mode` (default `ShellMode.Inline`), `TargetEditability`
   (default `TargetEditability.Editable`), `ShowLinkData` (default
   `false`), `LinkDataHeading` (default `"Link Data"`), button labels,
   `Working` / `WorkingChanged`, `LinkWorking` / `LinkWorkingChanged`.
   New slots: `FilterContent`, `GridContent`, `RowUseAction`,
   `DetailsContent`, `LinkDataContent`, `DetailsActions`,
   `CollapsedSummaryContent`, `SessionSummaryContent`. New callbacks:
   `OnAddRequested`, `OnRowSelected`, `OnUseRequested`,
   `OnSaveRequested`, `OnSaveAndUseRequested`,
   `OnTargetEditingToggled`, `OnCancelRequested`.
   *Scenario-1 panel drop and link-data card rendering land in later
   sessions; the parameters are declared now so the surface is stable
   from the start.*
4. **Add `MasterDetailShellContexts.cs`** with `GridContext<TRow>`,
   `DetailsContext<TEntity>` (carrying `IsTargetReadOnly`,
   `ToggleTargetEditing`, `IsDirty`, `SetDirty`, `MudFormRef`),
   `LinkContext<TLink>`, `DetailsActionsContext<TEntity, TLink>`,
   `SaveRequest<TEntity, TLink>`, `SaveAndUseResult<TEntity, TLink>`,
   `ShellMode` enum, `TargetEditability` enum.
5. **Migrate `RoleMasterDetailsTab`** to the new slot pattern:
   FilterPanel moves into `FilterContent`; `RoleMasterGrid` strips
   down to a pure `GridContent` fragment; `RoleDetailsSection` moves
   into `DetailsContent`. `RoleMasterGrid`'s existing filter/toolbar
   responsibilities move out to their respective slots on the tab.
6. **Migrate any other shell consumers** identified in step 2 to the
   new slot pattern. The scope here is minimal — enough to keep the
   build green. Deep migration of those pages is out of scope.
7. **bUnit coverage.** Route test generation via
   `route(task_type="test-generation")` for:
   - Shell renders `FilterContent`, `GridContent`, and
     `DetailsContent` slots in Inline mode.
   - `ShowAddAction=false` hides the Add toolbar button.
   - `OnRowSelected` collapses the expansion panel and surfaces
     `CollapsedSummaryContent`.
   - `OnAddRequested` expands the panel if collapsed.
   - Regression: Role tab selection behavior mirrors pre-migration.
8. **Build and test.** `dotnet build Dabbler.Platform.sln` then
   `dotnet test Dabbler.Platform.sln`. Existing Role-related
   Playwright tests should still pass; if any break, root-cause
   and fix before proceeding (never silently tolerate — see Lessons).
9. **Cross-provider verification** via
   `route(task_type="session-verification")`. Include: rewritten shell,
   new contexts file, migrated `RoleMasterDetailsTab`, stripped
   `RoleMasterGrid`, any other migrated tabs, new bUnit tests, build
   and test results.
10. **Commit, push, notify.**

**Creates:** `MasterDetailShellContexts.cs`; bUnit tests for shell
slot/parameter behavior.
**Touches:** `MasterDetailExpansionShell.razor` (rewrite);
`RoleMasterDetailsTab.razor`; `RoleMasterGrid.razor`; any other admin
tabs wrapping the shell.
**Ends with:** Generic shell in place with all parameters declared;
Scenario 2 (Role master-details) passes bUnit + existing Playwright
with no behavior regression; build is green.
**Progress keys:** `session-1/read-planning`, `session-1/explore-consumers`,
`session-1/shell-generify`, `session-1/contexts-file`,
`session-1/migrate-role-tab`, `session-1/migrate-other-consumers`,
`session-1/bunit-tests`, `session-1/build-and-test`,
`session-1/verification`, `session-1/commit-push-notify`.

---

### Session 2 of 5: Scenario 1 (Add Only) — `ShowGrid=false` layout + demo

**Steps:**

1. Read planning docs.
2. **Scenario 1 layout.** When `ShowGrid=false`, the shell drops the
   `MudExpansionPanels` wrapper entirely and renders `DetailsContent`
   in a full-width `MudPaper`. Optional `SessionSummaryContent`
   fragment renders as a thin `MudAlert` above the form (e.g.,
   "Records added this session: 14").
3. **Session counter.** Add a shell-owned counter that increments on
   successful `OnSaveRequested` when `ShowGrid=false`. Expose via the
   `SessionSummaryContent` context so consumers render whatever copy
   they want. Form clears on save.
4. **Demo page.** Pick a real entity suitable for pure data-entry
   (determine in-session — the Customer or similar candidate).
   Create or repurpose a page rendering the shell with
   `ShowGrid=false, ShowAddAction=false` (Add is implicit — the page
   IS the add form). If no real-use page exists, build a minimal
   demo under `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/` that
   writes to an existing table.
5. **bUnit coverage** (`test-generation`):
   - `ShowGrid=false` renders no expansion panel.
   - Form takes full width (assertion: no `MudExpansionPanel` in
     render tree; `MudPaper` present).
   - Session counter increments on save; form buffer resets.
   - `SessionSummaryContent` renders when provided.
6. **Playwright coverage** for the demo page:
   - Navigate to the page; confirm no collapse/expand chrome.
   - Fill form; click Save; confirm form clears and counter
     increments.
   - Add a second record; confirm database persists both records
     (verify via the app's list view or a read-back API).
7. **Build and test.**
8. **Cross-provider verification.**
9. **Commit, push, notify.**

**Creates:** New demo page (or adaptation of existing); bUnit tests;
Playwright test.
**Touches:** `MasterDetailExpansionShell.razor` (ShowGrid=false
branch); shell contexts (session counter plumbing).
**Ends with:** Scenario 1 is demonstrated end-to-end: form-only
layout, session counter, database persistence verified through the
real UI.
**Progress keys:** `session-2/read-planning`, `session-2/showgrid-false`,
`session-2/session-counter`, `session-2/demo-page`,
`session-2/bunit-tests`, `session-2/playwright`,
`session-2/build-and-test`, `session-2/verification`,
`session-2/commit-push-notify`.

---

### Session 3 of 5: `PickOrAddDialog` + Scenario 3 (pick-or-add, no link data)

**Steps:**

1. Read planning docs.
2. **Create `PickOrAddDialog<TRow, TEntity, TLink>`** under
   `src/Libs/Dabbler.Security.Blazor/Components/Admin/Shared/`. Hosts
   the shell with `Mode=Dialog`. Translates `OnSaveAndUseRequested`
   and `OnUseRequested` into `IMudDialogInstance.Close(
   DialogResult.Ok(id))`. Accepts the same slot/parameter set as the
   shell so callers configure the dialog exactly like an inline
   shell.
3. **Row-wise Use.** Default `RowUseAction` template renders a
   `MudIconButton` that raises `OnUseRequested`. Consumer can
   override for custom button copy.
4. **Save-and-Use label-swap.** Implement dirty-state tracking:
   `_entityDirty` flips on `DetailsContext.SetDirty()` (consumer
   pushes from `MudForm` events). Action button reads "Use" when
   `AllowLinkOnly && SelectedRow != null && !_entityDirty`,
   otherwise "Save and Use" (when `ShowRowUseAction=true`) or "Save"
   (in Scenarios 1/2).
5. **Wire Scenario 3 in the app.** Pick an existing parent form that
   needs a related record (candidate: Order → Customer). Add a "Pick
   customer" button that opens `PickOrAddDialog<CustomerRow, Customer, object>`.
   On close-with-id, parent form stores the returned id.
6. **bUnit coverage** (`test-generation`):
   - Row-click on an existing row → `OnUseRequested` fires with that
     row; parent dialog closes with the row's id.
   - Click Add → form buffer clears → fill fields → Save-and-Use
     fires `OnSaveAndUseRequested`; parent closes with new id.
   - Click existing row, then touch a field → label flips from
     "Use" to "Save and Use".
   - Cancel resets buffer and reshows grid; dirty state resets.
7. **Playwright coverage** driving the real parent page:
   - Open parent form, click "Pick customer".
   - Row-wise Use links existing customer; parent's customer field
     populates; persist parent → verify customer id stored in DB.
   - Add path: Add → fill → Save-and-Use; new customer row exists
     in DB; parent's FK points at it.
   - Dirty relabel: pick existing row → type in a field → confirm
     button text changes to "Save and Use".
8. **Build and test.**
9. **Cross-provider verification.**
10. **Commit, push, notify.**

**Creates:** `PickOrAddDialog.razor`; bUnit tests; Playwright test;
any parent-form wiring.
**Touches:** Shell (dirty-state tracking, label logic, Mode=Dialog
rendering); parent form that adopts the dialog.
**Ends with:** Scenario 3 is demonstrated end-to-end through a real
parent form: row-wise Use and in-form Save-and-Use both close the
dialog with the correct id; dirty-state relabels correctly.
**Progress keys:** `session-3/read-planning`, `session-3/dialog-component`,
`session-3/row-use-template`, `session-3/dirty-state-label`,
`session-3/wire-parent-form`, `session-3/bunit-tests`,
`session-3/playwright`, `session-3/build-and-test`,
`session-3/verification`, `session-3/commit-push-notify`.

---

### Session 4 of 5: Scenarios 4a / 4b + inspect-to-decide — `LinkDataContent` + `TargetEditability`

> **Decisions locked 2026-05-12 (between Sessions 3 and 4):**
> - **Scenario 4b** wires the existing `OrderItemDto` (Order ↔ Product with
>   `Quantity`/`UnitPrice`/`Discount` link payload). The current ad-hoc
>   `_showProductPicker` flow in
>   [OrderDetail.razor:164-194](../../../src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Orders/OrderDetail.razor#L164-L194)
>   is **migrated** to a `PickOrAddDialog<ProductDto, ProductDto, OrderItemDto>`
>   with `ShowLinkData=true, TargetEditability=ReadOnly` (and a second
>   invocation with `ReadOnlyWithToggle` for the 4b-toggle variant). The
>   existing Playwright tests in `OrdersWorkflowTests` are the regression
>   net — they must stay green throughout the rewrite.
> - **Scenario 4a** (`ShowLinkData=true, TargetEditability=Editable`) does
>   not fit the Order Items flow (editing a `Product` mid-order is
>   contrived). Build a **small fresh demo page** (e.g.,
>   `/admin/order-item-tuner`) where editing the product alongside the
>   link data is the actual use case. Keep the demo minimal — the goal is
>   to exercise the 4a code path, not to ship admin tooling.
> - **Contingency:** if the OrderItems migration proves disruptive
>   mid-session, fall back to a fresh demo page for 4b too. Do not let
>   the migration block Session 4's verification.


**Steps:**

1. Read planning docs.
2. **Add `LinkDataContent` slot + `ShowLinkData`** rendering. Shell
   wraps the slot in a `MudCard` with heading `LinkDataHeading` (default
   "Link Data"), rendered above the target card so the link fields
   are the first thing the user sees.
3. **Add `TargetEditability` modes:**
   - `Editable` — current behavior.
   - `ReadOnly` — `DetailsContext.IsTargetReadOnly=true`,
     permanent. No toggle button.
   - `ReadOnlyWithToggle` — starts read-only; shell renders an
     "Edit record" button in the target card's header (or via
     `DetailsActions` slot) that calls `ToggleTargetEditing()`,
     flipping `IsTargetReadOnly`. `OnTargetEditingToggled(bool)`
     fires so consumers can log or pre-snapshot.
   - Consumers bind their `MudTextField` etc.'s `ReadOnly` or
     `Disabled` to `DetailsContext.IsTargetReadOnly`.
4. **Link dirty state.** `LinkContext.SetLinkDirty()` is the push
   channel; `_linkDirty` is separate from `_entityDirty`. Action
   label uses `_anyDirty = _entityDirty || _linkDirty`.
5. **Toggle without change preserves "Use".** Flipping
   `ReadOnlyWithToggle` to editable does NOT set `_entityDirty` by
   itself; only an actual field change does.
6. **Wire Scenarios 4a and 4b in the app.** Pick an existing M2M
   relationship with payload (candidate: OrderProductLine with
   quantity/unit-price; if none fit, build a small demo page against
   an existing join). Wire two invocations:
   - **4a:** `ShowLinkData=true, TargetEditability=Editable`.
   - **4b:** `ShowLinkData=true, TargetEditability=ReadOnly` AND a
     second variant with `TargetEditability=ReadOnlyWithToggle`.
   The save handler decides which EF writes to make from the
   `EntityDirty` / `LinkDirty` flags on `SaveAndUseResult`.
7. **Wire Scenario 3 inspect-to-decide.** Configure the
   `PickOrAddDialog` from Session 3 with
   `TargetEditability=ReadOnlyWithToggle, AllowLinkOnly=true,
   ShowLinkData=false`. User drills in read-only; "Use" stays
   available; toggling to edit and making a change flips label to
   "Save and Use".
8. **bUnit coverage** (`test-generation`):
   - `ShowLinkData=true` renders the link card with heading above
     the target card.
   - `TargetEditability=ReadOnly` → `IsTargetReadOnly=true` and no
     toggle button.
   - `TargetEditability=ReadOnlyWithToggle` → toggle button renders;
     clicking flips `IsTargetReadOnly`; `OnTargetEditingToggled` fires.
   - Link dirty alone (entity clean) flips label to "Save and Use".
   - Toggle-on-without-change → label stays "Use" when selected row
     is present.
   - `SaveAndUseResult` carries both buffers and both dirty flags.
9. **Playwright coverage** for each scenario:
   - **4a:** open dialog, fill both cards, Save-and-Use → both rows
     persist in DB; dialog returns target id.
   - **4b (locked):** open dialog, target fields rendered disabled,
     fill link fields only, Save-and-Use → only the join row
     persists (target unchanged in DB).
   - **4b (toggle):** open dialog, click "Edit record", target fields
     become editable, change one, Save-and-Use → both rows persist.
   - **Inspect-to-decide:** open dialog with `AllowLinkOnly=true`,
     drill into a row, target read-only, click "Use" → link only.
     Same dialog, toggle-edit-change, Save-and-Use → target and link
     both update.
10. **Build and test.**
11. **Cross-provider verification.**
12. **Commit, push, notify.**

**Creates:** Demo or adapted pages for Scenarios 4a, 4b, and
inspect-to-decide; bUnit tests; Playwright tests.
**Touches:** Shell (link card rendering, TargetEditability handling,
toggle button, link dirty state); `SaveRequest` / `SaveAndUseResult`
(add dirty flags); the parent form from Session 3 (to exercise
inspect-to-decide).
**Ends with:** All three remaining scenarios work end-to-end through
real pages; split-EF-write save handler proven; dirty-state logic
proven for each mode combination.
**Progress keys:** `session-4/read-planning`, `session-4/link-data-slot`,
`session-4/target-editability`, `session-4/link-dirty-state`,
`session-4/wire-4a`, `session-4/wire-4b`,
`session-4/wire-inspect-to-decide`, `session-4/bunit-tests`,
`session-4/playwright`, `session-4/build-and-test`,
`session-4/verification`, `session-4/commit-push-notify`.

---

### Session 5 of 5: UAT checklist + coverage review + change log + reorganization

**Steps:**

1. Read planning docs.
2. **Generate the UAT checklist** via
   `route(task_type="uat-plan-generation")`. The checklist is saved
   at
   `docs/session-sets/unified-master-details-composite/unified-master-details-composite-uat-checklist.json`
   and must match the schema at
   `https://github.com/darndestdabbler/uat-checklist-editor/blob/main/checklist-schema.json`.
   The checklist must cover:
   - **Scenario 1:** Add-Only page renders full-width with no
     expansion chrome; session counter increments; records persist;
     form clears per entry.
   - **Scenario 2:** Role master-details tab still collapses on row
     selection, expands on filter change, Save persists, no
     regression versus pre-migration.
   - **Scenario 3:** Parent form's "Pick customer" dialog — row-wise
     Use links existing; Add → Save-and-Use creates and links;
     dirty-state relabels correctly.
   - **Scenario 3 inspect-to-decide:** drill-in read-only; Use
     without toggle links; toggle-edit-change flips to Save-and-Use
     and persists both.
   - **Scenario 4a:** both cards editable; Save-and-Use persists
     both rows; dialog returns target id.
   - **Scenario 4b locked:** link card editable; target disabled;
     Save-and-Use persists only the join row.
   - **Scenario 4b toggle:** target starts read-only; toggle unlocks;
     changes persist target + join.
   Items whose expectation is purely aesthetic (e.g., "Link Data
   heading is visually prominent above the target card") are marked
   `IsJudgmentItem: true` with a one-sentence justification.
3. **Run the W2 runner** via
   `python ai-router/uat_runner/cli.py --checklist
   docs/session-sets/unified-master-details-composite/unified-master-details-composite-uat-checklist.json
   --catalog docs/component-catalog/index.json --session-set
   unified-master-details-composite`. The runner compiles the DSL
   checklist via W0, executes the compiled `.cs` via `dotnet test`,
   and classifies failures via the four-class triage taxonomy. The
   runner pass is the coverage proof — it replaces the retired
   `route(task_type="uat-coverage-review")` AI gate. Resolve any
   human-review issues filed by the triage classifier before
   committing.
4. **Build and test (full suite).**
   ```bash
   dotnet build Dabbler.Platform.sln
   dotnet test Dabbler.Platform.sln
   docker compose -f docker/docker-compose.infra.yml up -d
   docker compose -f docker/docker-compose.app.yml up --build -d
   dotnet test tests/AcmeCoffeeOnline.Playwright/
   ```
   If the Playwright run surfaces any deterministic fail, root-cause
   and fix before commit. If a test is pass-alone / fail-in-suite,
   move it to `FlakyPlaywrightCollection` with rationale. Never
   silently carry a broken test forward.
5. **Cross-provider verification** via
   `route(task_type="session-verification")`. Include the checklist,
   the W2 runner output, build/test logs, and pointers to the
   W0-compiled Playwright tests.
6. **Generate `change-log.md`** via `route(task_type="summarization")`.
   Summarize across all five sessions — do not re-narrate each one.
7. **Cost report** via `print_cost_report(SESSION_SET)`.
8. **Commit, push, notify** (all changes from this session, including
   the checklist and the change log, in a single commit).
9. **Step 9 reorganization review** — scan
   `docs/planning/project-guidance.md` and
   `docs/planning/lessons-learned.md` for candidates this set's work
   justifies (likely candidates: "Composite controls with capability
   parameters beat mode-specific variants" as a Convention or
   Principle promotion if the pattern holds elsewhere; "Split
   dirty-state per form section when a form hosts multiple EF
   entities" as a lesson). If no candidate qualifies, output the
   exact "no changes recommended" sentence. Commit any accepted
   proposals in a **separate follow-up commit**.
10. **Stop message** must:
    - State each scenario's UAT status (pending human review).
    - Link the checklist path.
    - Link the checklist editor
      (`https://darndestdabbler.github.io/uat-checklist-editor/`).
    - State that downstream session sets are blocked on the human's
      UAT review unless the human explicitly overrides.

**Creates:** `unified-master-details-composite-uat-checklist.json`;
`change-log.md`.
**Touches:** Potentially `docs/planning/project-guidance.md` and
`docs/planning/lessons-learned.md` (only if Step 9 accepted moves,
in a separate commit).
**Ends with:** Set work is complete; UAT checklist is human-ready
with full E2E parity; change log documents the set; notification
fires; reorganization review completes.
**Progress keys:** `session-5/read-planning`,
`session-5/uat-checklist`, `session-5/uat-runner`,
`session-5/build-and-test`, `session-5/verification`,
`session-5/change-log`, `session-5/cost-report`,
`session-5/commit-push-notify`, `session-5/reorganization`.

---

## Exit criteria

- Build green on `Dabbler.Platform.sln`.
- All unit, bUnit, and Playwright tests pass (or are documented in
  `FlakyPlaywrightCollection` with rationale).
- The W2 runner returned a deterministic pass on the W0-compiled
  Playwright test (replacing the retired `uat-coverage-review` gate).
- `MasterDetailExpansionShell` is generic and drives all four
  scenarios; `RoleMasterDetailsTab` and any other migrated tabs
  behave identically to pre-migration.
- `PickOrAddDialog` is used from at least one real parent form in
  the app, exercising Scenarios 3 and 3-inspect-to-decide.
- Scenario 4a and 4b each have a working demo or real-use page in
  the app, exercised by Playwright.
- `unified-master-details-composite-uat-checklist.json` exists and
  covers all four scenarios plus subscenarios (3-inspect-to-decide,
  4b-locked, 4b-toggle).
- Change log committed; notification sent.

## Risk notes

- **Shell rewrite touches shared admin infrastructure.** Keep the
  Session 1 cross-provider verification strict. If other admin
  consumers exist (Users, Organization, Default Masks per the
  exploration report), migrating them is in-scope for Session 1 to
  keep the build green — but deep feature work on those tabs is NOT
  in scope. If a consumer needs more than a slot-shape migration to
  compile, flag it for a follow-up set rather than growing Session 1.
- **M2M-with-payload demo entity.** If no existing join table fits
  the Scenarios 4a/4b demo (simple target + simple payload), build
  the minimum new demo page the set needs rather than introducing a
  new domain concept. Do not let Session 4 scope-creep into a
  domain-design exercise.
- **Playwright brittleness around dialogs.** Dialog open/close is a
  common Playwright flake source. Prefer explicit
  `IDialogService`-driven assertions and `WaitForSelectorAsync`
  before interacting with dialog content. If a test proves unstable,
  root-cause — do not silently tolerate. Follow the "Fix broken
  tests; do not carry them forward" discipline.
- **UAT coverage parity is mechanical, not approximate.** Under the
  DSL pipeline parity is a property of the W0 compile step (the
  Playwright `.cs` is the compiled output of the DSL checklist, so
  drift cannot occur). If the W2 runner reports any failure outside
  the auto-recovered `CATALOG_STALE` class, Session 5 cannot commit
  the checklist. Resolve filed `APP_FUNCTIONAL_BUG`,
  `SEED_DATA_GAP`, or `AMBIGUOUS_LOCATOR` issues before handoff —
  do not ship a checklist with implicit "the test is close enough"
  handwaves.
- **Keyboard scope creep.** The Add-Only scenario has a genuine
  usability gap around Enter-moves-next keyboard navigation. It is
  intentionally deferred. If a reviewer flags it during UAT, log it
  as a follow-up item, not a Session 2 remediation.
