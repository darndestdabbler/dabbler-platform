# security-dashboard-uat-remediation-review — UAT Review Session Spec

> **Purpose:** Disposition every finding in the filled
> `security-dashboard-uat-remediation-uat-checklist.json` so the
> human's UAT signal is not left sitting in a JSON file. This is a
> **UAT Review Session** set per
> `docs/planning/project-guidance.md` → Principles → Human UAT
> Philosophy → "UAT Review Session (set type)".
> **Created:** 2026-04-21
> **Session Set:** `docs/session-sets/security-dashboard-uat-remediation-review/`
> **Prerequisite:** `security-dashboard-uat-remediation` complete
> (change-log.md committed 2026-04-21); human UAT run committed
> 2026-04-20 as `11b6fab`.
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Context

The `security-dashboard-uat-remediation` session set closed with a
54-item UAT checklist. The human ran the checklist through the editor
and committed the filled file in commit `11b6fab`. Of 54 items:

| Count | Disposition |
|---:|---|
| 26 | Pass, clean, no feedback — no action required |
| 1  | Pass, judgment item with rationale ([32] DisplayAlias width) |
| 10 | Fail or feedback with substantive content |
| 17 | `Other` auto-rows with `Passes=false` and empty Feedback — treated as no-finding |

This UAT Review Session classifies the 10 substantive items, produces
a disposition artifact, and queues the next action. Per project-guidance,
the output is one of:

- A **stub spec** for a follow-on remediation set (Substantive findings
  that need design+plan work)
- A **one-commit micro-fix plan** (Minor findings small enough to
  batch into a single remediation session)
- A **signoff note** (no action required)

Based on the ten substantive findings below, the most likely output is
a **hybrid**: a stub spec for a small follow-on set (the Substantive
items clustered around role-gated nav, in-circuit refresh, and the
inline-edit architectural question) **plus** a separate Minor-batch
note for items that can fold into adjacent planned sets.

---

## Session Plan

### Session 1 of 1: Classify + Disposition + Queue Next Action

**Steps:**

1. Read `docs/planning/project-guidance.md` and
   `docs/planning/lessons-learned.md`.
2. Read the filled UAT checklist at
   `docs/session-sets/security-dashboard-uat-remediation/security-dashboard-uat-remediation-uat-checklist.json`.
3. Enumerate every Review item where `Passes == false` OR
   `Feedback` is non-empty (including `Other` auto-rows). Empty `Other`
   auto-rows with `Passes=false` and empty `Feedback` are treated as
   non-findings (the reviewer did not fill them in).
4. Route the classification pass through
   `route(task_type="analysis")`. The router picks the cheapest capable
   tier for mechanical pattern matching; per project-guidance this sizing
   fits Sonnet cleanly. For each finding, produce:
   - Severity (**Blocker** / **Substantive** / **Minor** / **Out of scope**)
   - Suggested disposition (new set / fold-into-existing-set / micro-fix / signoff)
   - One-line rationale
5. Produce the disposition artifact
   `docs/session-sets/security-dashboard-uat-remediation-review/disposition.md`
   with one row per substantive finding plus a summary block naming the
   next action.
6. If the dispositions justify a remediation set, produce a stub
   `spec.md` shell at
   `docs/session-sets/<next-set-slug>/spec.md` with the goals and the
   sessions implied by the findings. If a micro-fix plan is sufficient,
   produce a one-section micro-fix plan inline in `disposition.md`
   instead. If no action is required, record the signoff in
   `disposition.md` and stop.
7. Build + test. `dotnet build Dabbler.Platform.sln`
   then `dotnet test Dabbler.Platform.sln`. This set does not touch
   product code, so both should pass without change. The check is a
   guardrail against accidental edits during the review. Playwright
   (Docker) is not re-run when the session creates no product-code
   changes; the prior set's green Playwright baseline applies.
8. End-of-session verification via
   `route(task_type="session-verification")` with the classification
   reasoning, the disposition artifact, and the stub spec (if any).
9. Cost report, change-log.md, commit + push.
10. Send session-complete notification.
11. Step 9 reorganization proposals review (run after notify).

**Creates:**
- `docs/session-sets/security-dashboard-uat-remediation-review/disposition.md`
- Possibly `docs/session-sets/<next-set-slug>/spec.md` (stub) if a
  remediation set is justified

**Touches:** None in product code. This is a review session; it does
not modify Blazor components, handlers, tests, or configuration.

**Ends with:** The checklist's findings have a named disposition. The
next action is either a queued remediation set, a queued micro-fix, or
an explicit "no remediation needed" signoff. The human is not left
holding the checklist.

**Progress keys:**
- `session-1/read-guidance`
- `session-1/parse-checklist`
- `session-1/route-classification`
- `session-1/disposition-artifact`
- `session-1/next-action-artifact`
- `session-1/build-and-test`
- `session-1/session-verification`
- `session-1/change-log`
- `session-1/notify`
- `session-1/step9-reorganization`

---

## What This Set Preserves

| Existing Asset | Status |
|---|---|
| Filled UAT checklist | Not modified — the human's inline results are the input |
| All Playwright tests | Must continue passing |
| All non-Playwright tests | Must continue passing |
| Session-set spec + activity-log for `security-dashboard-uat-remediation` | Not modified |

## Risk Notes

- **Misclassifying a Substantive as Minor** risks folding a real
  defect into an unrelated set's scope where it gets diluted. When a
  finding is ambiguous between Substantive and Minor, the orchestrator
  MUST route to Opus (`complexity_hint=80`) per the project-guidance
  Orchestrator guidance for UAT Review Sessions, not default to the
  cheaper tier.
- **Empty `Other` rows with `Passes=false`.** Treat these as
  non-findings unless the reviewer's `Feedback` content contradicts
  that interpretation. The UAT editor's default state for an unchecked
  `Other` row is `Passes=false`; without feedback content there is no
  human signal to act on.
