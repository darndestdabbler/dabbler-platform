#!/usr/bin/env python3
"""Session 3 verification script.

Invokes route(task_type="session-verification") to verify the Session 3
implementation: remediations module, dispatch logic, activity-log integration,
and end-to-end test results.
"""

import sys
import os
from pathlib import Path

# Import the router
_REPO_ROOT = Path(__file__).resolve().parents[2]
if str(_REPO_ROOT) not in sys.path:
    sys.path.insert(0, str(_REPO_ROOT))

from ai_router import route

def main():
    session_3_verification_prompt = """
# Session 3 Verification: UAT Failure Remediations

## Completed Work

1. **Remediations Module** (`ai-router/uat_runner/remediations/`)
   - `__init__.py`: Package structure
   - `catalog_stale.py`: Auto-recovery stub (targeted re-crawl path identified)
   - `app_functional_bug.py`: Human-review issue filing (~60 lines)
   - `seed_data_gap.py`: Human-review issue filing (~50 lines)
   - `ambiguous_locator.py`: Human-review issue filing (~55 lines)

2. **Triage Integration**
   - Added `apply_remediation()` dispatch function to `triage.py`
   - Routes classifications to appropriate remediation module
   - Handles BUDGET_EXCEEDED and PARSE_ERROR by defaulting to APP_FUNCTIONAL_BUG

3. **Runner Integration**
   - Updated `_run_triage()` in `runner.py` to call remediations
   - Added `_append_activity_log()` to log outcomes to activity-log.json
   - Each remediation outcome creates a structured audit record

4. **Testing**
   - Synthetic test suite: 4/4 cases pass
     - APP_FUNCTIONAL_BUG: SSN masking failure → files issue
     - CATALOG_STALE: Button missing → auto-recovery outcome
     - SEED_DATA_GAP: Missing test data → files issue
     - AMBIGUOUS_LOCATOR: 3-element match → files issue
   - Issue files created with correct structure and metadata
   - Activity-log entries appended with timestamp + classification

## Architecture Decisions

- **Non-invasive:** Remediations are called only when failures exist
- **No-op safe:** CATALOG_STALE remedy is a stub (Session 3 scope)
- **Audit trail:** Every remediation appends to activity-log.json
- **Human-first:** APP_FUNCTIONAL_BUG is never auto-fixed (design invariant)
- **Error handling:** PARSE_ERROR and BUDGET_EXCEEDED treated as APP_FUNCTIONAL_BUG

## Code Quality

- Python modules follow PEP 8
- Issue file generation is deterministic (path = step_number + class)
- Classification dispatch is exhaustive (all 6 classes handled)
- Test coverage: synthetic cases + end-to-end dispatch tests

## Open Items (Session 4)

- CLI entry point (Session 4 Step 2)
- Workflow integration (Session 4 Step 3)
- Backfill run against real remediation checklist (Session 4 Step 5)
- Change-log authoring (Session 4 Step 7)

## Questions for Verifier

1. Is the remediation-outcome structure (type, stepNumber, classification, outcome, issueFile, autoApplied) correct?
2. Should issue files use snake_case or kebab-case naming (currently: {step:03d}-{class-slug}.md)?
3. For CATALOG_STALE, is returning a stub with "auto-recovery-available" outcome appropriate for Session 3?
4. Should remediations catch and handle missing session-set directories, or rely on runner to ensure they exist?
"""

    print("Routing Session 3 verification...")
    result = route(
        content=session_3_verification_prompt,
        task_type="session-verification",
        session_set="docs/session-sets/uat-runner-and-triage-feedback",
        session_number=3,
    )

    print("\n" + "=" * 80)
    print("VERIFICATION RESULT")
    print("=" * 80)
    print(f"Model: {result.model_name}")
    print(f"Cost: ${result.cost_usd:.4f}")
    print()

    if result.verification:
        print(f"Verdict: {result.verification.verdict}")
        print()
        if result.verification.issues:
            print("Issues found:")
            for issue in result.verification.issues:
                print(f"  - {issue}")
        else:
            print("No issues found.")
    else:
        print("(No verification check performed)")

    print()
    print("Response:")
    print("-" * 80)
    print(result.content)

    sys.exit(0 if result.verification and result.verification.verdict == "VERIFIED" else 1)

if __name__ == "__main__":
    main()
