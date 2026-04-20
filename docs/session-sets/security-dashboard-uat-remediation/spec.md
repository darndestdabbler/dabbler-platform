# Security Dashboard UAT Remediation

> **Purpose:** Close the functional and UX defects the human found while
> running the Set B UAT checklist
> (`rearchitect-role-administration-dashboard-uat-checklist.json`), add
> Playwright coverage that mirrors each UAT item's steps and parameters
> so these regressions cannot slip through again, and apply the newly-
> promoted Human UAT Philosophy principle that every UAT checklist item
> must be proven executable by automation before the human runs it.
> **Created:** 2026-04-19 *(stub — fleshed out at the start of Session 1)*
> **Session Set:** `docs/session-sets/security-dashboard-uat-remediation/`
> **Prerequisite:**
> [Set B (`rearchitect-role-administration-dashboard`)](../rearchitect-role-administration-dashboard/change-log.md)
> complete and its UAT checklist filled in by the human
> ([link](../rearchitect-role-administration-dashboard/rearchitect-role-administration-dashboard-uat-checklist.json)).
> This set runs **before** Set D (`sensitive-field-masking-audit`),
> because most of Set D's work plugs into the Set B enforcement seam
> and restriction-driven column resolution — both of which must behave
> correctly before masking can be layered on top.
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Status

**This is a stub spec.** Session 1 of this set will flesh it out into
the full per-session plan (Steps, Creates, Touches, Progress keys,
Ends-with criteria, and risk notes) before any remediation work
begins. The stub below captures the confirmed findings grouped by
functional area so the fleshing-out has concrete inputs.

## UAT findings to remediate

Grouped by the UAT checklist's functional areas. Each finding below is
drawn directly from the human's inline feedback on
[`rearchitect-role-administration-dashboard-uat-checklist.json`](../rearchitect-role-administration-dashboard/rearchitect-role-administration-dashboard-uat-checklist.json).

### 1. Users and Role Assignment tab — critical crash (session-blocking)

- Clicking the tab throws
  `InvalidOperationException: A second operation was started on this
  context instance before a previous operation completed` from
  [`AdminUserQueryService.GetActorAssignedOrganizationIdsAsync`](../../../src/Libs/Dabbler.Security/Services/AdminUserQueryService.cs)
  line 133, called from
  [`UserRoleAssignmentDetails.RefreshActorContextAsync`](../../../src/Libs/Dabbler.Security.Blazor/Components/Admin/Users/UserRoleAssignmentDetails.razor)
  line 175 on `OnInitializedAsync`. Followed by an `ObjectDisposedException`
  during render.
- Blocks every UAT item under **Users and Role Assignment**, and
  contaminates the **Cross-Cutting Quality / Visual styling** item
  because the tab is unreachable.

### 2. Per-role tab gating — Security nav hidden for non-SystemAdmin

- `asmith@acmecoffee.com` (CrossOrgUserAdmin) and
  `jdoe@acmecoffee.com` (BackofficeUser) cannot see the Admin ▸ Security
  nav entry at all.
- Expected behaviour per Set B spec and Architecture doc: all five
  baked-in roles can reach `/admin/security`; the Role Documentation
  tab renders for every role; administrative tabs render only for the
  roles whose compiled privileges allow them.
- Root cause is likely a `UiRestriction` on `/admin/security` in
  `user-roles.json` or a `DabblerPageAuthorize` rule that's evaluated
  page-wide rather than tab-wide.

### 3. Organization scope filter — CrossOrgUserAdmin and BackofficeUser see zero retail businesses

- Neither `asmith@acmecoffee.com` (CrossOrgUserAdmin) nor
  `jdoe@acmecoffee.com` (BackofficeUser) sees any retail-business
  organizations or customer records.
- Expected: CrossOrgUserAdmin has `CanSeeAllOrganizations = true` and
  should see every org; BackofficeUser should see its assigned org's
  data.
- Root cause candidates: the `OrganizationScopeFilter` (Set B Session 6)
  is escaping only SystemAdmin, or the active-org context isn't being
  seeded into `IOrganizationContextAccessor` for non-SystemAdmin
  sessions.
- Also: filter on org `Key` contains `cust-` returns nothing; `cust`
  without the dash works — the dash is being treated as a delimiter or
  escape character somewhere.

### 4. Data-bound restriction enforcement — Access denied before seam can render

