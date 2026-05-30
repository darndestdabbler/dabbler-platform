# admin-user-creation-flow

> **Purpose:** Implement an admin-driven user-creation flow for
> backoffice users. Today's only user-creation surface is `/register`,
> which is `[AllowAnonymous]` self-service for retail-business
> customers — there is no path for a SystemAdmin or UserAdmin to
> create a new backoffice colleague, a CrossOrgMonitor reviewer, or
> any other non-customer user. Users are seeded directly into the
> database; this set adds the missing UI + API.
> **Created:** 2026-05-01 *(fleshed out 2026-05-11 at start of Session 1)*
> **Session Set:** `docs/session-sets/admin-user-creation-flow/`
> **Authoritative source:** [docs/session-sets/sensitive-field-masking-audit-uat-remediation/user-admin-audit.md](../sensitive-field-masking-audit-uat-remediation/user-admin-audit.md) → Option 1, component 6.
> **Prerequisite:** [admin-users-cross-links](../admin-users-cross-links/) shipped 2026-05-11 (cross-link surface is stable). Verified via [tests/AcmeCoffeeOnline.Playwright/AdminUsersCrossLinksUatTests.cs](../../../tests/AcmeCoffeeOnline.Playwright/AdminUsersCrossLinksUatTests.cs).
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Session Set Configuration

```yaml
requiresUAT: true
requiresE2E: true
uatScope: full
outsourceMode: first
```

