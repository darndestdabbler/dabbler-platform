# Solution plan — dabbler-platform

> The plan this repository runs under the v3 Dabbler workflow. Its substance
> is the operator's, taken from `docs/planning/brief.md` (2026-10-03) and from
> the plan material already in the tree: `docs/platform-overview.md` (the
> canonical description of what the libraries are), `docs/planning/phase-3-platform-roadmap.md`
> (the Access-migration workflow the platform feeds) and `docs/planning/project-status.md`
> (stale, v1-era; kept only for the pointers it still owns).
>
> Session 2 challenges this plan's cuts and breaks it into numbered sessions.
> A later revision is another plan session amending this same file.

---

## Objective

Keep a set of reusable .NET 10 libraries, `dotnet new` templates and dotnet
tools that let a small team — mostly AI sessions under human supervision —
build secure, data-driven Blazor Server applications, and keep them published
to nuget.org so other solutions can consume them by version.

The platform is **a set of libraries, not a framework and not a product**. It
does not generate applications or dictate a domain model; it adds a focused
MudBlazor-native layer over the things enterprise apps reinvent: complex
filtering, restriction-based authorization, banded reporting with PDF export,
deterministic Playwright synchronization, composable CRUD helpers and an
opt-in stubbing story. `src/Apps/AcmeCoffeeOnline` is the reference
application the libraries are proven against, not a deliverable.

**The consumer drives the work.** `D:\Projects\dabbler-access-migration` (the
Access-to-Blazor migration solution) consumes `Dabbler.Templates` and
`Dabbler.CrudSlice.Tool` as build-time dotnet tools, and the applications it
generates consume the runtime packages;
`D:\Projects\Dabbler.NorthwindToBlazor` consumes eight runtime packages. The
platform is a dependency of that migration work, so the smallest change that
unblocks a consumer beats the more complete change that does not.

Success, stated so a reader can act on it:

1. Every package a consumer needs is on nuget.org at a version the consumer
   can pin. Today that is false for exactly one package:
   `Dabbler.CrudSlice.Tool` became packable after the only release tag
   (`nuget-v0.1.0-preview.1`, 2026-05-29) and was never pushed. Fixing that is
   the first numbered session.
2. `dotnet test Dabbler.Unit.slnf` is green on a clean checkout with no
   infrastructure, and every gate that is *not* in that suite is named, with
   the reason, where a reader will meet it.
3. A consumer can restore and build without a machine-local feed or a sibling
   checkout.
4. Every generated artifact — scaffolded page, service, template output —
   stays readable and overridable by the human developer who owns it.
5. The planning documents in `docs/planning/` describe what exists. Specs for
   things that were never built are archived, not left to mislead.
6. *(Added 2026-11-07.)* The source of every published package is public and
   the project URL each package carries resolves. `dabbler-platform` and
   `dabbler-pdf` were made public on 2026-11-07; this plan records it so the
   next reader does not re-check.
7. *(Added 2026-11-07.)* The libraries compile and test against the current
   MudBlazor major (9.x; 9.12.0 on nuget.org as of 2026-10-29), and a written
   support policy says how quickly a new MudBlazor major is adopted.
8. *(Added 2026-11-07.)* The published surface is the set the operator will
   stand behind, at a 1.0 version: no package carries a permanent preview
   label, no deprecated package is still listed, and the Stubs ship as one
   package.
9. *(Added 2026-11-07.)* `Dabbler.Reports.Core` and `Dabbler.Reports.Export`
   reference no UI framework, so the report engine and PDF export survive any
   UI churn.
10. *(Added 2026-11-07.)* A maintainer who has never seen this repository can
    build, test, pack and release it from one document, and every consumer is
    told that the fallback for a stalled library is to copy its MIT source
    into their own application.

Out of scope for this repository: the Access metadata extractor and the
migration workflow itself (they live in `dabbler-access-migration`), the UAT
DSL (it lives in `dabbler-uat-dsl`), and anything that would turn the platform
into a low-code or metadata-driven UI generator — a stance two independent
cross-provider reviews already upheld.

