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
| 22 | Fix the 12 Playwright E2E failures | no | 2026-11-10 |
| 23 | The E2E fixes that need a public API change | — | not declared |
| 24 | The Debug solution build is green under .NET SDK 11.0.100 | no | 2026-11-12 |
| 25 | MasterDetailExpansionShell moves to Dabbler.Components and gets its Add button | no | 2026-11-12 |
| 26 | One filter-operator vocabulary, and no silently dropped filter | no | 2026-11-12 |
| 27 | Shipped text, the download helper and the READMEs | no | 2026-11-12 |
| 28 | Take DabblerPdf 1.3.0, and measure report text the way it is drawn | no | 2026-11-12 |
| 29 | Report text can use a Unicode TrueType font | no | 2026-11-12 |
| 30 | Bump to 2.0.0 | no | 2026-11-12 |
| 31 | Clear the release build's nullable and vulnerability warnings | no | 2026-11-13 |
| 32 | Clear the solution's remaining audited packages | no | 2026-11-13 |
| 33 | Bump to 2.0.1 | no | 2026-11-13 |
| 34 | Move bUnit to 2.x | no | 2026-11-13 |
| 35 | Clear the shipped packages' build warnings | no | 2026-11-13 |
| 36 | Clear the apps', tests' and workflows' warnings | no | 2026-11-13 |
| 37 | Bump to 2.0.2 | — | not declared |

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

### Session 22 — Fix the 12 Playwright E2E failures

**Releasable: no.**

Make the Playwright `e2e` suite green by fixing the root causes of its 12 failures (all seen again on 2026-11-10 against a fresh AcmeCoffeeOnline_Dev, 161 passed / 12 failed). Four causes, none of which needs a public API change. (1) Save race: OrderNestedItemsSaveTests (3 tests) and ValidationTests.ProductDetail_UnitPriceBoundary_EnforcesMinimumAsync click Save and then read or restore through the API before the UI's PUT has landed. The order tests read stale item counts and quantities. The validation test's API restore of product 17 is overwritten by the late save, which leaves it named '... boundary', so ProductAdministrationWorkflowTests.E2E_WF_04 then cannot find 'Blonde Roast - Decaf - Espresso'. Fix: those tests wait for the save to finish with the fixture's established ClickSaveAsync/AssertSaveDisabledAsync pattern before touching the API. (2) Seed data: src/Apps/AcmeCoffeeOnline.Web/user-roles.json grants jdoe@acmecoffee.com CrossOrgUserAdmin. That entry was introduced by the 2026-05-11 id-to-email rewrite (the old key "3" was asmith). It lets the BackofficeUser persona into /admin/products and /admin/security, so the three EdgeCaseTests.AccessDenied_BackofficeUser_* fail. Fix: drop jdoe's entry from the web file. (3) UsersAndRoleAssignmentWorkflowTests.CrossOrgUserAdmin_* looks for users-row-14-*, but the users grid keys rows by AdminUserRow.UserId, which is documented as the user's email (users-row-robert.williams@example.com-*). Fix: the test selects by the target's email. (4) The four Generated UATs (AdminUsersCrossLinksUat, ComposableCrudHelpersUat, SensitiveFieldMaskingAuditUatRemediationUat, UnifiedMasterDetailsCompositeUat) are not in the repository. They are untracked, gitignored May-2026 output of the retired v1 uat_runner, and compile into the suite only on this machine. Per the operator's decision, the Playwright project stops compiling Generated/**, so the gate is the same on every machine. Tracked equivalents (AdminUsersCrossLinksUatTests, SensitiveFieldMaskingUatCoverageTests) keep that coverage. tools/e2e.ps1 gains an optional -Filter so each step's check runs its classes from a cold start: rebuilt app stack, fresh database. No cause is left for Session 23, so the `e2e` suite becomes required_for_close: true, checked by tools/e2e.ps1 exiting 0 on a fresh database, and the prose saying Session 22 will make it green is updated. No packed library changes, so CHANGELOG.md is not touched.

