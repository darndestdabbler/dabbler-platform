# Verification Round 1

## Cross-Provider Session Verification: Session 3 of 8

### 1. Single control per cell

**PASS.** `DabblerEndpointPermissionMatrix.razor` renders exactly one `<DenyAllowSwitch>` per `role × endpoint` cell inside the inner `@foreach (var role in _roles)` loop. There are no `MudCheckBox` references anywhere in the file. The table has exactly two `<th>` columns: "Role" and the SVG header — no separate "Allow" and "Deny" columns.

### 2. Deny modelled end-to-end

**PASS.** Full chain verified:

| Layer | Evidence |
|-------|----------|
| **Config DTO** | `EndpointPermission.cs` has both `AllowedRoles` and `DeniedRoles` as `List<string>` |
| **Entity** | `AppEndpointPermission.cs` has `bool Allowed` property with default `true` |
| **Migration** | `20260418050000_AddEndpointPermissionAllowed.cs` adds `Allowed` column with `defaultValue: true`, backfilling legacy rows |
| **Store Load** | `EfCoreSecurityStore.LoadAsync` groups by `EndpointKey`, splits on `p.Allowed` into `AllowedRoles`/`DeniedRoles` |
| **Store Save** | `SyncEndpointPermissionsAsync` iterates both `ep.AllowedRoles` (Allowed=true) and `ep.DeniedRoles` (Allowed=false) |
| **Handler** | `EndpointAuthorizationHandler` checks `DeniedRoles` first — if any match, returns without calling `context.Succeed()` |

### 3. Mutual exclusion at the data model

**PASS.** The matrix's internal `_state` dictionary maps `(role, endpointKey) → PermissionState` (a single enum value: `Unset`, `Allow`, or `Deny`). This is structurally a ternary — it's impossible for a single cell to hold two states simultaneously.

`SaveAsync` projects via:
```csharp
AllowedRoles = _roles.Where(r => GetState(r, epKey) == PermissionState.Allow).ToList(),
DeniedRoles = _roles.Where(r => GetState(r, epKey) == PermissionState.Deny).ToList()
```

Since `GetState` returns exactly one enum value, a role can appear in at most one list. `LoadFromConfig` processes `DeniedRoles` after `AllowedRoles`, so if legacy data has a role in both, Deny wins (defensive, with comment).

### 4. deny-allow-header.svg reference

**PASS.** Both `DabblerEndpointPermissionMatrix.razor` and `DabblerPagePermissionMatrix.razor` reference:
```html
<img src="_content/Dabbler.Security.Blazor/deny-allow-header.svg" ...>
```
This is the correct RCL static web asset path pattern. The bUnit test `RendersDenyAllowHeaderSvg_NotSeparateAllowAndDenyColumns` also asserts this path is present in markup.

### 5. bUnit invariant coverage

**PASS.** Three key invariant tests present:

| Test | Invariant |
|------|-----------|
| `FlipAllowToDeny_DropsAllow_AndAddsDeny_OnSave` | Allow→Deny: asserts `DeniedRoles.Contains("Admin") && !AllowedRoles.Contains("Admin")` |
| `FlipDenyToAllow_DropsDeny_AndAddsAllow_OnSave` | Deny→Allow: asserts `AllowedRoles.Contains("Admin") && !DeniedRoles.Contains("Admin")` |
| `SetToUnset_OmitsRoleFromBothLists_OnSave` | Allow→Unset: asserts `!AllowedRoles.Contains("Admin") && !DeniedRoles.Contains("Admin")` |

Additionally `ExistingDeniedEndpoints_RenderSwitchInDenyState` verifies Deny loads correctly from config.

### 6. Handler-level deny-precedence test

**PASS.** `EndpointAuthorizationHandlerTests.DeniedRole_DoesNotSucceed_EvenWhenSameUserHasAnAllowedRole` creates a user with both "Manager" (allowed) and "Contractor" (denied) roles, and asserts `context.HasSucceeded.Should().BeFalse()`. This directly tests the deny-takes-precedence semantic.

### 7. Accessibility

**PASS.**

- **DenyAllowSwitch**: `aria-label="@AriaLabel"` on `MudToggleGroup`; endpoint matrix passes `AriaLabel="@($"Permission for {role} to call {ep.DisplayName}")"` — includes both role and endpoint.
- **SVG header**: `alt="Deny or Allow"` and `title="Deny or Allow"` attributes present.
- **Table**: `aria-label="@($"Endpoint permissions for {ep.DisplayName}")"` on `MudSimpleTable`.

### 8. No regressions

**PASS.**

- `EndpointPermission.DeniedRoles` defaults to `[]` (empty list), so existing JSON configs with only `AllowedRoles` deserialize correctly.
- Migration backfills `Allowed = true` for existing rows.
- `EfCoreSecurityStore.LoadAsync` uses `.Where(p => p.Allowed)` / `.Where(p => !p.Allowed)` — legacy data (all `Allowed=true`) produces empty `DeniedRoles`.
- Test counts: +5 bUnit tests (126→131), +2 unit tests (268→270), zero new failures.
- The pre-existing `WorkflowAssetSmokeTests` failure is unrelated and carried from session 1.

---

### Notes (non-blocking observations)

- **Minor consistency gap**: `PageAuthorizationHandler` (session 1) calls `context.Fail(new AuthorizationFailureReason(...))` on deny, while `EndpointAuthorizationHandler` simply returns without calling `Fail()`. The session explicitly scoped this out ("scoping Fail() to endpoints was not called out in the session 3 spec"), but a future session should align these for audit/logging parity.
- The migration file references `type: "bit"` which is SQL Server-specific. This is consistent with the existing migration style and the project's SQL Server target, so it's fine.

VERDICT: VERIFIED
