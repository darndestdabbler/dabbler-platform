# Packaging Approach Proposal

> **Session:** packaging-and-template-readiness / Session 5 of 11
> **Date:** 2026-05-15
> **Author:** Claude Opus 4.7 (orchestrator, direct authoring)
> **Status:** Draft for tri-provider review (Opus self-contribution + GPT-5.4 + Gemini Pro).
> **Inputs:** Session 4 [`platform-value-review.md`](./platform-value-review.md)
> gap dispositions G3, G4, G8, G9, G10, G12, G13, G14, G21, G22, G23 carry
> into the packaging decisions below; the value-review's "no critical
> value gap" verdict is the green light for this proposal to exist.
> **Doubles as:** Opus's `packaging-review-opus.md` review input per the
> `feedback_orchestrator_self_contributes_to_review_panel` discipline. The
> orchestrator's perspective is embedded in §11 (rationale notes) rather
> than written as a separate file.
> **Post-review revisions:** sections §3.3, §4.4, §6.2, §8, §10.1, §10.2,
> §11.2, and §11.7 carry inline edits applied after the tri-provider
> review. Each edit either opens with a `> Updated post-review (Ax)…`
> block or uses inline phrasing that cites the originating adjustment
> from [`packaging-approach-review.md`](./packaging-approach-review.md)
> §4. Substantive proposal shape (package matrix, opt-in stubs contract,
> two-template model, lock-step versioning, consumer-install validation
> strategy) is unchanged.

---

## 0. Reading guide

A reviewer with no prior context should read:

- §1 (executive summary) — what changes, what doesn't, what defers.
- §2 (library packaging matrix) — which libraries pack, which don't.
- §3 (NuGet metadata + versioning policy).
- §4 (Stubs as opt-in).
- §5 (VS template surface).
- §6 (consumer-install validation strategy).
- §7 (how the value-review carry-overs land).
- §8 (open questions for Session 6).
- §9 (out of scope).

The proposal is self-contained. Where it relies on a fact from the value
review, the relevant clause is quoted inline; reviewers do **not** need
to fetch `platform-value-review.md` to evaluate this proposal.

---

## 1. Executive summary

**What the platform looks like after this proposal lands:**

- Eight libraries package as NuGet packages with consistent metadata,
  a single repository-wide version, MIT license (with `DabblerPdf` license
  disclosure in the two Reports.* packages that transit it), MIT-licensed
  symbol packages (snupkg) + Source Link.
- One target framework: `net10.0` (current LTS). Multi-targeting is
  rejected at this stage; revisit only if external demand surfaces.
- `Dabbler.Platform.Stubs` ships as a single opt-in package; the
  Components/Reports stub split discussed in §4.4 is deferred to a
  post-1.0 follow-on session set.
- One VS template package (`Dabbler.Templates`) ships two templates:
  `dabbler-app` (full Security + one query page + one report + Stubs-wired
  bUnit) and `dabbler-min` (Security + Components only). Both templates
  reference the published NuGet packages, not project references.
- Pre-1.0 versioning: `0.x.y-preview.N` while the contract continues to
  harden; `1.0.0` when Session 9's consumer validation passes
  end-to-end with no `fix-during-packaging` items outstanding.
- Consumer-install validation is a build step: pack into a local feed,
  scaffold from template, build the scaffolded app, smoke a Playwright
  test that the home page renders. This gates publication to nuget.org.

**What does not change:**

- The narrowed contract from the rebuild stays narrowed. No metadata-
  driven UI generation, no role inheritance, no
  `MasterDetailsConfig<TEntity>` config DSL. The packaging story does
  not silently re-open scope that the rebuild closed.
- The integrated-source adoption path under `src/Libs/` remains
  supported for the dabbler-platform repo itself. NuGet is the
  *external* adoption path; the inner-loop dev experience stays on
  ProjectReferences via the existing `PackAsDependency` MSBuild
  condition.
- All eight library boundaries stay where they are. No package merges,
  no package splits beyond the optional Stubs split deferred in §4.4.

**What defers to later sessions:**

- The Components compatibility-surface decision (G4) — pick in Session 6
  surface freeze, not here. This proposal lists the three options but
  does not lock one in; locking it in now would pre-empt the surface
  freeze.
- The first-app tutorial (G11) — Session 8 deliverable.
- The Components README's stale "Access migration" description — fix
  inline during Session 6 with the rest of the overview tightening.
- The Stubs sub-package split — post-1.0 if it earns its place via
  consumer complaint; the meta-package + focused helpers approach in
  §4.3 covers the immediate need.

---

## 2. Library packaging matrix

Eight library projects under `src/Libs/` plus one development helper:

| # | Project | Packs? | Package ID | Tier |
|---|---------|--------|------------|------|
| 1 | `Dabbler.Components` | Yes | `Dabbler.Components` | Foundation |
| 2 | `Dabbler.Platform.Stubs` | Yes | `Dabbler.Platform.Stubs` | Opt-in stubs |
| 3 | `Dabbler.Reports.Core` | Yes | `Dabbler.Reports.Core` | Reports |
| 4 | `Dabbler.Reports.Export` | Yes | `Dabbler.Reports.Export` | Reports |
| 5 | `Dabbler.Reports.Blazor` | Yes | `Dabbler.Reports.Blazor` | Reports |
| 6 | `Dabbler.Security` | Yes | `Dabbler.Security` | Security |
| 7 | `Dabbler.Security.Blazor` | Yes | `Dabbler.Security.Blazor` | Security |
| 8 | `Dabbler.Testing.Blazor` | Yes | `Dabbler.Testing.Blazor` | Testing |
| — | `Dabbler.Security.DevIdp` | **No** | (n/a — it is an ASP.NET Core *app*, not a library) | Dev helper |

Plus the template package:

| Project | Packs? | Package ID | Tier |
|---------|--------|------------|------|
| `templates/Dabbler.Templates/` (new in Session 8) | Yes | `Dabbler.Templates` | Template |

### 2.1 Why these eight, and not more or fewer?

Each library earns its own package by satisfying one criterion: an
external consumer can plausibly want this library without wanting some
other one in the list. Specifically:

- A team that wants the QBE filter model + CRUD helpers but no
  reporting takes `Dabbler.Components` alone.
- A team that wants reporting but no authorization takes
  `Dabbler.Reports.{Core,Export,Blazor}` and skips `Dabbler.Security`.
