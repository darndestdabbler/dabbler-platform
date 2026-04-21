# Security Dashboard UAT Follow-up Fixes — Stub Spec

> **Purpose:** Remediate the four actionable findings from the
> `security-dashboard-uat-remediation` UAT review: two Blocker-severity
> regressions against deliverables of the just-closed set, and two
> Minor UX/cosmetic defects.
> **Created:** 2026-04-21 *(stub — fleshed out at the start of Session 1)*
> **Session Set:** `docs/session-sets/security-dashboard-uat-followup-fixes/`
> **Prerequisite:** `security-dashboard-uat-remediation` complete
> (change-log 2026-04-21) and UAT reviewed
> ([`../security-dashboard-uat-remediation-review/disposition.md`](../security-dashboard-uat-remediation-review/disposition.md)).
> This set runs **before Set D (`sensitive-field-masking-audit`)**
> because its two Blockers are regressions against deliverables Set D's
> masking work plugs into.
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Status

**Stub.** Session 1 of this set is planning + diagnosis + fix. A
second session will be added only if diagnosis reveals the two
Blockers are unrelated problems that exceed a one-session fix budget.
The spec fleshes out at the start of Session 1 per the
`security-dashboard-uat-remediation` pattern.

## Scope

Four findings, drawn from the disposition artifact:

### 1. [Blocker] Admin drawer not visible for baked-in `User` (finding #2)

- **What the reviewer saw:** Signed in as baked-in `User` (e.g.,
  `cashier@acmecoffee.com` or equivalent), expected the Security
  Dashboard nav entry to appear with only the "Role Documentation" tab
  accessible.
- **What happened:** "Admin Drawer is not visible."
- **Expected deliverable (from Set remediation Session 3 change log):**
  *"Made `/admin/security` reachable by every baked-in role while
  preserving per-tab privilege gating. SystemAdmin sees five tabs,
  CrossOrgUserAdmin sees three, BackofficeUser sees only Role
  Documentation."* Baked-in `User` should fall under the same
  reachability rule as BackofficeUser (Role Documentation only).
- **Hypotheses to rule out in order:**
  1. **MudNavMenu/security-config regression.** Between Session 3 and
     Session 10 of the just-closed set, did any edit to
     `security-config.json`, `user-roles.json`, or
     `DabblerSecurityDashboard.razor` drop baked-in `User` from the
     Security nav reachability set?
  2. **`AdminDrawer` gating vs. `Admin ▸ Security` nav entry.** The
     reviewer said "Admin Drawer" — is the entire `MudDrawer` for
     `/admin/*` hidden for `User`, or just the Security item inside
     it? This distinguishes drawer-level from item-level gating.
  3. **Stale browser state.** Unlikely but cheapest to rule out
     first: clear cookies and re-run.
- **Diagnostic entry points:**
  - `src/Libs/Dabbler.Security.Blazor/Components/Admin/DabblerSecurityDashboard.razor`
  - `src/Apps/AcmeCoffeeOnline.Web/Components/Layout/MainLayout.razor`
    (or wherever the `MudDrawer` + `MudNavMenu` live)
  - `src/Apps/AcmeCoffeeOnline.Web/security-config.json`
  - `src/Apps/AcmeCoffeeOnline.Web/user-roles.json`
  - Playwright coverage: the
    `SecurityDashboardUatCoverageTests` tests that hit Shell and
    Access / Per-Role Security Nav Visibility for each baked-in role.

### 2. [Blocker] Create-organization does not refresh the grid (finding #11)

- **What the reviewer saw:** Created a new organization through the
  Create dialog.
- **What happened:** "The new organization is not visible in the grid."
- **Expected deliverable (from Set remediation Session 4 change log):**
  *"Implemented in-circuit grid refresh on Organization Master Details
  (add / edit / archive + filter change) via the same `IDbContextFactory`
  pattern Session 2 landed."* And Session 7 added the `_isCreatingNew`
  latch + shell summary for the create-state, confirming the flow was
  still exercised in the refactor.
- **Hypotheses to rule out in order:**
  1. **Shell-refactor regression.** Session 7's
     `MasterDetailExpansionShell` refactor could have disrupted the
     grid-refresh wire-up (the shell now mediates collapse/expand on
     row-select and filter-change; the create-refresh path may have
     been lost in the refactor).
  2. **`IDbContextFactory` vs. EF tracking.** The newly-created org
     may be persisted in one DbContext while the grid queries from a
     separate factory-resolved context that still has a stale result
     set cached.
  3. **`StateHasChanged()` timing.** The create path may complete the
     save but never invoke the grid's reload method.
- **Diagnostic entry points:**
  - `src/Libs/Dabbler.Security.Blazor/Components/Admin/Organization/OrganizationMasterDetailsTab.razor`
  - `src/Libs/Dabbler.Security.Blazor/Components/Admin/Shared/MasterDetailExpansionShell.razor`
  - The Playwright test in `SecurityDashboardUatCoverageTests` that
    covers "Create organization refreshes grid in-circuit without
    browser reload" — it should currently be failing if the bug is
    deterministic; if it passes, the test does not match what the
    human exercised.

