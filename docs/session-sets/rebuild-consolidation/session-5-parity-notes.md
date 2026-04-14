# Session 5 Parity Notes

## Scope landed in Session 5

- Imported the required component/report libraries into `src/Libs/`:
  - `Dabbler.Components`
  - `Dabbler.Reports.Core`
  - `Dabbler.Reports.Export`
  - `Dabbler.Reports.Blazor`
- Added `src/Apps/AcmeCoffeeOnline.Shared/` and migrated the shared DTOs, models, and enums used by the rebuilt app.
- Replaced the placeholder Web/API foundations with the proven AcmeCoffeeOnline app slices from the reference repo, retargeted to integrated source references.
- Preserved integrated-repo guardrails by keeping a single Dabbler asset-linking entrypoint, explicit health diagnostics, and Development/Test-only seeded-session helpers for deterministic automation.
- Updated browser and server smoke coverage so `dotnet test Dabbler.Platform.sln` now exercises the migrated shell, login page, health endpoints, and self-starting Playwright smoke path.

## App parity now present

- Cookie-authenticated Blazor Server host wired to integrated `Dabbler.Security` and `Dabbler.Security.Blazor`
- Magic-link and external OIDC entry points on the rebuilt login flow
- Permission-aware Dabbler shell with dashboard, products, customers, orders, admin, and report navigation
- Shared DTO/model surface and API controllers/services for products, customers, orders, order notes, and registration
- Report definitions and report-page wiring for the initial Acme reporting surfaces

## Intentional carry-forward choices

- The integrated repo now owns the active app, libraries, workflow assets, and smoke-test conventions.
- Seeded-session automation remains Development/Test-only and loopback/secret guarded.
- Playwright now self-starts the local Web and API hosts when needed so solution-level test runs stay deterministic.

## Deferred from Session 5

- Deeper end-to-end assertions for mutating workflows and backend truth verification remain Session 6 work.
- Final cutover/archive guidance for the older repos remains Session 6 work.
- Additional component/report projects beyond the currently required app surface remain intentionally out of scope until a concrete parity need exists.