---

## Production split

The operator's answer, from the brief, is the standard split:

> **A generated application is a Blazor Server web tier that calls a Web API
> tier, and only the API tier reaches SQL Server.**

The reference application is built that way and holds the split honest:
`AcmeCoffeeOnline.Web` (Blazor Server) talks to `AcmeCoffeeOnline.Api`
(ASP.NET Core Web API) over HTTP, and `AcmeCoffeeDbContext` lives in the API
project alone. The compose stack runs them as separate services with SQL
Server behind the API (`docker/docker-compose.app.yml` over
`docker-compose.infra.yml`).

What the split means for the platform:

- **The web app is not a second data layer.** All *business* data access goes
  through the REST service layer. The web tier must never grow a `DbContext`
  for domain entities, and no library may push it into one.
- **One documented exception, and it does not breach the split.**
  Platform-internal stores — security configuration and the audit log — need
  host-startup persistence, so they do not go through the REST service layer.
  That exception is to the *REST-only* rule, not to the database rule: the EF
  Core store (`options.UseEfCoreStore()`, whose migrations are
  `src/Libs/Dabbler.Security/Migrations`) is hosted **by the API tier only**,
  and a web tier that needs platform configuration uses the JSON file store
  instead. The reference application holds to that — both
  `src/Apps/AcmeCoffeeOnline.Api/Program.cs:153` and
  `src/Apps/AcmeCoffeeOnline.Web/Program.cs:206` register `UseJsonFileStore`,
  and `AcmeCoffeeDbContext` exists only in the API project. **No web tier
  reaches SQL Server, with no exception.** Amending that is the operator's
  call, not a library's.
- **Libraries are tier-aware by package.** `Dabbler.Api.Querying` is the API
  tier's half of filtering (descriptors to server-side queries);
  `Dabbler.Components`, `Dabbler.Components.Compat`, `Dabbler.Security.Blazor`,
  `Dabbler.Reports.Blazor`, `Dabbler.Testing.Blazor` and the `Stubs` providers
  are the web tier's. `Dabbler.Security` and `Dabbler.Reports.Core` /
  `.Export` are tier-neutral. A library that forces a tier to take on the
  other tier's job is a bug in the cut, not a feature.
- **The split is a rule worth proving, not just stating.** This repository has
  no architecture test asserting it today; whether to add one (a test that no
  web-tier assembly references `Microsoft.EntityFrameworkCore` outside the
  platform-store exception) is a cut for Session 2.

---

## Handoff artifacts

This repository ships three kinds of thing, and only the first is a release.

| Part | Handed over as | Standard command |
|---|---|---|
| The packed `Dabbler.*` libraries under `src/Libs`, `Dabbler.Templates`, and the two dotnet tools (16 package IDs; see the allowlist in `tools/pack.ps1`) | NuGet packages on nuget.org | `pwsh tools/pack.ps1 -OutputDir pack-output -CleanOutput`, then CI publishes on an annotated `nuget-v<version>` tag |
| `AcmeCoffeeOnline.Web` (reference app, web tier) | an IIS site package | `dotnet publish src/Apps/AcmeCoffeeOnline.Web -c Release` |
| `AcmeCoffeeOnline.Api` (reference app, API tier) | an IIS site package | `dotnet publish src/Apps/AcmeCoffeeOnline.Api -c Release` |
| The database the API tier owns | an idempotent SQL migration script | `dotnet ef migrations script --idempotent` |

Notes a reader needs:

- **Packages are the only real handoff.** The reference app's publish outputs
  exist so the split is demonstrable and the E2E suite has something to drive;
  nobody deploys AcmeCoffeeOnline. `docker/docker-compose.app.yml` is its
  real-world form today.
