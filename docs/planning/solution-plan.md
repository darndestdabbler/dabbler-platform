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
- **One documented exception.** Platform-internal stores — security
  configuration and the audit log — need host-startup persistence and may use
  `options.UseEfCoreStore()` (or the JSON file store) in whichever tier hosts
  them. `src/Libs/Dabbler.Security/Migrations` is that store's migrations. The
  REST-only principle does not apply to them, and this is the only exception.
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

---

## Modules

Every module below is a `.csproj` listed in `Dabbler.Platform.sln` at the
repository root, and each reaches a sibling by `<ProjectReference>` — never by
a `PackageReference` to its own published package. That is what makes the
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
  not by rendering a blank page. *Depends on:* `Dabbler.Components`.
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
architecture test. Session 2 decides each, along with the six below.

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

### Phase 2 — Make the repository honest about its gates

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

### Phase 3 — Retire what does not run

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

### Phase 4 — Platform work the consumer asks for

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
