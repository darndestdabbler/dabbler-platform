## Changelog for uat-workflow-dsl-alignment

This session set aligns the AI-led session workflow + cross-repo documentation with the new DSL-driven UAT/E2E pipeline shipped by the prior `uat-dsl-watermark-integration` and `w0-cli-suppressions-and-role-pinning` sets. Tool-fundamental documentation (DSL syntax, CLI flags, JSON schemas) moves to the sibling `dabbler-uat-dsl` repo so it versions with the toolchain; integration commentary (watermark pairing, runner orchestration, version-compatibility) stays in `dabbler-platform`.

### Session 1

**What changed:** Three reference documents authored in the sibling `dabbler-uat-dsl` repo (`docs/language-reference.md`, `docs/cli-reference.md`, `docs/data-contracts.md`) covering the closed verb list, structured `VERIFY` forms, both CLI subcommands (`validate` / `compile`) with every flag and JSON output shape, the `ValidationErrorKind` taxonomy, and the catalog (1.0 / 1.1 with `isWatermarked`) + suppressions JSON contracts. The sibling README gained a Documentation section linking the three new docs and a How-this-fits-in pointer back to dabbler-platform's AI-led workflow + the new `COMPATIBILITY.md`. In `dabbler-platform`, `docs/COMPATIBILITY.md` was created with the supported W0 Core 1.2.0 / Cli 1.2.1 / Emitters 1.1.0 / catalog-schemaVersion 1.1 combination plus a bump procedure. `docs/planning/project-guidance.md` had its `Catalog-Grounded UAT Checklist Authoring` and `The UAT/E2E DSL — closed verb list` Conventions refactored to link to the new sibling docs instead of restating syntax; integration commentary (watermark pairing rule, W2 runner role-inference, click-driven cursor known limitation) was preserved in dabbler-platform.

**Artifact paths touched:**
- `dabbler-uat-dsl/docs/language-reference.md` (new)
- `dabbler-uat-dsl/docs/cli-reference.md` (new)
- `dabbler-uat-dsl/docs/data-contracts.md` (new)
- `dabbler-uat-dsl/README.md` (Documentation + How-this-fits-in sections added)
- `dabbler-platform/docs/COMPATIBILITY.md` (new)
- `dabbler-platform/docs/planning/project-guidance.md` (two Convention entries refactored)

**Measurable outcome:** A fresh project pulling W0 1.2.0 from NuGet can read the sibling docs and learn DSL syntax + CLI flags + data contracts without touching dabbler-platform; dabbler-platform's project-guidance no longer duplicates W0 syntax. `dotnet build` clean, 709/709 non-Playwright unit/integration/bUnit tests pass. Cross-provider verification (gemini-pro, $0.0313) flagged the Three-Step Authoring Discipline collapse as missing; orchestrator disposition is FALSE POSITIVE because that collapse is Session 2 Step 4 work per spec. Session marked VERIFIED. Note: first verification call hit the known same-provider routing penalty (landed on Opus, $1.24 wasted); re-routed with `max_tier=2` to force a true cross-provider verifier.
