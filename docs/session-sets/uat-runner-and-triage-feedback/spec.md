# UAT Runner and Triage Feedback Spec

> **Purpose:** Build a runner that executes UAT checklists against the
> live app via the W0 compiler — `dabbler-uat-dsl compile` produces a
> Roslyn-compilable C# Playwright test, which is run via `dotnet test`,
> with failures captured and routed through a Sonnet-driven triage stage
> that classifies and (where appropriate) auto-remediates them. This
> closes the validation loop that human UAT was previously doing alone.
> Together with W0 (compiler) and W1 (catalog), this means: authorship is
> grounded (W1), syntax and semantics are validated at compile time (W0),
> and runtime failures are classified and routed (W2).
> **Created:** 2026-04-28
> **Session Set:** `docs/session-sets/uat-runner-and-triage-feedback/`
> **Prerequisite:** `uat-dsl-compiler` (W0) complete (compiler + Playwright C#
> emitter + CLI shipped); `component-catalog-grounded-authorship` (W1)
> complete (`docs/component-catalog/index.json` present, validation
> integrated).
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Session Set Configuration

```yaml
requiresUAT: false
requiresE2E: false
uatScope: none
```

> Rationale: This set adds ai-router task types (triage + remediation
> prompts), a runner orchestrator script, and a small fixture-adapter
> implementation in dabbler-platform that exposes
> `IPlaywrightUatFixture` over the existing `PlaywrightFixture`. No
> Blazor UI surface, API endpoint, or user-facing behavior changes. Per
> the platform addendum, ai-router and tooling sets are
> `requiresUAT: false` / `requiresE2E: false`. The set's quality bar is:
> the runner executes the catalog-grounded remediation checklist
> end-to-end and either passes cleanly or files structured issue entries
> for human review.

---

## Project Overview

### What this set delivers

1. **`ai-router/uat_runner/`** — Python orchestrator that:
   - Invokes `dabbler-uat-dsl compile` on a target checklist with the
     catalog injected.
   - Writes the emitted C# to a temporary test file under
     `tests/AcmeCoffeeOnline.Playwright/Generated/`.
   - Runs `dotnet test` filtering to the generated test class.
   - Captures stdout/stderr/test-result XML.
   - On failure, calls the triage stage.

2. **`ai-router/uat_runner/triage.py`** — failure classifier. Calls
   Sonnet via `route(task_type="uat-failure-triage")` with a structured
   failure record. Classifies into one of four runtime classes (the
   compiler's compile-time gate eliminates the original W2-plan's
   translation and label/element-type-drift classes).

3. **`ai-router/uat_runner/remediations/`** — one Python module per
   class. Auto-apply for `CATALOG_STALE`. Human-review for
   `APP_FUNCTIONAL_BUG`, `SEED_DATA_GAP`, `AMBIGUOUS_LOCATOR`.

4. **`ai-router/prompts/uat_failure_triage.md`** and per-class
   remediation prompts.

5. **`ai-router/router-config.yaml` additions** — `uat-failure-triage`
   and per-class remediation task types with cost caps.

6. **`tests/AcmeCoffeeOnline.Playwright/Infrastructure/PlaywrightFixtureUatAdapter.cs`** —
   small adapter implementing `IPlaywrightUatFixture` from
   `Dabbler.Uat.Dsl.Emitters.Playwright.CSharp`. Delegates to the
   existing `PlaywrightFixture` methods.

7. **`docs/ai-led-session-workflow.md` update** — document the runner
   as an optional pre-human-UAT gate the orchestrator invokes when a
   set has `requiresUAT: true`.

8. **`docs/planning/project-guidance.md` addition** — runner usage as a
   Convention.

### What this set does not do

- Does not build a translation layer. The W0 compiler handles
  translation deterministically; LLMs are not involved in the
  compile-and-execute path.
- Does not replace human UAT. The runner is a pre-human gate.
- Does not auto-fix functional bugs. `APP_FUNCTIONAL_BUG` is always
  human-reviewed; this is a hard invariant.
