# Project work plan — sessions

**Written by `dabbler` as a fold of `activity-log.json`.**
Hand edits are overwritten by the next append. The record is the log;
this page is one view of it.

---

## Sessions

The numbered sessions are declared from `session-plan.md`; each one's task is what its own plan step declared.

| # | Session | Releasable | Declared |
| ---: | --- | --- | --- |
| 1 | Author or import the solution plan | no | 2026-10-03 |
| 2 | Challenge the plan, then break it into numbered sessions | no | 2026-11-07 |
| 3 | Skeleton — complete the existing solution and prove the production split | no | 2026-11-07 |
| 4 | Declare packaging | no | 2026-11-07 |
| 5 | Bump to 0.1.0-preview.2 | no | 2026-11-07 |
| 6 | Move Expressions into Dabbler.Reports.Core | no | 2026-11-07 |
| 7 | Surface reduction — 16 package IDs to 12 | no | 2026-11-07 |
| 8 | MudBlazor 9 — libraries and the reference app compile | no | 2026-11-07 |
| 9 | MudBlazor 9 — templates, scaffolding and the E2E gate | no | 2026-11-07 |
| 10 | Maintainer guide, gate rule and support policy | no | 2026-11-08 |
| 11 | Bump to 0.2.0 | no | 2026-11-08 |
| 12 | Retire what does not run | no | 2026-11-08 |
| 13 | Prune the v1 guidance | no | 2026-11-08 |
| 14 | Bump to 1.0.0 | no | 2026-11-08 |
| 15 | Build on push | no | 2026-11-08 |
| 16 | Templates reference the version being packed | no | 2026-11-08 |
| 17 | Make the push build green on a clean machine | no | 2026-11-09 |
| 18 | Scaffolded-app restores take their sources from nuget.config | no | 2026-11-09 |
| 19 | Take DabblerPdf 1.1.3 | no | 2026-11-09 |
| 20 | Bump to 1.0.1 | no | 2026-11-09 |
| 21 | The framework runs the Playwright E2E gate | no | 2026-11-09 |
| 22 | Fix the 12 Playwright E2E failures | — | not declared |
| 23 | The E2E fixes that need a public API change | — | not declared |

### Session 1 — Author or import the solution plan

**Releasable: no.**

Author docs/planning/solution-plan.md for dabbler-platform, importing the plan material this repository already holds (docs/planning/brief.md as the operator's substance, docs/platform-overview.md, docs/planning/phase-3-platform-roadmap.md and docs/planning/project-status.md) into one plan a reader can act on. The file states the objective; the Production split the brief already answers (a generated application is a Blazor Server web tier calling a Web API tier, and only the API tier reaches SQL Server -- the platform's own reference app, AcmeCoffeeOnline, is built that way); the Handoff artifacts each separately running part is delivered as, with the standard command for its form (NuGet packages via tools/pack.ps1 for the libraries, templates and dotnet tools; dotnet publish IIS site packages for the Web and Api tiers of the reference app; dotnet ef migrations script --idempotent for the database); the modules -- the Dabbler.* libraries under src/Libs, the Dabbler.Templates package, the two dotnet tools under tools/ and the AcmeCoffeeOnline reference app under src/Apps -- with what each is responsible for, the contract it exposes (preconditions, guarantees, failure mode), the dependency direction, the reason for each cut and the cuts left deferred, and how each is a .csproj listed in Dabbler.Platform.sln reaching its siblings by <ProjectReference>; and the phases or feature areas with their key deliverables, each scoped to a handful of focused AI sessions and ordered so publishing Dabbler.CrudSlice.Tool unblocks the Access-migration consumer first. The six open decisions the brief lists (uat_runner, the Playwright and Packaging.Smoke manual gates, the CrudSlice.Tool release, the stale planning docs, the legacy branches and the dabbler-uat-dsl feed) are carried into the plan as deferred cuts with their options and the operator's leaning, for Session 2 to challenge and decide.

### Session 2 — Challenge the plan, then break it into numbered sessions

**Releasable: no.**

