"""CATALOG_STALE remediation — re-crawl the affected component and re-run.

When a locator that compiled successfully finds nothing at runtime,
it indicates the component catalog is stale relative to the live app.
This remediation:

  1. Invokes tools/ComponentCatalogCrawler/ to re-crawl the affected
     route × role combination.
  2. Updates docs/component-catalog/index.json with the new entry.
  3. Re-runs the originally-failing test by invoking the runner
     scoped to that step.
  4. If the re-run passes, records the auto-recovery in activity-log.json.
  5. If the re-run still fails, re-invokes triage (which may now
     classify differently).

On success, the TriageResult's outcome is "auto-recovered".
If re-triage is needed, that is recursively handled by the caller.
"""

from __future__ import annotations

import json
import subprocess
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[3]
CATALOG_PATH = REPO_ROOT / "docs/component-catalog/index.json"
COMPONENT_CRAWLER = REPO_ROOT / "tools/ComponentCatalogCrawler"


def apply(
    triage_result: object,
    runner_module: object = None,
) -> dict:
    """Apply CATALOG_STALE remediation and return outcome record.

    Args:
        triage_result: A TriageResult object with classification="CATALOG_STALE"
        runner_module: The ai_router.uat_runner module (for re-run access)

    Returns:
        A dict with keys: {type, stepNumber, classification, outcome, reason}
    """
    failure = triage_result.failure  # type: ignore
    step_number = failure.step_number

    # TODO: Session 3 will implement the full flow (crawler invocation, catalog update,
    #       test re-run, and recursive triage). For now, return a stub outcome
    #       that indicates the auto-recovery path was identified.
    return {
        "type": "uat-runner-failure",
        "stepNumber": step_number,
        "classification": "CATALOG_STALE",
        "autoApplied": True,
        "outcome": "auto-recovery-available",
        "reason": (
            "Catalog locator is stale; targeted re-crawl + re-run not yet "
            "implemented in Session 3 scaffolding."
        ),
    }
