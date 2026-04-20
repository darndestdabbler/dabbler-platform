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

**Session 1 complete — spec fleshed out 2026-04-20.** The per-session
plan, risk notes, and exit criteria below were generated via the AI
router (Gemini Pro, `task_type="planning"`) against the stub findings,
the human's Set B UAT feedback, and the two locked-in scope decisions
from Session 1:

1. **The sensitive-name safety-net feature is being removed, not fixed.**
   Per the human's direction at the start of Session 1, Session 6
   deletes the feature, its UI surface, and its tests outright rather
   than remediating the in-circuit refresh defect. The backing
   principle — trust business + auditor backstop over auto-detection
   tooling for compliance-accountable classifications — was promoted
   into `docs/planning/project-guidance.md` under **Principles →
   Security And Organization Model** in the same Session 1 commit.
2. **Eight sessions confirmed.** Session 1 (planning) stays separate
   from Session 2 even though it ships no code; its deliverable is
   this plan. Session 6 stays as its own session despite the reduced
   scope because the remaining Default Masks / Column Restrictions
   work still has enough surface area to warrant the focus.

The findings below remain the source-of-truth mapping; each session's
**Steps** cross-reference finding numbers.

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

### 10. Docker test infrastructure — four pre-existing failures surfaced by Session 3

Session 3 unmasked four pre-existing Docker-specific Playwright failures
that were previously hidden by the `_framework/blazor.web.js` 404. All
four existed before Session 3 and are unrelated to Finding 2. They must
be triaged **at the start of Session 4** (before Session 4's scope work
begins), not deferred past this set. Session 3 landed 95/99 green; these
four are the gap.

- **10.1** `SmokeTests.AdminNavigation_UsesFocusedProductsEntryAndActualRolePermissionsAsync`
  fails at `SmokeTests.cs:253`. The test asserts `access-denied-page` is
  visible when `kbrown@acmecoffee.com` (BackofficeUserAdmin) navigates
  to `/admin/products`. Either the test's assertion is stale (products
  admin is now reachable by the role) or the page-authorization rule
  drifted. Needs a same-session decision: update the test OR restore
  the rule.
- **10.2** `RestrictionEnforcementTests.CustomerRole_SeesUnitPriceFieldButReadOnlyAsync`
  fails with `HTTP 500: GET http://web:8080/products/17`. Web container
  logs show `HttpRequestException: 401 (Unauthorized)` from the typed
  `ProductApiClient` calling the api. Root cause is likely that the
  api's authorization for `GET /api/products/{id}` rejects the web's
  `X-Internal-Api-Key` + `X-App-User-Id` headers for a Customer-role
  user under the Docker bridge network. Verify the internal-api-key
  flow still authenticates in Docker.
- **10.3** `OrdersWorkflowTests.OrderDetail_SystemAdmin_CanEditNotesAndPersistAsync`
  fails with `Assert.Equal` — expected `""` but got a previous run's
  `UAT edit <guid>` note. This is sqlserver-container **state
  pollution** between runs (the seeded AcmeCoffee DB retains notes
  from prior test runs). Either reset the AcmeCoffee schema between
  runs or scope the test to a unique order per run.
- **10.4** `EdgeCaseTests.AccessDenied_CustomerRole_BlockedFromCustomersPageAsync`
  fails waiting for `GetByRole(Link, "Go to sign in")` on the
  access-denied page. The access-denied page no longer surfaces that
  link, or the test's selector drifted. Align the test with the
  current page contract.

**Assigned to:** Session 4 Step 0 (pre-scope triage). Session 4 must
confirm all four are fixed or explicitly retired before starting
Session 4's primary work.

### 10. Session 4 Step 0 triage outcome (2026-04-20)

- **10.1 — Fixed (test updated).** `SmokeTests.cs` lines 248-259 asserted
  BackofficeUserAdmin saw access-denied on `/admin/products` and
  `/admin/security`. Both assertions were stale: Set A Session 3's handler
  rewrite removed the SystemAdmin-only gate on `/admin/products`, and
  Session 3 of this set (`security-dashboard-uat-remediation`) made
  `/admin/security` reachable by every baked-in role. Per the
  "Hard-Asserted Disabled-State Tests" convention, both assertions were
  inverted to `ToHaveCountAsync(0)` so the role whose product definition
  actually allows the access is the one proving it.