Challenge docs/planning/solution-plan.md and turn it into numbered sessions. Step one adds a Session 2 rationale section to the plan. It covers each module: whether it should exist, whether its contract is a promise a test can prove, and whether its dependency direction hides the decisions most likely to change. It also records what the tree shows that the plan got wrong. Reports.Core takes DabblerPdf directly, so the stated reason for the Reports.Export cut is false. Dabbler.Security carries the EF Core SqlServer provider into every web tier. Stubs.Reports uses Dabbler.Components.Data.Metadata. The pack allowlist has 16 IDs, not 17. Router 3.29.0 still hard-wires release tags to vsix-v, and a pack-only release would push a tag build.yml ignores. The section then decides each of the thirteen deferred cuts or says why it stays deferred. Step two appends Sessions 3 to 14 to docs/sessions/session-plan.md. Session 3 is the skeleton, adapted to the existing solution: it adds AcmeCoffeeOnline.Shared to Dabbler.Platform.sln and adds the production-split architecture test under the unit suite that is already declared. Then come the packaging-only session and the 0.1.0-preview.2 version bump that unblocks Dabbler.CrudSlice.Tool. Next are the Expressions move, surface reduction (ordered before MudBlazor 9 so the deleted Compat package is never migrated), the two MudBlazor 9 sessions, the maintainer guide with the gate rule and support policy, the 0.2.0 bump, retirement of what does not run, pruning of the v1 guidance, and the 1.0.0 bump. Each version is proposed as patch, minor or major for the operator to approve. Because of cut 12, each release is a version-bump session followed by the operator's hand tag on nuget-v<version>, not a release-headed session.

### Session 3 — Skeleton — complete the existing solution and prove the production split

**Releasable: no.**

Complete the existing solution skeleton and prove the production split. List the already-existing src/Apps/AcmeCoffeeOnline.Shared/AcmeCoffeeOnline.Shared.csproj in Dabbler.Platform.sln at the solution root (alongside AcmeCoffeeOnline.Web and AcmeCoffeeOnline.Api, which are not nested in a solution folder), adopting every other project, reference and the existing unit suite as they are. Then add tests/AcmeCoffeeOnline.Web.Tests/ProductionSplitTests.cs, an xUnit architecture test in the existing AcmeCoffeeOnline.Web.Tests project (so it runs in the unit suite), asserting that no type in the AcmeCoffeeOnline.Web assembly derives from Microsoft.EntityFrameworkCore.DbContext and that AcmeCoffeeOnline.Web.csproj names no Microsoft.EntityFrameworkCore* package reference.

### Session 4 — Declare packaging

**Releasable: no.**

Declare packaging in dabbler.yaml: replace the commented-out packaging intent at the end of the file with a live `packaging:` block whose `pack.argv` is ["pwsh", "-NoProfile", "-File", "tools/pack.ps1", "-Version", "{version}", "-OutputDir", "{output}", "-CleanOutput"], with no `push` and no `release:` member because CI publishes to nuget.org from the operator's annotated `nuget-v*` tag through trusted publishing. The step is proven by `dabbler packaging --rehearse --version 0.1.0-preview.2`.

### Session 5 — Bump to 0.1.0-preview.2

**Releasable: no.**

Bump the single repository version to 0.1.0-preview.2 ahead of the operator-tagged release: set DabblerVersion in Directory.Build.props to 0.1.0-preview.2, change the -Version parameter default (and its help text) in tools/pack.ps1 to match, and create the root CHANGELOG.md with a 0.1.0-preview.2 entry stating that it changes no API and that Dabbler.CrudSlice.Tool is published for the first time.

### Session 6 — Move Expressions into Dabbler.Reports.Core

**Releasable: no.**

