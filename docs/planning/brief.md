# Brief: dabbler-platform under the v3 Dabbler workflow

> The operator's brief for Session 1 (author or import the plan) and Session 2
> (challenge it and break it into sessions). Written 2026-10-03, the day the
> repo was moved from the retired v1 orchestration workflow to the Dabbler AI
> Orchestration extension 3.19.x. The substance here is the operator's.

## What this repository is

A set of reusable .NET 10 libraries, templates and tools for building secure,
data-driven Blazor Server applications: `Dabbler.Components`,
`Dabbler.Api.Querying`, `Dabbler.Security` (+ `.Blazor`, `.DevIdp`),
`Dabbler.Reports.Core` / `.Export` / `.Blazor`, `Dabbler.Testing.Blazor`, the
`Dabbler.Platform.Stubs*` providers, the `Dabbler.Templates` package
(`dabbler-app`, `dabbler-min`, `dabbler-crud-service`, `dabbler-crud-page`),
and two dotnet tools (`Dabbler.CrudSlice.Tool`, `Dabbler.Security.DevIdp.Tool`).
AcmeCoffeeOnline under `src/Apps/` is the reference application the libraries
are proven against. `docs/platform-overview.md` is the canonical description;
`docs/planning/phase-3-platform-roadmap.md` and `docs/planning/project-status.md`
are the existing plan material to import.

Published: all packages at `0.1.0-preview.1` on nuget.org **except
`Dabbler.CrudSlice.Tool`**, which became packable after the only release tag
(`nuget-v0.1.0-preview.1`, 2026-05-29) and so was never pushed.

## Who consumes it

- `D:\Projects\dabbler-access-migration` (the Access-to-Blazor migration
  solution) consumes `Dabbler.Templates` and `Dabbler.CrudSlice.Tool` as
  build-time dotnet tools, and the generated applications consume the runtime
  packages. This is the consumer whose needs drive the next platform work.
- `D:\Projects\Dabbler.NorthwindToBlazor` consumes eight runtime packages.
- Production split of a generated app: Blazor Server web tier calling a Web
  API tier; only the API tier reaches SQL Server. The platform must not fight
  that split.

## What was done in the migration commit (2026-10-03)

- `dabbler bootstrap` scaffolded `AGENTS.md` (managed fence), `CLAUDE.md`,
  `dabbler.yaml`, `docs/sessions/session-plan.md`, the `.dabbler/` ignore rule
  and the pre-commit guard.
- `dabbler.yaml` declares one suite, `unit`, over the new `Dabbler.Unit.slnf`
  (every test project except Playwright and Packaging.Smoke). No `packaging`
  block yet: the managed instructions say the release session adds it, and the
  intended block (`pack` through `tools/pack.ps1`, no `push`) is left there as a
  comment. nuget.org publishing stays with the `nuget-v*` tag workflow and
  trusted publishing.
- `solution-dependencies.json` declares the only sibling dependency,
  `Dabbler.Uat.Dsl.*` from `../dabbler-uat-dsl/pack-output`, and `dabbler deps
  scaffold` wrote the matching membership file into `../dabbler-uat-dsl`
  (uncommitted there; that repo has no remote). `dabbler deps check` still
  reports `feed-not-configured` because the `dabbler-uat-dsl-local` source is
  declared only in the Playwright project's own `nuget.config`, not at the
  root or machine level; the check reads those. See decision 6.
- Removed the v1 relics: `ai_router/`, `requirements.txt`, the session Python
  scripts, `GEMINI.md`. Untracked `pack-output/` (61 nupkgs were committed),
  `tmp/` and the root `devidp.db*`.
- Added `Dabbler.Reports.Export`, `Dabbler.Reports.Blazor` and
  `ComponentCatalogCrawler.Tests` to the solution; CI had been building the
  first two by path because they were missing from it.

## Decisions the plan must make (Session 2 challenges each)

1. **`uat_runner/`.** The v1 Python package that turned a human UAT checklist
   into generated Playwright tests and triaged failures through the router. It
   cannot run (no router, hard-coded `../dabbler-uat-dsl` CLI path). Its six
   generated test files are committed and still build. Options: port the
   checklist-to-Playwright emission to a dotnet tool in `dabbler-uat-dsl`
   (where the DSL already lives) and drop triage; or retire it and keep the
   generated tests as ordinary Playwright tests. The operator leans to retire
   from this repo and let `dabbler-uat-dsl` own any port.
2. **Playwright and Packaging.Smoke as manual gates.** Neither is in
   `dabbler.yaml` because both need infrastructure (Docker stack; packed nupkgs
   plus SQL LocalDB) and the Playwright tests have no skip guard. Either add an
   environment-gated skip so they can join a suite, or keep them manual and say
   so in each session that touches the reference app.
