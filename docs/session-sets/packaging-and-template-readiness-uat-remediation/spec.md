# Packaging And Template Readiness UAT Remediation Spec

> **Purpose:** Close the findings recorded in the `packaging-and-template-readiness`
> UAT review disposition (1 Blocker + 4 Substantive + 3 Minor). The Blocker is a
> template/runtime mismatch that makes the scaffolded full app crash before
> `/login`; closing it unblocks the headline deliverable of the source set.
> **Created:** 2026-05-16
> **Session Set:** `docs/session-sets/packaging-and-template-readiness-uat-remediation/`
> **Prerequisite:** `packaging-and-template-readiness` Session 11 closed VERIFIED
> with filled UAT checklist on 2026-05-16. Disposition lives at
> [`../packaging-and-template-readiness/uat-review-disposition.md`](../packaging-and-template-readiness/uat-review-disposition.md).
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Session Set Configuration

```yaml
requiresUAT: true
requiresE2E: true
uatScope: per-set
```

> **Rationale:**
> - `requiresUAT: true` because the source set's UAT failed and the remediation must
>   produce visible improvements the human can re-judge. The new checklist re-verifies
>   the source findings against the post-fix state and adds items for the new fixes
>   (devidp --help behavior, Reports.Core license surface).
> - `requiresE2E: true` because Session 1 adds a new Playwright test that drives the
>   scaffolded app through `dotnet run → /login` — the exact assertion that was
>   missing and let B1 slip into UAT.
> - `uatScope: per-set` — one end-of-set checklist covers the combined remediation
>   surface.

---

## Project Overview

This is a focused remediation set. It does not extend the platform's surface; it
closes specific defects in the just-shipped packaging and template work. Sessions
are scoped so each one ends with a verifiable improvement against the disposition
table.

The Blocker (B1) is the priority. Once Session 1 lands the template fix plus a
runtime-reaching automated test, the source set's headline claim ("a scaffolded
app reaches `/login` in under 30 minutes") becomes true and stays true. The
remaining Substantive items are doc and metadata corrections; they are sized
small but should not be folded into Session 1 because doc edits should not share
a verification round with a runtime fix.

## Parallelization Notes

This set is small and sequential. No worktree parallelization is justified.

## Session Plan

### Session 1 of 4: Template Runtime Crash Fix + Runtime-Reaching E2E

**Steps:**

1. Read `docs/planning/project-guidance.md` and
   `docs/planning/lessons-learned.md` (especially the just-added lesson
   *Author Template Content And Consumer Smoke Test In The Same Session*).
2. Reproduce the crash: run `tools/pack.ps1` to produce the local feed, then
   `dotnet new install pack-output/Dabbler.Templates.0.1.0-preview.1.nupkg`,
   `dotnet new dabbler-app -o /tmp/smoke`, `cd /tmp/smoke && dotnet build &&
   dotnet run`. Confirm the host crashes before `/login` with the
   `BakedInRoleAliasOverrides` deserialization failure.
3. Locate the template's `security-config.json` source at
   `templates/Dabbler.Templates/content/dabbler-app/.template.config/`'s
   sibling content directory (the actual `security-config.json` file lives
   in `templates/Dabbler.Templates/content/dabbler-app/<root>/security-config.json`
   or similar — discover via `Grep` for the literal
   `"BakedInRoleAliasOverrides"`). Change the array `[]` to an empty object
   `{}` so the deserializer succeeds.
4. Apply the same fix to the `dabbler-min` template if it has the same
   `security-config.json` shape.
5. Re-run the scaffold + build + run cycle. Confirm the host now reaches
   `/login`.
6. **Add the runtime-reaching automated test.** Either:
   - Extend `tests/AcmeCoffeeOnline.Playwright/TemplateSmokePlaywrightTests.cs`
     with a non-SkippableFact that actually starts the scaffolded app and
     navigates to `/login`, OR
   - Add a new test (xUnit `[Fact]`, not `SkippableFact`) in
     `tests/Dabbler.Packaging.Smoke/` that does the full scaffold + `dotnet
     run` cycle programmatically (start the host, hit `/login`, assert
     non-error HTTP response), tear down.
   - The test MUST fail before the template fix and pass after. Run the test
     before applying the fix to confirm red, then apply the fix and confirm
     green.
6. Build and test the full suite.

**Creates:** New test method or test file proving the scaffolded app reaches
`/login`; updated template `security-config.json` (one or both templates).
**Touches:** `templates/Dabbler.Templates/content/dabbler-{app,min}/<root>/security-config.json`;
test project under `tests/`.
**Ends with:** B1 closed; future regressions of this class would be caught by
automation, not UAT.
**Progress keys:** `session-1/reproduce-crash`, `session-1/fix-template-config`,
`session-1/add-runtime-test`, `session-1/build-and-test`

---

### Session 2 of 4: Documentation De-Staling Pass

**Steps:**

1. Read `docs/planning/project-guidance.md` and the disposition's S1+S2 entries.
2. Update `docs/platform-overview.md`: rewrite any section that frames NuGet
   packaging or template scaffolding as "planned" or "future" work to reflect
   that both shipped in `packaging-and-template-readiness` Sessions 7-9. Add
   a brief, accurate sub-section on the two-template contract (`dabbler-app` /
   `dabbler-min`) and the stubs sub-package split if not already present.
3. Sweep all `src/Libs/*/README.md` files for future-tense language about
   Session 7 packaging work that has shipped. Convert to present tense and
   include accurate `dotnet add package <id>` instructions where appropriate.
