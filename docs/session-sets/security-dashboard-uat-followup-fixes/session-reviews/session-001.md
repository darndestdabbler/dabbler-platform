# Verification Round 1

# End-of-Session Verification

## Finding #2 (Blocker): Admin drawer hidden for baked-in `User` role

### Fix correctness: ✅ Correct and sufficient
- New persona `jdoe-bakedin-user` in `QuickSignInDialog.razor` passes `RolesOverride = "User"` through the existing `signIn()` JS helper.
- `dev-quick-signin.js` conditionally appends `&roles=…` only when the parameter is truthy — existing null-override personas are unaffected.
- The `DevPersona` record cleanly carries the optional override with no change to the record shape consumed by other entries.

### Regression check: ✅ No adjacent UX regressions
All six other persona entries pass `null` for `RolesOverride`; the JS `if (roles)` guard ensures no query-string pollution on their sign-in flow.

### Playwright coverage: ✅ Matches UAT path
`SecurityNav_BakedInUserPersona_NavVisible_RoleDocOnlyAsync` signs in as `jdoe@acmecoffee.com` with `roles: "User"`, asserts the Admin drawer button is visible, navigates to `/admin/security`, asserts Role Documentation tab visible, and asserts all four privileged tabs (Role Master Details, Default Masks, Organization Master Details, Users and Role Assignment) have count 0.

### Auth bypass risk: ✅ Constrained
- `X-Test-Auth-Secret` header is still required on every `/test-auth/seeded-session` call (line 3 of `dev-quick-signin.js`).
- The `roles` parameter is a query-string addition to an already-gated endpoint. The `AllowedRoles` allow-list in `appsettings.Development.json` constrains acceptable values server-side.
- The endpoint is only registered in the Development environment.

---

## Finding #11 (Blocker): Create-organization did not refresh the grid

### Fix correctness: ✅ Correct and sufficient
`OnPersistedAsync` in `OrganizationMasterDetailsTab.razor` now:
1. Sets `_isResultsExpanded = true` — forces the shell open.
2. Clears `_summaryText = null` — removes the collapsed summary so the expanded header renders.
3. Calls `_grid.RefreshAsync(organizationId)` — queries a fresh `IDbContextFactory`-resolved `DbContext` and pre-selects the new row.
4. Calls `InvokeAsync(StateHasChanged)` — ensures the Blazor circuit picks up all state changes.

This directly addresses the root cause: the prior flow left the shell collapsed after save, and the reviewer never saw the refreshed grid.

### Collapse-on-create compatibility: ✅ No conflict
- `OnCreateRequestedAsync` (sets `_isResultsExpanded = false`, `_summaryText = "New organization"`) fires when the "New organization" button is clicked — **before** save.
- `OnPersistedAsync` fires **after** save completes.
- These are temporally disjoint. The existing test `OrganizationMasterDetails_NewOrganization_CollapsesShellWithNewSummaryAsync` exercises only the button-click phase and never reaches `OnPersistedAsync`, so it is unaffected.
- Confirmed by 661/0 unit-test results.

### Playwright coverage: ✅ Matches UAT path
`OrgMasterDetails_CreateOrganization_AutoReexpandsShellAsync`:
- Creates an org with a unique marker key/name.
- Asserts collapsed summary reads "New organization" (pre-save state).
- Clicks save, then asserts the grid becomes visible **without any shell-header click**.
- Scopes the new-row text assertions to `organization-master-grid-container` to avoid false positives from snackbars or summary blurbs.

The existing `OrgMasterDetails_CreateOrganization_GridRefreshesInCircuitAsync` also benefits from the fix and passes.

---

## Finding #14 (Minor): Currency symbol `¤` in Docker

### Fix correctness: ✅ Correct and sufficient

**Api (`AcmeCoffeeOnline.Api/Program.cs`):**
```csharp
var apiCulture = CultureInfo.GetCultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = apiCulture;
CultureInfo.DefaultThreadCurrentUICulture = apiCulture;
```
Set after `builder.Build()` but before any middleware executes or `app.Run()` is called. All Kestrel worker threads spawned after this point inherit en-US.

