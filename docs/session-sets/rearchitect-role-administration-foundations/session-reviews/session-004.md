# Verification Round 1



# VERIFIED

## Session 4: Row restriction enforcement runtime

### Checklist Results

| # | Check | Result | Detail |
|---|-------|--------|--------|
| 1 | ExpressionBuilder grammar parity | ✅ PASS | Int literal, single-quoted string, `@Token` — all three branches are structurally identical to `RowFilterService.TryBuildExpression`. Returns `null` on parse failure, unknown property, missing/null token. The only meaningful delta is that `@Token` is now generic (any token name, case-insensitive dictionary lookup) rather than hardcoded to `@CurrentUserId` — this is a strict superset, not a regression. |
| 2 | Broker resolves (user, role, entity) → filter or null | ✅ PASS | `GetRestriction<T>` reads `IRoleSwitcher.EffectiveRoles`, iterates `SecurityConfiguration.UserTypeRoles`, matches `EntityKey` case-insensitively, and returns `null` for unauthenticated principal, baked-in roles, no matching user-type role, or no matching entity restriction. |
| 3 | No inheritance walk | ✅ PASS | Broker iterates only `effectiveRoles` (from claims / switcher) and looks up each by `RoleName` in `UserTypeRoles`. The `InheritsFrom` field is never read. Regression test `InheritanceIsNotApplied` confirms. |
| 4 | OrderService broker-first + fallback | ✅ PASS | `ApplyRowFilter` calls `_rowRestrictionBroker.GetRestriction<Order>("Order")` first; if non-null, applies it and returns. Otherwise falls through to the legacy `ICurrentUserService.Role`-based branch, preserving current seed behavior until Session 5 migrates that rule. |
| 5 | Test coverage: restricted vs unrestricted | ✅ PASS | Unit: `UserTypeRoleWithMatchingRestriction_ReturnsCompiledFilter` (match + non-match), `UnparseableRestriction_ProducesDenyAllForThatRole`, `MultipleRolesWhenOneIsUnrestricted_ReturnsNull`. Integration: `UserTypeRoleWithRowRestriction_SeesOnlyMatchingRecordsAsync` (restricted), `UserTypeRoleWithoutMatchingRestriction_SeesAllRecordsAsync` (unrestricted). Covers spec step 5. |
| 6 | Multi-role OR combination + parameter rebinding | ✅ PASS | `OrElse` creates a fresh `ParameterExpression`, uses `ReplaceParameterVisitor` to rewrite both left and right bodies to the shared parameter before combining with `Expression.OrElse`. Test `MultipleUserTypeRolesWithRestrictions_CombineViaOr` validates CustomerId 1 ∨ 2 passes, 3 fails. |
| 7 | Security regressions vs legacy | ✅ PASS | Legacy `RowFilterService` returns `_ => false` on parse failure (deny-all); new broker does the same via `DenyAll<T>()`. Legacy returns `_ => true` when no filter configured; new broker returns `null`, and `OrderService` falls through to the existing legacy branch which is functionally equivalent for the current seed. No path widens access. The broker is additionally fail-closed for unauthenticated users (returns `null` → legacy fallback still applies). |
| 8 | DI registration consistency | ✅ PASS | `IRowRestrictionBroker` registered as `Scoped` in `ServiceCollectionExtensions`, consistent with `IRoleSwitcher`, `IRowFilterService`, `IPermissionService`, and other per-request services. Legacy `IRowFilterService` registration is preserved (not removed), matching the out-of-scope disclosure. |

### Additional Observations (Informational — no action required this session)

- **Token expansion surface area**: `BuildTokenMap` populates `CurrentUserId`, `CurrentUserCustomerId`, and `CurrentOrganizationId`. The legacy service only supported `@CurrentUserId`. The new tokens (`@CurrentUserCustomerId`, `@CurrentOrganizationId`) are additive and only activate when a `FilterExpression` references them — no regression path.

- **`DenyAll` detection heuristic**: `ReferenceEqualsDenyAll` checks `expr.Body is ConstantExpression { Value: false }`. The `DenyAll<T>()` lambda `_ => false` compiles to exactly this shape, so the log-once guard works correctly. If a user-authored expression legitimately evaluated to a constant `false` body (pathological but possible), it would suppress the warning — acceptable given the security outcome is identical (deny-all).

- **Legacy `IRowFilterService` still registered and on disk**: Confirmed as intentional per out-of-scope disclosure; no consumers call it from new code paths.

### Progress Keys

| Key | Status |
|-----|--------|
| `session-4/row-restriction-port` | ✅ Complete — `RowRestrictionExpressionBuilder` ports the grammar faithfully |
| `session-4/broker` | ✅ Complete — `IRowRestrictionBroker` + `RowRestrictionBroker` with full multi-role, baked-in short-circuit, and fail-closed semantics |
| `session-4/sample-integration` | ✅ Complete — `OrderService.ApplyRowFilter` consults the broker first |
| `session-4/integration-test` | ✅ Complete — 2 integration tests (restricted + unrestricted) against `WebApplicationFactory` |
| `session-4/build-and-test` | ✅ Complete — 0 errors, 0 warnings from new code; +20 new tests; 1 pre-existing unrelated failure |

**Verdict: VERIFIED — no Major or Critical issues identified. Session 4 is complete and ready for Session 5.**
