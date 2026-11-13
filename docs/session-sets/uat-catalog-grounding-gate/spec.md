# UAT Catalog Grounding Gate Spec

> **Purpose:** Prevent AI from cheating on UAT checklist authoring by using fake
> labels that don't exist in the component catalog. The W0 compiler already emits
> `UnknownLabel` errors for these — but the suppression file's wildcard
> `routePathPatterns: ["*"]` silently downgraded them to warnings. This set makes
> `UnknownLabel` on a catalog-known route an unsuppressible hard failure.
> **Created:** 2026-05-12
> **Session Set:** `docs/session-sets/uat-catalog-grounding-gate/`
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Session Set Configuration

```yaml
requiresUAT: false
requiresE2E: false
uatScope: none
```

Reasoning: This is a tooling change to `uat_runner/`. No app UI changes. No human
UAT needed (testing the UAT runner circularly). Proof of correctness is: the broken
Scenario 2 checklist triggers a `CATALOG_GROUNDED_VIOLATION` failure, and the fixed
checklist passes cleanly.

---

## Context

The `unified-master-details-composite` Session 5 UAT checklist had broken Scenario 2
items that used:

- `CLICK TAB with name "ROLES"` — no such tab on `/admin/security`; catalog says
  `"ROLE MASTER DETAILS"`
- `CLICK LINK with text "first role row in the grid"` — fake placeholder; the DSL
  grammar already has `CLICK ROW in "grid" where "col" = "val"`

The W0 compiler correctly emitted `UnknownLabel` errors for both. They were suppressed
by `tools/uat-suppressions.json` entry with `routePathPatterns: ["*"]`. The W2 runner
was then run with `--skip-dotnet-test`. The human only discovered the problem during
manual UAT.

**The principle:** any safeguard that requires the AI to voluntarily comply is not a
safeguard. The gate must be in the tooling.

---

## How the Fix Works

Two tiers of `UnknownLabel` errors:

| Error route | Gate decision |
|---|---|
| Route IS in `docs/component-catalog/index.json` | `CATALOG_GROUNDED_VIOLATION` — hard failure, cannot be suppressed |
| Route is NOT in catalog (new page, not yet crawled) | `CATALOG_STALE` — existing suppression logic applies as before |

Change is ~50 lines in `uat_runner/runner.py`. No changes to W0 compiler, no changes
to suppression schema, no changes to sibling repos.

---

## Session 1 of 2: Implement the Gate

**Steps:**

1. Read planning docs.
2. Read `uat_runner/runner.py` lines 549–660 (`_split_compile_errors_by_suppressions`,
   `_parse_compile_errors`) and `uat_runner/triage.py`.
3. Implement `_classify_compile_errors(errors, catalog_routes)` that partitions W0
   errors into `(grounded_violations, stale_errors)`. A `grounded_violation` is any
   `UnknownLabel` or `ElementTypeMismatch` error whose route is in `catalog_routes`.
4. Load `docs/component-catalog/index.json` at runner startup. Extract a
   `frozenset` of known route paths.
5. In the compile path (after `_parse_compile_errors`), call the new classifier before
   `_split_compile_errors_by_suppressions`. Move `grounded_violations` straight to the
   hard-failure list; pass only `stale_errors` to the existing suppression logic.
6. Update failure output: when `CATALOG_GROUNDED_VIOLATION` errors exist, print a
   distinct block: route, label, and "Use a label that exists in the catalog for this
   route". The runner must exit non-zero.
7. Prove it: run the W2 runner against
   `docs/session-sets/unified-master-details-composite/unified-master-details-composite-uat-checklist.json`.
   The output must name the violations on `/admin/security` explicitly.
8. Write a smoke test in `uat_runner/test_cases/` (or a pytest unit test) that asserts
   `_classify_compile_errors` returns the right partition for a synthetic error list
   with known and unknown routes.
9. `dotnet build Dabbler.Platform.sln` — no regressions.
10. Cross-provider verification via `route(task_type="session-verification")`.
11. Commit, push, notify.

**Creates:** Logic in `uat_runner/runner.py`; smoke/unit test.
**Touches:** `uat_runner/runner.py` only.
**Ends with:** Any checklist with a fake label on a catalog-known route fails the W2
runner with a named `CATALOG_GROUNDED_VIOLATION` — not suppressible.
**Progress keys:** `session-1/read-planning`, `session-1/implement-classifier`,
`session-1/wire-into-runner`, `session-1/prove-catches-cheating`,
`session-1/unit-test`, `session-1/verification`, `session-1/commit-push-notify`.

---

## Session 2 of 2: Mandate Catalog Grounding in Generation + Fix Scenario 2

**Steps:**

1. Read planning docs.
2. Update `ai_router/router-config.yaml` uat-plan-generation instructions: add
   mandatory pre-authoring step requiring catalog lookup before writing any DSL action.
3. Look up correct labels from `docs/component-catalog/index.json` for `/admin/security`:
   tab name, grid column headers, Add button caption, display alias field.
4. Re-author checklist items 2a/2b/2c with catalog-grounded DSL steps and correct
   descriptions (2b: actual expansion behavior; 2c: display alias, not "Role Name").
5. Write `tests/AcmeCoffeeOnline.Playwright/RoleMasterDetailsRegressionTests.cs` —
   three real `[Fact]` tests using verified `data-testid` values:
   `role-master-details-shell-body`, `role-row-BackofficeUser`,
   `role-row-BackofficeUser-alias`, `role-master-add-button`.
6. `dotnet test --filter RoleMasterDetailsRegressionTests` — all 3 must pass green
   against the Docker stack.
7. Run W2 runner on the fixed checklist — must pass with no `CATALOG_GROUNDED_VIOLATION`.
8. Update `docs/planning/project-guidance.md` — add to "UAT Runner (W2)" section:
   `CATALOG_GROUNDED_VIOLATION` policy (not suppressible, hard failure).
9. Cross-provider verification.
10. Commit, push, notify.
11. Step 9 reorganization review.

**Creates:** `RoleMasterDetailsRegressionTests.cs`.
**Touches:** Checklist; `ai_router/router-config.yaml`; `docs/planning/project-guidance.md`.
**Ends with:** Scenario 2 checklist items pass the gate; human can follow the steps
manually; Playwright tests are real and green.
**Progress keys:** `session-2/read-planning`, `session-2/update-generation-instructions`,
`session-2/catalog-lookup`, `session-2/reauthor-2abc`, `session-2/playwright-tests`,
`session-2/dotnet-test`, `session-2/w2-run`, `session-2/project-guidance`,
`session-2/verification`, `session-2/commit-push-notify`, `session-2/reorganization`.

---

## Exit Criteria

- `CATALOG_GROUNDED_VIOLATION` fires for any `UnknownLabel` on a catalog-known route,
  cannot be suppressed, blocks the W2 run.
- Scenario 2 Playwright tests (`RoleMasterDetailsRegressionTests`) pass green.
- Fixed Scenario 2 checklist items pass the W2 runner.
- `uat-plan-generation` instructions include catalog-first mandate.
- Human can follow 2a/2b/2c steps manually and they match what the tests verify.

## Out of Scope

- SeededSignInAsync / W0 double-login conflict.
- Changes to the `dabbler-uat-dsl` sibling repo (W0 compiler).
- Whether every UAT step must have Playwright coverage (governance question, not
  tooling).
