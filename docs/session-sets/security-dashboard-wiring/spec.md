# Security Dashboard Wiring Spec

> **Purpose:** Wire the AcmeCoffeeOnline Security Dashboard tabs to the
> Dabbler.Security registration API so that Page Access, Entity Access,
> Endpoint Access, Control Permissions, Row Filters, and User Role
> Assignments are populated and functional at runtime.
> **Created:** 2026-04-16
> **Session Set:** `docs/session-sets/security-dashboard-wiring/`
> **Prerequisite:** `post-rebuild-uat` remediation complete. The Security
> Dashboard page renders and the duplicate-header fix is in place.
> **Workflow:** Orchestrator -> AI Router -> Cross-provider verification

---

## Problem Statement

The Security Dashboard at `/admin/security` currently shows empty tabs:

- "No pages registered. Register pages via options.RegisterPages() at startup."
- "No entities registered. Register entities via options.RegisterEntities()
  at startup."
- Likewise for Endpoint Access, Control Permissions, and Row Filters.
- User Role Assignments tab shows no users.

The API project (`AcmeCoffeeOnline.Api`) already has full registration calls
in its `AddDabblerSecurity` configuration and `SecuritySeedData.cs` populates
a complete `SecurityConfiguration` with role hierarchy, page permissions,
entity CRUD rules, field visibility, endpoint access, control states, and row
filters.

The Web project (`AcmeCoffeeOnline.Web`) either does not call the same
registration methods, or the dashboard components are not reading from the
correct configuration source.

## Goals

1. Every Security Dashboard tab shows populated, meaningful data for the
   AcmeCoffeeOnline application's pages, entities, endpoints, and controls.
2. Runtime permission changes made through the dashboard are persisted and
   take effect without restart.
3. The role hierarchy (Customer -> BackofficeUser -> BackofficeUserAdmin ->
   SystemAdmin) is visible and correctly represented.
4. User Role Assignments shows the seeded users with their role memberships.

## Architecture Context

### Registration API (Dabbler.Security)

All registration is done via `DabblerSecurityOptions` in `Program.cs`:

```csharp
builder.Services.AddDabblerSecurity(options =>
{
    options.RegisterPages(pages => { ... });
    options.RegisterEntities(entities => { ... });
    options.RegisterEndpoints(endpoints => { ... });
    options.RegisterControls(controls => { ... });
});
```

### Persistence

Two backends are supported:
- `UseJsonFileStore(path)` — JSON file, suitable for development
- `UseEfCoreStore()` — SQL Server via EF Core, suitable for production

### Dashboard Components

Each tab reads from `SecurityConfiguration` via `SecurityConfigurationManager`:
- **Roles** — `DabblerRoleManager` — role definitions with inheritance
- **Page Access** — `DabblerPagePermissionMatrix` — Allow/Deny per role per page
- **Entity Access** — `DabblerEntityPermissionMatrix` — CRUD matrix per role per entity,
  plus `DabblerFieldPermissionEditor` for field-level visibility (Editable/ReadOnly/Hidden)
- **Endpoint Access** — `DabblerEndpointPermissionMatrix` — Allow per role per endpoint
- **Control Permissions** — `DabblerControlPermissionTab` — Enabled/Disabled/Hidden per role per control
- **Row Filters** — `DabblerRowFilterTab` — filter expressions scoping entity data per role
- **User Role Assignments** — role membership grid

### Existing API Registration

`src/Apps/AcmeCoffeeOnline.Api/Program.cs` already registers:
- 7 pages (/, /products, /customers, /orders, /admin/users, /admin/products, /admin/security)
- 3 entities with fields (Product, Customer, Order)
- Multiple API endpoints (GET/POST/PUT products, customers, orders)
- UI controls (create/delete buttons)

`src/Apps/AcmeCoffeeOnline.Api/Data/SecuritySeedData.cs` provides a complete
`SecurityConfiguration` with permissions for all 4 roles.

---

## Session Set Configuration

