# Platform UI Foundation Review Ledger

## Session 1 Scope

Assess the current state of the core `Dabbler` UI wrapper surfaces against
MudBlazor v8.15.0. This session inventories the primary reusable surfaces,
captures the strongest framework-alignment and UAT friction signals, and
records a keep/simplify/rethink ledger that later sessions can use without
reopening the same baseline questions.

## Surface Inventory

No standalone reusable spacing primitives, layout helpers, or design-token
candidates were identified as first-class surfaces in the current codebase
review. Session 2 should confirm whether any such helpers exist implicitly and
deserve separate treatment.

| Surface | Current Role | Current Friction |
| --- | --- | --- |
| `DabblerQueryPage<T>` | Core composition primitive for query/list pages built from filter + toolbar + result grid. | Lowest current friction. It still reads as a valid reusable page-level composition surface. |
| `DabblerDataGrid<T>` | Wrapper around `MudDataGrid<T>` that now also owns keyboard navigation, edit lifecycle hooks, grouped rendering, detail rows, popup editing, clipboard helpers, CSV export, pinning, and live-region behavior. | Heavy overlap with MudBlazor, large custom state machine, compatibility-shim evidence of framework coupling, and thin direct interaction coverage. |
| `DabblerFilterGrid<T>` | Reusable QBE filter surface with row-based AND/OR logic, date presets, status chips, and optional quick-filter or column-toggle paths. | Valuable filtering model, but too much UI wrapper behavior sits on top of MudBlazor controls and current operability proof is very light. |
| `DabblerShell` | Top-level reusable shell that combines the left navigation panel with a tabbed workspace. | Works against route-based navigation, contributes to mixed navigation semantics, and sits awkwardly beside the app's separate top app bar. |
| `DabblerNavigationPanel` | Primary left-side entity/report navigation plus contextual smart-filter pills. | Entity selection opens content through events instead of URL changes, active-state styling is fragile, and the panel is tied to the workspace model. |
| `DabblerTabbedWorkspace` | In-shell tab manager for search, detail, report, and dashboard content. | Core of the non-route workspace model, allows duplicate search tabs, and increases state-management complexity without matching browser expectations. |

## MudBlazor Alignment Notes

- The current MudBlazor target is `8.15.0`.
- `DabblerQueryPage<T>` still looks like a durable platform primitive because
  it standardizes a high-value query/list page composition pattern rather than
  merely wrapping one MudBlazor control.
- `DabblerDataGrid<T>` is the clearest overlap hotspot. MudBlazor already
  covers grid editing modes, dialog editing, column pinning, and much of the
  interaction model that the wrapper now re-owns.
- `src/Libs/Dabbler.Components/DataGrid/MudDataGridExtensions.cs` is a direct
  maintenance warning sign: the wrapper already had to compensate for
  differences between a local MudBlazor fork and the published package.
- `DabblerFilterGrid<T>` still appears to earn its existence where it preserves
  domain-specific QBE behavior, readable filter summaries, and reusable preset
  logic. The broader UI shell around those behaviors should still be challenged.
- `DabblerShell`, `DabblerNavigationPanel`, and `DabblerTabbedWorkspace`
  currently fight the Blazor and browser routing model by loading content
  through events and in-memory tabs instead of clear route transitions.

## Friction Scan

- Likely direct bugs from the current evidence include: the `Switch org`
  interaction appearing dead, unreadable active-nav styling, the cut-off home
  title, non-working filter pill deletion, non-working `Clear All`, dead
  calendar and toggle interactions in the filter grid, mutation-refresh gaps
  after add/delete flows, duplicate search tabs, and filter-grid spacing and
  alignment problems.
- The strongest UX-model problem is mixed navigation. The user can see shell
  content change without the browser URL changing, which makes the system feel
  inconsistent and weakens bookmarking, browser history, and orientation.
- `MainLayout` likely amplifies this friction because it currently combines a
  custom `MudAppBar`, routed page content, and a separate `DabblerShell`
  beneath that routed content.
- Existing reusable-surface tests are still minimal. They confirm that the
  retained surfaces render, but they do not yet prove most real interactions:
  filter chips, filter controls, grid mutations, workspace behavior, active
  styling, or route-vs-tab expectations.
- No separate reusable layout-helper or token surface emerged from Session 1.
  The current evidence points more toward layout conventions and guardrails than
  toward an already-stable layout abstraction to keep or retire.
- The human-UAT stop signal was appropriate: too many basic interactive defects
  remained discoverable only by manual review.

## Keep Simplify Or Rethink

| Surface | Recommendation | Why |
| --- | --- | --- |
| `DabblerQueryPage<T>` | **Keep** | It still provides durable value as the standard query/list composition surface and does not depend on broad custom behavior to justify itself. |
| `DabblerFilterGrid<T>` | **Simplify** | The QBE/filter-summary/preset model still looks valuable, but the UI wrapper should be narrowed and aligned more directly with MudBlazor composition and stronger interaction proof. |
| `DabblerNavigationPanel` | **Simplify** | The navigation surface still has value, but it should stop depending on event-driven workspace semantics and move toward clearer route-aware navigation and safer active-state styling. |
| `DabblerDataGrid<T>` | **Rethink** | It carries the highest maintenance burden, the clearest overlap with MudBlazor, and the weakest confidence relative to the amount of behavior it owns. |
| `DabblerShell` | **Rethink** | The current shell reinforces a non-standard navigation model and clashes with the app's separate top-level layout composition. |
| `DabblerTabbedWorkspace` | **Rethink** | The tabbed workspace is the main carrier of duplicate-search-tab friction and non-URL navigation behavior, so it should not remain the unquestioned default. |

## Session 2 Handoff

- Define the preferred testing architecture for reusable UI components before
  more human UAT: direct interaction coverage for filter controls, mutation and
  refresh coverage for grid flows, route-aware shell or nav coverage, focused
  accessibility assertions, and browser-level proof for the real user path.
- Propose styling and accessibility guardrails for active-nav states, selected
  states, readable status chips, focus visibility, top-level shell/header
  layout safety, and layout-overlap detection.
- Propose a reusable spacing and layout standard for query pages so filter
  grids, result grids, toolbars, and shell content share a consistent rhythm.
- Record the platform rule for when to prefer MudBlazor-native surfaces versus
  custom Dabbler abstractions, using the Session 1 keep/simplify/rethink ledger
  as the starting point rather than reopening the inventory from scratch.
