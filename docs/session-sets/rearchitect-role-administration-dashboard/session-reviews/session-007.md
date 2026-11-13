# Verification Round 1

ISSUES_FOUND

1. **Category:** Security  
   **Severity:** Critical  
   **Details:** `UserRoleAssignmentDetails` only enforces role-assignment rules in the rendered UI, not in the mutation handlers.  
   - `AssignAsync()` calls `UserRoleStore.AddRoleAsync(User.UserId, _pendingRoleName)` after only null checks. It does **not** re-run `RoleAssignmentAuthority.Evaluate(...)` at submit time.  
   - `RemoveRoleAsync()` likewise calls `UserRoleStore.RemoveRoleAsync(...)` without `EvaluateRemoval(...)`.  
   - `_pendingRoleName` is just bound UI state. A tampered client can bypass the filtered dropdown and submit a disallowed value anyway.  
   - `RoleAssignmentAuthority` also treats any unknown non-empty string as a user-type role, and `AssignAsync()` never validates the submitted role name against `IBakedInRoleProvider` + `SecurityConfiguration.UserTypeRoles`.  
   Result: a forged request/client state could persist unauthorized or even nonexistent role names (for example, elevating to `SystemAdmin` or storing arbitrary strings) despite the UI looking gated.

2. **Category:** Security  
   **Severity:** Major  
   **Details:** `AdminUserQueryService.GetUsersAsync()` scopes **which users** a `UserAdmin` can see, but does not scope the returned **organization memberships** for those users.  
   - The user filter correctly limits rows to users sharing at least one administered org.  
   - But the projected `Organizations` list uses all active memberships:
     `user.OrganizationMemberships.Where(...).Select(...)`
     with no intersection against `scopeOrganizationIds` for non-cross-org actors.  
   - `UsersMasterGrid` and `UserRoleAssignmentDetails` then render that full organization list.  
   Result: a `UserAdmin` who should only see their own org scope can learn other org memberships/names/keys for any visible user.

3. **Category:** Security  
   **Severity:** Major  
   **Details:** `AdminUserQueryService` does not actually implement its documented visibility rules for `CrossOrgMonitor`, and it does not fail closed for non-admin users.  
   - `GetUsersAsync()` decides cross-org visibility from `GetActorActiveAdminRole()`.  
   - `GetActorActiveAdminRole()` only returns baked-in roles with `CanAssignUsersToRoles` or `SystemAdmin`.  
   - `CrossOrgMonitor` has `CanSeeAllOrganizations` but **not** `CanAssignUsersToRoles`, so this method returns `null` for CrossOrgMonitor.  
   - That means `ActorSeesAllOrganizations(...)` becomes false, so a CrossOrgMonitor will **not** get the “see all users” behavior described in the service xmldoc and requested verification scope.  
   - Also, any authenticated user with org memberships but no admin role falls into the shared-org branch instead of being rejected/returning empty.  
   Result: the service contract is incorrect as implemented, and access control depends on outer UI gating rather than the query service itself.

4. **Category:** Completeness  
   **Severity:** Major  
   **Details:** The session scope explicitly called for **integration + bUnit tests**, but the attached additions only show:
   - authority unit tests (`RoleAssignmentAuthorityTests`)
   - bUnit component tests (`UsersMasterGridTests`, `UserRoleAssignmentDetailsTests`, dashboard tests)
   - one Playwright workflow test file  
   
   Missing from the attached work are integration tests covering the persistence/query boundary, such as:
   - `AdminUserQueryService` against a real `DabblerSecurityDbContext` with scoped-org data
   - store-backed role assignment round-trip through `IUserRoleStore`
   - verification of the scoped projection behavior for `UserAdmin`
   
   Additionally, the reported run says the Playwright test was **compiled but not executed**, so the browser workflow was not actually validated in the session’s test run.

Checked and okay:
- No `IHttpContextAccessor` was injected directly into the Blazor components.
- The next-sign-in claim merge path appears wired correctly via `UserSessionContextService` merging `IUserRoleStore` roles into the created principal.

---

# Verification Round 2

ISSUES_FOUND

Round-1 issues 2, 3, and 4 appear properly resolved. Round-1 issue 1 is only partially resolved.

1. **Category:** Security  
   **Severity:** Major  
   **Details:** `UserRoleAssignmentDetails.AssignAsync()` now re-runs authority at submit time and rejects arbitrary unknown strings, but the submit-time role-recognition check is **not actually provider-backed** as claimed.  
   - `IsRecognizedRole(string roleName)` uses `BakedInRoles.IsBakedIn(roleName)` plus `ConfigManager.Current.UserTypeRoles`.  
   - It does **not** validate baked-in roles against the injected `IBakedInRoleProvider` (`GetBakedInRoles()` / `TryGetBakedInRole()`), even though the dropdown itself is built from that provider.  
   - Result: in any deployment/test where `IBakedInRoleProvider` is customized to expose only a subset of baked-in roles, a tampered `_pendingRoleName` can still submit a static baked-in name that the configured provider did not expose. If `RoleAssignmentAuthority.Evaluate(...)` allows it, `UserRoleStore.AddRoleAsync(...)` will persist a role outside the configured provider surface.  
   - So the original “submit handler must validate against the configured role sources, not just UI state” issue is not fully closed across provider configurations.

   **Concrete fix:** change `IsRecognizedRole` to consult `BakedInRoleProvider` rather than `BakedInRoles.IsBakedIn`, and add a bUnit test with a mocked provider that omits a built-in role to verify tampered submit-time rejection.

---

# Verification Round 3

VERIFIED

- `UserRoleAssignmentDetails.IsRecognizedRole(...)` now checks baked-in roles through the injected `IBakedInRoleProvider` via `TryGetBakedInRole(roleName)` instead of the static `BakedInRoles.IsBakedIn(...)`.
- `AssignAsync()` still performs this submit-time recognition check before calling `UserRoleStore.AddRoleAsync(...)`, so a tampered canonical baked-in name that the configured provider does not expose is no longer accepted.
- The new bUnit test `AssignAsync_RejectsBakedInRoleNotExposedByConfiguredProvider` exercises the custom-provider subset scenario and verifies `AddRoleAsync(...)` is not called.

The round-2 issue is properly closed.
