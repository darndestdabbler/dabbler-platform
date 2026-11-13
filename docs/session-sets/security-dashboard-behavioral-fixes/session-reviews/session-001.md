# Verification Round 1



## VERIFIED

The Session 1 fix is correct, well-scoped, and properly tested. Detailed analysis per each verification question:

---

### 1. Root cause: ✅ Correct

The old `ApplyRowFilter` checked `ActiveOrganizationId.HasValue` first, then required `CustomerId.HasValue` to return rows. Since every authenticated user has `ActiveOrganizationId` set (via `UserSessionContextService` resolving the default membership), and BackofficeUser staff have `CustomerId == null`, the filter evaluated to `Where(_ => false)`. This is exactly the UAT symptom.

The new `ApplyRowFilter` aligns with `security-config.json` RowFilters, which declares exactly one row filter: `Customer` role scoped by `CustomerId == @CurrentUserId`. The new code mirrors this precisely — internal staff pass through, Customer is scoped, unresolvable Customer is denied.

### 2. Role gate: ✅ Safe

`MapHighestRole` iterates role claims and parses each via `Enum.TryParse<UserRole>`. The enum values are:

- `Customer = 0`
- `BackofficeUser = 1`
- `BackofficeUserAdmin = 2`
- `SystemAdmin = 3`

A Customer principal would need a `ClaimTypes.Role` claim with value `"BackofficeUser"`, `"BackofficeUserAdmin"`, or `"SystemAdmin"` to satisfy `Role >= BackofficeUser`. Role claims are set by:
- **InternalUserMiddleware** (from `AppUserRoles` in `HttpContext.Items`, populated from the Dabbler.Security user-role store)
- **Cookie/OIDC claims** (stamped at authentication time from the same store)
- **Test code** (explicitly constructed)

There is no path where a user with only a Customer role assignment gets an elevated role claim. The comparison is safe.

### 3. Deny-all fallback: ✅ Appropriate

Returning `Where(_ => false)` (empty result set) for a Customer with no resolvable `CustomerId` is the correct defensive choice. The alternatives are worse:
- **Throwing** would surface a 500 to the user for what is a data-integrity gap, not a request error.
- **Returning all orders** would be a data leak.

The current behavior returns an empty page, which is functionally correct — a Customer who can't be identified as any specific customer has no orders to see. This matches the "safer than leaking" comment in the code.

### 4. Integration test coverage: ✅ Exercises the bug path

`BackofficeUser_WithInternalOrganizationActive_SeesAllOrdersAsync` explicitly:
- Sets `ClaimTypes.Role` to `"BackofficeUser"`
- Sets `ActiveOrganizationIdClaimType` to the internal organization's ID
- Sets `ActiveOrganizationKeyClaimType` to the internal org key (not a `customer-{id}` key, so `CustomerId` resolves to `null`)

This is the exact principal shape that jdoe@acmecoffee.com has in production — the configuration that previously hit `Where(_ => false)`. The test asserts `page.TotalCount == ctx.TotalOrderCount` (all orders visible), directly confirming the bug is fixed.

The Customer test correctly sets the customer org key (`customer-{id}`), which causes `TryResolveCustomerIdFromOrganizationKey` to return the customer ID, exercising the scoped path. The `Assert.All` on `CustomerId` confirms no data leakage.

The SystemAdmin test confirms the pass-through path remains intact.

### 5. Scope discipline: ✅ Clean

The diff touches exactly:
- `OrderService.cs` — `ApplyRowFilter` method only
- `OrderVisibilityTests.cs` — new file
- `OrdersWorkflowTests.cs` — new test added to existing file

No changes to:
- `OrderDetail.razor` / `OrderDetailPage.razor` (Session 2 scope)
- `DabblerUserRoleManager.razor` / `EfCoreUserRoleStore.cs` (Session 3 scope)
- Any control permission or endpoint permission configuration

The session stayed strictly within the "BackofficeUser zero-orders" boundary.

### 6. Regressions: ✅ None identified

The old code's `ActiveOrganizationId.HasValue` branch did not implement any intentional "admin acting as customer" behavior — it was a bug that happened to also deny BackofficeUser access. Organization-switching and role-switching are separate mechanisms in this codebase:

- **Role switching** is governed by `RoleSwitching.AllowedRoles` in `security-config.json` and operates at the claims/session level, changing the `ClaimTypes.Role` values. If a SystemAdmin switches to act as Customer, `_currentUser.Role` would return `Customer`, and the new code correctly scopes them to their `CustomerId`.
- **Organization switching** changes `ActiveOrganizationId`/`ActiveOrganizationKey` claims but does not change the role. The new `ApplyRowFilter` no longer depends on `ActiveOrganizationId` at all — it gates purely on role, which is the correct dimension for row-level access control.

The one pre-existing test failure (`AiRouterConfig_LoadsNestedPromptTemplates`) is confirmed unrelated — it references a file removed in a prior commit on master.
