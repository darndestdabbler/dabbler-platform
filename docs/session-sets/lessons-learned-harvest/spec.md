# Lessons-Learned Harvest Spec

> **Purpose:** Review completed issue artifacts, session reviews, and adjacent
> workflow documents to extract durable lessons and resolutions that should be
> recorded in `docs/planning/lessons-learned.md`.
> **Created:** 2026-04-16
> **Session Set:** `docs/session-sets/lessons-learned-harvest/`
> **Prerequisite:** `docs/planning/guiding-principles.md` and
> `docs/planning/lessons-learned.md` are current enough to serve as the
> baseline, and the orchestrator should review completed session reviews and
> issue-oriented artifacts before adding new durable lessons.
> **Workflow:** Orchestrator -> AI Router -> Cross-provider verification

---

## Project Overview

This set creates a deliberate pass over the repo's recent execution history so
that useful failures, corrections, and workflow surprises become durable team
memory instead of staying buried in raw session reviews or chat history.

The goal is not to re-litigate every past decision.

The goal is to identify lessons that are concrete, repeatable, and useful for
future AI-led sessions or future projects that inherit this workflow.

This work should stay focused on durable lessons and their phrasing. It should
not quietly turn into a broad rewrite of unrelated planning docs.

Because `full-app-uat` is currently in progress, this set is intended to be
run only when the human explicitly overrides the default active-session-set
selection.

## Candidate Inputs

Likely lesson sources include:

- completed `session-reviews/` files from recent session sets
- `activity-log.json` entries that document failures, reruns, or corrections
- issue logs and extracted UAT findings where they already exist
- workflow and reviewer docs whose wording changed after confusion or drift
- instruction-file edits that closed repeated execution mistakes

## Parallelization Notes

No parallelization assumption is required for this set.

Inside the set, artifact review and candidate-lesson drafting can proceed in
parallel once the initial source list is frozen.

## Session Plan

### Session 1 of 3: Source Scan And Candidate Lesson Inventory

**Steps:**
1. Review the highest-value completed session reviews, issue-oriented artifacts,
   and workflow docs that are most likely to contain durable lessons.
2. Build a candidate inventory of concrete lessons, grouped by theme such as
   workflow, testing, UAT, documentation, or validation.
3. Filter out observations that are too project-specific, too temporary, or
   already captured well enough in existing guidance.
4. Record which lesson candidates appear suitable for
   `docs/planning/lessons-learned.md` and which, if any, belong instead in
   `docs/planning/guiding-principles.md`.

**Creates:** Candidate lesson inventory and triage notes
**Touches:** This spec and planning notes as needed
**Ends with:** The likely additions are identified and scoped before wording
work begins
**Progress keys:** `session-1/source-scan`, `session-1/candidate-inventory`,
`session-1/theme-grouping`, `session-1/triage`

---

### Session 2 of 3: Lesson Drafting And Guidance Alignment

**Steps:**
1. Draft the approved lesson additions in `docs/planning/lessons-learned.md`
   using short, action-oriented wording.
2. Normalize the phrasing so each lesson states the context, the failure or
   friction, the lesson, and the future action.
3. Where a lesson appears broad enough to become a cross-project rule, propose
   a corresponding update for `docs/planning/guiding-principles.md`.
4. Update any nearby workflow docs only when the drafted lesson reveals that a
   key reference is ambiguous or incomplete.

**Creates:** Updated lessons-learned guidance and any tightly scoped alignment
notes
**Touches:** `docs/planning/lessons-learned.md`,
`docs/planning/guiding-principles.md`, and minimal adjacent workflow docs if
needed
**Ends with:** The durable lessons are written in a stable format that future
sessions can consume quickly
**Progress keys:** `session-2/lesson-drafting`,
`session-2/format-normalization`, `session-2/guidance-alignment`,
`session-2/doc-tightening`

---

### Session 3 of 3: Resolution Review, Closeout, And Change Log

**Steps:**
1. Re-review the drafted lesson additions against the source artifacts and
   confirm that each one still feels durable and actionable.
2. Capture any explicit follow-up recommendations for how future sessions
   should consult or extend the lessons-learned guidance.
3. Update the accepted baseline for lessons-learned usage if the harvested
   lessons justify it.
4. Generate `change-log.md` for this session set.

**Creates:** Harvest closeout summary and `change-log.md`
**Touches:** Planning docs and session-set closeout artifacts
**Ends with:** The repo has a deliberate, human-reviewed lessons-learned
baseline rather than ad hoc memory
**Progress keys:** `session-3/resolution-review`,
`session-3/followup-recommendations`, `session-3/baseline-update`,
`session-3/change-log`
