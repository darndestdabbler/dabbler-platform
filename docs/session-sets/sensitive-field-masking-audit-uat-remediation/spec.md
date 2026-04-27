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
- **Three-step UAT discipline + UAT/E2E DSL** per the Convention in
  `docs/planning/project-guidance.md` → Human UAT And Checklist
  Editor. Every step in the checklist is one primitive verb from the
  closed DSL verb list, parameterised by visible labels and visible
  text only. Every Review item carries an `Id` (GUID) and its E2E
  method has a header `UAT Item Id: <guid>` comment plus
  `// begin STEP NNN` block delimiters. The `uat-coverage-review`
  call uses the canonical prompt template shipped in
  `ai-router/prompt-templates/task-prompts.md` (which already
  enforces per-step reachability + DSL conformance + GUID pairing).

---

## Session Plan

### Session 1 of 2: Real-login UAT remediation

**Purpose:** Make the Set D masking/audit UAT actually executable by a
human, by replacing every test-only shortcut with a real-login,
role-switch flow that the human can also perform.

**Steps:**

1. Read `docs/planning/project-guidance.md` (especially the new
   three-step UAT discipline) and `docs/planning/lessons-learned.md`.
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
4. Rewrite `tests/AcmeCoffeeOnline.Playwright/SensitiveFieldMaskingUatCoverageTests.cs`
   so each [Fact] corresponds to one UAT Review item with a
   header `<summary>` block declaring `UAT Item Id: <guid>` and
   `// begin STEP NNN` comments delimiting each step block. Each
   step block uses the DSL (visible-label locators only — no
   `GetByTestId`, no row indices, no framework backdoors). The
   real-login path is:
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
5. Regenerate the UAT checklist via `route(task_type="uat-plan-generation")`
   from the rewritten test, with the prompt explicitly requesting
   the maximally-explicit human-action shape per the Convention.
   Validate the output against the canonical schema URL before
   committing.
6. Cross-verify with `route(task_type="uat-coverage-review")` —
   prompt MUST request per-step reachability confirmation in
   addition to the existing 1:1 step-match check. ISSUES_FOUND on
   any human-unreachable step blocks the handoff.
7. Run the Playwright suite (Docker preferred,
   `docker-compose.app.yml` + `docker-compose.infra.yml`). Confirm
   the rewritten test is green. Run twice for idempotency.
8. Run the full non-Playwright suite (`dotnet test
   Dabbler.Platform.sln --filter "FullyQualifiedName!~Playwright"`).
9. Cross-provider session-verification.
10. Commit + push + send notification.

**Creates:** Possibly nothing new — primarily edits.
**Touches:** `user-roles.json`, possibly `SeedData.cs` (deterministic
IDs), possibly `UserContextBar.razor[.cs]` (role-switcher fix if
needed), `SensitiveFieldMaskingUatCoverageTests.cs` (full rewrite),
`sensitive-field-masking-audit-uat-checklist.json` (full rewrite — the
file lives in the **prior** set's folder per the
"Generate a new UAT checklist per session set" convention; this
remediation set updates it in place because the masking UAT itself is
not a fresh set).
**Ends with:** A human can sign in once, switch through three roles,
and execute every checklist step in a real browser — and the
Playwright suite drives the same path through the same locators.
**Progress keys:** `session-1/read-planning`, `session-1/devidp-prereq`,
`session-1/multi-role-persona`, `session-1/role-switcher-verify`,
`session-1/playwright-rewrite`, `session-1/regenerate-checklist`,
`session-1/coverage-review`, `session-1/full-test-run`,
`session-1/verification`, `session-1/commit-push-notify`.

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
