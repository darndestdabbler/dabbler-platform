# admin-users-cross-links

> **Purpose:** Implement the disposition's Option 1 components 1-5 from
> the Set D UAT-remediation Session 2 audit: rename `/admin/users` to
> "Manage Organization Admins"; make it the canonical surface for
> UserAdmin grant/revoke; add org-context-aware cross-link from the
> Security Dashboard's "Users and Role Assignment" tab; add reverse
> cross-link from `/admin/users` to the dashboard tab; gate both
> banners by destination-surface authorization rather than actor's
> active role.
> **Created:** 2026-05-01 *(stub — fleshed out at start of Session 1)*
> **Session Set:** `docs/session-sets/admin-users-cross-links/`
> **Authoritative source:** [docs/session-sets/sensitive-field-masking-audit-uat-remediation/user-admin-audit.md](../sensitive-field-masking-audit-uat-remediation/user-admin-audit.md) → Option 1, components 1-5.
> **Prerequisite:** None (the disposition lives in a closed set; this
> set is independent).
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Status

**This is a stub spec.** The substantive design lives in the
disposition doc cited above. Before running Session 1, the
orchestrator should flesh this spec out with explicit step lists,
progress keys, and updated risk notes — particularly around the
shared-library impact on `Dabbler.Security.Blazor`.

## Scope summary

Implements 5 of the 6 components of the disposition's Option 1.
Component 6 (admin-driven user-creation flow) is its own session set —
see [docs/session-sets/admin-user-creation-flow/spec.md](../admin-user-creation-flow/spec.md).

1. **Rename `/admin/users` page title.** Change from "User
   Administration" to "Manage Organization Admins" (or
   "Promote to Organization Admin"). Keep the route to avoid breaking
   existing bookmarks/links. Single-file Razor edit on
   [src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Admin/UserManagement.razor](../../../src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Admin/UserManagement.razor).

2. **Make `/admin/users` the canonical surface for UserAdmin
   grant/revoke.** Modify the Security Dashboard's "Users and Role
   Assignment" tab to *exclude* `UserAdmin` from its assignable-role
   dropdown (or render it as a read-only chip with a "Manage in Manage
   Organization Admins" prompt). This is a change to
   [src/Libs/Dabbler.Security.Blazor/Components/Admin/Users/UserRoleAssignmentDetails.razor](../../../src/Libs/Dabbler.Security.Blazor/Components/Admin/Users/UserRoleAssignmentDetails.razor)
   in the published library. Changes to this file affect every
   downstream consumer of `Dabbler.Security.Blazor` — coordinate the
   change carefully and bump the library version.

3. **Org-context-aware cross-link from the dashboard tab.** When a
   SystemAdmin / CrossOrgUserAdmin selects a user in the dashboard tab
   from an organization that is not the actor's active org, the
   cross-link must either:
   - (a) Deep-link to `/admin/users?targetOrganizationId={id}` and
     have the target page resolve the active org from the query
     parameter, or
   - (b) Trigger the existing `/auth/switch-organization?organizationId={id}&returnUrl=/admin/users`
     redirect flow before landing.
   The cross-link's text should make the org-switch explicit (e.g.,
   "Switch to {OrgName} and manage admins") so the target-org
   context-change is not a surprise.

4. **Cross-link banner on `/admin/users`.** "Looking for full role
   assignment? Open the [Security Dashboard → Users and Role
   Assignment tab](/admin/security-dashboard?tab=users-and-role-assignment)."

5. **Authorization-correct cross-link visibility.** Each banner must
   be gated by **effective authorization for the destination surface**,
   not by the actor's active role alone. `/admin/users` uses
   `[DabblerPageAuthorize("/admin/users")]` + runtime
   `_scope.CanManageUsers`; the dashboard tab uses baked-in-role
   visibility + `RoleAssignmentAuthority`. Use `IPermissionService.CanAccessPageAsync`
   for `/admin/users` and the dashboard tab's existing visibility rule
   for the dashboard. A naïve role-only check yields dead links and
   confusing partial-access states.

## Session Set Configuration

```yaml
requiresUAT: true
requiresE2E: true
uatScope: full
```

> Rationale: Components 2 and 3 affect the SAME human-equivalent flows
> that the masking-audit UAT exercises (role-switching across orgs,
> clicking through to user-admin surfaces). The UAT must verify the
> canonical-surface change empirically — e.g., a SystemAdmin attempting
> to grant UserAdmin from the dashboard tab now sees the read-only chip,
> and the cross-link successfully lands them on `/admin/users` for the
> correct org.

## Sessions

### Session 1 of 1: Implement disposition Option 1 components 1-5

**Goal:** Mechanical Razor edits + one library bump + authorization-correct gating (sonnet-class).

**Scope:** Components 1-5 of the disposition's Option 1, as enumerated in the Scope summary above. Component 6 (admin-driven user-creation flow) is its own session set ([admin-user-creation-flow](../admin-user-creation-flow/spec.md)).

> **Stub session.** Before running this session, the orchestrator should flesh out per-step actions, progress keys, and risk-tightening notes from the disposition doc — particularly the shared-library impact on `Dabbler.Security.Blazor`.

## Risk notes

- **Shared-library coordination.** `UserRoleAssignmentDetails.razor`
  in `Dabbler.Security.Blazor` is a published library consumed by
  AcmeCoffeeOnline.Web (and potentially other tenant apps). The
  UserAdmin filter must be a parameter / configuration option — not a
  hard-coded rule — so apps that legitimately want UserAdmin in the
  dropdown can opt out. Default the parameter to "exclude UserAdmin"
  (since that's the AcmeCoffeeOnline.Web policy) but expose the
  override.
- **Org-switch redirect side effects.** The existing
  `/auth/switch-organization` flow does a full page reload and
  re-establishes the SignalR circuit. Any in-flight role-switch
  state on the dashboard's circuit is lost. Verify that the deep-link
  approach (option 3.a) preserves enough state for the user to
  resume — or document explicitly that the switch is one-way.
- **Banner-rendering authorization checks**. `IPermissionService.CanAccessPageAsync`
  is async; if the banner re-renders frequently it may cause perf
  issues. Cache the result per-circuit (similar to how
  `RestrictedFormField` caches `_resolved`).