```yaml
requiresUAT: false
requiresE2E: false
```

## Sessions
### Session 1 of 2: Registration and Configuration Wiring

**Steps:**
1. Read `docs/planning/guiding-principles.md` and
   `docs/planning/lessons-learned.md`.
2. Investigate how the Web app's `AddDabblerSecurity` call is currently
   configured — identify what's missing vs the API's configuration.
3. Add `RegisterPages`, `RegisterEntities`, `RegisterEndpoints`, and
   `RegisterControls` calls to the Web app's security configuration,
   matching the API's resource registrations for the pages this app serves.
4. Configure the persistence backend (`UseJsonFileStore` for development)
   so runtime changes are saved.
5. Ensure the `SecurityConfigurationManager` is seeded with the role
   hierarchy and default permissions from `SecuritySeedData` on first run
   (when no persisted configuration exists).
6. Verify each dashboard tab populates:
   - Roles tab shows Customer, BackofficeUser, BackofficeUserAdmin, SystemAdmin
   - Page Access shows the 7 registered pages with role permissions
   - Entity Access shows Product, Customer, Order with CRUD matrix
   - Endpoint Access shows registered API endpoints
   - Control Permissions shows registered UI controls
   - Row Filters shows any configured row-level filters
   - User Role Assignments shows seeded users
7. Build and test.

**Creates:** Updated Web app security configuration, security-config.json seed
**Touches:** `src/Apps/AcmeCoffeeOnline.Web/Program.cs`, possibly new seed/config files
**Ends with:** All dashboard tabs populated with real data
**Progress keys:** `session-1/investigate`, `session-1/registration`,
`session-1/persistence`, `session-1/seed-config`, `session-1/verify-tabs`,
`session-1/build-and-test`

---

### Session 2 of 2: Runtime Enforcement Verification and Testing

**Steps:**
1. Read `docs/planning/guiding-principles.md` and
   `docs/planning/lessons-learned.md`.
2. Verify runtime permission changes take effect:
   - Change a page permission in the dashboard (e.g., deny BackofficeUser
     access to /products). Verify the change persists across app restart.
   - Verify the denied user sees access-denied when navigating to that page.
   - Restore the original permission.
3. Verify entity permission changes:
   - Modify CRUD permissions for a role. Verify the API enforces the change.
4. Verify field-level visibility:
   - Set a field to Hidden or ReadOnly for a role. Verify the UI respects it.
5. Test control permissions:
   - Set a control to Disabled or Hidden for a role. Verify in the UI.
6. Add targeted tests (unit or integration) for the configuration loading
   and enforcement paths.
7. Build and test.

**Creates:** Enforcement verification tests, documented verification results
**Touches:** Test files, possibly enforcement middleware/attributes
**Ends with:** Confidence that the Security Dashboard is not just visual but
functional — changes made there are enforced by the application
**Progress keys:** `session-2/page-enforcement`, `session-2/entity-enforcement`,
`session-2/field-enforcement`, `session-2/control-enforcement`,
`session-2/tests`, `session-2/build-and-test`

---

## What This Set Preserves

| Existing Asset | Status |
|---|---|
| All existing Playwright tests (75) | Must continue passing |
| All non-Playwright tests (467) | Must continue passing |
| Current page authorization via DabblerPageAuthorize | Preserved |
| Current role hierarchy from SecuritySeedData | Preserved, made visible |
| Current seeded user data | Preserved |

## Risk Notes

- **Fail-Open Policy:** The framework defaults to `UnregisteredResourcePolicy.FailOpen`.
  This means unregistered resources are accessible. Session 2 should verify this
  behavior is intentional and document it.
- **Dual Registration:** Both the API and Web apps may need matching registration
  calls if they serve different resource sets. Ensure registrations are consistent.
- **Persistence Backend:** Using `UseJsonFileStore` in development means the
  config file must be in `.gitignore` if it contains runtime changes. The seed
  data should provide the baseline; runtime changes layer on top.