Move the report expression engine out of Dabbler.Components into Dabbler.Reports.Core, its only consumer. The nine files in src/Libs/Dabbler.Components/Expressions/ move (git mv) to src/Libs/Dabbler.Reports.Core/Expressions/ with their namespace changed from Dabbler.Components.Expressions to Dabbler.Reports.Core.Expressions and no other change; BandLayoutEngine.cs and LayoutEvaluationContext.cs, the only callers (no test references the namespace), have their usings fixed. Dabbler.Reports.Core.csproj then drops both Dabbler.Components references (the development ProjectReference and the pack-time PackageReference) and the now-unneeded InternalsVisibleTo Dabbler.Components.Tests (that test project references no Reports project). The dependency statements are updated: the Reports.Core README no longer lists Dabbler.Components as a prerequisite or related package and says the expression engine is its own, docs/platform-overview.md stops attributing the expression engine to Dabbler.Components, and the stale ExpressionEngine row is removed from the Dabbler.Components README surface table.

### Session 7 — Surface reduction — 16 package IDs to 12

**Releasable: no.**

Reduce the published surface from sixteen package IDs to twelve. Delete Dabbler.Components.Compat together with its tests (DabblerShellTests, CompatibilitySurfaceTests) and every reference to it: Dabbler.Components.Tests, Dabbler.PublicSurface.Tests, Dabbler.Platform.sln, Dabbler.Publish.slnf. The reference web app references Compat too: its /platform/stub demo page renders DabblerShell. That page is rebuilt from plain MudBlazor (the title, the stub customer grid and the stub sales report in MudTabs), and PlatformStubDetail.razor, which only DabblerShell used, is deleted. WebGuardrailTests keeps covering the route unchanged. Then fold the source of Dabbler.Platform.Stubs.Components, .Reports and .Security into src/Libs/Dabbler.Platform.Stubs. The files keep the Dabbler.Platform.Stubs namespace they already have, so registration calls compile unchanged. The merged project references Dabbler.Components, Dabbler.Reports.Core and the Microsoft.AspNetCore.App framework. The three projects are deleted, and the solution, the publish filter, the test project that referenced them and the template comment are updated. Finally, bring pack.ps1's allowlist and project list, the PublicSurface and Packaging.Smoke expectations, the release-checklist publish order, docs/platform-overview.md and every other live document that names a retired ID to the twelve IDs.

### Session 8 — MudBlazor 9 — libraries and the reference app compile

**Releasable: no.**

Move the central MudBlazor pin in Directory.Packages.props from 8.15.0 to 9.12.0 and fix every project in Dabbler.Unit.slnf so that it compiles and its tests pass. A probe build of this tree under 9.12.0 found the following breaks. GridDataProviderExtensions.ToServerDataFunc must return MudDataGrid's new ServerData delegate, Func<GridState<T>, CancellationToken, Task<GridData<T>>>, and pass the token to IGridDataProvider.FetchAsync. That interface already declares the token, so it and its implementers stay as they are; the public API change is the adapter's delegate type, and it gets a new GridDataProviderExtensionsTests. IDialogService.ShowMessageBox becomes ShowMessageBoxAsync, in DirtyAwareActionRow and in the reference app. MudForm.Validate() becomes ValidateAsync(), in ParameterPromptForm and its tests, in the Security.Blazor dialogs and forms, and in the reference app. MudTabs PanelClass becomes TabPanelsClass. The hand-written ServerData callbacks, in ComplianceReviewTab and in the reference app's four list pages, take the CancellationToken and pass it through. In the probe, FilterPanel, DeclarativeFilterPanel, the Security.Blazor ShowAsync dialog calls, OrganizationDetailsForm's reset, and ParameterPromptForm's SelectedValues (passed as a component parameter, never assigned) compiled and passed their tests under 9.12.0 with no source change. They are proven by the builds and tests in each step's checks, and nothing is changed in them for its own sake.

**Amended after acceptance:**

- 2026-11-07 — step 'components-reports': its files: The MudBlazor pin is central and the named-test run builds all of Dabbler.Unit.slnf, so this step must also carry the compile-only fixes the 9.12.0 bump forces downstream: the ServerData callback signatures (ComplianceReviewTab and four list pages) and ShowMessageBox->ShowMessageBoxAsync in two reference-app pages. Steps security-blazor and reference-app keep the ValidateAsync/TabPanelsClass renames and the token pass-through. (claude-code (anthropic, claude-opus-5-5))

### Session 9 — MudBlazor 9 — templates, scaffolding and the E2E gate

