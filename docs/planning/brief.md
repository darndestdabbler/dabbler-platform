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
