# Change Log: `uat-dsl-watermark-integration`

## Summary
This session set integrates the Dabbler.Testing.Blazor watermark into the UAT DSL pipeline. It enables DSL-compiled E2E tests to automatically wait for `data-actions-in-flight = "[]"` after interactions on watermarked components. This involved extending the component catalog, updating the DSL grammar with a new `WAIT FOR ACTIONS QUIET` verb, and updating the UAT script authoring prompt to leverage this new capability.

## What landed
- **Session 1: Catalog crawler — flag IsWatermarked per route**
    - Added `IsWatermarked: bool` field to `CatalogElement` record.
    - Bumped catalog `schemaVersion` to 1.1.
    - Implemented `WatermarkDetector` in `tools/ComponentCatalogCrawler/` for source-level scanning of `.razor` files.
    - Added 8 unit tests in `tools/ComponentCatalogCrawler.Tests/`.
- **Session 2: DSL grammar + `WAIT FOR ACTIONS QUIET` verb (sibling repo: dabbler-uat-dsl)**
    - Introduced new grammar rule `waitForActionsQuietStep` in `DabblerUatDsl.g4`.
    - Defined `WaitForActionsQuietStep` AST record.
    - Added emitter case: `await fixture.WaitForQuietAsync(page);`.
    - Added 7 new tests (4 grammar + 1 AST + 2 emitter).
    - Bumped `Dabbler.Uat.Dsl` Core and Emitters.Playwright.CSharp pack-output to 1.1.0.
    - Sibling commit: `dabbler-uat-dsl@620238fa98de63c246af6be0dbb7fa27a7ab7de7`.
- **Session 3: Prompt update + end-to-end smoke + close**
    - Updated `Directory.Packages.props` to consume `Dabbler.Uat.Dsl 1.1.0`.
    - Modified `uat-script-author` prompt template to emit `WAIT FOR ACTIONS QUIET` after interaction verbs on `isWatermarked: true` routes.
    - Re-crawled `docs/component-catalog/index.json`, marking `/admin/products` entries as `isWatermarked: true`.
    - Smoke-authored a checklist confirming correct LLM emission of `WAIT FOR ACTIONS QUIET`.
    - Confirmed end-to-end compilation of the new verb to `await fixture.WaitForQuietAsync(page);`.

## Known gaps
- The full 5x runner-driven proof of concept is blocked by a pre-existing W0 limitation where `SemanticValidator` defaults `activeRole` to `"*"`, but catalog entries require concrete role names. A follow-up session set candidate (`w0-cli-suppressions-and-role-pinning`) addresses this.

## Files of interest
- `tools/ComponentCatalogCrawler/WatermarkDetector.cs`
- `tools/ComponentCatalogCrawler/CatalogElement.cs`
- `Directory.Packages.props`
- `ai-router/prompt-templates/task-prompts.md`
- `docs/component-catalog/index.json`
- `docs/session-sets/uat-dsl-watermark-integration/smoke/`