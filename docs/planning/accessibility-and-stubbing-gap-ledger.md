# Accessibility And Stubbing Gap Ledger

> **Purpose:** Capture the Session 2 contract-gap inventory, accessibility
> baseline, and WCAG evidence plan for
> `docs/session-sets/accessibility-and-stubbing/`.
> **Created:** 2026-04-15
> **Source Inputs:** `docs/session-sets/accessibility-and-stubbing/spec.md`,
> `docs/planning/accessibility-and-stubbing-review-ledger.md`,
> `docs/planning/notes-driven-supported-library-surface.md`,
> `docs/planning/notes-driven-app-acceptance-baseline.md`,
> `docs/planning/phase-2-platform-roadmap.md`,
> `docs/planning/user-functionality-notes.md`, the current reusable-surface
> test homes, and routed Session 2 analysis

---

## Session 2 Verdict

The retained reusable surface now has dedicated test homes, but the direct
proof baseline is still intentionally light.

Current proof is strongest where the repo already asserts a small contract
slice:

- `DabblerQueryPage<T>` manual search flow and advanced-action opt-ins
- `DabblerFilterGrid<T>` summary generation across rows and groups
- `DabblerDataGrid<T>` top-level accessible grid shell attributes
- `DabblerComboBox<T>` and `DabblerMultiComboBox<T>` retained public surface
  plus selected-chip rendering
- `DabblerDetailForm<T>`, `DabblerRecordPopup<T>`, `DabblerShell`, and
  `DabblerDashboardPage` minimal workflow or composition checks
- `DabblerReportPage` one successful auto-run flow
- `DabblerSecured` and `DabblerRoleSwitcher` isolated security-adjacent
  rendering and activator semantics

That baseline is enough to prove the retained surface still exists, but not
enough to prove the reusable contracts directly.

The highest remaining risks are:

- `ParameterPromptForm` and `ReportViewer` still have no dedicated direct-proof
  test files
- keyboard navigation, focus management, and live-region behavior are mostly
  unproven across the retained interactive surfaces
- role-aware or state-aware affordances inside reusable components are still
  mostly inferred from isolated security tests instead of being proved where
  the components consume those seams

## Contract Gap Ledger

### Components

| Surface | Current direct proof | Remaining gap to close | Expected session handoff |
| --- | --- | --- | --- |
| `DabblerQueryPage<T>` | Manual search flow, filter handoff, and advanced-action opt-ins are covered. | Add direct proof for richer query-page state handoff, toolbar affordances, and stub-backed role-aware or read-only behavior where the retained surface exposes it. | Session 3 freezes stubbed data and security context seams; Session 4 expands bUnit coverage. |
| `DabblerDataGrid<T>` | Wrapper-level `role="grid"` plus row and column count semantics are covered. | Add direct proof for paging, sorting, row selection, CRUD-affordance state, keyboard navigation, focus movement, and live announcements. | Session 3 must freeze reusable grid-data stubs; Session 4 adds interaction and accessibility proof. |
| `DabblerFilterGrid<T>` | Summary generation across AND/OR groups is covered. | Add direct proof for row composition, operator changes, keyboard flow, focus order, and readable status output beyond the current summary test. | Session 4 expands the existing test home rather than recreating it. |
| `DabblerComboBox<T>` and `DabblerMultiComboBox<T>` | Public API surface and retained multiselect chip rendering are covered. | Add direct proof for search, value binding, cascading behavior, multi-column rendering, and combobox/listbox accessibility semantics during interaction. | Session 3 must freeze lookup-provider stubs; Session 4 adds behavior and accessibility assertions. |
| `DabblerDetailForm<T>` | Dirty-state tracking, modified fields, and save-button enablement are covered. | Add direct proof for validation messaging, child-region behavior, toolbar semantics, tab order, and read-only or role-aware affordances. | Session 3 must define metadata and security-context seams; Session 4 closes the proof gap. |
| `DabblerRecordPopup<T>` | Close callback wiring is covered. | Add direct proof for dialog rendering, modal semantics, focus entry and return, validation affordances, and save/cancel command behavior. | Session 4 expands the popup contract suite after shared stubs are frozen. |
| `DabblerShell` | Main workspace labeling and optional top-menu composition are covered. | Add direct proof for skip-to-content behavior, navigation-driven workspace composition, and provider-backed tab or workspace state changes. | Session 3 must freeze navigation and metadata stubs; Session 4 consumes them. |
| `DabblerDashboardPage` | Focused navigation-card and quick-link rendering plus compatibility-widget placeholders are covered. | Add direct proof for keyboard traversal, visible focus, accessible naming, and stub-backed widget interactions where retained widgets are interactive. | Session 4 adds dashboard accessibility evidence and direct widget proof. |

