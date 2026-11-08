# Security Dashboard Polish — Follow-up Spec *(SUPERSEDED — NEVER RUN)*

> **⚠ ARCHIVED 2026-04-19.** This spec was written on 2026-04-18 and
> approved, but never executed. The human subsequently proposed a
> fundamentally simpler authorization architecture (Default-Masks +
> Mask-Exemptions, binary column restrictions, baked-in roles, no
> role inheritance, no grant-based permission matrices). That work
> is now tracked by the roadmap in
> `C:\Users\denmi\.claude\plans\ok-i-did-some-abstract-blossom.md`
> and is being executed as the session-set quartet
> `rearchitect-role-administration-foundations` → `-dashboard` →
> `sensitive-field-masking-audit` → `transactional-system-columns`.
>
> Preserved for historical context — the three-state Allow / Soft
> Deny / Force Deny model in this spec represents a considered
> direction that was rejected during design review. Do not execute.
> Do not reference from new work.

---

> **Purpose:** Simplify the authorization model (permissive union of a user's
> roles, no role-hierarchy inheritance, three states per Page / Endpoint:
> Allow / Soft Deny / Force Deny) and roll the proven mockup design
> (`/dev/permission-mockup`) into the real Security Dashboard tabs.
> **Created:** 2026-04-18
> **Session Set:** `docs/session-sets/security-dashboard-polish-followup/`
> **Prerequisite:** `security-dashboard-polish` complete (commit `afa7da3`).
> Human UAT re-run of the polish set is **not** blocking — the follow-up
> supersedes several of its rows (see "Human Re-run" at the bottom).
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Context

After `security-dashboard-polish` landed, the human and orchestrator
iterated on a throwaway mockup at
[`src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Dev/PermissionMockup.razor`](../../../src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Dev/PermissionMockup.razor)
to validate three treatments:

1. **Page Access / Endpoint Access:** single three-icon toggle per cell
   (`allow.svg` → `soft-deny.svg` → `force-deny.svg`) instead of the
   `DenyAllowSwitch` tri-state `MudToggleGroup`.
2. **Entity Access:** compact CRUD grid (28px columns, 48px header SVGs,
   20px checkbox glyph, preserved Create/Read/Update/Delete color coding).
3. **Control Permissions:** dual cross-wired `MudSwitch` (Visible blue +
   Enabled green) instead of the `MudSelect` dropdown.

The mockup also drove a conversation about the underlying authorization
model. The human chose a deliberately simpler model (captured as a durable
Principle in Session 1) to avoid the complexity of role-hierarchy
inheritance and deny-wins semantics the current code implements.

### Assets already staged by the human

These SVGs exist in `src/Libs/Dabbler.Security.Blazor/wwwroot/`:

- `allow.svg` — green check glyph (~24mm wide).
- `soft-deny.svg` — black `!` glyph (**must be re-saved with black fills
  before Session 2 starts** — currently red in the committed asset; the
  human confirmed black is more intuitive so red does not become a
  "soft-red vs bright-red" guessing game with `force-deny.svg`).
- `force-deny.svg` — red `X` with embedded bold `!`, deliberately wider
  (~33mm) than the other two.
- `create-header.svg`, `read-header.svg`, `update-header.svg`,
  `delete-header.svg` — vertical-text column headers (updated by the
  human after Session 4 of the prior set).

## Goals

1. A user's effective permissions are the permissive union of their
   directly assigned roles. No role-hierarchy traversal.
2. Page and Endpoint permissions have three states per (role, resource):
   Allow, Soft Deny, Force Deny. Force Deny beats Allow; Soft Deny is
   documentary (same runtime outcome as "not listed").
3. Entity permissions are pure CRUD booleans per (role, entity). No
   entity-level three-state layer. If a role needs all-or-nothing entity
   access, uncheck all four CRUD boxes (or check all four). Execute-style
   operations extend the acronym (CRUDE, etc.) — never introduce a
   separate entity-level gate.
4. Control Permissions render as two cross-wired switches (Visible +
   Enabled). The `ControlState` enum (Hidden / Disabled / Enabled) stays
   as the persisted model so the JSON schema does not change.
5. The visual design of the mockup is carried forward faithfully:
   20px SVG heights in cells and legends, 48px CRUD header heights, 28px
   CRUD column widths, color-coded CRUD checkbox ticks.

## Architecture Context

### Components touched

