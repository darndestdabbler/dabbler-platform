# Platform UI Foundation Review Spec

> **Purpose:** Reassess the reusable UI-component direction, testing strategy,
> and styling guardrails so the platform libraries become easier to use,
> easier to validate, and safer to carry into future applications.
> **Created:** 2026-04-16
> **Session Set:** `docs/session-sets/platform-ui-foundation-review/`
> **Prerequisite:** `docs/planning/guiding-principles.md` and
> `docs/planning/lessons-learned.md` have been reviewed, and current UAT
> findings from `full-app-uat` are available as evidence of where the present
> approach is not meeting the intended platform bar.
> **Workflow:** Orchestrator -> AI Router -> Cross-provider verification

---

## Project Overview

This set is a deliberate design review of the reusable UI layer rather than a
simple bug-fix pass.

The goal is to decide whether the current custom component direction is helping
or hindering the long-term platform mission:

- reusable components and libraries that support many future apps
- a developer experience that is simple and predictable
- an automated testing approach that catches operability and styling defects
  before human UAT

This set should explicitly evaluate whether some current custom surfaces would
be better replaced, simplified, or reoriented around built-in MudBlazor
capabilities.

It should also define durable guardrails for styling, accessibility, spacing,
and automated UI verification so future app work inherits a stronger base.

This set is not meant to quietly rewrite the whole UI stack in one pass.

It is meant to produce a clear architecture direction, adoption plan, and
implementation backlog for follow-on execution.

## Core Questions

This set should explicitly investigate at least these questions:

1. Are we reimplementing behaviors that MudBlazor already provides well enough?
2. Which custom Dabbler UI abstractions genuinely improve developer experience,
   consistency, or supported capability?
3. What testing architecture would make reusable UI components effectively
   bullet-proof for operability, styling, and accessibility regressions?
4. How should styling safety work across dynamic states such as selected rows,
   active navigation, banners, and theme-driven color changes?
5. What reusable spacing and layout conventions should the platform enforce so
   margin and padding choices do not drift page by page?

## Candidate Focus Areas

Likely high-value review targets include:

- `DabblerFilterGrid`
- `DabblerDataGrid`
- `DabblerQueryPage`
- `DabblerShell` and related navigation or workspace components
- theme, color, and selected-state styling conventions
- spacing primitives, layout helpers, or design-token candidates
- automated accessibility and visual-regression guardrails

## Parallelization Notes

No default parallelization assumption is required.

Inside the set, component-surface inventory, testing-gap analysis, and
styling-system research may proceed in parallel once the core review scope is
frozen.

## Session Plan

### Session 1 of 3: Inventory, Friction Scan, And Framework-Alignment Review

**Steps:**
1. Inventory the current reusable UI surfaces, with emphasis on custom grid,
   filter, shell, navigation, and layout components.
2. Compare those surfaces against the relevant built-in MudBlazor capabilities
   and identify overlap, divergence, and maintenance burden.
3. Review UAT findings, current automated tests, and component responsibilities
   to identify where the current abstraction strategy is creating friction for
   operability, styling, accessibility, or developer experience.
4. Produce a focused ledger of candidate components or patterns that should be
   kept, simplified, wrapped differently, or reconsidered entirely.

**Creates:** Component inventory, friction scan, and MudBlazor-alignment notes
**Touches:** This spec and planning notes as needed
**Ends with:** The current UI-platform surface is understood well enough to
evaluate direction instead of making isolated guesses
**Progress keys:** `session-1/component-inventory`,
`session-1/framework-alignment`, `session-1/friction-scan`,
`session-1/keep-vs-rethink-ledger`

---

### Session 2 of 3: Testing, Styling, And Spacing Architecture Proposal

**Steps:**
1. Define the preferred testing architecture for reusable UI components:
   unit-test seams, bUnit interaction coverage, integration coverage,
   browser-based operability checks, accessibility checks, and visual-regression
   evidence.
2. Propose a proactive styling and accessibility strategy for contrast safety,
   selected states, focus visibility, layout overlap detection, and dynamic
   theme changes.
3. Propose a reusable spacing and layout standard for margin, padding, and
   sibling or container rhythm so future pages inherit consistent structure.
4. Record the proposed platform rule for when to prefer MudBlazor-native
   surfaces versus custom Dabbler abstractions.

**Creates:** Proposed testing architecture, styling-guardrail direction, and
spacing-system direction
**Touches:** Planning docs and this spec
**Ends with:** The future reusable UI direction is explicit enough to guide
implementation sessions
**Progress keys:** `session-2/testing-architecture`,
`session-2/styling-guardrails`, `session-2/spacing-standard`,
`session-2/component-boundary-rule`

---

### Session 3 of 3: Adoption Plan, Pilot Targets, And Closeout

**Steps:**
1. Convert the proposed direction into a practical adoption plan with pilot
   targets, likely starting points, and risk-managed sequencing.
2. Identify which current components or workflows are the best pilot candidates
   for proving the new direction, such as search/filter pages, shell
   navigation, or a representative grid workflow.
3. Produce a follow-on implementation backlog that separates research,
   refactor, testing, and migration work.
4. Generate `change-log.md` for this session set.

**Creates:** Adoption plan, pilot backlog, and `change-log.md`
**Touches:** Planning docs and session-set closeout artifacts
**Ends with:** Future implementation work can proceed from an intentional,
reviewed platform direction instead of incremental drift
**Progress keys:** `session-3/adoption-plan`, `session-3/pilot-targets`,
`session-3/implementation-backlog`, `session-3/change-log`