- Signing in as `jdoe@acmecoffee.com` (BackofficeUser) and navigating
  to `/admin/products` or the Product detail page returns an
  Access-denied message box.
- Expected: BackofficeUser sees UnitPrice as `ViewOnly` (read-only).
- Same outcome for WarehouseUser and the Hidden path.
- Root cause: the route-level `DabblerPageAuthorize` on
  `/admin/products` likely denies BackofficeUser before the
  `RestrictedFormField` / `RestrictedGridColumn` seam gets a chance to
  render its Hidden/ViewOnly variants. The seam needs to live on a
  route the target roles can reach.

### 5. Default Masks + Column Restrictions — metadata and UI defects

- Default Masks column dropdown does **not** list
  `Customer.SocialSecurityNumber` despite Set B Session 3 seeding it.
- Sensitive-name safety-net `Add to Default Masks` requires a browser
  refresh to reflect in the grid (not in-circuit reactive — regressed
  between bUnit-green and browser reality).
- Role Master Details ▸ Data Column Restrictions ▸ **Add button is
  disabled**.
- Default Masks table dropdown uses plural table names; Microsoft
  convention is singular entity names — update the dropdown label
  source.

### 6. Organization Master Details — mutation UX

- Clicking **New Organization** is not obviously scroll-linked; the
  new row appears far down the grid with no scroll-to-row or layout
  cue. The human suggested a vertical-layout-tabs idiom where the
  **Details** tab becomes the **New** tab when a new record is staged.
- Grid does not dynamically refresh after add / edit / archive —
  requires a browser refresh.
- Filter changes should similarly refresh the grid in-circuit.

### 7. Master-details real-estate management (pick one, then apply uniformly)

- The results grid is too tall across every master-detail tab. The
  human suggested two options and wants to try **both**, then pick:
  1. **MudExpansionPanel wrap.** When an item is selected, the
     results-grid expansion panel collapses and the selected row's
     summary is rendered in the panel header (via a `RenderFragment`).
     Changing the filter resets the panel header and re-expands the
     grid.
  2. **Vertical-layout tabs.** One tab for the Results grid, one tab
     for Details. Selecting a row navigates to Details; changing
     filters returns to Results.
- Apply the winning option uniformly to Role Master Details, Default
  Masks, Organization Master Details, and Users and Role Assignment.
- Condensed grid format with vertical scroll; default viewport ≈ 6
  rows without scrolling, grid expands to 10+ rows where the layout
  permits.

### 8. Column width and rendering — DisplayAlias

- The DisplayAlias column clips text and uses a larger font than its
  siblings. Fix by: (a) letting the column auto-size like HTML tables
  do, or (b) enabling text-wrap inside the cell. Column-width drag
  adjustment would also be welcome if MudDataGrid supports it cleanly.

### 9. Keyboard events — remove tab-strip arrow-key navigation

- The tab-strip arrow-key navigation + space-bar shortcut (added in
  `security-dashboard-polish` Session 6 and carried into Set B) is
  interfering with normal text-field editing. Arrow keys steal focus
  out of the editor into the tab strip.
- Human explicitly requests: "Let's not do fancy stuff with the
  keyboard events." Remove the tab-strip `@onkeydown` handler and the
  `HandleTabKeyDown` / `NextPanelIndex` logic in
  `DabblerSecurityDashboard.razor`. The Tab key itself still advances
  focus normally through MudBlazor defaults.
- Update the bUnit and Playwright tests that assert arrow-key
  behaviour — delete those assertions (do not try to keep them alive).

## Goals (for Session 1 to expand)

1. Every UAT finding above is closed, verified by:
    - A Playwright test that reproduces the exact steps the human ran
      with the exact same seeded parameters and asserts the expected
      outcome.
    - A bUnit test for the component-level fix where applicable.
2. Apply the newly-promoted Human UAT Philosophy principle: this
   set's own UAT checklist is committed together with its Playwright
   coverage suite (`*UatCoverageTests.cs`), not after.
3. No new surface area beyond what is required to close the findings
   and add coverage. In particular, Set D masking rendering, Set C
   system columns, and any new tabs or components are explicitly out
   of scope.
4. Build + all non-Playwright tests stay green after every session;
   Playwright tests stay green after Session 2 once the Users-tab
   crash is fixed.

## Session count (for Session 1 to confirm)

Seven or eight sessions are anticipated:

