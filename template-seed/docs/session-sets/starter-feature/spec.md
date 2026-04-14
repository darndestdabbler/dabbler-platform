# {{PROJECT_NAME}} Starter Spec

> **Purpose:** Bootstrap the first guarded workflow session set for {{APP_NAME}}
> **Created:** 2026-04-14
> **Session Set:** `docs/session-sets/{{SESSION_SET_NAME}}/`
> **Prerequisite:** Repo skeleton exists and the root instruction files point to valid paths
> **Workflow:** Orchestrator -> AI Router -> Cross-provider verification

---

## Project Overview

Describe the first body of work for the new repo here.

## Session Plan

### Session 1 of 2: Baseline And Guardrails

**Steps:**
1. Capture the current baseline and intended behavior.
2. Document the first guardrails and test expectations.
3. Create the initial parity or acceptance checklist.

**Creates:** Baseline and guardrail docs
**Touches:** `docs/`
**Ends with:** Future sessions have a clear source of truth
**Progress keys:** `session-1/baseline`, `session-1/guardrails`, `session-1/checklist`

---

### Session 2 of 2: First Implementation Slice

**Steps:**
1. Implement the first feature slice.
2. Add the tests that prove it.
3. Verify the session externally and close out the set.

**Creates:** Feature files and tests
**Touches:** App/library/test code
**Ends with:** The repo has one fully verified working slice
**Progress keys:** `session-2/implementation`, `session-2/tests`, `session-2/verification`
