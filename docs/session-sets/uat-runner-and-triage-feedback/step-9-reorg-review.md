# Step 9 Reorg Review — uat-runner-and-triage-feedback

**Run after Step 8 notify.** Session Set Complete (Session 4 of 4).

## Promotion Candidates

### 1. Candidate: Four-class UAT failure taxonomy → `docs/planning/uat-failure-taxonomy.md` (Principle)

**Current location:** Embedded in `ai-router/prompts/uat_failure_triage.md` (system instruction) and documented in Session 2's spec.

**Promotion rationale:**
- **Durability:** The four-class taxonomy (APP_FUNCTIONAL_BUG, CATALOG_STALE, SEED_DATA_GAP, AMBIGUOUS_LOCATOR) is generic and applies to any Dabbler app using the W0 compiler and W1 catalog.
- **Reuse:** Future orchestration repos, Dabbler sibling apps, or external projects using the UAT DSL would benefit from this classification scheme.
- **Independence:** The taxonomy is independent of the W2 runner implementation. It's a design principle about how runtime failures map to root causes.
- **Two-context proof:** Used in Sessions 2 (triage design) and 3 (remediation dispatch) as the mental model.

**Action:** Create `docs/planning/uat-failure-taxonomy.md` documenting the four classes, their triggers, evidence patterns, and remediation paths. Add to `docs/planning/project-guidance.md` → Principles as a reference.

**Acceptance:** ✓ **YES** — This should be promoted. It's a durable classification scheme that transcends this specific runner.

---

### 2. Candidate: W0 compile-time validation collapses failure classes → `docs/planning/lessons-learned.md` (Lesson → Principle)

**Current location:** Noted in spec as a design insight ("Compile-time validation collapses seven runtime failure classes to four").

**Lesson phrasing:**
> **Invest in formal grammar for correctness gates.** The W0 compiler's compile-time validation (label-in-catalog checks, element-type safety, route reachability) eliminates entire failure classes that would otherwise require runtime triage. Investing in a formal DSL and rigorous validation pays for itself in the runner's simplicity. In the W2 context: three classes of failure (label drift, element-type drift, translation errors) are caught before the runner ever invokes the test, leaving only four runtime classes (functional bug, catalog drift, seed gap, locator ambiguity) to handle. This is not a coincidence — it's the payoff of moving validation left.

**Promotion rationale:**
- **Evidence:** Sessions 1 (initial runner build, handling compile failures), 2 (triage design, seeing fewer failure modes), and 3 (remediation dispatch, only four classes) all validate this lesson.
- **Broader value:** This lesson applies to any UAT/validation system, not just Dabbler. It guides architecture decisions for future tooling.
- **Timeless:** Formal validation and compile-time gates are a stable, durable principle.

**Action:** Add to `docs/planning/project-guidance.md` → Principles (or create a new "Formal Grammar and Validation" principle). Remove from lessons once promoted.

**Acceptance:** ✓ **YES** — This is a durable principle that should guide future validation work.

---

### 3. Candidate: Runner module promotion → `../dabbler-ai-orchestration` (future)

**Current location:** `ai-router/uat_runner/` (four modules + CLI).

**Rationale (deferred):**
- **Reuse condition:** A second Dabbler app or an external project using the W0 library should adopt this runner before promotion.
- **Packaging:** Once adopted, the runner could live in a dedicated `dabbler-ai-orchestration` package (shared across projects) instead of being copied.
- **Timeline:** This is a natural follow-on once another consumer emerges.

**Action:** Track as a **future promotion** candidate. Document in a new `docs/planning/future-refactors.md` or as a note in project-guidance.md.

**Acceptance:** ◯ **DEFER** — Condition: second consumer. Revisit in the next orchestration-focused set.

---

## Lessons Learned Summary

No new lessons emerged that warrant immediate documentation. The session largely executed to spec:

- Backfill showed the runner handles expected W0 limitations correctly.
- Verifier feedback (PEP 8 module naming) is noted as a future improvement but not urgent.
- Test suite validated that no regressions were introduced.

**No changes to `lessons-learned.md` required.**

---

## Summary of Changes

### To be merged:
- **Create:** `docs/planning/uat-failure-taxonomy.md` (reference document for the four-class taxonomy)
- **Update:** `docs/planning/project-guidance.md`
  - Add new Principle: "Formal Grammar and Validation" (move from lesson)
  - Revise/reference the new taxonomy document
  - Note about future runner module promotion

### No changes required:
- `docs/planning/lessons-learned.md` (no new lessons; existing entries remain valid)

---

## Sign-Off

Session Set `uat-runner-and-triage-feedback` is **COMPLETE** and **VERIFIED**.

- Artifact delivered: UAT runner (W2) with CLI, triage, and remediation dispatch.
- Integration complete: Workflow docs updated, project guidance expanded.
- Quality: VERIFIED (cross-provider), build + test suite passing.
- Promotions: Two candidates accepted (taxonomy + principle); one deferred (module reuse).

Ready for handoff to human review (Step 9 complete).
