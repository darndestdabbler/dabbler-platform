# Change Log — Security Dashboard UAT

> **Status:** Complete
> **Completed:** 2026-04-19 (retroactive close-out)
> **Sessions:** 1 of 1
> **Session Set:** `docs/session-sets/security-dashboard-uat/`

## Summary

Single-session human-UAT effort for the `/admin/security` dashboard after
the `security-dashboard-wiring` set populated all seven tabs with real
seed data. Session 1 generated the checklist, validated it against the
checklist-editor schema, and handed off to the human. The human executed
the checklist inline in `security-dashboard-uat-checklist.json`; the
findings fed two follow-up session sets that are now complete.

## What shipped

- `security-dashboard-uat-checklist.json` — 72 items (originally 40 at
  Round 1 verification; expanded to 42 after Round 1 remediation; grew
  again during human execution as the reviewer added per-area "Other"
  rows). Items cover Shell and Access, Tab Strip, each of the seven
  dashboard tabs individually, and Cross-Cutting Quality, with seeded
  test data (`admin@acmecoffee.com`, `jdoe@acmecoffee.com`,
  `james.smith@example.com`, and concrete entity/control/endpoint names)
  in every `HumanAction` field.
- Two rounds of cross-provider verification via `gemini-pro`. Round 1
  flagged 1 Major + 3 Minor issues (missing open-feedback rows in five
  functional areas; checklist coverage gaps). Round 2 was clean.
- Saved raw verifier output in `session-reviews/session-001.md`.

## Human UAT results (inline in the checklist JSON)

- **72 items** executed by the human.
- **17 Pass / 55 Fail** (many "Fail" marks attach to the per-area
  open-feedback rows rather than to concrete defects).
- **19 items** carry written feedback.
- Key findings:
  - `BackofficeUser` could not see any orders — remediated in
    `security-dashboard-behavioral-fixes` Session 1.
  - `SystemAdmin` could not edit orders — remediated in
    `security-dashboard-behavioral-fixes` Session 2.
  - Role elevations from the User Assignments tab did not take effect
    until read by the session-context loader — remediated in
    `security-dashboard-behavioral-fixes` Session 3.
  - Numerous UX polish items (tab-strip arrow keys, Allow/Deny control
    unification, inherited-vs-explicit visualization, access-denied
    flow, status-code re-execute, unassigned-users toggle, inline
    duplicate-role-name validation demo) — remediated in
    `security-dashboard-polish` Sessions 1–8.

## Follow-up session sets (both complete)

| Set | Scope | Sessions |
|---|---|---|
| `security-dashboard-polish` | UX-only UAT items | 8 of 8 |
| `security-dashboard-behavioral-fixes` | Three behavioral defects | 3 of 3 |

## Close-out note

`activity-log.json` previously recorded `totalSessions: 3`. This was a
placeholder typo: the spec defines exactly one session (generate +
hand off), and only Session 1 entries ever landed in the log. The
value was corrected to `1` as part of this close-out.

## Files

- **Created:** `security-dashboard-uat-checklist.json` (plus a timestamped
  snapshot, `security-dashboard-uat-checklist-20260417.json`).
- **Reviewed and archived:** `session-reviews/session-001.md`,
  `issue-logs/session-001.json`.
- **Corrected:** `activity-log.json` (`totalSessions: 3` → `1`).