- **`tools/pack.ps1` is the pack of record.** It is hermetic: every package ID
  in its in-repo allowlist resolves from the local scratch feed via NuGet
  source mapping, so a stale global cache cannot leak into a release.
  `Dabbler.Publish.slnf` is the matching solution filter.
- **No credential lives on a developer machine.** Publication is
  `.github/workflows/build.yml` on the `nuget-v<version>` tag, through trusted
  publishing (OIDC). `DabblerVersion` in `Directory.Build.props` is bumped
  first; it stamps every package.
- **`dabbler.yaml` has no `packaging:` block yet, on purpose.** The intended
  block (`pack` through `tools/pack.ps1`, no `push`) sits in that file as a
  comment. The release session adds it and does nothing else.
- Current state: everything is at `0.1.0-preview.1` on nuget.org **except**
  `Dabbler.CrudSlice.Tool`. `Dabbler.SchemaApplicator` and
  `ComponentCatalogCrawler` are `IsPackable=false` — the first is bundled into
  `Dabbler.CrudSlice.Tool`, the second is a dev helper.
- *(Added 2026-11-07.)* `Directory.Packages.props` pins MudBlazor at `8.15.0`
  while nuget.org serves `9.12.0`. The published packages declare a floor
  (`MudBlazor >= 8.15.0`), not a pin, so a consumer on 9.x restores cleanly
  and runs binaries that were never compiled or tested against 9.x. MudBlazor
  9 removed the obsolete synchronous APIs (`DialogService.Show<T>`,
  `Validate()`, `Reset()`, `MudSelect.Clear`), changed `MudDataGrid.ServerData`
  to take a `CancellationToken`, retired `MudGlobal` theming defaults and
  renamed the `MudTabs` class parameters. `Dabbler.Security.Blazor` (36 razor
  files) carries most of the exposure; `Dabbler.Components` exposes the
  `ServerData` shape through `IGridDataProvider`.
- *(Added 2026-11-07.)* The framework's `release: tag` declaration makes a
  `vsix-v<version>` tag and a `pack`-only declaration names its tag from
  `version.json`; `.github/workflows/build.yml` publishes on `nuget-v*`. Until
  the framework can name the tag, a release here is an operator's hand tag
  after an ordinary session bumps `DabblerVersion` (deferred cut 12).

---

## Modules

Every module below is a `.csproj` that reaches a sibling by
`<ProjectReference>` — never by a `PackageReference` to its own published
package — and every one of them **except `AcmeCoffeeOnline.Shared`** is listed
in `Dabbler.Platform.sln` at the repository root. That single omission is a
known gap, not a design choice: it is recorded under the reference application
below and as deferred cut 7, and it builds transitively today. That is what makes the
whole platform build and test as one solution while still packing as separate
NuGet packages (`Directory.Build.props` rewrites project references into
package dependencies at pack time). The dependency direction below is acyclic
and is the real direction in the project files today.

### Core, tier-neutral

- **`Dabbler.Components`** — MudBlazor-native UI primitives and the
  Query-by-Example filter panel (AND/OR groups, operators, presets, active
  filter chips) plus the composable CRUD helpers (`EditSession<T>`,
  `<DirtyAwareActionRow>`, `<MasterDetailExpansionShell>`).
  *Contract:* in — a host that has registered MudBlazor; out — components that
  compose into a page the developer still owns, and filter descriptors that
  `Dabbler.Api.Querying` understands; fails — by throwing at registration when
  a required service is missing, never by silently rendering nothing.
  *Depends on:* nothing in-repo. It is the root; everything UI-facing sits on
  it. *Why the cut:* it is the one package an adopter can take alone.
- **`Dabbler.Components.Compat`** — shims for the pre-MudBlazor-native
  component contract. *Contract:* in — code written against the retired
  surface; out — the same behaviour over the current components; fails — at
  compile time, deliberately, where no shim exists. *Depends on:*
  `Dabbler.Components`. *Why the cut:* so the narrowed contract can stay
  narrow without stranding existing callers; it is a candidate for retirement
  once no consumer references it.
