# Session 7 Implementation Plan

> **Session:** 7 of 8
> **Scope:** Master-details UX refactor, DisplayAlias layout fix, and dashboard keyboard-handler removal
> **Source session set:** `docs/session-sets/security-dashboard-uat-remediation/`
> **Status:** Drafted for router review before implementation

---

## Human-approved decisions

The human approved the following Session 7 decisions after Gemini and Opus review:

- Skip the dual-idiom spike and proceed directly with `Idiom A`.
- Treat Default Masks as a deliberate partial adaptation of the master-details idiom rather than inventing a separate row-details editor.
- Prefer a small shared shell for the outer expansion/header/results/details behavior.
- Fix Session 7-caused regression failures inline during Session 7 rather than deferring them by default.
- Session 7 implementation may be handled mostly directly by the orchestrator, but the session must still end with independent final verification by another AI provider; verifier-requested fixes must either be implemented or raised to the human for a decision.

---

## Purpose

Turn Session 7 of `security-dashboard-uat-remediation` into a concrete implementation pass that:

- applies one uniform master-details idiom across the four affected dashboard surfaces
- fixes the `DisplayAlias` column clipping and font mismatch
- removes the dashboard-level arrow-key tab-strip behavior and the tests that depend on it
- updates existing Playwright coverage to navigate through the new UX
- adds new Playwright and bUnit assertions for the new interaction contract

This plan assumes **Idiom A** is the selected direction:

- results area wrapped in a `MudExpansionPanel`
- selection collapses the panel
- collapsed header shows a compact summary of the current selection
- filter changes re-expand the panel and clear the collapsed-summary state

The human explicitly approved this as a spec deviation from the original
"spike both idioms first" wording in Session 7.

---

## Decision Summary

## Chosen idiom: Idiom A

Reasons:

- It matches the current structure of the live components, which already render as "grid first, details below" in:
  - `src/Libs/Dabbler.Security.Blazor/Components/Admin/Role/RoleMasterDetailsTab.razor`
  - `src/Libs/Dabbler.Security.Blazor/Components/Admin/Organization/OrganizationMasterDetailsTab.razor`
  - `src/Libs/Dabbler.Security.Blazor/Components/Admin/Users/UsersAndRoleAssignmentTab.razor`
- It avoids introducing a second tab-navigation metaphor inside a page that already uses dashboard tabs.
- It works better with the Session 7 requirement to remove custom tab-strip keyboard behavior instead of adding more nested tab navigation.
- It keeps the biggest refactor pressure on layout/state composition, not on navigation semantics.

## One intentional adaptation

For **Organization Master Details**, the collapsed summary state must explicitly show `New organization` when a create flow is staged. That is the visible cue the UAT feedback asked for.

---

## Files In Scope

## Primary implementation files

- `src/Libs/Dabbler.Security.Blazor/Components/Admin/DabblerSecurityDashboard.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Role/RoleMasterDetailsTab.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Role/RoleMasterGrid.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Role/RoleDetailsSection.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Masks/DefaultMasksTab.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Masks/DefaultMasksGrid.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Organization/OrganizationMasterDetailsTab.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Organization/OrganizationMasterGrid.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Organization/OrganizationDetailsForm.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Users/UsersAndRoleAssignmentTab.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Users/UsersMasterGrid.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Users/UserRoleAssignmentDetails.razor`

## Likely new shared file

- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Shared/MasterDetailExpansionShell.razor`

Use a **small shared shell** rather than broad duplication. The shell should standardize only:

- results expanded/collapsed state
- custom collapsed-summary header content
- results content slot
- details content slot

Do not turn it into a generic page-generation abstraction.

## Test files expected to change

- `tests/AcmeCoffeeOnline.Playwright/SecurityDashboardUatCoverageTests.cs`
- `tests/AcmeCoffeeOnline.Playwright/SecurityDashboardFullSweepTests.cs`
- `tests/AcmeCoffeeOnline.Playwright/SecurityDashboardFiveTabSmokeTests.cs`
- `tests/AcmeCoffeeOnline.Playwright/UsersAndRoleAssignmentWorkflowTests.cs`
- `tests/Dabbler.Security.Blazor.Tests/Components/Admin/DabblerSecurityDashboardTests.cs`
- `tests/Dabbler.Security.Blazor.Tests/Components/Admin/Role/RoleMasterDetailsTabTests.cs`
- `tests/Dabbler.Security.Blazor.Tests/Components/Admin/Masks/DefaultMasksTabTests.cs`
- `tests/Dabbler.Security.Blazor.Tests/Components/Admin/Masks/DefaultMasksGridTests.cs`
- `tests/Dabbler.Security.Blazor.Tests/Components/Admin/Organization/OrganizationMasterGridTests.cs`

---

## Target Interaction Contract

## Shared shell behavior

Each of the four affected dashboard surfaces should follow the same outer pattern:

1. Top-level tab title and explanatory copy remain visible.
2. Results/filter/actions live inside a `MudExpansionPanel`.
3. The details surface lives below the panel.
4. Initial state is expanded.
5. Selecting a row collapses the results panel.
6. Collapsed header shows a compact read-only summary and a clear affordance to show results again.
7. Changing filters re-expands the results panel and clears the summary state.
8. Persist operations keep the details surface visible and refresh the summary text from saved data.

Use the `MudExpansionPanel` header-content surface intended for custom title rendering, such as `TitleContent` or the equivalent live MudBlazor API, for the collapsed summary fragment. The summary is read-only and should not contain editing controls.

## Shared state model

Each tab should own a small state model with these concepts:

- `IsResultsExpanded`
- `IsCreatingNew`
- `HasSelection`
- `SelectedSummary`

Expected transitions:

- Initial load: expanded, no summary
- Select row: collapsed, summary populated
- Filter change: expanded, summary cleared
- Create new: collapsed, `IsCreatingNew = true`, summary = `New ...`
- Save: remain collapsed, summary refreshed from persisted entity
- Delete/archive current selection: expanded, summary cleared

---

## Per-Surface Plan

## 1. Role Master Details

Files:

- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Role/RoleMasterDetailsTab.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Role/RoleMasterGrid.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Role/RoleDetailsSection.razor`

