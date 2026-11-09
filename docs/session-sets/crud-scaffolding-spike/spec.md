# CRUD Scaffolding Spike Spec

> **Purpose:** Plan command-line scaffolding for Dabbler CRUD pages, service
> endpoints, and end-to-end feature slices. The spike's single deliverable is a
> spec for a follow-on implementation set (`crud-scaffolding`). It does not
> ship scaffolding tooling itself.
> **Created:** 2026-05-16
> **Session Set:** `docs/session-sets/crud-scaffolding-spike/`
> **Prerequisite:** `packaging-and-template-readiness` (Sessions 1-11)
> closed with `packaging-and-template-readiness-uat-remediation` ideally also
> closed so the scaffolding work targets a known-good template baseline. This
> spike CAN run in parallel with the remediation set since it is planning-only
> and does not touch the same files.
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Session Set Configuration

```yaml
requiresUAT: false
requiresE2E: false
uatScope: none
```

> **Rationale:** This is a planning spike. The output is a written spec, not
> user-visible behavior. UAT and E2E coverage apply to the follow-on
> implementation set, not the spike.

---

## Project Overview

The composable-crud-helpers set produced a set of explicit-composition primitives
(`EditSession<T>`, `<DirtyAwareActionRow>`, `<DeclarativeFilterPanel>`,
`IGridDataProvider<T>`) that pages wire à la carte. That set explicitly rejected
a runtime configuration composite (`MasterDetailsConfig<TEntity>`) after two
independent cross-provider reviews converged on "the composite picked the wrong
axis of variation."

Code generation is a different shape of the same goal: instead of *generating
behavior at runtime* from a config object, the scaffolder *generates source
code* the developer customizes and owns. This is consistent with the explicit-
composition principle — the generator's output is the same explicit code a
developer would write by hand, just typed faster. This is also the right shape
for AI-led development, where reducing boilerplate per new entity has high
leverage.

**Open design questions this spike must resolve before implementation:**

1. **Scaffolding granularities.** What can the generator produce?
   - A single Razor CRUD page using the helpers?
   - A service endpoint trio (controller + service + EF query) for one entity?
   - A full feature slice (page + service + tests + nav entry + security
     restrictions)?
   - The implementation set will likely produce more than one; the spike picks
     which to ship first.
2. **CLI shape.** Where does the scaffolding live?
   - As additional `dotnet new` templates in `Dabbler.Templates` (e.g.,
     `dotnet new dabbler-crud-page --entity Customer`)?
   - As a separate `dotnet tool` (e.g., `dabbler scaffold crud-page --entity Customer`)?
   - As a Roslyn source generator that emits code at build time given a
     marker attribute (rejected by default — source generators are runtime-
     adjacent and harder to customize; the spike should reconfirm this).
3. **Filter representation in scaffolded endpoints.** The spike's leading
   recommendation is the platform's existing QBE filter model (already in
   `Dabbler.Components`), NOT OData. OData would create a second filter
   representation competing with the QBE one. The spike must validate this
   recommendation against tri-provider review and document the reasoning so
   the implementation set does not relitigate.
4. **Idempotency and regeneration story.** When the developer re-runs the
   generator on an existing entity, does it overwrite, refuse, or merge?
   What guarantees the generated code does not silently undo developer
   customizations?
5. **Entity discovery.** Where does the generator learn an entity's shape
   from? Options:
   - A C# class the developer already wrote (`--entity Customer` resolves to
     `App.Data.Customer` via reflection over the loaded assembly).
   - A schema file (e.g., `--schema customers.dabbler-entity.json`).
   - An EF Core `DbContext` introspection.
   - The spike must pick one and explain why.
6. **What the generator does NOT do.** Explicit non-goals to avoid feature
   creep:
   - No runtime configuration composite.
   - No reflection-driven UI at runtime.
   - No replacement for explicit `MudDataGrid` column definitions in
     generated pages (columns generated once at scaffold time, not at
     render time).

