## crud-scaffolding-remediation

### Overview

This session set addressed concrete gaps in the CRUD scaffolding templates delivered by the `crud-scaffolding` set. It delivered a repo-aware wrapper command (`dabbler-crud-slice`), a security and catalog registration generator, and a cross-platform schema applicator. All open documentation drift identified from the prior set's feedback was reconciled, including new "how-to" recipes. This enables a genuinely drop-in CRUD onboarding workflow.

### Per-session deliverables

**Session 1: Cross-platform schema applicator**

*   Created `tools/Dabbler.SchemaApplicator/` (.NET 10 console app).
*   Created `tools/Dabbler.SchemaApplicator.Tests/` (7 tests, contract-compliant with PS1 equivalent).
*   Deprecated `Apply-{Entity}Schema.ps1` (retained for backward compatibility).
*   Updated `crud-service-template.md` and `crud-page-template.md` to reference the new applicator.
*   Cost: ~$0.20

**Session 2: Repo-aware dabbler-crud-slice wrapper command**

*   Created `tools/Dabbler.CrudSlice/` console app.
*   Created `tools/Dabbler.CrudSlice.Tests/` (integration tests for the full pipeline).
*   Generated `SupplierScaffoldingExtensions.cs` and `SupplierApiRegistrationExtensions.cs` match real host wiring (`Services:ApiBaseUrl`, `X-Internal-Api-Key`, `UserIdentityHandler`).
*   Replaced the original `SupplierScaffoldingExtensions.cs` which used `Api:BaseUrl` and omitted header/handler.
*   Cost: ~$0.09

**Session 3: Security/catalog registration generator**

*   Added `--emit-registrations` mode to `Dabbler.CrudSlice`.
*   Emits `{Entity}Registrations.g.cs` as a copy-paste reference for entity-registration, API endpoint, web page, and web endpoint blocks matching `Api/Program.cs` and `Web/Program.cs` shapes.
*   Templates extracted to `tools/Dabbler.CrudSlice/Templates/` and loaded via embedded resources.
*   Fixed duplicate `/suppliers` page registration in `Web/Program.cs`.
*   3 verification rounds (1 Major endpoint-shape gap, 1 Minor compliance follow-up resolved).
*   Cost: ~$0.21

**Session 4: Documentation reconciliation + section-8 additions + end-to-end proof**

*   Reconciled doc-accuracy items from `FEEDBACK_REMEDIATION_SUMMARY.md`: single-source-of-truth wiring, nav menu not generated, Playwright contradiction.
*   Added applicability note to `docs/scaffolding-templates/README.md`.
*   Added reauthorization gate to `docs/scaffolding-templates/non-mvp-roadmap.md`.
*   Created `docs/scaffolding-templates/recipes/extend-crud-page-with-workflow-action.md` (worked example).
*   Updated `FEEDBACK_REMEDIATION_SUMMARY.md` to mark all three doc-accuracy items resolved.
*   End-to-end proof: `dabbler-crud-slice` execution against a temporary directory produced byte-identical output to the committed Supplier slice.
*   2 verification rounds (1 MEDIUM finding on recipe path/namespace, fixed).
*   Cost: ~$0.24 (Round 1 $0.18 + Round 2 $0.06)

### End-of-set deliverables

**Tooling:**

*   `tools/Dabbler.SchemaApplicator/` — cross-platform C# CLI replacing `Apply-{Entity}Schema.ps1`.
*   `tools/Dabbler.CrudSlice/` — repo-aware wrapper invoking templates, applicator, and registration emit.

**Generated files (committed in `src/Apps/.../Supplier...`):**

*   `SupplierScaffoldingExtensions.cs` (Web-side DI with `Services:ApiBaseUrl`, `X-Internal-Api-Key`, `UserIdentityHandler`) — replaced previous version.
*   `SupplierApiRegistrationExtensions.cs` (API-side DI).
*   `SupplierRegistrations.g.cs` (`#if false` copy-paste reference for `Api/Program.cs` and `Web/Program.cs` blocks).

**Bugfix:**

*   Duplicate `/suppliers` page registration removed from `Web/Program.cs` (single registration at `Web/Program.cs:101`).

**Documentation:**

*   `docs/scaffolding-templates/README.md` — applicability note and wrapper section added.
*   `docs/scaffolding-templates/crud-page-template.md` — nav menu not generated clarified.
*   `docs/scaffolding-templates/non-mvp-roadmap.md` — reauthorization gate (`Granularity 3+ deferred`).
*   `docs/scaffolding-templates/recipes/extend-crud-page-with-workflow-action.md` — new recipe.
*   `CRUD_SCAFFOLDING_FINAL_SUMMARY.md` — single-source-of-truth wiring; Playwright matrix row corrected.
*   `FEEDBACK_REMEDIATION_SUMMARY.md` — three doc-accuracy items marked Resolved.

### Verification

**Per-session cross-provider verification (Anthropic Opus orchestrator -> OpenAI gpt-5-4 verifier):**

*   Session 1: VERIFIED Round 1 (1 spec-compliance gap fixed).
*   Session 2: VERIFIED Round 1.
*   Session 3: VERIFIED Round 3 (1 Major endpoint-shape, 1 Minor templates-directory; both fixed).
*   Session 4: VERIFIED Round 2 (1 MEDIUM recipe path/namespace; fixed).

### Costs

~$0.74 total routed cost across 4 sessions.