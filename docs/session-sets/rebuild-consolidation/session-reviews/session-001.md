# Verification Round 1

# Session 1 Verification: Baseline Capture And Lessons Learned

## Verdict: **VERIFIED**

## Spec Compliance

| Step | Required | Delivered | Status |
|------|----------|-----------|--------|
| 1. Inventory intended behavior and confirmed working flows | `baseline-capture.md` | ✅ Current system shape, business scope, roles, confirmed behavior, and partially-trusted areas all documented | PASS |
| 2. Document escaped issues, root causes, and prevention owners | `lessons-learned.md` | ✅ Seven escaped issues mapped to root causes and prevention layers (scaffolding/library/tests/docs) | PASS |
| 3. Create parity checklist | `parity-checklist.md` | ✅ Must-match, may-improve, and must-rethink sections with checkable items | PASS |
| 4. Create "do not carry forward" list | `do-not-carry-forward.md` | ✅ Eight fragile patterns with rationale and preferred replacements | PASS |
| 5. Define minimum critical user journeys | `parity-checklist.md` §Minimum Critical User Journeys | ✅ Seven numbered journeys covering auth, denial, role access, and backend-verified mutation | PASS |

| Progress Key | Present in Outputs | Status |
|---|---|---|
| `session-1/baseline-capture` | `baseline-capture.md` | PASS |
| `session-1/lessons-learned` | `lessons-learned.md` | PASS |
| `session-1/parity-checklist` | `parity-checklist.md` | PASS |
| `session-1/do-not-carry-forward` | `do-not-carry-forward.md` | PASS |

## Internal Consistency Check

- **Escaped issues ↔ Do-not-carry-forward:** Every escaped issue in the lessons-learned table has a corresponding entry in the do-not-carry-forward table. No orphaned items in either direction.
- **Baseline "partially trusted" ↔ Parity "must be rethought":** Authorization response shape ambiguity (`404` vs. in-app denial), dev email naming mismatch, and page-smoke false confidence all appear in both documents consistently.
- **Role model consistency:** `SystemAdmin`, `BackofficeUserAdmin`, `BackofficeUser`, `Customer` and their page-level permissions are consistent across `baseline-capture.md`, `parity-checklist.md`, and `spec.md`.
- **Spec ↔ Session outputs:** The session-set `spec.md` migration principles, guardrails, and security rules are all reflected in the lessons and carry-forward guidance without contradiction.
- **Critical user journeys ↔ Baseline confirmed behavior:** Journeys 1–6 map directly to validated behavior documented in baseline-capture. Journey 7 (backend-verified mutation) is explicitly flagged as a gap in current coverage, which is honest and correct.

## Build/Test Outcome Assessment

No production code was changed in this session, so the build/test results reflect the **pre-existing state of the current demo repo**, not a regression introduced by Session 1.

**Important process observations:**

1. **Magic-link test failures** ("No dev magic-link code was logged") are **already documented** in `baseline-capture.md` under "Dev email semantics" and in `lessons-learned.md` row 3. The documents correctly identify this as a naming/behavior mismatch the rebuild must fix. The test failures confirm the baseline capture is accurate.

2. **Shell/nav test failures** (missing smart filters, record pills, orders grid) fall under the "Broader E2E confidence" partially-trusted area in `baseline-capture.md`, which explicitly warns that the full Playwright suite has not been revalidated. This is consistent.

3. **Test timeout** (10 minutes) is a pre-existing infrastructure concern. It does not affect Session 1 deliverables but reinforces the spec's emphasis on Docker-first deterministic test infrastructure in Sessions 2 and 4.

**Net assessment:** The test failures do not invalidate Session 1 outputs. They actually validate the accuracy of the baseline capture's "partially trusted" designations.

## Minor Observations (non-blocking)

- `parity-checklist.md` includes "Customer row-level order restrictions remain part of the intended model" as a must-match item, but `baseline-capture.md` does not list this as confirmed/validated behavior. This is acceptable—it’s capturing intent—but the rebuild team should note it needs explicit validation during Session 5.
- The spec references progress keys but no progress-tracking file was created to record completion. This is fine for Session 1 but should be addressed before Session 2 begins.

