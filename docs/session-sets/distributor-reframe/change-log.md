# Change Log: distributor-reframe

## Overview

This 3-session set is complete.

The primary goal was to replace the older `AcmeCoffeeOnline` story with an
accepted `AcmeCoffeeDistributors` distributor-to-retail-business baseline that
later security, app-proof, E2E, UAT, and documentation work can rely on
directly.

The set successfully froze the distributor glossary, reframed the main app and
stub/demo story, and closed with a durable accepted-baseline artifact plus the
minimal downstream spec updates needed to keep the remaining Phase 2 queue
aligned.

## Session Highlights

### Session 1: Review, Domain Glossary, And Queue Scan

- Reviewed the accepted app baseline, completed stubbed-UAT outputs, and the
  remaining Phase 2 session-set specs.
- Froze the canonical distributor glossary and created the legacy-to-
  distributor migration map.
- Confirmed that no new cross-set parallelization was safe before this set
  completed.

### Session 2: App, Seed, And Navigation Reframe

- Reframed the application shell, dashboard, auth/admin copy, reports, and
  sample-story content around `AcmeCoffeeDistributors`.
- Updated the retained stub/demo route and reviewer guidance so `/platform/stub`
  now participates in the same distributor narrative as the main app.
- Preserved the existing platform-first architecture, routes, namespaces, and
  technical entity keys while updating user-facing terminology and proof.

### Session 3: Test Cleanup, Acceptance Baseline, And Closeout

- Cleaned the remaining active test and planning references that still read
  like the older app story.
- Created `docs/planning/distributor-acceptance-baseline.md` as the durable
  handoff for accepted distributor terminology, seeded-story framing, and
  transitional technical identifiers.
- Rebaselined the remaining Phase 2 queue minimally by referencing the new
  accepted baseline from the roadmap and the downstream security, app-proof,
  E2E, UAT, and docs-readiness specs.

## Durable Baseline

The `distributor-reframe` session set is closed.

The repo now has one accepted distributor-era story for the shell, dashboard,
retail-business workflows, report naming, and stub/demo guidance.

Project names, namespaces, routes, and current technical identifiers such as
`Customer`, `BackofficeUser`, `BackofficeUserAdmin`, and related API shapes
were intentionally preserved for now. Later implementation sets may change
them only when they also update the supporting security, app, and test proof.

## Downstream Handoff

Later Phase 2 work should use
`docs/planning/distributor-acceptance-baseline.md` as the accepted domain
handoff for distributor wording and transitional technical-name guidance.

No broader queue rewrite is justified by this closeout. The downstream spec
updates are intentionally limited to anchoring that new baseline artifact and
avoiding premature technical-renaming churn.

## Final Validation

The final commit for this session set passed the required build and test steps.

- `dotnet build Dabbler.Platform.sln` succeeded with the existing 11
  `DevEmailInbox` XML-comment warnings plus preview-SDK notices.
- `dotnet test Dabbler.Platform.sln --no-build` passed.
- Total tests passed: `383`
  - `AcmeCoffeeOnline.Web.Tests`: 21
  - `AcmeCoffeeOnline.Api.Tests`: 1
  - `Dabbler.Security.Tests`: 207
  - `Dabbler.Security.Blazor.Tests`: 120
  - `Dabbler.Components.Tests`: 21
  - `Dabbler.Reports.Blazor.Tests`: 7
  - `AcmeCoffeeOnline.Playwright`: 6
