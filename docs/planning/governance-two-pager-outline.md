# Governance Two-Pager — Outline

> **Purpose:** Section-level outline for the AI-led development governance
> two-pager. Each section lists a single-sentence claim the full draft will
> expand into a short paragraph.
> **Audience:** Architecture, security, and enterprise application review boards.
> **Status:** Outline only. Full draft is Session 7 work.
> **Tool-agnosticism:** The outline describes the *workflow*, not any specific
> AI tooling. The same outline applies whether the workflow runs on the
> prototype's custom Python router or a commercial platform.

---

## Orientation

The two-pager answers the single governance question that spans all three
boards: *"What stops an AI agent from shipping broken code into our
environment?"* Everything downstream follows from that question.

The three board-specific paragraphs (sections 6–8) come **after** the
workflow is explained so reviewers have shared context before the
targeted reassurance. Section 11 scopes the ask — boards respond well to
an explicit *"here is what we are not asking you to approve."*

---

## Section-by-section outline

### 1. Problem and Position

AI-led development at the State needs the same governance guardrails that
apply to human-led development; this workflow is how we make existing
guardrails apply to AI-produced code, not a request for new ones.

### 2. The Workflow

Every change flows through a defined pipeline — spec → sessions →
cross-provider verification → commit → E2E tests → human UAT — with
append-only audit artifacts at each stage.

### 3. Why Human Judgment Comes Last, Not First

Automated E2E tests verify correctness before any human reviews anything;
human UAT verifies judgment-based qualities the tests cannot assert, and
is never the first line of defense against broken code.

### 4. How Correctness Is Verified

Every substantive AI output is reviewed by a model from a different
vendor than the one that produced it, against instructions stored in
three human-readable prompt-template files that reviewers can audit
directly.

### 5. Where Humans Decide

The workflow defines four mandatory human decision points — spec
approval, verifier-disagreement resolution, UAT execution, and
session-set sign-off — and the human is the authority on every judgment
the system surfaces.

### 6. Architecture Board View

The workflow is stack-agnostic, does not bind the State to any AI
vendor, and the implementation can move from custom Python (prototype)
to a commercial platform (production) without changing the workflow
discipline.

### 7. Security Board View

AI-produced changes pass through the same security controls as
human-produced changes — code review, static analysis, secret scanning,
existing vendor-risk review for the AI providers — with verification
output retained as evidence.

### 8. Enterprise Application Board View

Every existing review gate is preserved; what changes is only who drafts
first, and the verification discipline raises the signal-to-noise ratio
of board review rather than replacing it.

### 9. Implementation Spectrum

The prototype implements the workflow in custom Python to prove the
discipline is real and tractable; the production recommendation is a
commercial tool (Copilot Enterprise, Claude Code, Cursor Business)
configured to match the same workflow.

### 10. Metrics and Accountability

Every routed call logs cost, verdict, and disagreement outcome; a
manager-facing report summarizes cost and unreliability rates per
session set, making the workflow's discipline self-auditing.

### 11. What This Is Not

The workflow does not automate production deployment, bypass human
sign-off, remove any existing review gate, or introduce AI into any
regulated decision currently reserved for a human.

---

## Notes for the full draft (Session 7)

- **Section 3 is the load-bearing paragraph.** It is the direct answer
  to the question every board will ask. Expand it first and use the
  rest of the two-pager to support it.
- **Sections 6–8 are the three explicit board paragraphs** the BATON
  called for. Each board gets its own paragraph; each paragraph should
  call out the one concern most likely to come from that board and
  address it specifically.
- **Section 4 intentionally consolidates cross-provider verification
  and auditable LLM instructions.** The instructions only matter in the
  context of who reads them; splitting them wastes a section heading in
  a two-pager.
- **Section 10's headline number is the cost-vs-most-expensive-tier
  ratio** from `report.py` (Session 4 deliverable). That ratio is the
  strongest single governance artifact the workflow produces — a
  self-auditing metric that improves over time.
- **Reference the three prompt template files by name** in section 4:
  `system-prompts.md`, `task-prompts.md`, `verification.md`. Reviewers
  will want to read them directly.
- **Voice discipline:** boring is the goal. Avoid anything that sounds
  like marketing. Every paragraph should read as something a procurement
  officer could quote back without losing meaning.

---

## Open questions for the human before Session 7

1. One master two-pager with three paragraph blocks, or three distinct
   two-pagers (one per board)? Default assumption is one master.
2. Board order in sections 6–8. Outline has Architecture → Security →
   Enterprise Application. If one board is politically the hardest
   audience, leading with them may be the right move.
3. Whether section 10 (Metrics and Accountability) stays as its own
   section or folds into section 9 (Implementation Spectrum). Kept
   separate in this outline because the cost-ratio number deserves the
   visibility.

---

*Outline drafted in Session 1 of the BATON v2 deliverable list. The
streamlining execution plan (companion artifact) is driven by which
files this outline references.*
