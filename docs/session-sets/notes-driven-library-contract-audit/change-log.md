# Change Log: Notes-Driven Library Contract Audit

> **Status:** Complete
> **Completed:** 2026-04-14
> **Session Set:** `docs/session-sets/notes-driven-library-contract-audit/`
> **Source Contract:** `docs/planning/user-functionality-notes.md`

---

## Summary

This session set audited the reusable library surface against the notes-driven
contract, mapped real app/template consumption, classified retained versus
speculative functionality, recorded a human decision queue, and closed with a
coverage gate for retained non-security reusable surfaces.

The primary handoff to later work is now explicit:

- the retained core reusable surface is documented
- retirement and isolation candidates are documented
- simplification work starts with a reusable-surface coverage gate instead of
  relying only on app smoke coverage

---

## Session Outcomes

### Session 1

- inventoried the public-facing reusable library surfaces under `src/Libs/`
- mapped those surfaces to `docs/planning/user-functionality-notes.md`
- recorded the initial keep/simplify/isolate/remove baseline in
  `docs/planning/notes-driven-library-contract-audit.md`

### Session 2

- mapped current `src/Apps/` consumption of the major reusable surfaces
- confirmed `template-seed/` currently proves workflow assets, not Dabbler UI
  or report surfaces
- separated directly proven surfaces from speculative or weakly supported ones

### Session 3

- created the feature-level decision ledger for retained, isolated, and
  retirement-target surfaces
- recorded the remaining human decision queue for popup versus inline editing,
  dashboard defaults, and external-auth breadth
- rebaselined downstream specs for:
  `docs/session-sets/notes-driven-core-surface-simplification/spec.md`,
  `docs/session-sets/notes-driven-app-alignment/spec.md`, and
  `docs/session-sets/notes-driven-template-encoding/spec.md`

### Session 4

- mapped current automated coverage to the retained core reusable surfaces
- documented the highest-value test gaps for retained non-security reusable
  components and reports
- appended Session 5 after confirming the audit still lacked an explicit
  retained-surface coverage gate

### Session 5

- converted the Session 4 gap map into a durable coverage gate for retained
  non-security reusable surfaces
- documented where dedicated `Dabbler.Components` and `Dabbler.Reports`
  contract-test homes must exist before adjacent functionality is retired
- rebaselined downstream simplification, app-alignment, and template specs so
  reusable-surface contract tests are treated as required input instead of an
  optional follow-up

---

## Retained Core Surface

- `DabblerQueryPage<T>`, `DabblerDataGrid<T>` core behavior, and
  `DabblerFilterGrid<T>` core behavior
- `DabblerComboBox<T>` with retained multiselect capability
- `DabblerDetailForm<T>` and `DabblerRecordPopup<T>`
- `DabblerReportPage`, `ParameterPromptForm`, and `ReportViewer`
- `DabblerDashboardPage` core behavior
- `DabblerShell` plus navigation/runtime provider contracts
- security UI/runtime authorization surfaces

## Retirement Or Isolation Pressure

- grid extras such as grouping, aggregates, clipboard, CSV export, pinning,
  and detail expansion
- filter extras such as quick filter, presets, expression line, and column
  toggles
- combo add-new and grouping behavior
- `DabblerQueryReportPage<T>`
- `DabblerCatalogPage`
- dashboard favorites and recent-items as default platform behavior

---

## Downstream Handoff

- `docs/session-sets/notes-driven-core-surface-simplification/spec.md` now
  treats the Session 5 coverage gate as binding input and expects dedicated
  reusable-surface contract-test homes before adjacent non-security
  functionality is retired
- `docs/session-sets/notes-driven-app-alignment/spec.md` now assumes the
  simplification set establishes reusable-surface contract tests first, with
  app integration and Playwright coverage complementing that baseline
- `docs/session-sets/notes-driven-template-encoding/spec.md` now assumes
  templates should inherit only surfaces that survived the audit and the
  simplification coverage gate

## Open Human Decisions

- inline editing versus popup-only editing for retained CRUD workflows
- default dashboard experience after favorites and recent-items are treated as
  non-core by default
- external user authentication breadth beyond the core runtime-authorization
  story
