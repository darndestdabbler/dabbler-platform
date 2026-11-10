# Verification Round 1

## Verification Report — Session 1 (Planning)

### Scope decisions correctly reflected?

**Safety-net removal:** Correctly stated in three reinforcing locations — the Status section (decision 1), Session 6 Scope note ("removes … rather than remediating"), and Session 6 Step 1 ("Delete the sensitive-name safety-net feature"). The exit criteria for Finding 5 explicitly says "The safety-net feature is **removed** (no exit bullet for the in-circuit refresh bug, because the feature is gone)." ✓

**Three remaining Finding 5 items:** SSN in dropdown (Session 6 Step 2, "Closes finding 5a"), Add button (Step 3, "5b"), singular table names (Step 4, "5c"). All three have matching Playwright assertions in Step 5 and matching exit-criteria sub-bullets. ✓

**8-session count:** Status section confirms it; Sessions 1–8 are all present with distinct scope. ✓

### New Principle consistency

The Principle explicitly names "name-scanners, 'suggested items' safety-nets, auto-flag heuristics" as the class of tooling to avoid — directly matching the feature being removed. The narrowing clause ("specific to items a business role is already accountable for under an external review process; it is not a general rejection of security detection") correctly scopes it to compliance-accountable classifications and preserves room for dependency scanning, test guardrails, and automated access reviews. ✓

### Finding cross-references in Steps

| Session | Findings closed | Reference text present |
|---------|----------------|----------------------|
| 2 | 1 | ✓ |
| 3 | 2 | ✓ |
| 4 | 3, 3 (sub), 6 | ✓ |
| 5 | 4 | ✓ |
| 6 | 5a, 5b, 5c | ✓ |
| 7 | 7, 8, 9 | ✓ |
| 8 | (closeout) | n/a |

### Playwright coverage + regression steps per behavioural session

| Session | New Playwright step | Regression step |
|---------|-------------------|-----------------|
| 2 | Step 4 ✓ | Step 5 ✓ |
| 3 | Step 3 ✓ | Step 4 ✓ |
| 4 | Step 4 ✓ | Step 5 ✓ |
| 5 | Step 4 ✓ | Step 5 ✓ |
| 6 | Step 5 ✓ | Step 6 ✓ |
| 7 | Step 6 (update only — see Issue 1) | Progress key only — see Issue 2 |
| 8 | Step 2 (gap-fill) ✓ | Step 3 ✓ |

### Session sizing

Session 7 is the largest (spike two idioms → apply winner to four tabs → condensed grid → DisplayAlias fix → arrow-key removal → update all Playwright tests). The risk notes explicitly acknowledge this and provide a mitigation (timebox the spike; defer the Users tab if needed). Remaining sessions are appropriately scoped. ✓

### 8-session count justification