- **`Dabbler.Api.Querying`** — the API tier's half of filtering: translates UI
  filter descriptors into server-side queries.
  *Contract:* in — a descriptor and an `IQueryable`; out — a composed query
  that never evaluates client-side; fails — by rejecting an unknown operator
  or field rather than ignoring it. *Depends on:* nothing in-repo. *Why the
  cut:* it is the half that must not be referenced by the web tier, so it is
  its own package — the cut *is* the production split.
- **`Dabbler.Security`** — restriction-based authorization (`ColumnRestriction`,
  `RowRestriction`, `UiRestriction`, masking and per-role exemptions), five
  baked-in roles with compiled-in privileges, and the EF Core / JSON-file
  configuration stores.
  *Contract:* in — a user principal and a configured store; out — a decision
  that is default-permissive and shaped only by explicit restrictions; fails —
  closed, by denying when configuration cannot be read. Role inheritance is
  intentionally unsupported. *Depends on:* nothing in-repo. *Why the cut:* the
  authorization model is the thing most likely to be audited, so it carries no
  UI dependency.
- **`Dabbler.Reports.Core`** — the banded report layout engine over
  `.rdf.json` report definition files (grouping, aggregates, running totals,
  subreports, conditional visibility).
  *Contract:* in — a valid report definition and a data source; out — a
  paginated layout model; fails — by reporting the offending definition node,
  not by rendering a blank page. *Depends on:* `Dabbler.Components` — and
  *(found 2026-11-07)* only for the nine files under
  `Dabbler.Components/Expressions/`, used by `BandLayoutEngine` and
  `LayoutEvaluationContext`. Nothing else in `Dabbler.Components` uses that
  folder. The reference drags MudBlazor into a package this plan calls
  tier-neutral; deferred cut 8 moves the folder.
- **`Dabbler.Reports.Export`** — PDF export of a composed layout.
  *Contract:* in — a layout from `Reports.Core`; out — a PDF byte stream;
  fails — by surfacing the underlying PDF error. *Depends on:*
  `Dabbler.Reports.Core`. *Why the cut:* so a consumer that only renders
  reports on screen does not take the PDF dependency.

### Web tier

- **`Dabbler.Reports.Blazor`** — the report viewer UI. *Depends on:*
  `Dabbler.Reports.Core`, `Dabbler.Reports.Export`, `Dabbler.Components`.
- **`Dabbler.Security.Blazor`** — the Blazor UI for security administration
  and the restriction-aware rendering helpers. *Depends on:*
  `Dabbler.Components`, `Dabbler.Security`.
- **`Dabbler.Testing.Blazor`** — the DOM "watermark" that tells Playwright when
  Blazor Server has finished an async render.
  *Contract:* in — a registration call in the app under test; out — a
  deterministic signal a test can await; fails — by timing out visibly rather
  than passing a flaky test. *Depends on:* nothing in-repo. *Why the cut:* an
  adopter with a Blazor Server E2E flake problem and nothing else can take
  this one package.
- **`Dabbler.Platform.Stubs`** and its three providers
  (`.Components`, `.Reports`, `.Security`) — opt-in stub registrations for
  bUnit tests, demos and the early life of a generated application.
  *Contract:* in — an explicit opt-in call; out — working services with
  obviously-fake data; fails — by never being the default, so production
  cannot acquire a stub by accident. *Depends on:* the three providers, which
  depend on `Dabbler.Components` / `Dabbler.Reports.Core` respectively. *Why
  the cut:* one package per subsystem so a test host takes only what it stubs.
- **`Dabbler.Security.DevIdp`** — a development identity provider. *Depends
  on:* nothing in-repo. Not packed on its own; it ships inside the tool below.

### Templates and tools

