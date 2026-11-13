# Session plan

> **Purpose:** the numbered sessions this repository runs, in order. The
> first two set the project up; everything after them is the work.
> **Workflow:** Full

---

## Sessions

### Session 1: Author or import the solution plan

1. Register.
2. The brief is the prompt this session was started with, or
   `docs/planning/brief.md`: what the solution is — its purpose, who uses
   it, what it must do and what is out of scope, what success looks like,
   and whether a plan or notes already exist. With neither, ask the person
   who started the session for it and write their answer to that path
   before any work. The plan's substance is theirs: do not search
   neighbouring directories for one, and do not draft one from the folder
   name. Before any module is proposed, ask them how production is split:
   what runs separately in production, and which part may talk to the
   database? Offer the default -- an application tier that calls an API
   tier, and only the API tier talks to the database. Then create — or
   import — `docs/planning/solution-plan.md`: the
   objective a reader can act on; the *Production split* they answered;
   the *Handoff artifacts*, what each separately running part is handed
   over as, with the standard command for its form -- for .NET an IIS site
   package (`dotnet publish`) or a Windows Service (a Worker Service
   published for a service host), for Java a runnable jar or a war (`mvn
   package` writing into `{output}`, which `packaging.pack` requires --
   say through a property the POM's jar or war plugin takes as its
   output directory, `-Dhandoff.dir={output}`) or an image (Spring
   Boot's `build-image`), for the database a
   SQL migration script (`dotnet ef migrations script --idempotent`); the
   real form is produced later, and a tutorial produces the easiest one to
   test; the modules, with what each is
   responsible for, the contract each exposes (what must be true going
   in, what is guaranteed coming out, how it fails), the dependency
   direction, the reason for each cut and the cuts deferred — one module
   is a fine answer, and most small solutions are one; and the phases or
   feature areas with their key deliverables, each scoped to a handful of
   focused AI sessions. A repository that already has
   `docs/planning/project-plan.md` amends that file instead.
3. Where the plan has several modules, say in it how each is a project:
   a `.csproj` listed in the root solution file for .NET, or a Maven module
   listed in the parent `pom.xml`, each reaching a sibling by project
   reference -- a `<ProjectReference>`, or a dependency at
   `${project.version}`. The first numbered session, the skeleton, writes
   the projects.
4. Cross-provider verification.
5. Full test suite, recorded as the run of record.
6. Close-out.

This session writes the plan and nothing else: it creates, edits or
deletes no code, project, build file, test, `dabbler.yaml` suite or
packaging.

**Creates:** `docs/planning/solution-plan.md`. A later revision is just
another plan session that amends the same file.

### Session 2: Challenge the plan, then break it into numbered sessions

1. Register.
2. Read `docs/planning/solution-plan.md` (or `docs/planning/project-plan.md`
   where that is the file this repository keeps its plan in) and challenge
   its cuts: for each
   module, whether it should exist, whether its contract is a promise
   something can prove, and whether the dependency direction hides the
   decisions most likely to change; decide each deferred cut or say why it
   stays deferred. Record the answers in the plan's rationale.
3. Break the plan into numbered sessions appended to this file. Each
   session is a focused unit of
   work one AI coding session can complete: one
   `### Session <N>: <title>` heading, and its steps as a top-level
   ordered list. Step 1 registers the session; the last steps are
   cross-provider verification, the complete suite once against the
   verified tree, and close-out; the middle steps are the work. Never
   write a step that says "run the tests" without saying which run it
   means. Order sessions so earlier ones unblock later ones — a module
   before the modules that depend on it — and keep at most ~3 work steps
   per session.

   The first numbered session is the skeleton. For .NET it writes the
   solution file and every project the plan names, with their
   references; for Maven or Gradle, the parent build and every module.
   Either way it declares the test suite in `dabbler.yaml` under
   `testing.suites`, even before any tests exist, with `expensive: true`
   where its run is the run of record and a hang limit in its command --
   `dotnet test --blame-hang-timeout 5m`, or Surefire's
   `forkedProcessTimeoutInSeconds` -- so every later run inherits it,
   and adds the architecture test the plan requires -- where only the
   API tier may reach the database, that rule. Later sessions fill the
   modules in and never re-create what the skeleton made.

   Packaging and releases are sessions of their own. Where the plan
   names *Handoff artifacts*, plan one packaging session -- early, with
   project files only, or later -- that writes the `packaging:` block in
   `dabbler.yaml` and does nothing else. A release is a session with no
   steps, headed `### Session <N>: Release <version> (release:
   <version>)`; propose its version -- patch, minor or major by what the
   sessions it releases change -- for the person to approve with the plan.
4. Cross-provider verification.
5. Full test suite, recorded as the run of record.
6. Close-out.

This session writes the plan and nothing else: it creates, edits or
deletes no code, project, build file, test, `dabbler.yaml` suite or
packaging.

**Creates:** the numbered session list the rest of this repository runs,
starting with the skeleton session.

> Do NOT hand-author `sessions.json`. The first `session start`
> bootstraps it from this plan — state files are the writers' job.

> **Sessions 3–14** carry out the decisions in *Rationale — Session 2
> challenge* in `docs/planning/solution-plan.md`. Every session's run of
> record is the `unit` suite declared in `dabbler.yaml` (`dotnet test
> Dabbler.Unit.slnf -m:1 --blame-hang-timeout 5m`).
>
> Some gates are not in that suite (cut 2):
> - The **Playwright E2E run** is a manual gate. It runs against the compose
>   stack rebuilt from the tree: `docker compose -f
>   docker/docker-compose.infra.yml up -d`, then `docker compose -f
>   docker/docker-compose.app.yml up --build -d`, then `dotnet test
>   tests/AcmeCoffeeOnline.Playwright/`.
> - **`Dabbler.PublicSurface.Tests` and `Dabbler.Packaging.Smoke`** are CI's
>   gate on every `nuget-v*` tag, in `.github/workflows/build.yml`.
>
> Releases are bump sessions followed by the operator's hand tag (cut 12):
> the router cannot yet name a `nuget-v*` tag.

### Session 3: Skeleton — complete the existing solution and prove the production split

1. Register.
2. List `src/Apps/AcmeCoffeeOnline.Shared/AcmeCoffeeOnline.Shared.csproj`
   in `Dabbler.Platform.sln` (cut 7). The rest of the skeleton already
   exists: the solution, every other project with its `<ProjectReference>`s,
   and the `unit` suite in `dabbler.yaml` (already `expensive: true`, with
   `--blame-hang-timeout 5m` in its command). Adopt it and re-create none
   of it.
3. Add the production-split architecture test,
   `tests/AcmeCoffeeOnline.Web.Tests/ProductionSplitTests.cs`, which runs
   in the `unit` suite. It asserts two things:
   - no type in the `AcmeCoffeeOnline.Web` assembly derives from
     `DbContext`;
   - `AcmeCoffeeOnline.Web.csproj` names no `Microsoft.EntityFrameworkCore*`
     package.

   It makes no transitive-assembly assertion: `Dabbler.Security` carries
   EF Core by design (see the plan's rationale).
4. Cross-provider verification.
5. The `unit` suite once against the verified tree, recorded as the run of
   record.
6. Close-out.

This session changes no suite and no packaging.

### Session 4: Declare packaging

1. Register.
2. In `dabbler.yaml`, replace the commented-out intent with the `packaging:`
   block: `pack.argv: ["pwsh", "-NoProfile", "-File", "tools/pack.ps1",
   "-Version", "{version}", "-OutputDir", "{output}", "-CleanOutput"]`. It
   has no `push` and no `release:` member, because CI publishes from the
   operator's `nuget-v*` tag. The step's check is `dabbler packaging
   --rehearse --version 0.1.0-preview.2`.
3. Cross-provider verification.
4. The `unit` suite once against the verified tree, recorded as the run of
   record.
5. Close-out.

This session writes the `packaging:` block and nothing else.

### Session 5: Bump to 0.1.0-preview.2

1. Register.
2. Set `DabblerVersion` in `Directory.Build.props` to `0.1.0-preview.2`, and
   change the `-Version` default in `tools/pack.ps1` to match. Create the
   root `CHANGELOG.md` with the `0.1.0-preview.2` entry: it changes no API,
   and `Dabbler.CrudSlice.Tool` is published for the first time.
3. Cross-provider verification.
4. The `unit` suite once against the verified tree, recorded as the run of
   record.
5. Close-out.

After the session has pushed, the operator:
1. runs the Playwright E2E gate;
2. hand-tags `nuget-v0.1.0-preview.2` (`git tag -a nuget-v0.1.0-preview.2 -m
   "Release 0.1.0-preview.2"` and `git push origin nuget-v0.1.0-preview.2`).

CI's packaging job then runs `Dabbler.PublicSurface.Tests` and
`Dabbler.Packaging.Smoke` before it publishes. *Done when* `dotnet tool
install --global Dabbler.CrudSlice.Tool --version 0.1.0-preview.2` succeeds
on a clean machine. Pinning that version in the migration solution's
playbook is that repository's session.

### Session 6: Move Expressions into Dabbler.Reports.Core

1. Register.
2. Move the nine files in `src/Libs/Dabbler.Components/Expressions/` to
   `src/Libs/Dabbler.Reports.Core/Expressions/` under the namespace
   `Dabbler.Reports.Core.Expressions`, with no change in behaviour. Fix the
   usings in `BandLayoutEngine.cs`, `LayoutEvaluationContext.cs` and every
   other caller, tests included.
3. In `Dabbler.Reports.Core.csproj`, remove both `Dabbler.Components`
   references (the `<ProjectReference>` and the pack-time
   `<PackageReference>`), and remove `InternalsVisibleTo
   Dabbler.Components.Tests` if nothing needs it any more. Update the
   dependency statements in the Reports.Core README and in
   `docs/platform-overview.md`. `Dabbler.Platform.Stubs.Reports` keeps its
   `Dabbler.Components` reference, because it uses `Data.Metadata`.
4. Cross-provider verification.
5. The `unit` suite once against the verified tree, recorded as the run of
   record. `Dabbler.Reports.Blazor.Tests` is the engine's coverage.
6. Close-out.

### Session 7: Surface reduction — 16 package IDs to 12

1. Register.
2. Delete `src/Libs/Dabbler.Components.Compat` and its tests. Remove its
   references from `Dabbler.Components.Tests`, `Dabbler.PublicSurface.Tests`,
   `Dabbler.Platform.sln` and `Dabbler.Publish.slnf`.
3. Fold the source of `Dabbler.Platform.Stubs.Components`, `.Reports` and
   `.Security` into `src/Libs/Dabbler.Platform.Stubs`, keeping their
   namespaces so that registration calls compile unchanged. Delete the
   three projects, and update the solution, the solution filters and every
   test project that referenced them.
4. Bring everything else to the twelve IDs:
   - the allowlist and project list in `tools/pack.ps1`;
   - the publish order in `docs/packaging/release-checklist.md`;
   - the expectations in `Dabbler.PublicSurface.Tests` and
     `Dabbler.Packaging.Smoke`;
   - `docs/platform-overview.md`.

   The step's checks are `pwsh tools/pack.ps1 -OutputDir pack-output
   -CleanOutput`, then `dotnet test tests/Dabbler.PublicSurface.Tests/`
   (which runs instead of skipping, because the packages are present), then
   `dotnet build tests/Dabbler.Packaging.Smoke/`. The Smoke run itself
   stays CI's gate, at the `0.2.0` tag.
5. Cross-provider verification.
6. The `unit` suite once against the verified tree, recorded as the run of
   record.
7. Close-out.

### Session 8: MudBlazor 9 — libraries and the reference app compile

1. Register.
2. In `Directory.Packages.props`, move `MudBlazor` from `8.15.0` to `9.12.0`.
   Then fix `Dabbler.Components` and `Dabbler.Reports.Blazor`:
   - `IGridDataProvider` and `GridDataProviderExtensions` take a
     `CancellationToken`, which is a public API change; update their tests;
   - `FilterPanel`, `DeclarativeFilterPanel` and `DirtyAwareActionRow`;
   - `ParameterPromptForm`'s validation goes async.
3. Fix `Dabbler.Security.Blazor`:
   - dialogs move to `ShowAsync`;
   - forms move to `ValidateAsync` and `ResetAsync`;
   - the `MudTabs` class parameters are renamed;
   - `MudSelect.SelectedValues` is now read-only.
4. Fix `src/Apps/AcmeCoffeeOnline.Web` so that it and
   `AcmeCoffeeOnline.Web.Tests` build and pass. The pin is central, so the
   whole of `Dabbler.Unit.slnf` must compile by the end of this session.
5. Cross-provider verification.
6. The `unit` suite once against the verified tree, recorded as the run of
   record.
7. Close-out.

### Session 9: MudBlazor 9 — templates, scaffolding and the E2E gate

1. Register.
2. Move `templates/Dabbler.Templates/content/**` to MudBlazor `9.12.0` and
   fix the generated code. It is proven by `Dabbler.Templates.Tests` with the
   packages present: the step's checks are `pwsh tools/pack.ps1 -OutputDir
   pack-output -CleanOutput`, then `dotnet test tests/Dabbler.Templates.Tests/`.
3. Move `tools/Dabbler.CrudSlice/Templates/**` to MudBlazor 9 in the same
   way. It is proven by `dotnet test tests/Dabbler.CrudSlice.Tests/` after the
   same pack.
4. Run the Playwright E2E manual gate against the compose stack rebuilt from
   this tree, fix what it finds in `AcmeCoffeeOnline.Web`, and record the
   result in the step's notes. It restores `Dabbler.Uat.Dsl.*` from
   `../dabbler-uat-dsl/pack-output` (cut 6 is still deferred).
5. Cross-provider verification.
6. The `unit` suite once against the verified tree, recorded as the run of
   record.
7. Close-out.

### Session 10: Maintainer guide, gate rule and support policy

1. Register.
2. Write `docs/MAINTAINING.md`. It covers:
   - how to clone, build, test (the `unit` suite) and pack;
   - how to release: a bump session, then the hand tag `nuget-v<version>`,
     then unlisting retired IDs;
   - where every gate lives: the `unit` suite in `dabbler.yaml`;
     PublicSurface and Packaging.Smoke in CI's packaging job; and the
     Playwright manual gate before each release tag and in every session
     that changes the reference app's UI or the MudBlazor pin;
   - the MudBlazor policy: adopt a new major within one quarter of its
     release.

   Also point the *Building and testing* part of `AGENTS.md` (outside the
   managed fence) and
   `docs/planning/session-set-authoring-guide.platform-addendum.md` at the
   gate rule.
3. Add `.github/dependabot.yml`: nuget, weekly, with `MudBlazor` in one group
   and `Microsoft.*` 10.x in another.
4. Add an "If this library stops being maintained" section to `README.md`
   and to each of the twelve package READMEs. It names the fallback: copy
   the MIT source into your application and own it.
5. Cross-provider verification.
6. The `unit` suite once against the verified tree, recorded as the run of
   record.
7. Close-out.

### Session 11: Bump to 0.2.0

1. Register.
2. Set `DabblerVersion` in `Directory.Build.props`, and the `-Version`
   default in `tools/pack.ps1`, to `0.2.0`. Add the `0.2.0` entry to
   `CHANGELOG.md`, naming the three breaking changes:
   - the Expressions namespace is now `Dabbler.Reports.Core.Expressions`;
   - the MudBlazor 9 floor, with `IGridDataProvider`'s `CancellationToken`;
   - `Dabbler.Components.Compat` and the three `Dabbler.Platform.Stubs.*`
     providers are retired, the providers folded into
     `Dabbler.Platform.Stubs`.
3. Cross-provider verification.
4. The `unit` suite once against the verified tree, recorded as the run of
   record.
5. Close-out.

After the session has pushed, the operator:
1. runs the Playwright E2E gate;
2. hand-tags `nuget-v0.2.0` (CI's packaging job runs PublicSurface and
   Packaging.Smoke before it publishes);
3. once `0.2.0` is listed, unlists (never deletes)
   `Dabbler.Components.Compat` and the three `Dabbler.Platform.Stubs.*`
   providers on nuget.org.

### Session 12: Retire what does not run

1. Register.
2. Delete `uat_runner/` (cut 1). The generated tests under
   `tests/AcmeCoffeeOnline.Playwright/Generated/` stay as ordinary
   Playwright tests. Remove `uat_runner` from the project section of
   `AGENTS.md` and from `README.md`.
3. Move `docs/planning/access-to-blazor-cli-spec.md`,
   `docs/planning/project-status.md`, `CRUD_SCAFFOLDING_FINAL_SUMMARY.md` and
   `FEEDBACK_REMEDIATION_SUMMARY.md` to `docs/planning/_archived/` (cut 4),
   and fix every link that pointed at them.
4. Add `docs/session-sets/README.md`: one paragraph saying the 47
   directories are read-only v1 history (cut 13). Correct the count in
   `AGENTS.md`.
5. Cross-provider verification.
6. The `unit` suite once against the verified tree, recorded as the run of
   record.
7. Close-out.

Deleting the two safety-net branches (cut 5) is the operator's action, not
this session's.

### Session 13: Prune the v1 guidance

1. Register.
2. Prune `docs/planning/project-guidance.md`. Remove the sections that
   describe the retired v1 workflow (`route()`, `session-state.json`,
   `ai-assignment.md`, session sets), keep the testing, UAT, MudBlazor, CRUD
   and code-style rules, and drop its banner.
3. Prune `docs/planning/lessons-learned.md` the same way. Keep the
   repository-specific failure patterns, remove what only describes the
   retired router, and drop its banner.
4. Remove the v1 caveat from `AGENTS.md`'s *Required reading* list.
5. Cross-provider verification.
6. The `unit` suite once against the verified tree, recorded as the run of
   record.
7. Close-out.

### Session 14: Bump to 1.0.0

1. Register.
2. Set `DabblerVersion` in `Directory.Build.props`, and the `-Version`
   default in `tools/pack.ps1`, to `1.0.0`. Add the `1.0.0` entry to
   `CHANGELOG.md`: it changes no API since `0.2.0`, and states that the
   twelve packages, the gates and `docs/MAINTAINING.md` are what the
   operator stands behind.
3. Cross-provider verification.
4. The `unit` suite once against the verified tree, recorded as the run of
   record.
5. Close-out.

After the session has pushed, the operator runs the Playwright E2E gate and
hand-tags `nuget-v1.0.0`. If the router can name a `nuget-v*` tag by then
(cut 12), a plan session first turns this session into a release session.

### Session 15: Build on push

1. Register.
2. Add `.github/workflows/ci.yml`: on `push` to `master` and on
   `pull_request`, one `windows-latest` job that sets up the .NET 10 SDK and
   runs the `unit` suite exactly as `dabbler.yaml` declares it
   (`dotnet test Dabbler.Unit.slnf -m:1 --blame-hang-timeout 5m`), with a
   `concurrency` group per ref that cancels in-progress runs and
   `permissions: contents: read`. `build.yml` (the `nuget-v*` publish) is not
   touched. The step's check is `dotnet test Dabbler.Unit.slnf -m:1
   --blame-hang-timeout 5m`, the same command the job runs; the first green
   run on `master` after the session lands is the operator's confirmation.
3. Make the change-log honest: under `0.1.0-preview.2` and `0.2.0` in
   `CHANGELOG.md`, add one line each saying the version was never published
   to nuget.org and that `1.0.0` is the first release after
   `0.1.0-preview.1`. In `docs/MAINTAINING.md`, "Where every gate lives"
   names the push build as the gate on `master` and on Dependabot pull
   requests, and "Dependencies and the MudBlazor policy" says a Dependabot
   pull request merges only on a green check.
4. Cross-provider verification.
5. The `unit` suite once against the verified tree, recorded as the run of
   record.
6. Close-out.

Why now (2026-11-08): `build.yml` runs only on a `nuget-v*` tag or a manual
dispatch, so every push since May, all fourteen sessions included, was tested
only on the operator's machine, and the five open Dependabot pull requests
carry no checks. Requiring the check before merge is a branch-protection
setting the operator makes on GitHub after the first green run; it is not
this session's work.

### Session 16: Templates reference the version being packed

1. Register.
2. Every `Dabbler.*` `PackageReference` under
   `templates/Dabbler.Templates/content/**` (the `dabbler-app` Web and
   Web.Tests projects and `dabbler-min`) hard-pins `0.1.0-preview.1`. Make
   them carry the version being packed: a token that the pack target in
   `templates/Dabbler.Templates/Dabbler.Templates.csproj` rewrites to
   `$(DabblerVersion)`, or a `.template.config` symbol whose default is
   stamped the same way, whichever the templating engine supports without a
   second source of truth. The step's check: `pwsh tools/pack.ps1 -OutputDir
   pack-output -CleanOutput`, then unzip the packed `Dabbler.Templates`
   nupkg and search it for `0.1.0-preview.1`; nothing is found.
3. Bring the scaffolded-app build into a local gate, because
   `Dabbler.Templates.Tests` builds only the CRUD templates and
   `tests/Dabbler.Packaging.Smoke` is outside `Dabbler.Unit.slnf`: add a
   `smoke` suite to `dabbler.yaml` (`dotnet test
   tests/Dabbler.Packaging.Smoke` with `DABBLER_PACK_OUTPUT` pointing at the
   pack output, `expensive: true`), and say in `docs/MAINTAINING.md` that a
   bump session runs it. The step's check is that suite, green against the
   output of step 2.
4. Cross-provider verification.
5. The `unit` suite once against the verified tree, recorded as the run of
   record.
6. Close-out.

Why now (2026-11-08): the `workflow_dispatch` dry run of `build.yml` for
`1.0.0` (run 42482135732) failed in "Run consumer smoke tests": the
scaffolded `dabbler-app` restored the May `0.1.0-preview.1` binaries from
nuget.org beside MudBlazor 9.12.0 and failed with `CS0030` on the grid
delegate. Session 9 moved the templates to MudBlazor 9 but not their package
pins, and no local gate covered the gap. A `nuget-v1.0.0` tag would fail the
same way. After this session lands, the operator re-runs the dry run
(`gh workflow run build.yml -f version=1.0.0`) and tags only when it is
green.

### Session 17: Make the push build green on a clean machine

1. Register.
2. Stop the `AcmeCoffeeOnline.Api.Tests` race on a fresh LocalDB. Every test
   class in that project owns a `WebApplicationFactory<Program>` that, in the
   Development environment, runs `DatabaseInitializer.InitializeAsync`
   (`EnsureCreatedAsync` plus seed) against the single database
   `AcmeCoffeeOnline_Dev`; xUnit runs those classes in parallel, so on a
   machine where the database does not yet exist several factories create
   and seed it at once ("Database 'AcmeCoffeeOnline_Dev' already exists",
   duplicate-key inserts into Customers, AppOrganizations, AppUsers,
   AppUserRoles). The operator's machine has had that database since
   2026-04-13, which is why the suite is green locally. Fix: add
   `tests/AcmeCoffeeOnline.Api.Tests/xunit.runner.json` with
   `"parallelizeTestCollections": false` (copied to output), so the first
   class creates and seeds the database and the rest find it. The step's
   check: drop the database (`sqllocaldb` or `DROP DATABASE
   AcmeCoffeeOnline_Dev`) and run `dotnet test
   tests/AcmeCoffeeOnline.Api.Tests` twice; both runs 51/51.
3. Make the push build run the 46 tests it currently skips.
   `Dabbler.PublicSurface.Tests` (41), `Dabbler.Templates.Tests` (4) and
   `Dabbler.CrudSlice.Tests`' `FullPipeline_endToEnd_against_smoke_fixture`
   (1) are `SkippableFact`/`SkippableTheory` tests that skip when
   `DABBLER_PACK_OUTPUT` (or the default `pack-output/` folder) holds no
   nupkgs, so on the runner they report skipped and the job stays green. In
   `ci.yml`, before the test step, build the package set the way
   `build.yml`'s *Validate And Pack* job does (`dotnet build
   Dabbler.Publish.slnf --configuration Release`, then
   `Dabbler.Reports.Export` and `Dabbler.Reports.Blazor` directly, because
   `pack.ps1` packs with `--no-build` and the filter does not lay down their
   output), run `tools/pack.ps1` with `-OutputDir` and `-ScratchCacheDir`
   under `$env:RUNNER_TEMP`, and export that output folder as
   `DABBLER_PACK_OUTPUT` for the test step. The check: the run's summary
   lines for those three projects show `Skipped: 0`, with PublicSurface at
   41 passed and Templates at 4.
4. Cross-provider verification.
5. The `unit` suite once against the verified tree, recorded as the run of
   record.
6. Close-out.

Why now (2026-11-08): the first `ci.yml` run on `master` (run 42486813518,
after Session 15 closed) failed with 10 Api.Tests failures, and the
pack-dependent tests were silently skipped (41 in PublicSurface, 4 in
Templates, 1 in CrudSlice). Both are properties of a clean machine, which is
exactly what the push build exists to represent; until they are fixed the
check Session 15 added says nothing reliable and cannot be made a
branch-protection requirement.

### Session 18: Scaffolded-app restores take their sources from nuget.config

1. Register.
2. Stop passing nuget.org to `dotnet restore` on the command line. Four
   restores of a scaffolded app pass `--source "<pack output>" --source
   "https://api.nuget.org/v3/index.json"`:
   `tests/Dabbler.Templates.Tests/CrudServiceTemplateTests.cs` and
   `CrudPageTemplateTests.cs` (one each), and
   `tests/Dabbler.Packaging.Smoke/PackagingSmokeTests.cs` (in
   `DabblerMin_Template_InstallsAndBuildsClean_SingleProjectShape` and
   `DabblerApp_ScaffoldedHost_ServesBelowFiveHundredOnRootRequest`). The
   .NET 10.0.403 SDK on the runner resolves that URL as a path under the
   project directory (`NU1301: The local source
   '<tempdir>\https:\api.nuget.org\v3\index.json' doesn't exist`); the same
   defect on a `.slnx` is already recorded in
   `DabblerApp_Template_InstallsAndBuildsClean` in `PackagingSmokeTests.cs`,
   and the fix there was a local `nuget.config` and no inline source. Apply
   the same fix to all four: each test already writes, or (the
   `dabbler-min` restore) is made to write with the file's existing
   `WriteLocalNugetConfig`, a `nuget.config` that clears the sources and
   lists the pack output and nuget.org, so `dotnet restore` runs with no
   `--source`. No test changes what it asserts. The step's checks: no
   `--source "https://api.nuget.org` remains under `tests/`; then, with
   `DABBLER_PACK_OUTPUT` pointing at a fresh `tools/pack.ps1` output,
   `dotnet test tests/Dabbler.Templates.Tests` shows 4 passed and 0
   skipped, and `dotnet test tests/Dabbler.Packaging.Smoke --filter
   "FullyQualifiedName!~ServesBelowFiveHundred"` passes.
3. Cross-provider verification.
4. The `unit` suite once against the verified tree, recorded as the run of
   record.
5. Close-out.

Non-goals: no change to `ci.yml`, `build.yml` or the SDK the workflows
install, and no `global.json`; no change to the CRUD templates themselves.

Why now (2026-11-09): with Session 17's pack step in place, the push build on
`master` (run 42621003718) ran the pack-dependent tests for the first time:
Api.Tests 51/51, PublicSurface 41/41 and CrudSlice 29/29 with nothing
skipped, but three of the four `Dabbler.Templates.Tests` failed with the
`NU1301` above. The operator's machine passes because its default SDK is an
11.0 preview. `build.yml`'s *Validate And Pack* job runs the same smoke
restores on the same SDK, so a `nuget-v1.0.0` tag would fail there too. The
operator's confirmation is the first green push build on `master` after this
session lands; then the `1.0.0` dry run (`gh workflow run build.yml -f
version=1.0.0`) and the tag.

### Session 19: Take DabblerPdf 1.1.3

1. Register.
2. Set the `DabblerPdf` `PackageVersion` in `Directory.Packages.props` from
   `1.1.2` to `1.1.3`, the one place the platform pins it. The step's checks:
   `Directory.Packages.props` names `DabblerPdf` at `1.1.3` and nowhere at
   `1.1.2`; `dotnet test tests/Dabbler.Reports.Blazor.Tests` passes (it holds
   the PDF export tests); then, after a Release build and `tools/pack.ps1
   -OutputDir pack-output -CleanOutput`, the `.nuspec` inside the packed
   `Dabbler.Reports.Core` nupkg declares its `DabblerPdf` dependency at
   `1.1.3`.
3. Cross-provider verification.
4. The `unit` suite once against the verified tree, recorded as the run of
   record.
5. Close-out.

Non-goals: no change to `DabblerVersion`, `CHANGELOG.md` or any tag (shipping
this to consumers is a release of its own); no change to what any test
asserts; no change to `../dabbler-pdf`; the tracked `*.csproj.lscache` files
that still name `dabblerpdf/1.1.2` are left alone.

Why now (2026-11-09): DabblerPdf 1.1.3 is on nuget.org (its `nuget-v1.1.3`
publish, run 42666717096). It changes no API since 1.1.2 and is the first
DabblerPdf version built, validated and published from CI with trusted
publishing. The platform's 1.0.0 packages depend on 1.1.2, and NuGet resolves
the lowest version a dependency allows, so a Dabbler consumer gets 1.1.2
until a Dabbler release raises the floor.

### Session 20: Bump to 1.0.1

1. Register.
2. Set `DabblerVersion` in `Directory.Build.props`, and the `-Version`
   default in `tools/pack.ps1` (with the `(1.0.0)` its help text names as
   that default), to `1.0.1`. Add the `1.0.1` entry to `CHANGELOG.md`: no
   API changes since `1.0.0`; the `DabblerPdf` dependency moves from `1.1.2`
   to `1.1.3`, which changes no DabblerPdf API either. The step's checks:
   `Directory.Build.props` and `tools/pack.ps1` name `1.0.1` and no longer
   name `1.0.0` as the version or the default; `CHANGELOG.md` has a `## 1.0.1`
   heading above `## 1.0.0`; then, after a Release build and
   `tools/pack.ps1 -OutputDir pack-output -CleanOutput`, the smoke suite
   (`dotnet test tests/Dabbler.Packaging.Smoke --filter
   "FullyQualifiedName!~ServesBelowFiveHundred"` with `DABBLER_PACK_OUTPUT`
   pointing at that output) passes against the `1.0.1` packages.
3. Cross-provider verification.
4. The `unit` suite once against the verified tree, recorded as the run of
   record.
5. Close-out.

Non-goals: no API change and no dependency change beyond Session 19's; no
change to `ci.yml`, `build.yml` or `dabbler.yaml`.

After the session has pushed, the operator runs the Playwright E2E gate and
the `1.0.1` dry run (`gh workflow run build.yml -f version=1.0.1`), and tags
`nuget-v1.0.1` only when it is green. If the router can name a `nuget-v*` tag
by then, a plan session first turns this session into a release session.

Why now (2026-11-09): Session 19 raises the DabblerPdf floor in the source,
but nothing reaches a consumer until the twelve packages ship again at a new
version. A patch is the proposed version because neither the Dabbler API nor
the DabblerPdf API changes.

### Session 21: The framework runs the Playwright E2E gate

1. Register.
2. Add `tools/e2e.ps1`, the one command that runs the E2E gate from a cold
   start. It uses `podman` when it is on `PATH` and its machine is running,
   otherwise `docker` (`DABBLER_CONTAINER_ENGINE` overrides the choice), and
   fails with a plain message, not a test failure, when neither engine is
   up or `../dabbler-uat-dsl/pack-output` is missing. It brings
   `docker/docker-compose.infra.yml` up, drops the `AcmeCoffeeOnline_Dev`
   database so the API creates and seeds it fresh (the persistent volume and
   the other databases stay), rebuilds and starts
   `docker/docker-compose.app.yml`, waits for `api` and `web` to report
   healthy with a time limit, then runs `dotnet test
   tests/AcmeCoffeeOnline.Playwright/` with `--blame-hang-timeout` and a trx
   logger, and exits with the test run's exit code. The step's check runs
   the script and passes when the trx it wrote holds 178 tests of which no
   more than the 12 known failures failed: the run proves the gate, not the
   app.
3. Declare an `e2e` suite in `dabbler.yaml` that runs `tools/e2e.ps1`:
   `expensive: true`, `required_for_close: false` until Session 22 makes it
   green, covering `src/Apps`, `src/Libs`, `tests/AcmeCoffeeOnline.Playwright`,
   `docker` and `Directory.Packages.props`. Rewrite the places that call
   Playwright a manual gate the operator runs — `docs/MAINTAINING.md` (*Where
   every gate lives* and *Releasing*), the *Building and testing* part of
   `AGENTS.md` outside the managed fence, and
   `docs/packaging/release-checklist.md` — to say the framework runs it as the
   `e2e` suite and the operator only needs Podman or Docker running. The
   step's check: `dabbler.yaml` parses and declares the `e2e` suite, and none
   of the three documents still tells the operator to run Playwright by hand.
4. Cross-provider verification.
5. The `unit` suite once against the verified tree, recorded as the run of
   record.
6. Close-out.

Non-goals: no change to the app, the libraries or what any Playwright test
asserts (the 12 failures are Session 22's); no Playwright in `ci.yml` or
`build.yml` (CI has no `../dabbler-uat-dsl` feed); no change to the compose
files beyond what the script needs to drive them.

Why now (2026-11-09): the operator ran the gate after Session 20 under Podman
and got 161 passed, 12 failed, 5 skipped, the same counts as Session 9's
fresh-database baseline; nothing app-facing has changed since Session 9
except DabblerPdf 1.1.3, and no PDF test is among the 12. The operator wants
the framework, not a person, to run the gate, and wants the 12 fixed.

### Session 22: Fix the 12 Playwright E2E failures

1. Register.
2. Run the `e2e` suite (`tools/e2e.ps1`) on a fresh database and record the
   12 failing tests by name with each one's first error line. The ones seen
   on 2026-11-09 are three `OrderNestedItemsSaveTests` (item counts after the
   parent save), three `EdgeCaseTests.AccessDenied_BackofficeUser_*` (no
   access-denied page or card), `AdminUsersCrossLinksUat` and
   `UnifiedMasterDetailsCompositeUat` (no Admin button),
   `ComposableCrudHelpersUat` (no ACTIVATE button),
   `SensitiveFieldMaskingAuditUatRemediationUat` (no `XXX-XX-` mask),
   `ProductAdministrationWorkflowTests.E2E_WF_04` (product not found through
   the API) and `UsersAndRoleAssignmentWorkflowTests.CrossOrgUserAdmin_*`
   (no roles cell for row 14).
3. One step per root cause that can be fixed without a public API change:
   fix it where the cause is (the app, a library's internals, a test's setup
   or the seed data), each step's check being that cause's failing tests
   passing in a class run against the running stack. A cause whose fix needs
   a public API change is not fixed here: the step's notes name the tests,
   the cause and the API change it needs, for Session 23.
4. If no cause was left for Session 23, set the `e2e` suite's
   `required_for_close` to `true`, with the check that `tools/e2e.ps1` exits
   0 on a fresh database; otherwise it stays `false`.
5. If a fix changes a packed library, add a line saying what it fixes to the
   `1.0.1` entry in `CHANGELOG.md`.
6. Cross-provider verification.
7. The `unit` and `e2e` suites once against the verified tree, recorded as
   the run of record (`e2e` may fail only on the tests left for Session 23).
8. Close-out.

Non-goals: no test is fixed by weakening what it asserts, skipping it, or
adding it to `tools/uat-suppressions.json`; no public API change (that is
Session 23's); no change to `ci.yml` or `build.yml`.

If nothing was left for Session 23, the operator cancels it and, once this
session has pushed, runs the `1.0.1` dry run (`gh workflow run build.yml -f
version=1.0.1`) and tags `nuget-v1.0.1` when it is green. The version stays
the proposed patch, `1.0.1`: these are bug fixes.

Why now (2026-11-09): the release checklist says all Playwright tests are
green, and they have not been since before Session 9. `1.0.1` is not tagged
yet, so it ships with the fixes.

### Session 23: The E2E fixes that need a public API change

Run only if Session 22 left a cause for it; otherwise the operator cancels
this session.

1. Register.
2. One step per cause Session 22's notes left here: make the public API
   change in the library, fix the cause, and update the
   `Dabbler.PublicSurface.Tests` baseline for the change. Each step's check:
   that cause's failing tests pass in a class run against the running stack,
   and `dotnet test tests/Dabbler.PublicSurface.Tests` passes against a fresh
   `tools/pack.ps1 -OutputDir pack-output -CleanOutput`.
3. Set the version to `1.1.0`: `DabblerVersion` in `Directory.Build.props`
   and the `-Version` default in `tools/pack.ps1` with its help text. The
   unreleased `## 1.0.1` entry in `CHANGELOG.md` becomes `## 1.1.0`, keeps
   the DabblerPdf line and Session 22's fixes, and names each API addition.
   The step's checks: neither file names `1.0.1`; `CHANGELOG.md` has
   `## 1.1.0` directly above `## 1.0.0` and no `## 1.0.1`; and the smoke
   suite passes against the packed `1.1.0` packages.
4. Set the `e2e` suite's `required_for_close` to `true`. The step's check:
   `tools/e2e.ps1` exits 0 on a fresh database.
5. Cross-provider verification.
6. The `unit`, `smoke` and `e2e` suites once against the verified tree,
   recorded as the run of record.
7. Close-out.

Non-goals: only additive API changes (a breaking change stops the session,
and a plan session re-proposes the version as a major); no change to
`ci.yml` or `build.yml`.

After the session has pushed, the operator runs the `1.1.0` dry run (`gh
workflow run build.yml -f version=1.1.0`) and tags `nuget-v1.1.0` when it is
green; `1.0.1` is never tagged.

Why now (2026-11-09): the proposed version is a minor, `1.1.0`, because an
added public API is a new feature under semantic versioning; `1.0.1` was
never tagged, so it folds into `1.1.0` rather than shipping separately.

### Session 24: The Debug solution build is green under .NET SDK 11.0.100

1. Register.
2. Stop `tools/Dabbler.Security.DevIdp.Tool` failing the Debug build. Under
   .NET SDK 11.0.100, `dotnet build Dabbler.Platform.sln` fails in that
   project with "The following scoped css bundles do not have matching
   endpoints:
   `_framework/Microsoft.NET.Sdk.BlazorWeb.HotReload#[.{fingerprint}]!.lib.module.js`".
   In Debug, SDK 11 adds a Hot Reload browser-tools module to every
   `Microsoft.NET.Sdk.Web` project, and this tool has no static web assets
   for it to attach to. The Release build passes. Set
   `<StaticWebAssetsEnabled>false</StaticWebAssetsEnabled>` in
   `Dabbler.Security.DevIdp.Tool.csproj`, with a comment that says why: the
   tool serves no static files (no `wwwroot`, no `UseStaticFiles` or
   `MapStaticAssets`). No other project changes. The step's checks:
   `dotnet build Dabbler.Platform.sln` succeeds; `dotnet build
   Dabbler.Platform.sln -c Release` succeeds; `dotnet run --project
   tools/Dabbler.Security.DevIdp.Tool -- --help` exits 0.
3. Cross-provider verification.
4. The `unit` and `e2e` suites once against the verified tree, recorded as
   the run of record.
5. Close-out.

Non-goals: no `global.json` SDK pin; no change to the DevIdp tool's
behaviour; no change to `ci.yml`, `build.yml` or `dabbler.yaml`; no version
or `CHANGELOG.md` change (Session 30's).

Why now (2026-11-12): SDK 11.0.100 was installed on the development machine
on 2026-11-10, after Session 22's last green run. The first attempt at the
shell move (now Session 25) was rolled back because its `dotnet build
Dabbler.Platform.sln` check failed in this project, a project the move does
not touch. Every session whose checks build the solution in Debug fails the
same way until this is fixed. Setting the property on the command line was
confirmed to fix the Debug build, and `--help` still runs with it.

> **Sessions 25–30** answer the maintainer findings a downstream consumer
> (the TRIMS application, on the `1.0.0` packages) sent on 2026-11-12, and
> the operator's request for Unicode report text the same day. Session 23
> was cancelled, so `1.0.1` is still unreleased and `1.1.0` was never made;
> both fold into `2.0.0`. The findings about DabblerPdf are Sessions 13–15
> of `../dabbler-pdf`, which release DabblerPdf `1.2.0`, and Unicode is its
> Sessions 16–18, which release `1.3.0`. Both were on nuget.org by the time
> Session 26 closed, so Session 28 here takes `1.3.0` directly and no
> session pins `1.2.0`; Session 29 builds on Session 28 and `2.0.0` waits
> for both. Sessions 25–27 depend on neither.

### Session 25: MasterDetailExpansionShell moves to Dabbler.Components and gets its Add button

1. Register.
2. Move the shell to the package its documentation names. `git mv`
   `MasterDetailExpansionShell.razor`, `MasterDetailShellContexts.cs` and
   `PickOrAddDialog.razor` from
   `src/Libs/Dabbler.Security.Blazor/Components/Admin/Shared/` to
   `src/Libs/Dabbler.Components/MasterDetail/`, and change their namespace
   from `Dabbler.Security.Blazor.Components.Admin.Shared` to
   `Dabbler.Components.MasterDetail`. Nothing in them uses a security type.
   Fix every caller: the four Security admin tabs (`RoleMasterDetailsTab`,
   `OrganizationMasterDetailsTab`, `UsersAndRoleAssignmentTab`,
   `DefaultMasksTab`) and the five reference-app pages (`OrderDetail`,
   `CustomerBatchEntryPage`, `OrderItemTunerPage`, `CustomerPickerDialog`,
   `ProductLinePickerDialog`). Move `MasterDetailExpansionShellTests.cs` and
   `PickOrAddDialogTests.cs` from
   `tests/Dabbler.Security.Blazor.Tests/Components/Admin/Shared/` to
   `tests/Dabbler.Components.Tests/MasterDetail/`. No forwarding type is
   left in the old namespace: this is the breaking change `2.0.0` is for.
   The step's checks: no file under `src`, `tests`, `templates` or `tools`
   names `Components.Admin.Shared`; `dotnet build Dabbler.Platform.sln`
   succeeds; the two moved test classes pass in `Dabbler.Components.Tests`.
3. Make the shell's documented Add button real, and remove the parameters
   that do nothing. Today `ShowAddAction` (default `true`) and
   `OnAddRequested` are declared and never read, so no button is rendered
   and the callback never fires; `Mode` and `OnRowSelected` are declared and
   never read either.
   - `ShowAddAction` defaults to `false`. When it is `true` and `ShowGrid` is
     `true`, the shell renders an Add button in the results body, after
     `FilterContent` and above `GridContent`, right-aligned, with
     `data-testid="{TestId}-add-button"`, labelled by a new `AddLabel`
     parameter (default `"Add"`) and disabled while `Working`. A click calls
     `ResetSelection()` and then raises `OnAddRequested`. With `ShowGrid`
     `false` no button is rendered: that surface is already the add form.
   - Remove `Mode` and the `ShellMode` enum, and remove `OnRowSelected` from
     the shell: it cannot see a click on a grid it does not own, and
     consumers already call `NotifyRowSelected`. `PickOrAddDialog` loses the
     `OnRowSelected` it only forwarded; the two reference-app pickers that
     pass it call their handler from their own row click instead, with no
     change in behaviour.
   - `PickOrAddDialog.ShowAddAction` defaults to `false` as well, and its
     `OnAdd` now fires.
   - The XML docs lose "Implemented in Session 2" and "Implemented in
     Session 4", and the `TestId` doc lists `{TestId}-add-button`.
   - In the reference app, `OrderDetail.razor` replaces its hand-rolled
     "Add Product" button in `GridContent` with the shell's
     (`ShowAddAction="@(!IsReadOnly)"`, `AddLabel="Add Product"`,
     `OnAddRequested="OpenProductLinePickerAsync"`), and the three
     Playwright classes that locate that button (`OrderNestedItemsTests`,
     `OrderNestedItemsSaveTests`, `OrderProductLinePickerWorkflowTests`)
     locate `order-items-shell-add-button`. `ProductList.razor` has a
     different button with the same test id; it is not touched. The three
     pages that pass `ShowAddAction="false"` drop it.
   - Replace the structural tests (`Shell_ShowAddActionFalse_RendersWithoutError`,
     `Shell_OnRowSelectedCallback_AcceptedWithoutError`) with tests that
     assert behaviour: no Add button by default; with `ShowAddAction` the
     button renders with `AddLabel`; a click raises `OnAddRequested` once
     and clears the selection and both dirty flags; the button is disabled
     while `Working`; no button when `ShowGrid` is `false`;
     `PickOrAddDialog` raises `OnAdd`.

   The step's checks: `dotnet test tests/Dabbler.Components.Tests --filter
   "FullyQualifiedName~MasterDetailExpansionShellTests|FullyQualifiedName~PickOrAddDialogTests"`
   passes, and `pwsh -NoProfile -File tools/e2e.ps1 -Filter
   "FullyQualifiedName~Order"` passes.
4. Make the documents true. The `<MasterDetailExpansionShell>` rows in
   `src/Libs/Dabbler.Components/README.md` and `docs/platform-overview.md`
   name the namespace `Dabbler.Components.MasterDetail` and the opt-in Add
   button; `docs/planning/project-guidance.md` and
   `docs/scaffolding-templates/README.md` are corrected where they describe
   the shell's knobs; `src/Libs/Dabbler.Security.Blazor/README.md` says its
   admin tabs use the shell from `Dabbler.Components`. If
   `Dabbler.PublicSurface.Tests` names any moved type, update it. The step's
   check: after `pwsh tools/pack.ps1 -OutputDir pack-output -CleanOutput`,
   `dotnet test tests/Dabbler.PublicSurface.Tests/` passes.
5. Cross-provider verification.
6. The `unit` and `e2e` suites once against the verified tree, recorded as
   the run of record.
7. Close-out.

Non-goals: no change to the shell's save, use, link-data or
target-editability behaviour; no new Add buttons on the four Security admin
tabs (they never set `ShowAddAction`, which is why its default becomes
`false`); no version or `CHANGELOG.md` change (Session 30's).

Step 2 was done once, in the first attempt at this session, which was rolled
back on 2026-11-12 (see Session 24). That work is kept on the local,
unpushed branch `wip/session-24-shell-move`, one commit on `877921b`. Where
the branch exists, take the move from it rather than redoing it; it is
step 2 alone, and steps 3 and 4 were never started.

Why now (2026-11-12): the `Dabbler.Components` `1.0.0` README lists the
shell, but the type ships in `Dabbler.Security.Blazor`, so a consumer who
wants a generic master/detail layout must take the security UI package and,
through `Dabbler.Security`, EF Core. A consumer who follows the `ShowAddAction`
documentation gets no way to add a row. Both were confirmed in the source;
the reference app itself hand-rolls the Add button the shell documents.

### Session 26: One filter-operator vocabulary, and no silently dropped filter

1. Register.
2. `DeclarativeFilterPanel` (`src/Libs/Dabbler.Components/MasterDetail/DeclarativeFilterPanel.razor`)
   builds its descriptors with the literals `"contains"`, `"eq"`, `"gte"`
   and `"lte"`, while `LocalDataProvider`, `StubGridDataProvider` and
   `Dabbler.Api.Querying` match the `FilterOperators` constants
   (`"Contains"`, `"Equals"`, `"GreaterThanOrEqual"`, `"LessThanOrEqual"`).
   Every provider therefore needs an adapter, and an unmapped operator is
   dropped. Make the panel emit the `FilterOperators` constants. Then
   remove the workarounds: `NormaliseOperator` in
   `src/Apps/AcmeCoffeeOnline.Web/Data/SupplierGridDataProvider.cs`, and the
   `"contains"` comparisons in `CustomerDataProvider`, `OrderDataProvider`
   and `ProductDataProvider`, which compare with `FilterOperators.Contains`.
   Correct the `FilterDescriptor` doc comment in `GridDataRequest.cs`, which
   gives `"equals"` and `"contains"` as examples. Update the tests in
   `tests/Dabbler.Components.Tests/MasterDetail/` that assert the lowercase
   strings, and add a test there that the descriptors a
   `DeclarativeFilterPanel` raises, passed unchanged to a
   `StubGridDataProvider<T>`, filter its rows (a text contains, an enum
   equals and a date range). The step's checks: no `"contains"`, `"eq"`,
   `"gte"` or `"lte"` operator literal remains under `src`; `dotnet test
   tests/Dabbler.Components.Tests` and `dotnet test
   tests/AcmeCoffeeOnline.Web.Tests` pass.
3. `Dabbler.Api.Querying` (`QueryableExtensions.cs`) converts a filter value
   with `Convert.ChangeType` inside a `catch` that returns no predicate, so a
   filter on a `DateOnly` property, on a `Guid`, or on a property that does
   not exist is ignored and the caller gets unfiltered rows; an unknown sort
   property throws a raw `ArgumentException` from the expression tree.
   Change it so that:
   - a value is converted to `DateOnly`, `TimeOnly`, `DateTime`,
     `DateTimeOffset`, `Guid` and enum members (by name or number) as well
     as the numeric types, from a `string` or a `JsonElement`, with the
     invariant culture and ISO 8601 forms;
   - `In` is applied when the value is an array;
   - an unknown property (in a filter or a sort), an unknown operator, an
     operator the property's type does not support, and a value that cannot
     be converted each throw a new public `GridQueryException` that names
     the property and the operator. Nothing is dropped.

   In `AcmeCoffeeOnline.Api`, a `GridQueryException` from a query endpoint
   is answered with `400` and a problem-details body. Tests go in
   `tests/AcmeCoffeeOnline.Api.Tests/QueryableExtensionsTests.cs`, one per
   rule above, plus one that the Supplier query endpoint returns `400` for
   an unknown property. `src/Libs/Dabbler.Api.Querying/README.md` states the
   rules. The step's checks: `dotnet test tests/AcmeCoffeeOnline.Api.Tests`
   passes, and `pwsh -NoProfile -File tools/e2e.ps1 -Filter
   "FullyQualifiedName~Filter"` passes (no Playwright class is named for
   suppliers; the whole `e2e` suite is the run of record).
4. Cross-provider verification.
5. The `unit` and `e2e` suites once against the verified tree, recorded as
   the run of record.
6. Close-out.

Non-goals: no generic `IGridDataProvider<T>`-to-API adapter in the platform
(`Dabbler.Components` does not reference `Dabbler.Api.Querying`, and a
package that joins them is a decision for a plan session; once the
vocabulary is one, a consumer's adapter is a field-for-field copy); no
change to the legacy `FilterPanel<T>`, which already emits the constants; no
version or `CHANGELOG.md` change (Session 30's).

Why now (2026-11-12): carried over in the consumer's findings and confirmed
here: the panel and the server disagree on operator names, the reference
app carries an adapter for it, and `Dabbler.Api.Querying` hides three kinds
of caller mistake by returning unfiltered data. Both changes break callers
that relied on the old strings or the silence, so they belong in `2.0.0`.

### Session 27: Shipped text, the download helper and the READMEs

1. Register.
2. Remove internal workflow references from what ships. XML docs and
   comments under `src/Libs` say "Session N" in 22 files (56 places: for
   example "Implemented in Session 2" on a public parameter), and
   `src/Libs/Dabbler.Components/wwwroot/dabbler-download.js` opens with
   "Will be replaced by MudBlazor-native approach in Session 5." Reword each
   to say what the member does, or delete the sentence; change no code. Do
   the same for `tools/Dabbler.CrudSlice/README.md` and
   `tools/Dabbler.CrudSlice/TemplatePreflight.cs`, which are packed. The
   step's check: a search for `Session [0-9]+` in `*.cs`, `*.razor` and
   `*.js` under `src/Libs` and in the two CrudSlice files finds nothing,
   and `dotnet build Dabbler.Platform.sln` succeeds.
3. Give the download script a supported C# entry point. Add
   `DabblerDownloadExtensions.DownloadFileAsync(this IJSRuntime js, string
   fileName, byte[] content, string mimeType, CancellationToken
   cancellationToken = default)` in `src/Libs/Dabbler.Components/`, which
   imports `./_content/Dabbler.Components/dabbler-download.js` and calls
   `downloadFile`. `DabblerReportPage.razor.cs` and `ComplianceReviewTab.razor`
   call it and no longer name the script path. Tests in
   `tests/Dabbler.Components.Tests/DabblerDownloadExtensionsTests.cs`: the
   module path imported, and the arguments passed (base64 of the content).
   The step's check: that test class, `ComplianceReviewTabTests` and
   `dotnet test tests/Dabbler.Reports.Blazor.Tests` pass.
4. Bring the READMEs in step with the API.
   - `src/Libs/Dabbler.Components/README.md`: the "Smallest working example"
     uses `DeclarativeFilterPanel` with `FilterColumns` and `Coordinator`,
     parameters it does not have; rewrite it with `Config` (a
     `FilterConfig<T>`) and `FilterChanged`, the pattern
     `AcmeCoffeeOnline.Web`'s `CustomerList.razor` uses. Add a section on
     `EditSession<T>`: `Live` is the instance passed to the constructor, not
     a copy; `NotifyChanged` serialises the whole object on every call;
     `Undo` and `CommitFrom` copy public properties that have a getter and a
     setter (init-only included) and nothing else. Add a section on
     `DownloadFileAsync`, saying that the content crosses the circuit as
     base64 and that a large file is better served from an endpoint.
   - Every package README under `src/Libs`: the prerequisite is MudBlazor
     9.x; the install command has no `--prerelease` and names no
     `0.1.0-preview.1`; each caveat that says "in MudBlazor 8.x" is checked
     against 9.12.0 and either restated for 9.x or removed.
   - `src/Libs/Dabbler.Testing.Blazor/README.md` gains a note: in a
     `WebApplicationFactory` host whose environment is not `Development`, a
     request for `_content/Dabbler.Components/dabbler-download.js` returned
     `200` with an empty body for the consumer. Reproduce it in
     `tests/AcmeCoffeeOnline.Web.Tests` first, and write what the
     reproduction shows and what makes the asset load (expected:
     `UseStaticWebAssets()` on the test host); if it does not reproduce,
     write only what the consumer observed.

   The step's check: no README under `src/Libs` contains `MudBlazor 8`,
   `0.1.0-preview.1` or `--prerelease`, and after `pwsh tools/pack.ps1
   -OutputDir pack-output -CleanOutput`, `dotnet test
   tests/Dabbler.PublicSurface.Tests/` passes.
5. Cross-provider verification.
6. The `unit` and `e2e` suites once against the verified tree, recorded as
   the run of record.
7. Close-out.

Non-goals: no change to `EditSession<T>`'s behaviour (the findings ask for
its semantics to be stated, not changed); no streaming download; nothing
under `docs/session-sets/` or `docs/transcripts/`; no version or
`CHANGELOG.md` change (Session 30's).

Why now (2026-11-12): the consumer read "Implemented in Session 2" and
"Will be replaced … in Session 5" in the shipped package as unfinished
work, copied a README example that does not compile, and imports the
download script by a path no document promises.

### Session 28: Take DabblerPdf 1.3.0, and measure report text the way it is drawn

Run only once DabblerPdf `1.3.0` is listed on nuget.org (Sessions 13–18 of
`../dabbler-pdf`, then that repository's `nuget-v1.3.0` tag; it was listed
on 2026-11-12). If it is not, report the first step blocked and say so.

1. Register.
2. Set the `DabblerPdf` `PackageVersion` in `Directory.Packages.props` from
   `1.1.3` to `1.3.0`, the one place the platform pins it. The step's
   checks: `Directory.Packages.props` names `DabblerPdf` at `1.3.0` and
   nowhere at `1.1.3`; `dotnet test tests/Dabbler.Reports.Blazor.Tests`
   passes; then, after a Release build and `tools/pack.ps1 -OutputDir
   pack-output -CleanOutput`, the `.nuspec` inside the packed
   `Dabbler.Reports.Core` nupkg declares its `DabblerPdf` dependency at
   `1.3.0`.
3. A field's height is measured by `OpenPdfTextMeasurer`
   (`src/Libs/Dabbler.Reports.Core/Layout/`) and its text drawn by
   `PdfPageRenderer` through `PdfStyleResolver`
   (`src/Libs/Dabbler.Reports.Export/`), and the two disagree:
   - each has its own word wrap (`WrapParagraph` and `WrapText`), and both
     split on single spaces and rejoin with one, so a run of spaces is
     collapsed in every PDF report, the bug DabblerPdf 1.2.0 fixed in
     `TextLayout.WrapText`;
   - a style with no font size is measured at 12 pt and drawn at
     `ReportMetadata.DefaultFontSize` (10 by default);
   - a style with no font family is measured in Helvetica and drawn in the
     family `ReportMetadata.DefaultFontFamily` maps to.

   Give both one internal font resolver in `Dabbler.Reports.Core` (style,
   then report metadata, then the Base 14 mapping), and have the measurer
   count lines with `TextLayout.CountLines` and the renderer draw the lines
   of `TextLayout.WrapText`; delete both private wraps. A word wider than
   the field is now broken at characters, where it used to overflow. Tests
   go in `tests/Dabbler.Reports.Blazor.Tests`, in files named after the
   classes they test (add `InternalsVisibleTo` for an internal type): a
   double space in a field survives into the PDF's `Tj` operand; for a
   style with no size and no family, and for one with both, the measurer's
   line count equals the number of lines the renderer draws; a style with
   no size is measured at the report's default size. The step's check:
   `dotnet test tests/Dabbler.Reports.Blazor.Tests` passes.
4. `DabblerReportPage` (`src/Libs/Dabbler.Reports.Blazor/DabblerReportPage.razor.cs`)
   composes with `textMeasurer: null`, so `CanGrow` and `CanShrink` never
   act in the shipped page: long text is clipped instead of growing its
   band. Pass an `OpenPdfTextMeasurer` built from the report definition.
   Add a test to `tests/Dabbler.Reports.Blazor.Tests/DabblerReportPageTests.cs`
   that a `CanGrow` field with three lines of text composes taller than its
   designed height. The step's check: `dotnet test
   tests/Dabbler.Reports.Blazor.Tests` passes.
5. Cross-provider verification.
6. The `unit` and `e2e` suites once against the verified tree, recorded as
   the run of record.
7. Close-out.

Non-goals: no use of `PdfWriter.CompressContentStreams`; no change to the
HTML `ReportViewer`'s own text layout; no font other than the Base 14
(Session 29's); no change to `DabblerVersion` or `CHANGELOG.md`; the
tracked `*.csproj.lscache` files are left alone.

Why now (2026-11-12): the platform's packages should depend on the
DabblerPdf that carries the fixes its consumers asked for, and NuGet
resolves the lowest version a dependency allows. Taking it changes nothing
in a report unless the platform wraps with it: its own wraps collapse
spaces exactly as TRIMS reported, and measuring and drawing with different
fonts, sizes and wraps is how a grown field clips or leaves a gap. Found
while planning DabblerPdf's Sessions 14–18. Amended 2026-11-12: `1.3.0` was
listed before this session ran, so it takes `1.3.0` directly instead of
`1.2.0`, and Session 29 no longer moves the pin.

### Session 29: Report text can use a Unicode TrueType font

Runs after Session 28, which takes DabblerPdf `1.3.0` and adds the font
resolver this session extends.

1. Register.
2. Every PDF report font is one of the Base 14 with Windows-1252, so Polish,
   Cyrillic, Greek and CJK text prints as `?`, and
   `src/Libs/Dabbler.Reports.Export/README.md` says custom fonts are given
   "through the report definition", which nothing reads. Add a public
   `ReportFonts` in `Dabbler.Reports.Core` that registers a family name
   (case-insensitive) against a regular `.ttf` file and, optionally, bold,
   italic and bold-italic files. The font resolver from Session 28 uses a
   registered family, when a style's family or the report's default family
   names one, through `BaseFont.CreateFont(path, BaseFont.IdentityH,
   true)`, choosing the file for the style's weight and slant and falling
   back to the regular file (no synthetic bold or italic); any other family
   keeps the Base 14 mapping. `OpenPdfTextMeasurer` and `PdfExporter` take
   an optional `ReportFonts`; `services.AddDabblerReportFonts(fonts => …)`
   registers one, and `DabblerReportPage` and the PDF export it starts use
   it when it is registered. Vendor the Liberation Sans fixture with its
   licence, as `../dabbler-pdf` does, under `tests/`. Tests in
   `tests/Dabbler.Reports.Blazor.Tests`, in files named after the classes
   they test: a report whose field holds Polish, Cyrillic and Greek text in
   a registered family exports a PDF from which PdfPig extracts that text;
   that field's `CanGrow` height is the one `TextLayout.MeasureHeight` gives
   with the registered font; a bold style in a family registered with only
   a regular file uses the regular file; an unregistered family still maps
   to the Base 14. Rewrite the README's font paragraph: the Base 14
   families and their Windows-1252 limit, how to register a Unicode font,
   and what it does not do (no shaping or right-to-left text, nothing beyond
   U+FFFF, no fallback between fonts). The step's checks: `dotnet test
   tests/Dabbler.Reports.Blazor.Tests` passes; the README no longer says
   "through the report definition".
3. Cross-provider verification.
4. The `unit` and `e2e` suites once against the verified tree, recorded as
   the run of record.
5. Close-out.

Non-goals: no font file shipped in any package (consumers bring their own);
no RDF syntax for fonts; no change to the HTML `ReportViewer`, which the
browser draws with its own fonts; no use of fonts in the reference app; no
change to `DabblerVersion` or `CHANGELOG.md`.

Why now (2026-11-12): the operator asked for Unicode report text. DabblerPdf
1.3.0 draws it from an embedded, subset TrueType font, but the platform
creates every font as a Base 14 one, so taking 1.3.0 alone (Session 28)
changes nothing.

### Session 30: Bump to 2.0.0

1. Register.
2. Set `DabblerVersion` in `Directory.Build.props`, and the `-Version`
   default in `tools/pack.ps1` with the version its help text names, to
   `2.0.0`. In `CHANGELOG.md`, the unreleased `## 1.0.1` entry becomes
   `## 2.0.0`, keeps what it has, and states:
   - breaking: `MasterDetailExpansionShell`, its context types and
     `PickOrAddDialog` moved from `Dabbler.Security.Blazor` (namespace
     `Dabbler.Security.Blazor.Components.Admin.Shared`) to
     `Dabbler.Components` (namespace `Dabbler.Components.MasterDetail`);
   - breaking: `ShowAddAction` now renders an Add button and defaults to
     `false`; `Mode`, `ShellMode` and the shell's and dialog's
     `OnRowSelected` are removed;
   - breaking: `DeclarativeFilterPanel` emits the `FilterOperators`
     constants, not `"contains"`, `"eq"`, `"gte"` and `"lte"`;
   - breaking: `Dabbler.Api.Querying` throws `GridQueryException` for an
     unknown property, an unknown or unsupported operator and an
     unconvertible value, where it used to skip the filter;
   - added: `AddLabel`; `DateOnly`, `TimeOnly`, `DateTimeOffset`, `Guid`,
     enum and `In` filters in `Dabbler.Api.Querying`; `GridQueryException`;
     `DownloadFileAsync`; `ReportFonts` and `AddDabblerReportFonts` for
     Unicode TrueType report fonts;
   - changed: PDF report text keeps runs of spaces, breaks a word wider
     than its field, and is measured with the font and size it is drawn
     with; `DabblerReportPage` measures `CanGrow` and `CanShrink` fields, so
     their bands grow and shrink in the shipped page;
   - changed: the `DabblerPdf` dependency moves from `1.1.2` to `1.3.0`.

   It gives the one-line migration for each breaking change. The step's
   checks: neither file names `1.0.1`; `CHANGELOG.md` has `## 2.0.0`
   directly above `## 1.0.0`; then, after a Release build and
   `tools/pack.ps1 -OutputDir pack-output -CleanOutput`, the `smoke` suite
   passes against the `2.0.0` packages.
3. Cross-provider verification.
4. The `unit`, `smoke` and `e2e` suites once against the verified tree,
   recorded as the run of record.
5. Close-out.

Non-goals: no API or dependency change beyond Sessions 25–29; no change to
`ci.yml`, `build.yml` or `dabbler.yaml`.

Proposed version: **2.0.0 (major)**, approved by the operator on
2026-11-12: a public type changes assembly and namespace, parameters are
removed, and two behaviours callers could rely on change.

This is a bump session, not a `(release: 2.0.0)` session: with the pack-only
`packaging:` block the router makes a `session-<N>` tag that publishes
nothing (Session 11 of `../dabbler-pdf`). After the session has pushed, the
operator runs the `2.0.0` dry run (`gh workflow run build.yml -f
version=2.0.0`) and tags `nuget-v2.0.0` by hand when it is green; `1.0.1` is
never tagged.

### Session 31: Clear the release build's nullable and vulnerability warnings

1. Register.
2. The Release build of `Dabbler.Components` prints CS8669 ("The annotation
   for nullable reference types should only be used in code within a
   '#nullable' annotations context") some twenty times, every one in the
   code generated from
   `src/Libs/Dabbler.Components/MasterDetail/DeclarativeFilterPanel.razor`.
   That file passes nullable reference types as markup type arguments
   (`<MudTextField T="string?"` at line 18, `<MudSelect T="object?"` and
   `<MudSelectItem T="object?"` at lines 147 and 157), which the Razor
   compiler copies into generated code outside a `#nullable` context.
   Remove the warning at its cause, by dropping the `?` from those
   reference-type arguments or by whatever smaller change does it, without
   a `NoWarn` and without changing what the panel renders or emits; the
   `bool?` and `decimal?` arguments are value types and stay.
   `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Customers/CustomerDetail.razor`
   has the same `T="string?"` at five places: fix it the same way. The
   step's checks: `dotnet build
   src/Libs/Dabbler.Components/Dabbler.Components.csproj -c Release
   --no-incremental` and the same build of
   `src/Apps/AcmeCoffeeOnline.Web/AcmeCoffeeOnline.Web.csproj` print no
   CS8669; `dotnet test tests/Dabbler.Components.Tests` passes.
3. Restoring `Dabbler.Publish.slnf` reports two audited packages:
   - `Microsoft.Build.Tasks.Git` `8.0.0` (NU1902, GHSA-23fw-v26w-5fgq), in
     every packable project, brought by the `Microsoft.SourceLink.GitHub`
     `8.0.0` pin in `Directory.Packages.props`. It is `PrivateAssets="All"`,
     so it reaches no consumer. Raise the pin to the lowest version the
     advisory names as patched.
   - `System.Security.Cryptography.Pkcs` `9.0.4` (NU1903,
     GHSA-846f-f764-76mx, and NU1902, GHSA-2fmv-rv2c-59x7), which
     `Dabbler.Security` brings through
     `Microsoft.EntityFrameworkCore.SqlServer` `10.0.5` and
     `Microsoft.Data.SqlClient` `6.1.1`. This one does reach every consumer
     of `Dabbler.Security` and `Dabbler.Security.Blazor`. Raise it to a
     version both advisories name as patched, in the way that also raises
     it for consumers: a newer `Microsoft.EntityFrameworkCore.SqlServer`
     or `Microsoft.Data.SqlClient` within the 10.x policy in
     `docs/MAINTAINING.md` if one carries it, otherwise a direct
     `PackageReference` from `Dabbler.Security`.

   The step's checks: `dotnet restore Dabbler.Publish.slnf --force` prints
   no NU1901, NU1902, NU1903 or NU1904; then, after a Release build and
   `tools/pack.ps1 -OutputDir pack-output -CleanOutput`, the `.nuspec`
   inside the packed `Dabbler.Security` nupkg declares the dependency that
   was raised at its new version, and `dotnet test
   tests/Dabbler.PublicSurface.Tests` passes.
4. Release step 4 of `docs/MAINTAINING.md` ("Unlist retired IDs") says only
   to unlist. Unlisting hides a package from search and tells an existing
   consumer nothing; deprecating is what warns them and names the
   replacement. Rewrite the step to do both for every retired ID: deprecate
   as *Legacy*, with `Dabbler.Platform.Stubs` as the alternate package for
   the three retired `Dabbler.Platform.Stubs.*` providers and none for
   `Dabbler.Components.Compat`, then unlist. The step's check: the step
   names both actions and the alternate package.
5. Cross-provider verification.
6. The `unit` and `e2e` suites once against the verified tree, recorded as
   the run of record.
7. Close-out.

Non-goals: no public API change; no fix to the XML-comment, obsolete-member
and possible-null warnings in `Dabbler.Security`; no change to the action
pins in `ci.yml` or `build.yml` (the Node.js 20 notice); no change to
`DabblerVersion` or `CHANGELOG.md` (Session 32's).

Why now (2026-11-13): the `nuget-v2.0.0` run was green and noisy, and the
operator asked what the nullable warnings were. Reading the same log showed
that `Dabbler.Security` `2.0.0` ships a transitive dependency with a
high-severity advisory, which is the part that needs a release.

### Session 32: Clear the solution's remaining audited packages

Runs after Session 31.

1. Register.
2. `Microsoft.OpenApi` `2.0.0` (NU1903, GHSA-v5pm-xwqc-g5wc, patched in
   `2.7.5`) reaches `src/Apps/AcmeCoffeeOnline.Api` and
   `tests/AcmeCoffeeOnline.Api.Tests` through the
   `Microsoft.AspNetCore.OpenApi` `10.0.5` pin in `Directory.Packages.props`.
   `Microsoft.AspNetCore.OpenApi` `10.0.15` depends on `Microsoft.OpenApi`
   `[2.13.0, 3.0.0)`: raise that one pin to `10.0.15`, within the 10.x
   policy in `docs/MAINTAINING.md`. No shipped package and no template
   references it. The step's checks: `dotnet restore
   src/Apps/AcmeCoffeeOnline.Api/AcmeCoffeeOnline.Api.csproj --force` and
   the same restore of `tests/AcmeCoffeeOnline.Api.Tests` print no NU1901,
   NU1902, NU1903 or NU1904; `dotnet test tests/AcmeCoffeeOnline.Api.Tests`
   passes.
3. `AngleSharp` `1.1.2` (NU1902, GHSA-pgww-w46g-26qg, patched in `1.5.0`)
   reaches every bUnit test project through `bunit` `1.37.7` →
   `bunit.web`: `tests/AcmeCoffeeOnline.Web.Tests`,
   `tests/Dabbler.Components.Tests`, `tests/Dabbler.Reports.Blazor.Tests`,
   `tests/Dabbler.Security.Blazor.Tests` and
   `tests/Dabbler.Testing.Blazor.Tests`. It also reaches the test project
   that the `dabbler-app` template generates
   (`templates/Dabbler.Templates/content/dabbler-app/tests/DabblerApp.Web.Tests/DabblerApp.Web.Tests.csproj`,
   which pins `bunit` `1.37.7`), so it reaches consumers of
   `Dabbler.Templates`. No `bunit` 1.x carries a patched AngleSharp (`1.40.0`
   brings `1.2.0`), and `bunit` 2.x is a breaking major that this session
   does not take. Add a `PackageVersion` for `AngleSharp` at `1.5.0`, the
   lowest patched version, and a direct `PackageReference` to it in each of
   the five test projects. Add the same reference at `Version="1.5.0"` to
   the template's test project, with a one-line comment saying it lifts
   bUnit's transitive AngleSharp past the advisory. The step's checks: the
   restore of each of the five test projects with `--force` prints no
   NU1901, NU1902, NU1903 or NU1904; `dotnet test` of each passes; then,
   after a Release build and `tools/pack.ps1 -OutputDir pack-output
   -CleanOutput`, `dotnet test tests/Dabbler.Templates.Tests` passes.
4. Cross-provider verification.
5. The `unit` and `e2e` suites once against the verified tree, recorded as
   the run of record.
6. Close-out.

Non-goals: no move to `bunit` 2.x; no raise of the other
`Microsoft.AspNetCore.*` 10.0.5 pins; no public API change; no change to
`DabblerVersion` or `CHANGELOG.md` (Session 33's).

Why now (2026-11-13): Session 31 cleared every advisory in
`Dabbler.Publish.slnf`. A restore of the whole solution still reports these
two, and the operator asked for them to be cleared. The AngleSharp one ships
in the `dabbler-app` template, so it belongs in the 2.0.1 release.

### Session 33: Bump to 2.0.1

Runs after Session 32.

1. Register.
2. Set `DabblerVersion` in `Directory.Build.props`, and the `-Version`
   default in `tools/pack.ps1` with the version its help text names, to
   `2.0.1`. Add a `## 2.0.1` entry to `CHANGELOG.md`, directly above
   `## 2.0.0`: no API change since `2.0.0`; the dependency Session 31 raised
   in `Dabbler.Security`, by name, with its old and new versions and the
   two advisories it clears; and the `AngleSharp` `1.5.0` reference Session
   32 added to the `dabbler-app` template's test project, with the advisory
   it clears. The step's checks: `Directory.Build.props` and
   `tools/pack.ps1` name `2.0.1` and no longer name `2.0.0` as the version
   or the default; `CHANGELOG.md` has `## 2.0.1` directly above `## 2.0.0`;
   then, after a Release build and `tools/pack.ps1 -OutputDir pack-output
   -CleanOutput`, the `smoke` suite passes against the `2.0.1` packages.
3. Cross-provider verification.
4. The `unit`, `smoke` and `e2e` suites once against the verified tree,
   recorded as the run of record.
5. Close-out.

Non-goals: no API or dependency change beyond Sessions 31 and 32; no
change to `ci.yml`, `build.yml` or `dabbler.yaml`.

Proposed version: **2.0.1 (patch)**, for the operator to approve: no API
changes, and what a consumer receives is a patched transitive dependency
of `Dabbler.Security` and a patched AngleSharp in the `dabbler-app`
template's test project.

This is a bump session, not a `(release: 2.0.1)` session, for the reason
Session 30 gives. After the session has pushed, the operator runs the
`2.0.1` dry run (`gh workflow run build.yml -f version=2.0.1`) and tags
`nuget-v2.0.1` by hand when it is green.