**Releasable: no.**

Finish the MudBlazor 9 move that session 8 started, for the parts outside Dabbler.Unit.slnf's compile path: the dotnet new templates, the dabbler-crud-slice scaffolding and the Playwright E2E gate. In templates/Dabbler.Templates/content, the explicit MudBlazor 8.15.0 PackageReference pins in dabbler-app's Web and Web.Tests projects and in dabbler-min move to 9.12.0. So does the SmokeApp fixture in tests/Dabbler.Templates.Tests, which the crud-page test builds the scaffolded slice into. The generated dabbler-crud-page code gets the MudBlazor 9 API fixes session 8 found: T1EntityDetail.razor calls ShowMessageBoxAsync and _form.ValidateAsync(), and the ServerData callback in T1EntityList.razor takes the CancellationToken and passes it to IGridDataProvider.FetchAsync. Packing and running Dabbler.Templates.Tests with the packages present proves this. The text templates under tools/Dabbler.CrudSlice/Templates emit only registration lines and contain no MudBlazor API, and the tool's Razor output comes from the dabbler-crud-page template. So the same step also runs Dabbler.CrudSlice.Tests after that pack to show the scaffolding is on MudBlazor 9. The step changes a CrudSlice file only if that run shows one needs it. Then the Playwright E2E manual gate runs against the compose stack rebuilt from this tree, restoring Dabbler.Uat.Dsl.* from ../dabbler-uat-dsl/pack-output. Anything it finds is fixed in AcmeCoffeeOnline.Web, and the result goes in that step's notes. Once the gate has passed on 9.12.0, the two docs that name the platform's MudBlazor major (the coupling bullet in docs/platform-overview.md and the stack note in docs/getting-started.md) say 9.x instead of 8.x.

**Amended after acceptance:**

- 2026-11-07 — step 'e2e-gate': its files: The compose stack cannot be rebuilt from this tree: docker/Dockerfile.Api restores before copying Dabbler.Api.Querying.csproj, which AcmeCoffeeOnline.Api references, so 'dotnet publish --no-restore' fails with NETSDK1004. The gate needs a one-line COPY of that csproj. (claude-code (anthropic, claude-opus-5-5))
- 2026-11-07 — step 'e2e-gate': its files: The E2E gate on MudBlazor 9.12.0 fails 22 of 178 tests; a fresh-database baseline run of the pre-session-8 tree (MudBlazor 8.15.0) fails the same 12 of them, so 10 are MudBlazor 9 regressions in three causes. (A, 6 tests) MudMenu ActivatorContent now receives a MenuContext and no longer wraps a click handler, so UserContextBar's role and organization activators never open their menus: fix in AcmeCoffeeOnline.Web. (B, 1 test) MudNavGroup now stays expanded after navigating to one of its own links, so PlaywrightFixture.NavigateViaDrawerGroupAsync's unconditional group click collapses it: the helper clicks the group only when the link is hidden. (C, 3 tests) FilterPanel's 300 ms debounced text now lands after an immediate collapse click and re-expands the shell by design: the three FilterChange_ClearsCollapsedSummaryState tests wait for the filter chip before collapsing. (claude-code (anthropic, claude-opus-5-5))

### Session 10 — Maintainer guide, gate rule and support policy

**Releasable: no.**

Write docs/MAINTAINING.md, the maintainer guide: how to clone, build, run the `unit` suite and pack; how to release (a bump session, then the hand-pushed annotated tag `nuget-v<version>`, then unlisting retired package IDs on nuget.org); where every gate lives (the `unit` suite in dabbler.yaml, PublicSurface and Packaging.Smoke in CI's Validate And Pack job, and the Playwright manual gate before each release tag and in every session that changes the reference app's UI or the MudBlazor pin); and the MudBlazor policy of adopting a new major within one quarter of its release. Point the *Building and testing* part of AGENTS.md (outside the managed fence) and the platform authoring addendum at that gate rule. Add .github/dependabot.yml for nuget, weekly, with MudBlazor in one group and the Microsoft.* packages pinned at 10.x in another (held to 10.x by ignoring their semver-major updates). Add an "If this library stops being maintained" section, naming the fallback of copying the MIT source into your application and owning it, to the root README.md and to the README of each of the twelve packages; ten READMEs exist, so the two packages without one (Dabbler.Security.DevIdp.Tool and Dabbler.Templates) get a short README packed as their PackageReadmeFile, the same way the other packages pack theirs.

