# Integrated Library Home

This repo's reusable libraries live together under `src/Libs/` so app,
library, and workflow work can ship from one solution without sibling-repo
coordination.

For the platform's intended audience, scope, and adoption path, read
[`docs/platform-overview.md`](../../docs/platform-overview.md) first.

## Consumable libraries

- [`Dabbler.Components`](Dabbler.Components/README.md) — QBE filter UI,
  expression engine, CRUD master/details helpers, data-provider
  abstractions.
- [`Dabbler.Platform.Stubs`](Dabbler.Platform.Stubs/README.md) — opt-in
  stub providers for bUnit tests, demos, and future generated apps.
- [`Dabbler.Reports.Core`](Dabbler.Reports.Core/README.md) — banded
  report layout engine, RDF format, expression evaluation.
- [`Dabbler.Reports.Export`](Dabbler.Reports.Export/README.md) — PDF
  export of composed report pages.
- [`Dabbler.Reports.Blazor`](Dabbler.Reports.Blazor/README.md) — Blazor
  UI for the reporting workflow (parameter prompt, viewer).
- [`Dabbler.Security`](Dabbler.Security/README.md) — authentication
  (magic link, TOTP MFA, external IdP) plus restriction-based
  authorization with sensitive-field masking.
- [`Dabbler.Security.Blazor`](Dabbler.Security.Blazor/README.md) —
  Blazor UI for security (login, admin pages, dashboard,
  `DabblerSecured`, role switcher).
- [`Dabbler.Testing.Blazor`](Dabbler.Testing.Blazor/README.md) —
  deterministic Playwright synchronization watermark plus bUnit helpers.

## Development helpers (not consumable libraries)

- `Dabbler.Security.DevIdp` — a small ASP.NET Core web project that
  runs a local OIDC identity provider for testing external-login flows
  during development. Loopback-only; not deployed to production.

## Library tests

Per-library test projects live under `tests/`:

- `tests/Dabbler.Components.Tests`
- `tests/Dabbler.Reports.Blazor.Tests`
- `tests/Dabbler.Security.Tests`
- `tests/Dabbler.Security.Blazor.Tests`
- `tests/Dabbler.Testing.Blazor.Tests`