- **10.2 — Deferred to Session 5 (test skipped with rationale).** The
  failure is the restriction-seam reachability seam described in
  Finding 4, observed for Customer instead of BackofficeUser /
  WarehouseUser. Rather than solve the Docker-specific principal
  resolution here and then re-solve it in Session 5 alongside the
  page-level authorization decision, the test is marked
  `[Fact(Skip = "Deferred to Session 5 ...")]`. Session 5 re-enables or
  rewrites it once the seam route strategy is finalized.
- **10.3 — Fixed (test simplified).** `OrderDetail_SystemAdmin_CanEdit
  NotesAndPersistAsync` appended its marker to the prior notes value,
  so any run that crashed between Save and Restore left residue that
  snowballed across subsequent Docker runs. The test now overwrites
  with a unique marker and restores the originally-captured notes,
  making it idempotent regardless of sqlserver state between runs.
- **10.4 — Fixed (test updated).** `AccessDenied_CustomerRole_Blocked
  FromCustomersPageAsync` asserted a `Go to sign in` link on the
  access-denied page. The current `AccessDeniedCard` renders a
  `Back to dashboard` button (`data-testid="access-denied-back-button"`)
  and does not surface a sign-in link. The test now asserts the
  back-to-dashboard button, matching the live contract.

### Finding 3 sub-issue — dash-in-filter clarification (2026-04-20)

The UAT called out `Key contains 'cust-'` returning zero rows while
`cust` returned all retail orgs. Tracing the filter pipeline end-to-end
found no tokenization or escape-character handling that would mis-treat
the dash:

- `FilterPanel<T>` uses `MudTextField T="string"` and passes the value
  through `FilteringCoordinator.SetFilter` as a raw `object?`.
- `OrganizationMasterGrid.ApplyFilter` uses `string.Contains(needle,
  StringComparison.OrdinalIgnoreCase)`, which is dash-literal by spec.
- The seed data (`SeedData.GetCustomerOrganizationKey` returning
  `$"customer-{customer.Id}"`) uses keys like `customer-101`, so
  `cust-` is correctly NOT a substring; `customer-`, `mer-1`, and
  `cust` all are.

This is not a tokenization bug — the filter works as `.Contains`
contracts. The UAT observation reflects the seed data shape rather
than a defect in the pipeline. Session 4 adds
`Grid_FilterByKeyContains_HandlesDashCharacter` (theory, four rows)
to `OrganizationMasterGridTests.cs` to lock the semantics down.
The `/admin/security` Playwright coverage in
`SecurityDashboardUatCoverageTests.cs` additionally drives the
dashboard filter with `customer-` against the live web stack so any
future regression in the router between UI and grid would surface.

## Goals

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

## Session Plan

### Session 1 of 8: Planning and Scoping

**Steps:**
1. Read the prerequisite Set B UAT checklist, this stub spec, and
   `docs/planning/project-guidance.md` + `docs/planning/lessons-learned.md`.
2. Lock in the two scope decisions captured in **Status** (safety-net
   removal; 8-session count).
3. Promote the "trust business + auditor backstop over auto-detection
   tooling" rule to a Principle under Security And Organization Model
   in `docs/planning/project-guidance.md`.
4. Route the plan expansion (`task_type="planning"`); splice the
   response into this spec's Session Plan / Risk notes / Exit criteria
   sections.
5. Build + test (no code changes expected — verifies the repo is
   green before remediation starts).
6. End-of-session verification through a different AI provider.

**Creates:** None (artifact is the plan itself, in this spec file).
**Touches:**
- `docs/session-sets/security-dashboard-uat-remediation/spec.md`
- `docs/planning/project-guidance.md` (new Principle bullet)

**Progress keys:** `session-1/read-context`,
`session-1/promote-principle`, `session-1/route-plan`,
`session-1/splice-plan-into-spec`, `session-1/build-test`,
`session-1/verify`.

**Ends with:** Build + test green; this spec contains a concrete
Session Plan, Risk notes, and Exit criteria; the new Principle is in
`project-guidance.md`; `session-001.md` cross-provider review is
saved raw.

### Session 2 of 8: Users and Role Assignment crash fix