### Reports

| Surface | Current direct proof | Remaining gap to close | Expected session handoff |
| --- | --- | --- | --- |
| `DabblerReportPage` | Auto-run loads RDF, executes one query, composes pages, and renders the title. | Add direct proof for prompt-to-viewer handoff, loading and status state, collapsed parameter summary, and toolbar enablement for PDF export, print, and zoom. | Session 4 expands the existing test file once report-provider stubs are frozen. |
| `ParameterPromptForm` | No dedicated direct-proof test file exists today. | Add direct proof for parameter rendering by control type, required and pattern validation, lookup loading, cascading dependency reloads, keyboard flow, and `aria-required` or label semantics. | Session 3 must freeze report-data lookup stubs; Session 4 adds `ParameterPromptFormTests.cs`. |
| `ReportViewer` | No dedicated direct-proof test file exists today. | Add direct proof for document and page semantics, element rendering, grouped or banded layout seams where applicable, style application, and scale-sensitive output. | Session 4 adds `ReportViewerTests.cs` using pre-composed page layouts. |

### Security-Adjacent UI

| Surface | Current direct proof | Remaining gap to close | Expected session handoff |
| --- | --- | --- | --- |
| `DabblerSecured` | Hidden, disabled, enabled, and unauthenticated rendering states are covered in isolation. | Preserve this baseline, then add proof that retained reusable components consume the same state correctly for save/new/delete or comparable affordances. | Session 3 must freeze security-context seams that reusable components can consume in tests. |
| `DabblerRoleSwitcher` | Allowed-role visibility and activator semantics are covered. | Add direct proof for keyboard operation and status behavior only where later retained surfaces depend on it. | Session 4 can extend the existing suite if the reusable shell proof needs it. |
| Reusable component role-aware state | No direct reusable-surface proof exists yet. | Add proof that retained components and pages expose disabled, hidden, or read-only affordances correctly when security or organization context requires it. | Session 3 defines the seam; Session 4 proves the consumption path. |

## Accessibility Baseline

All retained reusable surfaces in this set must satisfy WCAG 2.1 Level AA with
evidence that is durable enough to survive future refactors.

The baseline expectations are:

- all interactive elements must be keyboard reachable and keyboard operable
- visible focus must remain obvious across component, page, and modal flows
- labels, instructions, and icon-only commands must have accessible names
- validation and status changes must be announced with reliable semantics
- reused shell or page structure must expose sensible landmarks and skip paths
- report prompting and report viewing surfaces must be readable and navigable
  without relying on pointer interaction

This set does not need to solve every browser-level or product-level
accessibility question. It must, however, freeze the reusable proof standard
that later app, UAT, and documentation work can rely on.

## WCAG Evidence Plan

### Automated Evidence

Automated evidence for the retained reusable surface should combine:

- durable bUnit assertions for ARIA roles, labels, required-state semantics,
  disabled-state semantics, and live-region or status attributes
- targeted browser-based `axe-core` scans on stub-backed pages that isolate the
  retained reusable surfaces without requiring full application wiring

Automated evidence should be captured for:

- `DabblerShell`, `DabblerDashboardPage`, `DabblerDataGrid<T>`,
  `DabblerFilterGrid<T>`, `DabblerComboBox<T>`, `DabblerMultiComboBox<T>`,
  `DabblerDetailForm<T>`, and `DabblerRecordPopup<T>`
- `DabblerReportPage`, `ParameterPromptForm`, and `ReportViewer`
- security-adjacent activators or conditional-rendering surfaces when they are
  part of a retained reusable page or component flow

### Focused Manual Validation

Manual validation should remain narrow and repeatable.

Each retained surface should have a focused checklist for:

- `Tab` and `Shift+Tab` order through the interactive sequence
- arrow-key or component-specific keyboard behavior where the component exposes
  it as part of the contract
