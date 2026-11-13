# Change Log — admin-user-creation-flow-uat-remediation

**Session set:** `docs/session-sets/admin-user-creation-flow-uat-remediation/`
**Completed:** 2026-05-11
**Sessions:** 1 of 1
**Prerequisite closed:** `admin-user-creation-flow` (UAT-1 PASS, UAT-3 PASS; UAT-2/4 BLOCKED)

---

## Summary

Unblocked two blocked UAT items from `admin-user-creation-flow` and fixed the root cause
of the ID-brittleness that produced UAT-2's blocker.

---

## Changes

### Seed data — `manager@acmecoffee.com` UserAdmin persona

Added `manager@acmecoffee.com` (display name "Morgan Manager") as a new seeded
backoffice user in `SeedData.cs`:

- **AppUser** inserted after the 50 retail-contact loop so prior IDs 1–61 remain stable.
- **Internal Operations org membership** so the user can access the security dashboard.
- **BackofficeUserAdmin app-defined role** (gates dashboard access).
- **UserAdmin baked-in role** granted via `user-roles.json` (drives the dropdown
  restriction logic in `RoleAssignmentAuthority`).

### user-roles.json — integer-ID keys → email keys

**Root cause fix.** The prior JSON files used SQL identity integer IDs as keys
(`"1"`, `"62"`, etc.). Because `BackfillSecuritySeedDataAsync` inserts new users
via SQL identity at whatever value the counter sits at (raised by Playwright test runs),
the key for any newly seeded persona was non-deterministic across environments.

Both `Api/user-roles.json` and `Web/user-roles.json` are now keyed by email address:

```json
{
  "admin@acmecoffee.com":   [ "SystemAdmin", "BackofficeUser", "CrossOrgMonitor" ],
  "jdoe@acmecoffee.com":    [ "CrossOrgUserAdmin" ],
  "asmith@acmecoffee.com":  [ "CrossOrgUserAdmin" ],
  "mwilson@acmecoffee.com": [ "BackofficeUser" ],
  "kbrown@acmecoffee.com":  [ "BackofficeUser" ],
  "tjones@acmecoffee.com":  [ "BackofficeUser" ],
  "juser@acmecoffee.com":   [ "User" ],
  "manager@acmecoffee.com": [ "UserAdmin" ]
}
```

Email keys are stable across any environment — fresh seed, dirty dev DB, CI — without
caring about what integer ID the insert got.

### Library — `IUserRoleStore` lookup key changed to email

`UserSessionContextService.cs` and `AdminUserQueryService.cs` both previously called
`GetRolesAsync(user.Id.ToString(), ct)`. Changed to `GetRolesAsync(user.Email, ct)`
to match the new JSON file key format. `AdminUserRow.UserId` now carries the email
address (doc comment updated; rename to `UserEmail` is a known future clean-up).

All affected unit tests updated (`UserSessionContextServiceTests`,
`AdminUserQueryServiceTests`).

### UAT checklist — UAT-4 email and instructions

- Step 10 email: `audit-test@test.local` → `audit-uat-v2@test.local` (avoids
  collision with rows left by prior Playwright runs).
- Feedback: updated with docker reset instructions for any future collision.

---

## Re-UAT scope

Human runs **only** the two previously blocked items:

- **UAT-2** — sign in as `manager@acmecoffee.com` via magic-link; verify Initial Role
  dropdown excludes SystemAdmin and CrossOrgUserAdmin.
- **UAT-4** — use email `audit-uat-v2@test.local`; verify UserCreated audit row appears.

UAT-1 and UAT-3 remain `Passes: true` from the prior run.

---

## Next session set

`sensitive-field-masking-audit` — matches the master roadmap; all prerequisite sets
(A, B, UAT-remediation, followup-fixes, admin-user-creation-flow) are complete.