3. **Publish `Dabbler.CrudSlice.Tool`.** A release session: bump
   `DabblerVersion` to `0.1.0-preview.2`, tag `nuget-v0.1.0-preview.2`, let CI
   publish, then pin that version in the migration solution's playbook. This
   unblocks the Access migration walking skeleton and should be the first
   numbered session.
4. **Stale docs.** `docs/planning/access-to-blazor-cli-spec.md` describes an
   extractor/generator design that was never built and names projects that do
   not exist; archive it. `docs/planning/project-guidance.md` and
   `lessons-learned.md` carry a banner pointing readers past their v1 sections;
   a later session can prune them.
5. **Branches.** `pre-role-rearchitecture` and `archive/pre-role-cleanup-master`
   are ancestors of master kept as a safety net until the role re-architecture
   finished; it did. Delete them or say why not.
6. **The `dabbler-uat-dsl` feed.** The Playwright project restores two
   `Dabbler.Uat.Dsl.*` packages from a relative folder in a sibling checkout.
   That works on one machine and fails on any other (CI builds the solution
   filter to avoid it). Options: publish `dabbler-uat-dsl` to nuget.org like
   every other Dabbler package and drop the local source (operator's
   preference; it also lets the source move to the root `nuget.config` so
   `dabbler deps check` can see it); or keep the sibling feed and accept the
   `feed-not-configured` report. Moving the relative source to the root
   `nuget.config` without publishing would break CI restore (NU1301 on a
   missing local source), so do not do that.

## Constraints

- All work runs through the extension; sessions are cross-provider verified.
- Do not over-engineer. The platform is a dependency of the Access migration
  work, not the product; prefer the smallest change that unblocks a consumer.
- Human developers supervise. Every generated artifact must be readable and
  overridable.

---

## Addendum 2026-11-07 — sustainment priorities, for Session 2

Written by the operator on 2026-11-07, five weeks after the brief above.
Session 2 reads it beside `docs/planning/solution-plan.md`, which was amended
the same day (success criteria 6–10, deferred cuts 8–13, Phases 2 and 5).

### Why

The operator retires in roughly four years. The staff who inherit the
Access-to-Blazor migration must either keep these libraries alive or be able
to copy them in and own them. Both outcomes need the same things: public
source (done 2026-11-07 for `dabbler-platform` and `dabbler-pdf`), currency
with the UI framework the libraries sit on, a surface small enough for one
person to maintain, a report engine that does not depend on that UI
framework, and one document a stranger can release from.

A second AI's advice to "use `DabblerPdf` and `Dabbler.Testing.Blazor` but not
`Dabbler.Components`, `Dabbler.Api.Querying` or `Dabbler.Reports.Blazor`
because they are pinned to MudBlazor 8.15" was checked and is half right: the
published packages declare a **floor** (`>= 8.15.0`), not a pin, and
`Dabbler.Api.Querying` has no MudBlazor dependency at all. The real risk is
that the UI libraries were compiled and tested against 8.15 and MudBlazor 9
removed the APIs they may call. Recompiling against 9.12 is the fix, and it is
cheap enough to do now.

### What changed in the repository today

- `dabbler bootstrap` re-run under extension 3.29.0; the managed block in
  `AGENTS.md` was refreshed (commit "Set up Dabbler", local).
- `docs/planning/solution-plan.md` amended as described above (local commit).
- Nothing pushed; the operator pushes after reading both.

### The decomposition Session 2 is asked to challenge

Each entry below is the shape the framework wants (`### Session <N>: <title>`,
register first, cross-provider verification, the suite once against the
verified tree, close-out last; at most ~3 work steps). Session 2 appends the
list it settles on to `docs/sessions/session-plan.md`; this addendum is the
operator's proposal, not the record.

- **Session 3: Declare packaging.** Write the `packaging:` block in
  `dabbler.yaml` — `pack` through `pwsh -NoProfile -File tools/pack.ps1
  -Version {version} -OutputDir {output} -CleanOutput`, no `push` — and run
  `dabbler packaging --rehearse`. Nothing else. (Phase 1.)
- **Session 4: Bump to `0.1.0-preview.2`.** Bump `DabblerVersion` in
  `Directory.Build.props`; add the change-log entry
  (`Dabbler.CrudSlice.Tool` first published). The operator then hand-tags
  `nuget-v0.1.0-preview.2` and CI publishes (cut 12). Pinning the version in
  the migration solution's playbook is that repository's session. (Phase 1.)
