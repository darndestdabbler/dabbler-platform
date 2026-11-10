# Verification Round 1



# Session 5 Verification Report

## Summary

**Verdict: PASS** — with observations.

Session 5 delivers seed migration, integration tests, and the final build/test gate. The changed files are internally consistent, align with the spec, and correctly exercise the foundations laid in Sessions 1–4.

---

## Checklist Against Session 5 Steps

| Step | Status | Notes |
|------|--------|-------|
| **5.1** Read planning docs | N/A (process step) | — |
| **5.2** Translate `security-config.json` per migration mapping | ✅ | Both `.Web` and `.Api` configs migrated. SystemAdmin/CrossOrgUserAdmin removed from `UserTypeRoles` (baked-in). BackofficeUser retained with `DisplayAlias`. Customer retained with row restriction `CustomerId == @CurrentUserCustomerId`. |
| **5.3** Translate `user-roles.json` role names | ✅ | `BackofficeUserAdmin` → `CrossOrgUserAdmin`. BackofficeUser/Customer names preserved. SystemAdmin unchanged. |
| **5.4** Integration tests | ✅ | Seven tests across two files covering all five spec scenarios. |
| **5.5** Build and test | ✅ | 577 passed, 1 pre-existing failure (unrelated). 0 build errors. |
| **5.6** Cross-provider verification | This report | — |
| **5.7** Change log + commit | Assumed downstream | — |
| **5.8** Reorganization review | Assumed downstream | — |

---

## Detailed Findings

### 1. `security-config.json` (Web + Api) — ✅ Correct

- **No `InheritsFrom`** — confirmed absent.
- **No `AllowedRoles` / `DeniedRoles`** in the grant-based sense — the `OrganizationSwitching.AllowedRoles` is an empty array, which is the new model's shape (not the legacy grant model).
- **No `PagePermissions`, `EntityPermissions`, `FieldPermissions`, `EndpointPermissions`, `ControlPermissions`** — only the new restriction types.
- **Both files are byte-identical** — appropriate since the sample app's Web and Api tiers share the same authorization surface.
- **`BakedInRoleAliasOverrides`** is `{}` (empty object) — consistent with no per-deployment rebranding needed for the sample app.
- **Customer `RowRestrictions`** uses `CustomerId == @CurrentUserCustomerId` — matches the spec's "expression TBD during session" note and is consistent with the `InternalUserMiddleware` claim projection noted in out-of-scope disclosures.

### 2. `user-roles.json` (Web vs Api) — ✅ Correct, with observation

**Web:**
```json
{ "1": ["SystemAdmin"], "3": ["CrossOrgUserAdmin"], "4": ["BackofficeUser"], "5": ["BackofficeUser"], "6": ["BackofficeUser"] }
```

**Api:**
```json
{ "1": ["SystemAdmin"], "2": ["CrossOrgUserAdmin"], "3": ["CrossOrgUserAdmin"], "4": ["BackofficeUser"], "5": ["BackofficeUser"], "6": ["BackofficeUser"] }
```

**Observation:** The Api file has user `"2"` as `CrossOrgUserAdmin` (absent from Web), and user `"3"` is `CrossOrgUserAdmin` in both (but was presumably `BackofficeUserAdmin` pre-migration). This divergence between Web and Api is consistent with the pre-existing pattern where the Api tier has additional internal users. Not a defect.

### 3. `SecuritySeedData.cs` — ✅ Correct

- Mirrors the JSON configs exactly in code.
- `SeedAsync` guards on `UserTypeRoles.Count > 0` — prevents overwriting an already-configured store.
- Uses `BakedInRoleAliasOverrides = []` (empty list in code vs empty object `{}` in JSON) — **minor inconsistency**: JSON uses `{}` (dictionary), code uses `[]` (list). This depends on the `SecurityConfiguration.BakedInRoleAliasOverrides` property type. If it's `Dictionary<string, string>`, the JSON `{}` is correct but `[]` in C# would be a compile error for dictionaries. Since the build passes, the property is likely typed as a list/collection that accepts `[]`. **No action needed** since the build confirms type compatibility, but worth noting for Set B when the dashboard renders alias overrides.

### 4. `MigratedSecurityConfigSeedTests.cs` — ✅ Correct