**Amended after acceptance:**

- 2026-11-10 — step 'e2e-filter': its files: The step's cold-start check exposed a reproducible race (twice in a row; passes warm): SmokeTests.DashboardCards_NavigateToEntityPagesAsync goes back to /dashboard before the first, cold render of /orders has replaced it, then clicks the stale dashboard card. It is the intermittent 13th failure from Session 21's run of record and would keep the e2e gate red. Fix: wait for the dashboard to leave the page after each navigation; assertions unchanged. (claude-code (anthropic, claude-opus-5-5))
- 2026-11-10 — step 'e2e-required-for-close': its files: The full cold-start e2e check failed only on E2E_WF_04 (passed in the save-race filtered run): WaitForQuietAsync returns before the 300 ms debounced search begins, so the availability click lands mid-rerender and is dropped. The gate cannot be required while it races; the test waits until the grid shows only the searched product before clicking. Assertions unchanged. (claude-code (anthropic, claude-opus-5-5))
- 2026-11-10 — step 'e2e-required-for-close': its files: The second full cold-start e2e run (E2E_WF_04 now green) failed only on SensitiveFieldMaskingUatCoverageTests.Uatv1 (green alone 3/3): its setup clicks Remove on the BackofficeUser mask exemption Uatv3 left behind and closes the page on NetworkIdle, before the removal lands. The setup helpers now wait for the exemption row to disappear or appear. Assertions unchanged. (claude-code (anthropic, claude-opus-5-5))

### Session 24 — The Debug solution build is green under .NET SDK 11.0.100

**Releasable: no.**

Make the Debug solution build green under .NET SDK 11.0.100 by setting <StaticWebAssetsEnabled>false</StaticWebAssetsEnabled> in tools/Dabbler.Security.DevIdp.Tool/Dabbler.Security.DevIdp.Tool.csproj, with a comment explaining that the tool serves no static files (no wwwroot, no UseStaticFiles or MapStaticAssets), so SDK 11's Debug-only Hot Reload browser-tools module has no static web assets to attach to and otherwise fails the build. No other project changes.

**Rolled back:**

- 2026-11-12 — session 24 — Rollback to Start of This Session from the Work Explorer (operator)

### Session 25 — MasterDetailExpansionShell moves to Dabbler.Components and gets its Add button

**Releasable: no.**

Move MasterDetailExpansionShell, its contexts (MasterDetailShellContexts.cs) and PickOrAddDialog from Dabbler.Security.Blazor (namespace Dabbler.Security.Blazor.Components.Admin.Shared) to Dabbler.Components (namespace Dabbler.Components.MasterDetail), taking the finished move from the local branch wip/session-24-shell-move, fixing the four Security admin tabs and five reference-app callers, and moving the two test classes to Dabbler.Components.Tests with no forwarding type left behind. Then make the shell's documented Add button real: ShowAddAction defaults to false and, with ShowGrid true, renders a right-aligned Add button (data-testid {TestId}-add-button, label AddLabel default "Add", disabled while Working) between FilterContent and GridContent whose click calls ResetSelection() and raises OnAddRequested; remove the dead Mode/ShellMode and OnRowSelected from the shell and OnRowSelected from PickOrAddDialog (whose ShowAddAction also defaults to false and whose OnAdd now fires); drop the stale 'Implemented in Session N' doc notes; switch OrderDetail's hand-rolled Add Product button to the shell's and point the three Order Playwright classes at order-items-shell-add-button; replace the two structural shell tests with behavioural ones. Finally correct the documents that describe the shell's package, namespace and knobs.

### Session 26 — One filter-operator vocabulary, and no silently dropped filter

**Releasable: no.**

