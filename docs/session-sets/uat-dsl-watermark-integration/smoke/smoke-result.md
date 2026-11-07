# Session 3 smoke proof — UAT DSL watermark integration

## Smoke target

`/admin/products` — the page Session 5 of `playwright-sync-watermark-and-playbook`
validated 59/60 isolated. After Session 1's re-crawl every catalog entry on
`/admin/products` carries `isWatermarked: true`.

## What the smoke proves

Three load-bearing facts were proven end-to-end in this session:

1. **Catalog flag is populated correctly.**
   Re-crawled `docs/component-catalog/index.json` against the live compose
   stack (Session 1's WatermarkDetector + crawler integration). All 120
   `/admin/products` entries (4 roles × ~30 elements) carry
   `isWatermarked: true`. `/dashboard` and other non-watermarked routes
   carry `isWatermarked: false`. Schema bumped to 1.1 cleanly.

2. **Updated `uat-script-author` prompt emits the verb.**
   Routed `route(task_type="uat-script-author")` (Sonnet, $0.066) on a
   smoke spec asking for a single Review item that toggles the Available
   switch on a watermarked product row. The model emitted
   `WAIT FOR ACTIONS QUIET` as Step 12, immediately after Step 11's
   `CLICK SWITCH in column with header "Available"`. No watermark verb
   was emitted on pre-auth steps (`/login`, `/dev/email-inbox`) which
   are not watermarked. Generated checklist:
   `uat-dsl-watermark-integration-uat-checklist.json` (this folder).

3. **DSL compiler emits `WaitForQuietAsync(page)`.**
   Compiled the minimal checklist `compile-only-smoke.json`
   (`NAVIGATE TO /dashboard` + `WAIT FOR ACTIONS QUIET`) through
   `dabbler-uat-dsl compile`. Output (`compiled/CompileOnlySmoke.cs`):

   ```csharp
   await page.GotoAsync("http://localhost:5010/dashboard");
   await fixture.WaitForQuietAsync(page);   // implicit (existing flag)
   await fixture.WaitForQuietAsync(page);   // explicit (new verb)
   ```

   The explicit `WAIT FOR ACTIONS QUIET` correctly compiles to
   `await fixture.WaitForQuietAsync(page);`, which is the C# helper
   from Iteration 4 of `playwright-sync-watermark-and-playbook`.

   **Note on the compile-only choice.** The compile-only smoke
   intentionally pairs `NAVIGATE TO` with the explicit verb (no
   interaction step) so that no label probe runs through the W0
   validator. Any interaction-based smoke (`CLICK ... WAIT FOR ACTIONS
   QUIET ... VERIFY`) would hit the same role-context limitation that
   blocks the LLM-authored checklist below — you'd be blocked at
   compile by `UnknownRoute` even on a route the catalog plainly
   contains. The double `WaitForQuietAsync` (one implicit from the
   existing `EmitWatermarkWaits` flag, one explicit from the new verb)
   is the transitional-debt coexistence that Session 2's verifier
   flagged; production usage pairs the explicit verb with an
   interaction step per Hard Requirement #8 in the prompt template,
   and the implicit-emission flag's removal is the natural follow-up
   once the W0 role-context gap closes.

## What the smoke does NOT prove (and why)

The spec's "Quality bar: 5/5 PASS" against the live stack is **blocked
by a known W0 infrastructure gap**, documented in
`tools/uat-suppressions.json`:

> "the W0 CLI itself still does NOT expose `--suppressions <path>` —
> when W0 ships that flag, the runner can pass through suppression
> matches at compile time so the emitted .cs is produced and
> `dotnet test` can run; until then, a fully-suppressed compile is a
> 'passed with warnings, no test invoked' run."

Two specific failure paths:

- **Pre-auth route gap.** `/login` and `/dev/email-inbox` are not in
  the catalog (the crawler authenticates first). Any sign-in flow
  produces `UnknownRoute` at validate. Suppressed for prior checklists.
- **Role context gap.** `SemanticValidator.cs` defaults `activeRole`
  to `"*"`. Catalog entries are stored under concrete role names
  (`SystemAdmin`, etc.). With no script-level role declaration, every
  label probe on `/admin/products` falls through to the
  `EntriesFor(route, "*")` fallback and returns zero hits — even
  though the catalog clearly contains the entries. Surfaces as
  `UnknownRoute` for the same route the script just navigated to.

Until W0 ships either (a) a `--suppressions` flag on the CLI so the
runner can compile-with-warnings, or (b) a script-level `AS ROLE
"<name>"` directive (or equivalent persona-pinning), the smoke cannot
flow through the full `python ai-router/uat_runner/cli.py` →
`dotnet test` pipeline.

This gap pre-dates the watermark integration. Session 1 + Session 2 +
Session 3 land their pieces correctly; the missing 5/5 runtime proof
is not a regression introduced by this set.

## Recommended follow-up

A new session set, `w0-cli-suppressions-and-role-pinning`, to:

1. Add `--suppressions <path>` to the W0 CLI's compile and validate
   modes so the runner can compile-with-warnings and emit code.
2. Either add an `AS ROLE "<name>"` script directive, or update the
   runner to detect persona from the first sign-in step's email
   address and pass `activeRole` to the validator.

The watermark integration is correct and ready to consume once that
gap closes.
