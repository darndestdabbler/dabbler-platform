# Phase 2 Roadmap: Platform Proof, AcmeCoffeeDistributors, Multi-Org Security, and Release Readiness

This roadmap is the durable planning reference for upcoming session-set
creation in this repo and for related future projects that build on the same
platform direction.

Use it alongside `docs/planning/guiding-principles.md`, which now holds the
cross-project architecture, testing-hierarchy, and human-UAT workflow rules.

## Summary

Phase 2 should begin by strengthening the reusable platform itself before
expanding the sample app. The original Session Set 1 (Platform Contract,
Accessibility, and Stubbed UI Harness) has been **superseded by the
MudBlazor-native rebuild** (`mudblazor-native-rebuild`), which replaced the
custom wrapper layer with MudBlazor-native patterns across 8 sessions. The
rebuild established route-based navigation, explicit page composition,
MudBlazor-native grids/forms/dialogs, preserved the QBE filter model, and
added 57 new tests (bUnit conformance, filter panel interactions,
accessibility assertions). Downstream session sets should build on this
MudBlazor-native foundation.

The DI-driven stub capability remains a first-class platform feature, not
just a test trick, so it can support bUnit, demo and stubbed apps, future
generated apps, early design and navigation review, and downstream consumers.

After that, the sample app should shift early and fully to
`AcmeCoffeeDistributors`, where retail-business organizations are customers,
internal staff use a configurable `internal` organization concept, and
regional sales reps are modeled as users who hold organization-scoped
`UserAdmin` in multiple retail businesses.

End-to-end coverage should remain representative rather than exhaustive, but
that decision is paired with comprehensive unit and integration coverage and
broader bUnit coverage against real DI seams. UAT should be split into two
tracks: early stub-backed design and navigation spot testing and later
full-application spot testing based on a subset of representative Playwright
workflows.

## Ordered Session Sets

### 1. Platform Contract, Accessibility, and Stubbed UI Harness

> **Status: SUPERSEDED** by the `mudblazor-native-rebuild` session set
> (8 sessions, completed 2026-04-16). The rebuild replaced custom wrapper
> abstractions with MudBlazor-native patterns, established route-based
> navigation, added 57 new bUnit and conformance tests, and preserved the
> QBE filter model and DI-driven stub architecture. Downstream session sets
> should build on the MudBlazor-native foundation.

**Original purpose:** Finish proving the retained platform surface directly
and create the reusable DI-stub architecture.

**Rough session count:** 4-6 sessions (superseded by 8-session rebuild).

### 2. Early UAT: Stubbed Design and Navigation Spot Testing

**Purpose:** Catch layout, navigation, and interaction issues before the full
app is fully wired.

**Why now:** This uses the new stub capability to get fast human confirmation
of design and navigation behavior before later E2E investment.

**Prerequisites:** MudBlazor-native rebuild complete (supersedes Session Set 1).

**Rough session count:** 2-3 sessions.

**Likely scope:**

- Define a lightweight checklist-editor UAT checklist JSON for stub-backed
  review.
- Focus on page layout, tab order, visible focus, labeling, menu behavior,
  route-based navigation, and page-to-page navigation.
- Use stubbed data and providers so reviewers can exercise the UI without
  depending on full backend completeness.
- Capture issues as design and navigation remediation inputs for later session
  sets.

### 3. AcmeCoffeeDistributors Domain Reframe

**Purpose:** Replace the old app story with the distributor to retail-business
model.

**Why now:** All later security, workflow, and test work depends on the right
domain and seeded personas.

**Prerequisites:** Session Set 2 complete.

**Rough session count:** 2-4 sessions.

**Likely scope:**

- Reframe naming, navigation copy, seeded organizations, users, and sample
  workflows around `AcmeCoffeeDistributors`.
- Treat retail businesses as organizations with multiple employee users.
- Preserve platform-first architecture while making the sample app
  presentation-worthy and internally coherent.
- Remove lingering `AcmeCoffeeOnline` assumptions from seeds, docs, and tests.

### 4. Multi-Organization Security and User Administration

**Purpose:** Add and prove organization-aware identity, role assignment,
switching, and admin behavior.

**Why now:** This is the key architecture deepening for the distributor
scenario and the strongest additional security proof point.