## Parallelization Notes

The spike is sequential — each session's output feeds the next. No internal
parallelization is justified. Externally, the spike can run in parallel with
`packaging-and-template-readiness-uat-remediation` (disjoint files; no
shared session-state).

## Session Plan

### Session 1 of 3: Helper And Pattern Inventory

**Steps:**

1. Read `docs/planning/project-guidance.md`,
   `docs/planning/lessons-learned.md`, and the just-promoted
   *Composition over configuration for CRUD page composites* principle.
2. Inventory the existing CRUD helpers in `src/Libs/Dabbler.Components/`:
   - `EditSession<T>` — dirty-state tracking. Capture: required entity
     shape, supported field types, snapshot semantics.
   - `<DirtyAwareActionRow>` — Save/Cancel/Delete primary actions. Capture:
     slot model, CustomActions extensibility, navigation handling.
   - `<DeclarativeFilterPanel>` — QBE filter UI. Capture: the filter-model
     DTO shape, the expression engine that translates it to a predicate,
     how it serializes for transport.
   - `IGridDataProvider<T>` — server-side grid contract. Capture: the
     request shape (sort, page, filter), the response shape (rows, total
     count).
   - Any other helpers used by the demo app's existing CRUD pages
     (Products, Orders, Customers).
3. Inventory the demo app's service endpoint patterns in
   `src/Apps/AcmeCoffeeOnline.Api/`:
   - How are CRUD controllers shaped? (Are there base classes, attribute
     conventions, route conventions?)
   - How are grid-query endpoints shaped today? (What does the request
     DTO look like? Is it the QBE filter directly, or a wrapper?)
   - How are mutations (POST/PUT/DELETE) shaped? Are there shared
     validation or auth conventions?
   - How does each endpoint apply EF Core filtering — does the QBE
     expression engine translate to LINQ?
4. Inventory the demo app's existing CRUD pages in
   `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/`:
   - Pick 2-3 representative pages (e.g., a list+detail, a master+detail,
     a paged grid with filters). Capture the explicit composition shape:
     namespaces, using directives, page directive, layout, helper wiring,
     navigation, validation.
5. Route a structural analysis via
   `route(task_type="analysis", complexity_hint=50)` to summarize: "Given
   the inventory above, what does a generator need to emit at each
   granularity (page, service endpoint, feature slice) to produce
   code that's indistinguishable from the existing reference pages?"
6. Save the routed analysis as
   `docs/session-sets/crud-scaffolding-spike/helper-and-pattern-inventory.md`
   alongside an orchestrator-authored summary section that captures the
   inventory itself.

**Creates:** `helper-and-pattern-inventory.md` (inventory + routed analysis).
**Touches:** This session-set folder only.
**Ends with:** A complete picture of what reference code the generator must
match. No design decisions yet — pure inventory.
**Progress keys:** `session-1/inventory-helpers`,
`session-1/inventory-api-patterns`, `session-1/inventory-pages`,
`session-1/route-analysis`, `session-1/save-inventory`

---

### Session 2 of 3: Scaffolding Approach Proposal

**Steps:**

1. Read the Session 1 inventory and the open design questions in this spec's
   *Project Overview* section.
2. The orchestrator (Opus expected per the next-orchestrator analysis pattern)
   drafts `scaffolding-approach-proposal.md` inside this session-set folder
   directly — no `route()` call to the orchestrator's own model. The proposal
   must cover, with explicit reasoning for each choice:
   - **Resolution of every open design question** in the *Project Overview*
     (granularities, CLI shape, filter representation, idempotency, entity
     discovery, non-goals).
   - **The MVP shipping shape** — which granularities and which CLI shape
     land in the first implementation set, and which are deferred.
   - **The generated-code samples** — for each granularity that ships in the
     MVP, include a hand-drafted example of what the generator would emit
     given a specific input (e.g., for `dotnet new dabbler-crud-page
     --entity Customer`, show the full Customer.razor source the generator
     would produce).
   - **The non-MVP roadmap** — what later sets would add (e.g., source-
     generator backend, multi-entity slices, OData query mode if ever
     reconsidered), with the rationale for the deferral.
   - **OData re-litigation guard.** Document the QBE-vs-OData decision and
     the reasoning so the implementation set does not reopen it.