### 3. [Minor] Currency symbol incorrect on `/products` (finding #14)

- **What the reviewer saw:** While hitting the `/products` restriction
  seam page as BackofficeUser, noticed the currency symbol is not the
  expected one (likely `$` vs. the locale-appropriate symbol, but the
  reviewer did not specify).
- **Scope:** Confirm intended currency symbol, fix formatting on the
  product list + detail pages, add a narrow visual-regression
  assertion if practical.
- **Diagnostic entry points:**
  - `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Products/*.razor`
  - `src/Apps/AcmeCoffeeOnline.Web/appsettings.json` (locale / culture)

### 4. [Minor] Two clicks to collapse the expansion shell (finding #49)

- **What the reviewer saw:** With the `MasterDetailExpansionShell`
  expanded, clicking a cell in the first column of the grid should
  collapse the shell and select that row. Instead, it takes two
  clicks (first click selects the cell, second click collapses).
- **Scope:** Audit the first-column click handler in
  `MasterDetailExpansionShell.razor` (and consumers). Most likely the
  cell's first-click triggers the cell-level focus/edit event before
  bubbling to the row-select handler.
- **Diagnostic entry points:**
  - `src/Libs/Dabbler.Security.Blazor/Components/Admin/Shared/MasterDetailExpansionShell.razor`
  - `src/Libs/Dabbler.Security.Blazor/Components/Admin/Role/RoleMasterGrid.razor`
    (canonical consumer)
  - `tests/AcmeCoffeeOnline.Playwright/SecurityDashboardUatCoverageTests.cs` —
    the existing row-select/collapse tests may have passed because
    they clicked in a non-first column or relied on a row-level click
    helper that did not reproduce the human's click target.

---

## Session Plan (draft)

### Session 1 of 1: Diagnose + fix + regression tests + new UAT checklist

**Steps (to be fleshed out at the start of Session 1):**

1. Read planning docs + this spec + the disposition artifact.
2. Run the existing Playwright suite in full. Confirm which of the
   four findings surface as deterministic test failures and which
   require new diagnostic steps.
3. Finding #2 (Admin drawer): diagnose, fix, land regression test.
4. Finding #11 (in-circuit refresh): diagnose, fix, land regression test.
5. Finding #14 (currency symbol): fix + narrow visual assertion.
6. Finding #49 (double-click collapse): fix + bUnit/Playwright
   assertion that first-column click collapses in one click.
7. Generate a new UAT checklist
   `security-dashboard-uat-followup-fixes-uat-checklist.json`
   covering the four fixes. Use the structured `Steps` array schema
   with `E2ETestReference` pointing at the new regression tests.
   Correct the step URLs for items [8] and [9] from the prior
   checklist so those scenarios can be re-verified against the
   working URL.
8. Route `uat-coverage-review` on the new checklist vs. the test
   file; land the checklist only when it returns VERIFIED.
9. Build + test + full Playwright suite.
10. End-of-session verification via
    `route(task_type="session-verification")`.
11. Cost report, change-log.md, commit + push.
12. Send session-complete notification.
13. Step 9 reorganization proposals review (run after notify).

**Creates:**
- `tests/AcmeCoffeeOnline.Playwright/SecurityDashboardUatFollowupCoverageTests.cs`
  (or extensions to the existing `SecurityDashboardUatCoverageTests`)
- `docs/session-sets/security-dashboard-uat-followup-fixes/security-dashboard-uat-followup-fixes-uat-checklist.json`

**Touches:**
- Whichever files the diagnoses of #2 and #11 identify
- Product pages for currency formatting (#14)
- `MasterDetailExpansionShell.razor` for #49

**Ends with:** All four findings verified by automated test coverage,
a new UAT checklist queued for the human, and the roadmap clear to
proceed to Set D.

**Progress keys (tentative):**
- `session-1/read-guidance-and-disposition`
- `session-1/baseline-suite-run`
- `session-1/finding-2-admin-drawer`
- `session-1/finding-11-in-circuit-refresh`
- `session-1/finding-14-currency-symbol`
- `session-1/finding-49-double-click-collapse`
- `session-1/uat-plan-generation`
- `session-1/uat-coverage-review`
- `session-1/build-and-test`
- `session-1/session-verification`
- `session-1/change-log`
- `session-1/notify`
- `session-1/step9-reorganization`

---

## Risk Notes

- **If diagnosis of finding #2 or #11 uncovers a systemic refactor
  regression (rather than a narrow miswire),** Session 1 stops,
  the spec is expanded to a multi-session plan, and the human is
  consulted before proceeding.
- **Finding #33 (`ArrowRight does not switch tabs`)** was explicitly
  *not* scoped into this set — it had empty `Feedback` and was marked
  `Out of scope` pending reviewer clarification. The new UAT checklist
  for this follow-up set should either re-ask it with a clarified
  expectation or drop it if the Playwright keyboard-assertion covers
  the semantics.
- **The architectural question from finding #20** (inline-edit vs.
  row-click-to-details) is deferred to a separate design
  conversation; it does not affect this set's scope.
