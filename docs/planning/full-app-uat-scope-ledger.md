# Full-App UAT Scope Ledger

> **Purpose:** Freeze the Session 1 workflow subset, human-only checks,
> findings rules, and sign-off gates for
> `docs/session-sets/full-app-uat/`.
> **Created:** 2026-04-16
> **Source Inputs:** `docs/planning/full-app-uat-handoff-notes.md`,
> `docs/planning/e2e-persona-route-matrix.md`,
> `docs/planning/e2e-representative-suite-baseline.md`,
> `docs/planning/distributor-acceptance-baseline.md`,
> `docs/planning/phase-2-platform-roadmap.md`,
> `docs/session-sets/phase-2-docs-readiness/spec.md`, and
> `docs/session-sets/packaging-and-template-readiness/spec.md`

---

## Session 1 Verdict

The current `full-app-uat` spec still matches the accepted Phase 2 baseline.

No rebaseline of this session set's core purpose is needed.

The prerequisite chain is complete and consistent:

- `e2e-workflow-implementation` is closed and handed off the stabilized
  representative suite explicitly to this set
- the frozen representative order still matches
  `E2E-WF-04` -> `E2E-WF-05` -> `E2E-WF-02` -> `E2E-WF-01` -> `E2E-WF-03`
- the downstream documentation and packaging sets still depend on the accepted
  UAT baseline that this set will produce

The durable Session 1 outcomes are:

- keep the later UAT lane deliberately small and tied to the stabilized
  representative Playwright baseline
- retain all five approved representative workflow IDs in their frozen order
- spend human attention on clarity, coherence, trust, and other judgment calls
  that automation cannot score well
- require durable triage and sign-off rules so Session 2 findings can feed
  cleanly into Session 3 remediation and closeout

## Frozen Automated Workflow Subset

All five representative workflow IDs remain in scope for human confirmation.
Each covers a distinct high-value seam that would leave a meaningful gap if it
were dropped.

### `E2E-WF-04`

- **Persona:** `SystemAdmin`
- **Auth mode:** Seeded Session
- **Route path:** `/` -> `/admin/products`
- **Human confirmation focus:** confirm that the product-availability toggle is
  understandable, that the feedback around the state change feels trustworthy,
  and that the resulting page state is easy to read after the mutation

### `E2E-WF-05`

- **Persona:** `SystemAdmin`
- **Auth mode:** Seeded Session
- **Route path:** `/` -> `/admin/products` -> add-product dialog
- **Human confirmation focus:** confirm that the add-product dialog layout,
  validation copy, focus behavior, and keyboard path feel coherent rather than
  merely technically passable

### `E2E-WF-02`

- **Persona:** dual-membership internal plus customer user
- **Auth mode:** Seeded Session
- **Route path:** `/` -> `/customers` -> `Switch org` -> `/orders` ->
  `/admin/users`
- **Human confirmation focus:** confirm that switching from the internal
  organization to the retail-business scope makes the active organization
  obvious and that the route transition plus later denial state feel
  trustworthy

### `E2E-WF-01`

- **Persona:** regional sales rep with scoped `BackofficeUserAdmin` authority
- **Auth mode:** Seeded Session
- **Route path:** `/` -> `/admin/users` -> `Switch org` -> `/admin/users`
- **Human confirmation focus:** confirm that the initial warning state is
  understandable before the allowed scope is chosen and that the scoped admin
  banner and follow-on action remain clearly tied to the active organization

### `E2E-WF-03`

- **Persona:** retail-business `Customer`
- **Auth mode:** Real Magic Link
- **Route path:** `/login` -> magic-link completion -> `/orders` ->
  order-detail surface
- **Human confirmation focus:** confirm that the real magic-link journey is
  understandable end to end and that the signed-in orders flow feels coherent
  in the customer scope

## Human-Only Checks Worth Keeping

The later checklist should explicitly preserve these judgment-driven checks in
addition to replaying the selected workflows:

- clarity of the active-organization label after switching scope
- clarity of the `/admin/users` warning state before the allowed scope is
  chosen
- clarity of product-dialog validation messaging and keyboard flow
- clarity of the real magic-link sign-in experience
- general browser-feedback trustworthiness after the product-availability
  toggle changes state