- **`Dabbler.Templates`** (`templates/Dabbler.Templates`) — the `dotnet new`
  templates `dabbler-app`, `dabbler-min`, `dabbler-crud-service` and
  `dabbler-crud-page`. `dabbler-app` is itself a web/API/shared split, so the
  template teaches the production split.
  *Contract:* in — `dotnet new <template>` with its parameters; out — a
  solution that builds and whose tests pass unmodified; fails — at template
  instantiation, before any file is written.
- **`Dabbler.CrudSlice.Tool`** (`tools/Dabbler.CrudSlice`, bundling
  `tools/Dabbler.SchemaApplicator`) — the `dabbler-crud-slice` dotnet tool that
  scaffolds a CRUD slice (service + page + tests) from an entity description,
  and applies the schema.
  *Contract:* in — an entity JSON description (e.g. `supplier.dabbler-entity.json`)
  and a target solution; out — readable, overridable source files a human can
  edit afterwards; fails — by refusing to overwrite without an explicit flag.
  *Depends on:* `Dabbler.SchemaApplicator` by project reference; packed as one
  tool. **This is the package the Access-migration consumer is blocked on.**
- **`Dabbler.Security.DevIdp.Tool`** — the `dabbler-devidp` dotnet tool.
  *Depends on:* `Dabbler.Security.DevIdp`.
- **`ComponentCatalogCrawler`** (`tools/`) — a non-packable dev helper that
  crawls the component catalog; it exists for maintainers only.

### Reference application

- **`AcmeCoffeeOnline.Web`**, **`AcmeCoffeeOnline.Api`** and
  **`AcmeCoffeeOnline.Shared`** (`src/Apps/`) — the reference application the
  libraries are proven against, built as the production split demands.
  *Contract:* in — the compose stack up; out — a running web tier at
  `http://localhost:5010` that the Playwright suite drives; fails — visibly, in
  the E2E run. Not packed.
  *Known gap:* `AcmeCoffeeOnline.Shared` is reached by `<ProjectReference>`
  from both Web and Api but is **not listed in `Dabbler.Platform.sln`**. It
  builds transitively, so nothing is broken, but a solution-level operation
  misses it. Adding it is a one-line build-file change this session may not
  make; Session 2 should give it a home.

### Cuts deferred

The library cuts above are settled and in the tree. What is still open is
whether two of them should survive — `Dabbler.Components.Compat` once no
consumer references the retired surface, and `Dabbler.Platform.Stubs` as four
packages rather than one — and whether the production-split rule gets an
architecture test. Session 2 decides each, along with those below.

---

## Deferred cuts

The six decisions the brief leaves open, with their options and the operator's
stated leaning. This plan records them; **Session 2 challenges and decides
each**, and nothing here is settled by writing it down.

1. **`uat_runner/`.** The v1 Python package that turned a human UAT checklist
   into generated Playwright tests and triaged failures through the router. It
   cannot run (no router; hard-coded `../dabbler-uat-dsl` CLI path). Its six
   generated test files under `tests/AcmeCoffeeOnline.Playwright/Generated/`
   are committed and still build. *Options:* port the
   checklist-to-Playwright emission to a dotnet tool in `dabbler-uat-dsl`,
   where the DSL already lives, and drop triage; or retire it here and keep
   the generated tests as ordinary Playwright tests. *Operator leans:* retire
   from this repository, and let `dabbler-uat-dsl` own any port.
2. **Playwright and Packaging.Smoke as manual gates.** Neither is in
   `dabbler.yaml`: Playwright needs the Docker stack and has no skip guard;
   Packaging.Smoke needs packed nupkgs and SQL LocalDB. *Options:* add an
   environment-gated skip so they can join a suite, or keep them manual and
   say so in every session that touches the reference app. *No stated
   leaning* — Session 2 decides, and whichever way it goes the answer must be
   written where a session author will meet it.
3. **Publish `Dabbler.CrudSlice.Tool`.** Bump `DabblerVersion` to
   `0.1.0-preview.2`, tag `nuget-v0.1.0-preview.2`, let CI publish, then pin
   that version in the migration solution's playbook. *Operator leans:* do it,
   and make it the first numbered session — it unblocks the Access-migration
   walking skeleton.