**Prerequisites:** Session Set 3 complete.

**Rough session count:** 4-6 sessions.

**Likely scope:**

- Add organization membership and organization-scoped role assignments.
- Add active-organization context and organization switching.
- Keep `SystemAdmin` globally cross-organization.
- Add retail-business roles: `ViewOrders`, `MakeOrders`, and `UserAdmin`.
- Model regional sales reps as a persona that has `UserAdmin` across multiple
  retail businesses, not as a separate security role.
- Seed a dual-membership user with different roles per organization.
- Seed a regional sales rep with `UserAdmin` in multiple retail businesses.
- Use a configurable `internal` default-organization concept for internal and
  admin contexts.
- Prove organization-scoped admin limits, cross-org switching, and row-filter
  behavior.

### 5. Application Architecture and Coverage Conformance

**Purpose:** Ensure the distributor app fully exercises the platform and
aligns with [user-functionality-notes.md](/C:/Users/denmi/source/repos/dabbler-platform/docs/planning/user-functionality-notes.md).

**Why now:** After the domain and security model stabilize, the app can become
the durable proof harness.

**Prerequisites:** Session Set 4 complete.

**Rough session count:** 3-5 sessions.

**Likely scope:**

- Audit workflows against architecture notes and retained platform
  capabilities.
- Split mixed-responsibility methods into unit-testable and
  integration-testable seams.
- Fill coverage gaps in business logic, API behavior, organization-aware
  services, and security evaluation.
- Keep the sample app broad enough to prove the platform without letting it
  become product-specific scaffolding.

### 6. End-to-End Mini-Workflow Design

**Purpose:** Design representative, traceable E2E mini-workflows before
implementing them.

**Why now:** E2E design should be deliberate, requirement-driven, and informed
by the now-stable domain and security model.

**Prerequisites:** Session Set 5 complete.

**Rough session count:** 2-3 sessions.

**Likely scope:**

- Expand
  [example-of-end-to-end-mini-workflow-test.md](/C:/Users/denmi/source/repos/dabbler-platform/docs/planning/example-of-end-to-end-mini-workflow-test.md)
  into a complete design set.
- Define representative business workflows, edge cases, navigation
  expectations, validation expectations, and backend-truth checks.
- Include multi-organization and regional-sales-rep scenarios where they
  materially exercise the platform.
- Explicitly map which UI behaviors are covered by bUnit versus Playwright
  versus UAT.

### 7. End-to-End Mini-Workflow Implementation

**Purpose:** Implement and run the representative Playwright suite.

**Why now:** Depends on the designed workflows and stable app behavior.

**Prerequisites:** Session Set 6 complete.

**Rough session count:** 3-5 sessions.

**Likely scope:**

- Implement Playwright workflows with Docker-backed execution as the baseline.
- Keep E2E representative rather than exhaustive.
- Assert backend and database truth for important mutations.
- Add a small number of browser-only accessibility and focus or navigation
  checks where real browser proof is necessary.

### 8. Full-Application UAT Checklist and Human Confirmation

**Purpose:** Perform targeted human confirmation after automation is strong.

**Why now:** Human UAT should validate the remaining meaningful risks,
including confirming selected Playwright outcomes that automation alone can
miss.

**Prerequisites:** Session Set 7 complete.

**Rough session count:** 2-4 sessions.

**Likely scope:**

- Generate a second checklist-editor UAT checklist JSON focused on the full
  application.
- Base it on a subset of representative Playwright workflows plus a few
  intentionally human-only checks.
- Include selective database-verification scripts only where UI confirmation is
  insufficient.
- Use this set as both human spot testing and a confidence check against prior
  Playwright results.
- Include remediation sessions inside the set if needed.

### 9. Documentation Readiness

**Purpose:** Create technical and user documentation after accessibility,
platform proof, app proof, and UAT are complete.

**Why now:** Documentation should describe the validated Phase 2 system, not
an unstable intermediate state.

**Prerequisites:** Session Set 8 complete, and Session Set 1 accessibility
work complete.

**Rough session count:** 2-3 sessions.

**Likely scope:**

- Update technical docs, user docs, README, architecture docs, and
  test-strategy docs.
