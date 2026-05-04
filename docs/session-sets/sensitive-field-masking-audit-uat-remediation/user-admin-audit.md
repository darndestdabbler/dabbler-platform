# User Administration vs. Users-and-Role-Assignment — Disposition

> **Authored:** 2026-05-01 (Session 2 of 2 of `sensitive-field-masking-audit-uat-remediation`)
> **Question being answered:** Is the standalone `/admin/users` page redundant with the Security Dashboard's "Users and Role Assignment" tab? Do they intentionally differ? How does an admin actually create a user or change a user's roles through the UI?
> **Output type:** Disposition only — no code changes in this session. Recommended remediation is sized but the actual build lands in a follow-on session set if the human approves.

---

## TL;DR

The two surfaces are **NOT redundant** — they serve different scopes, but their names mislead the user into expecting parity.

| Surface | What it actually does | What its name suggests |
|---|---|---|
| `/admin/users` ([UserManagement.razor](../../../src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Admin/UserManagement.razor)) | **One narrow operation**: grant or revoke `UserAdmin` for an org membership | "Full user administration" |
| Security Dashboard → "Users and Role Assignment" ([UsersAndRoleAssignmentTab.razor](../../../src/Libs/Dabbler.Security.Blazor/Components/Admin/Users/UsersAndRoleAssignmentTab.razor)) | **General role assignment**: assign or revoke any role from any user, gated by `RoleAssignmentAuthority` | "Tab inside the dashboard" — sounds smaller than `/admin/users` |

The human's confusion is justified. The Security Dashboard tab is the broader, more capable surface; the `/admin/users` page is the narrower, more focused one. The naming and routing convey the opposite.

There is **no admin-driven user-creation flow anywhere** — `/register` is `[AllowAnonymous]` self-service for retail-business accounts. Admins cannot create users. This is a real gap, called out below.

---

## Catalogue

### `/admin/users` (User Administration page)

