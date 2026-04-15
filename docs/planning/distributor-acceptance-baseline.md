# Distributor Acceptance Baseline

> **Purpose:** Capture the accepted distributor-era app baseline after the
> `docs/session-sets/distributor-reframe/` set so later Phase 2 work can rely
> on one durable domain handoff.
> **Created:** 2026-04-15
> **Source Inputs:** `docs/planning/distributor-domain-glossary.md`,
> `docs/planning/distributor-reframe-migration-map.md`,
> `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Dashboard.razor`,
> `src/Apps/AcmeCoffeeOnline.Web/Components/Layout/MainLayout.razor`,
> `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Stub/PlatformStubDemo.razor`,
> `tests/AcmeCoffeeOnline.Web.Tests/WebGuardrailTests.cs`, and
> `tests/AcmeCoffeeOnline.Playwright/SmokeTests.cs`

---

## Summary

The sample app now presents a coherent `AcmeCoffeeDistributors` story for the
accepted shell, dashboard, retail-business, order, product, report, and
stub-demo surfaces.

This closeout is intentionally a story and handoff baseline, not a solution-
wide rename. Current technical identifiers such as `Customer`,
`BackofficeUser`, existing routes, namespaces, and project names remain in
place until a later implementation set changes them deliberately.

## Accepted Distributor Baseline

### Business Identity And User-Facing Terms

- `AcmeCoffeeDistributors` is the canonical business identity for page titles,
  shell branding, dashboard copy, reviewer guidance, and sample-story
  narration.
- `Retail business` is the preferred business-facing term for the distributor's
  customer organizations.
- `Internal staff` is the preferred user-facing term for internal users and
  admin flows.
- The current `Customer` data surface remains the accepted technical bridge to
  retail-business records until later organization-aware implementation work
  lands.

### Shell, Navigation, And Reporting

- The accepted dashboard and shell now lead with distributor-era language,
  including `Retail Businesses`, `Orders by Retail Business`, and distributor
  catalog wording.
- The accepted empty-shell, auth, admin, and report surfaces all use the
  distributor story without changing their underlying technical routes.
- `/platform/stub` is part of the same accepted distributor narrative; it is no
  longer treated as a separate legacy demo that can keep older naming.

### Seeded Personas And Transitional Technical Identifiers

- The seeded baseline visibly supports internal staff, retail-business
  contacts, and system-administration flows.
- Existing technical role and entity identifiers such as `Customer`,
  `BackofficeUser`, `BackofficeUserAdmin`, and `SystemAdmin` remain the current
  implementation baseline for now.
- Later Phase 2 work may replace or refine those identifiers, but only when
  the owning session set also updates the supporting security, app, and test
  proof.

### Proof And Guardrails

- Repo guardrail and Playwright coverage now prove the distributor-facing shell
  title, retail-business wording, and representative sign-in or permission
  behavior.
- The accepted proof still relies on the current architecture-first app shape:
  project names, namespaces, page routes, API paths, and technical record
  models are preserved unless a later set explicitly changes them.

## Not Part Of This Baseline

- no solution-wide rename of `AcmeCoffeeOnline` project, namespace, or route
  identifiers
- no organization-membership or organization-switching implementation yet
- no immediate replacement of `Customer` or `BackofficeUser` technical names
  just because the user-facing story changed

## Downstream Implications

- `multi-org-security` should treat this document as the domain handoff for
  distributor terminology, seeded-persona framing, and the rule that
  user-facing terms may advance ahead of technical renames.
- `app-coverage-conformance`, E2E, UAT, and documentation work should use this
  baseline for business-language expectations while continuing to respect the
  preserved technical identifiers that still appear in the current code and
  tests.
- Historical planning artifacts from earlier session sets remain useful as
  implementation history, but this document is the current accepted baseline
  for distributor-story wording.