### Session 11 — Bump to 0.2.0

**Releasable: no.**

Bump the single repository version to 0.2.0 ahead of the operator-tagged release: set DabblerVersion in Directory.Build.props to 0.2.0, change the -Version parameter default in tools/pack.ps1 (and the help text and example that name the current version) to 0.2.0, and add a 0.2.0 entry at the top of CHANGELOG.md naming the three breaking changes: the report expression engine's namespace is now Dabbler.Reports.Core.Expressions (moved from Dabbler.Components.Expressions); MudBlazor 9 is the floor (pinned at 9.12.0), and GridDataProviderExtensions.ToServerDataFunc now returns MudDataGrid's CancellationToken-taking ServerData delegate and passes that token to IGridDataProvider.FetchAsync; Dabbler.Components.Compat and the three Dabbler.Platform.Stubs.Components/.Reports/.Security providers are retired, the providers folded into Dabbler.Platform.Stubs under the namespace they already had.

### Session 12 — Retire what does not run

**Releasable: no.**

Retire what no longer runs in dabbler-platform. Delete the v1 Python UAT runner under uat_runner/ (cut 1) and remove its entries from the project section of AGENTS.md and from README.md; the Playwright tests under tests/AcmeCoffeeOnline.Playwright/ stay untouched. Move docs/planning/access-to-blazor-cli-spec.md, docs/planning/project-status.md and the two root v1 summaries CRUD_SCAFFOLDING_FINAL_SUMMARY.md and FEEDBACK_REMEDIATION_SUMMARY.md into docs/planning/_archived/ with git mv (cut 4), fix the relative links inside the moved files so they still resolve from their new location, and update the full-path pointers to the moved files in docs/planning/brief.md and docs/planning/solution-plan.md. Add docs/session-sets/README.md, one paragraph stating that its 47 directories are read-only v1 history (cut 13), and correct the session-set count in AGENTS.md from 45 to 47.

### Session 13 — Prune the v1 guidance

**Releasable: no.**

Prune the retired v1 workflow from the repository's standing guidance. In docs/planning/project-guidance.md remove the dated v3 banner and every passage that only describes the retired v1 router and its workflow: route() task types and tiers, ai_router / ai-router and the W2 uat_runner, session-state.json, ai-assignment.md and next-orchestrator recommendations, session sets and their specs, disposition.json, UAT Review Session sets, maxout overrides, and pointers to the deleted docs/ai-led-session-workflow.md and session-set-authoring-guide.md. Keep the Principles (Architecture, Security And Organization Model, UI Philosophy, Testing Hierarchy, Human UAT Philosophy, Precedence) and the testing, UAT, MudBlazor, CRUD, filter, styling and code-style Conventions, rewording a kept rule only where a sentence of it leans on a v1 term (for example 'session set' becomes 'session'). Do the same in docs/planning/lessons-learned.md: drop the banner, delete the lessons that only describe the retired router (register_session_start, route(task_type=documentation), router complexity score, verifier selection, task-type calibration, truncation, verification cost guard, routed-output persistence, session-state.json, ai-assignment.md, verification prompt template, disposition.json gate and the like), keep the repository-specific failure patterns (Blazor, MudBlazor, Playwright, Docker, .NET, UAT authoring, testing), and strip v1 references from the kept entries and the file's preamble. Finally remove the v1 caveat sentence from the Required reading list in AGENTS.md's project section.

### Session 14 — Bump to 1.0.0

**Releasable: no.**

Bump the single repository version to 1.0.0: set DabblerVersion in Directory.Build.props and the -Version parameter default in tools/pack.ps1 (with its help text and usage example that name the current version) to 1.0.0, and add a 1.0.0 entry at the top of CHANGELOG.md stating that it changes no API since 0.2.0 and that the twelve packages, the gates and docs/MAINTAINING.md are what the operator stands behind. Verification, the unit suite as the run of record and the close follow as the framework runs them.