- Does not extend the catalog (W1's job). The runner reads the catalog
  for triage but does not update it directly (it may trigger a
  re-crawl if classification is `CATALOG_STALE`).
- Does not change any Blazor component, page, or API endpoint.

---

## Design Commitments (Decided Up Front)

### Runner uses the W0 compiler — no LLM in the execution path

```
checklist.json
   │
   ▼
dabbler-uat-dsl compile --catalog index.json --output Generated.cs
   │
   ▼
dotnet test --filter FullyQualifiedName=...Generated
   │
   ▼
trx XML / stdout / stderr
   │
   ▼ (on failure only)
triage (Sonnet) → classify → remediation dispatch
```

Compile-time errors (label not in catalog, malformed step) are caught by
W0's `validate` subcommand before the runner ever invokes `dotnet test`.
Runtime failures (the test compiles and runs but an assertion fails)
are what triage handles.

### Failure taxonomy — four runtime classes (down from seven)

The original W2-plan's seven classes assumed an LLM-driven runner. With
the W0 compiler, three classes are eliminated:

- ~~`UAT_SCRIPT_LABEL_DRIFT`~~ — caught at compile time by the validator.
- ~~`UAT_SCRIPT_ELEMENT_TYPE_DRIFT`~~ — caught at compile time.
- ~~`NAIVE_RUNNER_TRANSLATION_ERROR`~~ — does not exist; no LLM translation.

Remaining runtime classes:

| Class | Auto-apply? | Description |
|---|---|---|
| `APP_FUNCTIONAL_BUG` | Never | A `VERIFY` failed: user-visible state doesn't match expected. The system under test has a bug. |
| `CATALOG_STALE` | Yes (re-crawl) | A locator that compiled successfully now finds nothing — the app changed since the catalog was generated. |
| `SEED_DATA_GAP` | No — human review | A step requires test data that wasn't seeded (e.g., "click row where customer = X" but X doesn't exist). |
| `AMBIGUOUS_LOCATOR` | No — human review | A locator matched multiple elements; needs a `data-testid` discriminator. |

**Invariant:** `APP_FUNCTIONAL_BUG` is never auto-fixed by modifying the
checklist. If triage is uncertain between `APP_FUNCTIONAL_BUG` and a
`CATALOG_STALE` case, it defaults to `APP_FUNCTIONAL_BUG`. Preserving
the bug-detection signal is more important than self-healing speed.

### Cost cap

Sonnet triage is capped at $0.20 per UAT execution (typical: 0–2
triage calls × ~$0.05 each — most runs have zero failures). Hard cap of
3 triage cycles per execution; beyond that, escalate the entire
remainder to human review.

### Failure record schema

```json
{
  "stepNumber": 13,
  "stepText": "VERIFY TEXT in \"customer-row\" matches \"XXX-XX-\\d{4}\"",
  "playwrightError": "Expected text matching pattern; got '123-45-6789'",
  "screenshotPath": "<path>",
  "domSnapshotPath": "<path>",
  "consoleErrors": [],
  "currentUrl": "/customers/123",
  "trxFile": "<path>"
}
```

The runner extracts these fields from the `dotnet test` `.trx` output
and the Playwright trace artifacts.

### Audit trail

Every triage cycle appends to `activity-log.json`:
```json
{
  "timestamp": "...",
  "type": "uat-runner-failure",
  "stepNumber": 13,
  "classification": "APP_FUNCTIONAL_BUG",
  "autoApplied": false,
  "issueFile": "docs/session-sets/{slug}/issues/13-bug.md",
  "outcome": "human-review-required"
}
```

### Fixture adapter is the only dabbler-platform code change

The W0 emitter generates code that calls `IPlaywrightUatFixture`. This
set adds `PlaywrightFixtureUatAdapter` to
`tests/AcmeCoffeeOnline.Playwright/Infrastructure/` that implements the
interface against the existing `PlaywrightFixture`. The adapter is
small (~30 lines) and is the only C# code added to dabbler-platform in
this set.

---

## Session Plan

### Session 1 of 4: Runner scaffold, fixture adapter, happy-path execution

**Steps:**

1. Read `docs/planning/project-guidance.md` and
   `docs/planning/lessons-learned.md`.
