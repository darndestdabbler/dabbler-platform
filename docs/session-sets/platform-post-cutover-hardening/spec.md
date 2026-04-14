# Platform Post-Cutover Hardening Spec

> **Purpose:** Tighten the rebuilt platform's operational reliability after the
> consolidation cutover without reopening the migration itself.
> **Created:** 2026-04-14
> **Session Set:** `docs/session-sets/platform-post-cutover-hardening/`
> **Prerequisite:** `rebuild-consolidation` is complete and the integrated repo
> is the canonical home.
> **Workflow:** Orchestrator -> AI Router -> Cross-provider verification

---

## Project Overview

This session set focuses on the first stability pass after cutover: remove
rebuild-era breadcrumbs, strengthen observability, and harden the highest-risk
automation and startup paths.

## Session Plan

### Session 1 of 3: Health And Diagnostics Cleanup

**Steps:**
1. Replace rebuild-era stage breadcrumbs with durable environment/version diagnostics.
2. Document the supported health payload contract for app and API services.
3. Add tests that fail if those diagnostics drift unexpectedly.

**Creates:** Durable health-contract guidance
**Touches:** App/API health endpoints and tests
**Ends with:** Environment diagnostics are stable and no longer session-specific
**Progress keys:** `session-1/health-contract`, `session-1/diagnostic-cleanup`, `session-1/health-tests`

---

### Session 2 of 3: Workflow And Auth Observability

**Steps:**
1. Add structured logging around magic-link capture, seeded-session use, and session verification runs.
2. Document how local and CI-like runs should inspect those signals.
3. Add smoke checks that fail if critical workflow/auth diagnostics disappear.

**Creates:** Observability guidance and smoke checks
**Touches:** App auth helpers, workflow assets, docs, and tests
**Ends with:** Operators and future agents can quickly diagnose auth/workflow failures
**Progress keys:** `session-2/auth-observability`, `session-2/workflow-observability`, `session-2/smoke-checks`

---

### Session 3 of 3: Flake Resistance And Cleanup

**Steps:**
1. Review the current Playwright and startup flake points.
2. Harden fixture/process startup behavior and reduce avoidable nondeterminism.
3. Capture the resulting stable guidance for future session sets.

**Creates:** Flake-reduction notes and updated guidance
**Touches:** Playwright infrastructure, startup helpers, docs
**Ends with:** The rebuilt repo has a cleaner, less session-specific reliability baseline
**Progress keys:** `session-3/flake-review`, `session-3/startup-hardening`, `session-3/stable-guidance`