4. **Stale docs.** `docs/planning/access-to-blazor-cli-spec.md` describes an
   extractor/generator design that was never built and names projects that do
   not exist. `project-guidance.md` and `lessons-learned.md` carry banners
   pointing readers past their v1 sections; `project-status.md` is marked
   stale as of 2026-04-21 and describes the retired router. *Operator leans:*
   archive the CLI spec; prune the v1 sections of the other three in a later
   session.
5. **Branches.** `pre-role-rearchitecture` and
   `archive/pre-role-cleanup-master` are ancestors of `master`, kept as a
   safety net until the role re-architecture finished. It did. *Options:*
   delete them, or record why they stay.
6. **The `dabbler-uat-dsl` feed.** `tests/AcmeCoffeeOnline.Playwright` restores
   `Dabbler.Uat.Dsl.*` from `../dabbler-uat-dsl/pack-output` through a
   `dabbler-uat-dsl-local` source in its own `nuget.config`. That works on one
   machine and fails on any other; CI builds the solution filter to avoid it,
   and `dabbler deps check` reports `feed-not-configured` because it reads the
   root and machine-level configuration, not the project's.
   *Options:* publish `dabbler-uat-dsl` to nuget.org like every other Dabbler
   package and drop the local source — which also lets the source move to the
   root `nuget.config` where `dabbler deps check` can see it — or keep the
   sibling feed and accept the report. *Operator leans:* publish. **Do not**
   move the relative source to the root `nuget.config` without publishing: CI
   restore breaks with NU1301 on a missing local source.
7. *(Found while writing this plan, not in the brief.)*
   **`AcmeCoffeeOnline.Shared` is missing from `Dabbler.Platform.sln`.** It is
   reached only by project reference. Add it, or say why the solution leaves
   it out.
8. *(Added 2026-11-07 — sustainment.)* **`Dabbler.Reports.Core` depends on
   `Dabbler.Components`** solely for `Dabbler.Components.Expressions`.
   *Options:* move the nine files into `Dabbler.Reports.Core` under
   `Dabbler.Reports.Core.Expressions` and drop the reference (a namespace
   change; neither `Dabbler.NorthwindToBlazor` nor the migration solution
   references the old namespace directly), or cut a new tier-neutral
   `Dabbler.Expressions` package. *Operator leans:* move; no new package.
   `Dabbler.Platform.Stubs.Reports` keeps its `Dabbler.Components` reference
   only if something other than Expressions needs it.
9. *(Added 2026-11-07 — sustainment.)* **MudBlazor 9.** Bump the central pin
   to `9.12.0` and fix what breaks, libraries first (`Dabbler.Components`,
   `.Compat`, `Dabbler.Reports.Blazor`, `Dabbler.Security.Blazor`), then the
   reference app, the `dotnet new` templates and the CrudSlice scaffolding
   templates. The `IGridDataProvider` contract gains a `CancellationToken`,
   which is a public API change. *Operator leans:* do it now, and write the
   policy as "adopt a new MudBlazor major within one quarter of its release",
   enforced by a `.github/dependabot.yml` that watches MudBlazor and the
   `Microsoft.*` 10.x line.
10. *(Added 2026-11-07 — sustainment.)* **Surface reduction.** Seventeen
    package IDs are published; the operator will stand behind twelve.
    `Dabbler.Components.Compat` is already marked deprecated and no consumer
    references it (`Dabbler.NorthwindToBlazor` references eight runtime
    packages, none of them Compat; the migration solution consumes
    `Dabbler.Templates` and `Dabbler.CrudSlice.Tool`). The three
    `Dabbler.Platform.Stubs.*` providers fold into the one
    `Dabbler.Platform.Stubs` package, which is the only Stubs ID a consumer
    references today. `Dabbler.Templates` stays: it is the migration
    consumer's entry point. *Operator leans:* delete Compat, fold the Stubs,
    unlist the four retired IDs on nuget.org after the next release (unlist,
    never delete).