- visible focus movement and focus persistence during state changes
- no keyboard traps in popups, menus, prompts, or viewer-related controls
- validation or status announcements that a screen-reader user can understand
- focus return after closing a popup or finishing a temporary interaction

## Coverage Matrix

### Components

| Surface | Automated evidence | Manual validation focus |
| --- | --- | --- |
| `DabblerShell` | Assert the skip path, main landmark, workspace labeling, and any retained nav semantics. Add stub-backed `axe-core` coverage for a minimal shell page. | Confirm skip-to-content behavior, nav-to-workspace focus flow, and visible focus through the shell frame. |
| `DabblerDashboardPage` | Add semantic and labeling assertions plus a stub-backed `axe-core` scan for focused dashboard widgets. | Confirm keyboard traversal and visible focus for cards, quick links, and focused-search affordances. |
| `DabblerDataGrid<T>` | Keep the existing accessible-shell assertions, then add row or cell active-state, live-region, and command-state assertions. | Confirm keyboard navigation, edit-entry or exit behavior where retained, and visible focus movement. |
| `DabblerFilterGrid<T>` | Add label, operator, and readable-status assertions plus a stub-backed `axe-core` scan. | Confirm row-to-row keyboard flow, operator selection, and filter-summary readability. |
| `DabblerComboBox<T>` and `DabblerMultiComboBox<T>` | Add combobox, listbox, selected-item-group, and multicolumn-header semantics assertions. | Confirm keyboard search, selection, deselection, and visible focus inside expanded lists. |
| `DabblerDetailForm<T>` | Add label, toolbar-button, validation-message, and read-only-state assertions. | Confirm tab order, validation announcement, child-region focus flow, and read-only affordances. |
| `DabblerRecordPopup<T>` | Add dialog and modal semantics assertions plus focusable-command assertions. | Confirm focus entry, no keyboard trap beyond the modal contract, `Esc` handling, and focus return. |

### Reports

| Surface | Automated evidence | Manual validation focus |
| --- | --- | --- |
| `DabblerReportPage` | Add region, loading, status, and toolbar-state assertions plus a stub-backed `axe-core` scan for a rendered report path. | Confirm keyboard access to parameter changes, toolbar controls, and status changes after a run. |
| `ParameterPromptForm` | Add label, required-state, validation, and dependency-driven rerender assertions plus a stub-backed `axe-core` scan. | Confirm tab order across parameter controls, keyboard use of date or selection controls, and understandable validation feedback. |
| `ReportViewer` | Add document, page-region, and rendered-element assertions plus a stub-backed `axe-core` scan of the composed output. | Confirm the composed report can be navigated and understood as structured output, not just styled boxes. |

### Security-Adjacent UI

| Surface | Automated evidence | Manual validation focus |
| --- | --- | --- |
| `DabblerSecured` consumption paths | Keep the isolated hidden/disabled proof, then add component-level assertions where retained components consume those states. | Confirm removed controls disappear from the tab order and disabled controls remain understandable. |
| `DabblerRoleSwitcher` | Keep the activator semantics assertions and add further proof only when reusable shell flows depend on it. | Confirm keyboard operation and visible focus if later sessions rely on it in the retained shell baseline. |
| Reusable role-aware affordances | Add button, menu, and page-state assertions in the retained component and page suites. | Confirm restricted actions remain discoverable or understandable without exposing unintended interaction paths. |

## Session 3 And Session 4 Handoff

Session 3 must freeze the reusable seams that Session 4 depends on:

- grid and query data-provider stubs for paging, sorting, and stateful query
  interactions
- report-data and lookup stubs for parameter prompts, report execution, and
  viewer composition
- metadata, navigation, and workspace stubs for shell and detail-form flows
- security or organization-context seams that retained reusable components can
  consume without app-specific coupling

Session 4 should plan around these direct-proof deliverables:

- add `tests/Dabbler.Reports.Blazor.Tests/ParameterPromptFormTests.cs`
- add `tests/Dabbler.Reports.Blazor.Tests/ReportViewerTests.cs`
- expand the retained component test homes rather than adding duplicate homes
- capture reusable-surface accessibility evidence with both bUnit assertions
  and targeted stub-backed scans
- keep the proof centered on retained surfaces only, without reopening
  compatibility-only catalog or query-report defaults
