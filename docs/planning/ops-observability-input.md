# Ops And Observability Input

## Why This Is Not Yet A Session Set

The repo now has stronger app and workflow guardrails, but production-facing
operability work still needs sharper scope and environment decisions before it
should become a ready-to-run session spec.

## Likely Scope

- structured app and workflow telemetry
- failure triage around magic-link delivery, session verification, and E2E regressions
- dashboarding or alert guidance for build/test and runtime health
- cleanup of stage markers and environment breadcrumbs after the rebuild narrative fades

## Open Decisions

- which sinks are baseline versus optional
- whether local observability work should stay Docker-first
- which alerts should be exercised by automated tests versus documented runbooks