11. *(Added 2026-11-07 — sustainment.)* **Versioning.** `0.1.0-preview.2`
    now, to ship `Dabbler.CrudSlice.Tool` (cut 3). `0.2.0` once cuts 8, 9 and
    10 have landed — those are the breaking changes, and the preview label
    retires with them. `1.0.0` once Phase 3 (honest gates) and the maintainer
    guide are in. *Operator leans:* exactly that sequence; no further preview
    releases.
12. *(Added 2026-11-07 — sustainment.)* **The framework cannot yet make a
    `nuget-v*` tag.** `release: tag` is hard-wired to `vsix-v<version>`
    (`packages/router/src/packaging.ts`, `tagsFor`) and a `pack`-only
    declaration takes its version from `version.json`, which this repository
    does not keep. *Options:* the operator hand-tags `nuget-v<version>` after
    an ordinary version-bump session (works today; the framework records no
    `published` row); or a session in `dabbler-ai-orchestration` lets
    `release: tag` name its tag (for example `tag: "nuget-v{version}"`) so a
    release session here publishes through the framework; or `build.yml`
    accepts a second tag pattern. *Operator leans:* hand-tag `0.1.0-preview.2`
    now; land the framework change before `0.2.0`; never widen `build.yml`.
13. *(Added 2026-11-07 — sustainment.)* **Legacy `docs/session-sets/`.**
    Fifty-plus v1 session-set directories remain beside the v3 record.
    *Options:* fold them with `dabbler session migrate --from <dir>` one by
    one, move them under `docs/session-sets/_archived/`, or leave them and add
    a one-paragraph README that says they are history. *Operator leans:*
    leave them, add the README; migration buys nothing a reader needs.

---

## Phases and feature areas

Ordered so earlier work unblocks later work. Each phase is a handful of
focused AI sessions; Session 2 turns them into the numbered list.

### Phase 1 — Unblock the consumer (first, and small)

The Access-migration walking skeleton is waiting on one package.

- **Packaging session.** Add the `packaging:` block to `dabbler.yaml` —
  `pack` through `tools/pack.ps1` into `{output}`, no `push` — and nothing
  else.
- **Release `0.1.0-preview.2`.** Bump `DabblerVersion`, tag
  `nuget-v0.1.0-preview.2`, let CI publish through trusted publishing. The
  deliverable is `Dabbler.CrudSlice.Tool` on nuget.org; every other package
  goes along at the same version.

*Done when:* `dotnet tool install --global Dabbler.CrudSlice.Tool --version
0.1.0-preview.2` succeeds from a clean machine.

### Phase 2 — Sustainment (added 2026-11-07)

The operator retires in roughly four years and the staff who inherit these
libraries must be able to keep them alive, or copy them in and own them. This
phase is what makes either outcome possible. Cuts 8 through 11 decide it; the
packaging and `0.1.0-preview.2` sessions of Phase 1 come first because the
migration consumer is waiting and the work is small.

- **Move Expressions into `Dabbler.Reports.Core`** and drop the
  `Dabbler.Components` reference from the report engine (cut 8).
- **MudBlazor 9, libraries.** Bump the central pin, fix the four UI
  libraries, run `Dabbler.Unit.slnf` (cut 9, first half).
- **MudBlazor 9, reference app and templates.** Fix `AcmeCoffeeOnline.Web`,
  `templates/Dabbler.Templates/content` and `tools/Dabbler.CrudSlice/Templates`;
  `Dabbler.Templates.Tests` and `Dabbler.CrudSlice.Tests` prove the generated
  apps still build; the Playwright run is a recorded manual gate until Phase 3
  settles cut 2 (cut 9, second half).
