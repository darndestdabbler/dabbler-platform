"""UAT runner orchestrator.

Wires the W0 (`dabbler-uat-dsl`) compiler to `dotnet test`, parses
the resulting .trx report into a structured RunReport, and classifies
failures via the four-class triage stage (Session 2).
"""

from .runner import RunReport, StepFailure, run_checklist
from .triage import (
    Classification,
    FailureRecord,
    TriageResult,
    TriageSession,
    classify_failure,
)

__all__ = [
    "RunReport",
    "StepFailure",
    "run_checklist",
    "Classification",
    "FailureRecord",
    "TriageResult",
    "TriageSession",
    "classify_failure",
]