**Steps:**
1. Diagnose the `InvalidOperationException` ("second operation started
   on this context") and subsequent `ObjectDisposedException` thrown
   from `AdminUserQueryService.GetActorAssignedOrganizationIdsAsync`
   when the Users and Role Assignment tab initializes. (Closes
   finding 1.)
2. Refactor `AdminUserQueryService` and its Blazor callers to use
   `IDbContextFactory<DabblerSecurityDbContext>` so each query gets
   its own context instance, eliminating cross-await reuse on the
   Blazor Server circuit.
3. Update DI registration (e.g., `AddDbContextFactory` alongside the
   existing `AddDbContext`) and audit sibling services that share the
   same pattern — fix callers that await concurrently without
   factoring the context.
4. Create `tests/AcmeCoffeeOnline.Playwright/SecurityDashboardUatCoverageTests.cs`
   with one Playwright test per Users-tab UAT item, signing in as the
   personas the Set B checklist named (`admin@acmecoffee.com` for
   tab load, `asmith@acmecoffee.com` for role-assignment mutation,
   etc.) and asserting the page loads without circuit exceptions.
5. Regression: run the full Playwright suite. (First session creating
   the file, so regression-set is this file + any pre-existing
   dashboard coverage.)

**Creates:**
- `tests/AcmeCoffeeOnline.Playwright/SecurityDashboardUatCoverageTests.cs`

**Touches:**
- `src/Libs/Dabbler.Security/Services/AdminUserQueryService.cs`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Users/UserRoleAssignmentDetails.razor`
- DI registration in the Web host (`Program.cs` or equivalent) and
  any sibling services that need the factory
- Relevant bUnit tests that assert the Users tab renders

**Progress keys:** `session-2/diagnose-dbcontext-crash`,
`session-2/refactor-to-dbcontextfactory`,
`session-2/update-di-registration`,
`session-2/playwright-users-tab-coverage`,
`session-2/run-regression-tests`.

**Ends with:** Build + test green; the Users tab renders end-to-end
without circuit exceptions for every seeded persona named in the
Users and Role Assignment UAT items; new Playwright tests pass.

### Session 3 of 8: Per-role tab gating and Security nav visibility

**Steps:**
1. Make `/admin/security` reachable by all five baked-in roles. Root
   cause is likely a page-wide `UiRestriction` or
   `DabblerPageAuthorize` rule in `user-roles.json` or the page
   attributes. (Closes finding 2.)
2. Preserve per-tab privilege gating so only SystemAdmin sees all
   five tabs; CrossOrgUserAdmin sees the three its compiled
   privileges allow; BackofficeUser sees only Role Documentation.
   Tab visibility stays a compiled-privilege decision, not a page
   authorization decision.
3. Add Playwright tests covering:
   - `admin@acmecoffee.com` sees five tabs.
   - `asmith@acmecoffee.com` (CrossOrgUserAdmin) sees three tabs
     (Organization Master Details, Users and Role Assignment, Role
     Documentation) and CAN reach `/admin/security` via the nav
     drawer.
   - `jdoe@acmecoffee.com` (BackofficeUser) reaches `/admin/security`
     and sees only Role Documentation.
4. Re-run Session 2's Users-tab tests against every newly-permitted
   persona to confirm the tab load fix still holds under the broader
   nav-visibility change.

**Creates:** None.
**Touches:**
- `src/Apps/AcmeCoffeeOnline.Web/security-config.json` (or the
  equivalent `user-roles.json` / page-restriction source)
- The `DabblerPageAuthorize` usage on `/admin/security` and the
  per-tab gating logic in `DabblerSecurityDashboard.razor`
- The navigation drawer / `MudNavMenu` entry for Admin ▸ Security
- `tests/AcmeCoffeeOnline.Playwright/SecurityDashboardUatCoverageTests.cs`

**Progress keys:** `session-3/open-security-page-for-five-roles`,
`session-3/preserve-per-tab-privilege-gating`,
`session-3/playwright-per-role-nav-and-tab-coverage`,
`session-3/run-regression-tests`.

**Ends with:** Build + test green; Playwright confirms the Admin ▸
Security nav entry appears for every baked-in role; tab visibility
continues to match compiled privileges; Session 2 tests remain green
on the broader persona set.

### Session 4 of 8: Organization scope filter and mutation UX

**Steps:**
0. **Pre-scope triage of Finding 10** (the four Docker-test-infra
   failures surfaced by Session 3). For each of 10.1–10.4: confirm
   the test is run under Docker, identify root cause, and either
   fix the app/test or formally retire the assertion with a
   one-line rationale in this spec. Full 99-test Playwright must
   go clean (or cleanly-green-with-retired-tests) before Step 1.
1. Fix the `OrganizationScopeFilter` so CrossOrgUserAdmin
   (`CanSeeAllOrganizations = true`) sees every organization, and
   BackofficeUser sees only its assigned org's records. The EF
   global query filter must stop excluding non-SystemAdmin sessions.
   (Closes finding 3.)
2. Fix the org-key filter so `contains 'cust-'` matches — the dash
   is being treated as a delimiter somewhere in the filter pipeline
   (likely tokenization in the expression engine or FilterPanel
   parsing). (Closes the sub-finding under #3.)
3. Implement in-circuit grid refresh on Organization Master Details
   after Create, Edit, and Archive, plus on filter changes. No
   browser refresh should be required. (Closes finding 6.)
4. Playwright coverage for:
   - `asmith@acmecoffee.com` sees all organizations AND customer
     records under `/customers` or `/orders`.
   - `jdoe@acmecoffee.com` sees only its own org's customer
     records.
   - Filter `Key contains 'cust-'` returns the same rows as
     `contains 'cust'`.
   - Add / rename / archive org and observe the grid update
     in-circuit.
5. Regression: re-run Session 2 and Session 3 tests.

**Creates:** None.
**Touches:**
- `src/Libs/Dabbler.Security/Data/OrganizationScopeFilter.cs` (or
  wherever the EF global filter lives) and related `DbContext`
  setup
- `src/Libs/Dabbler.Components/Filter/` expression engine or
  `FilterPanel<T>` parsing code for the dash bug
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Organizations/OrganizationMasterDetails.razor`
- `tests/AcmeCoffeeOnline.Playwright/SecurityDashboardUatCoverageTests.cs`

**Progress keys:** `session-4/fix-org-scope-filter`,
`session-4/fix-dash-in-filter-expression`,
`session-4/in-circuit-grid-refresh`,
`session-4/playwright-org-scope-and-refresh-coverage`,
`session-4/run-regression-tests`.

**Ends with:** Build + test green; CrossOrgUserAdmin and
BackofficeUser see expected orgs and customers; dash filter works;
grid refreshes in-circuit after mutations and filter changes.

### Session 5 of 8: Data-bound restriction enforcement reachability

**Steps:**
1. Diagnose the "Access denied" message BackofficeUser and
   WarehouseUser see on `/admin/products` and the product detail
   page. The route-level `DabblerPageAuthorize` is denying them
   before the `RestrictedFormField` / `RestrictedGridColumn` seam
   gets a chance to render the ViewOnly/Hidden variants. (Closes
   finding 4.)
2. Decide: move the restriction-seam page onto a route the target
   roles can reach (e.g., `/products` rather than `/admin/products`)
   OR soften the page-level authorization so the component-level
   restriction engine can render its degraded variants. Document the
   decision in the session's verification prompt with its rationale.
3. Make sure the BackofficeUser ViewOnly path renders the UnitPrice
   field disabled on both the grid and detail form. Make sure the
   WarehouseUser Hidden path removes the UnitPrice column from the
   grid and the field from the detail form entirely.
4. Playwright coverage:
   - `jdoe@acmecoffee.com` reaches the product list and detail pages
     without an access-denied box; UnitPrice renders read-only.
   - A WarehouseUser persona (from Session 2's role-assignment
     elevation, or via a seed update) sees no UnitPrice column or
     field anywhere in the Products path.
5. Regression: re-run Sessions 2-4 tests.

**Creates:** None (may create a seed role-assignment for a
WarehouseUser persona if none exists — declare it here rather than
adding a new seed file).
**Touches:**
- Route-level authorization on `/admin/products` (or replace with a
  BackofficeUser-reachable route)
- `src/Libs/Dabbler.Security.Blazor/Components/Restricted/` seam
  components
- Product list and detail pages under `src/Apps/AcmeCoffeeOnline.Web/`
- `tests/AcmeCoffeeOnline.Playwright/SecurityDashboardUatCoverageTests.cs`
- Possibly `src/Apps/AcmeCoffeeOnline.Web/security-config.json` if a
  restriction seed needs adjustment

**Progress keys:** `session-5/choose-restriction-seam-route-strategy`,
`session-5/implement-viewonly-and-hidden-rendering`,
`session-5/playwright-restriction-seam-coverage`,
`session-5/run-regression-tests`.

**Ends with:** Build + test green; BackofficeUser and WarehouseUser
reach the products path without access-denied; ViewOnly and Hidden
semantics render as designed; UAT #4 items are provably executable.

### Session 6 of 8: Default Masks and Column Restrictions metadata + UI fixes

**Scope note:** This session **removes** the sensitive-name safety-net
feature rather than remediating its in-circuit refresh bug, per the
Session 1 scope decision and the newly-promoted Principle. Business
owners populate Default Masks directly and the internal auditor is
the backstop. Removal includes the safety-net panel UI, its backing
scanner/service, its bUnit tests, and any Playwright assertions that
exercise the "Add to Default Masks" flow from the safety-net panel.

**Steps:**
1. Delete the sensitive-name safety-net feature:
   - Remove the expansion panel from the Default Masks tab.
   - Remove the backing scanner service and its DI registration.
   - Remove the associated bUnit tests and any Playwright
     assertions that operate the safety-net.
   - Remove the matching item from the previous Set B UAT
     checklist's coverage expectations (but do NOT edit the Set B
     checklist JSON itself — it's preserved as historical record).
2. Fix the Default Masks column dropdown so `Customer.SocialSecurityNumber`
   appears — root cause is either the seed in Set B Session 3 not
   flowing through or the dropdown data source excluding it.
   (Closes finding 5a.)
3. Enable the Role Master Details ▸ Data Column Restrictions ▸
   **Add** button. (Closes finding 5b.)
4. Switch the Default Masks table-name dropdown to use singular entity
   names (`Customer`, not `Customers`) per the Microsoft convention
   call-out in the UAT. (Closes finding 5c.)
5. Playwright coverage:
   - `admin@acmecoffee.com` can add a Default Mask for
     `Customer.SocialSecurityNumber`.
   - Role Master Details ▸ Data Column Restrictions ▸ Add button is
     enabled for SystemAdmin on a user-type role; a row can be added.
   - Default Masks table dropdown shows singular names.
   - Safety-net expansion panel is absent from the Default Masks tab.
6. Regression: re-run Sessions 2-5 tests.

**Creates:** None.
**Touches:**
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Masks/DefaultMasksTab.razor`
  (or equivalent) for safety-net removal + dropdown fixes
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Roles/ColumnRestrictionsSubTab.razor`
  for the Add button
- Any `SensitiveNameScanner` service + DI registration
- Seed file(s) behind the column dropdown data source to restore
  `Customer.SocialSecurityNumber` visibility
- `tests/AcmeCoffeeOnline.Playwright/SecurityDashboardUatCoverageTests.cs`
- Relevant bUnit tests (delete safety-net tests; keep/add tests for
  the three remaining fixes)

**Progress keys:** `session-6/remove-safety-net-feature`,
`session-6/fix-ssn-in-dropdown`,
`session-6/enable-add-column-restriction-button`,
`session-6/singular-table-names-in-dropdown`,
`session-6/playwright-masks-coverage`,
`session-6/run-regression-tests`.

**Ends with:** Build + test green; safety-net gone from UI and tests;
the three remaining UAT #5 findings are resolved and have Playwright
coverage.

### Session 7 of 8: Master-details UX refactor + tab-strip keyboard removal + DisplayAlias width

Detailed implementation plan: [session-7-master-details-implementation-plan.md](C:/Users/denmi/source/repos/dabbler-platform/docs/session-sets/security-dashboard-uat-remediation/session-7-master-details-implementation-plan.md). Treat that document as the concrete source of truth for Session 7 component sequencing, state transitions, and test updates.

Human-approved Session 7 decisions:

- Skip the dual-idiom spike and proceed directly with `Idiom A`.
- Treat Default Masks as an approved partial adaptation of the master-details idiom rather than forcing an artificial row-details editor.
- Prefer a small shared shell for the outer expansion/header/results/details behavior.
- Fix Session 7-caused regression failures inline during Session 7 rather than deferring them by default.
- Session 7 may be implemented mostly directly by the orchestrator rather than routing every difficult reasoning step. The mandatory exception is final verification: end-of-session verification must still go to another AI provider, and any verifier-requested fix must either be implemented or raised to the human for a decision before the session closes.

**Steps:**
1. Apply the human-approved `Idiom A` plan from the companion
   implementation document rather than running a dual-idiom spike.
   Capture the approved spec deviation in the verification prompt:
   - wrap the results grid or management surface in a
     `MudExpansionPanel`
   - on row select, collapse the panel and render a summary in the
     panel header
   - filter change re-expands the panel and clears the summary state
   - Organization Master Details shows a visible `New organization`
     cue when a create flow is staged
2. Apply the chosen idiom across Role Master Details, Organization
   Master Details, Users and Role Assignment, and the approved
   partial-adaptation treatment for Default Masks. (Closes finding 7;
   closes the layout portion of finding 6 — Session 4 closes its
   in-circuit-refresh portion.)
3. Standardize the results grid as the condensed MudDataGrid layout
   with a ~6-row default viewport, vertical scroll, and room to
   expand to 10+ rows where the parent layout permits. Preserve any
   existing pagination behavior unless Session 7 proves a specific
   incompatibility that must be documented explicitly.
4. Fix the DisplayAlias column so content isn't clipped, and
   reconcile the larger font so the column matches siblings.
   Human-approved direction: use wrapped body-typography styling
   rather than auto-growing width. (Closes finding 8.)
5. **Remove** the tab-strip `@onkeydown` handler,
   `HandleTabKeyDown`, and `NextPanelIndex` logic from
   `DabblerSecurityDashboard.razor`. Let MudBlazor's default Tab
   behaviour handle tab focus. **Delete** every bUnit and
   Playwright assertion that covers the arrow-key behaviour (the
   Set B UAT item that had `Passes: true` for arrow-key nav MUST
   be removed from the remediation coverage — do not try to
   preserve it). (Closes finding 9.)
6. Update all prior Playwright tests to navigate through the new
   master-details UX so they remain green — this becomes the
   single biggest UX-migration pass of the set.
7. Add NEW Playwright assertions for the behaviours this session
   introduces. This is distinct from step 6 (updating existing
   tests to navigate the new layout) and covers:
   - The chosen master-details idiom's core behaviour (e.g., panel
     collapse on row select + header summary for Idiom A; plus the
     Organization `New organization` create-state cue). (New
     assertions for finding 7.)
   - The condensed grid viewport (~6 rows visible without scroll,
     expandable where the parent layout permits).
   - DisplayAlias column text-wrap behaviour so the previously-
     clipped content is now fully visible and the font matches
     sibling columns. (New assertions for finding 8.)
