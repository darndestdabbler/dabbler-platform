# UAT Review Disposition — security-dashboard-uat-remediation

**Input:**
[`security-dashboard-uat-remediation-uat-checklist.json`](../security-dashboard-uat-remediation/security-dashboard-uat-remediation-uat-checklist.json)
(filled by the human 2026-04-20, commit `11b6fab`).

**Review Session:** `security-dashboard-uat-remediation-review`,
Session 1 of 1, 2026-04-21.

**Classification:** performed via `route(task_type="analysis")` →
`gemini-pro`, $0.012. Raw classification output preserved at
`session-reviews/classification-round1.md`.

---

## Checklist overview

| Count | State |
|---:|---|
| 26 | Pass, clean, no feedback — no action |
| 1  | Pass, judgment item with justification ([32] DisplayAlias width) — no action |
| 10 | Substantive findings (classified below) |
| 17 | `Other` auto-rows with `Passes=false` + empty `Feedback` — treated as non-findings |

---

## Classified findings

| # | Area / Subarea | Severity | Disposition | One-line rationale |
|---:|---|---|---|---|
| 2 | Shell and Access / Per-Role Security Nav Visibility and Tab Gating | **Blocker** | new-set | Admin drawer not visible for baked-in User — regression against the Session 3 deliverable that made `/admin/security` reachable by every baked-in role. |
| 8 | Organization Master Details / Org-Scope Filter | Out of scope | checklist-fix | Product works; the step's URL in the checklist was wrong. |
| 9 | Organization Master Details / Org-Scope Filter | Out of scope | checklist-fix | Product works; the step's URL in the checklist was wrong. |
| 11 | Organization Master Details / In-Circuit Refresh + New Org Cue | **Blocker** | new-set | New organization not visible after create — regression against the Session 4 in-circuit grid refresh deliverable. |
| 14 | Data-Bound Restriction Enforcement / Restriction-Seam Reachability | Minor | new-set | Currency symbol on `/products` is incorrect — incidental cosmetic issue unrelated to the checklist item's subject. |
| 20 | Data-Bound Restriction Enforcement / ViewOnly Rendering | Out of scope | signoff (architectural question) | Reviewer raised a design question about inline-edit vs. row-click-to-details; not a bug, warrants a design conversation. |
| 33 | Accessibility and Keyboard / Tab-Strip Arrow-Key Handler Removal | Out of scope | needs-reviewer-clarification | `Passes=False` with empty `Feedback`; ambiguous whether this is a stale auto-default or a real "didn't test" signal. |
| 34 | Shell and Access / (Other) | Out of scope | product-backlog | General suggestion to compact grid layouts — design backlog, not a defect. |
| 35 | Users and Role Assignment / (Other) | Out of scope | checklist-tooling | Reviewer asking for a "summary row of what's already been tested" in the UAT editor — a tooling/process suggestion for the checklist editor, not a product defect. |
| 49 | Master-Details Expansion Shell / Re-Expand | Minor | new-set | Two clicks to collapse when clicking a first-column cell — real but minor UX bug in the `MasterDetailExpansionShell` refactor (Session 7). |

---

## Next action

**(A) Stub spec for a new single-session follow-up set, plus two
light-weight side notes:**

1. **New session set:**
   `security-dashboard-uat-followup-fixes` (1 session).
   Scope: findings **[2]** (baked-in User nav regression), **[11]** (in-circuit
   refresh regression), **[14]** (currency symbol), **[49]** (master-details
   double-click collapse). Stub spec committed in this review session at
   [`../security-dashboard-uat-followup-fixes/spec.md`](../security-dashboard-uat-followup-fixes/spec.md).
   This set runs **before Set D (`sensitive-field-masking-audit`)** because
   the two Blockers are regressions against the just-closed set.

2. **Checklist URL corrections for [8] and [9]:** deferred to the
   follow-up set above (it already has to re-run UAT on the in-circuit
   refresh fix, so the step-URL corrections land in the new UAT
   checklist for that set's Session 1, not as a bare edit to the
   filled historical checklist).

3. **Architectural question on [20]:** deferred. The
   inline-edit-vs-row-click question is a product-level design
   conversation that should happen before a session set is scoped to
   implement inline editing. Recorded here so the signal is not lost.

4. **Process/tooling suggestions on [34] and [35]:** recorded as
   product-backlog items for the human to consider. Neither belongs in
   product code.

5. **Finding [33] ambiguity:** the new follow-up set's UAT checklist
   should either re-ask the same item with a clarified expectation, or
   explicitly drop it as covered-by-test (the arrow-key handler
   *removal* is verifiable by a Playwright keyboard assertion).

## Roadmap impact

The role rearchitecture roadmap was A → B → UAT-remediation → D → C.
With this review, the ordering becomes:

**A** (complete) → **B** (complete) → **UAT-remediation** (complete)
→ **security-dashboard-uat-followup-fixes** (queued, 1 session)
→ **D: sensitive-field-masking-audit** → **C: rearchitecting-role-administration** (still deferred).

The follow-up set is inserted ahead of Set D because its two Blockers
are regressions against deliverables Set D's masking-audit work plugs
into (the restriction seam and the in-circuit refresh pattern both
need to behave reliably before masking is layered on).
