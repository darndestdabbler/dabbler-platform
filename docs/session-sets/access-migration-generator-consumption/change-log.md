# Change Log — access-migration-generator-consumption

> Closes the one packaging gap left by `dabbler-nuget-feed-publication`: the build-time
> CRUD generators were not consumable as artifacts, forcing a `dabbler-platform` source
> checkout to generate a slice. This set decides and ships the durable acquisition story.

## Session 1 — Decide the generator acquisition mechanism (architecture)

- No code. Recorded `generator-acquisition-decision.md` with a tri-provider verdict
  (Opus full-context + GPT-5.4 + Gemini 2.5 Pro on self-contained prompts).
- **Decision:** publish the generator as a **`dotnet tool` on nuget.org** (Option A,
  unanimous), mirroring the existing `Dabbler.Security.DevIdp.Tool` precedent and
  reusing the locked single `DabblerVersion` + `pack.ps1` + trusted-publishing path
  unchanged. Runner-up: Option C (formalize source checkout). Rejected B (library shape
  invites a `<PackageReference>`, dragging build-time code into runtime restore —
  permanent on public IDs).
- **Surface:** one tool now — `Dabbler.CrudSlice.Tool` / `dabbler-crud-slice` — with
  `Dabbler.SchemaApplicator.Tool` pre-named and deferred (CrudSlice bundles
  SchemaApplicator in-process; reversibility).
- **Decision 5 (binds Session 2):** CrudSlice shells out to `dotnet new
  dabbler-crud-service`/`-page`, so acquisition is **two installs at the same pinned
  version** (`Dabbler.Templates` + the tool); the clean-machine gate MUST install both.
- Cross-provider verification (GPT-5.4): VERIFIED, Round 1.

## Session 2 — Implement the chosen mechanism + consumer-validation gate

**Packaging:**
- `tools/Dabbler.CrudSlice/Dabbler.CrudSlice.csproj`: flipped from `IsPackable=false`
  to a `dotnet tool` — `PackAsTool=true`, `ToolCommandName=dabbler-crud-slice`,
  `PackageId=Dabbler.CrudSlice.Tool`, `IsPackable=true`, description/tags/`PackageReadmeFile`.
- `tools/pack.ps1`: added the project to `$LibraryProjects` and `Dabbler.CrudSlice.Tool`
  to `$InRepoPackageIds` (15 → 16).
- `Dabbler.Publish.slnf`: added the project to the publish solution filter.
- No new versioning scheme — rides the single shared `DabblerVersion`.
- Verified: `pack.ps1` produces `Dabbler.CrudSlice.Tool.<ver>.nupkg` with
  `Command Name="dabbler-crud-slice"` and **bundles `Dabbler.SchemaApplicator.dll`
  in-process** under `tools/net10.0/any/`.

**Templates preflight (Decision 5, recommended):**
- `tools/Dabbler.CrudSlice/TemplatePreflight.cs` (new) + a one-line hook in `Program.cs`
  before the first `dotnet new`: fails fast when `dabbler-crud-service`/`dabbler-crud-page`
  are not installed, printing the exact version-matched
  `dotnet new install Dabbler.Templates::<version>` command. Respects `--skip-templates`
  and `--template-hive`.

**Consumer-validation gate (acceptance proof):**
- `tests/Dabbler.Packaging.Smoke/PackagingSmokeTests.cs`: new permanent `[SkippableFact]`
  `CrudSliceTool_AcquiredFromFeed_GeneratesSliceThatBuildsClean` — installs the templates
  into an isolated hive, installs `Dabbler.CrudSlice.Tool` from the local feed via
  `dotnet tool install --tool-path … --add-source … --version <pinned>` (checkout-free,
  not a project reference), runs `dabbler-crud-slice` against the SmokeApp consumer
  fixture + a sample `.dabbler-entity.json`, and **builds the result green**. Also added
  `Dabbler.CrudSlice.Tool` to the smoke required-package allowlist.

**Cross-repo follow-up (Decision deliverable 3):**
- `dabbler-access-migration-orchestrator/docs/playbooks/migration-playbook.md` Step 00:
  replaced the "Code generators — source checkout (not on NuGet)" bullet with the
  two-command packaged acquisition (`dotnet new install Dabbler.Templates::<ver>` +
  `dotnet tool install --global Dabbler.CrudSlice.Tool --version <ver>`, same pinned
  version).
- `…/docs/planning/platform-output-contract.md`: noted that the `tools/...` anchors stay
  valid against the packaged tool (which carries SchemaApplicator), preserving traceability.

**Greenfield finding-power pilot (LEAD signal):**
- Ran the Step-6 provider×surface matrix (`push:anthropic` × {`pull:openai`,
  `pull:google`}) on the frozen pre-remediation source diff. Adjudicated against the
  fixed rubric: **0 TP / 7 FP** (precision 0.00 each arm) — all push-elision +
  pull-out-of-sandbox. Frozen reports + `adjudication.md` shipped to the canonical
  Set 075 telemetry corpus (`includedInFindingPower=true`, `diffClass=source-dominated`).

**Verification:**
- Cross-provider session verification (GPT-5.4): VERIFIED, Round 1, all four deliverables
  met, no defects. Build/pack/29 CrudSlice tests/allowlist/consumer-gate all green.