Make the filter-operator vocabulary one, and stop Dabbler.Api.Querying dropping filters without a word. DeclarativeFilterPanel emits the FilterOperators constants (Contains, Equals, GreaterThanOrEqual, LessThanOrEqual) in place of the literals "contains", "eq", "gte" and "lte"; the reference app's workarounds go (NormaliseOperator in SupplierGridDataProvider, the "contains" comparisons in CustomerDataProvider, OrderDataProvider and ProductDataProvider, which compare with FilterOperators.Contains); the FilterDescriptor doc comment in GridDataRequest.cs names the constants; the Dabbler.Components.Tests MasterDetail tests assert the constants, and a new DeclarativeFilterPanelTests test passes the descriptors the panel raises unchanged to a StubGridDataProvider<T> and shows a text contains, an enum equals and a date range filter its rows. Then QueryableExtensions converts filter values (from a string or a JsonElement, invariant culture, ISO 8601) to DateOnly, TimeOnly, DateTime, DateTimeOffset, Guid, enum members by name or number, and the numeric types; applies In when the value is an array; and throws a new public GridQueryException naming the property and the operator for an unknown property in a filter or a sort, an unknown operator, an operator the property's type does not support, and a value that cannot be converted, so nothing is dropped. GridQueryException derives from ArgumentException, so callers that caught the old sort ArgumentException still catch it and AcmeCoffeeOnline.Api's existing ErrorHandlingMiddleware already answers it with 400 and a problem-details body; a test proves that through the Supplier query endpoint. QueryableExtensionsTests gets one test per rule, and the Dabbler.Api.Querying README states the rules.

### Session 27 — Shipped text, the download helper and the READMEs

**Releasable: no.**

Remove internal workflow references ("Session N", "Will be replaced ... in Session 5") from the XML docs and comments of what ships under src/Libs and from the packed tools/Dabbler.CrudSlice README.md and TemplatePreflight.cs, rewording each to say what the member does or deleting it, with no code change. Give dabbler-download.js a supported C# entry point, DabblerDownloadExtensions.DownloadFileAsync(this IJSRuntime, fileName, byte[] content, mimeType, CancellationToken) in Dabbler.Components, which imports ./_content/Dabbler.Components/dabbler-download.js and calls downloadFile with the base64 of the content; DabblerReportPage and ComplianceReviewTab call it instead of naming the script path, with DabblerDownloadExtensionsTests covering the module path and the arguments. Bring the package READMEs in step with the API: the Dabbler.Components smallest example uses DeclarativeFilterPanel with Config (a FilterConfig<T>) and FilterChanged as CustomerList.razor does, and new sections state EditSession<T>'s semantics (Live is the constructor's instance, NotifyChanged serialises the whole object per call, Undo/CommitFrom copy public get+set properties including init-only and nothing else) and DownloadFileAsync's (content crosses the circuit as base64; serve large files from an endpoint); every src/Libs README states MudBlazor 9.x, drops --prerelease and 0.1.0-preview.1, and restates or removes each "MudBlazor 8.x" caveat after checking it against 9.12.0. Reproduce in tests/AcmeCoffeeOnline.Web.Tests the empty-body response for _content/Dabbler.Components/dabbler-download.js under a non-Development WebApplicationFactory host and document in the Dabbler.Testing.Blazor README what the reproduction shows and what makes the asset load (or, if it does not reproduce, only what the consumer observed).

### Session 28 — Take DabblerPdf 1.3.0, and measure report text the way it is drawn

**Releasable: no.**

