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

### Session 1 — Author or import the solution plan

**Releasable: no.**

Author docs/planning/solution-plan.md for dabbler-platform, importing the plan material this repository already holds (docs/planning/brief.md as the operator's substance, docs/platform-overview.md, docs/planning/phase-3-platform-roadmap.md and docs/planning/project-status.md) into one plan a reader can act on. The file states the objective; the Production split the brief already answers (a generated application is a Blazor Server web tier calling a Web API tier, and only the API tier reaches SQL Server -- the platform's own reference app, AcmeCoffeeOnline, is built that way); the Handoff artifacts each separately running part is delivered as, with the standard command for its form (NuGet packages via tools/pack.ps1 for the libraries, templates and dotnet tools; dotnet publish IIS site packages for the Web and Api tiers of the reference app; dotnet ef migrations script --idempotent for the database); the modules -- the Dabbler.* libraries under src/Libs, the Dabbler.Templates package, the two dotnet tools under tools/ and the AcmeCoffeeOnline reference app under src/Apps -- with what each is responsible for, the contract it exposes (preconditions, guarantees, failure mode), the dependency direction, the reason for each cut and the cuts left deferred, and how each is a .csproj listed in Dabbler.Platform.sln reaching its siblings by <ProjectReference>; and the phases or feature areas with their key deliverables, each scoped to a handful of focused AI sessions and ordered so publishing Dabbler.CrudSlice.Tool unblocks the Access-migration consumer first. The six open decisions the brief lists (uat_runner, the Playwright and Packaging.Smoke manual gates, the CrudSlice.Tool release, the stale planning docs, the legacy branches and the dabbler-uat-dsl feed) are carried into the plan as deferred cuts with their options and the operator's leaning, for Session 2 to challenge and decide.

### Session 2 — Challenge the plan, then break it into numbered sessions

**Releasable: no.**

Challenge docs/planning/solution-plan.md and turn it into numbered sessions. Step one adds a Session 2 rationale section to the plan. It covers each module: whether it should exist, whether its contract is a promise a test can prove, and whether its dependency direction hides the decisions most likely to change. It also records what the tree shows that the plan got wrong. Reports.Core takes DabblerPdf directly, so the stated reason for the Reports.Export cut is false. Dabbler.Security carries the EF Core SqlServer provider into every web tier. Stubs.Reports uses Dabbler.Components.Data.Metadata. The pack allowlist has 16 IDs, not 17. Router 3.29.0 still hard-wires release tags to vsix-v, and a pack-only release would push a tag build.yml ignores. The section then decides each of the thirteen deferred cuts or says why it stays deferred. Step two appends Sessions 3 to 14 to docs/sessions/session-plan.md. Session 3 is the skeleton, adapted to the existing solution: it adds AcmeCoffeeOnline.Shared to Dabbler.Platform.sln and adds the production-split architecture test under the unit suite that is already declared. Then come the packaging-only session and the 0.1.0-preview.2 version bump that unblocks Dabbler.CrudSlice.Tool. Next are the Expressions move, surface reduction (ordered before MudBlazor 9 so the deleted Compat package is never migrated), the two MudBlazor 9 sessions, the maintainer guide with the gate rule and support policy, the 0.2.0 bump, retirement of what does not run, pruning of the v1 guidance, and the 1.0.0 bump. Each version is proposed as patch, minor or major for the operator to approve. Because of cut 12, each release is a version-bump session followed by the operator's hand tag on nuget-v<version>, not a release-headed session.
