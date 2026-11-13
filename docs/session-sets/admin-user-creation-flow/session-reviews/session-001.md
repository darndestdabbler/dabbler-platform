# Verification Round 1

```json
{
  "verdict": "ISSUES_FOUND",
  "issues": [
    {
      "severity": "Critical",
      "description": "Internal contradiction on audit emission timing. Scope summary step 4 says audit is emitted INSIDE the EF transaction ('emit SensitiveAccessEventKind.UserCreated, set IsProvisioned=true, commit'). Design decisions table says 'Emitted by BackofficeUserCreationService once the transaction commits, before the magic-link enqueue.' These are mutually exclusive. For a security-sensitive write-side event, inside-the-transaction is the correct choice (crash between commit and audit-emit would leave an unaudited user creation). The spec must pick one unambiguously or Session 2 will guess.",
      "file": "docs/session-sets/admin-user-creation-flow/spec.md",
      "line": "Design decisions table → Audit event kind row vs. Scope summary step 4"
    },
    {
      "severity": "Major",
      "description": "IsProvisioned defaults to false, but the backfill only covers seeded emails ('UPDATE … SET IsProvisioned=true WHERE Email IN (seeded emails)'). Any users previously created via /register or other paths will have IsProvisioned=false. Session 2 step 4 modifies IMagicLinkService.IssueAsync to reject unprovisioned users — this breaks magic-link issuance for all non-seeded users if any existing flow (password reset, passwordless login) calls IssueAsync for them. Fix: either (a) change the column default to true and explicitly set false only in the admin creation INSERT, (b) backfill ALL existing users to true ('UPDATE AppUser SET IsProvisioned = true'), or (c) explicitly document that no existing flow calls IssueAsync for non-seeded users and that /register does not use magic links.",
      "file": "docs/session-sets/admin-user-creation-flow/spec.md",
      "line": "Session 2 step 1 and step 4"
    },
    {
      "severity": "Major",
      "description": "No database-level unique constraint on AppUser.Email is specified or confirmed to exist. The service-layer email-uniqueness check (SELECT then INSERT) is a classic TOCTOU race: two concurrent requests for the same email both pass the check, both INSERT, and one succeeds silently — creating a duplicate or throwing an unhandled DbUpdateException. The spec should either (a) mandate adding a UNIQUE INDEX on AppUser.Email in the schema step, or (b) confirm one already exists in the codebase, and (c) specify that the service catches the constraint-violation exception and maps it to the validation error.",
      "file": "docs/session-sets/admin-user-creation-flow/spec.md",
      "line": "Session 2 step 3 — 'Validates: email-uniqueness'"
    },
    {
      "severity": "Minor",
      "description": "UAT checklist lists 4 journeys (happy path, UserAdmin cross-org rejection, duplicate email, audit emission) but omits CrossOrgMonitor denial — despite Session 2 unit tests and Session 3 Playwright tests both explicitly including 'CrossOrgMonitor denial' as a scenario. Either add UAT-5 for CrossOrgMonitor denial or explain why it's covered only in automated tests.",
      "file": "docs/session-sets/admin-user-creation-flow/spec.md",
      "line": "UAT scope section"
    },
    {
      "severity": "Minor",
      "description": "API controller authorization is described as '[Authorize] + policy gate by baked-in admin role' but does not name the specific policy or enumerate which roles satisfy it (UserAdmin? CrossOrgUserAdmin? SystemAdmin? All three?). Session 2 step 5 implementor needs this to be unambiguous. The defense-in-depth pattern (policy + RoleAssignmentAuthority) requires the outer policy to be at least as permissive as the union of roles that could pass the inner authority check.",
      "file": "docs/session-sets/admin-user-creation-flow/spec.md",
      "line": "Session 2 step 5"
    },
    {
      "severity": "Minor",
      "description": "No compensating action specified for magic-link enqueue failure after the EF transaction has committed. If IssueAsync throws (email service down, queue unavailable), the user record exists with IsProvisioned=true but the new user never receives their onboarding email. The spec should acknowledge this failure mode and specify a mitigation (retry, admin-visible error with 're-send' button, or at minimum a documented risk note).",
      "file": "docs/session-sets/admin-user-creation-flow/spec.md",
      "line": "Scope summary step 5 / Risk notes section"
    }
  ]
}
```

---

# Verification Round 2

```json
{"verdict": "VERIFIED", "issues": []}
```

**Analysis of each Round 1 fix:**

1. **Audit emission timing (Critical):** Now stated identically in three places — design-decisions table, scope-summary step 4, and Session 2 step 3 — all specifying emission *inside* the transaction, *before* the `IsProvisioned` flip, *before* commit. No contradiction remains.

2. **IsProvisioned default (Major):** Session 2 step 1 explicitly sets column default to `true`, scope-summary step 4 parenthetically confirms it, and the admin INSERT explicitly opts into `false`. Backward compatibility for `/register`, seeded, and passwordless users is preserved without backfill. Clean.

3. **TOCTOU race (Major):** Session 2 step 3 eliminates the pre-check entirely, relying on `IX_AppUsers_Email` unique index and catching `DbUpdateException` → `EmailAlreadyExistsException` → 409. Correct pattern; race eliminated.

4. **CrossOrgMonitor denial UAT (Minor):** Added as UAT-5 with both UI-level (button absent/disabled) and API-level (direct POST → 403) verification. Also covered in Session 2 unit tests and Session 3 Playwright tests.

5. **API controller policy (Minor):** Session 2 step 5 now names the role set `{SystemAdmin, CrossOrgUserAdmin, UserAdmin}`, specifies the mechanism (`IRoleSwitcher.EffectiveRoles` against `IBakedInRoleProvider`), references the matching pattern (`DabblerSecurityDashboard.ResolveVisibleTabs`), and documents the defense-in-depth relationship with the service-level `RoleAssignmentAuthority.Evaluate`.

6. **Magic-link enqueue failure (Minor):** Risk notes now describe the full mitigation: controller catches post-commit `IssueAsync` exception, returns 201 with non-fatal warning body, dialog shows snackbar, re-send endpoint documented as followup scope.

No new contradictions introduced. The transaction step ordering (insert user → insert membership → insert role → emit audit → flip IsProvisioned → commit → enqueue magic-link) is consistent across all spec sections and correctly places the side-effect (email) outside the transaction boundary.
