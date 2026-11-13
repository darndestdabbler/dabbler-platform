# Change Log — template-scaffolding-followup-fixes

> **Session set closed:** 2026-05-27
> **Sessions:** 3 of 3 (all VERIFIED)
> **Prerequisite sets:** `packaging-and-template-readiness`, `packaging-and-template-readiness-uat-remediation`, `crud-scaffolding`

This set stabilized the external scaffolding contract after real-consumer smoke validation exposed template-topology drift, stale wrapper documentation, and wrong-convention native CRUD template defaults. All three sessions passed cross-provider verification.

---

## Session 1 — Backfill Split Scaffold And CRUD Remediation

**What changed:**

- `dabbler-app` template: docs, metadata, and generated project files now explicitly reflect the split web/api/shared/tests topology. Api and Shared scaffolded projects are pinned to `net10.0`. Template `.csproj` excludes `bin/`/`obj/` content from the nupkg (collapsed the 24 MB on-disk leftover to 149 KB clean).
- `PackagingSmokeTests.cs`: topology regression gate added — `DabblerApp_Template_InstallsAndBuildsClean` now asserts the exact web/api/shared/tests layout rather than only building.
- CRUD wrapper docs (`tools/Dabbler.CrudSlice/README.md`): replaced dead `--output`/`--plural` flags with the real `--web-root`/`--api-root`/namespace flags enforced by `Dabbler.CrudSlice`.
- `dabbler-crud-page` native template: page generator now emits `Services:ApiBaseUrl` + `ApiBaseUrl` fallback + `X-Internal-Api-Key` header + `UserIdentityHandler` — matching the actual host HTTP wiring conventions.
- `dabbler-crud-service` native template: added `{Entity}ApiRegistrationExtensions.cs` (the one-line DI helper that was entirely missing).

**Verification:** VERIFIED (3 rounds — Rounds 1–2 surfaced artifact bookkeeping gaps; Round 3 clean).

---

## Session 2 — Resolve Minimal Template Contract

**What changed:**

- **Architecture decision (routed, Opus-tier):** `dabbler-min` stays intentionally single-project. Splitting it to match `dabbler-app` would produce two near-identical four-project solutions; the correct fix for consumer confusion is documentation clarity, not structural duplication.
- `dabbler-min` template source: added `DabblerApp/README.md` (single-project intent, board-constraint callout, CRUD wrapper same-root usage, outgrowing path); added 4-line intent comment to `Program.cs`.
- `dabbler-min` template metadata: rewrote description to explicitly state single-project shape; added `Framework` choice symbol matching `dabbler-app`.
- `docs/getting-started.md`: replaced scattered paragraphs with a pick-the-right-template comparison table plus an architecture-board callout.
- `docs/scaffolding-templates/README.md`: expanded same-root paragraph with explicit `dabbler-min` invocation guidance and a pointer to the `FullPipeline_endToEnd_against_smoke_fixture` regression gate.
- `PackagingSmokeTests.cs`: renamed `DabblerMin_Template_InstallsAndBuildsClean` → `DabblerMin_Template_InstallsAndBuildsClean_SingleProjectShape`; added `AssertDabblerMinSingleProjectShape` helper (positive: single `.csproj`, README emitted; negative: no Api/Shared/Tests projects, no `src/`/`tests/` directories).
- `WrapperOrchestrationTests.cs`: added XML doc comment to `FullPipeline_endToEnd_against_smoke_fixture` identifying it as the standing dabbler-min same-root regression gate.
- `docs/packaging/consumer-readiness.md` (out-of-scope-but-necessary): updated test-name reference to match the renamed smoke method.

**Verification:** VERIFIED (Round 1 — single round, zero findings).

---

## Session 3 — Remove Template Pack Warning And Close Out

**What changed:**

- `Apply-T1EntitySchema.ps1` (in `dabbler-crud-page` template content) was renamed to `Apply-T1EntitySchema.dabbler-ps` in the template source directory. NuGet's NU5110/NU5111 warnings fire on any `.ps1` file anywhere in a package; the file is not a NuGet install script but a post-scaffolding helper, so the extension change removes the false-positive warnings without suppressing them.
- `dabbler-crud-page/.template.config/template.json`: added a `sources[].rename` entry mapping the source `.dabbler-ps` file to `Apply-T1EntitySchema.ps1` on scaffold output. The existing `entity`/`entityPlural` `fileRename` symbols substitute `T1Entity`/`T2EntityPlural` tokens in the renamed output path, producing the correct `Apply-{Entity}Schema.ps1` in the developer's project.
- `Dabbler.Templates` now packs with zero NU5110/NU5111 warnings. Only the informational NETSDK1057 preview-SDK message and the readme advisory remain (both expected and benign for a template package).

**Verification:** VERIFIED — Gemini Pro (cross-provider), Round 1, zero findings.

---

## Deferred / Out-of-Scope

The following items were explicitly identified but are out of scope for this set:

- **Scaffolded API host completeness:** The `dabbler-app` scaffolded API host still wires data directly from the web host rather than being a production-complete standalone API. Making the API host fully independent (separate EF context, DI chain, migrations) is a follow-up architecture concern.
- **`UserIdentityHandler` registration:** The native page template now emits `AddHttpClient<...>().AddHttpMessageHandler<UserIdentityHandler>()`, but `UserIdentityHandler` itself is a platform type that must be registered by the consuming web app. This is documented but not scaffolded.
- **Readme warning in NuGet pack output:** `The package Dabbler.Templates is missing a readme` advisory remains. Adding a `README.md` to the template package is a cosmetic improvement deferred to a future packaging pass.
- **NETSDK1057:** Preview .NET SDK message; not actionable until the SDK goes GA.