Implementation:

- Move the `RoleMasterGrid` into the expansion shell's results area.
- Keep `RoleDetailsSection` below the panel, as the details area.
- Collapse the panel when `SelectedRoleChanged` fires with a non-null row.
- Re-expand the panel when the filter changes or the selected row disappears after archive/delete.
- Summary text format:
  - baked-in role: `Role: SystemAdmin`
  - user-type role: `Role: BackofficeUser`
- Include the existing baked-in chip in the collapsed summary when applicable.

DisplayAlias work here:

- Fix the `Display alias` column in `RoleMasterGrid.razor` so the inline `MudTextField` does not clip text and does not visually read larger than neighboring cells.
- Approved fix:
  - constrain the input to normal body typography
  - use text wrapping
  - avoid solutions that rely on auto-growing width or horizontal scrolling for ordinary aliases

## 2. Organization Master Details

Files:

- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Organization/OrganizationMasterDetailsTab.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Organization/OrganizationMasterGrid.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Organization/OrganizationDetailsForm.razor`

Implementation:

- Wrap `OrganizationMasterGrid` in the results panel.
- Keep `OrganizationDetailsForm` as the details area.
- When `CreateRequested` fires:
  - collapse the results panel
  - clear row selection
  - set summary to `New organization`
- When an organization row is selected:
  - collapse the results panel
  - set summary to `Organization: <Name>`
- After save:
  - keep details visible
  - refresh the grid
  - update summary from the persisted organization name

Important behavior:

- The new-record cue must be obvious without scrolling the grid.
- If the form is dirty and a filter change or explicit `Show results` action would re-expand the panel and abandon the staged edit/create state, show a confirmation modal before discarding changes.
- If the form is clean, re-expand immediately and clear the summary state.

## 3. Users and Role Assignment

Files:

- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Users/UsersAndRoleAssignmentTab.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Users/UsersMasterGrid.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Users/UserRoleAssignmentDetails.razor`

Implementation:

- Wrap `UsersMasterGrid` in the results panel.
- Keep `UserRoleAssignmentDetails` below the panel.
- Collapse the panel when a user is selected.
- Summary text format:
  - `User: Alice Smith`
  - secondary muted text may include email if layout permits
- After role assignment add/remove:
  - refresh the grid
  - preserve the selected user
  - keep the panel collapsed in details mode

## 4. Default Masks

Files:

- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Masks/DefaultMasksTab.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Masks/DefaultMasksGrid.razor`

Implementation:

- Treat the tab's management surface as the "results" region:
  - filter panel
  - add form
  - count
  - grid
- Because this tab does not currently have a separate deep-details form, use a lighter adaptation:
  - wrap the whole management surface in the expansion panel
  - collapsed summary can show a compact status such as `Default Masks: 4 declared`
  - if Session 7 introduces row selection for this tab, then align it with the other tabs and use a row-specific summary instead

Guardrail:

- Do not invent a fake details editor just to force parity. Uniformity matters, but not at the cost of unnecessary UI surface.
- This is an approved partial adaptation and a deliberate exception to strict uniformity for Session 7.

---

## Shared Layout Rules

Apply these consistently to the results grids:

- Keep `Dense="true"`, `Bordered="true"`, `Striped="true"`.
- Put the actual grid rows inside a scrollable region with a default viewport around six visible rows.
- Keep filters and top-row actions outside the scroll region.
- Prefer a `max-height` band around `22rem` to `26rem` rather than a tiny hardcoded height.
- Let larger layouts show more rows where the surrounding tab content allows it.

This scrollable viewport treatment is additive to the current grid behavior. Do not rewrite pagination solely for Session 7 unless a specific target grid proves incompatible with the viewport approach; if that happens, document the change explicitly in the implementation notes and tests.

---

## Dashboard Keyboard Removal

File:

- `src/Libs/Dabbler.Security.Blazor/Components/Admin/DabblerSecurityDashboard.razor`

Implementation:

- Remove the `@onkeydown` handler from the dashboard tabstrip container.
- Delete `HandleTabKeyDown`.
- Delete `NextPanelIndex`.
- Let MudBlazor's built-in tab behavior handle focus and keyboard interaction.

Expected test cleanup:

- Remove bUnit tests that assert arrow-key/home/end dashboard-tab navigation.
- Remove Playwright assertions that press `ArrowRight` and `ArrowLeft` on the dashboard tabstrip.
- Keep or add assertions that normal `Tab` focus progression still works where the UAT cares about it.

---

## Test Plan

## Update existing Playwright coverage

Update existing security-dashboard Playwright tests so they navigate through the new master-details flow instead of assuming the grid and details are always simultaneously open.

Expected changes:

- `tests/AcmeCoffeeOnline.Playwright/SecurityDashboardUatCoverageTests.cs`
  - update all Session 2-6 flows that currently click a row and immediately expect the grid/details layout to stay fully expanded
- `tests/AcmeCoffeeOnline.Playwright/UsersAndRoleAssignmentWorkflowTests.cs`
  - update row-selection navigation through the collapsed-results pattern
- `tests/AcmeCoffeeOnline.Playwright/SecurityDashboardFullSweepTests.cs`
  - remove arrow-key assertions

## Add new Playwright assertions

Add focused browser assertions for:

- selecting a row collapses the results panel
- collapsed header summary reflects the selected item
- changing filters re-expands the results panel
- Organization `New organization` action collapses results and shows the `New organization` summary
- `DisplayAlias` content is readable and no longer clipped
- dashboard still supports ordinary tab navigation without the custom arrow-key handler

## Update bUnit coverage

Expected bUnit focus:

- state transitions in `RoleMasterDetailsTab`
- filter-change re-expand behavior where practical
- `DabblerSecurityDashboardTests` removing arrow-key expectations
- any compact summary rendering logic extracted into a shared shell

If summary rendering is extracted into a shared component, give that component its own small bUnit test file rather than overloading page-level tests.

---

## Recommended Implementation Order

1. Remove dashboard arrow-key logic and delete obsolete tests first.
2. Build the shared expansion-shell behavior.
3. Apply the shell to Role Master Details first.
4. Fix `DisplayAlias` in the same Role pass.
5. Apply the shell to Organization Master Details, including the `New organization` state.
6. Apply the shell to Users and Role Assignment.
7. Apply the lighter shell treatment to Default Masks.
8. Update existing Playwright tests to the new flow.
9. Add new Session 7-specific Playwright assertions.
10. Run bUnit, non-Playwright tests, then full Playwright regression.

Why this order:

- Role is the cleanest proving ground.
- Organization is the most UAT-sensitive variant because of the create-state cue.
- Users has persistence/selection refresh behavior that is easier to adapt once the shell pattern is stable.
- Default Masks is the least naturally "details-driven" surface, so it should adapt last after the pattern is proven.

---

## Risks And Guardrails

- Do not create tabs-inside-tabs as a substitute for the chosen expansion-panel approach.
- Do not invent a metadata-heavy wrapper that obscures each page's explicit composition.
- Do not regress the add/edit/archive refresh behavior that earlier sessions fixed.
- Do not silently discard unsaved organization edits if filter-change re-expansion can invalidate the active create/edit state.
- Do not preserve arrow-key tests under a renamed helper; the behavior is intentionally being removed.
- Do not make Default Masks more complex than the UAT need requires.
- Fix Session 7-caused regression failures inline during Session 7.
- If a regression appears to be a pre-existing unrelated defect rather than a Session 7 consequence, document it and raise it to the human for direction instead of silently widening scope.

---

## Progress key mapping

- `session-7/spike-both-master-detail-idioms`
  - satisfied by the human-approved decision to skip the spike and proceed directly with `Idiom A`
- `session-7/pick-winner-and-apply-uniformly`
  - apply `Idiom A` across Role, Organization, Users, and the approved partial-adaptation treatment for Default Masks
- `session-7/condensed-grid-with-six-row-viewport`
  - implement the shared scrollable results-region treatment
- `session-7/fix-displayalias-column-width-and-font`
  - apply wrapped body-typography styling to `DisplayAlias`
- `session-7/remove-tab-strip-arrow-key-handler-and-tests`
  - remove the dashboard keydown handler and obsolete tests
- `session-7/update-playwright-to-new-ux`
  - update prior coverage to drive the new master-details flow
- `session-7/add-new-ux-assertions`
  - add collapse, summary, `New organization`, and readability assertions
- `session-7/run-regression-tests`
  - run full Session 7 regression and repair Session 7-caused failures inline

---

## Completion Criteria For Session 7

- One expansion-panel-based master-details pattern is visible across Role, Organization, Users, and Default Masks.
- Role and Users collapse into a clear selected-summary state after row selection.
- Organization shows a visible `New organization` state without requiring the human to scroll the results grid.
- Results grids use a condensed, scrollable viewport with about six rows visible by default.
- `DisplayAlias` text is readable and visually aligned with sibling columns.
- The dashboard-level arrow-key handler is gone.
- Obsolete arrow-key tests are removed.
- Existing Playwright coverage passes after navigation updates.
- New Session 7 Playwright assertions cover the new UX contract.