- Document stub mode, accessibility expectations, organization-aware security,
  and the testing pyramid.
- Keep the guidance platform-first, not Access-first.

### 10. NuGet Packaging, Consumer Validation, and VS Template Readiness

**Purpose:** Package the platform for external use and prepare for the future
CLI consumer story.

**Why now:** Packaging and templating should reflect the proven, documented,
accessibility-aware platform shape.

**Prerequisites:** Session Set 9 complete.

**Rough session count:** 3-5 sessions.

**Likely scope:**

- Validate packability of relevant libraries.
- Add consumer-install and package smoke tests.
- Develop and test a VS solution template that uses the narrowed, proven
  platform contract.
- Reuse the DI-stub architecture in a way that future generated apps can
  intentionally opt into.
- Keep all defaults platform-generic so future Access metadata tooling is only
  one consumer, not the design center.

## Architectural Additions and Interface Expectations

### Reusable DI-Driven Stub Capability

Treat reusable DI-driven stubbing as a supported platform feature.

Intended consumers:

- bUnit tests
- stub and demo app modes
- future generated apps
- early UAT and design review

Expected interface areas:

- data and query provider abstractions
- report provider abstractions
- metadata and layout provider abstractions
- navigation and workspace provider abstractions
- security and organization context abstractions where needed

### Organization-Aware Identity Model

Phase 2 should plan for:

- user-to-organization membership
- organization-scoped role assignment
- active-organization context

### Seeded Personas

Phase 2 should plan for these seeded personas:

- internal admins
- retail-business users
- regional sales rep persona with multi-org `UserAdmin`
- dual-membership user with different roles across organizations

## Testing Strategy

The Phase 2 test pyramid should be:

- unit tests: comprehensive for pure logic
- integration tests: comprehensive for non-UI boundaries
- bUnit: broad and comprehensive for component and page behavior using
  DI-driven stubs
- Playwright: representative end-to-end workflows only
- UAT:
  - early stub-backed design and navigation spot testing
  - later full-app spot testing based on selected Playwright flows

Both UAT layers should use the checklist editor schema and save human findings
inline in the checklist JSON instead of a separate pending-results scaffold.

Representative E2E is acceptable only because the lower layers are
intentionally strengthened.

## Important Public Interfaces and Artifacts

Phase 2 should explicitly produce or standardize:

- [phase-2-platform-roadmap.md](/C:/Users/denmi/source/repos/dabbler-platform/docs/planning/phase-2-platform-roadmap.md)
  as the durable roadmap artifact
- [distributor-acceptance-baseline.md](/C:/Users/denmi/source/repos/dabbler-platform/docs/planning/distributor-acceptance-baseline.md)
  as the accepted distributor-domain handoff for later security, app-proof,
  E2E, UAT, and documentation work
- one stub-backed UAT checklist JSON artifact for early design and navigation
  review
- one full-app UAT checklist JSON artifact for later human confirmation
- reusable DI abstractions for stub and real provider swapping
- session sets that distinguish:
  - reusable platform proof
  - sample-app business proof
  - representative E2E proof
  - human confirmation

## Test Cases and Acceptance Criteria

Before Phase 2 is considered complete, the roadmap expects these outcomes:

- retained platform surfaces have direct contract tests and WCAG 2.1 AA
  evidence
- reusable stubs work in bUnit and in stub or demo app mode
- early UAT catches design and navigation problems before full app completion
- multi-organization security is proven with seeded dual-membership scenarios
- Playwright covers representative end-to-end workflows with backend-truth
  validation
- later UAT confirms a subset of Playwright outcomes and catches issues
  automation can miss
- documentation, packages, and templates reflect the proven platform rather
  than historical assumptions

## Assumptions and Defaults

Record these Phase 2 defaults explicitly:

- accessibility verification is required before documentation-focused work
- DI stubbing is a reusable platform capability, not a repo-internal trick
- regional sales rep is a persona, not a separate security role
- regional sales reps are `UserAdmin` across multiple retail businesses by
  default, without automatic order permissions
- `internal` remains a configurable default-organization concept
- the sample app shift to `AcmeCoffeeDistributors` happens early
- the future Access metadata-harvesting CLI is a downstream consumer, not the
  platform’s design center