- A team that has its own authorization model but wants the
  Playwright-watermark testing utilities takes
  `Dabbler.Testing.Blazor` only.
- A team that wants stub mode for bUnit tests takes
  `Dabbler.Platform.Stubs`.

Merging Security + Security.Blazor into one package would force
non-Blazor consumers (an API-only consumer needing only the
restriction model) to pull in Blazor render dependencies they don't
need. Splitting `Dabbler.Reports.Core` apart from `.Export` lets a
consumer compose reports for an HTML viewer without taking the
DabblerPdf transitive dependency (and therefore the LGPL/MPL license
question that comes with it).

### 2.2 Why `Dabbler.Security.DevIdp` does NOT pack

It is an ASP.NET Core *web project* (a runnable dev-time OIDC IdP),
not a class library. There is no consuming surface — adopters do not
take a dependency on it; they run it loopback-only for local testing
of the external-login flow. Packaging it as NuGet would invite the
wrong adoption pattern. Adopters who want a local OIDC IdP for
development should either reference the source under `src/Libs/` or
run an OSS IdP (Duende, OpenIddict samples, etc.) directly.

### 2.3 Dependency closure

```
Components (no deps on other Dabbler libraries)
├── Security ← FrameworkReference Microsoft.AspNetCore.App + EF Core + OIDC
│   └── Security.Blazor ← Components + Security
├── Reports.Core ← Components (browser primitives are independent) + DabblerPdf
│   ├── Reports.Export ← Reports.Core + DabblerPdf
│   └── Reports.Blazor ← Components + Reports.Core + Reports.Export
├── Platform.Stubs ← Components + Reports.Core
└── Testing.Blazor (no deps on other Dabbler libraries)
```

Two observations:

- **Security and Reports are independent.** A consumer can take Reports
  without Security and vice versa.
- **Platform.Stubs currently pulls Reports.Core.** This is a real
  coupling: a consumer wiring stubs for bUnit Components tests also
  pulls in the Reports stack. §4.4 discusses the future split; this
  proposal does NOT split Stubs in this session.

---

## 3. NuGet metadata and versioning policy

### 3.1 Common metadata (set in `Directory.Build.props` once)

The current `Directory.Build.props` carries only build-time defaults
(TargetFramework, Nullable, ImplicitUsings, Deterministic, LangVersion).
This proposal extends it with package-metadata defaults shared by every
packable library:

```xml
<!-- Directory.Build.props additions -->
<PropertyGroup>
  <!-- Package identity -->
  <Authors>Dabbler Platform contributors</Authors>
  <Company>Dabbler Platform</Company>
  <Copyright>Copyright (c) Dabbler Platform contributors</Copyright>

  <!-- Repository link -->
  <RepositoryUrl>https://github.com/darndestdabbler/dabbler-platform</RepositoryUrl>
  <RepositoryType>git</RepositoryType>
  <PackageProjectUrl>https://github.com/darndestdabbler/dabbler-platform</PackageProjectUrl>

  <!-- License (libraries themselves are MIT) -->
  <PackageLicenseExpression>MIT</PackageLicenseExpression>
  <PackageRequireLicenseAcceptance>false</PackageRequireLicenseAcceptance>

  <!-- Reproducible build + symbols -->
  <PublishRepositoryUrl>true</PublishRepositoryUrl>
  <EmbedUntrackedSources>true</EmbedUntrackedSources>
  <IncludeSymbols>true</IncludeSymbols>
  <SymbolPackageFormat>snupkg</SymbolPackageFormat>
  <ContinuousIntegrationBuild Condition="'$(CI)' == 'true'">true</ContinuousIntegrationBuild>

  <!-- Single repository version (overridden via /p:DabblerVersion=…) -->
  <DabblerVersion Condition="'$(DabblerVersion)' == ''">0.1.0-preview.1</DabblerVersion>
  <Version>$(DabblerVersion)</Version>
  <PackageVersion>$(DabblerVersion)</PackageVersion>
  <AssemblyVersion>$(DabblerVersion.Split('-')[0])</AssemblyVersion>
  <FileVersion>$(DabblerVersion.Split('-')[0])</FileVersion>
</PropertyGroup>

<!-- Source Link for GitHub -->
<ItemGroup>
  <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0"
                    PrivateAssets="All" Condition="'$(IsPackable)' == 'true'" />
</ItemGroup>
```

Per-library `.csproj` keeps only the per-package distinctive metadata:
`PackageId`, `Description`, `PackageTags`, `PackageReadmeFile`. Plus
the `PackAsDependency` conditional that swaps ProjectReference ↔
PackageReference (see §3.4).

### 3.2 Package icon — fix the broken reference

Every packable .csproj today says
`<None Include="..\..\icon.png" Pack="true" PackagePath="" />` but
**no `src/icon.png` file exists**. `dotnet pack` therefore currently
fails on any library with that reference. **This is a Session 6
fix-during-packaging item, but the cause is mechanical and worth
calling out here:** either commit an icon at `src/icon.png` and update
the path, or remove the `<PackageIcon>icon.png</PackageIcon>` +
`<None Include=…>` lines until a real icon is authored.

This proposal recommends: **commit a 128×128 PNG at `src/icon.png`,
update references to `..\..\icon.png` (correct existing relative
path), and let it ship.** A neutral platform icon is fine; the brand
question is below the priority bar. Asset authoring lands in Session 7
as part of the metadata tighten.

### 3.3 Versioning policy

> **Updated post-review (A5, A17):** the three-phase ladder collapsed
> to two phases per gpt-5-4's review (a non-preview pre-1.0 release
> would mislead adopters that the external path is proven). First
> public publish is gated on Session 7's metadata-tighten passing the
> §6.1 smoke; this is binding, not a recommendation.

| Phase | Version range | What can break |
|-------|---------------|----------------|
| Pre-1.0 | `0.x.y-preview.N` for **every** public release (no non-preview pre-1.0 versions) | Anything. Minor bumps may carry breaking changes; documented per pre-release. |
| 1.0 release | `1.0.0` after Session 9 consumer validation passes end-to-end with no outstanding `fix-during-packaging` items from the value review or this packaging review | SemVer 2.0 from here. Breaking changes require major bump; deprecations get one minor cycle's notice. |
| Post-1.0 | `1.x.y` / `2.x.y` | Standard SemVer. |