3. The proposal draft doubles as Opus's contribution to the Session 3
   tri-provider review per
   `feedback_orchestrator_self_contributes_to_review_panel`. The synthesis
   in Session 3 must disclose this context asymmetry explicitly per the
   just-promoted lesson *Tri-Provider Review — Orchestrator Contribution
   Uses Full Context, Not An Isolated Prompt*.
4. Build and run any markdown checks if configured.

**Creates:** `scaffolding-approach-proposal.md` (Opus-authored, doubles as the
Opus review input for Session 3).
**Touches:** This session-set folder only.
**Ends with:** A complete, opinionated proposal ready for tri-provider review.
**Progress keys:** `session-2/draft-proposal`,
`session-2/draft-generated-code-samples`, `session-2/document-non-mvp-roadmap`,
`session-2/odata-guard`

---

### Session 3 of 3: Tri-Provider Review And Synthesize Implementation Spec

**Steps:**

1. Read `docs/planning/project-guidance.md`,
   `docs/planning/lessons-learned.md`, and Session 2's
   `scaffolding-approach-proposal.md`.
2. Route the proposal for review to two non-Anthropic providers, one call
   each. Each gets the SAME self-contained prompt (the proposal text plus the
   explicit question: *"Does this scaffolding approach align with the
   composition-over-configuration principle, deliver value for AI-led
   development, and avoid reintroducing the variation-axis trap that
   abandoned the runtime composite? What would you change?"*).
   - `route(task_type="architecture", force_model="gpt-5-4")` (or via the
     underlying `call_model` primitive if `force_model` is not honored —
     the same pattern Sessions 4 and 5 of `packaging-and-template-readiness`
     used).
   - `route(task_type="architecture", force_model="gemini-pro")`.
   - Save outputs as `scaffolding-review-gpt-5-4.md` and
     `scaffolding-review-gemini-pro.md`.
3. The orchestrator (Opus) synthesizes its own proposal-author perspective
   plus the two routed reviews into `scaffolding-approach-review.md`.
   Required shape (same as `packaging-and-template-readiness` Sessions 4-5):
   verdicts, areas of agreement, areas of disagreement, an adjustment list
   with dispositions, and an explicit disclosure that the orchestrator's
   contribution used full proposal-author context.
4. Apply small-scope adjustments to the proposal inline. For larger
   adjustments, record them as inputs to the implementation-set spec
   rather than expanding this session.
5. Author the implementation-set spec at
   `docs/session-sets/crud-scaffolding/spec.md` (do NOT create the folder
   structure beyond `spec.md` — the implementation set's other artifacts
   land when it runs). The spec must follow
   `docs/planning/session-set-authoring-guide.md`: Session Set Configuration
   block, project overview, parallelization notes, sized session plan with
   Steps/Creates/Touches/Ends with/Progress keys per session.
6. Cross-provider verify this session's outputs via standard Step 7.

**Creates:** `scaffolding-review-gpt-5-4.md`, `scaffolding-review-gemini-pro.md`,
`scaffolding-approach-review.md` (synthesis),
`docs/session-sets/crud-scaffolding/spec.md` (the implementation set's spec).
**Touches:** This session-set folder + the new `crud-scaffolding/` folder for
the spec only.
**Ends with:** A tri-provider-validated scaffolding approach plus an executable
spec for the implementation set.
**Progress keys:** `session-3/route-gpt-5-4`, `session-3/route-gemini-pro`,
`session-3/synthesize`, `session-3/author-implementation-spec`,
`session-3/cross-provider-verify`
