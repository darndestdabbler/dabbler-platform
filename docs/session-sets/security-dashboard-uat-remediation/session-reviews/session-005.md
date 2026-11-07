# Verification Round 1

# VERIFIED

All Session 5 steps (Finding 4 restriction-seam reachability + Finding 10.2 unskip) are correctly implemented. Build and tests are green modulo a pre-existing, unrelated flake.

---

## Verification Details

### Strategy B consistency across Web + Api configs

Both `src/Apps/AcmeCoffeeOnline.Web/security-config.json` and `src/Apps/AcmeCoffeeOnline.Api/security-config.json` are byte-identical. The critical change — removing BackofficeUser's page-level `{ "PageKey": "/products", "ControlKey": "*", "Kind": "Hidden" }` and replacing it with two ControlKey-scoped entries (`CreateProductButton`, `EditPriceButton`) — is present in both files. `PageAuthorizationHandler.IsAllowed` (line 86–91) only denies when `ControlKey == "*" && Kind == Hidden`, so BackofficeUser now passes page auth on `/products`. WarehouseUser never had a page-level block on `/products` in either config. ✓

### WarehouseUser persona resolution chain

| Seed method | What it seeds | Verified |
|---|---|---|
| `GetSecurityRoles()` | `AppRole { Name = "WarehouseUser", IsSystemRole = true }` — no `InheritsFromRoleName` (standalone track, correct per spec) | ✓ |
| `GetSecurityUsers()` | `AppUser { Email = "warehouse@acmecoffee.com" }` | ✓ |
| `GetOrganizationMemberships()` | Internal-org membership via the `foreach` loop over staff emails (line includes `"warehouse@acmecoffee.com"`) | ✓ |
| `GetUserRoleAssignments()` | `AddScopedRole(warehouseUser, "WarehouseUser", memberships => memberships.Single(m => m.IsDefault))` — resolves `roles.First(r => r.Name == "WarehouseUser")` to the DB row, creates `AppUserRole` with correct `RoleId` + `OrganizationMembershipId` | ✓ |

### customer@acmecoffee.com seeding and TryResolveCustomerId

`GetOrganizationMemberships()` bridges the persona to `customerOrganizations[GetCustomerOrganizationKey(customers.OrderBy(c => c.Id).First())]`. After EF save, the first customer has `Id = 1`, so the org key is `"customer-1"`. `TryResolveCustomerId` parses the numeric suffix → `ActiveCustomerId = 1`, satisfying the `CustomerId == @CurrentUserCustomerId` row-restriction filter. `GetUserRoleAssignments()` then assigns the `Customer` role scoped to that membership. The original 401 was caused by a missing `AppUser` row, not a Docker-bridge networking issue — the diagnosis is correct and the fix is narrowly targeted. ✓

### Playwright test scoping (7 new tests in SecurityDashboardUatCoverageTests)

| Test | Persona | Assertion | Human UAT Philosophy |
|---|---|---|---|
| `Products_BackofficeUser_ReachesListWithoutAccessDeniedAsync` | jdoe (real BackofficeUser) | No `access-denied-page`, Unit Price header visible | Objective ✓ |
| `Products_BackofficeUser_UnitPriceColumn_IsNotEditableAsync` | jdoe | Column visible, no inline editor in header | Objective ✓ |
| `Products_BackofficeUser_UnitPriceField_IsDisabledOnDetailAsync` | jdoe | `field-unit-price` visible + disabled | Objective ✓ |
| `AdminProducts_BackofficeUser_StillDeniedAsync` | jdoe | `access-denied-page` present on `/admin/products` | Regression guard ✓ |
| `Products_WarehouseUser_UnitPriceColumn_AbsentFromGridAsync` | warehouse@ (real WarehouseUser) | Unit Price header count = 0 | Objective ✓ |
| `Products_WarehouseUser_UnitPriceField_AbsentFromDetailAsync` | warehouse@ | `field-unit-price` count = 0 | Objective ✓ |
| `Products_CustomerPersona_UnitPriceField_IsDisabledOnDetailAsync` | customer@ (real Customer) | `field-unit-price` visible + disabled | Objective ✓ |

All tests use real seeded personas via `SeededSignInAsync`, not role-overrides on admin. The `RestrictedFormField.razor` (line 5: `Visibility == ColumnVisibility.Hidden` → render nothing) and `RestrictedGridColumn.razor` (line 8: same gate) confirm the rendering contracts the assertions depend on. ✓

### Finding 10.2 unskip risk

`CustomerRole_SeesUnitPriceFieldButReadOnlyAsync` in `RestrictionEnforcementTests.cs` is now un-skipped. It depends **exclusively** on the new seeding (`AppUser` + org membership + role assignment for `customer@acmecoffee.com`). The Docker-bridge theory is fully retired — the test passed on both fresh-container Playwright runs. No residual risk from the original theory. ✓

### Out-of-scope disclosures

All seven disclosures are honest:

1. **Add Product button ungated** — genuinely pre-existing; `ProductList.razor` never wraps the `<MudButton Href="/admin/products">` in a `RestrictedControl`. The new ControlKey entries in config prepare for future wiring but don't claim to activate it now. The regression test (`AdminProducts_BackofficeUser_StillDeniedAsync`) correctly pins the current click-through-to-denied behavior.
2–7: Informational/contextual, no masked defects.

### Build/test results

- 0 build errors ✓
- 672/672 unit tests ✓
- 109/110 Playwright (single failure is `MultiOrganizationWorkflowTests` timing flake in `SwitchOrganizationFromShellAsync` — different test fails on different run, fully independent of Session 5 changes) ✓
- All 7 new Session 5 tests + Finding 10.2 unskip passed on both runs ✓