4. Spot-check `docs/getting-started.md` for any references that drifted since
   authoring (e.g., the "Step 6" vs "Step 10" stub-section reference from M1).
5. Route the sweep via `route(task_type="documentation")` for each file that
   needs more than a tense pass — let the router pick (Gemini Pro typical).
   Do NOT force Opus.
6. Build and run markdown checks if configured.

**Creates:** No new files; updated docs in place.
**Touches:** `docs/platform-overview.md`, `src/Libs/*/README.md`,
`docs/getting-started.md` (light spot-fix only).
**Ends with:** S1, S2, M1 closed. Reading the docs in onboarding order no longer
surfaces the future-tense contradiction.
**Progress keys:** `session-2/refresh-platform-overview`,
`session-2/library-readme-tense-pass`, `session-2/getting-started-spot-fix`,
`session-2/markdown-checks`

---

### Session 3 of 4: Reports.Core License Surface + DevIdp Tool Help Fix

**Steps:**

1. Read `docs/planning/project-guidance.md` and disposition entries S3+S4.
2. **Reports.Core license surfacing (S3).** Update
   `src/Libs/Dabbler.Reports.Core/Dabbler.Reports.Core.csproj`:
   - The `<PackageLicenseExpression>` is currently `MIT`. The package's own
     code is MIT, but its required transitive dependency `DabblerPdf` is
     LGPL-2.1-only OR MPL-2.0. The cleanest path: keep `MIT` as the
     license-expression but rewrite the `<Description>` to explicitly state
     that `Dabbler.Reports.Core` is MIT-licensed AND that taking a dependency
     transitively pulls `DabblerPdf` under LGPL-2.1-only OR MPL-2.0; point at
     `THIRD-PARTY-NOTICES.txt` for the canonical text.
   - Apply the same description pattern to `Dabbler.Reports.Blazor` and
     `Dabbler.Reports.Export` for consistency.
   - Add an `NupkgArtifactTests` assertion that the description of each
     `Dabbler.Reports.*` package contains the words `DabblerPdf` and `LGPL`
     so future packagers can't accidentally drop the surfacing.
3. **DevIdp tool --help fix (S4).** In
   `tools/Dabbler.Security.DevIdp.Tool/Program.cs` (or equivalent entry
   point), add an explicit handler at the start of `Main` that detects
   `args.Contains("--help")`, `-h`, or `-?` and prints usage + exits 0 before
   building the host. Standard pattern: print `Description`, `Usage`,
   `Options` (e.g., `--port`, `--inbox-path`), and any environment-variable
   overrides the tool honors.
4. Add an integration test or smoke check that invokes `dotnet
   Dabbler.Security.DevIdp.Tool.dll --help` and asserts the process exits 0
   with usage text on stdout. (xUnit `[Fact]` using `System.Diagnostics.Process`
   is fine — the tool doesn't need a full E2E.)
5. Run `tools/pack.ps1` to regenerate `pack-output/`, then run the new
   assertions in `NupkgArtifactTests` and the new tool --help test to
   confirm both pass.
6. Build and test.

**Creates:** Test assertions for the license surface and the --help behavior.
**Touches:** `src/Libs/Dabbler.Reports.{Core,Blazor,Export}/*.csproj`,
`tools/Dabbler.Security.DevIdp.Tool/Program.cs`,
`tests/Dabbler.PublicSurface.Tests/NupkgArtifactTests.cs`, possibly a new test
file under `tests/` for the tool --help check.
**Ends with:** S3, S4 closed with automated regression protection.
**Progress keys:** `session-3/reports-license-surface`,
`session-3/devidp-help-handler`, `session-3/license-assertions`,
`session-3/build-and-test`

---

### Session 4 of 4: UAT Checklist + Closeout

**Steps:**

1. Read `docs/planning/project-guidance.md` and `docs/planning/lessons-learned.md`.
2. Generate a fresh human-UAT checklist
   `packaging-and-template-readiness-uat-remediation-uat-checklist.json` against
   the canonical schema. Items re-verify each disposition finding post-fix:
   - UAT-R1: Re-execute the scaffold + build + run cycle, confirm `/login` is
     reached (closes B1)
   - UAT-R2: Re-read `docs/platform-overview.md`, confirm packaging/template
     story is present-tense (closes S1)
   - UAT-R3: Re-read per-library READMEs, confirm tense pass landed cleanly
     (closes S2)
   - UAT-R4: Inspect `Dabbler.Reports.Core.nupkg` metadata, confirm description
     surfaces DabblerPdf's LGPL/MPL transitive license (closes S3)
   - UAT-R5: Run `dabbler-devidp --help`, confirm usage prints and process
     exits 0 without starting the host (closes S4)
   - UAT-R6: Re-read the onboarding journey end-to-end, confirm coherence
     (closes S5 as a function of S1+S2)
   - Include the M1/M2/M3 corrections inline (this checklist's references
     should be accurate from the start).
3. Validate the checklist against the canonical schema.
4. Verify the checklist loads in the UAT checklist editor.
5. Hand off the checklist to the human for execution.
6. Generate `change-log.md` for this session set.

**Creates:** Human-UAT checklist JSON, `change-log.md`.
**Touches:** This session-set folder.
**Ends with:** Checklist ready for human execution; remediation set closes when
human UAT passes.
**Progress keys:** `session-4/generate-checklist`, `session-4/verify-checklist`,
`session-4/handoff`, `session-4/change-log`