Move the platform's DabblerPdf pin from 1.1.3 to 1.3.0 in Directory.Packages.props and prove the packed Dabbler.Reports.Core nupkg declares DabblerPdf 1.3.0. Then make report text measured the way it is drawn: add one internal font resolver in Dabbler.Reports.Core (ReportFontResolver: style, then ReportMetadata defaults, then the Base 14 mapping) used by both OpenPdfTextMeasurer and PdfStyleResolver, have the measurer count lines with TextLayout.CountLines and PdfPageRenderer draw the lines of TextLayout.WrapText, and delete both private wraps (WrapParagraph/CountWrappedLines and PdfPageRenderer.WrapText), so runs of spaces survive, an unsized style is measured at the report's DefaultFontSize, an unfamilied style in the family DefaultFontFamily maps to, and an over-wide word breaks at characters. Finally have DabblerReportPage compose with an OpenPdfTextMeasurer built from its report definition so CanGrow and CanShrink act in the shipped page. Tests go in tests/Dabbler.Reports.Blazor.Tests in files named after the classes they test.

### Session 29 — Report text can use a Unicode TrueType font

**Releasable: no.**

Let report text print in a Unicode TrueType font. Add a public ReportFonts in Dabbler.Reports.Core (namespace Dabbler.Reports.Core.Layout) that registers a family name, case-insensitively, against a regular .ttf file and optional bold, italic and bold-italic files. The internal ReportFontResolver from Session 28 takes an optional ReportFonts: when a style's family, or the report's DefaultFontFamily, names a registered family it creates the font with BaseFont.CreateFont(path, BaseFont.IdentityH, true), choosing the file for the style's weight and slant and falling back to the regular file (no synthetic bold or italic); any other family keeps the Base 14 mapping. OpenPdfTextMeasurer and PdfExporter take an optional ReportFonts and pass it to the resolver (PdfExporter through PdfStyleResolver), so text is measured and drawn in the same font. Dabbler.Reports.Blazor gains services.AddDabblerReportFonts(fonts => ...), which registers one ReportFonts singleton; DabblerReportPage resolves it when registered and hands it to the OpenPdfTextMeasurer it composes with and, through ReportPdfExporterFactory.Create, to the PdfExporter its PDF export uses. The Liberation Sans fixture and its licence are vendored from ../dabbler-pdf under tests/Dabbler.Reports.Blazor.Tests/Fonts. Tests in tests/Dabbler.Reports.Blazor.Tests, in files named after the classes they test, prove: PdfPig extracts Polish, Cyrillic and Greek text from a PDF exported with a registered family; that field's CanGrow height equals what TextLayout.MeasureHeight gives with the registered font; a bold style in a family registered with only a regular file uses the regular file; an unregistered family still maps to the Base 14. Finally the Dabbler.Reports.Export README's font paragraph is rewritten: the Base 14 families and their Windows-1252 limit, how to register a Unicode font, and what it does not do (no shaping or right-to-left text, nothing beyond U+FFFF, no fallback between fonts).

### Session 30 — Bump to 2.0.0

**Releasable: no.**