8. Regression: re-run Sessions 2-6 tests end-to-end against the
   new UX to confirm no behavioural regressions; this is the
   largest regression pass of the set. Fix Session 7-caused
   regressions inline during Session 7; if a failure appears to be a
   pre-existing unrelated defect, raise it to the human for
   direction before expanding scope.

**Creates:** None.
**Touches:**
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/DabblerSecurityDashboard.razor`
  (remove arrow-key handler)
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Role/RoleMasterDetailsTab.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Masks/DefaultMasksTab.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Organization/OrganizationMasterDetailsTab.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Users/UserRoleAssignmentDetails.razor`
- Shared master-details wrapper component
- DisplayAlias column definitions
- `tests/AcmeCoffeeOnline.Playwright/SecurityDashboardUatCoverageTests.cs`
- bUnit tests that still reference arrow-key nav (delete those
  assertions)

**Progress keys:** `session-7/spike-both-master-detail-idioms`,
`session-7/pick-winner-and-apply-uniformly`,
`session-7/condensed-grid-with-six-row-viewport`,
`session-7/fix-displayalias-column-width-and-font`,
`session-7/remove-tab-strip-arrow-key-handler-and-tests`,
`session-7/update-playwright-to-new-ux`,
`session-7/add-new-ux-assertions`,
`session-7/run-regression-tests`.

