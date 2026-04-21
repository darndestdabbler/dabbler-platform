# Change Log — security-dashboard-uat-followup-fixes

> **Session Set:** `docs/session-sets/security-dashboard-uat-followup-fixes/`
> **Completed:** 2026-04-21
> **Sessions:** 1 of 1
> **Purpose:** Remediate the four actionable findings from the
> `security-dashboard-uat-remediation` UAT review (two Blocker
> regressions against the preceding set's deliverables, plus two
> Minor UX/cosmetic defects).

This set runs between `security-dashboard-uat-remediation` and Set D
(`sensitive-field-masking-audit`) because the two Blockers were
regressions against deliverables Set D's masking rendering plugs into.
Set D is now unblocked.

---

### Session 1 of 1: Diagnose + fix + regression tests + UAT checklist

- **Summary:** Remediated all four findings in-scope, added matching
  Playwright regression tests, authored a new UAT checklist covering
  them, and cleared the `uat-coverage-review` gate after 5 iteration
  rounds. The three pre-existing Playwright failures surfaced on the
  baseline (DefaultMasks_AddPhoneMask,
  ColumnRestrictions_AddButton_EnabledForFreshPair,
  E2E_WF_05_AddsProductAfterKeyboardAndValidationChecks) were
  SecurityConfiguration-pollution artefacts in the long-running web
  container; they cleared with the container rebuild and are not
  scope for this set.

  **Finding #2 (Blocker) — Admin drawer hidden for baked-in `User`
  role.** Reviewer signed in as `jdoe` expecting baked-in User
  semantics, but `jdoe`'s seeded assignment is `BackofficeUser`
  (user-type role with a `/admin/security` Hidden UiRestriction) and
  no dev persona existed for the baked-in User path. Added a
  roles-override path to the dev-quick-signin helper:
  `dev-quick-signin.js` now accepts an optional `roles` URL parameter;
  `DevPersona` carries a nullable `RolesOverride`; `QuickSignInDialog.razor`
  and `Login.razor` send the override when present. New persona
  "Jordan Ellis — User (baked-in role override, Role Documentation
  only)" uses jdoe's email with `roles=User` — no DB seed change
  needed. The `X-Test-Auth-Secret` gate and the `AllowedRoles`
  allow-list in `appsettings.Development.json` still constrain what
  can be requested, so no new auth bypass surface.

  **Finding #11 (Blocker) — Create-org did not refresh the grid.**
  The Session-7 `MasterDetailExpansionShell` refactor left the shell
  collapsed after create/save, and the re-expand click in Step 9 of
  the UAT checklist was easy to miss. Fix:
  `OrganizationMasterDetailsTab.OnPersistedAsync` now flips
  `_isResultsExpanded = true` and clears `_summaryText` so the
  post-save grid is visible immediately. The existing
  `OrgMasterDetails_CreateOrganization_GridRefreshesInCircuitAsync`
  test updated to match (no manual re-expand click). Adjacent
  `OrganizationMasterDetails_NewOrganization_CollapsesShellWithNewSummaryAsync`
  still passes because its path stops before the save completes.

  **Finding #14 (Minor) — `/products` Unit Price rendered `¤` in
  Docker.** Linux containers start with POSIX locale, which makes
  .NET fall back to InvariantCulture — whose currency symbol is
  `¤`. Fix: both `AcmeCoffeeOnline.Web.Program.cs` and
  `AcmeCoffeeOnline.Api.Program.cs` now pin
  `CultureInfo.DefaultThreadCurrentCulture = en-US` before any
  requests, and Web additionally calls
  `UseRequestLocalization(en-US)` so request-scoped formatting
  inherits the same culture.

  **Finding #49 (Minor) — Two clicks to collapse the shell on
  first-column cell.** `RoleMasterGrid`, `OrganizationMasterGrid`,
  and `UsersMasterGrid` switched from `SelectedItemChanged` +
  `SelectedItem` to `RowClick` + `ReadOnly="true"`. `RowClick` fires
  on every row click regardless of prior SelectedItem state,
  eliminating the "first click sets cell focus, second click selects
  row" behaviour. The inline DisplayAlias editor in RoleMasterGrid
  remains fully interactive because `MudDataGrid.ReadOnly` controls
  the grid's built-in cell-editing mode, not the interactivity of
  custom CellTemplate contents.

  **Regression coverage:** `SecurityDashboardUatFollowupCoverageTests.cs`
  contains one test per finding, each mapping 1:1 to a Review item
  in the new
  `security-dashboard-uat-followup-fixes-uat-checklist.json`.

- **Key Decisions:**
  1. Chose roles-override over DB-seed change for Finding #2 to
     avoid the user-id-ordering fragility of inserting a new seed
     user (`user-roles.json` keys are integer user IDs, and seeding
     is order-dependent — an initial seed attempt produced exactly
     this collision).
  2. Went with auto-re-expand-after-persist for Finding #11 rather
     than a snackbar-only signal, because the reviewer's feedback
     was specifically about the refreshed grid not being visible —
     the shortest path to "the refresh IS visible" is to actually
     show the grid.
  3. Switched all three master grids (Role, Organization, Users) to
     `RowClick` even though only Role was mentioned in the UAT
     feedback, because the `SelectedItemChanged` → two-click failure
     mode is a grid-level concern that would recur on the other two
     if a future UAT clicks into a first-column cell.

- **Verification:**
  - Non-Playwright tests: 661/0 green (Dabbler.Components 33,
    Dabbler.Security 280, Dabbler.Security.Blazor 185,
    AcmeCoffeeOnline.Api.Tests 27, Dabbler.Reports.Blazor 13,
    AcmeCoffeeOnline.Web.Tests 123).
  - Playwright (Docker): 127/0/1 green (1 skip is the pre-existing
    MultiOrganization E2E_WF_02, inherited from the remediation
    set).
  - `uat-coverage-review` gate: VERIFIED on round 5 (Opus generator,
    GPT-5.4 verifier). Earlier rounds tightened checklist wording
    and test assertions into strict 1:1 parity (explicit drawer-
    visibility assertion, `ToHaveTextAsync` on the summary,
    grid-scoped row locators, `.mud-table-head` column-header
    assertion, `role-details-section` visibility assertion, native
    Playwright `ToContainTextAsync` / `Not.ToContainTextAsync` for
    currency-symbol checks).
  - Cross-provider session verification: PASS on all four findings
    including the cross-cutting safety checks (auth-bypass surface
    for the roles-override, culture-pin timing vs. Blazor Server
    circuit start, and create-flow compatibility with the existing
    collapse-on-create flow).
