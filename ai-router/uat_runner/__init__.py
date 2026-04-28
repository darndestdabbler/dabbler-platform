"""UAT runner orchestrator.

Wires the W0 (`dabbler-uat-dsl`) compiler to `dotnet test` and parses
the resulting .trx report into a structured RunReport. Triage is wired
in Session 2 of the `uat-runner-and-triage-feedback` set; this module
ships the scaffolding (compile + emit + run + parse) only.
"""

from .runner import RunReport, StepFailure, run_checklist

__all__ = ["RunReport", "StepFailure", "run_checklist"]
