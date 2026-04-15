# Accessibility And Stubbing Review Ledger

> **Purpose:** Freeze the Session 1 baseline, queue decisions, and initial
> direct-proof targets for `docs/session-sets/accessibility-and-stubbing/`.
> **Created:** 2026-04-15
> **Source Inputs:** `docs/planning/phase-2-platform-roadmap.md`,
> `docs/planning/notes-driven-supported-library-surface.md`,
> `docs/planning/notes-driven-app-acceptance-baseline.md`,
> `docs/planning/user-functionality-notes.md`, and the current Phase 2
> session-set specs

---

## Session 1 Verdict

The current `accessibility-and-stubbing` spec still matches the Phase 2
roadmap, the retained supported-surface definition, the accepted app baseline,
and the completed `notes-driven-template-encoding` handoff.

No rebaseline of this session set's core purpose is needed.

The durable Session 1 outcomes are:

- keep the scope centered on direct proof of the retained reusable surface
- treat WCAG 2.1 AA evidence as a required deliverable before
  documentation-focused work
- treat DI-driven stubbing as a reusable platform capability, not a repo-local
  testing trick
- record conservative overlap rules explicitly instead of assuming them

## Queue And Parallelization Decisions

### Cross-Set

No broad Phase 2 reordering is safe today.

The only newly safe overlap is a narrow one:

- `stubbed-uat` Session 1 and Session 2 may begin after
  `accessibility-and-stubbing` Session 3 completes and the reusable stub
  abstractions are frozen
- `stubbed-uat` Session 3 remains blocked until
  `accessibility-and-stubbing` Session 4 has produced enough stub-backed proof
  and reviewable surfaces to avoid false negatives
- all later Phase 2 sets remain sequential behind the validated
  `stubbed-uat` to `distributor-reframe` to `multi-org-security` chain

### Intra-Set

Inside `accessibility-and-stubbing`, no implementation parallelization should
start before Session 3 freezes the shared stub contracts.

After that freeze, these Session 4 slices are safe to split:

- `Dabbler.Components` direct proof work
- `Dabbler.Reports.Blazor` direct proof work
- accessibility evidence capture for the retained reusable surfaces

### Coordination Rules

- Session 3 must explicitly declare the stub abstractions and provider seams
  frozen before any overlapping work starts
- overlapping work must treat the retained supported surface as fixed input and
  must not reopen compatibility-only or retirement-candidate features
- `stubbed-uat` may author scope and checklist assets early, but it must keep a
  readiness ledger of which stub-backed pages are actually mature enough for
  human review

## Frozen Coverage Targets

### Components

Later sessions should prove the retained `Dabbler.Components` contract
directly in `tests/Dabbler.Components.Tests/` for:

- `DabblerQueryPage<T>` composition and retained query workflow behavior
- `DabblerDataGrid<T>` rendering, paging, sorting, selection, CRUD-affordance,
  and keyboard/focus behavior where practical
- `DabblerFilterGrid<T>` row composition, operator behavior, summary output,
  and keyboard/focus behavior where practical
- `DabblerComboBox<T>` search, binding, cascading, multicolumn rendering, and
  accessibility semantics
- `DabblerMultiComboBox<T>` retained multiselect rendering, state, and
  accessibility semantics
- `DabblerDetailForm<T>` validation, save/cancel state, child-region behavior,
  and role-aware or state-aware affordances where practical
- `DabblerRecordPopup<T>` modal rendering, validation affordances, and command
  behavior
- `DabblerShell` navigation, workspace composition, and provider-driven state
- `DabblerDashboardPage` navigation-card, quick-link, and focused-search
  behavior

### Reports

Later sessions should prove the retained report UI contract directly in
`tests/Dabbler.Reports.Blazor.Tests/` for:

- `DabblerReportPage` composition, prompt-to-viewer handoff, and export-state
  expectations
- `ParameterPromptForm` parameter rendering, validation, and execution
  callbacks
- `ReportViewer` rendered output, grouped/banded presentation seams where
  applicable, and PDF-oriented viewer expectations

The current reports test home still needs dedicated `ParameterPromptForm` and
`ReportViewer` test files rather than relying on smoke coverage and broader
`DabblerReportPage` proof alone.

### Security-Adjacent UI Seams

Later sessions should add or strengthen direct proof for reusable
security-adjacent UI behavior without reopening the broader security roadmap:

- role-aware or permission-aware conditional rendering in retained component or
  page surfaces
- organization-aware UI state only where the retained reusable surface
  genuinely depends on it
- handoff boundaries between retained components and the existing
  `Dabbler.Security.Blazor` suites

This set should not expand into full multi-organization feature design. It
should only freeze the reusable proof targets that later security work will
depend on.

### Accessibility Evidence

Later sessions should produce both automated and focused manual WCAG 2.1 AA
evidence for the retained reusable surfaces.

Minimum evidence targets:

- durable ARIA and semantic assertions in the direct bUnit suites
- targeted automated accessibility scan results for retained reusable surfaces
- focused manual checks for keyboard navigation, tab order, visible focus,
  labeling, and live-region or status announcements where applicable
- explicit evidence for shell or workspace behavior and for report prompting
  and viewing surfaces, not only grid-level semantics

## Execution Notes For Later Sessions

- Session 2 should convert this ledger into a gap inventory with clear
  ownership across components, reports, and security-adjacent UI seams
- Session 3 should keep the stub contracts platform-generic so future
  generated apps and stub or demo modes can consume them intentionally
- Session 4 is the highest schedule-risk session because the repo currently has
  only a light direct-proof baseline in the component and report test homes
- Session 5 should only add catch-up scope if the direct proof or accessibility
  work exposes a real omission inside this set