These checks should remain small, focused add-ons to the frozen workflow
subset. They are not license to expand into new routes or re-run backend-truth
proof already owned by automation.

## Deferred And Excluded Work

The following areas remain out of scope for this set unless a finding directly
exposes them as follow-up work:

- new end-to-end route entry points or personas beyond the five approved
  representative workflow IDs
- broad exploratory regression testing across unrelated routes
- re-proving backend truth, database state, or mutating correctness that the
  representative Playwright suite already validates
- documentation or packaging readiness decisions beyond recording targeted
  remediation inputs when this set discovers a material issue
- broad accessibility recertification work that belongs to the completed
  `accessibility-and-stubbing` baseline unless a concrete full-app issue is
  observed during the selected workflows

## Queue And Parallelization Decisions

### Cross-Set

No downstream Phase 2 session set is safe to overlap with `full-app-uat`.

The initial assumption remains correct:

- `phase-2-docs-readiness` depends on the accepted full-app UAT baseline, not a
  draft checklist or untriaged findings ledger
- `packaging-and-template-readiness` still waits behind the documentation set
- no downstream spec text changes are justified by Session 1

### Intra-Set

Once this frozen scope is accepted:

- Session 2 checklist authoring can proceed
- Session 2 execution guidance and any narrowly scoped verification helpers can
  proceed in parallel with checklist authoring once they stay inside the frozen
  subset and findings rules

### Coordination Rules

- Session 2 should build the checklist directly from this ledger and the frozen
  workflow order rather than reopening workflow selection
- Session 2 should add verification helpers only when UI observation alone is
  insufficient and the helper materially improves confidence
- Session 3 should remediate only the small, clearly in-set issues and should
  defer broader follow-up work explicitly when the owner belongs elsewhere

## Findings Capture Rules

Each captured finding should include these fields:

- `id`
- `workflowId`
- `title`
- `location`
- `stepsToReproduce`
- `expectedResult`
- `actualResult`
- `classification`
- `severity`
- `status`
- `screenshotPath`
- `notes`

### Classification Buckets

- `Defect`: the observed behavior conflicts with an accepted requirement,
  established baseline, or clear workflow expectation
- `Acceptable Difference`: the observed behavior differs from reviewer
  preference or expectation but does not justify remediation inside the current
  baseline
- `Deferred Follow-up`: the observation is real or potentially valuable, but
  the proper owner is outside this set or the work is too broad for the current
  UAT lane

### Severity Scale

- `Critical`: blocks sign-in, navigation, or a core success path inside the
  frozen workflow subset
- `Major`: materially harms clarity, trust, or successful completion of a
  selected workflow and should be remediated before closeout
- `Minor`: noticeable issue with a reasonable workaround that does not break
  the workflow
- `Trivial`: cosmetic or extremely low-impact issue recorded for completeness

### Status Buckets

- `New`
- `Triaged`
- `Remediate`
- `Defer`
- `Closed`

## Remediation And Sign-Off Rules

The final sign-off and closeout work for this set should follow these rules:

1. Every captured finding must receive a final classification, severity, and
   status before the set can close.
2. Any `Defect` classified as `Critical` or `Major` must be remediated and
   rechecked inside Session 3 unless the project lead explicitly accepts a
   deferral with written rationale.
3. `Acceptable Difference` items do not require remediation, but the final
   findings ledger should explain why they were accepted.
4. `Deferred Follow-up` items must name the intended downstream owner or
   planning destination.
5. The accepted full-app UAT baseline must summarize the final disposition of
   all findings and what this human-confirmation pass does or does not
   guarantee.

## Execution Notes For Later Sessions

- Session 2 should generate a machine-readable checklist that preserves the
  frozen workflow order from this ledger
- Session 2 should keep human-only checks visibly distinct from automation-led
  assertions and backend-truth proof
- Session 2 findings capture should distinguish confirmed defects, acceptable
  differences, and deferred follow-up without blurring the categories
- Session 3 should update downstream assumptions only if the accepted UAT
  result materially changes what documentation or packaging work can safely
  assume