### Session 15 — Build on push

**Releasable: no.**

Add a push build: a new .github/workflows/ci.yml that, on push to master and on pull_request, runs one windows-latest job which checks out the repository, sets up the .NET 10 SDK and runs the unit suite exactly as dabbler.yaml declares it (dotnet test Dabbler.Unit.slnf -m:1 --blame-hang-timeout 5m), with a concurrency group per ref that cancels in-progress runs and permissions contents: read. Then make the change-log honest by adding one line each under 0.1.0-preview.2 and 0.2.0 in CHANGELOG.md saying the version was never published to nuget.org and that 1.0.0 is the first release after 0.1.0-preview.1, and update docs/MAINTAINING.md so 'Where every gate lives' names the push build as the gate on master and on Dependabot pull requests, and 'Dependencies and the MudBlazor policy' says a Dependabot pull request merges only on a green check. Verification, the unit suite as the run of record and the close follow as the framework runs them.

### Session 16 — Templates reference the version being packed

**Releasable: no.**

Make the dabbler-app (Web and Web.Tests) and dabbler-min templates reference the Dabbler.* version being packed instead of the hard-pinned 0.1.0-preview.1, and bring the scaffolded-app build into a local gate. Each Dabbler.* PackageReference under templates/Dabbler.Templates/content/** carries a single version token; a pack-time target in Dabbler.Templates.csproj copies the template .csproj files to the intermediate directory with the token replaced by $(DabblerVersion) and packs those copies in place of the originals, so Directory.Build.props stays the one source of truth and the template engine needs no new symbol. The two pieces of template prose that name 0.1.0-preview.1 (the dabbler-app README and nuget.config comment) are reworded to say the references are pinned to the exact version of the Dabbler.Templates package that scaffolded the app. Then a `smoke` suite is added to dabbler.yaml (expensive: true, required_for_close: false so only a bump session that packs runs it) that sets DABBLER_PACK_OUTPUT to the resolved pack-output directory -- failing when it is absent rather than letting the tests skip -- and runs tests/Dabbler.Packaging.Smoke with the same ServesBelowFiveHundred exclusion CI's Validate And Pack job uses; docs/MAINTAINING.md says a bump session runs it.

### Session 17 — Make the push build green on a clean machine

**Releasable: no.**

Make the push build (ci.yml) green and meaningful on a clean machine. First, stop the AcmeCoffeeOnline.Api.Tests race on a fresh LocalDB: every test class owns a WebApplicationFactory<Program> that runs DatabaseInitializer.InitializeAsync (EnsureCreated plus seed) against the one database AcmeCoffeeOnline_Dev, and xUnit runs the classes in parallel, so on a machine without that database several factories create and seed it at once. Adding tests/AcmeCoffeeOnline.Api.Tests/xunit.runner.json with parallelizeTestCollections false, copied to the output directory by the project file, makes the first class create and seed it and the rest find it. Second, make the push build run the 46 pack-dependent tests it currently skips (41 in Dabbler.PublicSurface.Tests, 4 in Dabbler.Templates.Tests, FullPipeline_endToEnd_against_smoke_fixture in Dabbler.CrudSlice.Tests): before the unit-suite step, ci.yml builds Dabbler.Publish.slnf in Release and then Dabbler.Reports.Export and Dabbler.Reports.Blazor directly, as build.yml's Validate And Pack job does, runs tools/pack.ps1 with -OutputDir and -ScratchCacheDir under RUNNER_TEMP, and exports that output folder as DABBLER_PACK_OUTPUT for the test step.

### Session 18 — Scaffolded-app restores take their sources from nuget.config

**Releasable: no.**

Make the four scaffolded-app restores in the test suites take their package sources from a local nuget.config instead of inline --source arguments, because the .NET 10.0.403 SDK resolves an inline nuget.org URL as a path under the project directory (NU1301). In tests/Dabbler.Templates.Tests/CrudServiceTemplateTests.cs and CrudPageTemplateTests.cs the tests already write a nuget.config (clear, pack output, nuget.org) into the temp dir, so the restore simply drops both --source arguments. In tests/Dabbler.Packaging.Smoke/PackagingSmokeTests.cs, DabblerMin_Template_InstallsAndBuildsClean_SingleProjectShape is made to call the file's existing WriteLocalNugetConfig before its restore, DabblerApp_ScaffoldedHost_ServesBelowFiveHundredOnRootRequest (which already writes it) drops the --source arguments, and WriteLocalNugetConfig itself is made to clear the sources and list the pack output and nuget.org (matching the Templates helpers), with the comment in DabblerApp_Template_InstallsAndBuildsClean that claimed the .csproj restore was unaffected corrected. No test changes what it asserts.

### Session 19 — Take DabblerPdf 1.1.3

**Releasable: no.**

Raise the platform's DabblerPdf pin from 1.1.2 to 1.1.3 by changing its PackageVersion in Directory.Packages.props, the one place the platform pins it. DabblerPdf 1.1.3 is on nuget.org, changes no API since 1.1.2, and is the first DabblerPdf version published from CI with trusted publishing. The step is proved by the props file naming DabblerPdf at 1.1.3 and nowhere at 1.1.2, by tests/Dabbler.Reports.Blazor.Tests (which holds the PDF export tests) passing, and by the .nuspec inside the packed Dabbler.Reports.Core nupkg declaring its DabblerPdf dependency at 1.1.3 after a Release build and tools/pack.ps1 -OutputDir pack-output -CleanOutput.

### Session 20 — Bump to 1.0.1

**Releasable: no.**

Bump the lock-step platform version from 1.0.0 to 1.0.1 so Session 19's DabblerPdf 1.1.3 floor reaches consumers. Set DabblerVersion in Directory.Build.props to 1.0.1, and in tools/pack.ps1 set the -Version default to 1.0.1 along with the help text that names it as the default (and the two help examples that name 1.0.0), so neither file names 1.0.0 any more. Add a `## 1.0.1` entry to CHANGELOG.md above `## 1.0.0`: no API changes since 1.0.0; the DabblerPdf dependency moves from 1.1.2 to 1.1.3, which changes no DabblerPdf API either. The step is proved by the two files naming 1.0.1 and not 1.0.0, by the changelog heading order, and by a Release build, tools/pack.ps1 -OutputDir pack-output -CleanOutput, and the Packaging.Smoke suite (ServesBelowFiveHundred excluded, DABBLER_PACK_OUTPUT at pack-output) passing against the packed 1.0.1 nupkgs.

### Session 21 — The framework runs the Playwright E2E gate

**Releasable: no.**

Make the Playwright E2E gate something the framework runs instead of a person. Add tools/e2e.ps1, one cold-start command: it picks podman when it is on PATH and `podman info` succeeds, otherwise docker (DABBLER_CONTAINER_ENGINE overrides), and stops with a plain message when no engine is up or ../dabbler-uat-dsl/pack-output is missing; it brings docker/docker-compose.infra.yml up, waits for dabbler-sqlserver to be healthy, takes the app stack down, drops only the AcmeCoffeeOnline_Dev database through sqlcmd in the sqlserver container, rebuilds and starts docker/docker-compose.app.yml, polls the api (5011) and web (5010) /health endpoints under a time limit, then runs dotnet test tests/AcmeCoffeeOnline.Playwright/ with --blame-hang-timeout and a trx logger into a results directory parameter, and exits with the test run's exit code. Then declare an `e2e` suite in dabbler.yaml that runs the script (expensive, not required for close until Session 22 makes it green, covering src/Apps, src/Libs, tests/AcmeCoffeeOnline.Playwright, docker and Directory.Packages.props), and rewrite docs/MAINTAINING.md (Where every gate lives, Releasing), the Building and testing part of AGENTS.md outside the managed fence, and docs/packaging/release-checklist.md so they say the framework runs Playwright as the `e2e` suite and the operator only needs Podman or Docker running.
