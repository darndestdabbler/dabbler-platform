# Security Dashboard Polish Spec

> **Purpose:** Execute the UX-only remediation items from the
> `security-dashboard-uat` human pass — cell/column alignment, switch vs.
> checkbox, CRUD icons, Control Permissions icons, inherited-vs-explicit
> visualization, unassigned-user filter, the access-denied flow, tab-strip
> keyboard navigation, and a concrete error-path demo.
> **Created:** 2026-04-17
> **Session Set:** `docs/session-sets/security-dashboard-polish/`
> **Prerequisite:** `security-dashboard-uat` complete, checklist JSON saved
> back to disk with human feedback.
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Context

The human UAT captured in
[`docs/session-sets/security-dashboard-uat/security-dashboard-uat-checklist.json`](../security-dashboard-uat/security-dashboard-uat-checklist.json)
flagged 13 items. This session set covers the 10 that are pure
UI/routing polish, grouped so each session ships a visible, demonstrable
change. The three remaining (behavioral) items are scoped to the separate
`security-dashboard-behavioral-fixes` session set because they require repro
against real data and real-DB integration tests.

The remediation plan, including decisions confirmed with the human, is in
[`.claude/plans/ok-i-finished-the-ancient-llama.md`](../../../.claude/plans/ok-i-finished-the-ancient-llama.md).

### Assets already staged by the human
These SVGs exist in `src/Libs/Dabbler.Security.Blazor/` and are untracked in
git (see `git status`). Each session below wires the ones it needs.

- `deny-allow-header.svg` — 45° Deny/Allow column header for permission
  matrices
- `create-header.svg`, `read-header.svg`, `update-header.svg`,
  `delete-header.svg` — 45° CRUD column headers for the Entity Access tab
- `enabled.svg`, `disabled.svg`, `hidden.svg` — inline icons for Control
  Permissions state

## Goals

1. Every UAT row with `Passes: false` in the UX group passes on a re-run.
2. The Allow/Deny invariant (mutually exclusive) is enforced in code and
   covered by a bUnit test, not just by UI convention.
3. Non-SystemAdmin users hitting a restricted page land on a centered
   "Access denied" card via the Blazor `<NotAuthorized>` fragment — not a
   404 re-executed through `UseStatusCodePagesWithReExecute`.
4. The User Assignments tab defaults to hiding users with zero role
   assignments; a toggle reveals them. No seed changes.
5. The Security Dashboard tab strip supports arrow-key navigation between
   tabs in addition to the default `Tab` focus.

## Architecture Context

### Components touched