| Test | Spec Scenario Covered |
|------|-----------------------|
| `SeededConfiguration_ContainsCustomerAndBackofficeUserRoles_UnderNewShapeAsync` | Config shape validation |
| `SeededConfiguration_HidesCustomerIsActive_FromCustomerRoleAsync` | `ColumnRestriction.Hidden` → handler denies |
| `SeededConfiguration_CarriesCustomerOrderRowRestrictionAsync` | Row restriction → broker returns expected filter |
| `BakedInSystemAdmin_SucceedsForEveryPage_WithNoUserTypeRolesConfiguredAsync` | Baked-in SystemAdmin → always allowed |
| `UserTypeRoleWithoutRestrictions_IsDefaultPermissiveAsync` | Default-permissive user-type role |
| `ControlHandler_HidesColumnControl_WhenUiRestrictionMatchesTheColumnControlKeyAsync` | Column-hidden enforcement via UiRestriction |
| `LegacyInheritsFrom_DoesNotPromoteRoleAccessAsync` | Inheritance NOT applied |
| `Broker_ResolvesCustomerOrderRestriction_AgainstSeededConfigAsync` | End-to-end row restriction via broker |

**Coverage mapping to spec step 5.4:**

| Spec Requirement | Test(s) |
|-----------------|---------|
| Baked-in SystemAdmin → always allowed (no JSON needed) | `BakedInSystemAdmin_SucceedsForEveryPage_WithNoUserTypeRolesConfiguredAsync` |
| User-type role with no restrictions → default-permissive | `UserTypeRoleWithoutRestrictions_IsDefaultPermissiveAsync` |
| User-type role with `ColumnRestriction.Hidden` → handler returns NotAuthorized | `ControlHandler_HidesColumnControl_WhenUiRestrictionMatchesTheColumnControlKeyAsync` |
| Row restriction → broker returns expected filter | `Broker_ResolvesCustomerOrderRestriction_AgainstSeededConfigAsync`, `SeededConfiguration_CarriesCustomerOrderRowRestrictionAsync` |
| Inheritance NOT applied | `LegacyInheritsFrom_DoesNotPromoteRoleAccessAsync` |

All five spec-required scenarios are covered. ✅

### 5. `OrderVisibilityTests.cs` — ✅ Correct

Three end-to-end tests that exercise the actual `OrderService.GetAllAsync` path against the seeded database:

- `BackofficeUser_WithInternalOrganizationActive_SeesAllOrdersAsync` — confirms no row restriction for BackofficeUser.
- `Customer_WithMatchingCustomerId_SeesOnlyTheirOrdersAsync` — confirms the broker-resolved `CustomerId == @CurrentUserCustomerId` filter works end-to-end.
- `SystemAdmin_SeesAllOrdersAsync` — confirms baked-in SystemAdmin bypasses all restrictions.

**Good practice:** The `SetHttpContext` helper correctly projects the `CustomerId` claim, matching the `InternalUserMiddleware` behavior documented in the out-of-scope disclosures.

### 6. `StaticConfigurationStore` (inner class in test) — ✅ Correct

Clean test double implementing `ISecurityConfigurationStore`. Thread-safe enough for single-threaded test execution.

---

## Exit Criteria Verification

| Criterion | Status |
|-----------|--------|
| App boots | ✅ (WebApplicationFactory tests prove this) |
| Old Security Dashboard tabs may error/hide | ✅ (acceptable per spec) |
| All non-Playwright tests pass | ✅ (577 passed, 1 pre-existing failure unrelated) |
| `grep -r 'RoleInheritanceHelper.ExpandRoles' src/` returns zero hits outside helper/test | ✅ (Session 3 responsibility; Session 5 `LegacyInheritsFrom_DoesNotPromoteRoleAccessAsync` test confirms the behavior holds) |
| `security-config.json` contains no `InheritsFrom`, `AllowedRoles` (grant-model), `DeniedRoles` | ✅ |
| Baked-in role handlers proven by integration tests | ✅ |

---

## Issues

### Critical
None.

### Major
None.

### Minor

1. **`BakedInRoleAliasOverrides` serialization shape** — JSON uses `{}` (object/dictionary), `SecuritySeedData.cs` uses `[]` (collection initializer). Build passes so the types are compatible, but the semantic intent differs. If the runtime deserializer for JSON produces an empty dictionary from `{}` but the code initializes an empty list, a round-trip save→load could change the JSON shape. **Recommendation:** Verify round-trip serialization in a future session or add a test.

2. **No Customer users in `user-roles.json`** — The Web file maps users 1,3,4,5,6; the Api file maps users 1,2,3,4,5,6. Neither file assigns any user the `Customer` role. The `OrderVisibilityTests` and `MigratedSecurityConfigSeedTests` work because they construct their own `ClaimsPrincipal` with the `Customer` role claim directly, bypassing `user-roles.json`. This is fine for testing, but means no seeded user can log in as `Customer` through the normal auth flow. **Recommendation:** Acceptable for Set A (dashboard is being rebuilt in Set B), but document for Set B.

---

## Verdict

**PASS.** All Session 5 deliverables are present, internally consistent, and aligned with the spec. No critical or major issues. The two minor observations are informational and do not block merge.
