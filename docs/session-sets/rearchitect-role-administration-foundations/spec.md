# Rearchitect Role Administration — Foundations

> **Purpose:** Replace the grant-based authorization model with the
> restriction-based model described in the
> [architecture doc](../rearchitecting-role-administration/rearchitecting-role-administration.md).
> Introduce the five baked-in roles, the `IBakedInRoleProvider` DI
> hook, the `DisplayAlias` field, the Default Masks / Mask
> Exemptions types (with time-bound `ExpiresAt`), and new
> authorization handlers that do not apply role inheritance.
> **Created:** 2026-04-19
> **Session Set:** `docs/session-sets/rearchitect-role-administration-foundations/`
> **Roadmap:** `C:\Users\denmi\.claude\plans\ok-i-did-some-abstract-blossom.md`
> **Prerequisite:** `pre-role-rearchitecture` branch pushed to origin
> (snapshot of today's grant-based model preserved for reference).
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Context

This is Session Set **A** of the four-set rearchitecture roadmap
(A → B → D → C). A is the load-bearing foundation: data model,
handlers, and the Principle that governs everything downstream.

After Set A, the Security Dashboard's seven old tabs will either
render errors or be hidden. That's acceptable — Set B rebuilds the
dashboard from scratch against the new model. App behavior for
users with `SystemAdmin` role continues to work; other roles will
see blank or minimal admin surfaces until Set B lands.

## Goals

1. Five baked-in roles (code constants + compiled privileges +
   `IBakedInRoleProvider` hook) replace the JSON-configured role
   list for non-user-type roles.
2. `RoleDefinition` gains `DisplayAlias`; `RoleDefinition.InheritsFrom`
   is deleted.
3. `SecurityConfiguration` flips to
   `{ UserTypeRoles, OrganizationSwitching, DefaultMasks, MaskExemptions }`.
   `ColumnRestriction`, `RowRestriction`, `UiRestriction`,
   `DefaultMaskRule`, `MaskExemption` types exist and round-trip
   through JSON serialization.
4. Authorization handlers consult baked-in role privileges first,
   then user-type restrictions. `RoleInheritanceHelper.ExpandRoles`
   has zero call sites in production code.
5. Seed configs (`security-config.json`, `user-roles.json`) migrated
   per the decided mapping (`BackofficeUserAdmin` →
   `CrossOrgUserAdmin`; `BackofficeUser`, `Customer` → app-defined
   user-type roles with `DisplayAlias`).
6. New Principle added to `docs/planning/project-guidance.md`
   Principles → Security And Organization Model.

## Architecture Context

Components touched (non-exhaustive):

- `src/Libs/Dabbler.Security/Configuration/SecurityConfiguration.cs`
  and siblings (heavy edits + deletions).
- `src/Libs/Dabbler.Security/Authorization/Handlers/*` (rewritten).
- `src/Libs/Dabbler.Security/Authorization/BakedInRoles.cs` (new
  constants class).
- `src/Libs/Dabbler.Security/Authorization/IBakedInRoleProvider.cs`
  (new DI contract).
- `src/Libs/Dabbler.Security/Services/RoleInheritanceHelper.cs`
  (left on disk; no callers after this set).
- `src/Apps/AcmeCoffeeOnline.Web/security-config.json` and
  `user-roles.json` (re-shaped).
- `docs/planning/project-guidance.md` (new Principle).

Reuse (confirmed by pre-flight Explore survey):

- `IRoleSwitcher` / `SessionRoleSwitcher` — circuit-lifetime role
  switching; no changes required by Set A.
- `AppOrganization`, `AppUserOrganizationMembership` — already
  temporal-configured in `Dabbler.Security`.
- `RowFilterDefinition` (existing type) — informs
  `RowRestriction` shape.
- `DabblerAuthorizationPolicyProvider` — policy-name interception
  stays; only the underlying handler logic changes.

### Decisions confirmed with the human (do not re-litigate)

All eight decisions from the roadmap's "Confirmed decisions"
section apply. Most relevant to this set:

- Hybrid baked-in roles: code constants + DI provider.
- Canonical names `SystemAdmin / CrossOrgUserAdmin /
  CrossOrgMonitor / UserAdmin / User`.
- CrossOrgUserAdmin can self-assign (vacation coverage).
- Binary `ColumnRestriction` kinds (`ViewOnly` | `Hidden`) — mask
  is NOT a kind here; masking lives in Default Masks / Mask
  Exemptions.
- `MaskExemption.ExpiresAt` is nullable; authority rules per
  target role (see architecture doc).
- Role inheritance is removed entirely; `RoleInheritanceHelper`
  stays on disk but unused.

---

## Session Plan

### Session 1 of 5: Baked-in role constants, privileges, DisplayAlias, Principle

**Steps:**
1. Read planning docs (`project-guidance.md`, `lessons-learned.md`).
2. Add new Principle to `project-guidance.md` under Principles →
   Security And Organization Model describing the whole new
   authorization model (see the architecture doc for wording).
3. Add `src/Libs/Dabbler.Security/Authorization/BakedInRoles.cs`
   with static string constants for each baked-in role.
4. Add privilege-definition types per role
   (`SystemAdminPrivileges`, `CrossOrgUserAdminPrivileges`, etc.)
   that the handlers will consult.
5. Add `IBakedInRoleProvider` DI contract + a default
   `PlatformBakedInRoleProvider` implementation that returns the
   five canonical roles.
6. Add `DisplayAlias` (nullable string) to `RoleDefinition` and to
   the baked-in role surface. Persist baked-in aliases in
   `SecurityConfiguration` (code constants are read-only but
   per-deployment rebranding is stored).
7. Build and test. Log.

**Progress keys:** `session-1/principle`, `session-1/constants`,
`session-1/privileges`, `session-1/di-hook`, `session-1/alias`,
`session-1/build-and-test`.

### Session 2 of 5: Restriction-based configuration model + Default Masks + Mask Exemptions

**Steps:**
1. Read planning docs.
2. Add `ColumnRestriction` `{ EntityKey, ColumnKey, RestrictionKind }`
   with `RestrictionKind` enum `ViewOnly` | `Hidden`.
3. Add `RowRestriction` `{ EntityKey, FilterExpression }`.
4. Add `UiRestriction` `{ PageKey, ControlKey, Kind }` with `Kind`
   enum `Disabled` | `Hidden` and a `"*"` convention for "Entire
   Page" on `ControlKey`.
5. Add `DefaultMaskRule` `{ EntityKey, ColumnKey, MaskFormat }`.
6. Add `MaskExemption`
   `{ EntityKey, ColumnKey, TargetRoleName, GrantedBy, GrantedAt, ExpiresAt? }`.
7. Shrink `SecurityConfiguration` to
   `{ UserTypeRoles, OrganizationSwitching, DefaultMasks, MaskExemptions }`.
   Each user-type role carries its three restriction lists.
   `MaskExemptions` is a top-level list (not per-role).
8. Delete `PagePermission`, `EndpointPermission`,
   `EntityPermission`, `FieldPermission`, `ControlPermission`,
   and the `InheritsFrom` field on `RoleDefinition`.
9. Build and test (expect test seed updates in session 5). Log.

**Progress keys:** `session-2/column-restriction`,
`session-2/row-restriction`, `session-2/ui-restriction`,
`session-2/default-mask-rule`, `session-2/mask-exemption`,
`session-2/security-config-shrink`, `session-2/delete-old-types`,
`session-2/build-and-test`.

### Session 3 of 5: New authorization handlers + remove inheritance

**Steps:**
1. Read planning docs.
2. Rewrite `PageAuthorizationHandler`:
   - If user has a baked-in role, resolve against its compiled
     privileges. SystemAdmin succeeds for every page.
     CrossOrgMonitor succeeds for every read-only page. Etc.
   - Otherwise, treat the user as a user-type role. Default is
     allow. Check `UiRestriction` for the (page, controlKey="*")
     tuple; if present with `Hidden` → fail with 403; with
     `Disabled` → succeed (handler doesn't enforce disabled state,
     UI does).
   - Never call `RoleInheritanceHelper.ExpandRoles`.
3. Rewrite `EndpointAuthorizationHandler` with the same baked-in-
   first, restriction-second pattern.
4. Rewrite `ControlAuthorizationHandler` to consult `UiRestriction`.
5. Delete `EntityAuthorizationHandler` and `FieldAuthorizationHandler`
   — replaced by default-permissive CRUD + column restrictions.
6. Remove every `RoleInheritanceHelper.ExpandRoles` call across
   `src/Libs/Dabbler.Security`. The helper itself stays on disk.
7. Build and test. Log.

**Progress keys:** `session-3/page-handler`,
`session-3/endpoint-handler`, `session-3/control-handler`,
`session-3/delete-entity-field-handlers`,
`session-3/remove-inheritance-calls`, `session-3/build-and-test`.

### Session 4 of 5: Row restriction enforcement runtime

**Steps:**
1. Read planning docs.
2. Port existing `RowFilterDefinition` logic into `RowRestriction`
   shape. Keep the expression-language evaluator.
3. Add `IRowRestrictionBroker` that resolves, for the active
   (user, role, entity), the effective filter expression or null.
   Data providers call this during query construction.
4. Add a reference integration: adapt one sample entity's
   `ServerData` call in the existing app (e.g.,
   `AcmeCoffeeOnline.Web`'s Orders grid) to consult the broker.
5. Integration test: user in a user-type role with a row
   restriction sees only matching records; user without
   restriction sees all.
6. Build and test. Log.

**Progress keys:** `session-4/row-restriction-port`,
`session-4/broker`, `session-4/sample-integration`,
`session-4/integration-test`, `session-4/build-and-test`.

### Session 5 of 5: Seed migration + handler test coverage + change log + reorganization

**Steps:**
1. Read planning docs.
2. Translate `src/Apps/AcmeCoffeeOnline.Web/security-config.json`
   per the migration mapping:
   - `SystemAdmin` → baked-in `SystemAdmin` (removed from the
     user-type roles list since baked-in roles aren't
     JSON-configured).
   - `BackofficeUserAdmin` → baked-in `CrossOrgUserAdmin` (removed
     from the list; users assigned to this role in `user-roles.json`
     get remapped).
   - `BackofficeUser` → app-defined user-type role named
     `BackofficeUser` with `DisplayAlias` "Backoffice User".
   - `Customer` → app-defined user-type role named `Customer`
     with `DisplayAlias` "Customer" and a row restriction limiting
     visibility to the current user's own records (expression TBD
     during session — likely `CustomerId = @currentUserCustomerId`
     or similar).
3. Translate `user-roles.json` role names accordingly.
4. Add integration tests covering:
   - Baked-in `SystemAdmin` → always allowed (no JSON needed).
   - User-type role with no restrictions → default-permissive.
   - User-type role with `ColumnRestriction.Hidden` → handler
     returns NotAuthorized for that column's control.
   - Row restriction → broker returns expected filter.
   - Inheritance is NOT applied (role A with `InheritsFrom = B`
     in legacy data must NOT gain B's privileges).
5. Build (`dotnet build Dabbler.Platform.sln`) and test
   (`dotnet test` excluding Playwright).
6. Cross-provider verification per Step 6 of the workflow.
7. Step 8: change log + commit + push + notify.
8. Step 9: reorganization review.

**Progress keys:** `session-5/seed-security-config`,
`session-5/seed-user-roles`, `session-5/integration-tests`,
`session-5/build-and-test`, `session-5/verification`,
`session-5/change-log`, `session-5/reorganization`.

---

## Exit criteria

- App boots.
- The seven old tabs of the Security Dashboard may render errors
  or be hidden — acceptable; Set B rebuilds them.
- All non-Playwright tests pass.
- `grep -r 'RoleInheritanceHelper.ExpandRoles' src/` returns zero
  hits outside the helper file itself and any test specifically
  covering the absence-of-inheritance behavior.
- `security-config.json` contains no `InheritsFrom`, no
  `AllowedRoles`, no `DeniedRoles` — only the new types.
- Baked-in role handlers proven by integration tests.

## Risk notes

- **Handler rewrite touches auth-critical code.** Keep the
  cross-provider verification step strict; do not merge with
  Critical issues unresolved.
- **`security-config.json` re-shape is destructive.** The
  `pre-role-rearchitecture` branch is the safety net — do not
  delete that branch until Set C is complete.
- **`RoleInheritanceHelper` deletion is deferred.** Any downstream
  consumer (e.g., in a sibling repo not surveyed) that imports
  `Dabbler.Security.Services.RoleInheritanceHelper` will still
  compile. The helper throws nothing; it is dead code. A future
  session-set decides the final removal.
