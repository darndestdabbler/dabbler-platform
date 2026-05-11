# admin-users-cross-links

> **Purpose:** Implement the disposition's Option 1 components 1-5 from
> the Set D UAT-remediation Session 2 audit: rename `/admin/users` to
> "Manage Organization Admins"; make it the canonical surface for
> UserAdmin grant/revoke; add org-context-aware cross-link from the
> Security Dashboard's "Users and Role Assignment" tab; add reverse
> cross-link from `/admin/users` to the dashboard tab; gate both
> banners by destination-surface authorization rather than actor's
> active role alone.
> **Created:** 2026-05-01 *(fleshed out at start of Session 1, 2026-05-11)*
> **Session Set:** `docs/session-sets/admin-users-cross-links/`
> **Authoritative source:** [docs/session-sets/sensitive-field-masking-audit-uat-remediation/user-admin-audit.md](../sensitive-field-masking-audit-uat-remediation/user-admin-audit.md) → Option 1, components 1-5.
> **Prerequisite:** None (the disposition lives in a closed set; this
> set is independent).
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Session Set Configuration

```yaml
requiresUAT: true
requiresE2E: true
uatScope: full
outsourceMode: first
```

Rationale: Components 2 and 3 affect the same human-observable flows
as the masking-audit UAT exercises (role-switching across orgs, clicking
through to user-admin surfaces). UAT must verify empirically that:
a SystemAdmin cannot grant UserAdmin from the dashboard dropdown; and
the cross-link successfully lands them in the correct org on
`/admin/users`.

---

## Scope summary

Implements 5 of the 6 components of the disposition's Option 1.
Component 6 (admin-driven user-creation flow) is its own session set —
see [docs/session-sets/admin-user-creation-flow/spec.md](../admin-user-creation-flow/spec.md).

1. **Rename `/admin/users` page title.** Change from "User Administration"
   to "Manage Organization Admins". Keep the route.

2. **Canonical UserAdmin surface.** Filter `UserAdmin` from the
   dashboard tab's assignable-role dropdown (`UserRoleAssignmentDetails.razor`).
   Expose an `ExcludeUserAdminAssignment` parameter (default `true`) so
   apps that legitimately want UserAdmin in the dropdown can opt out.
   When the parameter is `true`, render any currently-assigned UserAdmin
   chip as read-only (no close button) to prevent removal from this surface.

3. **Org-context-aware cross-link from the dashboard tab.** When a user
   is selected in `UserRoleAssignmentDetails.razor` and
   `ExcludeUserAdminAssignment = true`, show per-org links:
   "Manage org admins for {OrgName}" → `/auth/switch-organization?organizationId={orgId}&returnUrl=/admin/users`

4. **Cross-link banner on `/admin/users`.** A banner visible to actors
   whose active role can access the dashboard tab (SystemAdmin,
   CrossOrgUserAdmin, UserAdmin): "Looking for full role assignment?
   Open the Security Dashboard → Users and Role Assignment tab."
   Link: `/admin/security-dashboard?tab=users-and-role-assignment`

5. **Authorization-correct gating.** The cross-link in the dashboard
   tab (→ `/admin/users`) shows only when `_actorActiveRole ∈ {SystemAdmin,
   UserAdmin}`. The banner on `/admin/users` (→ dashboard) shows only when
   `_actorActiveRole ∈ {SystemAdmin, CrossOrgUserAdmin, UserAdmin}`. Values
   computed once on init and cached in component fields to avoid per-render
   async calls.

---

## Session Plan

### Session 1 of 1: Implement all 5 components + UAT checklist

**Steps:**
1. Flush out spec (already done — this step).
2. Read `project-guidance.md`, `lessons-learned.md` (done at session start).
3. Route `ai-assignment.md` analysis.
4. **Component 1:** Edit `UserManagement.razor` — rename `<PageTitle>`,
   `Typo.h4` heading, and description text.