**Ends with:** Build + test green; one master-details idiom applied
across all four tabs, with Default Masks using the approved
partial-adaptation treatment; condensed grid layout uniform;
DisplayAlias readable with wrapped body typography; arrow-key
handler and its tests are deleted; full Playwright suite green
against the new UX.

### Session 8 of 8: UAT checklist + change log + Step 9 review

**Steps:**
1. Create `security-dashboard-uat-remediation-uat-checklist.json`
   under this session set's folder, following the editor schema.
   One `Review` item per UAT finding remediated (1-9 minus the
   removed safety-net item, plus the Set B regression targets
   listed in the Risk notes). Each item's `HumanAction` names the
   persona and parameters matching its Playwright counterpart in
   `SecurityDashboardUatCoverageTests.cs`; each item's `Expectation`
   describes the observable outcome the Playwright test asserts.
2. Verify every Review item has a corresponding passing Playwright
   test in `SecurityDashboardUatCoverageTests.cs`. If any item does
   not have coverage, add it before the checklist is committed —
   this is the session-closeout gate per the promoted Human UAT
   Philosophy principle.
3. Run `dotnet build Dabbler.Platform.sln`, `dotnet test
   Dabbler.Platform.sln`, and the Playwright suite one last time
   end-to-end.
4. Generate the change log via `route(task_type="summarization")`
   (per the routing lesson — `documentation` wraps narrative
   prose in a code-doc template and fails). Write
   `docs/session-sets/security-dashboard-uat-remediation/change-log.md`
   in the same commit as the checklist and its coverage file.