- `src/Libs/Dabbler.Security/Authorization/Handlers/PageAuthorizationHandler.cs`
- `src/Apps/AcmeCoffeeOnline.Web/Components/Routes.razor`
- `src/Apps/AcmeCoffeeOnline.Web/Program.cs`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/DabblerSecurityDashboard.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/DabblerPagePermissionMatrix.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/DabblerEndpointPermissionMatrix.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/DabblerEntityPermissionMatrix.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/DabblerControlPermissionTab.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/DabblerControlPermissionEditor.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/DabblerUserRoleManager.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/DabblerRoleManager.razor`

### Decisions confirmed with the human (do not re-litigate)

- **Access-denied UX:** Blazor `<NotAuthorized>` fragment rendering a
  centered access-denied card. Copy: "You don't have access to this page.
  If you need access, please request it from your administrator."
- **Endpoint Access:** same single `MudSwitch` (Success/Error) +
  `deny-allow-header.svg` pattern as Page Access. Gains an explicit Deny
  state.
- **Unassigned users:** client-side filter toggle in the User Assignments
  tab, default off. Seed untouched.
- **Inherited vs. explicit:** italic/dimmed inherited cells, hover
  tooltip `inherited from <parent>`, one-line legend near the matrix top.

---

## Session Set Configuration

```yaml
requiresUAT: false
requiresE2E: false
```

## Sessions
### Session 1 of 8: Access-denied flow

**Steps:**
1. Read `docs/planning/project-guidance.md` and
   `docs/planning/lessons-learned.md`.
2. Change `PageAuthorizationHandler.HandleRequirementAsync` so that a
   denial calls `context.Fail()` (today it returns silently, which lets
   the status-code middleware treat the denial as a 404).
3. Narrow `UseStatusCodePagesWithReExecute("/not-found")` so it no longer
   swallows 401/403 responses — scope it to genuine 404 paths only, or
   exclude status codes 401 and 403.
4. Update `Routes.razor` `<NotAuthorized>` fragment so it renders a
   centered MudCard with the approved copy and a "Back to dashboard"
   button.
5. Add a Playwright test: sign in as a BackofficeUser, navigate to
   `/admin/security`, assert the access-denied card is visible and the
   URL stays on `/admin/security` (no redirect to `/not-found`).
6. bUnit test on the `NotAuthorized` render fragment to assert copy and
   button presence.
7. Build and test. Log via `log.log_step()`.

**Creates:** Playwright test, bUnit test, updated `NotAuthorized` fragment
**Touches:** `PageAuthorizationHandler.cs`, `Routes.razor`, `Program.cs`
**Ends with:** `/admin/security` as non-admin shows the centered access-
denied card; no route change, no 404.
**Progress keys:** `session-1/handler-fail`, `session-1/status-codes`,
`session-1/not-authorized-fragment`, `session-1/playwright`,
`session-1/bunit`, `session-1/build-and-test`

---

### Session 2 of 8: Page Access matrix — switch + layout

**Steps:**
1. Read planning docs.
2. In `DabblerPagePermissionMatrix.razor`:
   - Replace the two `MudCheckBox` controls with one `MudSwitch` bound to
     a three-state model (Allow / Deny / Unset) or to a single
     allow/deny enum; enforce mutual exclusion at the model level so both
     states cannot co-exist.
   - Use `deny-allow-header.svg` as the column header (Blazor static
     asset).
   - Widen cards (~33%) and center the switch column so no horizontal
     scroll is needed at a normal desktop width.
3. Extract the switch + header combo into a shared razor component if
   the refactor is cleaner that way — Sessions 3 and 4 will reuse it.
4. bUnit test: flipping the switch from Allow to Deny drops Allow from
   the underlying model (invariant test).
5. Build and test. Log via `log.log_step()`.

**Creates:** Shared switch component (if extracted); bUnit invariant test
**Touches:** `DabblerPagePermissionMatrix.razor`, wire `deny-allow-header.svg`
**Ends with:** Page Access matrix renders with a single switch per cell,
no horizontal scroll, invariant covered by test.
**Progress keys:** `session-2/switch-control`, `session-2/header-svg`,
`session-2/widths`, `session-2/bunit-invariant`, `session-2/build-and-test`

---

### Session 3 of 8: Endpoint Access matrix — same pattern

**Steps:**
1. Read planning docs.
2. Apply the switch component and `deny-allow-header.svg` to
   `DabblerEndpointPermissionMatrix.razor`. Gain explicit Deny (today
   only Allow is modelled).
3. Confirm the backing model/service supports an explicit-Deny state for
   endpoints; extend it if not. Security-config JSON schema may need a
   `Denied` array alongside `Allowed`.
4. Centre the column and remove horizontal scroll.
5. bUnit test for the invariant, parallel to session 2's.
6. Build and test. Log via `log.log_step()`.

**Creates:** bUnit test; possibly `SecurityConfiguration` schema extension
**Touches:** `DabblerEndpointPermissionMatrix.razor`,
`SecurityConfiguration.cs` and related persistence if Deny is new
**Ends with:** Endpoint Access matrix matches Page Access behavior and
visual pattern; Deny is persisted.
**Progress keys:** `session-3/switch-reuse`, `session-3/deny-model`,
`session-3/persistence`, `session-3/bunit-invariant`,
`session-3/build-and-test`

---

### Session 4 of 8: Entity Access matrix — CRUD icons, no scroll

**Steps:**
1. Read planning docs.
2. In `DabblerEntityPermissionMatrix.razor`, replace the literal
   "Create" / "Read" / "Update" / "Delete" column headers with the four
   45° SVGs. Keep the plain-text label as a `title` / `aria-label` for
   screen readers.
3. Adjust column widths so all four CRUD columns fit inside the card
   without horizontal scroll at a normal desktop width.
4. Centre checkboxes in each cell (current style is left-aligned; the
   UAT flagged this on other matrices too).
5. bUnit test: each column header has an accessible name.
6. Build and test. Log via `log.log_step()`.

**Creates:** bUnit accessibility test
**Touches:** `DabblerEntityPermissionMatrix.razor`, wire the four CRUD SVGs
**Ends with:** Entity Access shows all four CRUD columns inside the card,
centered, with the 45° icon headers and text still available to screen
readers.
**Progress keys:** `session-4/crud-headers`, `session-4/layout`,
`session-4/alignment`, `session-4/bunit-a11y`, `session-4/build-and-test`

---

### Session 5 of 8: Control Permissions — state icons

**Steps:**
1. Read planning docs.
2. In `DabblerControlPermissionTab.razor` and
   `DabblerControlPermissionEditor.razor`, render `enabled.svg`,
   `disabled.svg`, and `hidden.svg` alongside the Enabled / Disabled /
   Hidden labels inside the `MudSelect` items.
3. Keep text labels — icons augment, not replace.
4. bUnit test: the rendered option template contains both the icon and
   the text for each state.
5. Build and test. Log via `log.log_step()`.

**Creates:** bUnit test
**Touches:** `DabblerControlPermissionTab.razor`,
`DabblerControlPermissionEditor.razor`, wire three state SVGs
**Ends with:** Control Permissions dropdown shows icon + text for each
state.
**Progress keys:** `session-5/icon-markup`, `session-5/bunit`,
`session-5/build-and-test`

---

### Session 6 of 8: Inherited vs. explicit visualization

**Steps:**
1. Read planning docs.
2. In `DabblerPagePermissionMatrix.razor`, identify when a cell's value is
   inherited from a parent role (existing `SecurityConfigurationManager`
   resolution logic already knows this — reuse it, do not reimplement).
3. Render inherited cells with `font-style: italic` and `opacity: 0.6`;
   attach a `MudTooltip` reading `inherited from <ParentRoleName>`.
4. Add a one-line legend card above the matrix: `Solid = explicit,
   italic/dim = inherited`.
5. Apply the same treatment to Endpoint Access and Entity Access so the
   convention is consistent.
6. bUnit test: a cell with an inherited value has the `inherited` css
   class and tooltip text pointing at the parent role.
7. Build and test. Log via `log.log_step()`.

**Creates:** bUnit test, small shared legend component
**Touches:** `DabblerPagePermissionMatrix.razor`,
`DabblerEndpointPermissionMatrix.razor`,
`DabblerEntityPermissionMatrix.razor`, any shared
`SecurityConfigurationResolver` helper
**Ends with:** Inherited and explicit values are at-a-glance
distinguishable on all three matrices; legend explains the convention.
**Progress keys:** `session-6/resolver`, `session-6/styling`,
`session-6/legend`, `session-6/bunit`, `session-6/build-and-test`

---

### Session 7 of 8: User Assignments — unassigned filter toggle

**Steps:**
1. Read planning docs.
2. In `DabblerUserRoleManager.razor`, add a `MudSwitch` labeled "Show
   unassigned users" above the table. Default `false`.
3. Filter the rendered user list client-side to `row.Roles.Count > 0`
   when the switch is off.
4. Keep the full `IUserListProvider` result in state so toggling on is
   instant (no re-fetch).
5. bUnit test: switch off → unassigned users hidden; switch on → full
   list; seed is unchanged (no call to `EfUserListProvider`).
6. Build and test. Log via `log.log_step()`.

**Creates:** bUnit test
**Touches:** `DabblerUserRoleManager.razor`
**Ends with:** Default User Assignments view is clean; the toggle exposes
the full roster without touching the seed.
**Progress keys:** `session-7/toggle`, `session-7/client-filter`,
`session-7/bunit`, `session-7/build-and-test`

---

### Session 8 of 8: Tab-strip keyboard nav + error-path demo

**Steps:**
1. Read planning docs.
2. In `DabblerSecurityDashboard.razor`, add arrow-key handling to the
   tab strip. Left/Right (and Home/End) move focus across tabs. The
   current MudTabs default only supports Tab-into-focus.
3. Pick one concrete error path so the "not really possible to check
   very easily" UAT item has a demo: on `DabblerRoleManager.razor`, when
   the reviewer adds a duplicate role name, surface a MudAlert
   inline in the edit dialog reading "A role with this name already
   exists."
4. bUnit test for the keyboard handler (arrow keys move active tab).
5. bUnit test for the duplicate-role-name validation path.
6. Build and test. Log via `log.log_step()`.

**Creates:** Two bUnit tests
**Touches:** `DabblerSecurityDashboard.razor`, `DabblerRoleManager.razor`
**Ends with:** Arrow-key navigation across the seven tabs; a visible,
testable error path on Roles.
**Progress keys:** `session-8/keyboard`, `session-8/error-demo`,
`session-8/bunit-keyboard`, `session-8/bunit-validation`,
`session-8/build-and-test`

---

## Human Re-run After Session 8

After session 8 completes and verification passes, hand back to the human
for a targeted re-run of these rows in
`security-dashboard-uat-checklist.json`:

- Shell and Access — Role-Based Access (both rows)
- Tab Strip — Layout and Labels (keyboard)
- Page Access Tab — Matrix Layout, Matrix Editing, Inherited vs Explicit
- Entity Access Tab — Entity List and CRUD Matrix
- Endpoint Access Tab — Endpoint List
- Control Permissions Tab — State Semantics
- User Assignments Tab — User List
- Cross-Cutting Quality — Feedback and Errors, Keyboard and Accessibility

If any remain `Passes: false`, capture the feedback in a follow-up spec —
do not reopen the original UAT JSON.

## What This Set Preserves

| Existing Asset | Status |
|---|---|
| All existing Playwright tests | Must continue passing |
| All non-Playwright tests | Must continue passing |
| `SecurityConfiguration` JSON schema (outside session 3's Deny extension) | Preserved |
| Role hierarchy and seed data | Unchanged |
| `DabblerPageAuthorize` attribute and usage | Behavior preserved; failure path changes only |

## Risk Notes

- **Endpoint Deny is new.** Session 3 extends the security-config schema
  to add explicit Deny to endpoints. Any code that reads endpoint access
  with a hard-coded "Allowed list only" assumption must be updated and
  retested.
- **`UseStatusCodePagesWithReExecute` scope change.** Session 1's narrowing
  could expose previously-masked 4xx cases. Monitor integration tests for
  new failures and treat them as intentional surfacings, not regressions.
- **SVG static-asset wiring.** The library project may need
  `<StaticWebAsset>` configuration. If the SVGs don't load at runtime,
  check the library's csproj asset pipeline before assuming the Blazor
  reference is wrong.
