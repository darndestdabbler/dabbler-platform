"""Remediation dispatch for UAT failure triage results.

Each classification (APP_FUNCTIONAL_BUG, CATALOG_STALE, SEED_DATA_GAP,
AMBIGUOUS_LOCATOR) routes to a dedicated remediation module that either:

  - AUTO-APPLIES a fix and re-runs the test (CATALOG_STALE only)
  - FILES A HUMAN-REVIEW ISSUE (all others)

All remediations append structured records to activity-log.json for audit.
"""

from __future__ import annotations

from . import (
    ambiguous_locator,
    app_functional_bug,
    catalog_stale,
    seed_data_gap,
)

__all__ = [
    "app_functional_bug",
    "catalog_stale",
    "seed_data_gap",
    "ambiguous_locator",
]