5. Notify, then run the Step 9 reorganization review against
   `docs/planning/project-guidance.md` and
   `docs/planning/lessons-learned.md`. Post-notify ordering is
   mandatory so the human isn't blocked on reviewing proposals
   before the session-complete signal.

**Creates:**
- `docs/session-sets/security-dashboard-uat-remediation/security-dashboard-uat-remediation-uat-checklist.json`
- `docs/session-sets/security-dashboard-uat-remediation/change-log.md`

**Touches:**
- `tests/AcmeCoffeeOnline.Playwright/SecurityDashboardUatCoverageTests.cs`
  (final gap-fill if any item lacks coverage)
- `docs/planning/project-guidance.md` /
  `docs/planning/lessons-learned.md` only if the Step 9 review
  proposes and the human accepts a move

**Progress keys:** `session-8/create-uat-checklist`,
`session-8/verify-playwright-coverage-per-item`,
`session-8/full-suite-green-run`,
`session-8/generate-change-log`,
`session-8/commit-and-notify`,
`session-8/step-9-reorganization-review`.

**Ends with:** `change-log.md` exists; UAT checklist is committed
alongside its Playwright coverage; full build + test + Playwright
green; Set D (`sensitive-field-masking-audit`) is unblocked.

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

## Risk notes

- **Session 2 DbContext fix scope.** The `InvalidOperationException`
  is a Blazor Server `DbContext` lifetime issue. Root cause is almost
  certainly concurrent awaits on a shared scoped context; the
  standard fix is `IDbContextFactory<T>`. Cascade risk: every
  consumer of `AdminUserQueryService` and sibling services may need
  adjustment, and the Web host's DI registration needs both
  `AddDbContext` (for EF tracking graphs) and `AddDbContextFactory`
  (for factory-backed one-shot queries). **Mitigation:** before
  editing, grep for every caller of the affected services and plan
  adjustments as one batch so Session 2 doesn't accidentally expand
  into Session 3's scope.