- **Session 5: Move Expressions into `Dabbler.Reports.Core`.** Move the nine
  files to `src/Libs/Dabbler.Reports.Core/Expressions/` under
  `Dabbler.Reports.Core.Expressions`; remove the `Dabbler.Components` project
  and package references from `Dabbler.Reports.Core.csproj`; fix usings in
  `BandLayoutEngine`, `LayoutEvaluationContext` and the tests; confirm
  `Dabbler.Platform.Stubs.Reports` still needs `Dabbler.Components` or drop it
  too. (Cut 8, Phase 2.)
- **Session 6: MudBlazor 9 — libraries.** `Directory.Packages.props`:
  `MudBlazor` `8.15.0` → `9.12.0`. Fix `Dabbler.Components` (`IGridDataProvider`
  / `GridDataProviderExtensions` take a `CancellationToken`; `FilterPanel`,
  `DeclarativeFilterPanel`, `DirtyAwareActionRow`), `Dabbler.Reports.Blazor`
  (`ParameterPromptForm` validation goes async), `Dabbler.Security.Blazor`
  (dialogs → `ShowAsync`, forms → `ValidateAsync`/`ResetAsync`, `MudTabs`
  class renames, `MudSelect.SelectedValues` read-only) and `.Compat`. bUnit
  suites in `Dabbler.Unit.slnf` green. (Cut 9, Phase 2.)
- **Session 7: MudBlazor 9 — reference app, templates, scaffolding.** Fix
  `src/Apps/AcmeCoffeeOnline.Web`, `templates/Dabbler.Templates/content/**`
  and `tools/Dabbler.CrudSlice/Templates/**`; `Dabbler.Templates.Tests` and
  `Dabbler.CrudSlice.Tests` prove generated apps build; record a manual
  Playwright run in the change-log. (Cut 9, Phase 2.)
- **Session 8: Surface reduction.** Delete `src/Libs/Dabbler.Components.Compat`
  and its tests; fold `Dabbler.Platform.Stubs.Components/.Reports/.Security`
  into `src/Libs/Dabbler.Platform.Stubs`; update `tools/pack.ps1`'s allowlist
  to twelve IDs, `Dabbler.Publish.slnf`, `Dabbler.Platform.sln`,
  `docs/packaging/release-checklist.md`, `Dabbler.PublicSurface.Tests`,
  `Dabbler.Packaging.Smoke`, `docs/platform-overview.md`. (Cut 10, Phase 2.)
- **Session 9: Support policy and maintainer guide.** Write
  `docs/MAINTAINING.md`; add `.github/dependabot.yml` (nuget, weekly,
  MudBlazor and `Microsoft.*` grouped); add the "If this library stops being
  maintained" section to the root README and the package READMEs. (Phase 2.)
- **Session 10: Bump to `0.2.0`.** Version bump and change-log naming the three
  breaking changes (sessions 5, 6, 8). Operator hand-tags `nuget-v0.2.0`
  unless the framework can name the tag by then, in which case this is
  `### Session 10: Release 0.2.0 (release: 0.2.0)`. After publication the
  operator unlists `Dabbler.Components.Compat` and the three
  `Dabbler.Platform.Stubs.*` providers on nuget.org. (Cut 11, Phase 2.)
- **Session 11: Honest gates.** Decide and implement cut 2 (Playwright and
  Packaging.Smoke: environment-gated skip or written manual gate) and the
  production-split architecture test. (Phase 3.)
- **Session 12: The `dabbler-uat-dsl` feed.** This repository's share of cut 6
  once `dabbler-uat-dsl` publishes or declines. (Phase 3.)
- **Session 13: Retire what does not run.** Cuts 1, 4, 5, 7 and 13:
  `uat_runner/`, stale planning docs, safety-net branches,
  `AcmeCoffeeOnline.Shared` in the solution, the `docs/session-sets/README.md`
  note. (Phase 4.)
- **Session 14: Release `1.0.0`.** `### Session 14: Release 1.0.0 (release:
  1.0.0)` if the framework can tag `nuget-v*` by then; otherwise a bump session
  and a hand tag. (Phase 5.)

Sessions 11–13 may run before 10 if a consumer is not waiting on `0.2.0`;
sessions 3–4 run first regardless, and 5 before 6.

### Outside this repository, in order

1. `dabbler-pdf`: bootstrapped onto 3.29.0 today; its own brief is at
   `docs/planning/brief.md` there. Its CI and OIDC publishing come before its
   next release, and `Dabbler.Reports.Export` pins whatever it ships.
2. `dabbler-ai-orchestration`: one session so `release: tag` can name its tag
   (cut 12). Until then every NuGet release here is a hand tag.
3. nuget.org: after `0.2.0`, unlist the four retired IDs. Co-ownership of the
   packages is a separate decision the operator has deferred.
