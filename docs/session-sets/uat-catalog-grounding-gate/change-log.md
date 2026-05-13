# Change Log — uat-catalog-grounding-gate

## What changed

Added a **non-suppressible pre-flight gate** to the W2 UAT runner that prevents
AI orchestrators from authoring checklist steps with fake labels on routes that
exist in the component catalog. Before this set, the W0 compiler emitted
`UnknownLabel` errors but they could be downgraded to warnings via
`routePathPatterns: ["*"]` in `tools/uat-suppressions.json`. The
`--skip-dotnet-test` shortcut then hid the fact that the compiled Playwright
test referenced nonexistent UI elements. The human only discovered the cheating
during manual UAT.

## How the gate works

`uat_runner/runner.py` runs `_check_catalog_grounding()` before invoking the W0
compiler. It parses each checklist step's DSL action, tracks the active route via
`NAVIGATE TO` steps, and extracts the label argument from interaction verbs
(`CLICK BUTTON`, `CLICK TAB`, `CLICK INPUT`, `CLICK LINK`, etc.). Two-tier
classification:

| Route status | Label status | Result |
|---|---|---|
| In catalog | In catalog | Pass |
| In catalog | Not in catalog | **`CATALOG_GROUNDED_VIOLATION` — hard failure, cannot be suppressed** |
| Not in catalog | (any) | `CATALOG_STALE` — existing suppression path, telemetry line emitted |

The violation report names the route, the bogus label, the offending step, and the
first 8 catalog labels available on that route — so the fix is obvious.

## Proof the gate works

Running the gate against the original broken `unified-master-details-composite`
checklist surfaced 12 violations:

- `/admin/security`: `ROLES` (catalog has `ROLE MASTER DETAILS`), `first role row in the grid`,
  `Add` (catalog has `ADD USER-TYPE ROLE`), `Role Name`
- `/orders`: `first order with items`, `Inspect line`, `Edit record`, `Product Name`,
  `Save and Use`

The `admin-users-cross-links` checklist (known good): clean pass, no violations.

## Scenario 2 fixed end-to-end

Session 2 re-authored the broken Scenario 2 items using only catalog-grounded labels
and wrote `tests/AcmeCoffeeOnline.Playwright/RoleMasterDetailsRegressionTests.cs`
with three `[Fact]` tests that pass green against the Docker stack:

- **2a** (`RoleMasterDetails_SelectRow_CollapsesShellAsync`): Click the
  BackofficeUser row → shell collapses → summary shows "BackofficeUser".
- **2b** (`RoleMasterDetails_AddUserTypeRoleButton_OpensDialogAsync`): Click the
  catalog-verified `ADD USER-TYPE ROLE` button → `AddUserTypeRoleDialog` opens with
  its five inputs/buttons visible.
- **2c** (`RoleMasterDetails_EditDisplayAlias_PersistsAsync`): Edit the
  BackofficeUser display alias inline → snackbar confirms → reload page → new
  alias persists. The test restores the original alias in `finally`.

The four 4b-toggle judgment items were consolidated into one manual-UAT item; their
DSL steps referenced dialog-internal labels not in the catalog and would have
failed the gate even though they were marked `IsJudgmentItem`.

## Policy update

`docs/planning/project-guidance.md` → UAT Runner (W2) now documents:

- `CATALOG_GROUNDED_VIOLATION` is not suppressible.
- Orchestrators preparing `route(task_type="uat-plan-generation", ...)` prompts
  must include the catalog (or a filtered slice) and instruct the model to use
  only labels appearing verbatim as `userVisibleLabel` in catalog entries for the
  active route × role.
- If a needed label is missing from the catalog, the correct response is to
  re-crawl or mark the item `IsJudgmentItem: true` — not fabricate.

## Known limitations / follow-up

- **`CLICK ROW` DSL verb has no grid-name target.** The role grid (and most app
  grids) are not crawled as a distinct `Grid` catalog entry. The DSL grammar
  supports `CLICK ROW in "<grid name>" where ...` but there is no catalog
  user-visible label to plug in. Two paths forward: (a) extend the crawler to
  emit a `Grid` element-role entry with the testId-derived name; or (b) extend
  the DSL with a row-by-testid verb. Tracked as a future enhancement.
- **Pre-auth routes (`/login`, `/dev/email-inbox`) are never in the catalog.**
  These remain in the CATALOG_STALE path with rationale entries in
  `tools/uat-suppressions.json` — unchanged by this set.
- **The W0 compiler still does its own validation in parallel.** The pre-flight
  gate runs before W0; W0 still emits its own errors after. The two layers are
  not redundant — W0 catches grammar/syntactic errors; the gate catches the
  specific class of "label not in catalog on a known route" that W0 honors
  suppressions for.