**Publish gate:** no `dotnet nuget push` until Session 7's metadata
work passes the §6.1 smoke. The first preview package may be
**internal** to a staging feed earlier, but the first public-feed
release is gated.

**All Dabbler packages release in lock-step** with a single
`$(DabblerVersion)` MSBuild property. This avoids the version-skew
trap where `Dabbler.Components 0.2.1` and
`Dabbler.Security.Blazor 0.2.0` carry incompatible types because the
latter was published before a Components change landed. NuGet's
resolver handles upgrade fan-out fine when all package versions move
together.

Release cadence: **no fixed cadence pre-1.0**; releases ship when a
material change lands and Session 6's surface freeze passes a
consumer-install validation run (§6).

### 3.4 The `PackAsDependency` MSBuild conditional

Three Reports.* libraries already use a `Condition='$(PackAsDependency)' == 'true'`
pattern to swap `ProjectReference` ↔ `PackageReference` depending on
whether the build is an inner-loop dev build or a `dotnet pack` for
external publication. This proposal **promotes that pattern to every
packable library** and consolidates it. Concretely:

- During regular `dotnet build` / `dotnet test`, the libraries reference
  each other via `<ProjectReference>` (current behavior).
- During `dotnet pack /p:PackAsDependency=true /p:DabblerVersion=0.1.0-preview.1`,
  the `<PackageReference>` arm activates and packages consume each
  other via NuGet at the matching version.

A `tools/pack.ps1` script (Session 7 deliverable) wraps this:

```powershell
# tools/pack.ps1
param(
  [Parameter(Mandatory)][string]$Version,
  [string]$OutDir = "pack-output"
)
$projects = @(
  "src/Libs/Dabbler.Components/Dabbler.Components.csproj",
  "src/Libs/Dabbler.Security/Dabbler.Security.csproj",
  "src/Libs/Dabbler.Security.Blazor/Dabbler.Security.Blazor.csproj",
  "src/Libs/Dabbler.Reports.Core/Dabbler.Reports.Core.csproj",
  "src/Libs/Dabbler.Reports.Export/Dabbler.Reports.Export.csproj",
  "src/Libs/Dabbler.Reports.Blazor/Dabbler.Reports.Blazor.csproj",
  "src/Libs/Dabbler.Platform.Stubs/Dabbler.Platform.Stubs.csproj",
  "src/Libs/Dabbler.Testing.Blazor/Dabbler.Testing.Blazor.csproj",
  "templates/Dabbler.Templates/Dabbler.Templates.csproj"
)
foreach ($project in $projects) {
  dotnet pack $project `
    /p:PackAsDependency=true `
    /p:DabblerVersion=$Version `
    --configuration Release `
    --output $OutDir
}
```

### 3.5 Security and Security.Blazor lack package metadata today — fix in Session 6

Today `src/Libs/Dabbler.Security/Dabbler.Security.csproj` and
`src/Libs/Dabbler.Security.Blazor/Dabbler.Security.Blazor.csproj` carry
**no** package metadata at all (no `PackageId`, no `Description`, no
`PackageTags`, no `PackageReadmeFile`, no `PackAsDependency`
conditional). The other six libraries do. This proposal lands the
missing metadata in Session 6's surface freeze as part of the
"every-library-pack-clean" checklist.

This is a non-trivial omission and worth verifier review — surfacing
it here so the routed reviewers can confirm the gap before the surface
freeze locks it in.

### 3.6 Description tightening (G3, G4 carry-overs)

Two existing `<Description>` lines are stale relative to the rebuild:

- `Dabbler.Components.csproj` says *"Blazor UI component library for
  data-driven applications: DataGrid with QBE filtering, FilterGrid,
  ComboBox, Navigation Shell, Expression Engine, composite pages, and
  metadata-driven configuration. Migrated from Microsoft Access."*
  This still references *"FilterGrid, ComboBox, Navigation Shell,
  composite pages, metadata-driven configuration"* — none of which
  match today's narrowed contract. The "Migrated from Microsoft Access"
  framing is also wrong for the value proposition.