2. Confirm prerequisites:
   - `../dabbler-uat-dsl/` builds; CLI is available (either
     `dotnet pack` artifact installed as a global tool, or callable via
     `dotnet run --project ../dabbler-uat-dsl/src/Dabbler.Uat.Dsl.Cli/`).
   - `docs/component-catalog/index.json` exists.
   If not, halt and notify.
3. Add `tests/AcmeCoffeeOnline.Playwright/Infrastructure/PlaywrightFixtureUatAdapter.cs`:
   ```csharp
   internal sealed class PlaywrightFixtureUatAdapter : IPlaywrightUatFixture
   {
       private readonly PlaywrightFixture _inner;
       public PlaywrightFixtureUatAdapter(PlaywrightFixture inner) =>
           _inner = inner;

       public Task WaitForQuietAsync(IPage page) =>
           _inner.WaitForQuietAsync(page);  // no-op stub if W3 not yet shipped
       public Task WaitForReadyAsync(IPage page) =>
           _inner.WaitForReadyAsync(page);  // no-op stub if W3 not yet shipped
       public Task SeededSignInAsync(IPage page, string email,
           IReadOnlyList<string>? roles = null) =>
           _inner.SeededSignInAsync(page, email, roles);
   }
   ```
   Both `WaitForQuietAsync` and `WaitForReadyAsync` are stubbed as
   `Task.CompletedTask` until W3 wires the watermark. The `--no-watermark`
   compiler flag also avoids emitting these calls — pick one approach
   for v1; recommended is the stub adapter so the emitter can run with
   default settings.
4. Add a reference from the Playwright test project to the W0 packages:
   - `Dabbler.Uat.Dsl.Core`
   - `Dabbler.Uat.Dsl.Emitters.Playwright.CSharp`
   Via local `nuget.config` pointing at `../dabbler-uat-dsl/bin/Debug/`
   for v1 consumption. Document the consumption pattern in the test
   project README.
5. Create `ai-router/uat_runner/__init__.py`,
   `ai-router/uat_runner/runner.py`.
6. Implement `runner.py`:
   - Entry: `run_checklist(checklist_path, catalog_path, session_set_slug, base_url, auth_email)`.
   - Step 1: Invoke `dabbler-uat-dsl compile` (subprocess call). If
     compile fails (validation errors), report and exit 1 — these are
     authoring errors that should never reach the runner.
   - Step 2: Write the emitted C# to
     `tests/AcmeCoffeeOnline.Playwright/Generated/{slug}-uat.cs`.
   - Step 3: Invoke `dotnet test --filter FullyQualifiedName~{generated-class-name} --logger trx`.
   - Step 4: Parse the .trx and stdout to collect any failures.
   - Step 5: Return a `RunReport(passed=[], failed=[])`. If any failures,
     exit 1; else exit 0.
   - Triage is wired in Session 2.