- **Session 3 nav visibility vs. per-tab gating.** The
  `/admin/security` page must be reachable by all five baked-in
  roles; individual tabs must stay role-gated by compiled
  privileges. Risk: a blanket softening of the page restriction
  could accidentally open tabs the role isn't supposed to see.
  **Mitigation:** route-level access check is "can reach the page";
  per-tab check runs inside the dashboard component against the
  actor's compiled role. Document the split explicitly in the
  Session 3 verification prompt so the verifier sees the reasoning.
- **Session 4 org-scope EF filter scope.** The filter is global and
  applies to every query that touches organization-scoped entities.
  A too-aggressive fix could leak data across orgs; a too-loose fix
  could break BackofficeUser's isolation. **Mitigation:** write the
  Playwright tests first (CrossOrgUserAdmin sees all, BackofficeUser
  sees only its org), then adjust the filter until both pass.
- **Session 4 dash-in-filter bug.** Root cause is probably in the
  QBE expression engine's tokenizer or in the `FilterPanel<T>`'s
  string-value handling (a dash may be treated as a range operator
  or escape character). **Mitigation:** find the tokenization pass,
  add a unit test asserting that `Key contains 'cust-'` produces the
  expected predicate AST, and fix from there. Do NOT special-case
  the dash only in the Organizations tab — the bug lives in the
  reusable filter layer and will recur elsewhere.
- **Session 5 restriction-seam reachability.** Two viable
  approaches: move the seam onto a route the target roles can
  reach, or relax page-level authorization and let the seam render
  degraded variants. **Mitigation:** pick ONE approach with reasons
  captured in the verification prompt. A hybrid (relax for some
  routes, move for others) is a maintenance liability.
- **Session 6 safety-net removal cascade.** The safety-net's
  scanner service may be DI-registered from a shared extension
  method or called by more than just the Default Masks tab.
  **Mitigation:** grep for every reference before deletion; adjust
  any consumer in the same session. If a consumer is already owned
  by a future session (Set D masking), mark the scanner `[Obsolete]`
  rather than deleting — per the `[Obsolete]` lesson in
  project-guidance.md.
- **Session 7 master-details UX refactor size.** Applying one
  chosen idiom to four tabs plus fixing DisplayAlias plus removing
  keyboard logic is the biggest session in the set. **Mitigation:**
  the spike in step 1 must be timeboxed; if one idiom is obviously
  losing, abandon it rather than build parity. If the session
  starts running long, defer the Users and Role Assignment tab's
  master-details conversion to a follow-up session — it has the
  least UAT exposure of the four.
