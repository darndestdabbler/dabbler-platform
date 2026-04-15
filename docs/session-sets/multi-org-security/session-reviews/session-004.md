# Verification Round 1



VERDICT: VERIFIED

## Findings

### Strengths

**1. UserAdmin actions scoped to active organization — Satisfied**
- `AdminUserScopeService.EnsureMembershipCanBeManaged()` correctly checks `ActiveOrganizationId != organizationId` for non-SystemAdmin actors, throwing `UnauthorizedAccessException` on cross-org attempts.
- `BuildScopeAsync()` filters memberships by `actorContext.ActiveOrganizationId`, ensuring the user list returned is org-scoped.
- The `UserManagement.razor` page independently queries only the active organization's memberships.

**2. UI organization switching — Satisfied**
- `MainLayout.razor` displays active org label (`data-testid="active-organization-label"`), a "Switch org" menu, and routes through `/auth/switch-organization` with `forceLoad: true` to re-issue the cookie with updated org claims.
- `/auth/switch-organization` in `Program.cs` calls `sessionContextService.CreatePrincipalAsync` with the new `organizationId`, re-signs the cookie, and redirects to `returnUrl`.
- SystemAdmin sees all active organizations; non-SystemAdmin sees only their memberships.

**3. SystemAdmin unrestricted — Satisfied**
- `EnsureMembershipCanBeManaged()` short-circuits with `return actorContext` when `IsSystemAdmin` is true, bypassing org-match checks.
- `MainLayout.razor` builds `_organizationOptions` from all active orgs for SystemAdmin vs. only user memberships for others.
- Test `SystemAdmin_CanGrantScopedUserAdmin_AndCustomerRowsFollowActiveOrganizationAsync` proves SystemAdmin can grant across orgs.

**4. Tests meaningfully prove admin limits, switching, and row filtering — Satisfied**
- `RegionalSalesRep_ScopeAndElevationStayInsideActiveOrganizationAsync`: Proves scoped admin sees only allowed-org users and gets `UnauthorizedAccessException` on cross-org grant attempt.
- `SystemAdmin_CanGrantScopedUserAdmin_AndCustomerRowsFollowActiveOrganizationAsync`: Proves SystemAdmin can grant cross-org, verifies DB state, and demonstrates customer row filtering follows active org (internal = 0 customers, retail = 1 customer).
- `AuthenticatedOrganizationSwitch_UpdatesShellAndAdminScopeAsync`: Proves org switch changes session claims and unlocks admin page access.
- `SeededSession_MultiOrgDiagnostics_ShowAndSwitchActiveOrganizationAsync`: Proves role set changes per-org on switch.

### Minor Observations (non-blocking)

**Severity: Low — Duplicated scope-building logic**
- `UserManagement.razor` duplicates `BuildScopeAsync` logic from `AdminUserScopeService` rather than calling the API endpoint via `AdminUserApiClient`. The `AdminUserApiClient` is registered but appears unused by the Razor page. This works because SSR Blazor has direct DB access, but creates a maintenance risk where the two code paths could diverge.
- **File:** `UserManagement.razor` vs `AdminUserScopeService.cs`
- **Recommendation:** Consider having the Razor page delegate to `IAdminUserScopeService` or `AdminUserApiClient` to maintain a single source of truth.

**Severity: Low — CanGrantUserAdmin not gated by CanManageUsers in Razor page**
- In `UserManagement.razor`, `BuildScopeUser` sets `CanGrantUserAdmin = !hasScopedUserAdmin` without checking `_isSystemAdmin || hasAdminRole` like the service does. The page is already gated by `[DabblerPageAuthorize]` and the `_scope.CanManageUsers` check in the template, so this is not exploitable, but the DTO values could be misleading if consumed elsewhere.
- **File:** `UserManagement.razor` line in `BuildScopeUser`

**Severity: Low — Revoke endpoint uses DELETE on same path as grant**
- `[HttpDelete("memberships/{organizationMembershipId:int}/grant-user-admin")]` reuses the grant path with a different verb. Semantically, a path like `revoke-user-admin` or a separate action name would be clearer, but this is functional and REST-compatible.

**Severity: Info — Test fixture setup is thorough but verbose**
- The `ResolveFixtureIdsAsync` method in `AdminUserScopeTests.cs` is ~100 lines of ensure-or-create logic. This is acceptable for integration tests but could benefit from a shared fixture builder if future sessions add more test scenarios.

### Security Assessment

- No authorization bypass paths identified. Non-SystemAdmin actors are correctly blocked from cross-org operations.
- Last-admin protection is implemented in both `AdminUserScopeService.RevokeScopedUserAdminAsync` and `UserManagement.razor`.
- Self-revocation is prevented via `membership.UserId != actorContext.ActorUserId` check.
- Organization switching properly re-issues the authentication cookie with scoped claims.
