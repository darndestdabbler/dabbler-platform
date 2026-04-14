# Baseline Capture

## Purpose

This document records the current AcmeCoffeeOnline demo state that the rebuild should treat as its behavioral reference, not as its architectural template.

The current repo has known integration flaws and incomplete confidence areas, but it now has enough validated behavior to guide a cleaner rebuild.

## Current System Shape

- UI: Blazor Server app in `src/AcmeCoffeeOnline.Web/`
- Backend: ASP.NET Core REST API in `src/AcmeCoffeeOnline.Api/`
- Persistence: SQL Server
- UI libraries: Dabbler Components + MudBlazor
- Security: Dabbler Security + Dabbler Security Blazor
- Current repo topology: app repo with sibling source references to `../dabbler-components/` and `../dabbler-security/`

## Business Scope

The app is a coffee-shop demo covering:

- product browsing and management
- customer records
- order records and related order items/notes
- dashboard and reports
- backoffice/admin surfaces
- customer and backoffice authentication/authorization

## Roles And Authorization Model

The app's authorization intent is currently defined by [`security-config.json`](/c:/Users/denmi/source/repos/dabbler-demo/src/AcmeCoffeeOnline.Web/security-config.json).

### Roles

- `SystemAdmin`
- `BackofficeUserAdmin`
- `BackofficeUser`
- `Customer`

### Auth model

- backoffice users: Entra ID / OIDC challenge flow
- customers: magic-link sign-in with optional MFA pathway in the app design
- current dev/test helper: Development-only seeded-session sign-in flow for non-auth browser tests

## Confirmed Or Recently Validated Behavior

The following areas have been directly validated during remediation and should be treated as part of the current behavioral baseline.

### Shell and layout

- the Dabbler shell renders with the required component styles once the missing Dabbler CSS links are added to [`App.razor`](/c:/Users/denmi/source/repos/dabbler-demo/src/AcmeCoffeeOnline.Web/Components/App.razor)
- auth pages render correctly after separating them from the main shell layout and using [`AuthLayout.razor`](/c:/Users/denmi/source/repos/dabbler-demo/src/AcmeCoffeeOnline.Web/Components/Layout/AuthLayout.razor)
- shell-style regression coverage exists in [`ShellTests.cs`](/c:/Users/denmi/source/repos/dabbler-demo/tests/AcmeCoffeeOnline.Playwright/Navigation/ShellTests.cs)

### Login and auth flows

- the `LoginTests` Playwright class passes end-to-end with 13 passing tests
- login, register, logout, login-page rendering, and Entra challenge link rendering are currently validated
- real magic-link login works in dev when tests read the code from the dev-email log output rather than assuming MailHog delivery
- non-auth tests can establish deterministic sessions through the Development-only seeded sign-in path in [`Program.cs`](/c:/Users/denmi/source/repos/dabbler-demo/src/AcmeCoffeeOnline.Web/Program.cs)

### Permissions

- targeted permission regressions now pass for:
  - customer denial to admin users
  - customer denial to admin products
  - customer denial to security dashboard
  - backoffice-admin denial to admin products, by current config
  - backoffice-admin admin-menu visibility
- current page-permission intent includes:
  - `/admin/users`: `SystemAdmin`, `BackofficeUserAdmin`
  - `/admin/products`: `SystemAdmin` only
  - `/admin/security`: `SystemAdmin` only

### Current shell/navigation behavior

- the app uses `DabblerShell` as the authenticated workspace shell
- the main layout injects role-aware metadata/navigation and exposes an app-level admin menu
- the app currently supplements `DabblerSecurityNav` with its own admin menu because role naming does not align cleanly with the security nav’s built-in assumptions

## Current Areas To Treat As Only Partially Trusted

These areas should inform the rebuild, but should not be treated as fully proven or fully acceptable.

### Broader E2E confidence

- focused login, permission, and shell regressions are now credible
- the full Playwright suite still contains large surface area that has not yet been revalidated to the same standard
- some existing browser tests remain closer to page-smoke checks than meaningful E2E verification

### Authorization response shape

- some unauthorized direct requests return `404` rather than a rendered in-app "Access Denied" surface
- tests currently accept `404` as a valid denial signal for some admin routes
- the rebuild should make the desired denial behavior explicit instead of inheriting this ambiguity

### Dev email semantics

- the configured dev email sender type name suggests MailHog, but current behavior is log-based
- this mismatch is now understood, but it is still a product/library naming hazard that should not be preserved

### Library/app integration

- the current app works only after explicit app-level fixes for missing assets, auth layouts, and test helpers
- those integration points are now known, but they are still too fragile to serve as the target architecture

## Baseline Conclusion

The current repo is now good enough to answer "what should the rebuilt app do?" in the following categories:

- auth page rendering
- login/register/logout behavior
- representative role-based access behavior
- shell layout/styling requirements
- current role and permission intent from `security-config.json`

It is not good enough to serve as the architectural model for how the rebuilt repo should be structured.