Sessions cannot be trivially combined:
- Sessions 2 and 3 share a dependency (Session 3's Playwright tests need the tab-load crash fixed in Session 2).
- Sessions 4 and 5 operate on different subsystems (EF global query filter vs. route-level authorization).
- Sessions 6 and 7 mix metadata/seed work with UX refactoring across different component sets.
- Session 8 must follow all behavioural sessions.

No split is obviously required; the Session 7 risk note provides a contingency. ✓

### Step 9 ordering in Session 8

Step 5: "Notify, then run the Step 9 reorganization review… Post-notify ordering is mandatory so the human isn't blocked." Progress keys confirm: `session-8/commit-and-notify` precedes `session-8/step-9-reorganization-review`. ✓

### Set B regression targets called out

The Risk notes section "Regression targets from Set B that must NOT regress" lists seven specific behaviors (five-tab visibility, Role Documentation content, Mask Exemption grant, cross-tab reactivity, DisplayAlias persistence, filter narrowing by alias, Tab-key focus after arrow-key removal). All are appropriate. ✓

### Exit criteria completeness

One bullet per finding (1–4, 5 with three sub-bullets + removal note, 6–9). Additional bullets for: checklist + coverage co-committed, build green, Playwright green, Principle preserved, Set D unblocked. ✓

### Internal consistency check

**Progress-key collisions:** All 37 keys are unique across sessions. ✓

**Path consistency:** Created artifacts (`SecurityDashboardUatCoverageTests.cs` in Session 2; checklist and change-log in Session 8) are referenced correctly in downstream Touches sections. ✓

**Contradictions:** Session 6 Step 1 says "Remove the backing scanner service" while the Session 6 risk note says "If a consumer is already owned by a future session (Set D masking), mark the scanner `[Obsolete]` rather than deleting." This is standard risk-note practice (default action + contingency) and is not a true contradiction. ✓

---

## Issues Found

1. **[Minor] Session 7 lacks explicit numbered steps for NEW Playwright assertions on findings 7 and 8.** Step 6 says "Update all prior Playwright tests to navigate through the new master-details UX" — this covers making existing tests work with the new layout, but the set's own rule ("every session that adds a UAT-visible behaviour must add the corresponding Playwright test") requires *new* assertions for the master-details idiom behavior (e.g., panel collapse on row select, "New" tab rename for Organization), the condensed grid viewport, and the DisplayAlias fix. These new assertions are not called out as a step distinct from updating prior tests.

2. **[Minor] Session 7's regression-run step exists only as a progress key (`session-7/run-regression-tests`) without a numbered step.** Every other behavioural session (2–6) has an explicit numbered step reading "Regression: re-run Sessions N–M tests." Session 7 implies it via step 6's "so they remain green" phrasing, but the structural inconsistency could cause the regression run to be overlooked during execution.

3. **[Minor] Finding 6 attribution is split but exit criteria assigns it entirely to Session 4.** Finding 6 includes two concerns: (a) in-circuit grid refresh after mutations, and (b) the "New Organization" scroll-to-row / layout UX. Session 4 Step 3 addresses (a) and claims "(Closes finding 6.)" Session 7 Step 1 Idiom B explicitly addresses (b) ("the Details tab renames itself to 'New' when a new record is staged (per the UAT feedback)"). The exit criteria bullet says "Finding 6 … closed under Session 4's coverage" without acknowledging Session 7's contribution. Either the exit bullet should say "closed under Sessions 4 and 7" or Session 7 Step 2 should co-reference finding 6.

---

VERDICT: ISSUES_FOUND

1. [Minor] Session 7 missing explicit new-Playwright-assertion step for findings 7/8 (only has an "update prior tests" step).
2. [Minor] Session 7 regression-run is a progress key only, not a numbered step — inconsistent with Sessions 2–6.
3. [Minor] Finding 6 exit criteria attributes entirely to Session 4 despite the scroll/layout portion being addressed in Session 7.

---

# Verification Round 2

## Round 2 Verification — Targeted Fix Confirmation

### Issue 1: Session 7 missing explicit new-Playwright-assertion step for findings 7/8

**Expected:** A NEW step distinct from step 6, with a corresponding progress key.

**Found in spec — Session 7 Step 7:**
> "Add NEW Playwright assertions for the behaviours this session introduces. This is distinct from step 6 (updating existing tests to navigate the new layout) and covers:
> - The chosen master-details idiom's core behaviour … (New assertions for finding 7.)
> - The condensed grid viewport (~6 rows visible without scroll …)
> - DisplayAlias column text-wrap / auto-size behaviour … (New assertions for finding 8.)"

Corresponding progress key `session-7/add-new-ux-assertions` is present.

Step 7 is structurally separate from Step 6 (which handles updating prior tests). All three required assertion categories — idiom behaviour, condensed grid viewport, DisplayAlias — are enumerated. **✓ Resolved.**

### Issue 2: Session 7 regression-run is a progress key only, not a numbered step

**Expected:** An explicit numbered step parallel to Sessions 2–6.

**Found in spec — Session 7 Step 8:**
> "Regression: re-run Sessions 2-6 tests end-to-end against the new UX to confirm no behavioural regressions; this is the largest regression pass of the set."

Matches the phrasing pattern of Sessions 2–6 regression steps. Progress key `session-7/run-regression-tests` aligns. Session 7 now has 8 numbered steps with 8 progress keys — consistent. **✓ Resolved.**

### Issue 3: Finding 6 exit criteria attributes entirely to Session 4

**Expected:** Exit criteria credits both Sessions 4 and 7, and/or Session 7 Step 2 co-references finding 6.

**Found in spec — both remediation paths applied:**

Exit criteria bullet now reads:
> "**Finding 6** (Organization mutation UX + in-circuit grid refresh) closed across **Sessions 4 and 7**: Session 4 delivers the in-circuit grid refresh after add / edit / archive and filter changes; Session 7 delivers the 'new record' layout cue via the chosen master-details idiom …"

Session 7 Step 2 now reads:
> "(Closes finding 7; **closes the layout portion of finding 6** — Session 4 closes its in-circuit-refresh portion.)"

Both locations are updated and mutually consistent. **✓ Resolved.**

### New-issue scan (limited to the edits)

- **Step numbering continuity:** Session 7 steps 1–8 are sequential with no gaps or duplicates. ✓
- **Progress-key uniqueness:** All 8 Session 7 keys are distinct from each other and from all other sessions. ✓
- **"Ends with" consistency:** Session 7's closing statement references "full Playwright suite green against the new UX," consistent with the new Step 8 regression run. ✓
- **Exit-criteria internal consistency:** Finding 6 split (Sessions 4 + 7) is consistent between the exit-criteria bullet and the step-level cross-references. Session 4 Step 3 still says "(Closes finding 6.)" without the qualifier, but the exit criteria and Session 7 Step 2 together make the split unambiguous — no material risk of misexecution. ✓
- **No unintended deletions or edits** to surrounding sections (verified Session 6, Session 8, Rules, Risk notes are unchanged from round-1-verified content). ✓

---

VERDICT: VERIFIED
