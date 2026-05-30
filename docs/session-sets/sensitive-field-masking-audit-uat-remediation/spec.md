# Sensitive-Field Masking and Audit — UAT Remediation Spec

> **Purpose:** Replace the unrunnable `sensitive-field-masking-audit` UAT
> (which relied on `SeededSignInAsync(roles=)` for every persona — a
> test-only shortcut with no human equivalent) with a real-login,
> role-switch-driven UAT and Playwright suite. Then audit the two
> overlapping user-management surfaces (`/admin/users` User Administration
> page vs. Security Dashboard's "Users and Role Assignment" tab) so the
> next persona-introducing session is not a fresh source of confusion.
> **Created:** 2026-04-27.
> **Session Set:** `docs/session-sets/sensitive-field-masking-audit-uat-remediation/`
> **Prerequisite:** `restore-devidp` set is COMPLETE (DevIdp serves OIDC
> at `https://localhost:5100`, the SSO button is enabled, the OIDC happy
> path is Playwright-covered).
> **Workflow:** Two sessions. Session 1 ships the real-login remediation;
> Session 2 ships an audit document for User Administration vs.
> Users-and-Role-Assignment, output is a disposition doc that may spawn a
> follow-up session set.

> **Reset note (2026-04-30):** Session 1 was previously executed on
> 2026-04-28 under the manual `*UatCoverageTests.cs` +
> `uat-coverage-review` pattern that the `uat-workflow-dsl-alignment`
> session set retired. The prior attempt's activity log,
> session-state, ai-assignment ledger, hand-authored UAT checklist,
> `uat-coverage-review` raw output, and saved session-001 verification
> review have been moved to `.session1-prior-attempt/` (see
> `.session1-prior-attempt/README.md`). The next "Start the next
> session of `sensitive-field-masking-audit-uat-remediation`" run
> begins Session 1 from scratch under the DSL pipeline described
> below. The hand-authored
> `tests/AcmeCoffeeOnline.Playwright/SensitiveFieldMaskingUatCoverageTests.cs`
> remains in the Playwright project until the redo's W0-compiled
> output (`SensitiveFieldMaskingUatTests.cs`) is green; the same
> commit that lands the compiled output deletes the hand-authored
> file.

## Session Set Configuration

```yaml
requiresUAT: true
requiresE2E: true
uatScope: full
```

UAT applies because Session 1 ships a human-runnable masking/audit
checklist; Session 2 produces a disposition doc and does not need its
own UAT (the audit findings either become a stub spec for a future set
or a one-commit micro-fix plan with its own UAT decision).

---

## Context

`sensitive-field-masking-audit` (Set D) shipped 2026-04-27 with a UAT
checklist that the human could not execute. The `SensitiveFieldMaskingUatCoverageTests.cs`
suite signs in via `SeededSignInAsync(...roles="BackofficeUser")`, etc.
That endpoint (`/test-auth/seeded-session?roles=...`) is a test-only
shortcut: no human can reach it in a browser, no UI control invokes it,
and the human's actual sign-in path produces a principal whose roles come
from `user-roles.json` and the JSON role store, not from a query
parameter.

This is the third occurrence of the same failure pattern across the
role-rearchitecture roadmap (see the
`Three-Step UAT Authoring Discipline` lesson in `lessons-learned.md` and
the matching Convention in `project-guidance.md` → Human UAT And
Checklist Editor). The remediation here is the literal application of
that discipline to Set D.

The human also flagged a real UX confusion separate from the sign-in
issue: the "User Administration" page (`/admin/users`) does not appear to
have parity with the master/detail UX of the Security Dashboard's "Role
Master Details" tab — no drill-in, no search, no rollup. It is unclear
whether `/admin/users` is redundant with the dashboard's "Users and Role
Assignment" tab, or whether the two are meant to differ. There is also
no clear user-creation surface visible from either. Session 2 audits
these surfaces and writes up a disposition.

## Design Commitments (Decided Up Front)

- **Real login only.** Session 1's Playwright suite uses the magic-link
  path through `/login` → `/dev/email-inbox` → magic-link follow, and/or
  the DevIdp-driven SSO path. No `SeededSignInAsync(roles=...)` overrides
  for any sign-in step. Test setup that pre-creates state outside the
  UAT's scope (e.g., seeding a customer with a known SSN) is allowed,
  but every human-equivalent step in the UAT must be driven through
  user-visible surfaces only.
- **One-user, multi-role persona.** `admin@acmecoffee.com` (or a new
  named persona, decided in Session 1) is assigned all three roles needed
  for the masking UAT — `SystemAdmin`, `BackofficeUser`,
  `CrossOrgMonitor` — at deterministic IDs in `user-roles.json` (per the
  `Dev-Persona Roles-Override` lesson Exception about deterministic seed
  IDs). The human signs in once, then uses the `UserContextBar` role
  switcher to move between scenarios.
- **Verify the role switcher works.** If the `UserContextBar` does not
  surface all three roles for the persona, that's a real bug to fix
  inline in Session 1 — not a test scaffold to bypass.
- **DSL-driven UAT pipeline** per
  `docs/ai-led-session-workflow.md` → *UAT/E2E pipeline*. Author the
  checklist in DSL via `route(task_type="uat-script-author")` against
  the component catalog for the active route × role; W0
  (`dabbler-uat-dsl compile`) emits the Playwright `.cs` test from
  the checklist; the W2 runner (`python ai-router/uat_runner/cli.py`)
  runs the compiled test deterministically. The W2 pass is the
  coverage proof. The retired `uat-coverage-review` task type is
  not used.

---

## Sessions
### Session 1 of 2: Real-login UAT remediation

**Purpose:** Make the Set D masking/audit UAT actually executable by a
human, by replacing every test-only shortcut with a real-login,
role-switch flow that the human can also perform.

**Steps:**

1. Read `docs/planning/project-guidance.md` (especially Human UAT And
   Checklist Editor), `docs/planning/lessons-learned.md`, and
   `docs/ai-led-session-workflow.md` → *UAT/E2E pipeline*.
2. Confirm `restore-devidp` is complete (`docs/session-sets/restore-devidp/change-log.md`
   exists; SSO button works on `/login`). If not complete, block this
   session and surface the dependency.
3. Assign `SystemAdmin`, `BackofficeUser`, and `CrossOrgMonitor` to
   `admin@acmecoffee.com` in `src/Apps/AcmeCoffeeOnline.Api/Data/user-roles.json`.
   Use deterministic IDs per the `Dev-Persona Roles-Override` lesson —
   insert before any variable-length loops in `SeedData.cs`. Verify
   the `UserContextBar` role switcher offers all three roles after a
   fresh sign-in. If the switcher omits any role, fix the surfacing
   bug inline (the role assignment is correct; the switcher's
   `_allowedSwitchRoles` filter or DI wiring is wrong).
4. Author the DSL checklist via
   `route(task_type="uat-script-author")` against
   `docs/component-catalog/index.json` for the active route × role.
   The real-login flow the checklist must encode is:
   - Magic-link login through `/login` → `/dev/email-inbox` →
     follow the magic-link href (the canonical pattern from
     `SecurityDashboardUatFollowupCoverageTests`), OR
   - DevIdp SSO if it lands in the same circuit (verify which path
     is more reliable in CI).
   - Role switching via the `UserContextBar` shell component, not
     via re-signing-in or roles-override.
   - Pre-test cleanup of any leftover BackofficeUser exemption is
     allowed (it's test setup, not a human-equivalent step), but
     it must use the **same UI flow** the UAT exemption-grant
     step uses — not a direct `security-config.json` edit, not a
     bypass endpoint.
   Validate the output against the canonical schema URL before
   committing.
5. **Compile the DSL checklist into a Playwright `.cs` via W0:**
   `dabbler-uat-dsl compile --catalog docs/component-catalog/index.json
   --suppressions tools/uat-suppressions.json --active-role <role>
   --emitter playwright-csharp --output
   tests/AcmeCoffeeOnline.Playwright/SensitiveFieldMaskingUatTests.cs`.
   Then run `dotnet build Dabbler.Platform.sln` and confirm the
   compiled `.cs` builds cleanly (any `.cs` that does not compile
   under dotnet is a W0 emitter bug — file an issue against W0 and
   stop; do not hand-edit the emitter output).
6. **Execute the W0-compiled test via the W2 runner and iterate
   until green:**
   ```bash
   python ai-router/uat_runner/cli.py \
     --checklist docs/session-sets/sensitive-field-masking-audit-uat-remediation/sensitive-field-masking-audit-uat-remediation-uat-checklist.json \
     --catalog docs/component-catalog/index.json \
     --session-set sensitive-field-masking-audit-uat-remediation
   ```
   The runner compiles, runs (`dotnet test`), and on failure
   classifies each via the four-class triage taxonomy
   (`APP_FUNCTIONAL_BUG`, `CATALOG_STALE`, `SEED_DATA_GAP`,
   `AMBIGUOUS_LOCATOR`). `CATALOG_STALE` auto-recovers; the other
   three classes file human-review issues under
   `docs/session-sets/{slug}/issues/`. **Loop:** read the filed
   issue, apply the targeted fix (app code for
   `APP_FUNCTIONAL_BUG`, fixture seed for `SEED_DATA_GAP`,
   `data-testid` + re-crawl + re-compile for `AMBIGUOUS_LOCATOR`),
   re-run the runner. Repeat until exit code 0. Do not advance to
   Step 7 until the runner reports a clean deterministic pass; do
   not silently `[Skip]` failing items.
7. **Run the full Playwright suite alongside the W0-compiled test**
   (Docker preferred, `docker-compose.app.yml` +
   `docker-compose.infra.yml`). Confirm no regression in any other
   `*UatCoverageTests.cs` from prior closed sets. Run the masking
   test twice for idempotency.
8. **Delete the legacy hand-authored
   `SensitiveFieldMaskingUatCoverageTests.cs`** now that
   `SensitiveFieldMaskingUatTests.cs` (W0-compiled) is the
   authoritative coverage. Confirm the build is still clean with the
   legacy file removed.
9. **Run the full non-Playwright suite** (`dotnet test
   Dabbler.Platform.sln --filter "FullyQualifiedName!~Playwright"`)
   and confirm green.
10. **Cross-provider session-verification** via
    `route(task_type="session-verification")`. Include the DSL
    checklist, the W2 runner output (last clean run), the
    `dotnet build`/`dotnet test` results, and pointers to the
    W0-compiled test file. The verifier should confirm: (a)
    catalog-grounded label discipline, (b) runner-pass coverage proof,
    (c) deletion of the legacy hand-authored file, (d) no
    framework-backdoor sign-in for any human-equivalent step.
11. **Commit, push, and send the human-UAT handoff notification.**
    The notification body MUST name:
    - the path to the DSL checklist
      (`docs/session-sets/sensitive-field-masking-audit-uat-remediation/sensitive-field-masking-audit-uat-remediation-uat-checklist.json`),
    - the checklist editor URL
      (`https://darndestdabbler.github.io/uat-checklist-editor/`),
    - the explicit message that work is paused on the human's
      review and downstream sets are blocked unless the human
      explicitly overrides.
    Per Rule #9, the next session of any downstream set
    (notably `transactional-system-columns`) does not start until
    the human completes the masking-audit UAT review.

**Creates:**
- `docs/session-sets/sensitive-field-masking-audit-uat-remediation/sensitive-field-masking-audit-uat-remediation-uat-checklist.json`
  (DSL-authored, freshly generated by `uat-script-author`).
- `tests/AcmeCoffeeOnline.Playwright/SensitiveFieldMaskingUatTests.cs`
  (W0-compiled output of the DSL checklist).

**Touches:** `user-roles.json`, possibly `SeedData.cs` (deterministic
IDs), possibly `UserContextBar.razor[.cs]` (role-switcher fix if
needed). The hand-authored
`tests/AcmeCoffeeOnline.Playwright/SensitiveFieldMaskingUatCoverageTests.cs`
is **deleted** in Step 8 once the W0-compiled output is green.

**Ends with:** A human can sign in once, switch through three roles,
and execute every checklist step in a real browser. The W0-compiled
Playwright suite drives the same path through the same locators (it
*was* the DSL checklist before W0 compiled it). The legacy
hand-authored coverage file is gone; the W2 runner reports a
deterministic pass; the human is notified with the checklist path
and editor URL.

**Progress keys:** `session-1/read-planning`,
`session-1/devidp-prereq`, `session-1/multi-role-persona`,
`session-1/role-switcher-verify`,
`session-1/dsl-checklist-author`, `session-1/w0-compile`,
`session-1/w2-runner-iterate-to-green`,
`session-1/full-playwright-suite`,
`session-1/delete-legacy-coverage-file`,
`session-1/full-non-playwright-suite`, `session-1/verification`,
`session-1/commit-push-uat-handoff`.

---

### Session 2 of 2: User Administration vs. Users-and-Role-Assignment audit

**Purpose:** Resolve the human's open question — is `/admin/users`
redundant with Security Dashboard → Users-and-Role-Assignment, do they
intentionally differ, and how does a person actually add a user or
change a user's role through the UI? Session 2 produces a disposition
doc; any code change is a follow-up session set.

**Steps:**

1. Read planning docs.
2. Catalogue the two surfaces:
   - `/admin/users` (User Administration page) — what it renders,
     what controls it exposes, what role gates it.
   - Security Dashboard → "Users and Role Assignment" tab — same.
3. For each user-task the role model supports (add user, deactivate
   user, assign role to user, revoke role from user, search users,
   drill into a user's detail), document which surface supports it,
   how, and whether the support is parity-equivalent to Role Master
   Details.
4. Surface gaps:
   - Master/detail parity: does Users have search, rollup, drill-in?
   - Add-user flow: does either surface let an admin create a new
     user, and if so, where?
   - Role-modification flow: where does the admin click to add or
     remove a role from a user?
   - Redundancy: are there tasks both surfaces support? Is the
     duplication intentional?
5. Write a disposition doc at
   `docs/session-sets/sensitive-field-masking-audit-uat-remediation/user-admin-audit.md`
   with: (a) catalogue table, (b) gap list with severity, (c)
   recommended remediation — one of (i) consolidate the two surfaces
   (with which winning shape), (ii) differentiate them more
   explicitly with cross-links and clearer titles, (iii) defer with
   a stub spec for a future set.
6. Routed analysis review of the disposition for sanity-check.
7. Commit + push + notify the human with the disposition doc as the
   handoff.

**Creates:** `docs/session-sets/sensitive-field-masking-audit-uat-remediation/user-admin-audit.md`
**Touches:** Likely nothing else (no code changes).
**Ends with:** A clear disposition for User Administration vs.
Users-and-Role-Assignment in the human's hands — either a follow-up
session set is queued, or the human accepts the differentiation as
intentional.
**Progress keys:** `session-2/read-planning`, `session-2/catalogue`,
`session-2/gap-analysis`, `session-2/disposition`,
`session-2/routed-review`, `session-2/commit-push-notify`,
`session-2/step-9-reorg` (last session of the set).

---

## What This Set Does NOT Cover

- The DevIdp restoration itself — that's `restore-devidp`, a
  prerequisite.
- Net-new masking/audit features. Set D shipped the feature; this set
  fixes the UAT.
- Any User Administration UI changes recommended by Session 2's
  audit — that's a follow-up set.

## Risk Notes

- **Role-switcher gap.** It is plausible the `UserContextBar` does
  not currently surface multi-role switching for a persona that has
  only baked-in roles assigned, or that the switcher's
  `_allowedSwitchRoles` filter excludes some roles by design. Session
  1 must verify by inspection AND with a real browser; if the
  switcher is the gap, that's an inline fix scoped to this session.
- **Magic-link vs. DevIdp.** Both are real-login paths. The
  magic-link flow is already covered by
  `SecurityDashboardUatFollowupCoverageTests` and may be the lower-risk
  choice. Session 1 picks one and documents the rationale.
- **Pre-test cleanup discipline.** The prior Set D Session 5 used
  direct `security-config.json` edits as a fallback for pre-test
  cleanup; that's exactly the kind of shortcut the new convention
  forbids when the cleanup is in the human-equivalent path. Session
  1's pre-cleanup must use the same Mask Exemptions sub-tab UI flow
  the UAT-2 grant step uses.
- **Customer.SSN seed data.** If the local DB doesn't have the
  seeded `123-45-6789` for customer ID 1 (because the column was
  added via `EnsureAcmeCoffeeSchemaAsync` to an existing DB without
  reseeding), the masked-text assertion has to either (a) wipe and
  re-seed the DB before the test, (b) set the SSN via the API
  before the test, or (c) keep the relaxed `XXX-XX-` prefix
  assertion. Decide explicitly in Session 1.
