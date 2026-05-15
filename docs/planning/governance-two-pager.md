# Governance Two-Pager — AI-Led Development Workflow

> **Purpose:** Describe how the organization's architecture, security, and
> enterprise application review gates continue to apply when AI agents draft
> changes to the codebase.
> **Audience:** Reviewers and approvers responsible for architecture, security,
> and enterprise application concerns.
> **Scope:** The workflow, not any specific AI tooling. The same document
> applies whether the workflow runs on the current codebase, a commercial
> platform, or a hybrid.

---

## 1. Problem and Position

AI-led development in this organization is governed by the same standards
that apply to human-led development. This document describes the workflow
that makes the organization's existing review gates — architecture review,
security review, and enterprise application review — continue to function
when an AI agent drafts the first version of a change. It is not a request
to approve new controls, relax existing ones, or carve out an exception.
Every gate that applies today applies identically to AI-produced code under
this workflow.

## 2. The Workflow

Every change proceeds through a defined pipeline: a human-approved
specification, one or more AI-led work sessions, cross-provider verification
of each substantive output, commit to version control, execution of
automated end-to-end tests, and finally human user acceptance testing. Each
stage produces append-only audit artifacts — the specification itself, the
session log, the verification record, the commit history, the test results,
and the checklist execution record. The router's metrics log captures each
verification call in a single append-only file, which a manager-facing
report aggregates into a session-set-level summary.

## 3. Why Human Judgment Comes Last, Not First

The single governance question this workflow answers is: what stops an AI
agent from shipping broken code? The answer is not that a human will catch
it in review. The answer is that four layers of automated proof — strict
unit tests, integration tests, component tests, and end-to-end browser
tests — run against every change before any human reviews anything. Human
user acceptance testing is the fifth and final layer, and its purpose is
explicit: it evaluates judgment-based qualities that automation cannot
score — clarity, coherence, visual quality, workflow sensibility. Human
reviewers are not the first line of defense against broken code.

## 4. How Correctness Is Verified

Every substantive AI output is reviewed by a model from a different vendor
than the one that produced it. Verifiers are selected at runtime from the
organization's approved model pool by a rules engine that prefers a
different-provider model at matching capability tier and breaks ties on
price; retiring or replacing a model requires editing only its entry in
the configuration, with no pairing table to maintain. Verifier eligibility
is a separate opt-in from generator eligibility, so a newly approved model
performs generation work before it is trusted to verify other models'
output. The instructions given to every routed model live in three
human-readable files — `system-prompts.md`, `task-prompts.md`, and
`verification.md` — which reviewers audit directly.

## 5. Where Humans Decide

The workflow defines four mandatory human decision points. A human approves
the specification before any session begins. When the verifier and the
generator disagree, the router treats additive task types (code review,
documentation) differently from logic-divergence task types (architecture,
planning, analysis); the latter cannot be resolved by merging two responses
and go to human adjudication. A human executes user acceptance testing
against the checklist. A human signs off the completed session set. Each
decision, including the reasoning for adjudicated disagreements, is
written to the same append-only metrics log as every other routed call.

## 6. Security Concerns

AI-produced changes pass through the same security controls as
human-produced changes: code review, static analysis, secret scanning,
dependency review, and the organization's existing vendor-risk process for
each AI provider. API credentials for the AI providers go through
established secret-management channels; no new secret-storage surface is
introduced. The verification record for every routed call is preserved in
an append-only log that a reviewer can examine after the fact, providing
durable evidence that a different-provider verifier reviewed each
substantive output.

## 7. Architecture Concerns

The workflow is stack-agnostic and does not bind the organization to any
AI vendor. The implementation can be re-platformed or remain as it is
without changing the workflow discipline. If the generation platform is
ever consolidated to a single vendor — for example, a commercial tool that
natively uses only one provider — cross-provider verification is preserved
by a separate verification call to a second vendor's API after generation,
decoupling the verification guarantee from the generation tooling. When a
verifier provider is temporarily unavailable, the router selects an
alternate from a different provider and logs the fallback.

## 8. Enterprise Application Concerns

Every existing review gate is preserved. What changes is only who produces
the first draft. The cross-provider verification step raises the
signal-to-noise ratio of later human review: routine work-product defects —
the kind a first reader would typically surface — are flagged by a second
model before the material reaches a human reviewer. Reviewer attention
redirects toward the judgment-based concerns that automation cannot
address.

## 9. Implementation Spectrum

The current solution, while in prototype status, was developed against
production criteria — security, effectiveness, efficiency, implementation
transparency, and agility in the context of rapidly evolving technology
among them. The production implementation — whether this codebase, a
commercial AI development platform (for example, Copilot Enterprise,
Claude Code, or Cursor Business), or a hybrid of the two — is a choice
evaluated against those same criteria and revisited as the market matures.

## 10. Metrics and Accountability

Every routed call is logged with its cost, verdict, and any disagreement
outcome. A manager-facing report aggregates these records per session set
into a small number of headline measures: a cost ratio comparing actual
spend against what the same work would have cost at the most expensive
available tier, and an adoption rate that tracks how often verifier
findings are accepted by human adjudication. The report is reproducible
from the log alone, making the workflow self-auditing over time.

## 11. What This Is Not

This workflow does not automate production deployment. It does not bypass
human sign-off. It does not remove any existing review gate. And it does
not introduce AI into any regulated decision currently reserved for a
human.

---

*Draft produced in Session 7, 2026-04-20.*
