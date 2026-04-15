# Accessibility And Stubbing Evidence

> **Purpose:** Capture the Session 4 direct-proof and WCAG evidence artifacts for
> `docs/session-sets/accessibility-and-stubbing/`.
> **Created:** 2026-04-15
> **Source Inputs:** `docs/planning/accessibility-and-stubbing-gap-ledger.md`,
> the Session 3 stub seams, and the Session 4 automated proof updates

---

## Automated Evidence Captured

The retained reusable surface now has direct automated proof in the contract
test homes and the web-host guardrails.

### Reports

- `tests/Dabbler.Reports.Blazor.Tests/ParameterPromptFormTests.cs` proves
  required-state semantics, stub-backed lookup loading, dependent lookup
  parameter handoff, regex validation feedback, and typed run-value conversion.
- `tests/Dabbler.Reports.Blazor.Tests/ReportViewerTests.cs` proves document and
  page landmark semantics, page labeling, band background rendering, styled
  text output, image rendering, subreport composition, and scale-sensitive page
  sizing.
- `tests/Dabbler.Reports.Blazor.Tests/DabblerReportPageTests.cs` now proves the
  prompt-to-viewer handoff end to end: auto-run composition, collapsed
  parameter summary, accessible zoom status text, and enabled export/print
  affordances after a successful run.

### Components

- `tests/Dabbler.Components.Tests/Shell/DabblerShellTests.cs` now proves the
  shell skip link and workspace landmark labeling, while
  `tests/Dabbler.Components.Tests/Data/HomePageConfigurationTests.cs` proves
  the role-aware home-page override resolution that feeds shell startup state.
- `tests/Dabbler.Components.Tests/Shell/DabblerDetailFormTests.cs` now proves
  required-field alert semantics, invalid-field wiring via `aria-describedby`,
  and read-only toolbar and field states.
- `tests/Dabbler.Components.Tests/FilterGrid/DabblerFilterGridTests.cs` now
  proves the active-filter status region and clear-all affordance alongside the
  existing summary-generation contract.

### Stub-Backed Host Consumption

- `tests/AcmeCoffeeOnline.Web.Tests/WebGuardrailTests.cs` now proves that the
  `/platform/stub` route is reachable in a Development host with stub mode
  enabled and still bootstraps through the real app shell asset pipeline.

## WCAG 2.1 AA Evidence Summary

The automated proof collected in Session 4 directly covers these WCAG-aligned
expectations:

- keyboard-discoverable navigation landmarks and skip paths
- required-state and invalid-state semantics on reusable forms
- live or status text exposure for report toolbar state
- named document and region landmarks for rendered reports
- readable disabled and read-only affordances for reusable edit flows
- stub-backed host rendering that preserves the same semantics outside isolated
  bUnit harnesses

## Focused Manual Checks

Session 4 also freezes the focused manual browser checks that should be rerun
whenever these retained surfaces change.

- `/platform/stub`: tab from the page start to the skip link, activate it, and
  confirm focus lands in the main workspace without trapping inside shell chrome.
- `/platform/stub`: tab through the navigation, workspace tabs, and stub report
  menu affordances to confirm visible focus remains obvious.
- `ParameterPromptForm`: verify tab order through text, date, checkbox, and
  lookup controls; confirm validation feedback is understandable after an
  invalid entry.
- `DabblerReportPage`: verify toolbar controls remain keyboard reachable after a
  run and that the collapsed parameter summary can be reopened without pointer
  use.
- `ReportViewer`: verify rendered pages can be read in sequence and that zoom
  changes do not make the structured content unusable.

These focused manual checks are captured here as the durable checklist for the
retained reusable surface. This terminal session verified the automated
artifacts above; the checklist remains the live-browser follow-up path for
future accessibility regression passes.