- **Regression targets from Set B that must NOT regress.** Each
  Playwright session from Session 2 onward re-runs the full
  coverage file, so these become implicit regression guards:
  - `admin@acmecoffee.com` sees all five tabs (Set B UAT item
    "SystemAdmin can reach /admin/security through the nav
    drawer").
  - Role Documentation tab content for all baked-in roles
    (`Passes: true` in Set B).
  - Mask Exemption grant under SystemAdmin with blank `ExpiresAt`
    for a user-type target (`Passes: true` in Set B).
  - Cross-tab reactivity: a new Default Mask is available in the
    Mask Exemptions sub-tab in the same circuit (`Passes: true`
    in Set B).
  - DisplayAlias inline-edit persistence for both baked-in and
    user-type rows (functional part of the UAT item passed; only
    the column-width UX regressed — keep the persistence test).
  - Filter panel narrowing by alias (`Passes: true` in Set B).
  - Standard `Tab`-key focus progression continues to work after
    the arrow-key handler is deleted in Session 7 — this is
    important because the Cross-Cutting Accessibility UAT item
    explicitly called out Tab behaviour as the one keyboard
    pattern the human does care about.
- **Playwright suite volume and CI budget.** This set roughly
  doubles the `tests/AcmeCoffeeOnline.Playwright/` surface. Each
  session's regression-test step amplifies that. **Mitigation:**
  keep individual tests tightly scoped (one behaviour per test),
  share sign-in fixtures via `PlaywrightFixture.SeededSignInAsync`,
  and prefer navigation via the drawer (per the "don't bypass with
  GotoAsync" lesson) but without adding redundant drawer-click
  coverage inside every behavioural test.
- **Keyboard-handler removal is destructive by design.** The
  tab-strip arrow-key behaviour's one `Passes: true` UAT item
  from Set B must be deleted from coverage expectations here.
  Per the `security-dashboard-polish` Session 9 decision and the
  human's repeated feedback ("let's not do fancy stuff with
  keyboard events"), do NOT reintroduce the behaviour under any
  flag, toggle, or opt-in.

## Exit criteria (whole set)

- **Finding 1** (Users and Role Assignment DbContext crash) closed:
  the tab renders end-to-end for every persona the Users UAT items
  named.
- **Finding 2** (Security nav visibility for non-SystemAdmin roles)
  closed: all five baked-in roles can reach `/admin/security`; each
  sees the tabs their compiled privileges allow.
- **Finding 3** (org-scope filter + `cust-` dash-filter bug) closed
  under Session 4's Playwright coverage.
- **Finding 4** (Data-bound restriction enforcement reachability)
  closed: BackofficeUser reaches `/admin/products` and sees
  `UnitPrice` read-only; WarehouseUser cannot see `UnitPrice`
  anywhere.
- **Finding 5** partial scope:
  - `Customer.SocialSecurityNumber` appears in the Default Masks
    column dropdown.
  - The Data Column Restrictions **Add** button is enabled for
    SystemAdmin on a user-type role.
  - The Default Masks table dropdown uses singular entity names.
  - The safety-net feature is **removed** (no exit bullet for the
    in-circuit refresh bug, because the feature is gone).
- **Finding 6** (Organization mutation UX + in-circuit grid
  refresh) closed across Sessions 4 and 7: Session 4 delivers the
  in-circuit grid refresh after add / edit / archive and filter
  changes; Session 7 delivers the "new record" layout cue via the
  chosen master-details idiom (the Organization Details tab
  renames to "New" when a new record is staged under Idiom B, or
  the equivalent summary-expansion under Idiom A).
- **Finding 7** (master-details real-estate) closed: one idiom is
  chosen and applied uniformly to Role, Default Masks,
  Organization, and Users tabs; condensed grid with ~6-row default
  viewport.
- **Finding 8** (DisplayAlias column width + font parity) closed.
- **Finding 9** (tab-strip arrow-key navigation) closed by
  removal: the handler, the `HandleTabKeyDown`/`NextPanelIndex`
  logic, and all bUnit and Playwright assertions that cover the
  behaviour are deleted.
- The `security-dashboard-uat-remediation-uat-checklist.json` is
  committed in the **same commit** as its
  `tests/AcmeCoffeeOnline.Playwright/SecurityDashboardUatCoverageTests.cs`
  coverage file; every Review item in the checklist has a
  corresponding passing Playwright test.
- `dotnet build Dabbler.Platform.sln` and all non-Playwright tests
  green at set close.
- The full Playwright suite green at set close.
- The new "trust business + auditor backstop over auto-detection
  tooling" Principle remains in `docs/planning/project-guidance.md`
  unless the Step 9 review relocates it.
- Set D (`sensitive-field-masking-audit`) is unblocked and can
  start from a clean dashboard surface.
