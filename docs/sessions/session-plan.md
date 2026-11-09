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