[UserManagement.razor:1](../../../src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Admin/UserManagement.razor#L1)

| Aspect | Value |
|---|---|
| Route | `/admin/users` |
| Page-level gate | `[DabblerPageAuthorize("/admin/users")]` (DB-driven page permission) |
| Runtime gate | `_scope.CanManageUsers` — warns and hides grid if false |
| Data source | `AdminUserApiClient.GetScopeAsync()` → `GET /api/admin/users/scope` returns `AdminUserScopeDto` (active-org users + per-user `CanGrantUserAdmin` / `CanRevokeUserAdmin` flags) |
| Search | None explicit; `MudDataGrid Filterable="true"` provides client-side per-column filtering |
| Grid columns | DisplayName · Email · IsActive · Roles In Scope · Other Organizations · Actions |
| Drill-in | **None** — clicking a row does not navigate to a detail view |
| Add User | **None** |
| Per-row actions | Grant UserAdmin (visible if `CanGrantUserAdmin`) / Revoke UserAdmin (visible if `CanRevokeUserAdmin`) — both inline buttons |
| Role-management scope | **Only `UserAdmin` elevation/revocation** within the active organization. Cannot assign/revoke any other role. |
| API surface | `POST /api/admin/users/memberships/{id}/grant-user-admin`, `DELETE /api/admin/users/memberships/{id}/grant-user-admin` |

### Security Dashboard → "Users and Role Assignment" tab

[UsersAndRoleAssignmentTab.razor:1](../../../src/Libs/Dabbler.Security.Blazor/Components/Admin/Users/UsersAndRoleAssignmentTab.razor#L1) · [UserRoleAssignmentDetails.razor:1](../../../src/Libs/Dabbler.Security.Blazor/Components/Admin/Users/UserRoleAssignmentDetails.razor#L1) · [UsersMasterGrid.razor:1](../../../src/Libs/Dabbler.Security.Blazor/Components/Admin/Users/UsersMasterGrid.razor#L1)

| Aspect | Value |
|---|---|
| Tab visibility | Active baked-in role ∈ {SystemAdmin, CrossOrgUserAdmin, UserAdmin} |
| Detail-action gate | `RoleAssignmentAuthority.Evaluate()` per role; UserAdmin is restricted to assigned organizations |
| Data source | `IAdminUserQueryService.GetUsersAsync()` → projected `AdminUserRow` (UserId, DisplayName, Email, IsActive, Organizations[], Roles[]) |
| Search | **Yes** — FilterPanel with five searchable fields (Name, Email, Role, Organization, Active boolean). Cumulative AND; "X of Y users" count |
| Grid columns | Name · Email · Active Roles · Organizations · Status |
| Drill-in | **Yes** — `MasterDetailExpansionShell` collapses the grid on row select; detail panel renders below |
| Add User | **None** (users pre-exist) |
| Per-row actions | None directly; selection drives the detail panel |
| Role-management scope | **Any role** — assign via dropdown + Assign button; revoke via "X" on each role chip. Authority check before each call. |
| API surface | `IUserRoleStore` operations gated by `RoleAssignmentAuthority` |

### `/register` (the actual user-creation surface today)

[Register.razor:1](../../../src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Auth/Register.razor#L1)

| Aspect | Value |
|---|---|
| Route | `/register` |
| Auth | `[AllowAnonymous]` — public self-service |
| Layout | `AuthLayout` (the unauthenticated chrome) |
| Purpose | "Create the first account contact for a retail business" — i.e., creates a Customer record + an associated user, NOT a backoffice user |
| Admin equivalent | **None** |

---

## Per-task availability matrix

For each task the role model supports, where is it actually performed in the UI?

| Task | `/admin/users` | Dashboard "Users and Role Assignment" | `/register` | Notes |
|---|:---:|:---:|:---:|---|
| Create a backoffice user | ❌ | ❌ | ❌ | **Real gap.** |
| Self-register as retail-business customer | ❌ | ❌ | ✅ | Anonymous flow only |
| Search users | ⚠️ (per-column filter only) | ✅ (FilterPanel) | — | |
| List users in current org | ✅ | ✅ (filterable across orgs for cross-org roles) | — | |
| Drill into a single user's detail | ❌ | ✅ | — | |
| Assign role to user | ❌ (only `UserAdmin`) | ✅ (any role) | — | |
| Revoke role from user | ❌ (only `UserAdmin`) | ✅ (any role) | — | |
| Deactivate user | ❌ | ❌ | — | IsActive is read-only display in both surfaces |
| Modify user profile | ❌ | ❌ | — | No edit affordance anywhere |
| Grant `UserAdmin` for an org membership | ✅ | ⚠️ (technically possible — UserAdmin is a baked-in role you CAN assign, but the org-membership scoping is the dedicated path on `/admin/users`) | — | Same op, two routes |
| Revoke `UserAdmin` for an org membership | ✅ | ⚠️ | — | Same op, two routes |

✅ supported · ⚠️ partial · ❌ not supported

---

## Gap analysis

### Major gap: no admin-driven user creation

**Severity: Major.** No admin can create a user through the UI. Users either self-register at `/register` (which only creates retail-business customer accounts), or they're seeded into the database directly. There is no path for a SystemAdmin or UserAdmin to add a new backoffice colleague, a new CrossOrgMonitor reviewer, or any other non-customer user. The `[AllowAnonymous]` registration page does the wrong job for admin needs.

This isn't a defect in either of the two surfaces under review — it's an absent third surface.

### Major gap: misleading naming

**Severity: Major.** The `/admin/users` route reads as "the user administration page," implying the broad surface. In practice it does one thing: org-membership UserAdmin grant/revoke. The Security Dashboard tab is the actual user-administration surface — but its placement inside the dashboard makes it discoverable only to users who first navigate to the dashboard. A SystemAdmin opening `/admin/users` and looking for "the place I assign roles" gets a 50% answer (UserAdmin only) and walks away thinking the platform is broken.

### Minor gap: no cross-link between the two

**Severity: Minor.** Even if the user knows both surfaces exist, there's no link from one to the other. A SystemAdmin who arrives at `/admin/users` to grant UserAdmin won't see a hint that broader role assignment lives in the dashboard. A UserAdmin who navigates to the dashboard tab won't see a hint that the org-scoped UserAdmin elevation has its own dedicated page.

### Minor gap: master/detail parity

**Severity: Minor.** `/admin/users` is grid-only; the dashboard tab uses MasterDetailExpansionShell. Different UX shapes for different operations is defensible — the operations themselves differ — but the inconsistency contributes to the broader naming confusion.

### Not a gap, but worth noting: search surface

The dashboard tab's FilterPanel is genuinely better than the page's per-column filterable grid. Users searching by role or active state are well-served by the dashboard tab. Users grant-/revoke-toggling UserAdmin within a known small org membership list are well-served by the page.

---

## Recommended remediation

Three options, ordered by ambition:

### Option 1 (recommended): Differentiate explicitly with cross-links + clearer titles + canonical surface for UserAdmin

This is option (ii) from the spec ("differentiate them more explicitly with cross-links and clearer titles"), with structural additions surfaced by two rounds of cross-provider review of the disposition. The two surfaces stay separate, but the human's confusion is closed by:

1. **Rename the `/admin/users` page title** from "User Administration" to **"Manage Organization Admins"** (or "Promote to Organization Admin"). Keep the route to avoid breaking links. Avoid jargon-heavy titles — prefer user-task-oriented wording.
2. **Make `/admin/users` the canonical surface for UserAdmin grant/revoke.** The dashboard tab's role-assignment dropdown should *exclude* `UserAdmin` from its options (or render it as a read-only chip with a "Manage in Manage Organization Admins" prompt). This eliminates the functional duplication today: the SAME sensitive action is reachable from two places, complicating audit and training.
3. **Org-context-aware cross-link from the dashboard tab.** This is the gap a naïve cross-link would leave open: the dashboard tab is cross-organization (a SystemAdmin can select a user in any org), while `/admin/users` is scoped to the active organization. Sending the admin to `/admin/users` without context-preservation lands them in the active org, not the user's org. Concrete requirement for the follow-on session set: when a SystemAdmin / CrossOrgUserAdmin selects a user in the dashboard tab from an org that is not their active org, the cross-link must either (a) deep-link to `/admin/users?targetOrganizationId={id}` and have the target page resolve the active org from the query parameter, or (b) trigger the existing `/auth/switch-organization?organizationId={id}&returnUrl=/admin/users` redirect flow before landing. The cross-link's text should reflect the action ("Switch to {OrgName} and manage admins") so the org-switch is not a surprise. **Without this, Option 1 degrades cross-org workflows.**
4. **Add a cross-link banner** at the top of `/admin/users`: "Looking for full role assignment? Open the [Security Dashboard → Users and Role Assignment tab](/admin/security-dashboard?tab=users-and-role-assignment)."
5. **Authorization-correct cross-link visibility.** Each cross-link banner must be gated by **effective authorization for the destination surface**, not by the actor's active role alone. `/admin/users` uses `[DabblerPageAuthorize("/admin/users")]` + runtime `_scope.CanManageUsers`; the dashboard tab uses baked-in-role visibility + `RoleAssignmentAuthority`. The cross-links should consult those same gates (e.g., via `IPermissionService.CanAccessPageAsync` for `/admin/users` and the dashboard tab's existing visibility rule) rather than approximating "user has admin role." A naïve role-only check yields dead links and confusing partial-access states.
6. **File a stub spec** for an admin-driven user-creation flow. Place the stub at `docs/session-sets/admin-user-creation-flow-stub/spec.md`. The stub MUST scope the flow to **backoffice users** (not retail-business customer self-registration), MUST include initial role assignment AND initial org-membership assignment as part of the creation workflow (not as separate post-creation steps), and SHOULD live as an "Add User" affordance inside the dashboard's "Users and Role Assignment" tab so the canonical role-assignment surface and the user-creation surface are one navigation away from each other.

**Pros:** Closes the human's stated confusion. Eliminates the duplicate UserAdmin grant path (a real audit/training concern, not just cosmetic).
**Cons:** Two surfaces remain, which doesn't fully answer the underlying "should there be one?" question — but they now have non-overlapping responsibilities, so the answer becomes "yes, intentionally, because the authorization scopes differ."

**Honest sizing note:** This is *not* "smallest blast radius" — component #2 (canonical UserAdmin surface) requires changing `UsersAndRoleAssignmentTab` in the published `Dabbler.Security.Blazor` library to filter UserAdmin out of the dropdown, which is the same kind of shared-library impact previously called out as a Cons of Option 2. Components #3–#5 also require non-trivial work (org-context-aware deep links, authorization-correct gating). Option 1 is "smallest blast radius" *relative to consolidating the two surfaces into one* (Option 2), not relative to "no change."

### Option 2: Consolidate into one surface — "User Administration" inside the Security Dashboard

This is option (i) from the spec ("consolidate the two surfaces, with which winning shape").

1. Move the org-membership `UserAdmin` grant/revoke action **into the dashboard's "Users and Role Assignment" tab's detail panel**, under a clearly-labeled "Org Memberships" sub-section. The existing role-assignment dropdown handles the general case; the membership-scoped UserAdmin elevation gets its own affordance because it's tied to a specific organization rather than to the user globally.
2. **Retire `/admin/users`** — keep the route as a redirect to `/admin/security-dashboard?tab=users-and-role-assignment` for backward compatibility, then remove the redirect after one release.
3. Add the missing user-creation flow inline as the dashboard tab's "Add user" affordance.

**Pros:** Single canonical surface. Eliminates the naming confusion at its root.
**Cons:** Larger change. The dashboard tab is in a published library (`Dabbler.Security.Blazor`); merging the org-membership-scoped flow into it widens its responsibilities and may complicate downstream consumers. The page-permission model (`/admin/users` has its own `[DabblerPageAuthorize]` policy) needs rewiring.

### Option 3: Defer

This is option (iii) from the spec — accept that the two surfaces differ and write a stub spec for a future session set if the human ever wants to reopen it.

**Pros:** Zero immediate work.
**Cons:** Doesn't close the human's confusion. The next admin who hits this discovers the same gap.

### My recommendation

**Option 1 (differentiate + cross-links + canonical UserAdmin surface + add-user stub).** Reasons:

- The surfaces *legitimately* differ — `/admin/users` is org-membership-scoped, the dashboard is global-role-scoped. Forcing them into one surface conflates two distinct authorization scopes, and a sanity-review of this disposition (Gemini Pro 2026-05-01) noted that consolidation would also require merging two distinct authorization models (`[DabblerPageAuthorize]` page-permission for `/admin/users` and `RoleAssignmentAuthority` for the dashboard tab) — non-trivial security work.
- The human's actual stated confusion (in the BATON-from-Session-1 and the spec) is about **discoverability and naming**, not about the existence of two surfaces. Option 1 addresses that directly.
- Removing UserAdmin from the dashboard's assignable-role list (Option 1 component #2) is what makes the "they intentionally differ" answer hold up under scrutiny. Without it, a critic could reasonably ask "if the same UserAdmin grant is available in both surfaces, then they're partly redundant" — and they'd be right. With it, the surfaces have non-overlapping responsibilities, and "two surfaces" becomes a feature, not a smell.
- The missing user-creation flow is the most acute gap and is genuinely independent of the two-vs-one question. A stub spec lets the human prioritize it on its own merits.

If the human prefers Option 2 because they value canonical-single-surface UX, that's a defensible call — flag it explicitly so the follow-on spec can be sized correctly.

---

## Suggested follow-on session sets

If Option 1 is chosen:

- **`admin-users-cross-links`** — implements the title rename, the two cross-link banners, and routing-aware visibility. Low complexity, ~1 session.
- **`admin-user-creation-flow`** (stub) — designs and ships an admin-driven user creation flow. Medium complexity. To be sized on its own merits.

If Option 2 is chosen:

- **`admin-users-consolidation`** — merges `/admin/users` into the dashboard tab and retires the page. Medium-high complexity. ~1-2 sessions.

If Option 3 is chosen:

- No follow-on; close the audit and revisit if the issue resurfaces.

---

## Out of scope

- Any code changes. This session produces only this disposition doc.
- Backend authority changes (`RoleAssignmentAuthority`, `IAdminUserQueryService`, `AdminUserScopeService`). Both surfaces share the same authority backbone; the audit is purely about UI surfacing.
- The Org Master Details ↔ Users-and-Role-Assignment relationship. Out of scope per spec.