Rationale: New user-facing feature with multi-step flow, authorization
gating per role, magic-link side effects, and audit emissions. UAT
must exercise the happy path (SystemAdmin creates a BackofficeUser,
the magic-link arrives, the user signs in and appears in the
dashboard's Users-and-Role-Assignment tab) AND the denial paths
(UserAdmin cross-org rejection; CrossOrgMonitor full denial;
duplicate-email rejection).

---

## Design decisions (locked at Session 1 close)

| Question | Decision |
|---|---|
| Form layout | **Modal dialog** triggered by an "Add User" button at the top of the Users-and-Role-Assignment tab. Uses `MudDialog` + `MudForm` per existing platform conventions. |
| API surface | **New `BackofficeUserCreationService`** in `Dabbler.Security` orchestrates the AppUser + AppUserRole + AppUserOrganizationMembership writes in a single EF transaction, then enqueues the magic-link send. Web-side calls go through the existing `AdminUserApiClient` (new `CreateBackofficeUserAsync` method) hitting a new `POST /api/admin/users` endpoint. |
| Email collision | **Reject** with a validation error. The modal surfaces "A user with this email already exists." Admin picks a different email. Does NOT silently link-or-extend (avoids accidental privilege escalation of a Customer-role user). |
| Magic-link sync | **`IsProvisioned` flag on AppUser** (default `false`). Magic-link send checks the flag and surfaces "Account still being provisioned" if false. `BackofficeUserCreationService` sets `IsProvisioned=true` as the final step of the EF transaction. |
| Audit event kind | **New `SensitiveAccessEventKind.UserCreated`** entry. Emitted **inside** the EF transaction (before `IsProvisioned=true` flips) so a crash between commit and audit-emit can never leave an unaudited user creation. Payload includes target email (masked in display), target user ID, granter user ID, initial role name, initial org membership ID. |
| Library scope | **`Dabbler.Security.Blazor`** owns the `CreateBackofficeUserDialog` component (so all platform consumers get the dialog). `BackofficeUserCreationService` lives in `Dabbler.Security`. The web-app side owns the API controller (`/api/admin/users` POST) and the `IEmailSender` configuration. |
| Self-service parity diffs | `/register` validates `Name + Email + Password + AgreedToTerms`; admin flow validates `DisplayName + Email + InitialRoleName + InitialOrganizationId`. Admin does NOT agree to terms on the new user's behalf (terms acceptance happens at the new user's first sign-in via magic-link landing page; document this in the new-user-onboarding flow's HumanAction text in the UAT). |

---

## Scope summary

Add an **"Add User" button** at the top of the Security Dashboard's
"Users and Role Assignment" tab. Clicking it opens a modal dialog that
captures display name, email, initial role, and initial organization
membership. On submit:

1. Client posts to `POST /api/admin/users` with the new-user fields.
2. The API controller delegates to `BackofficeUserCreationService.CreateAsync`.
3. The service validates: email is unique; the granter's
   `RoleAssignmentAuthority` allows assigning the requested role to a
   target in the requested org; the org exists and is active.
4. Inside a single EF transaction (in this order): insert `AppUser`
   with `IsProvisioned=false` (the column's default is `true` for
   all OTHER paths — this admin flow explicitly opts in to the
   provisioning gate); insert `AppUserOrganizationMembership`;
   insert `AppUserRole` keyed to the membership; emit
   `SensitiveAccessEventKind.UserCreated` (audit BEFORE the
   IsProvisioned flip so a crash between commit and audit-emit can
   never leave an unaudited user creation); update `IsProvisioned`
   to `true`; commit.
5. Enqueue magic-link send via `IMagicLinkService.IssueAsync` (which
   reads `IsProvisioned` and refuses for unprovisioned users — this
   guard catches the race where the magic-link queue is drained
   before the transaction commits).
6. Return the new user's projection to the client. Dialog closes.
   The Users-and-Role-Assignment tab refreshes its grid; the new
   user is visible.

The new user receives the magic-link email, clicks it, lands on
`/login?token=...`, and completes first-sign-in. They appear with
their assigned role in the dashboard immediately.

Constraints from the disposition:

1. **Backoffice users only.** This flow does NOT replace `/register`
   — `/register` remains `[AllowAnonymous]` self-service for
   retail-business customer accounts (which create a `Customer`
   record + an associated user). The admin flow creates a backoffice
   user with no `Customer` record.

2. **Same-workflow role + org-membership assignment.** The form
   requires both — a user with no role or no membership is never
   useful, so the validator rejects either omission.

3. **Authorization rules.** The same `RoleAssignmentAuthority` that
   gates role assignment on the dashboard tab gates the initial role
   choices in the form. UserAdmin is restricted to assigning users
   to organizations they themselves admin; CrossOrgUserAdmin and
   SystemAdmin can assign cross-org.

4. **Email-based onboarding.** New backoffice users are created with
   no credential. They sign in via magic-link at first use. The
   existing `IMagicLinkService` + `IEmailSender` plumbing in
   `Dabbler.Security` is reused. (The `IsProvisioned` flag is the
   only new piece.)

---

## Session Plan

### Session 1 of 3: Design + spec fleshing-out (this session)

**Steps:**
1. Read project-guidance, lessons-learned, session-set-authoring-guide.
2. Register session start.
3. Read the stub spec + the prerequisite (admin-users-cross-links) closeout state.
4. Walk the human through the 4 critical design questions
   (form layout, API surface, email collision, magic-link sync) and
   secondary decisions (audit event kind, library scope, self-service
   parity).
5. Write the fleshed-out spec (this file) with per-session step
   lists, API contract, form layout sketch, audit plan, risk notes.
6. Route `ai-assignment.md` analysis.
7. Route cross-provider verification of the design.
8. Author `disposition.json`.
9. Commit, push, mark complete, notify.

**Creates:**
- `docs/session-sets/admin-user-creation-flow/ai-assignment.md`
- `docs/session-sets/admin-user-creation-flow/disposition.json`
- `docs/session-sets/admin-user-creation-flow/session-reviews/session-001.md`
- `docs/session-sets/admin-user-creation-flow/activity-log.json`

**Touches:**
- `docs/session-sets/admin-user-creation-flow/spec.md` (this file)

**Ends with:** Fleshed-out spec committed; cross-provider verification VERIFIED; no code changes yet.

**Progress keys:** `session-1/spec-fleshed-out`, `session-1/ai-assignment-authored`, `session-1/verification`

---

### Session 2 of 3: API + service layer + unit tests

**Steps:**
1. Add `IsProvisioned` (`bool`, **default `true`**) column to `AppUser`
   via the existing schema initializer pattern (`EnsureAcmeCoffeeSchemaAsync`).
   The default is `true` to preserve backward compatibility — every
   existing AppUser (seeded, `/register`-created customers, any
   future passwordless-login flows) is treated as already provisioned
   so `IMagicLinkService.IssueAsync` does NOT reject them. The admin
   creation INSERT explicitly sets `IsProvisioned=false` and the
   transaction flips it to `true` at the end. No data backfill
   required because the default applies to all existing rows
   automatically.
2. Add `SensitiveAccessEventKind.UserCreated` to the
   `Dabbler.Security` audit enum.
3. Implement `BackofficeUserCreationService` in
   `Dabbler.Security/Services/`:
   - `CreateAsync(CreateBackofficeUserRequest, ClaimsPrincipal granter, CancellationToken)`
   - Validates: role-name-exists-in-config, org-exists-and-active,
     `RoleAssignmentAuthority.Evaluate` on the granter + proposed
     role + target org.
   - **Email uniqueness is enforced by the existing
     `IX_AppUsers_Email` unique index** (confirmed at
     `src/Libs/Dabbler.Security/Data/ModelBuilderExtensions.cs:46`).
     The service does NOT pre-check email uniqueness — instead it
     attempts the INSERT and catches `DbUpdateException` whose
     inner is a unique-constraint violation, mapping it to a
     domain-specific `EmailAlreadyExistsException` that the
     controller translates to a 409 Conflict with the validation
     error message. This avoids the TOCTOU race a SELECT-then-INSERT
     check would have under concurrent admin requests.
   - Wraps `AppUser (IsProvisioned=false) +
     AppUserOrganizationMembership + AppUserRole + audit emission
     (inside the txn) + IsProvisioned=true flip` in a single
     `IDbContextTransaction`. Audit-emission-inside-the-transaction
     guarantees no orphan-uncovered creations.
   - Returns the new `AdminUserRow` projection.
4. Update `IMagicLinkService.IssueAsync` to read `IsProvisioned`
   and short-circuit with a domain-specific exception when false.
5. Add new endpoint `POST /api/admin/users` in
   `AcmeCoffeeOnline.Api/Controllers/AdminUsersController.cs`
   (create the controller if it doesn't exist). Maps to
   `BackofficeUserCreationService.CreateAsync`. **Controller-level
   authorization:** require the actor's effective baked-in role to
   be ∈ `{SystemAdmin, CrossOrgUserAdmin, UserAdmin}` (the same
   union the Security Dashboard's Users-and-Role-Assignment tab uses
   for tab visibility). CrossOrgMonitor and pure user-type actors
   get 403 at the controller before the service is even called.
   The service's `RoleAssignmentAuthority.Evaluate` check inside
   `CreateAsync` is defense-in-depth — it covers the case where a
   UserAdmin (allowed by controller) tries to assign a role to an
   org they don't admin (denied by authority). Implement the
   controller gate via an attribute or inline check on the actor's
   `IRoleSwitcher.EffectiveRoles` against `IBakedInRoleProvider`,
   matching the pattern in `DabblerSecurityDashboard.ResolveVisibleTabs`.
6. Add `CreateBackofficeUserAsync` to `AdminUserApiClient`
   (`AcmeCoffeeOnline.Web/Services/`).
7. Unit tests for `BackofficeUserCreationService` in
   `Dabbler.Security.Tests`:
   - Happy path (SystemAdmin creates a BackofficeUser).
   - UserAdmin cross-org rejection.
   - CrossOrgMonitor denial (cannot assign roles at all).
   - Duplicate email rejection.
   - Transaction rollback when role assignment fails authority at submit time.
8. Build + test; cross-provider verification; commit.

**Creates:**
- `src/Libs/Dabbler.Security/Services/BackofficeUserCreationService.cs`
- `src/Libs/Dabbler.Security/Services/IBackofficeUserCreationService.cs`
- `src/Libs/Dabbler.Security/Services/CreateBackofficeUserRequest.cs`
- `src/Apps/AcmeCoffeeOnline.Api/Controllers/AdminUsersController.cs` (if not present)
- `tests/Dabbler.Security.Tests/Services/BackofficeUserCreationServiceTests.cs`

**Touches:**
- `src/Apps/AcmeCoffeeOnline.Api/Data/DatabaseInitializer.cs` (`IsProvisioned` schema + backfill)
- `src/Libs/Dabbler.Security/Audit/SensitiveAccessEventKind.cs` (or wherever the enum lives)
- `src/Libs/Dabbler.Security/Services/IMagicLinkService.cs` + impl
- `src/Apps/AcmeCoffeeOnline.Shared/DTOs/CreateBackofficeUserDto.cs` (or similar)
- `src/Apps/AcmeCoffeeOnline.Web/Services/AdminUserApiClient.cs`

**Ends with:** Service + API + unit tests committed; W2 not run yet (no UI to exercise).

**Progress keys:** `session-2/schema-migration`, `session-2/audit-event-kind`, `session-2/service-impl`, `session-2/api-endpoint`, `session-2/api-client`, `session-2/unit-tests`, `session-2/build-test`, `session-2/verification`

---

### Session 3 of 3: Razor surface + UAT checklist + W2 + close

**Steps:**
1. Create `CreateBackofficeUserDialog.razor` in
   `Dabbler.Security.Blazor/Components/Admin/Users/`:
   - `MudDialog` host with `MudForm` content.
   - Fields: `DisplayName` (required), `Email` (required + regex),
     `InitialRoleName` (required, dropdown filtered by
     `RoleAssignmentAuthority` per the granter's active role —
     reuse the same logic as `UserRoleAssignmentDetails`),
     `InitialOrganizationId` (required, dropdown of orgs the
     granter can administer).
   - "Cancel" / "Create User" buttons. "Create User" disabled until
     form is valid. Posts to
     `AdminUserApiClient.CreateBackofficeUserAsync`. Shows snackbar
     on success / error. Closes dialog on success and raises
     `OnCreated` event so the Users tab can refresh.
2. Add "Add User" `MudButton` to `UsersAndRoleAssignmentTab.razor`
   at the top of the tab content. Visible to all actors who can
   see the tab (SystemAdmin / CrossOrgUserAdmin / UserAdmin). Opens
   the dialog via `IDialogService.ShowAsync`.
3. Update `AuthenticationStateProvider`-based actor-context
   resolution (introduced in `admin-users-cross-links` Session 1)
   to also flow through the new dialog. The dialog injects
   `AuthenticationStateProvider` and passes the principal through
   to the role/org-dropdown filtering logic.
4. bUnit tests in `Dabbler.Security.Blazor.Tests`:
   - Dialog renders with empty fields.
   - Submit-button stays disabled while form invalid.
   - Authorization filter excludes roles the granter can't assign.
   - Successful submit calls `AdminUserApiClient` once and raises
     `OnCreated`.
5. Author UAT checklist via `route(task_type="uat-script-author")`
   following the project-guidance Convention:
   "each Review item is a self-contained user journey starting from
   `/login`; no item may consist solely of `VERIFY` steps."
6. Author hand-rolled Playwright `[Fact]` tests covering the same
   scenarios as the UAT checklist (per the memory
   `feedback_e2e_verify_before_uat.md`): SystemAdmin happy path,
   UserAdmin cross-org rejection, CrossOrgMonitor denial,
   duplicate-email rejection.
7. Run W0 compile + W2 runner (best-effort — note the W0 runner
   conflict with DSL-driven login flagged at
   `docs/session-sets/admin-users-cross-links/upstream-feedback-disposition-gate.md`).
8. Build + test; cross-provider verification; change-log;
   disposition.json; commit; push; notify.

**Creates:**
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Users/CreateBackofficeUserDialog.razor`
- `tests/Dabbler.Security.Blazor.Tests/Components/Admin/Users/CreateBackofficeUserDialogTests.cs`
- `tests/AcmeCoffeeOnline.Playwright/AdminUserCreationFlowUatTests.cs`
- `docs/session-sets/admin-user-creation-flow/admin-user-creation-flow-uat-checklist.json`
- `docs/session-sets/admin-user-creation-flow/change-log.md`
- `docs/session-sets/admin-user-creation-flow/disposition.json`

**Touches:**
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Users/UsersAndRoleAssignmentTab.razor` (Add User button)

**Ends with:** Set complete; UAT checklist handed to human; W2 deterministic pass (or documented blocker per the W0 runner conflict).

**Progress keys:** `session-3/dialog-impl`, `session-3/add-user-button`, `session-3/bunit-tests`, `session-3/uat-checklist`, `session-3/e2e-tests`, `session-3/w0-compile`, `session-3/w2-run`, `session-3/build-test`, `session-3/verification`, `session-3/change-log`, `session-3/closeout`

---

## Session Set Configuration

```yaml
requiresUAT: true
requiresE2E: true
uatScope: full
```

The Session 3 checklist covers four self-contained user journeys
(per the new project-guidance Convention):

- **UAT-1 (happy path):** SystemAdmin signs in via magic-link →
  Security Dashboard → Users tab → Add User → fills form
  (DisplayName, Email, Role=BackofficeUser, Org=Internal) → submits
  → confirms new user appears in the grid → opens `/dev/email-inbox`
  in a second tab → sees the magic-link email for the new user →
  follows the link → signs in as the new user → sees the role
  assigned.
- **UAT-2 (UserAdmin cross-org rejection):** UserAdmin signs in →
  Users tab → Add User → tries to pick an org outside their
  assignment → form blocks / submit returns 403 with a clear error
  message.
- **UAT-3 (duplicate email):** SystemAdmin → Add User → enters an
  email of an existing user → form surfaces "A user with this email
  already exists."
- **UAT-4 (audit emission):** SystemAdmin → Add User → completes
  flow → switches to CrossOrgMonitor role → Compliance Review tab →
  confirms a `UserCreated` audit row exists for the just-created user.
- **UAT-5 (CrossOrgMonitor denial):** CrossOrgMonitor signs in →
  navigates to the Security Dashboard → confirms the
  Users-and-Role-Assignment tab is visible (read-only) but the
  "Add User" button is either absent or disabled → confirms a
  direct POST to `/api/admin/users` (via test-auth backdoor or
  documented manual `curl` step) returns 403 with the controller
  authorization error.

---

## Risk notes

- **Magic-link race with role assignment.** Mitigated by the
  `IsProvisioned` flag — the magic-link send refuses for unprovisioned
  users. The flag flips true only when the EF transaction that writes
  the role + membership rows commits.

- **Magic-link enqueue failure after commit.** If `IMagicLinkService.IssueAsync`
  throws (email-service outage, queue unreachable) AFTER the EF
  transaction has already committed, the user record exists with
  `IsProvisioned=true` but no onboarding email was sent. The user
  can't sign in until someone re-issues the magic link. **Mitigation:**
  the controller catches the post-commit `IssueAsync` exception,
  returns 201 Created (the user WAS created) with a body that
  surfaces a non-fatal warning ("User created but email could not
  be sent: <reason>"). The dialog shows the warning in a snackbar
  with a "Re-send invitation" button that calls a new
  `POST /api/admin/users/{id}/resend-invitation` endpoint (out of
  scope for the initial 3 sessions; documented here for the
  followup set if/when needed). Until that endpoint ships, an admin
  can resend manually via the existing magic-link admin tooling
  (or recreate the user — the email is unique so the admin must
  first delete the partial user, which is a deliberate friction
  point that surfaces the failure to the operator).

- **Email-collision UX.** Resolved as "reject with validation error"
  per the design decisions table. No silent linking.

- **Audit completeness.** New `SensitiveAccessEventKind.UserCreated`
  is the first *write*-side audit kind in this codebase
  (`FieldUnmaskedRead`, `ExemptionExpired`, `RoleSwitch` are all
  read or context-change events). Schema for the audit row stays
  unchanged — the new kind populates the existing
  `AppSensitiveAccessLog` columns (`EventKind` string, `TargetEntity`
  = "AppUser", `TargetId` = new user's ID, etc.). No new columns
  needed.

- **Library scope.** `CreateBackofficeUserDialog` lives in
  `Dabbler.Security.Blazor` so platform consumers get the dialog.
  Each consuming app configures `IEmailSender` per their tenant — the
  dialog and the service are agnostic to the email transport.

- **Transaction-boundary correctness for the dropdown filter.**
  `RoleAssignmentAuthority.Evaluate` is called in two places: once
  client-side during dropdown population (read-only, advisory) and
  once server-side at submit time (authoritative gate). The
  client-side check is for UX only — a tampered request still gets
  rejected by the service. Mirrors the existing
  `UserRoleAssignmentDetails` defense-in-depth pattern.

- **bUnit / interactive Blazor parity.** The dialog uses
  `AuthenticationStateProvider` (not `IHttpContextAccessor`) for
  actor-context resolution, per the lesson learned in
  `admin-users-cross-links` Session 1. The bUnit tests register a
  test `AuthenticationStateProvider` stub that returns a configurable
  principal — same pattern as `UserManagementPageTests`.

- **Form-layout reuse for org switching.** SystemAdmin can assign a
  user to any org from the dropdown without first switching to that
  org. UserAdmin's dropdown is filtered to their assigned orgs.
  CrossOrgUserAdmin's dropdown shows all orgs (their assignment
  authority is cross-org). This matches the existing
  `UserRoleAssignmentDetails` org-context handling.