**Web (`AcmeCoffeeOnline.Web/Program.cs`):**
Same `DefaultThreadCurrentCulture`/`DefaultThreadCurrentUICulture` pin, plus `UseRequestLocalization` (per scope description — in the truncated portion). The static properties ensure that Blazor Server circuits — which run on thread-pool threads — see en-US for `ToString("C2")` and all `CultureInfo.CurrentCulture`-dependent formatters.

### Timing: ✅ Early enough
The culture pin executes in the application startup path before `app.Run()`. Blazor Server circuits are created after the server starts accepting connections, so every circuit inherits the pinned culture. `UseRequestLocalization` provides a per-request backstop.

### Playwright coverage: ✅ Matches UAT path
`Products_BackofficeUser_UnitPriceColumn_ShowsDollarSymbolAsync`:
- Signs in as BackofficeUser, navigates to `/products`.
- Asserts "Unit Price" column header visible in `.mud-table-head`.
- Asserts first `td[data-label='Unit Price']` cell contains `$`.
- Asserts same cell does **not** contain `¤`.

This runs in Docker (Playwright suite), which is the exact environment where POSIX locale caused the original regression.

---

## Finding #49 (Minor): Two clicks to collapse shell

### Fix correctness: ✅ Correct and sufficient
All three master grids switched from `SelectedItemChanged` to `RowClick` + `ReadOnly="true"`:

| Grid | `RowClick` handler | `ReadOnly` |
|---|---|---|
| `RoleMasterGrid` | `OnRowClickAsync` | `true` |
| `OrganizationMasterGrid` | `OnRowClickAsync` | `true` |
| `UsersMasterGrid` | `OnRowClickAsync` | `true` |

`RowClick` fires on every click regardless of prior `SelectedItem` state, eliminating the "first click sets cell focus, second click selects row" behavior.

### Inline DisplayAlias editor (RoleMasterGrid): ✅ Still functional
`MudDataGrid.ReadOnly` controls the grid's **built-in editing mode**, not the interactivity of custom `CellTemplate` contents. The `MudTextField` in the DisplayAlias column is a standalone component with its own `Value`/`ValueChanged` binding and `DebounceInterval="400"`. It remains fully interactive.

When a user clicks the DisplayAlias text field, `RowClick` also fires, but:
- The shell is already collapsed (from the initial row selection).
- Re-firing `SelectedRoleChanged` with the same role ID is idempotent — the parent sets the same state.
- MudBlazor's diff engine preserves the `MudTextField` component instance, so pending input is not destroyed.

The `SelectedItem="@_selected"` remains as a one-way binding for visual row highlighting, correctly updated in `OnRowClickAsync`.

### Playwright coverage: ✅ Matches UAT path
`RoleMasterGrid_FirstColumnCellClick_CollapsesShellOnFirstClickAsync`:
- Clicks `role-row-BackofficeUser` (the first-column cell's inner `<div>`, not a row-level locator).
- Asserts `role-master-details-shell-summary` becomes visible on the **first** click (shell collapsed).
- Asserts `role-details-section` is visible (details panel rendered on same click).

---

## Summary

| Finding | Severity | Fix | Regression | Coverage | Verdict |
|---|---|---|---|---|---|
| #2 — Admin drawer / User role | Blocker | ✅ | ✅ | ✅ | **PASS** |
| #11 — Create-org grid refresh | Blocker | ✅ | ✅ | ✅ | **PASS** |
| #14 — Currency symbol `¤` | Minor | ✅ | ✅ | ✅ | **PASS** |
| #49 — Two-click collapse | Minor | ✅ | ✅ | ✅ | **PASS** |

| Cross-cutting check | Verdict |
|---|---|
| Roles-override does not open auth bypass | ✅ Secret gate + AllowedRoles allow-list intact |
| Culture pin applied before Blazor circuits / formatters | ✅ Set before `app.Run()`, static propagation to thread pool |
| Auto-re-expand does not conflict with collapse-on-create tests | ✅ Temporally disjoint code paths; existing test passes |

**All four findings verified. Session is clean.**
