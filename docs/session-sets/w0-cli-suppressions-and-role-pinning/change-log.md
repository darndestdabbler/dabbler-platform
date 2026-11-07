## Changelog for w0-cli-suppressions-and-role-pinning

This session set introduces the ability to suppress UAT errors directly within the W0 CLI, adds persona-based role contextualization to UAT validation, and finally validates the watermark integration end-to-end with a deterministic 5/5 passing smoke test. The watermark integration is now fully proven at runtime.

### Session 1

**What changed:** The W0 CLI now supports `--suppressions <path>` and `-s` flags for `validate` and `compile` subcommands. A new `SuppressionMatcher` class processes `tools/uat-suppressions.json` to downgrade matched errors to warnings. Compile exits 0 with warnings, and validate returns `valid:true` with warnings. Nine new unit tests were added.

**Artifact paths touched:**
- `Dabbler.Uat.Dsl.Cli` (NuGet package)
- `Dabbler.Uat.Dsl.Core` (NuGet package)
- `dabbler-uat-dsl` (repo)
- `tools/uat-suppressions.json` (schema honored)

**Measurable outcome:** W0 CLI 1.2.0 released with error suppression capabilities. Verified with a PASS (15/15 acceptance criteria met) on opus, confirming suppression functionality.

### Session 2

**What changed:** The UAT runner now infers the active role for validation based on email patterns in checklists, instead of relying on an `AS ROLE` directive. `ai-router/uat_runner/personas.json` and `ai-router/uat_runner/personas.py` were introduced for email-to-role mapping and role inference. The W0 CLI 1.2.1 was released, adding the `--active-role` / `-r` flag to `validate` and `compile`, and threading the active role into `SemanticValidator.Validate`. Seven new tests were added across .NET and Python.

**Artifact paths touched:**
- `ai-router/uat_runner/personas.json`
- `ai-router/uat_runner/personas.py`
- `ai-router/uat_runner/runner.py`
- `Dabbler.Uat.Dsl.Cli` (NuGet package)

**Measurable outcome:** W0 CLI 1.2.1 released with active role inference. Verified with a PASS on opus, with human dismissal of a verifier false positive.

### Session 3

**What changed:** The `Dabbler.Uat.Dsl.Core` NuGet dependency was bumped to 1.2.0. The component catalog was re-crawled, confirming `/admin/products [watermarked]` for all five roles. A new smoke checklist for `/admin/products` was authored via `uat-script-author`, which exposed and resolved three DSL/W0 gaps related to sign-in flow. The runner's emitted test wrapper now uses `PlaywrightFixture.SeededSignInAsync` for sign-in setup. The smoke checklist was simplified to a `/admin/products`-only flow and validates clean with `--active-role SystemAdmin` and `--suppressions tools/uat-suppressions.json`. The runner's `_invoke_w0_compile` now natively passes `--suppressions` to the W0 CLI, removing the legacy post-compile suppression-matching block.

**Artifact paths touched:**
- `Directory.Packages.props`
- `docs/component-catalog/index.json`
- `ai-router/uat_runner/runner.py`
- `docs/session-sets/.../smoke/run-5x-smoke.py`
- `docs/session-sets/.../smoke/compiled/W0SuppressionsAndRolePinningSmoke.cs`

**Measurable outcome:** The deferred 5/5 watermark smoke proof from `uat-dsl-watermark-integration` is now green end-to-end. Deterministic 5/5 PASS observed across iterations via the smoke driver, proving watermark integration at runtime. A known W0 emitter issue (ClickSwitchInColumnStep lowering to Columnheader click) is tracked for future resolution but does not prevent current watermark path exercising.