5. **Component 2:** Edit `UserRoleAssignmentDetails.razor` — add
   `ExcludeUserAdminAssignment` parameter; filter UserAdmin from candidates
   in `RebuildAssignableRolesAsync`; render assigned UserAdmin chips as
   read-only when excluded; cache `_canManageOrgAdmins` flag.
6. **Component 3:** In `UserRoleAssignmentDetails.razor` details panel (when
   `ExcludeUserAdminAssignment = true` and `User != null`), add per-org
   cross-links using the org-switch redirect.
7. **Component 4:** In `UserManagement.razor`, inject `IAdminUserQueryService`,
   compute and cache `_showDashboardBanner`, render conditional banner linking
   to the dashboard tab.
8. **Component 5 / gating:** Gating applied inline in steps 5–7 above.
9. Route code review.
10. Author UAT checklist via `route(task_type="uat-script-author")`.
11. Compile checklist via W0 DSL CLI.
12. Run W2 UAT runner; resolve any failures.
13. Run `dotnet build && dotnet test`.
14. Cross-provider session verification via `route(task_type="session-verification")`.
15. Handle verification result.
16. Generate `change-log.md` via `route(task_type="summarization")`.
17. Cost report, commit, push, mark complete, notify.
18. Step 9 reorganization review.

**Creates:**
- `docs/session-sets/admin-users-cross-links/activity-log.json` (auto)
- `docs/session-sets/admin-users-cross-links/ai-assignment.md`
- `docs/session-sets/admin-users-cross-links/session-reviews/session-001.md`
- `docs/session-sets/admin-users-cross-links/admin-users-cross-links-uat-checklist.json`
- `tests/AcmeCoffeeOnline.Playwright/AdminUsersCrossLinksUatTests.cs`
- `docs/session-sets/admin-users-cross-links/change-log.md`

**Touches:**
- `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Admin/UserManagement.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Users/UserRoleAssignmentDetails.razor`

**Ends with:** `dotnet build` green, `dotnet test` green, UAT checklist W2 deterministic pass, change-log.md committed.

**Progress keys:**
`session-1/spec-fleshed-out`, `session-1/ai-assignment-authored`,
`session-1/component-1-rename`, `session-1/component-2-library-param`,
`session-1/component-3-cross-link-from-tab`, `session-1/component-4-banner-on-page`,
`session-1/code-review`, `session-1/uat-checklist-authored`,
`session-1/w0-compile`, `session-1/w2-run`,
`session-1/build-test`, `session-1/verification`

---

## UAT scope

```yaml
requiresUAT: true
requiresE2E: true
uatScope: full
```

Reasoning: Components 2 and 3 affect the SAME human-equivalent flows
that the masking-audit UAT exercises (role-switching across orgs,
clicking through to user-admin surfaces). The UAT must verify the
canonical-surface change empirically — e.g., a SystemAdmin attempting
to grant UserAdmin from the dashboard tab now sees the read-only chip,
and the cross-link successfully lands them on `/admin/users` for the
correct org.

---

## Risk notes

- **Shared-library change in integrated source.** `UserRoleAssignmentDetails.razor`
  in `Dabbler.Security.Blazor` is integrated source (not a published NuGet
  package); changes apply immediately to all consumers in this solution.
  The `ExcludeUserAdminAssignment` parameter defaults to `true` — the
  AcmeCoffeeOnline.Web policy — and can be overridden by any consumer that
  wants the old behavior.
- **Org-switch redirect side effects.** The existing `/auth/switch-organization`
  flow does a full page reload. In-flight role-switch state on the dashboard's
  circuit is lost. This is acceptable and expected for a cross-org navigation
  (the user clicked a link to explicitly switch orgs).
- **Auth gating via role name vs. DB check.** Using `_actorActiveRole` ∈ known
  role set rather than `IPermissionService.CanAccessPageAsync` (which requires
  an int userId DB roundtrip). Valid because the page-permission configuration
  in this deployment is aligned with the role model: SystemAdmin and UserAdmin
  always have `/admin/users` permission. This decision is disclosed in the
  end-of-session verification prompt.
