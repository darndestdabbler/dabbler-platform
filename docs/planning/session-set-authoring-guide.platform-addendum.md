# Session-Set Authoring Guide — Platform Addendum (dabbler-platform)

> **Purpose:** Platform-specific extensions to the portable
> `session-set-authoring-guide.md`. The portable guide owns the
> universal authoring rules; this file owns the dabbler-platform
> conventions (Blazor Server, MudBlazor, Playwright, AcmeCoffeeOnline
> directory layout) that don't apply to non-UI sibling repos.
>
> **Audience:** Spec authors working in dabbler-platform.
>
> **Lifecycle:** This file evolves with the platform repo; the
> portable guide evolves on its own cadence and can be lifted into
> other repos without bringing this addendum along.

---

## Where artifacts live in this repo

- Session sets: `docs/session-sets/<slug>/`
- Archived sets: `docs/session-sets/_archived/`
- Spec template seeds: see `template-seed/` for the project-bootstrap
  copies of CLAUDE.md / AGENTS.md / GEMINI.md and the workflow doc.
- E2E tests: `tests/AcmeCoffeeOnline.Playwright/` — Playwright-driven
  through Docker compose stacks (`docker/docker-compose.infra.yml` for
  persistent dev infra, `docker/docker-compose.app.yml` for the app
  services). For new `requiresE2E: true` sets, the Playwright `.cs`
  test is the W0-compiled output of the DSL checklist (one
  `*UatTests.cs` per session set's checklist), not a hand-authored
  pairing file. Closed sets retain their existing
  `*UatCoverageTests.cs` files unchanged.
- W0 toolchain (DSL syntax, CLI flags, data contracts): the sibling
  [`dabbler-uat-dsl`](https://github.com/darndestdabbler/dabbler-uat-dsl)
  repo's `docs/` folder is the canonical reference. The supported
  W0 / catalog-schema / runner combination is pinned in
  `docs/COMPATIBILITY.md` at the root of this repo.
- UAT checklist editor schema:
  https://github.com/darndestdabbler/uat-checklist-editor

---

## When to declare `requiresUAT: true` in this repo

Most behavior-changing sets in dabbler-platform touch a Blazor Server
UI surface and therefore declare `requiresUAT: true`. Notable
exceptions where `requiresUAT: false` is the right call even though
the set is non-trivial:

- Pure ai-router or workflow changes (no UI consumed).
- Pure docs / planning updates.
- Test-only changes (adding coverage without changing behavior).
- Library refactors confined to `src/Libs/` with no consumer changes.
- Infrastructure / Docker compose changes.

---

## When to declare `requiresE2E: true` in this repo

Default to `true` for any set that changes shipping Blazor pages,
components, or API endpoints the UI consumes. The E2E suite in
`tests/AcmeCoffeeOnline.Playwright/` exercises the real UI through the
Docker compose stack — this is the project's lowest-friction
regression net. New `requiresE2E: true` sets author their checklist
in DSL and compile to Playwright via W0 (see
`docs/ai-led-session-workflow.md` → *UAT/E2E pipeline*); they do not
hand-author a pairing `*UatCoverageTests.cs` file or call
`route(task_type="uat-coverage-review")` (retired).

`requiresE2E: false` is appropriate for:

- Doc / planning / workflow sets.
- ai-router and tooling sets (the router has its own unit tests).
- Pure refactors with no behavior change.
- Sets whose entire surface is exercised at the unit/integration
  layer (rare, but legitimate when the change is purely structural
  and has no end-to-end pathway).

---

## Project-specific deliverables and conventions

- See `docs/planning/project-guidance.md` → Conventions → MudBlazor
  Usage, Testing Conventions, and Human UAT And Checklist Editor for
  the project-specific rules that govern UAT-bearing sets.
- See `docs/planning/lessons-learned.md` for the documented failure
  patterns that drove the current discipline (notably the
  "human UAT must not be the first discovery point" lesson, which is
  why E2E-Coverage-Before-UAT is a hard gate when both flags are
  true).
- The structured UAT checklist must follow the schema at
  https://github.com/darndestdabbler/uat-checklist-editor/blob/main/checklist-schema.json
  and is run through https://darndestdabbler.github.io/uat-checklist-editor/
  with results saved inline in the JSON.

---

## Master plan reference

dabbler-platform does not currently maintain a single master plan
(`docs/*-plan.md`) that orders session sets. The Step 8 next-session-set
recommendation in `docs/ai-led-session-workflow.md` therefore falls
through to its "no master plan" branch and recommends from open goals
and backlog. If a plan is introduced later (e.g., a multi-quarter
roadmap), record its filename here and the workflow's plan-aware branch
activates automatically.
