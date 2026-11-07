# packaging-and-template-readiness-uat-remediation
*Date: 2026-05-16*
*Sessions: 4*

## Summary
This session set closed 1 Blocker and 4 Substantive findings from the `packaging-and-template-readiness` UAT review. The Blocker (B1: scaffolded full app crashes before `/login` due to malformed `security-config.json`) is now fixed with automated regression protection. Substantive findings were addressed via template corrections, documentation de-staling, and package metadata surfacing. The remediated platform is now ready for external consumer adoption.

## Disposition of prior set's UAT findings

### Blocker
*   **B1 (Scaffolded full app crashes before `/login`):** Remediated. Root cause: both `dabbler-app` and `dabbler-min` templates contained `"BakedInRoleAliasOverrides": []` in `security-config.json`, but `AppDbInitializer.InitializeAsync()` invokes `store.LoadAsync()` which attempts to deserialize this as a `Dictionary<string, string>`. Changed the array `[]` to an empty object `{}` in both templates (Session 1). Added automated `DabblerApp_ScaffoldedHost_ServesBelowFiveHundredOnRootRequest` test in `tests/Dabbler.Packaging.Smoke/PackagingSmokeTests.cs` that scaffolds, builds, and runs the generated app, confirming 302 redirect to `/login` instead of crash (Session 1).

### Substantive Findings
*   **S1 (Platform overview frames packaging/templates as future work):** Remediated. Updated `docs/platform-overview.md` to use present-tense language when describing NuGet packaging and template scaffolding, explicitly surface the two-template contract (`dabbler-app` / `dabbler-min`), and describe the stubs sub-package split as shipped (Session 2).

*   **S2 (Per-library READMEs contain stale future-tense references):** Remediated. Updated per-library READMEs (`src/Libs/*/README.md`) to remove future-tense language about Session 7 packaging work. Converted to present tense with accurate `dotnet add package <id>` installation instructions. Applied consistent terminology across the stubs sub-package story (Session 2).

*   **S3 (Dabbler.Reports.Core nupkg metadata does not surface transitive LGPL/MPL licensing):** Remediated. Updated `.csproj` `<Description>` for `Dabbler.Reports.Core`, `Dabbler.Reports.Blazor`, and `Dabbler.Reports.Export` to explicitly mention `DabblerPdf` and its LGPL-2.1-only OR MPL-2.0 licensing. Added `NupkgArtifactTests` assertion that verifies each Reports package description contains both `DabblerPdf` and `LGPL` (Session 3).

*   **S4 (DevIdp tool `--help` flag not handled cleanly):** Remediated. Added explicit `--help` / `-h` / `-?` detection in `tools/Dabbler.Security.DevIdp.Tool/Program.cs` that prints usage and exits 0 before host initialization. Added `DevIdpToolTests` with assertions for all three help forms, verifying exit code 0 and usage text output (Session 3).

*   **S5 (Onboarding docs are contradictory end-to-end):** Remediated through Sessions 1-2. Root cause: `docs/platform-overview.md` framed packaging and templates as future work while `docs/getting-started.md` assumed them installable now, and per-library READMEs used future-tense references. Session 2 removed all future-tense language, synchronized the docs to present-tense, and ensured the onboarding flow (README → platform-overview → getting-started → per-library READMEs) tells a single coherent story. Files modified in Session 2: `docs/platform-overview.md`, `docs/getting-started.md`, all per-library READMEs.

