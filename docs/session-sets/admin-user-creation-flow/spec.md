# admin-user-creation-flow

> **Purpose:** Implement an admin-driven user-creation flow for
> backoffice users. Today's only user-creation surface is `/register`,
> which is `[AllowAnonymous]` self-service for retail-business
> customers — there is no path for a SystemAdmin or UserAdmin to
> create a new backoffice colleague, a CrossOrgMonitor reviewer, or
> any other non-customer user. Users are seeded directly into the
> database; this set adds the missing UI + API.
> **Created:** 2026-05-01 *(stub — fleshed out at start of Session 1)*
> **Session Set:** `docs/session-sets/admin-user-creation-flow/`
> **Authoritative source:** [docs/session-sets/sensitive-field-masking-audit-uat-remediation/user-admin-audit.md](../sensitive-field-masking-audit-uat-remediation/user-admin-audit.md) → Option 1, component 6.
> **Prerequisite:** Recommended (not strictly required) that
> [admin-users-cross-links](../admin-users-cross-links/spec.md) ships
> first so the cross-link landscape is stable when this set adds an
> "Add User" affordance to the dashboard tab.
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Session Set Configuration

```yaml
requiresUAT: true
requiresE2E: true
uatScope: full
outsourceMode: first
```

(Detailed UAT scope reasoning below in the [UAT scope](#uat-scope) section.)

---

## Status

**This is a stub spec.** The substantive scoping comes from the
disposition doc cited above, but the design space is large enough
that Session 1 of this set should produce a fleshed-out spec
(per-session step lists, API surface design, UI layout sketches,
risk notes) before Session 2 starts implementation.

## Scope summary

Add an **"Add User" affordance** inside the Security Dashboard's
"Users and Role Assignment" tab so the canonical role-assignment
surface and the user-creation surface are one navigation away.
Constraints from the disposition:

1. **Backoffice users only.** This flow does NOT replace `/register`
   — `/register` remains `[AllowAnonymous]` self-service for
   retail-business customer accounts (which create a `Customer`
   record + an associated user). The admin flow creates a backoffice
   user with no `Customer` record.

2. **Same-workflow role + org-membership assignment.** The "Add
   User" form must include initial role assignment AND initial
   organization-membership assignment AS PART OF the creation
   workflow — NOT as separate post-creation steps. A backoffice user
   created without any roles or memberships is never useful, so the
   form should require both.

3. **Authorization rules.** The same `RoleAssignmentAuthority` that
   gates role assignment on the dashboard tab gates the initial role
   choices in the "Add User" form. UserAdmin is restricted to
   assigning users to organizations they themselves admin;
   CrossOrgUserAdmin and SystemAdmin can assign cross-org.

4. **Email-based onboarding.** New backoffice users are created with
   a placeholder credential and an email magic-link is sent so the
   user sets their own password / completes MFA enrollment on first
   sign-in. Reuse the existing `IMagicLinkService` and
   `IEmailSender` plumbing from `Dabbler.Security`.

## Open design questions for Session 1

- **API surface.** New endpoints under `AdminUserApiClient` /
  `AdminUserScopeService`? Or a new `BackofficeUserCreationService`?
  Decide before Session 1 ends.
- **Audit trail.** What audit-event-kind covers user creation?
  Likely a new entry on `SensitiveAccessEventKind`. Coordinate with
  `Dabbler.Security` audit infrastructure.
- **Form layout.** Modal vs. dedicated panel vs. wizard. The
  "Users and Role Assignment" tab uses the
  `MasterDetailExpansionShell` for user details — does "Add User"
  reuse that pattern or get its own affordance?
- **Self-service customer registration parity.** `/register`
  validates name + email + password + agreed-to-terms. The admin
  flow may legitimately skip some validation (e.g., the admin
  doesn't agree to terms on the new user's behalf) and substitute
  others (e.g., the admin selects which org the user belongs to).
  Document the diffs explicitly.

## UAT scope

```yaml
requiresUAT: true
requiresE2E: true
uatScope: full
```

Reasoning: This is a new user-facing feature with multi-step flow,
authorization gating per role, and email/onboarding side effects.
The UAT must exercise the happy path (SystemAdmin creates a
BackofficeUser in their org, the new user receives the magic-link
email, signs in, and shows up in the Security Dashboard's "Users
and Role Assignment" tab) AND the authorization-denial paths
(UserAdmin can't create cross-org; CrossOrgMonitor can't create at
all).

## Estimated session count

2-3 sessions, opus-class:
- **Session 1:** Flesh out the spec; design API + form layout;
  agree on the role + membership constraint shape.
- **Session 2:** Implement API + service layer + audit emissions;
  unit tests for `RoleAssignmentAuthority` interaction.
- **Session 3:** Implement Razor surface ("Add User" affordance +
  form + magic-link email send); UAT checklist; W2 runner pass;
  commit.

## Risk notes

- **Magic-link race with role assignment.** If the magic-link is
  sent before the role assignment is committed (e.g., a transaction
  rollback partway through), the new user signs in to a no-roles
  account and gets a confusing error. Design the workflow so the
  magic-link is queued and only dispatched AFTER all role +
  membership rows are committed. Consider a "user is provisioned"
  flag on `AppUser` that the magic-link send checks.
- **Email-collision UX.** What happens when the admin tries to
  create a user with an email that already exists (e.g., as a
  retail-business customer)? Decide explicitly: error,
  link-existing-user-with-new-role, or allow-multiple-roles-on-one-user.
- **Audit completeness.** Set D's audit infrastructure focuses on
  *read* events (FieldUnmaskedRead, ExemptionExpired) and
  role-switch events (RoleSwitch). User creation needs a new
  *write*-side audit kind. Plan the schema addition early so
  Session 2's API can emit cleanly.
- **Library scope.** The dashboard tab is in
  `Dabbler.Security.Blazor`; the "Add User" affordance most likely
  goes there too (so all dabbler-platform consumers get it). But
  the email-onboarding side effects depend on each tenant app's
  configuration of `IEmailSender`. Surface the boundary explicitly
  in the design doc.
