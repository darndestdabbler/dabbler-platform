# UAT Review Disposition — packaging-and-template-readiness

> **Source checklist:** [`packaging-and-template-readiness-uat-checklist.json`](./packaging-and-template-readiness-uat-checklist.json)
> **Review date:** 2026-05-16
> **Reviewer (UAT execution):** Operator
> **Classifier (this disposition):** Claude Sonnet 4.6 (in-conversation, operator-confirmed classification)
> **Next action:** [`packaging-and-template-readiness-uat-remediation`](../packaging-and-template-readiness-uat-remediation/spec.md) (~4 sessions)

This document fulfills the UAT Review Session contract in `docs/planning/project-guidance.md` → *UAT Review Session (set type)*: every UAT item where `Passes == false` or `Feedback` is non-empty receives a disposition. The classification was done in-conversation between the operator and the Sonnet 4.6 orchestrator rather than as a separate 1-session set; the operator explicitly authorized this path.

---

## Findings

### Blocker — must remediate before any other Phase 2 work resumes

**B1. Scaffolded full app crashes before `/login`** (UAT-1, blocking finding)

- **Symptom:** `dotnet new dabbler-app -o MyApp && cd MyApp && dotnet build && dotnet run` builds clean but the host crashes at startup before reaching `/login`.
- **Root cause:** The template's generated `security-config.json` contains `"BakedInRoleAliasOverrides": []` (JSON array) while the runtime's `SecurityConfigurationManager` deserializes that property as `Dictionary<string, string>` (JSON object).
- **Why automation missed it:** Session 7's `NupkgArtifactTests` and Session 9's `PackagingSmokeTests` only validate `dotnet build`. Session 10's `TemplateSmokePlaywrightTests` are `SkippableFact` and skip whenever `PLAYWRIGHT_TEMPLATE_APP_URL` is not set — the CI/dev environment never set it, so the runtime-reachability assertion never executed.
- **Impact:** The set's headline deliverable (a runnable scaffolded app per `getting-started.md`) does not work end-to-end. Documentation tells the developer to run the app; the app crashes.

### Substantive — real defects, scope of the remediation set

**S1. `docs/platform-overview.md` is stale against shipped state** (UAT-4)
- The overview still frames NuGet packaging and template scaffolding as planned/future work, while `docs/getting-started.md` tells developers to install `Dabbler.Templates` directly. The contradiction undercuts the overview's role as the canonical entry-point doc.

**S2. Per-library READMEs use future tense for Session 7 packaging work that has shipped** (UAT-6)
- Multiple `src/Libs/*/README.md` files still describe NuGet packaging as planned. The packages exist in `pack-output/` and the metadata is in their csproj files.

**S3. `Dabbler.Reports.Core` nuspec license metadata does not surface DabblerPdf's transitive license** (UAT-5)
- The package includes `THIRD-PARTY-NOTICES.txt` (correct), but the nuspec `<license>` element is `MIT` and the `<description>` does not call out that taking a dependency on `Dabbler.Reports.Core` transitively pulls DabblerPdf under LGPL-2.1-only OR MPL-2.0. A license-sensitive adopter inspecting only the package metadata could miss a real adoption constraint.

**S4. `dabbler-devidp --help` starts the host instead of printing help and exiting** (operator-added Other item under Packaging)
- The CLI tool ignores `--help` and proceeds to start the DevIdp host. Standard `dotnet tool` UX is to print usage and exit (`-h`/`--help`/`-?`). This is a tool-surface defect noticed during installation testing.

**S5. Consumer journey coherence breaks because of S1+S2** (UAT-7)
- The end-to-end onboarding read (`README.md` → `platform-overview.md` → `getting-started.md` → library READMEs) surfaces the staleness from S1 and S2 as a journey-level coherence problem. This is not a separate fix — closing S1 and S2 closes S5 automatically. Tracked here only for traceability.

### Minor — fold into the remediation set's checklist or doc passes, not their own work

**M1. Checklist stale references to the getting-started.md stub section**
- The checklist points to "Step 6 (bUnit and Stub Integration)" but the stub section is now Step 10. Likely a downstream rename in `getting-started.md` between Session 8 authoring and Session 11 UAT execution.

**M2. Checklist's UAT-2 fallback template.json paths are wrong**
- The checklist says template.json lives under `templates/Dabbler.Templates/templates/dabbler-app/.template.config/`. The actual paths are `templates/Dabbler.Templates/content/dabbler-app/.template.config/template.json` and `.../content/dabbler-min/.template.config/template.json`.

**M3. `dotnet new list dabbler` sample output is directionally correct but not exact**
- Operator observed real tags are `Web/Blazor/Server/Security/Reports` (full) and `Web/Blazor/Server/Security` (minimal), not the simpler `Web/Blazor/Server` shown in the checklist sample. Minor for the next checklist regeneration; the assessment itself was answerable.

### Out of scope — not addressed

None. Every finding above belongs in the remediation set.

---

## Disposition summary

| Finding | Severity | Disposition |
|---|---|---|
| B1 — Template runtime crash | Blocker | Remediation Session 1 + new runtime-reaching E2E test |
| S1 — platform-overview.md stale | Substantive | Remediation Session 2 |
| S2 — Per-library README tense | Substantive | Remediation Session 2 |
| S3 — Reports.Core license surfacing | Substantive | Remediation Session 3 |
| S4 — devidp --help defect | Substantive | Remediation Session 3 |
| S5 — Journey coherence | Substantive (auto-resolves) | Covered by S1 + S2 close |
| M1 — Checklist Step 6/10 reference | Minor | Folds into remediation set's fresh checklist |
| M2 — Checklist template.json paths | Minor | Folds into remediation set's fresh checklist |
| M3 — Sample `dotnet new list` output | Minor | Folds into remediation set's fresh checklist |

---

## Next action

Author and execute [`packaging-and-template-readiness-uat-remediation`](../packaging-and-template-readiness-uat-remediation/spec.md). After that set closes VERIFIED + human UAT passes, `packaging-and-template-readiness` is fully closed.