7. Happy-path test: run the runner against the catalog-grounded
   `sensitive-field-masking-audit-uat-remediation` checklist (W1
   Session 2's output). All steps should pass IF the app is in a
   correct state. If any fail, those failures are real and need
   triage — but Session 1's job is just to confirm the runner
   plumbing works end-to-end.
8. `route(task_type="session-verification")` covering runner code +
   the adapter + a sample `RunReport`.
9. Commit. Commit message:
   `Add UAT runner scaffold, Playwright fixture adapter, happy-path execution`.

**Creates:** `tests/AcmeCoffeeOnline.Playwright/Infrastructure/PlaywrightFixtureUatAdapter.cs`, `ai-router/uat_runner/__init__.py`, `ai-router/uat_runner/runner.py`, `tests/AcmeCoffeeOnline.Playwright/Generated/.gitignore`.
**Touches:** `tests/AcmeCoffeeOnline.Playwright/AcmeCoffeeOnline.Playwright.csproj` (add W0 package references), `nuget.config` (point at local pack output for v1).
**Ends with:** `python -m ai_router.uat_runner.runner --checklist <path> --catalog <path>` runs end-to-end and produces a `RunReport`.
**Progress keys:** `session-1/read-planning`, `session-1/prereq-check`, `session-1/fixture-adapter`, `session-1/package-references`, `session-1/runner`, `session-1/compile-step`, `session-1/test-invocation`, `session-1/trx-parsing`, `session-1/happy-path`, `session-1/verification`, `session-1/commit`.

---

### Session 2 of 4: Triage stage and four-class taxonomy

**Steps:**

1. Read planning docs.
2. Create `ai-router/uat_runner/triage.py`:
   - Entry: `classify_failure(failure_record) -> TriageResult`.
   - Calls `route(task_type="uat-failure-triage", prompt=<failure_record_json>)`.
   - The model returns: `{classification, confidence, evidence, proposedFix?}`.
   - Returns a `TriageResult`.
3. Write `ai-router/prompts/uat_failure_triage.md`:
   - System: explains the four runtime classes, the auto-apply rules,
     and the `APP_FUNCTIONAL_BUG` default. Explicit instruction:
     "If a `VERIFY` step's user-visible expectation does not match the
     observed state (e.g., 'SSN should appear as XXX-XX-####' but
     observed text is '123-45-6789'), classify as
     `APP_FUNCTIONAL_BUG` regardless of whether locators are also
     suspect."
   - Injects: the failure record JSON + the catalog entry for the
     inferred route (so the model can spot CATALOG_STALE drift).
4. Add `uat-failure-triage` to `ai-router/router-config.yaml`:
   ```yaml
   uat-failure-triage:
     model: sonnet
     effort: medium
     prompt_template: prompts/uat_failure_triage.md
     cost_caps:
       max_calls_per_run: 3
       max_cost_usd_per_run: 0.20
   ```
5. Wire triage into `runner.py`: after each test failure, call
   `classify_failure(failure_record)` and store the `TriageResult`.
6. Build synthetic failure cases:
   - Inject `APP_FUNCTIONAL_BUG`: modify a seed value to violate a
     `VERIFY TEXT` assertion; run the runner; assert correct
     classification.
   - Inject `CATALOG_STALE`: rename a `data-testid` in the source so
     the catalog's locator misses; run the runner; assert correct
     classification.
   - Inject `SEED_DATA_GAP`: reference an unseeded entity in the
     checklist; run the runner; assert correct classification.
   - Inject `AMBIGUOUS_LOCATOR`: add a duplicate label to a page; run
     the runner; assert correct classification.
   These are scripted manual tests under
   `ai-router/uat_runner/test_cases/`, run by hand during the session.
7. `route(task_type="session-verification")`.
8. Commit. Commit message:
   `Add UAT failure triage stage with four-class taxonomy`.

**Creates:** `ai-router/uat_runner/triage.py`, `ai-router/prompts/uat_failure_triage.md`, `ai-router/uat_runner/test_cases/` (synthetic failure scripts).
**Touches:** `ai-router/uat_runner/runner.py`, `ai-router/router-config.yaml`.
**Ends with:** Triage correctly classifies each of the four synthetic failure cases.
**Progress keys:** `session-2/read-planning`, `session-2/triage`, `session-2/prompt`, `session-2/router-config`, `session-2/runner-wire`, `session-2/test-cases`, `session-2/verification`, `session-2/commit`.

---

### Session 3 of 4: Remediations (auto-apply for catalog-stale; human-review for the rest)

**Steps:**

1. Read planning docs.
2. Create `ai-router/uat_runner/remediations/__init__.py`.
3. Implement `catalog_stale.py`:
   - Triggers a targeted re-crawl by invoking
     `tools/ComponentCatalogCrawler/` (subprocess) with the affected
     route × role.
   - Updates `docs/component-catalog/index.json`.
   - Re-runs the originally-failing test by re-invoking the runner
     scoped to that single step's checklist file. If the failure
     persists, escalates to triage again (which may now classify
     differently).
4. Implement `app_functional_bug.py`:
   - Creates `docs/session-sets/{slug}/issues/{step-number}-bug.md`
     with structured content:
     ```markdown
     ## UAT Step {n} — Functional Bug

     **Step text:** {step_text}
     **Expected:** {expected from the VERIFY clause}
     **Observed:** {observed text / screenshot reference}
     **URL:** {currentUrl}
     **Screenshot:** {screenshotPath}
     **Triage confidence:** {confidence}
     **Triage evidence:** {evidence}
     **Requires:** Human review and code fix before UAT can proceed.
     ```
   - Does NOT modify the checklist. Does NOT propose a code fix.
5. Implement `seed_data_gap.py`:
   - Creates a structured gap entry under `issues/`.
   - Documents what data is needed, which route/role requires it, and
     a suggested EF seed addition.
6. Implement `ambiguous_locator.py`:
   - Creates a structured disambiguation entry proposing a
     `data-testid` addition on the affected component.
   - References the inferred `.razor` file and element from the DOM
     snapshot.
7. Wire all four into `triage.py` dispatch:
   ```python
   match result.classification:
       case "CATALOG_STALE":
           remediations.catalog_stale.apply(result, ...)
       case "APP_FUNCTIONAL_BUG":
           remediations.app_functional_bug.file_issue(result, ...)
       case "SEED_DATA_GAP":
           remediations.seed_data_gap.file_issue(result, ...)
       case "AMBIGUOUS_LOCATOR":
           remediations.ambiguous_locator.file_issue(result, ...)
   ```
8. Append remediation outcomes to `activity-log.json`:
   `{type: "uat-runner-failure", classification: ..., issueFile: ..., outcome: ...}`.
9. End-to-end test: run the four synthetic failure cases through the
   full pipeline. Confirm that:
   - `CATALOG_STALE` auto-recovers (re-crawl + re-run passes).
   - The other three each produce an issue file at the correct path
     with correct content.
10. `route(task_type="session-verification")`.
11. Commit. Commit message:
    `Add remediations: catalog-stale auto-apply + human-review issue filing`.

**Creates:** `ai-router/uat_runner/remediations/__init__.py`, `catalog_stale.py`, `app_functional_bug.py`, `seed_data_gap.py`, `ambiguous_locator.py`, issue-file templates.
**Touches:** `ai-router/uat_runner/triage.py`, `ai-router/uat_runner/runner.py`.
**Ends with:** Each of the four classes correctly dispatches to its remediation.
**Progress keys:** `session-3/read-planning`, `session-3/catalog-stale`, `session-3/bug-filing`, `session-3/seed-gap`, `session-3/ambiguous-locator`, `session-3/dispatch-wire`, `session-3/activity-log`, `session-3/e2e-test`, `session-3/verification`, `session-3/commit`.

---

### Session 4 of 4: Workflow integration, backfill, change log, Step 9 reorg

**Steps:**

1. Read planning docs.
2. Create `ai-router/uat_runner/cli.py`:
   ```bash
   python -m ai_router.uat_runner.cli \
     --checklist docs/session-sets/{slug}/{slug}-uat-checklist.json \
     --catalog docs/component-catalog/index.json \
     --session-set {slug} \
     --base-url http://localhost:5010 \
     --auth-email admin@acmecoffee.com
   ```
   Exits 0 if all steps pass or only `CATALOG_STALE` failures
   auto-recovered; exits 1 if any human-review issues filed.
3. Update `docs/ai-led-session-workflow.md` Step 5 (UAT generation):
   add a sub-step: "If the set has `requiresUAT: true`, after
   generating the catalog-grounded checklist, invoke the UAT runner
   via the CLI. If the runner exits 1, resolve filed issues before
   notifying the human." Conditional on the runner being available.
4. Update `docs/planning/project-guidance.md`: document the runner as
   a Convention for `requiresUAT: true` sets.
5. Backfill: run the runner against the
   `sensitive-field-masking-audit-uat-remediation` catalog-grounded
   checklist (the W1 output). This is the first real-world end-to-end
   proof. Expected outcomes:
   - Steps 2/9 (label drift) — **already eliminated** by W0/W1.
     They never reach the runner.
   - Steps 13/15/16 — should produce `APP_FUNCTIONAL_BUG` issue
     files since these were real bugs in the masking implementation
     that surfaced in the original UAT.
   - Other steps — pass.
   The result confirms the pipeline catches what manual UAT did.
6. For each filed issue, the human reviews and decides next steps
   (a follow-up code fix set, or a `[Skip]` with rationale on the
   underlying step). Issue resolution is out of scope for this set;
   the deliverable is that the issues are filed accurately and
   actionably.
7. Generate
   `docs/session-sets/uat-runner-and-triage-feedback/change-log.md`.
8. Full build:
   ```bash
   dotnet build Dabbler.Platform.sln
   dotnet test Dabbler.Platform.sln
   ```
9. Cross-provider verification.
10. Commit. Commit message:
    `Wire UAT runner into workflow + backfill remediation set`.
11. Send `send_session_complete_notification()`. Step 9 reorg review:
    - Promotion candidate: `ai-router/uat_runner/` is a strong
      candidate for `../dabbler-ai-orchestration` once a second
      consumer exists (a sibling Dabbler app or a non-Dabbler project
      using the W0 library).
    - Lessons-learned candidate: "Compile-time validation collapses
      seven runtime failure classes to four. Investing in a
      formal grammar (W0) pays for itself in the runner's
      simplicity (this set)."
    - Promotion candidate: `docs/planning/uat-failure-taxonomy.md` —
      the four-class taxonomy is generic; promote to orchestration
      repo.

**Creates:** `ai-router/uat_runner/cli.py`, `change-log.md`, `docs/planning/uat-failure-taxonomy.md` (if Step 9 accepts).
**Touches:** `docs/ai-led-session-workflow.md`, `docs/planning/project-guidance.md`, `docs/planning/lessons-learned.md` (if Step 9 accepts).
**Ends with:** Set complete. The runner runs end-to-end against a real checklist; issues are filed for the masking-set's known bugs; human is notified.
**Progress keys:** `session-4/read-planning`, `session-4/cli`, `session-4/workflow-update`, `session-4/guidance-update`, `session-4/backfill-run`, `session-4/issue-review`, `session-4/change-log`, `session-4/build-and-test`, `session-4/verification`, `session-4/commit-push-notify`, `session-4/step-9-reorg`.

---

## What This Set Preserves

| Existing Asset | Status |
|---|---|
| `Dabbler.Uat.Dsl` library and grammar | Unchanged; consumed via W0's published packages |
| `docs/component-catalog/index.json` | May be updated by `catalog_stale.py`; always commits cleanly |
| All existing ai-router task types | Unchanged; new task types appended |
| `docs/ai-led-session-workflow.md` step structure | Unchanged; one conditional clause added to Step 5 |
| `tests/AcmeCoffeeOnline.Playwright/Infrastructure/PlaywrightFixture.cs` | Unchanged; new adapter wraps it |
| All existing Playwright .NET tests | Unchanged; new generated tests run alongside via `dotnet test` |

## Risk Notes

- **Generated test files in source.** The runner writes emitted C# under
  `tests/AcmeCoffeeOnline.Playwright/Generated/`. The `.gitignore` in
  that directory excludes them. They are regenerated on every runner
  invocation. If a test author wants to inspect emitted output, they
  can run the W0 CLI directly with `--output -` to stdout.

- **`APP_FUNCTIONAL_BUG` vs `CATALOG_STALE` ambiguity.** A failure
  pattern that looks like locator drift may actually be a real bug
  manifesting as a missing element. The triage default (lean toward
  `APP_FUNCTIONAL_BUG`) means some catalog-stale cases land in human
  review. Acceptable: false positives (filing a non-bug as a bug) are
  far less harmful than false negatives.

- **Cost overrun on pathological runs.** A checklist with many failing
  steps could trigger up to 3 triage cycles before the cap stops the
  run. At ~$0.05 per call that's ~$0.15 — within the cap. The runner
  warns the human if failure count exceeds 5 and pauses for
  confirmation before triaging all of them.

- **W0 CLI consumption mechanism.** v1 uses `dotnet run --project
  ../dabbler-uat-dsl/...` or a local `dotnet pack` install. If the W0
  library is not packaged at runner-execution time, the runner errors
  with a clear "W0 library not built" message and exits.

- **`dotnet test --filter` reliability.** The runner depends on
  filtering to a generated test class via fully-qualified name. If the
  emitted class name collides with an existing test, the filter is
  ambiguous. Mitigation: emitter generates a unique class name based
  on checklist filename + a hash; documented in the W0 spec.

- **Auth secret in plaintext.** The runner uses
  `X-Test-Auth-Secret: "acme-local-seeded-session"` — same as the
  existing Playwright fixture. Dev/test-only credential; acceptable
  per the existing project convention.
