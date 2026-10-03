# Security Dashboard Wiring — Change Log

## Overview

This session set wired the AcmeCoffeeOnline Security Dashboard to the
Dabbler.Security registration API so that all dashboard tabs are populated and
functional at runtime, and added comprehensive enforcement tests proving that
runtime permission changes are enforced by the application.

## Session 1 of 2: Registration and Configuration Wiring

- Added `RegisterPages` (7 pages), `RegisterEntities` (5 entities with fields),
  `RegisterEndpoints` (21 endpoints), and `RegisterControls` (5 controls) to the
  Web app's `AddDabblerSecurity` configuration.
- Created `EfUserListProvider` to replace `NullUserListProvider` for the User
  Role Assignments tab.
- Verified `UseJsonFileStore` was already configured with `security-config.json`
  containing complete seed data: 4 roles, 7 page permissions, 5 entity
  permissions, 5 field permissions, 20 endpoint permissions, 5 control
  permissions, 1 row filter.
- Build: 0 errors. All 467 non-Playwright tests pass.

## Session 2 of 2: Runtime Enforcement Verification and Testing

- Created `AcmeCoffeeSecurityEnforcementTests.cs` with 63 new tests covering
  all permission types with the AcmeCoffeeOnline seed data:
  - Page access enforcement (17 tests): all roles x key pages, role inheritance,
    deny-precedence
  - Entity CRUD enforcement (18 tests): Product/Order CRUD per role, runtime
    modification
  - Field visibility enforcement (6 tests): UnitPrice access levels, runtime
    Hidden change, most-permissive-wins
  - Control permission enforcement (12 tests): button states per role, runtime
    Hidden change
  - Endpoint access enforcement (10 tests): API endpoint access per role
  - Persistence round-trip (3 tests): JSON store save/reload, runtime
    modification with persistence, manager save-and-refresh
  - Unauthenticated user denial (1 test)
- Cleaned up sample project references: removed 13 tests referencing deleted
  `samples/` directory, inlined `CookieHandler` to preserve OIDC tests.
- Build: 0 errors, 0 warnings. All 517 non-Playwright tests pass.

## Files Changed

| File | Action |
|---|---|
| `src/Apps/AcmeCoffeeOnline.Web/Program.cs` | Modified: added security registration calls |
| `src/Apps/AcmeCoffeeOnline.Web/Services/EfUserListProvider.cs` | Created |
| `tests/Dabbler.Security.Tests/Integration/AcmeCoffeeSecurityEnforcementTests.cs` | Created: 63 enforcement tests |
| `tests/Dabbler.Security.Tests/Dabbler.Security.Tests.csproj` | Modified: removed sample project refs |
| `tests/Dabbler.Security.Tests/Integration/OidcIntegrationTests.cs` | Modified: inlined CookieHandler |
| `tests/Dabbler.Security.Tests/DevIdp/DevIdpOidcFlowTests.cs` | Deleted (9 tests) |
| `tests/Dabbler.Security.Tests/Integration/WebApiIntegrationTests.cs` | Deleted (4 tests) |

## Test Impact

| Suite | Before | After |
|---|---|---|
| Dabbler.Security.Tests | 211 | 261 (removed 13 sample-dependent, added 63 enforcement) |
| Total non-Playwright | 467 | 517 |
| Playwright E2E | 67 pass / 8 pre-existing fail | Unchanged |

## AI Router Cost

$0.36 across 2 routed API calls (1 Opus verification, 1 Gemini-Pro verification).