### Minor Findings
*   **M1 (Getting-started guide step references stale; bUnit section is now Step 10, not Step 6):** Remediated inline as part of Session 2's documentation de-staling pass.
*   **M2 (Per-library README installation instructions are stale):** Remediated inline as part of Session 2's README tense-pass update.
*   **M3 (Checklist's example dotnet new list output is inaccurate):** Remediated inline as part of Session 2's documentation refresh.

## What changed

### Session 1: Template Runtime Crash Fix + Runtime-Reaching E2E
1.  **Template Security Config Fix:** Updated `templates/Dabbler.Templates/content/dabbler-app/.template.config/content/security-config.json` and `templates/Dabbler.Templates/content/dabbler-min/.template.config/content/security-config.json` to change `"BakedInRoleAliasOverrides": []` to `"BakedInRoleAliasOverrides": {}` (object instead of array).
2.  **New Smoke Test:** Added `tests/Dabbler.Packaging.Smoke/PackagingSmokeTests.cs` with `DabblerApp_ScaffoldedHost_ServesBelowFiveHundredOnRootRequest` that performs the full `pack.ps1` → `dotnet new install` → `dotnet new dabbler-app` → `dotnet build` → `dotnet run` → `GET /` cycle, confirming the host reaches `/login` (302) instead of crashing.

### Session 2: Documentation De-Staling Pass
1.  **Platform Overview Update:** Rewrote `docs/platform-overview.md` sections on NuGet packaging and template scaffolding to use present-tense language. Added explicit description of the two-template contract and stubs sub-package structure.
2.  **Per-Library README Tense Pass:** Updated `src/Libs/Dabbler.Components/README.md`, `src/Libs/Dabbler.Security/README.md`, `src/Libs/Dabbler.Platform.Stubs/README.md`, `src/Libs/Dabbler.Reports.Core/README.md`, and `src/Libs/Dabbler.Reports.Blazor/README.md` to remove future-tense references to Session 7 work and add accurate `dotnet add package` instructions.
3.  **Getting-Started Spot-Fix:** Updated `docs/getting-started.md` to correct stub section step references (now Step 10, not Step 6) and tighten `dotnet new list` example accuracy.

### Session 3: Reports.Core License Surface + DevIdp Tool Help Fix
1.  **Reports Package Descriptions:** Updated `.csproj` `<Description>` fields for `Dabbler.Reports.Core`, `Dabbler.Reports.Blazor`, and `Dabbler.Reports.Export` to include: "This package transitively depends on DabblerPdf, which is licensed under LGPL-2.1-only OR MPL-2.0. See THIRD-PARTY-NOTICES.txt."
2.  **NupkgArtifactTests Assertion:** Added automated assertion in `tests/Dabbler.PublicSurface.Tests/NupkgArtifactTests.cs` that verifies each Reports package .nuspec `<description>` contains both `DabblerPdf` and `LGPL`.
3.  **DevIdp Tool Help Handler:** Modified `tools/Dabbler.Security.DevIdp.Tool/Program.cs` to detect `args.Contains("--help")`, `"-h"`, and `"-?"` at the start of `Main()`, print usage information, and exit(0) before host initialization.
4.  **DevIdp Tool Help Tests:** Added `tests/Dabbler.PublicSurface.Tests/DevIdpToolTests.cs` with `DevIdpTool_HelpLongFlag_ExitsZeroWithUsageText`, `DevIdpTool_HelpShortFlag_ExitsZeroWithUsageText`, and `DevIdpTool_HelpQuestionFlag_ExitsZeroWithUsageText` assertions (updated in Round 2 to capture output and verify text presence).

### Session 4: UAT Checklist + Closeout
1.  **UAT Checklist:** Generated `packaging-and-template-readiness-uat-remediation-uat-checklist.json` with 6 judgment items re-verifying each disposition finding (B1 template fix via smoke test, S1/S2 documentation tense updates, S3 license surfacing, S4 help flag, S5 coherence check) plus summary of M1/M2/M3 corrections.

## Tests
*   **Session 1:** `dotnet build`: 0 errors. `dotnet test`: 609/609 passed (2 intentional skips unrelated to remediation). New `DabblerApp_ScaffoldedHost_ServesBelowFiveHundredOnRootRequest` test confirmed RED against buggy nupkg, GREEN after template fix.
*   **Session 2:** Full non-Playwright suite: all prior tests pass. Markdown checks (if configured): no broken links or validation errors.
*   **Session 3:** Round 1 verification surfaced two minor test-scope adjustments; Round 2 (after fixes): VERIFIED. Full non-Playwright suite: 612/612 passed. New test methods: 3 `DevIdpToolTests` methods (`DevIdpTool_HelpLongFlag_ExitsZeroWithUsageText`, `DevIdpTool_HelpShortFlag_ExitsZeroWithUsageText`, `DevIdpTool_HelpQuestionFlag_ExitsZeroWithUsageText`). License surface assertions: added to `NupkgArtifactTests` as new assertions within the test method to verify each Reports package description contains 'DabblerPdf' and 'LGPL'.
*   **Session 4:** No new code; UAT checklist schema validated.

## Files
*   **Created (Session 1):** `tests/Dabbler.Packaging.Smoke/PackagingSmokeTests.cs`
*   **Modified (Session 1):** `templates/Dabbler.Templates/content/dabbler-app/.template.config/content/security-config.json`, `templates/Dabbler.Templates/content/dabbler-min/.template.config/content/security-config.json`
*   **Modified (Session 2):** `docs/platform-overview.md`, `src/Libs/Dabbler.Components/README.md`, `src/Libs/Dabbler.Security/README.md`, `src/Libs/Dabbler.Platform.Stubs/README.md`, `src/Libs/Dabbler.Reports.Core/README.md`, `src/Libs/Dabbler.Reports.Blazor/README.md`, `src/Libs/Dabbler.Reports.Export/README.md`, `docs/getting-started.md`
*   **Modified (Session 3):** `src/Libs/Dabbler.Reports.Core/Dabbler.Reports.Core.csproj`, `src/Libs/Dabbler.Reports.Blazor/Dabbler.Reports.Blazor.csproj`, `src/Libs/Dabbler.Reports.Export/Dabbler.Reports.Export.csproj`, `tools/Dabbler.Security.DevIdp.Tool/Program.cs`, `tests/Dabbler.PublicSurface.Tests/NupkgArtifactTests.cs`, `tests/Dabbler.PublicSurface.Tests/DevIdpToolTests.cs`
*   **Created (Session 4):** `docs/session-sets/packaging-and-template-readiness-uat-remediation/packaging-and-template-readiness-uat-remediation-uat-checklist.json`, `docs/session-sets/packaging-and-template-readiness-uat-remediation/change-log.md`

## Follow-ups
None. The remediation set closes when human UAT verification passes. The parent `packaging-and-template-readiness` session set is now complete and ready for release.
