# Change Log — security-dashboard-uat-followup-fixes

> **Session Set:** `docs/session-sets/security-dashboard-uat-followup-fixes/`
> **Completed:** 2026-04-21
> **Sessions:** 2 of 2
> **Purpose:** Remediate the four actionable findings from the
> `security-dashboard-uat-remediation` UAT review (two Blocker
> regressions against the preceding set's deliverables, plus two
> Minor UX/cosmetic defects).

This set runs between `security-dashboard-uat-remediation` and Set D
(`sensitive-field-masking-audit`) because the two Blockers were
regressions against deliverables Set D's masking rendering plugs into.
Set D is now unblocked.

Session 1 shipped all four fixes and a matching UAT checklist. Human
UAT found 3 of 4 fixes held, but Finding #2 reopened because the
Session 1 Playwright test used a dev-quick-signin scaffold path
rather than the real magic-link login flow the human actually takes.
Session 2 re-remediated Finding #2 by seeding a proper baked-in-User
persona and writing a Playwright test that drives the real login
sequence end-to-end.

---

### Session 1 of 2: Diagnose + fix + regression tests + UAT checklist

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

---

### Session 2 of 2: Finding #2 re-remediation via real-login flow

- **Summary:** The human UAT reviewer reported "Admin Drawer is not
  visible" for Finding #2 even after the Session 1 fix landed.
  Root cause: the Session 1 Playwright test signed in through a
  dev-quick-signin scaffold (`SeededSignInAsync` with a `roles="User"`
  override), which only exercises the roles-override code path. The
  reviewer signed in through the REAL login screen (email →
  magic-link → follow link), which resolves `jdoe@acmecoffee.com`
  against the DB and finds their seeded role `BackofficeUser` —
  correctly hiding the Admin drawer. No persona with a seeded
  baked-in `User` role existed, so the UAT path was structurally
  unreachable. This session plugs that gap.

  **Seeded baked-in-User persona.** `SeedData.GetSecurityUsers`
  inserts `juser@acmecoffee.com` ("Jamie Ng") at a deterministic
  position (between `customer@acmecoffee.com` and the retail-contacts
  loop) so juser's `AppUser.Id` lands at 11 on a fresh seed
  regardless of the 50-customer count. `GetOrganizationMemberships`
  gives juser an internal-org membership. Both Web and Api
  `user-roles.json` carry `"11": ["User"]` so
  `UserSessionContextService.BuildAsync` merges the baked-in `User`
  role into juser's effective roles when they authenticate.

  **Dev persona simplification.** The Session 1 jdoe+roles=User
  override persona is replaced with a real-seeded "Jamie Ng — User"
  persona that signs in via juser's email without any roles override.
  The override path (`dev-quick-signin.js` roles parameter and
  `DevPersona.RolesOverride`) remains in the codebase for future
  personas that genuinely cannot be DB-seeded, but the checklist no
  longer exercises it.

  **Destructive reseed.** `docker compose -f
  docker/docker-compose.infra.yml down -v` wiped the sqlserver
  volume, then `up -d` + app rebuild reseeded from scratch.
  Verified juser landed at Id=11 via direct SQL query.

  **Real-login Playwright test.**
  `SecurityNav_BakedInUserPersona_RealLoginFlow_NavVisibleRoleDocOnlyAsync`
  replaces the Session 1 shortcut test. The test clears the dev
  inbox, navigates to `/login`, fills the email input with juser@,
  clicks "Send sign-in link", opens `/dev/email-inbox` in a second
  Playwright page (shared browser context — same analogue as a
  human opening a second tab), scopes the row lookup to
  `tbody tr.mud-table-row` (the MudTable `thead` also carries
  `tr.mud-table-row`, so unscoped `.First` picked the header),
  asserts the anchor's `href` matches `^https?://.+/login\?token=`,
  follows that URL on the original page, and asserts Admin drawer
  visible + only Role Documentation tab.

  **Checklist Item #1 rewritten.** Six steps mirror the Playwright
  test action-for-action, with `TestData` carrying the literal
  selectors (`input[type='email']`, `tbody tr.mud-table-row`,
  `td[data-label='Link']`) and the expected href pattern.

- **Key Decisions:**
  1. **Destructive volume wipe over an ID-keyed workaround.** The
     alternative was to let juser land at whatever ID the running
     backfill gave them and discover the mapping at runtime —
     fragile, stateful, and a maintenance hazard for future Set D
     work. Seed-determinism recovers simplicity at the cost of
     30 seconds of reseed time.
  2. **DB-seed persona over retaining the override-only approach.**
     The override path is a scaffold for automated tests, not a
     user-facing sign-in surface. The human UAT flow rightly uses
     the real login screen. Making the baked-in User role reachable
     through that flow requires a DB user whose JSON role
     assignment is `User`.
  3. **Second Playwright page for the inbox.** Mirrors a human
     opening a second browser tab. Same browser context shares
     cookies so the magic-link navigation on page 1 still works,
     but the two DOM contexts don't contend for inbox rendering
     versus login confirmation rendering.

- **Verification:**
  - Non-Playwright tests: 661/0 green (same as Session 1 final —
    confirms the seed change doesn't break any unit/integration
    coverage).
  - Playwright (Docker): 127/0/1 green. The replaced test kept the
    suite at the same total because it replaced the Session 1 test
    rather than adding alongside it.
  - `uat-coverage-review` gate: VERIFIED on Session-2 round 2 (Opus
    generator, GPT-5.4 verifier). The fix for round-1 issue was to
    swap the xUnit `Assert.False` on the href to a Playwright-native
    `ToHaveAttributeAsync` regex match, aligning the checklist
    verification to a genuine Playwright assertion.
  - Session verification (Opus): PASS on all four review criteria
    — seed determinism, real-login test fidelity, regex strictness,
    separate-page state-leak analysis. Noted two minor observations:
    Id=11 is a magic number duplicated in both user-roles.json
    files (fragile to future seed insertions), and the href regex
    requires `token` to be the first query parameter. Neither
    blocks the fix.