- `src/Libs/Dabbler.Security/Configuration/PagePermission.cs`
- `src/Libs/Dabbler.Security/Configuration/EndpointPermission.cs`
- `src/Libs/Dabbler.Security/Authorization/Handlers/PageAuthorizationHandler.cs`
- `src/Libs/Dabbler.Security/Authorization/Handlers/EndpointAuthorizationHandler.cs`
- `src/Libs/Dabbler.Security/Persistence/EfCoreSecurityStore.cs`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/DabblerPagePermissionMatrix.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/DabblerEndpointPermissionMatrix.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/DabblerEntityPermissionMatrix.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/DabblerControlPermissionTab.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/DabblerControlPermissionEditor.razor`
- New: `src/Libs/Dabbler.Security.Blazor/Components/Admin/DabblerPermissionTriState.razor`
  (the three-icon toggle; reused by Page and Endpoint matrices).
- Retained but unused at runtime after Session 1:
  `src/Libs/Dabbler.Security/Authorization/Helpers/RoleInheritanceHelper.cs`
  (kept to avoid breaking imports in tests and downstream libraries; a
  future session set can delete it once the call sites are fully audited).

### Decisions confirmed with the human (do not re-litigate)

- **Authorization philosophy.** Permissive union across a user's roles.
  Force Deny is the only override of Allow. Soft Deny is documentary
  ("we considered this role and chose not to grant"); it has no runtime
  distinction from "not listed." No role-hierarchy inheritance.
- **Entity Access.** CRUD booleans per (role, entity) are the only
  layer. No entity-level state on top of CRUD.
- **Control Permissions.** Dual cross-wired `MudSwitch`:
  - Visible → off forces Enabled → off.
  - Enabled → on while Visible is off flips Visible → on.
  - Invariant: `Hidden && Enabled` cannot hold.
- **Existing `DeniedRoles` data maps to `ForceDeniedRoles`.** Current
  handlers treat `DeniedRoles` as deny-wins, which matches the new
  `ForceDeniedRoles` semantics. The migration preserves current behavior
  for every existing config. `SoftDeniedRoles` starts empty.

---

## Session Plan

### Session 1 of 5: Authorization model simplification + Principle capture

**Steps:**
1. Read planning docs.
2. Add a new Principle to
   [`docs/planning/project-guidance.md`](../../../docs/planning/project-guidance.md)
   under **Security And Organization Model**:
   > Authorization resolution is the permissive union of a user's
   > directly-assigned roles. No role-hierarchy inheritance is applied
   > at resolution time. Each (role, Page) and (role, Endpoint)
   > permission has three states: Allow, Soft Deny (documentary, same
   > runtime as "not listed"), and Force Deny (overrides any Allow from
   > any other role). Entity permissions are pure CRUD booleans per
   > (role, entity) — no separate entity-level gate.
3. Extend `PagePermission` and `EndpointPermission` with a new
   `ForceDeniedRoles: List<string>` property. Rename the existing
   `DeniedRoles` to `SoftDeniedRoles`. Update every reference (models,
   JSON serialization attributes, matrix components, EF store, migrations,
   seed data, tests).
4. Update `PageAuthorizationHandler` and `EndpointAuthorizationHandler`:
   - Resolution order: if any user role ∈ `ForceDeniedRoles` → fail.
     Else if any user role ∈ `AllowedRoles` → succeed. Else → fail.
   - `SoftDeniedRoles` is not consulted at runtime.
   - Remove the `RoleInheritanceHelper.ExpandRoles` call. Use only the
     directly-assigned roles from `_roleSwitcher.EffectiveRoles(user)`.
5. EF migration: rename `Denied → ForceDenied` column on the existing
   permission tables (or the equivalent shape), add no new column if
   the store already modeled one Boolean per grant. Ensure existing
   rows' current "deny-wins" behavior is preserved by mapping any
   previously-denied grant to `ForceDenied = true`.
6. Update test seeds. Every config in `Dabbler.Security.Tests` and
   `Dabbler.Security.Blazor.Tests` that used `DeniedRoles` must decide
   between `SoftDeniedRoles` (to preserve the old "documented deny"
   intent when the test was not about runtime denial) and
   `ForceDeniedRoles` (when the test IS about runtime denial).
7. New integration-style tests covering:
   - Permissive union: two roles, one Allow → granted.
   - Force Deny beats Allow across roles.
   - Soft Deny is runtime no-op.
   - Inheritance is not applied (a role whose parent has Allow does
     not gain access through the parent).
8. Build and test. Log via `log.log_step()`.

**Creates:** `ForceDeniedRoles` on two model types; new integration
tests; Principle addition in `project-guidance.md`.
**Touches:** `PagePermission.cs`, `EndpointPermission.cs`,
`PageAuthorizationHandler.cs`, `EndpointAuthorizationHandler.cs`,
`EfCoreSecurityStore.cs`, related migrations, test seeds.
**Ends with:** Existing permission configs behave identically to today
(all existing `DeniedRoles` entries become `ForceDeniedRoles`); new
`SoftDeniedRoles` plumbing exists but is empty in seed data; no handler
consults role inheritance.
**Progress keys:** `session-1/principle`, `session-1/model-split`,
`session-1/handler-rewrite`, `session-1/inheritance-removed`,
`session-1/migration`, `session-1/integration-tests`,
`session-1/build-and-test`

---

### Session 2 of 5: Page Access matrix → three-icon toggle

**Steps:**
1. Read planning docs.
2. Confirm the human has re-saved `soft-deny.svg` with black fills.
   If not, stop and tell the human; do not apply a CSS filter as a
   fallback.
3. Create `DabblerPermissionTriState.razor` (new shared component). API:
   `Value: TriState` (`Allow` / `SoftDeny` / `ForceDeny`), `ValueChanged`,
   `Disabled`, tooltip, `data-testid` per cell. Cycle order on click:
   `Allow → SoftDeny → ForceDeny → Allow`. Renders a `MudButton` wrapping
   an `<img>` sourced from `_content/Dabbler.Security.Blazor/{allow|soft-deny|force-deny}.svg`
   at `height:20px`. Legend helper is exposed as a separate small component
   or static markup helper for matrix reuse.
4. Replace `DenyAllowSwitch` usage in
   `DabblerPagePermissionMatrix.razor` with `DabblerPermissionTriState`.
   The row-per-(page, role) backing model now writes to whichever of the
   three lists the state selects, and removes the role from the other
   two.
5. Add a legend card above the matrix (same layout as the mockup):
   allow | soft-deny | force-deny icons + captions explaining runtime
   meaning.
6. bUnit tests:
   - Each of three states renders the right SVG `src` and tooltip.
   - Click cycles Allow → Soft → Force → Allow.
   - Committing a cell updates the backing config's three lists
     correctly (role is present in exactly one list at any time).
   - The invariant test (role never appears in two lists simultaneously).
7. Delete `DenyAllowSwitch.razor` if and only if no other component
   uses it after Session 3; otherwise keep for Session 3. (Session 3's
   first step verifies and removes it.)
8. Build and test. Log via `log.log_step()`.

**Creates:** `DabblerPermissionTriState.razor`; bUnit tests.
**Touches:** `DabblerPagePermissionMatrix.razor`.
**Ends with:** Page Access tab renders the three-icon cell toggle with
20px icons, legend, and correct three-list persistence.
**Progress keys:** `session-2/svg-check`, `session-2/tri-state-component`,
`session-2/matrix-wire`, `session-2/legend`, `session-2/bunit`,
`session-2/build-and-test`

---

### Session 3 of 5: Endpoint Access matrix → same pattern

**Steps:**
1. Read planning docs.
2. Replace `DenyAllowSwitch` usage in
   `DabblerEndpointPermissionMatrix.razor` with
   `DabblerPermissionTriState`.
3. Reuse the legend helper from Session 2.
4. Delete `DenyAllowSwitch.razor` and its bUnit tests now that no
   component references it (grep-verify in the last step).
5. bUnit: parallel coverage to Session 2's invariant test plus a
   keyboard-accessibility check (Space/Enter on the toggle advances
   state, matching MudButton default).
6. Build and test. Log via `log.log_step()`.

**Creates:** bUnit tests.
**Touches:** `DabblerEndpointPermissionMatrix.razor`;
deletes `DenyAllowSwitch.razor` and its tests.
**Ends with:** Endpoint Access tab matches Page Access behavior and
visual pattern; `DenyAllowSwitch` is gone.
**Progress keys:** `session-3/matrix-wire`, `session-3/delete-denyallow`,
`session-3/bunit`, `session-3/build-and-test`

---

### Session 4 of 5: Entity Access compact layout

**Steps:**
1. Read planning docs.
2. Apply the mockup's compact layout to
   `DabblerEntityPermissionMatrix.razor`:
   - `<colgroup>` with 28px per CRUD column.
   - 48px CRUD header SVG height.
   - Checkbox glyph sized to 20px via a scoped `<style>` block (see
     mockup `.crud-tight .crud-cb` rules).
   - Preserved color coding: Create=Success, Read=Info,
     Update=Warning, Delete=Error (already present — verify unchanged).
   - Zero-padding on CRUD cells; `!important` overrides on
     `.mud-icon-button` padding/min-width.
3. No data-model change. Entity Access remains pure CRUD.
4. bUnit:
   - Renders the four CRUD header SVGs via `_content/...` src.
   - Checkbox ticks clickable at their declared 20px size.
   - Each CRUD column's `<col>` width is 28px.
   - Color coding verifies via the `Color` parameter on the checkbox.
5. Build and test. Log via `log.log_step()`.

**Creates:** bUnit layout test.
**Touches:** `DabblerEntityPermissionMatrix.razor`.
**Ends with:** Entity Access tab visually matches the mockup at a
normal desktop width with no horizontal scroll and color-coded CRUD
ticks.
**Progress keys:** `session-4/colgroup`, `session-4/checkbox-size`,
`session-4/color-coding`, `session-4/bunit`, `session-4/build-and-test`

---

### Session 5 of 5: Control Permissions dual switch + UAT checklist + change log

**Steps:**
1. Read planning docs.
2. Replace the `MudSelect` dropdowns in
   `DabblerControlPermissionTab.razor` and
   `DabblerControlPermissionEditor.razor` with the dual
   cross-wired `MudSwitch` pair (Visible `Color.Primary`,
   Enabled `Color.Success`). The persisted `ControlState` enum
   (Hidden / Disabled / Enabled) stays unchanged; the UI collapses the
   two booleans back to the enum on save.
3. Implement the cross-wiring handlers:
   - `OnVisibleChanged(v)`: set Visible; if `v == false` set Enabled = false.
   - `OnEnabledChanged(v)`: set Enabled; if `v == true && !Visible` set Visible = true.
4. Remove the `enabled.svg`/`disabled.svg`/`hidden.svg` icons from the
   option templates. They can remain on disk (they are still in
   `wwwroot/`) but are no longer referenced by any razor file. Do not
   delete the files in this session — leave the cleanup to a future
   repository-asset session.
5. bUnit:
   - Toggling Visible off forces Enabled off.
   - Toggling Enabled on while Visible is off flips Visible on.
   - Save collapses (Visible, Enabled) back to the correct
     `ControlState` enum (verified against three fixed cases).
   - Invariant: `!Visible && Enabled` never persists.
6. Write the UAT checklist
   `security-dashboard-polish-followup-uat-checklist.json` for the new
   UX (Page Access three-icon cycle, Endpoint same, Entity compact
   layout, Control dual switch, and an end-to-end Force-Deny-beats-Allow
   integration check the human can confirm by switching roles).
7. Build and test. Log via `log.log_step()`.

**Creates:** bUnit tests; UAT checklist JSON matching the editor
contract.
**Touches:** `DabblerControlPermissionTab.razor`,
`DabblerControlPermissionEditor.razor`.
**Ends with:** Control Permissions tab renders two cross-wired switches;
UAT checklist is queued for human review; session-set change log is
generated in Step 8 and the Step 9 reorganization review runs after
notify per the current workflow.
**Progress keys:** `session-5/switch-wire`, `session-5/cross-wiring`,
`session-5/remove-select-icons`, `session-5/bunit`,
`session-5/uat-checklist`, `session-5/build-and-test`

---

## Human Re-run After Session 5

The UAT checklist from the prior `security-dashboard-polish` set
(`docs/session-sets/security-dashboard-uat/security-dashboard-uat-checklist.json`)
becomes partially superseded by this set's work. After Session 5,
hand back to the human for two checklists:

1. **This set's** new `security-dashboard-polish-followup-uat-checklist.json`.
2. The polish set's original rows that were **not** changed by this
   set (Roles tab, Row Filters tab, User Assignments tab, tab-strip
   keyboard nav, access-denied flow). The human can re-run those
   individually in the original file.

## What This Set Preserves

| Existing Asset | Status |
|---|---|
| All Playwright tests | Must continue passing |
| All non-Playwright tests | Must continue passing |
| `SecurityConfiguration` JSON shape (outside Session 1's `DeniedRoles → SoftDeniedRoles` rename and the new `ForceDeniedRoles`) | Preserved |
| Role definitions, role hierarchy data, seed roles | Unchanged on disk. Hierarchy is ignored by handlers. |
| `DabblerPageAuthorize` attribute and usage | Behavior preserved where the existing `DeniedRoles` was deny-wins; that semantics moves to `ForceDeniedRoles`. |
| Entity CRUD model and persistence | Completely unchanged. |
| `ControlState` enum | Unchanged. UI just renders it differently. |

## Risk Notes

- **`SoftDeniedRoles` is a no-op at runtime.** Any downstream code that
  currently reads `DeniedRoles` and expects deny semantics must be
  updated to read `ForceDeniedRoles` instead. A rename-only refactor
  without handler review would silently unlock access. Session 1
  guards this with grep and a dedicated integration test.
- **Removing role-inheritance resolution is a behavior change for
  anyone relying on the hierarchy.** The existing role seeds still
  declare `InheritsFrom`, but handlers no longer expand. Documented
  explicitly in the new Principle and in this set's change log.
- **`DenyAllowSwitch.razor` deletion lands in Session 3.** If any new
  component adopted it between Session 2 and Session 3, the grep-
  verify step will catch it.
- **Soft-deny SVG color is a human asset task.** Session 2 refuses to
  proceed if `soft-deny.svg` is still red. No CSS-filter fallback —
  the asset must be correct.
- **MUD0002 analyzer warning on `DenyAllowSwitch.razor`** disappears
  when Session 3 deletes the file. Track that as a positive side
  effect, not as work.
