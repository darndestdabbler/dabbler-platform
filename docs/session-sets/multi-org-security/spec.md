# Multi-Organization Security And User Administration Spec

> **Purpose:** Add and prove organization-aware identity, role assignment,
> switching, row filtering, and admin behavior for the distributor scenario.
> **Created:** 2026-04-15
> **Session Set:** `docs/session-sets/multi-org-security/`
> **Prerequisite:** `distributor-reframe` is complete and its accepted
> distributor baseline is current.
> **Workflow:** Orchestrator -> AI Router -> Cross-provider verification

---

## Project Overview

This set is the main architecture deepening in Phase 2.

The target model is:

- one user may belong to multiple organizations
- roles are assigned per organization
- active-organization context participates in runtime evaluation
- `SystemAdmin` remains globally cross-organization
- regional sales reps are a seeded persona with multi-org `UserAdmin`, not a
  separate role
- `internal` remains a configurable default-organization concept for internal
  and admin flows

Treat `docs/planning/multi-organization-user-administration-input.md` and
`docs/planning/user-functionality-notes.md` as binding planning inputs for
defaults, seeded personas, and proof expectations.

Treat `docs/planning/distributor-acceptance-baseline.md` as the current domain
handoff for user-facing terminology, transitional technical identifiers, and
the accepted seeded-story baseline.

## Parallelization Notes

No downstream Phase 2 session set is marked safe to overlap at authoring time.
Session 1 must check whether any later work becomes conservatively safe once
the organization-aware contract is frozen.

Session 1 confirmed that no cross-set overlap is safe before
`multi-org-security` completes.

Inside this set, a narrow overlap is acceptable only after Session 2 freezes
the membership and seeded-persona contract:

- Session 3 persistence, API, and runtime permission work may proceed in
  parallel with the Session 4 organization-switching UI work when the edits
  stay on disjoint files and both slices use the Session 1 decision ledger and
  Session 2 membership contract as the shared merge baseline
- the rest of Session 4 remains blocked on Session 3 because admin-limit proof
  and organization-aware row-filter proof depend on the implemented active-
  organization runtime semantics

## Session Plan

### Session 1 of 5: Review, Default Decisions, And Queue Scan

**Steps:**
1. Review the distributor baseline, the multi-organization planning input, and
   all remaining Phase 2 session-set specs before implementation begins.
2. Resolve the Phase 2 defaults that must be frozen early, including the
   configurable `internal` organization concept, active-organization behavior,
   organization-selection timing, and the initial list of organization-scoped
   versus global entities.
3. Record any conservative adjustments needed in this set or downstream specs,
   including newly safe parallelization opportunities.
4. Capture the architecture decision ledger that later sessions will implement.

**Creates:** Multi-org decision ledger and architecture baseline
**Touches:** This spec, downstream Phase 2 specs if needed, and planning notes
**Ends with:** The set has explicit defaults instead of carrying security-model
ambiguity into implementation
**Progress keys:** `session-1/review-scan`,
`session-1/default-decisions`, `session-1/architecture-ledger`,
`session-1/parallelization-update`

---

### Session 2 of 5: Membership, Role-Assignment, And Seed Model

**Steps:**
1. Implement the organization membership and organization-scoped role-assignment
   model in persistence and core domain code.
2. Add or update the configurable default-organization behavior for internal
   and admin contexts.
3. Seed the required personas, including a dual-membership user with different
   roles per organization and a regional sales rep with `UserAdmin` across
   multiple retail businesses.
4. Establish migration or data-shape guidance for existing sample data where
   needed.

**Creates:** Organization-aware membership model and seeded persona baseline
**Touches:** Security or domain libraries, app or API persistence layers, seed
assets, and related docs
**Ends with:** The repo has a concrete organization-aware identity shape for
later permission, UI, and test work
**Progress keys:** `session-2/membership-model`,
`session-2/org-role-assignments`, `session-2/internal-default-org`,
`session-2/seeded-personas`

---

### Session 3 of 5: Active-Organization Context And Permission Evaluation

**Steps:**
1. Add active-organization context to the runtime security model and session or
   request flow.
2. Update permission evaluation and row-filter behavior so organization context
   and organization-scoped roles are applied consistently.
3. Preserve unrestricted cross-organization behavior for `SystemAdmin` and
   appropriate developer tooling where required.
4. Add or refine developer and system-admin switching tools so role and
   organization switching remain provable and diagnosable.

**Creates:** Active-organization runtime model and organization-aware
permission evaluation
**Touches:** Security evaluation code, session helpers, row-filtering services,
and relevant tests
**Ends with:** Organization context participates in authorization and data
visibility consistently
**Progress keys:** `session-3/active-org-context`,
`session-3/permission-evaluation`, `session-3/row-filter-behavior`,
`session-3/switching-tooling`

---

### Session 4 of 5: Admin Flows, UI Switching, And Cross-Org Proof

**Steps:**
1. Update admin APIs and UI flows so `UserAdmin` behavior is scoped to the
   organizations where the current user holds that role.
2. Add organization switching and organization-aware role state to the UI in a
   way that is clear, testable, and consistent with the distributor model.
3. Prove that organization-scoped admins can elevate other users only within
   their authorized organizations while `SystemAdmin` remains unrestricted.
4. Add focused tests for cross-organization switching, admin limits, and
   representative row-filter behavior.

**Creates:** Organization-aware admin flows and cross-org proof tests
**Touches:** `src/Apps/`, APIs, security-blazor surfaces, and integration or
UI tests
**Ends with:** The main multi-organization security behaviors are visible and
provable through the app and supporting tests
**Progress keys:** `session-4/admin-flows`,
`session-4/org-switching-ui`, `session-4/admin-limits-proof`,
`session-4/cross-org-tests`

---

### Session 5 of 5: Security Baseline, Queue Rebaseline, And Closeout

**Steps:**
1. Capture the accepted multi-organization security baseline, including seeded
   personas, default-organization behavior, and the approved switching model.
2. Revisit the remaining Phase 2 queue and adjust assumptions only where the
   implemented security model changed them materially.
3. Append one catch-up session only if the work exposed a direct omission that
   belongs inside this set.
4. Generate `change-log.md` for this session set.

**Creates:** Multi-org security baseline and `change-log.md`
**Touches:** Planning docs, downstream Phase 2 specs if needed, and closeout
artifacts
**Ends with:** Later coverage, E2E, UAT, documentation, and packaging work can
depend on a stable organization-aware security model
**Progress keys:** `session-5/security-baseline`,
`session-5/queue-rebaseline`, `session-5/catch-up-decision`,
`session-5/change-log`