- **Surface reduction.** Delete `Dabbler.Components.Compat`; fold the three
  Stubs providers into `Dabbler.Platform.Stubs`; update `tools/pack.ps1`'s
  allowlist, `Dabbler.Publish.slnf`, the publish order in
  `docs/packaging/release-checklist.md`, `Dabbler.PublicSurface.Tests` and
  `Dabbler.Packaging.Smoke` to the twelve IDs (cut 10).
- **Support policy and maintainer guide.** `docs/MAINTAINING.md` (clone, build,
  test, pack, release by tag, the MudBlazor policy, where every gate lives),
  `.github/dependabot.yml`, and an "If this library stops being maintained"
  section in the root README and every package README that names the
  fallback: copy the MIT source into your application and own it.
- **Release `0.2.0`.** The preview label retires; the change-log names the
  three breaking changes. Hand-tagged or framework-tagged per cut 12.

*Done when:* `Directory.Packages.props` names MudBlazor 9.x, `0.2.0` of twelve
package IDs is on nuget.org, the four retired IDs are unlisted, `dotnet test
Dabbler.Unit.slnf` is green, and a reader can release the next patch from
`docs/MAINTAINING.md` alone.

### Phase 3 — Make the repository honest about its gates

Decisions 2 and 6, which together decide whether a clean checkout on a second
machine can run everything this repository claims to run.

- **Decide and implement the Playwright / Packaging.Smoke gate.** Either an
  environment-gated skip that lets them join a `dabbler.yaml` suite, or a
  written manual-gate rule in `AGENTS.md` and the session-authoring addendum.
- **Decide and implement the `dabbler-uat-dsl` feed.** Publishing it is the
  operator's preference and is work in *that* repository; this repository's
  share is dropping the local source and moving to the published package, and
  getting `dabbler deps check` clean.
- **Add the production-split architecture test**, if Session 2 keeps it: no
  web-tier assembly references EF Core outside the platform-store exception.

*Done when:* a fresh clone with Docker available runs every suite this
repository names, and `dabbler deps check` is clean or its remaining report is
explained in `solution-dependencies.json`.

### Phase 4 — Retire what does not run

Decisions 1, 4 and 5 — removing dead weight so the next reader is not misled.

- **`uat_runner/`.** Retire it here, keeping the generated Playwright tests as
  ordinary tests, once `dabbler-uat-dsl` has or declines the port.
- **Planning docs.** Archive `access-to-blazor-cli-spec.md`; prune the v1
  sections from `project-guidance.md`, `lessons-learned.md` and
  `project-status.md` so the surviving rules (testing, UAT, MudBlazor, CRUD,
  code style) stand without a banner.
- **Branches.** Delete the two safety-net branches or record why they stay.
- **Housekeeping:** add `AcmeCoffeeOnline.Shared` to the solution file.

*Done when:* nothing in the repository describes a workflow that cannot be
run, and no document needs a banner to be read correctly.

### Phase 5 — Release `1.0.0`

One release session, after Phases 2 through 4. Nothing else changes: `1.0.0`
is the statement that the surface, the gates and the maintainer guide are
what the operator will stand behind, and that staff can pin it.

### Phase 6 — Platform work the consumer asks for

Open-ended, and deliberately unplanned beyond its first session: the
Access-migration solution is the customer, and its needs — CRUD slice
coverage, template parameters, querying operators, reporting gaps — arrive as
the migration proceeds. Each arrival is one session: the smallest library or
template change that unblocks it, its tests named after what it changes, and a
patch release when a consumer needs the package.

`docs/planning/phase-3-platform-roadmap.md` holds the migration workflow this
phase serves; the workflow itself belongs to `dabbler-access-migration`, not
here.

*Done when:* nothing, by design. This phase ends when the migration work does.

### Releases

One release per phase that changes a published package, patch-level while the
platform is at `0.1.0-preview.*`. Phase 1 proposes `0.1.0-preview.2`; Session 2
proposes the rest with the numbered sessions, for the operator to approve.