Bump the single repository version from the unreleased 1.0.1 to 2.0.0 (major, approved by the operator on 2026-11-12). Set DabblerVersion in Directory.Build.props, and the -Version default and the versions its help text names in tools/pack.ps1, to 2.0.0. Turn the unreleased `## 1.0.1` entry of CHANGELOG.md into `## 2.0.0` directly above `## 1.0.0`, recording what Sessions 25-29 changed relative to 1.0.0: the breaking moves and removals (MasterDetailExpansionShell, its context types and PickOrAddDialog moving to Dabbler.Components / Dabbler.Components.MasterDetail; ShowAddAction rendering an Add button and defaulting to false, with Mode, ShellMode and OnRowSelected removed; DeclarativeFilterPanel emitting the FilterOperators constants; Dabbler.Api.Querying throwing GridQueryException instead of skipping a filter), each with its one-line migration; what was added (AddLabel, the new filter types and In, GridQueryException, DownloadFileAsync, ReportFonts and AddDabblerReportFonts); and what changed (PDF report text layout and measurement, DabblerReportPage measuring CanGrow/CanShrink fields, and DabblerPdf moving from 1.1.2 to 1.3.0, which supersedes the entry's 1.1.3 line and its 'No API changes' line, both no longer true). Prove the bump with a Release build, a pack into pack-output and the smoke suite against the 2.0.0 packages.

### Session 31 — Clear the release build's nullable and vulnerability warnings

**Releasable: no.**

Clear the Release build's CS8669 warnings and the restore's NuGet audit warnings, and make the release procedure deprecate retired package IDs as well as unlist them. CS8669 is removed at its cause by dropping the `?` from the reference-type markup type arguments (`T="string?"` on the text filter and `T="object?"` on the foreign-key `MudSelect`/`MudSelectItem` in `DeclarativeFilterPanel.razor`, and the five `T="string?"` in `CustomerDetail.razor`), leaving the `bool?`/`decimal?` value-type arguments, the bound values and the rendered output unchanged. The audit warnings are cleared by raising pins in `Directory.Packages.props`: `Microsoft.SourceLink.GitHub` from 8.0.0 to 10.0.111, the lowest version GHSA-23fw-v26w-5fgq names as patched (no 8.x fix exists), which brings `Microsoft.Build.Tasks.Git` 10.0.111; and the EF Core family (`Microsoft.EntityFrameworkCore`, `.Design`, `.InMemory`, `.Sqlite`, `.SqlServer`) from 10.0.5 to 10.0.15, within the 10.x policy. `Microsoft.EntityFrameworkCore.SqlServer` 10.0.15 depends on `System.Security.Cryptography.Pkcs` 10.0.13, which both GHSA-846f-f764-76mx and GHSA-2fmv-rv2c-59x7 name as patched, so the fix reaches every consumer of `Dabbler.Security` through its nuspec without a direct `PackageReference`. `Microsoft.EntityFrameworkCore.Sqlite` 10.0.15 also brings `SQLitePCLRaw` 2.1.13, which clears the NU1903 on `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 (GHSA-2m69-gcr7-jv3q) in `Dabbler.Security.DevIdp`; the restore check requires that too. Because a consumer that pins EF Core 10.0.5 directly while referencing `Dabbler.Security` would hit an NU1605 downgrade, the EF pins in the `dabbler-min` and `dabbler-app` templates and in the `SmokeApp`/`SmokeApi` template-test fixtures move to 10.0.15 with it. Release step 4 of `docs/MAINTAINING.md` is rewritten so that each retired ID is deprecated as *Legacy* (alternate package `Dabbler.Platform.Stubs` for the three `Dabbler.Platform.Stubs.*` providers, none for `Dabbler.Components.Compat`) and then unlisted.

### Session 32 — Clear the solution's remaining audited packages

**Releasable: no.**

Clear the two NuGet audit findings that a restore of the whole solution still reports after Session 31. `Microsoft.OpenApi` 2.0.0 (NU1903, GHSA-v5pm-xwqc-g5wc, patched in 2.7.5) reaches `src/Apps/AcmeCoffeeOnline.Api` and `tests/AcmeCoffeeOnline.Api.Tests` through the `Microsoft.AspNetCore.OpenApi` 10.0.5 pin; that one pin in `Directory.Packages.props` moves to 10.0.15, which depends on `Microsoft.OpenApi` [2.13.0, 3.0.0), within the 10.x policy in `docs/MAINTAINING.md`. `AngleSharp` 1.1.2 (NU1902, GHSA-pgww-w46g-26qg, patched in 1.5.0) reaches every bUnit test project through `bunit` 1.37.7 -> `bunit.web`, and no bUnit 1.x carries a patched AngleSharp, so a `PackageVersion` for `AngleSharp` at 1.5.0 is added to `Directory.Packages.props` with a direct `PackageReference` in each of `tests/AcmeCoffeeOnline.Web.Tests`, `tests/Dabbler.Components.Tests`, `tests/Dabbler.Reports.Blazor.Tests`, `tests/Dabbler.Security.Blazor.Tests` and `tests/Dabbler.Testing.Blazor.Tests`, and the same reference at `Version="1.5.0"`, with a one-line comment saying it lifts bUnit's transitive AngleSharp past the advisory, in the `dabbler-app` template's generated test project, so the fix reaches consumers of `Dabbler.Templates`.

**Amended after acceptance:**

- 2026-11-13 — step 'anglesharp-lift': its files: AngleSharp 1.5.0 moved IHtmlCollection<T>'s int indexer onto IReadOnlyList<T>, so bunit 1.37.7's compiled RefreshableElementCollection indexer throws MissingMethodException; the three FindAll(...)[i] call sites in ReportViewerTests and UserEditPageTests move to ElementAt(i) (enumeration is unaffected), keeping the advisory fix without bunit 2.x. (claude-code (anthropic, claude-opus-5-5))

### Session 33 — Bump to 2.0.1

**Releasable: no.**

Bump the single repository version from 2.0.0 to 2.0.1 (patch, as the session plan proposes). Set DabblerVersion in Directory.Build.props, and the -Version default and the versions its help text names in tools/pack.ps1, to 2.0.1. Add a `## 2.0.1` entry to CHANGELOG.md directly above `## 2.0.0` saying there is no API change since 2.0.0, that `Dabbler.Security`'s `Microsoft.EntityFrameworkCore.SqlServer` dependency moves from 10.0.5 to 10.0.15 (Session 31), which brings `System.Security.Cryptography.Pkcs` 10.0.13 and clears GHSA-846f-f764-76mx and GHSA-2fmv-rv2c-59x7, and that the `dabbler-app` template's test project references `AngleSharp` 1.5.0 (Session 32), lifting bUnit's transitive AngleSharp past GHSA-pgww-w46g-26qg. Prove the bump with a Release build, a pack into pack-output and the smoke suite against the 2.0.1 packages.

### Session 34 — Move bUnit to 2.x

**Releasable: no.**

Move the solution and the `dabbler-app` template from `bunit` 1.37.7 to 2.15.2, which is built against `AngleSharp` 1.10.1, so Session 32's AngleSharp lift and its `ElementAt(i)` workaround both go. In `Directory.Packages.props` the `bunit` pin moves to 2.15.2, the `Microsoft.AspNetCore.Components.Web` and `Microsoft.AspNetCore.Components.Authorization` pins move from 10.0.5 to 10.0.15 (bUnit 2.15.2 depends on `Microsoft.AspNetCore.Components.*` 10.0.13, which would make the direct 10.0.5 reference in `tests/Dabbler.Security.Blazor.Tests` an NU1605 downgrade; 10.0.15 is within the 10.x policy in `docs/MAINTAINING.md`), and the `AngleSharp` `PackageVersion` is removed together with the direct `AngleSharp` references in `tests/AcmeCoffeeOnline.Web.Tests`, `tests/Dabbler.Components.Tests`, `tests/Dabbler.Reports.Blazor.Tests`, `tests/Dabbler.Security.Blazor.Tests` and `tests/Dabbler.Testing.Blazor.Tests`. The template's generated test project raises `Version="1.37.7"` to `2.15.2` and drops its `AngleSharp` reference and comment. The bUnit test code in those five projects and the template's `SampleItemListTests.cs` is ported to the 2.x API (`TestContext` -> `BunitContext`, `RenderComponent` -> `Render`, `IRenderedFragment` -> `IRenderedComponent<...>`, `AddTestAuthorization` -> `AddAuthorization`, `TestServiceProvider` -> `BunitServiceProvider`, and whatever else the build reports), Session 32's three `ElementAt(i)` calls in `ReportViewerTests.cs` and `UserEditPageTests.cs` return to `[i]`, and the bUnit example in `src/Libs/Dabbler.Platform.Stubs/README.md` moves to the 2.x API.

### Session 35 — Clear the shipped packages' build warnings

**Releasable: no.**

Clear the build warnings the shipped packages print in a Release build, without changing a public API or behaviour. In `src/Libs/Dabbler.Api.Querying/GridQueryRequest.cs` give the fifteen undocumented members of `GridQueryRequest`, `GridCompositeFilter`, `GridFilterLogic` and `GridQueryResponse<T>` a `<summary>` that says what each carries, as the code does (CS1591). In `src/Libs/Dabbler.Security/Services/` document `EmailAlreadyExistsException.Email` (CS1591), add `<param name="ct">` to the two `ClaimsPrincipal` overloads in `IAdminUserQueryService.cs` (CS1573), and repoint or turn into `<c>` the class-level `paramref`/`typeparamref` tags in `RoleAssignmentAuthority.cs` and `RowRestrictionExpressionBuilder.cs` that name no class parameter (CS1734, CS1735). In `AdminUserQueryService.cs` route the no-argument `GetUsersAsync` and `GetActorAssignedOrganizationIdsAsync` through the existing nullable, fail-closed helpers so a null `HttpContext` keeps today's empty result, with no `!` and no public signature change (CS8604). Wrap each deliberate use of the obsolete legacy configuration members in `Persistence/EfCoreSecurityStore.cs`, `Services/RowFilterService.cs` and `Dabbler.Security.Blazor/Components/DabblerRoleSwitcher.razor` in narrow `#pragma warning disable/restore CS0618` pairs with a one-line comment that the legacy member stays supported until it is removed (CS0618). Replace `DisableGutters="true"` with `Gutters="false"` on the `MudExpansionPanel` in `MasterDetailExpansionShell.razor` (MUD0002), restoring the gutter-less layout, and add a test to `MasterDetailExpansionShellTests.cs` asserting the panel renders without MudBlazor's gutters class. Afterwards a Release build of `Dabbler.Publish.slnf` prints no warning.

### Session 36 — Clear the apps', tests' and workflows' warnings

**Releasable: no.**

Clear the build warnings left in the apps, the test projects and the workflows, so a Release build of `Dabbler.Platform.sln` prints no warning and the workflows stop running on Node.js 20. In `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Orders/OrderDetail.razor` write the `Title=` on the one `MudButton` and the two `MudIconButton`s as `title=`, which renders the same HTML attribute, leaving the real `Title` parameter on `PropertyColumn` and `TemplateColumn` alone (MUD0002); in `Components/Shared/UserContextBar.razor.cs` wrap the `RoleSwitching` use in a narrow `#pragma warning disable/restore CS0618` pair with the one-line comment Session 35 uses (CS0618). Give each test file that tests the legacy `Dabbler.Security` members on purpose (`UserContextBarTests.cs`, `DabblerRoleSwitcherTests.cs`, and `EfCoreSecurityStoreTests.cs`, `JsonFileSecurityStoreTests.cs`, `SecurityConfigurationManagerTests.cs`, `RowFilterServiceTests.cs` in `tests/Dabbler.Security.Tests`) one file-level `#pragma warning disable CS0618` with a one-line comment saying so. Fix the remaining test warnings at their cause: `WebGuardrailTests.cs` asserts the organization name is not null before using it (CS8604); `PdfPageRendererTests.cs` returns a list whose element type matches what `ShowText.Text` gives (CS8619); `RoleListPageTests.cs` reads `Visible` through `GetState(x => x.Visible)` (MUD0012); `OrderCustomerPickerWorkflowTests.cs` uses `Assert.Single(customers, c => c.Email == email)` (xUnit2031). In `.github/workflows/ci.yml` and `build.yml` move the `actions/checkout`, `actions/setup-dotnet`, `actions/upload-artifact` and `actions/download-artifact` pins from their Node.js 20 v4 commits to v7.0.1, v6.1.0, v7.0.2 and v8.0.2 by full commit SHA with the version in the trailing comment, changing an input only where a release between v4 and the new version requires it; `NuGet/login` stays.