1. **Session 1 — flesh out this spec** into the full per-session plan.
2. **Session 2 — Users and Role Assignment crash fix** + per-item
   Playwright coverage for the Users-tab UAT items.
3. **Session 3 — per-role tab gating + Security nav visibility** + per-item
   Playwright coverage matching the UAT per-role gating items.
4. **Session 4 — Organization scope filter + `cust-` dash-filter bug +
   grid dynamic refresh + new-record UX** + per-item Playwright coverage
   matching the Organization UAT items.
5. **Session 5 — Data-bound restriction enforcement reachability**
   (fix Access-denied-before-seam on Products paths) + per-item
   Playwright coverage matching the Data-bound Restriction UAT items.
6. **Session 6 — Default Masks + Column Restrictions metadata + UI
   fixes** (Add button, SocialSecurityNumber in dropdown, safety-net
   in-circuit refresh, singular table names) + per-item Playwright
   coverage for the Default Masks UAT items.
7. **Session 7 — Master-details UX refactor** (try both MudExpansionPanel
   and vertical-tabs options; pick one; apply uniformly; fix
   DisplayAlias column width/wrap; condensed grid with ≈ 6-row
   viewport; remove tab-strip arrow-key nav) + Playwright coverage for
   the updated UX.
8. **Session 8 — UAT checklist (with every item backed by an in-repo
   Playwright test that passes), change log, Step 9 reorganization
   review.** The checklist is committed in the same commit as its
   Playwright coverage file, not separately.

Session 1 will confirm whether any of the above can fold together or
split apart based on the actual fix scope.

## Rules specific to this set

- **Every session that adds or modifies a UAT-visible behaviour must,
  in the same session, add the corresponding Playwright test that
  drives the same steps with the same parameters.** This is the
  on-ramp for the newly-promoted Human UAT Philosophy principle. If
  a session would otherwise ship a behavioural change without matching
  Playwright coverage, the session is incomplete.
- **Regression-test every prior fix on each new session.** By the end
  of the set, the dashboard Playwright suite covers every UAT item
  plus the arrow-key-nav removal.
- **Preserve the UAT checklist file.** Do not modify or overwrite the
  Set B UAT checklist at
  `../rearchitect-role-administration-dashboard/rearchitect-role-administration-dashboard-uat-checklist.json`.
  It's the source-of-truth record of what the human actually found on
  a given day. This set writes its own checklist,
  `security-dashboard-uat-remediation-uat-checklist.json`, and that one
  drives the human's second pass after remediation lands.

## Risk notes (for Session 1 to expand)

- **Session 2 DbContext fix scope.** The root cause may be
  `AddDbContext` scope mismatch (Singleton vs Scoped) or concurrent
  awaits inside a Blazor Server circuit. If the fix requires an
  `IDbContextFactory<T>` refactor, that may cascade across
  `AdminUserQueryService`, `UserRoleAssignmentDetails`, and sibling
  services. Budget accordingly.
- **Session 3 nav visibility.** The fix may require splitting the
  `/admin/security` `UiRestriction` behaviour: the page itself must be
  reachable by all five baked-in roles; the individual administrative
  tabs remain role-gated. This is a subtle design point — document the
  decision in the session's verification prompt.
- **Session 5 restriction-seam reachability.** The Access-denied
  behaviour may indicate that the enforcement seam needs to sit on a
  route that BackofficeUser and WarehouseUser can reach (e.g.,
  `/products` rather than `/admin/products`) or that the route-level
  `DabblerPageAuthorize` needs to be softened to allow restriction-seam
  evaluation.
- **Session 7 master-details UX.** Trying both idioms adds
  implementation time. If one option clearly wins during proof-of-
  concept, the other can be abandoned in that session rather than
  continued for parity. Document the decision in the session's
  verification prompt.
- **Playwright volume.** This set will roughly double the
  `tests/AcmeCoffeeOnline.Playwright/` surface. Budget run time in CI
  accordingly; keep individual tests tightly scoped so total suite
  time stays reasonable.

## Exit criteria (whole set)

- Every UAT finding above is closed, with matching Playwright coverage
  that uses the same personas and parameters as the UAT step.
- The `security-dashboard-uat-remediation` UAT checklist is committed
  together with its `*UatCoverageTests.cs` Playwright coverage suite;
  the suite is green.
- `dotnet build Dabbler.Platform.sln` and all non-Playwright tests
  green at set close.
- Set D (`sensitive-field-masking-audit`) is unblocked.