- `Dabbler.Platform.Stubs.csproj` description is fine
  ("Reusable DI-friendly stub providers for Dabbler components,
  reports, and stub-backed hosts").
- The Reports.* and Testing.Blazor descriptions are fine.

Replacement for Components (Session 6 surface freeze):

> *"MudBlazor-native CRUD primitives for Blazor Server: declarative
> QBE filter panel, dirty-tracking edit session, master/detail
> expansion shell, and the underlying expression-engine + data-provider
> abstractions. Composition-over-configuration: pages declare their own
> MudDataGrid columns and MudForm fields."*

### 3.7 What is NOT exported as public API

Two things are NOT going to be public surface in the packaged libraries
even though they currently compile cleanly:

- **Compatibility surfaces** (`DabblerShell`, `DabblerNavigationPanel`,
  `DabblerTabbedWorkspace`, `DabblerTopMenuBar`, `DabblerCatalogPage`)
  — see G4. Three options for Session 6: (a) move into a
  `Dabbler.Components.Compat` package, (b) mark `[Obsolete]` and ship
  them in the main package with a sunset version, (c) keep in the main
  package with stronger README labeling. This proposal recommends
  **option (b)** — `[Obsolete("Compatibility surface from the
  pre-MudBlazor-native era. Use MudDataGrid + MudLayout + MudDrawer
  + MudNavMenu directly. Slated for removal in 0.4.0.", false)]` — on
  the grounds that a separate compat package adds a maintenance burden
  without proportional value (a consumer who needs the compat
  surface is on a deprecated path either way), and stronger README
  labeling alone has been tried in Session 2 without closing the gap.
- **Internal seams** marked with `internal` today stay `internal`. The
  existing `InternalsVisibleTo` declarations to `*.Tests` projects
  remain unchanged.

### 3.8 SourceLink + reproducible builds

Source Link landing in every packable library means consumers can
F12-step into Dabbler code from their debugger and land on the exact
commit on GitHub. This is high-value for an enterprise adoption story
because library-internal bug reports become reproducible against
versioned source. The `Microsoft.SourceLink.GitHub` package (already
referenced from `Directory.Build.props` per §3.1) plus
`<EmbedUntrackedSources>` and `<ContinuousIntegrationBuild>` cover it.

Build determinism is already set at `<Deterministic>true</Deterministic>`
in `Directory.Build.props`. Together with SourceLink + snupkg symbols,
the published packages support full debugger source-stepping against
the published commit.

---

## 4. Stubs as opt-in (not a default)

### 4.1 The contract: stub mode never silently activates

`Dabbler.Platform.Stubs` is registered only when the consumer's
`Program.cs` explicitly calls it. There is no auto-discovery, no
"if-stubs-package-is-referenced-then-register" magic, no environment-
variable shortcut. The package is exactly what its name says: a set of
DI-registrable stub providers a consumer opts into.

Critically: **the production application registers real providers; the
stub providers are absent.** A test or demo host opts in via
`AddDabblerPlatformStubs(...)` and the focused per-provider helpers it
needs.

### 4.2 The wiring pattern (side-by-side `Program.cs` snippet)

The G5 carry-over from the value review ("stub-mode swap depth not
shown") lands during Session 6's overview update; the proposal-level
contract is that swapping is a **registration-time** decision, not a
runtime branch. The same component code runs against both real and
stub providers without conditional branches in the component itself:

```csharp
// Production app (real providers)
builder.Services
    .AddDabblerSecurity(options => { /* real Security */ })
    .AddDabblerSecurityBlazor()
    .AddDabblerReports();
builder.Services.AddScoped<IGridDataProvider<Customer>, CustomerGridDataProvider>();

// Test or demo host (stub providers)
builder.Services
    .AddDabblerPlatformStubs(definition)
    .AddStubGridDataProvider<Customer>(new[] { /* in-memory rows */ });
builder.Services.AddStubAuthentication(new StubUserContext { … });
```

A bUnit test that wires the same set of stubs via a `TestContext`
extension method (`ctx.AddDabblerStubs(definition);`) sees the same
component code render against stub providers.

### 4.3 Focused helpers stay focused

The current API (`AddDabblerPlatformStubs(definition)` plus per-provider
helpers `AddStubGridDataProvider`, `AddStubComboBoxItemProvider`,
`AddStubAuthentication`, `AddStubReportDataProvider`,
`AddStubEntityMetadataProvider`, `AddStubNavigationDataProvider`,
`AddStubArtifactCatalogProvider`, `AddStubUsageTrackingProvider`) is
already well-shaped. The proposal preserves it. A consumer who wants
only stubbed grid data uses `AddStubGridDataProvider` alone; a consumer
who wants a stub-mode host uses the bulk helper.

### 4.4 The Components / Reports stub split

> **Updated post-review (A3, Gemini Pro Critical):** this section's
> recommendation (defer the split) was **reversed**. The split lands
> in Session 7 (per `packaging-approach-review.md` §7.2). The
> rationale: a consumer who pulls `Dabbler.Platform.Stubs` for the
> Components stubs alone gets DabblerPdf transitively (Stubs → Reports.Core
> → DabblerPdf), which carries an LGPL-2.1-only OR MPL-2.0 license
> implication the consumer did not opt into. License transit is an
> asymmetric concern the original Opus §4.4 disposition underweighted.
> The original "defer until consumer complains" text below is
> preserved for context.

`Dabbler.Platform.Stubs` currently has a hard dependency on
`Dabbler.Reports.Core` because it ships `StubReportDataProvider`. A
consumer that wants only `StubGridDataProvider` for bUnit Components
tests transitively pulls in the Reports stack, which they may not need.

Two ways to handle this:

- **Option A (this proposal, recommended).** Keep one package
  `Dabbler.Platform.Stubs`. Document the Reports.Core transitive
  dependency explicitly in the README. A consumer who really doesn't
  want the Reports stack on disk and is willing to write a one-class
  stub themselves can avoid the package entirely. The cost of an
  unused Reports.Core in the consumer's bin output is on the order of
  a few hundred KB and zero runtime overhead.
- **Option B (deferred follow-on).** Split into three packages:
  `Dabbler.Platform.Stubs.Components`, `.Reports`, `.Security`. The
  meta-package `Dabbler.Platform.Stubs` PackageReferences all three.
  This addresses the transitive-dependency concern but adds three
  PackageId entries to maintain.

Option A ships now; option B is a post-1.0 follow-on if and only if a
consumer surfaces the complaint. The cost of the meta-package's
transitive Reports.Core dependency is low enough that the split's
maintenance overhead is not currently justified.

### 4.5 Stubs is NOT a production fallback

A common anti-pattern in stub libraries is letting them double as a
production fallback ("if no real provider is registered, use the
stub"). `Dabbler.Platform.Stubs` deliberately does not. Production
applications register real providers explicitly; if a real provider is
missing, DI throws and the app fails to start. This is the right
failure mode for a platform that wants its production posture to be
explicit.

---

## 5. VS template surface

### 5.1 What ships in the template package

One NuGet package, `Dabbler.Templates`, installs two `dotnet new`
templates:

| Short name | What it scaffolds | Reports? | Security? | Stubs wired? |
|------------|-------------------|----------|-----------|--------------|
| `dabbler-app` | Full Blazor Server app | Yes (one sample report) | Yes (cookie + JSON store, no MFA, no OIDC by default) | Yes (bUnit test project pre-wired) |
| `dabbler-min` | Minimal Blazor Server app | No | Yes (Security only) | No (test project authors stubs themselves) |

A future template `dabbler-api` (API-only consumer) is **not** in this
proposal — defer until a consumer asks for it. The two templates above
cover the validated Phase-2 contract.

Install pattern:

```bash
dotnet new install Dabbler.Templates
dotnet new dabbler-app -o MyApp
cd MyApp
dotnet build
```

The scaffold targets `net10.0`, references published NuGet versions of
the platform libraries (not project references — the template is for
external consumers), wires Security with the JSON file store
(simpler than EF Core for a first-run experience), and includes one
secured query page using `DeclarativeFilterPanel<TEntity>` +
`MasterDetailExpansionShell`, plus one banded report.

### 5.2 What the template does NOT scaffold

- **No domain-specific anything.** The query page is named
  `CustomersPage.razor` with a `Customer` entity stub; an adopter
  renames or replaces it. The template does not encode AcmeCoffeeOnline
  domain choices.
- **No CLI consumer specialization.** A future Access metadata-harvesting
  CLI is one consumer of the platform but **not the design center** for
  the template. The scaffold's defaults are generic enough to serve
  multiple downstream consumers; CLI-driven scaffolding lands its own
  customization on top.
- **No production-grade configuration.** Email sender defaults to
  console output (not SMTP, not MailHog). MFA is off. OIDC is not
  pre-wired. The adopter must wire production settings explicitly —
  this is the right default for a "starter" template.
- **No reusable theme.** `App.razor` uses MudBlazor defaults. Theme
  customization is an adopter decision, not a template default.

### 5.3 Template authoring discipline

The `template.json` (under `templates/Dabbler.Templates/content/<name>/.template.config/`)
follows the `Microsoft.TemplateEngine.Authoring.Tasks` conventions:

- `identity` matches `Dabbler.Templates.<short-name>` so the
  template engine's identity registration doesn't collide.
- `shortName` is the `dotnet new <name>` form.
- `symbols.Framework` is fixed to `net10.0` for now (single value),
  with the symbol shape ready to take additional values later.
- `sources` excludes the source-controlled `.template.config/` folder
  from the scaffolded output.
- Files use `<Application>` / `<Application>Web` as the root namespace
  placeholder via `sourceName`. The adopter's `MyApp` choice in
  `dotnet new dabbler-app -o MyApp` rewrites every namespace.

### 5.4 Template stays compatible with future CLI consumers

A future Access metadata-harvesting CLI (or similar code-gen consumer)
should be able to scaffold a Dabbler app **and then mutate it**: add
generated entities, generated CRUD pages, generated security
configurations. For the template not to fight the CLI:

- Generated CRUD pages use `DeclarativeFilterPanel<TEntity>` +
  `MasterDetailExpansionShell` directly — the same primitives a
  hand-authored page uses. The CLI does not need a "different" page
  abstraction.
- `security-config.json` lives at the app root (not embedded in code)
  so the CLI can edit it without source-modifying.
- The `Program.cs` blocks where Security and other platform services
  register are clearly demarcated by region comments so the CLI can
  insert/remove lines without re-formatting the file.

These rules apply to the template's hand-authored content; they do
not constrain the CLI's own design.

---

## 6. Consumer-install validation strategy

### 6.1 The smoke proof

A new test project, `tests/Dabbler.Packaging.Smoke/`, runs the
following sequence as a CI gate:

1. Pack every Dabbler.* library + the template package into a local
   feed at `pack-output/`.
2. `dotnet new install ./pack-output/Dabbler.Templates.<version>.nupkg`.
3. In a temp directory, run `dotnet new dabbler-app -o SmokeApp` with
   `nuget.config` pointing at the local feed.
4. `dotnet restore`, `dotnet build`. **Must succeed.**
5. Start the scaffolded app (`dotnet run`) on a free port; wait for
   the readiness endpoint.
6. A Playwright smoke spins up against `http://localhost:<port>/`,
   asserts the home page renders and the `/login` page is reachable.
7. Tear down.
8. Repeat with `dabbler-min` (no Reports, no Stubs wiring — just
   build + render-home-page).

Both templates passing this smoke is the gate for `dotnet nuget push`
to nuget.org. Failure is a Session 7/Session 8 closeout defect.

### 6.2 The packaged-contract check

Separately from the smoke, a `tests/Dabbler.PublicSurface.Tests/`
project asserts that the public API exported by each packed library
matches a checked-in `*.PublicSurface.txt` baseline (generated from
`Microsoft.DotNet.PublicApiAnalyzer`-style emit). This catches:

- **Accidental public-API additions** — a forgotten `public` modifier
  on a helper class. Surface accretion that wasn't reviewed.
- **Accidental public-API removals** — a refactor that drops a public
  member without a deprecation cycle. SemVer break that wasn't
  reviewed.

Diff-against-baseline failures block CI; updating the baseline is a
deliberate `git add` step that surfaces the decision in PR review.

**Artifact-content assertions** (added per review A10) extend the
contract check beyond public-surface diff. The same test project
asserts that every produced `.nupkg` contains:

- A `README.md` at package root (matching the in-repo per-library
  README).
- An `icon.png` at package root.
- `<PackageLicenseExpression>` metadata in the `.nuspec` (MIT for
  the libraries; the `THIRD-PARTY-NOTICES.txt` file for the Reports.*
  packages — see A7).
- SourceLink metadata resolvable to the published commit
  (`<RepositoryUrl>` + `<RepositoryCommit>` present, `<PublishRepositoryUrl>`
  honored).
- Dependency groups matching the project's `<TargetFramework>` — no
  empty or duplicated groups.
- Static-web-asset completeness for `.Razor`-SDK packages
  (Components, Reports.Blazor, Security.Blazor): every `.razor`
  compile output and `wwwroot/` asset present in the package's
  staticwebassets manifest.

The intent: build success is not package quality. A package can
restore cleanly and still be externally broken (missing license,
broken SourceLink, missing static web asset). Artifact-content
assertions catch those before publish.

### 6.3 What the validation does NOT prove

The smoke proves install + build + first-page-render. It does NOT
prove:

- That Security correctly enforces a `RowRestriction` against a real
  database — that's covered by the existing
  `Dabbler.Security.Tests` integration suite.
- That a real banded report generates correct PDF output — that's
  covered by `Dabbler.Reports.Blazor.Tests` and the existing
  `AcmeCoffeeOnline.Playwright/Reports/*` E2E.
- That MudBlazor + Dabbler.Components + Dabbler.Security.Blazor compose
  correctly under the Playwright watermark — the existing
  `AcmeCoffeeOnline.Playwright/` suite covers this against the
  ProjectReference path.

The smoke validates **packaging integrity**, not platform correctness.
Platform correctness is the rest of the test suite's job.

### 6.4 Pre-push checklist (Session 7 deliverable)

Before any `dotnet nuget push`:

- [ ] All non-Playwright tests green against the ProjectReference path.
- [ ] All Playwright tests green against the ProjectReference path.
- [ ] `tools/pack.ps1 -Version <next>` runs clean (every library packs).
- [ ] `Dabbler.Packaging.Smoke` passes against the freshly packed feed.
- [ ] `Dabbler.PublicSurface.Tests` clean (no unexplained baseline diff).
- [ ] `CHANGELOG.md` entry exists for the new version.

Session 7 lands this checklist as `docs/packaging/release-checklist.md`
plus the `tools/pack.ps1` script. Session 8 lands the template wiring
and a green smoke run.

---

## 7. How the Session 4 carry-overs land

The value review tagged 16 gaps as `fix-during-packaging`. Six of
those are within this proposal's scope and listed in the table
below (G3, G4, G8, G9, G10, G14 from the "Session 5 inputs" list
in `platform-value-review.md` §7); the remaining 10 land at
Session 6 surface freeze, Session 7 metadata work, Session 8
template work, or Session 9 consumer validation. The
review's §7.5 carry-over traceability table is the
authoritative cross-reference for every value-review gap (G1-G23)
and overrides the table below where they disagree.

| Gap (from §7 of value review) | Disposition in this proposal | Lands at |
|--------------|-----------------------------|---------|
| **G3** — `dotnet add package` Quick Starts before NuGet ships | Per-library READMEs **remove** Quick Start install commands until 0.1.0-preview.1 ships. Until then, Quick Starts say *"Reference `src/Libs/Dabbler.<Module>/` from your project; published NuGet packages forthcoming."* When the first preview ships, the READMEs flip to `dotnet add package Dabbler.<Module> --prerelease`. | Session 6 surface freeze |
| **G4** — Components compat surfaces | `[Obsolete]` markers per §3.7. Single sunset version (0.4.0). | Session 6 surface freeze |
| **G8** — Security defaults posture | Add a *"Security posture and defaults"* subsection to `docs/platform-overview.md` and `src/Libs/Dabbler.Security/README.md` spelling out: default-permissive user-type roles, `FailOpen` for unregistered resources, baked-in role bypass of `DefaultMask`. **Defaults do not change** — only documentation. | Session 6 surface freeze |
| **G9** — RDF schema documentation | New `docs/reports/rdf-schema.md` linked from `Dabbler.Reports.Core/README.md`. Schema + one worked example. Full authoring tooling stays deferred. | Session 6 surface freeze |
| **G10** — Multi-tenancy story | New *"Organization / multi-tenancy"* subsection in `docs/platform-overview.md` and `src/Libs/Dabbler.Security/README.md`: where `OrganizationId` is persisted, how row-scoping is applied, how role-switching interacts with org membership. | Session 6 surface freeze |
| **G14** — Versioning policy | §3.3 above. | This session |
| **G12 / G13** — Overview refactor | §5 and the overview's *"Where to go next"* split: "For external adopters" sub-section (libraries, install, license, compatibility matrix) vs "For contributors / repo maintainers" sub-section (workflow doc, governance two-pager, AI router). | Session 6 surface freeze |
| **G21** — Stack constraints | New *"Stack and audience"* one-paragraph callout in `docs/platform-overview.md` so adopters can self-disqualify in 60 seconds (Blazor Server-only, MudBlazor-coupled, .NET 10 / current LTS, SQL Server-centric security persistence). | Session 6 surface freeze |
| **G22** — Security model rigidity | Add bullets to the overview's *"Authorization model"* section explicitly noting no role inheritance and that baked-in privileges are compiled-in. | Session 6 surface freeze |
| **G23** — REST-only vs `UseEfCoreStore` | One-sentence clarification in `docs/platform-overview.md` that the REST-only principle applies to *business* data, not to platform-internal stores (security config, audit log). | Session 6 surface freeze |

The proposal does NOT pre-commit to wording for any documentation
update beyond what's needed to convey the substance. Session 6 surface
freeze owns the final text.

---

## 8. Open questions for Session 6 surface freeze

> **Updated post-review (A17):** the original Q1 (publish timing) is
> resolved by §3.3's binding publish gate and is removed. Q2-Q5 renumber
> to Q1-Q4. The compat-surface and DevIdp questions were also
> resolved by tri-provider review (A4 and A6); they are listed below
> as resolved-during-review entries, not open questions.

These are the remaining decisions the surface freeze must lock in:

- **Q1.** Versioned compatibility matrix between Dabbler.* and W0 +
  MudBlazor: how is it published? **Recommended:** extend the existing
  `docs/COMPATIBILITY.md` with a "Dabbler.* libraries" section listing
  each library version against MudBlazor / W0 / .NET versions known
  to work together. Update at every release. (Promoted from
  open question to Session 6 deliverable per A18.)
- **Q2.** Should the template package install `Dabbler.Testing.Blazor`
  by default? It's not strictly needed for `dabbler-min` and arguably
  not for `dabbler-app` either if the consumer never writes Playwright
  tests. **Recommended:** `dabbler-app` includes it (the bUnit test
  project demonstrates the watermark pattern); `dabbler-min` does not.

**Resolved during tri-provider review:**

- ~~Q (publish timing)~~ — resolved: §3.3 publish gate is binding,
  not advisory.
- ~~Q (Components compat surface disposition)~~ — resolved (A4):
  separate `Dabbler.Components.Compat` package, not `[Obsolete]`-in-main.
- ~~Q (DevIdp distribution)~~ — resolved (A6): `dotnet tool` via new
  `Dabbler.Security.DevIdp.Tool` package.

---

## 9. Out of scope (explicit non-goals)

The following are explicitly **not** in scope for this proposal or for
the remaining sessions of `packaging-and-template-readiness`:

- **Re-litigating the rebuild's narrowing decisions.** Composition over
  configuration, no role inheritance, no metadata-driven UI generation,
  no `MasterDetailsConfig<TEntity>` config DSL. These are settled per
  the project-guidance Principles → Precedence rule.
- **Changing the security defaults.** `FailOpen` for unregistered
  resources, default-permissive user-type roles, baked-in role
  bypass of `DefaultMask` — these are intentional posture decisions
  that the value review's G8 disposition explicitly chose to
  document, not change.
- **Adding multi-targeting** (net8.0 / net9.0). The current LTS
  principle applies. Multi-target only if external demand surfaces
  and is concrete (a named consumer asking).
- **Adding a Linux-first / containerized dev story.** Compose stack
  already runs Linux containers under Docker Desktop on Windows; an
  adopter on Linux uses the same compose files. No new dev-host
  affordances.
- **Cross-publishing to MyGet / Azure Artifacts / GitHub Packages.**
  nuget.org is the only public feed in scope. Private feeds are an
  adopter concern.
- **Authoring an Access-CLI specialization of the template.** A future
  CLI consumer scaffolds on top of `dabbler-app` (or `dabbler-min`)
  with its own code-gen; the template stays generic.

---

## 10. Risks and rollback

### 10.1 What can go wrong

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| `dotnet pack` fails on a library because of metadata gaps | High (already known: icon.png missing, Security.* has no metadata) | Pack failure blocks publish | Session 6 surface freeze + Session 7 release checklist; smoke (§6) catches this before push |
| Published preview package has wrong dependency closure (e.g., `Dabbler.Security.Blazor 0.1.0` references `Dabbler.Components 0.0.9`) | Medium | NuGet restore fails for consumers | Single `$(DabblerVersion)` MSBuild property + lock-step release |
| Template install hits version-mismatch with consumer's existing `Directory.Packages.props` | Medium | Adopter friction at first install | Template owns its own version pins; document the constraint |
| Source Link breaks under reproducible-build mode | Low | F12 → "Source not available" in consumer debugger | `EmbedUntrackedSources` mitigates; smoke validates a debugger source-step path |
| Compat surfaces sunset (`0.4.0` removal in `Dabbler.Components.Compat`) bites unannounced consumers | Low (today there are none) | Internal compile breakage on next release | The dedicated `.Compat` package per A4 means the sunset is in a single PackageId; warning gives one minor cycle's notice |
| Multi-package version skew during a partial publish (e.g., publishing 4 of 8 succeeded, 4 failed) | Medium | Half-published release is inconsistent | Staged publish pipeline pushes to staging feed first, then nuget.org in dependency order; partial-failure recovery is unlist + re-push at next patch version, not rollback (see §10.2). |
| **Upstream MudBlazor / W0 toolchain / .NET LTS version drift breaks Dabbler.* compatibility between releases** (A15) | Medium | Consumer-side compile breakage on upgrade | Maintain `docs/COMPATIBILITY.md` Dabbler.* matrix per release; release cadence aligned with W0/MudBlazor minor bumps so the matrix update lands with each Dabbler.* release. |
| **Adopters cargo-cult template dev defaults into production** (A16) — e.g., `EmailSenderType.DevMailHog`, no MFA, JSON file store, hard-coded magic-link secret | Medium | Production deployment with insecure defaults | Template `appsettings.json` block markers include `// PRODUCTION: replace before deploy` comments around every dev-only setting. Template README has a "Before you deploy" section enumerating each setting and its production replacement. |

### 10.2 Rollback paths

> **Updated post-review (A9):** the "delete from nuget.org within 24h"
> model was replaced. nuget.org is not transactional — consumers may
> have already resolved against a bad version by the time deletion
> lands, and some feeds aggressively cache. The staged-promotion +
> unlist model below is the right shape.

The release pipeline is **staged**: publish first to a staging feed,
validate install from there (§6.1 smoke against the staging feed),
then push to nuget.org in dependency order (Components first,
Security/Reports.Core next, etc.). The smoke is the primary defense;
the rollback paths below are the secondary defense.

- **First preview release goes wrong.** Publish a `0.x.y-preview.N+1`
  with the fix to nuget.org. **Unlist** the bad version on nuget.org
  (do not delete; deletion is not relied on for rollback because
  nuget.org is not transactional and feed caches may have already
  resolved against the bad version). No `1.x` SemVer commitments
  exist yet.
- **Template package goes wrong.** Push a fix; templates are forward-
  installed, so adopters who already scaffolded an app are not
  affected (the scaffolded app references published Dabbler.* package
  versions, not the template's source).
- **A compatibility-surface (`Dabbler.Components.Compat`) sunset breaks
  an unanticipated consumer.** Publish a patch release that re-includes
  the affected types; revisit the sunset version in the next minor.
- **Partial-publish failure** (4 of 8 succeed, 4 fail). Unlist the
  4 that succeeded so the half-published release is not consumed,
  fix the cause, re-pack with the next preview version, re-push the
  full set.

---

## 11. Methodology and rationale notes (Opus's review perspective)

These notes are the orchestrator's own perspective on the proposal —
analogous to the `value-review-opus.md` reviewer file in Session 4.
The routed reviewers (GPT-5.4, Gemini Pro) get §0-10 as their input
and may disagree with the choices below; this section is the
orchestrator's `packaging-review-opus.md` content folded into the
proposal so the routed reviews receive an identical self-contained
prompt.

### 11.1 Why lock-step versioning across all packages

The platform's coupling is implicit but real: `Dabbler.Security.Blazor`
consumes `Dabbler.Security` types in its component signatures, and
`Dabbler.Components` types appear in `Dabbler.Reports.Blazor` markup.
Independent versioning would require a deprecation matrix between
versions of two packages that may or may not be co-released. Lock-step
versioning trades a small risk of unnecessary version bumps for the
disappearance of an entire class of NuGet-restore-fails-with-confusing-
error-message bug. The cost (every consumer upgrades all eight at
once) is the right shape for pre-1.0 platforms.

### 11.2 Why `[Obsolete]` for compat surfaces vs. a `.Compat` package

> **Updated post-review:** this section's recommendation
> (`[Obsolete]`-in-main) was **reversed** by gpt-5-4's review at
> Major severity (A4). The reversed disposition is: ship the compat
> surfaces in a separate `Dabbler.Components.Compat` package with
> sunset version 0.4.0. The original rationale below is preserved for
> historical context.
>
> The reversal reasoning: `[Obsolete]` does not relax the public-API
> versioning commitment — a consumer who pins to a SemVer-aware range
> still gets the type and its compile-time signature when they
> restore. Public-surface pollution from compat types affects every
> consumer's IntelliSense, baseline diffs, and surface review. A
> separate `.Compat` package shifts those costs only onto the
> consumers who genuinely need the compat surface.

A `.Compat` package adds one more PackageId to the release matrix and
implies a maintenance commitment. The compatibility surfaces have no
production consumer today (the rebuild removed them); their reason for
existing is documentation continuity for adopters reading the
historical narrative. `[Obsolete]` ships them in the main package with
a compile-time signal and a sunset version; no separate maintenance
track. If a consumer surfaces in 0.4.0 needing them, restoring the
markers in a patch release is cheap.

### 11.3 Why the consumer-install smoke matters more than test coverage

The existing test suite (864 non-Playwright tests + Playwright)
validates the platform behavior **via ProjectReferences**. The smoke
test validates the platform behavior **via published packages**.
These prove different things. A library that builds clean with
ProjectReferences can fail with PackageReferences if (a) a transitive
public type is in an `InternalsVisibleTo`-only assembly, (b) a project
reference pulls a multi-target framework reference the package
manifest doesn't, (c) an analyzer or build target ships as
ProjectReference-only and is missing from the pack output, or (d) a
content file that ships as `Pack=true` paths incorrectly.

The smoke catches all four classes by running the same code path a
real consumer runs. It is the single highest-value addition the
packaging work can make.

### 11.4 Why single `net10.0` target and not multi-target

Multi-targeting (`net10.0;net9.0;net8.0`) doubles the build matrix,
roughly doubles symbol-package size, and ties the platform to LTS
versions whose security guarantees are eroding. The project-guidance
Principles → Architecture rule says *"Stay on the current .NET LTS
line unless the human explicitly approves a change."* Multi-target
adoption would implicitly relax that. Concrete external demand for
net9.0 / net8.0 has not surfaced; speculative multi-targeting is
overhead now in exchange for hypothetical future adoption flexibility.

### 11.5 Why two templates instead of one parameterized one

`dotnet new`'s `--include-reports false` parameterization model
requires the template to support both configurations cleanly. A
parameterized template's `Program.cs` becomes a forest of
`#if INCLUDE_REPORTS … #endif` blocks, the test scaffolding has to
either include or omit a project, and the README has to document
both configurations. Two templates trade a small amount of duplicated
content for clean, focused entry points. Adopters who want both
ends of the spectrum are not the design center; the design center is
adopters who want one of the two configurations and can read the
scaffold to understand it.

### 11.6 Why this proposal is shorter than Session 4's value review

Session 4 was a tri-provider review of an *existing* artifact (the
platform); the proposal had to absorb each reviewer's findings and
synthesize. Session 5 is a tri-provider review of a *new* artifact
(this proposal); the orchestrator's authorial direction is upstream
of the reviewers. The right shape for the proposal is "concrete
recommendations the reviewers can poke holes in", not "exhaustive
exploration of every option." If a reviewer surfaces an option this
proposal didn't consider, that's the value the review adds.

### 11.7 Where this proposal might be wrong (and how the review answered)

A reviewer who disagrees should poke at:

- **§3.3 versioning policy** — is lock-step the right call, or are
  some libraries (`Dabbler.Testing.Blazor` with no Dabbler.*
  dependencies) artificially constrained by it? Counter-argument: the
  cost of an unnecessary version bump on Testing.Blazor is one
  PackageVersion line in `Directory.Packages.props`; the benefit of
  no version-skew confusion is global.
- **§3.7 compat surface disposition** — is `[Obsolete]` really the
  right call, or does a separate `.Compat` package serve the small
  set of adopters who still have legacy DabblerShell code? Counter-
  argument: there are zero such adopters today (the rebuild removed
  them).
- **§4.4 Stubs split** — Option A (single package) vs. Option B
  (sub-packages) is a genuine judgment call. Counter-argument for
  Option B: the transitive Reports.Core dependency in
  `Dabbler.Platform.Stubs` is the kind of thing that, once published,
  is hard to walk back without a major version bump.
- **§5.1 template scope** — two templates vs. one parameterized
  template vs. three templates including `dabbler-api`. The
  proposal picks two; reviewers may surface a reason to pick a
  different number.
- **§6.1 smoke proof** — is the smoke comprehensive enough? Should it
  also validate that `dotnet new dabbler-app -o MyApp` produces an app
  whose Playwright tests pass (proving the watermark wiring round-
  trips)? Counter-argument: that's a Session 10 Playwright deliverable;
  the smoke focuses on packaging integrity.

These are the points where a tri-provider review most legitimately
adds value beyond a single-reviewer pass.

**Post-review calibration:** 4 of these 5 pre-identified weak points
were confirmed by routed reviewers as substantive critique:

- §3.3 — Both reviewers endorsed lock-step; gpt-5-4 (A5) challenged
  the related preview-suffix-duration choice. (Same-section critique,
  different specific detail.)
- §3.7 — GPT-5.4 (A4) confirmed: `[Obsolete]`-in-main is wrong;
  separate `.Compat` package is right. **Reversal recorded in §11.2.**
- §4.4 — Gemini Pro (A3, Critical) confirmed: split now, not deferred.
  **Reversal recorded in §4.4's note** (split lands in Session 7).
- §5.1 — Neither reviewer challenged the two-template scope. **Held
  as proposed.**
- §6.1 — GPT-5.4 (A2, Critical) confirmed: smoke depth must expand.
  **Acceptance criteria expanded; see `packaging-approach-review.md`
  §7.2.**

4/5 confirmation rate. The self-identification discipline of naming
weak points proactively is the orchestrator's signal to reviewers
about where to focus — and the routed reviewers honored that focus,
adding their own findings on top.

---

## 12. What happens after this proposal is reviewed

The proposal is the input for the tri-provider review (Step 4 of the
session). Each reviewer (Opus self-contribution = this §11; GPT-5.4
+ Gemini Pro = routed reviews) receives §0-10 as a self-contained
prompt and answers:

> *"Does this packaging approach deliver the platform's value cleanly
> to outside consumers? What would you change?"*

The synthesis lands as
`docs/session-sets/packaging-and-template-readiness/packaging-approach-review.md`
with verdicts, agreements, disagreements, and an adjustment list with
dispositions. Small adjustments are applied inline (proposal-text
edits, naming changes, defaults). Larger adjustments record as inputs
to Session 6 surface freeze.

---

## 13. Reference materials cited inline

- Session 4 [`platform-value-review.md`](./platform-value-review.md)
  §7 (Session 5 inputs) — gap list with dispositions used in §7 above.
- `docs/platform-overview.md` — current consumer-facing entry point;
  many proposal items prescribe updates to it that Session 6 owns.
- `Directory.Build.props` / `Directory.Packages.props` — current
  build / package version state, source of fact-check for §3.1.
- `src/Libs/Dabbler.*/Dabbler.*.csproj` — per-library packaging
  metadata, source of fact-check for §3.5, §3.6, §3.7.
- `docs/planning/project-guidance.md` Principles → Architecture (LTS
  rule), Principles → Precedence (rebuild narrowing).
