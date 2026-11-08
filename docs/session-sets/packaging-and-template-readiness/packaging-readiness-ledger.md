# Packaging Readiness Ledger

> **Session:** packaging-and-template-readiness / Session 6 of 11
> **Date:** 2026-05-15
> **Author:** Claude Opus 4.7 (orchestrator, direct synthesis using already-loaded
> Session 4 + Session 5 context per `feedback_orchestrator_self_contributes_to_review_panel`).
> **Status:** Frozen contract for Sessions 7-11. Every binding decision below is
> the authoritative input for downstream packaging, template, validation,
> Playwright, and UAT work. Reversals require either operator override or a new
> session-set spec.
> **Inputs:**
> - [`packaging-approach-proposal.md`](./packaging-approach-proposal.md) (post-inline-edits)
> - [`packaging-approach-review.md`](./packaging-approach-review.md) (tri-provider synthesis, 18 adjustments A1-A18)
> - [`platform-value-review.md`](./platform-value-review.md) (Session 4 gap dispositions G1-G23)
> - [`spec.md`](./spec.md) Session Set Configuration block (`requiresUAT: true`,
>   `requiresE2E: true`, `uatScope: per-set`)

---

## 0. Reading guide

A future-session orchestrator (Session 7 onwards) reads this document
top-to-bottom and treats §3-§9 as binding. Sessions 7-11 implement
against these contracts; they do not relitigate decisions recorded here.
Per the project-guidance Precedence rule, this ledger overrides earlier
text in the proposal or review where they disagree — both upstream
documents capture in-flight reasoning that this ledger has now resolved.

A reader who only wants the answer reads §3 (packable surface) and §10
(acceptance criteria for the set as a whole).

---

## 1. Executive summary

This ledger freezes:

- **Twelve library / sub-package NuGet packages + one template package
  + one CLI tool package = 14 total** (§3). The 12 library packages
  comprise the eight library projects under `src/Libs/` that pack, a
  new `Dabbler.Components.Compat` package carved out of
  `Dabbler.Components` per A4, and three new
  `Dabbler.Platform.Stubs.{Components,Reports,Security}` sub-packages
  carved out of `Dabbler.Platform.Stubs` per A3 (the original umbrella
  remains as a meta-package). Plus the new `Dabbler.Templates`
  package (Session 8 deliverable) and the new
  `Dabbler.Security.DevIdp.Tool` package per A6.
- **The `Dabbler.Platform.Stubs` sub-package split** (§4) — meta-package
  + three focused sub-packages (`.Components`, `.Reports`, `.Security`)
  per A3, implementing in Session 7.
- **The two-template VS template contract** (§5) — `dabbler-app` and
  `dabbler-min`, both referencing published NuGet versions only.
- **The consumer-install validation acceptance criteria** (§6) — smoke
  depth per A2, hermetic pack per A1, artifact-content assertions per
  A10.
- **The documentation deliverables landing in this session vs Session
  7/8** (§7) — scenarios.md, retrofit-existing-app.md, COMPATIBILITY.md
  Dabbler.* extension, rdf-schema.md, platform-overview.md G* updates,
  per-library README structural standardization.
- **The intra-set parallelization map** (§8) — which Sessions 7-11
  workstreams can run in parallel worktrees.
- **The packaging-and-template-readiness acceptance criteria** (§10) —
  the bar Session 9 consumer validation must clear before this set
  closes.

Nothing in this ledger is speculative. Every decision either restates
a Session 5 commitment, applies a Session 5 review adjustment, or
resolves an explicit open question the operator has approved this
session.

---

## 2. Binding decisions index

The table below is the single source of truth for every adjustment
from Session 4 (G*) and Session 5 (A*) review work. Status values:

- `frozen-here` — decision locked in this ledger; Session 7+ implements.
- `applied-this-session` — file edits land in Session 6 (this session).
- `applied-earlier` — already done in Session 4 or 5 inline edits.
- `defer-out-of-set` — explicitly deferred outside this session set.

### 2.1 Session 5 review adjustments (A1-A18)

| ID | Adjustment | Status | Where implemented |
|----|-----------|--------|--------------------|
| A1 | Hermetic pack pipeline (`tools/pack.ps1` with isolated `NUGET_PACKAGES`, explicit `RestoreSources`, fail-if-stale-cache). | `frozen-here` → `Session 7` | §6.3 acceptance criteria; Session 7 authors `tools/pack.ps1`. |
| A2 | Smoke depth expansion (sign-in, secured page, sample report, PDF export, scaffolded bUnit). | `frozen-here` → `Session 7` (+ Session 8 template wiring) | §6.1 + §6.2 acceptance criteria. |
| A3 | Split `Dabbler.Platform.Stubs` into `.Components`, `.Reports`, `.Security` sub-packages + meta-package. | `frozen-here` → `Session 7` | §4 stubs contract. |
| A4 | Move compat surfaces to separate `Dabbler.Components.Compat` package (sunset 0.4.0). | `frozen-here` + `applied-this-session` (csproj scaffold) | §3.4 + §9.1. |
| A5 | Stay on `-preview.N` through 1.0.0 (no intermediate `0.x.0-non-preview` phase). | `applied-earlier` (Session 5 inline edit to proposal §3.3) | Restated in §3.6. |
| A6 | `Dabbler.Security.DevIdp.Tool` as `dotnet tool`. | `frozen-here` → `Session 8` | §3.3. |
| A7 | `THIRD-PARTY-NOTICES.txt` shipped in `Dabbler.Reports.{Core,Export,Blazor}`; license disclosure in `<Description>`. | `frozen-here` → `Session 7` | §6.2 artifact-content + §3.7. |
| A8 | `docs/packaging/scenarios.md` scenarios matrix (Session 6). Optional 2-3 metapackages evaluation (Session 7). | `applied-this-session` (scenarios.md) + `frozen-here` (Session 7 metapackage decision) | §7.1 + §3.8. |
| A9 | Staged-promotion + unlist rollback model (replacing nuget.org-delete). | `applied-earlier` (Session 5 inline edit to proposal §10.2) | Restated in §6.3. |
| A10 | Artifact-content assertions in `Dabbler.PublicSurface.Tests` (readme, icon, license metadata, SourceLink, deps, static web assets). | `applied-earlier` (Session 5 inline edit to proposal §6.2) → Session 7 implementation | §6.2 acceptance criteria. |
| A11 | Per-library README structural standardization (audience, prerequisites, install, registration, example, related packages, warnings). | `applied-this-session` (8 READMEs) | §7.5 + Session 6 implementation. |
| A12 | Template seeds `admin@local.test` user + onboarding README. | `frozen-here` → `Session 8` | §5.4. |
| A13 | Template includes one-file SQLite or in-memory seed for sample query page + report. | `frozen-here` → `Session 8` | §5.4. |
| A14 | `docs/packaging/retrofit-existing-app.md` (Session 6). | `applied-this-session` | §7.2. |
| A15 | `§10.1` risks: MudBlazor/W0/.NET version-drift row. | `applied-earlier` (Session 5 inline edit to proposal §10.1) | Restated in §6.4. |
| A16 | `§10.1` risks: template-defaults-cargo-culted-to-prod row. | `applied-earlier` (Session 5 inline edit to proposal §10.1) | Restated in §5.6. |
| A17 | Publish gate binding (first public publish gated on Session 7 metadata + §6.1 smoke pass). | `applied-earlier` (Session 5 inline edit to proposal §3.3 + §8) | Restated in §3.6. |
| A18 | `docs/COMPATIBILITY.md` Dabbler.* matrix extension (Session 6). | `applied-this-session` | §7.3. |

### 2.2 Session 4 value-review gaps (G1-G23)

| ID | Gap | Status | Where implemented |
|----|-----|--------|--------------------|
| G1 | No shipped packaged adoption path. | `frozen-here` → `Sessions 7-9` | The entire packaging effort. |
| G2 | `DabblerPdf` undocumented at platform level. | `applied-earlier` (Session 4 inline edit to overview + Reports.Export README) | Reinforced by A7 in Session 7. |
| G3 | Per-library Quick Starts show `dotnet add package` prematurely. | `applied-this-session` (8 READMEs) | §7.5 — Quick Starts read "Reference src/Libs/Dabbler.<Module>/" until first preview ships. |
| G4 | Compat surfaces muddy supported contract. | `frozen-here` + `applied-this-session` (csproj scaffold) — resolved by A4. | §3.4 + §9.1. |
| G5 | Stub-mode swap depth not shown. | `applied-this-session` (platform-overview.md update) | §7.4 + §4.5. |
| G6 | `RegisterPages/Entities/Endpoints/Controls` taxonomy opaque. | `applied-this-session` (Security README via A11) | §7.5 + §7.4 (overview cross-link). |
| G7 | End-to-end security enforcement examples missing. | `applied-this-session` (Security README via A11) | §7.5. |
| G8 | Security defaults need prominent documentation. | `applied-this-session` (platform-overview.md update) | §7.4. |
| G9 | RDF schema documentation absent. | `applied-this-session` (`docs/reports/rdf-schema.md`) | §7.6. |
| G10 | Multi-tenancy story implicit. | `applied-this-session` (platform-overview.md update + Security README) | §7.4 + §7.5. |
| G11 | First-app tutorial / migration guide. | `frozen-here` → `Session 8` | §5.5. |
| G12 | UAT DSL + Security.Blazor + Testing.Blazor undersold. | `applied-this-session` (platform-overview.md update) | §7.4. |
| G13 | Internal-facing references clutter "Where to go next". | `applied-this-session` (platform-overview.md update — external/contributor split) | §7.4. |
| G14 | Public versioning policy. | `applied-earlier` (Session 5 proposal §3.3 + A5/A17) | Restated in §3.6; per-library README disclosure via A11. |
| G15 | Copy-able accessibility test pattern. | `defer-out-of-set` | Honored as proposed (not blocking). |
| G16 | Platform-level license aggregation in overview. | `applied-earlier` (Session 4 inline edit) | Reinforced by A7 disclosure in Reports.* READMEs (§7.5). |
| G17 | Governance / issue-filing / maintainer-contact pointer. | `applied-this-session` (platform-overview.md update) | §7.4. |
| G18 | Per-library README depth varies. | `accept-as-limitation` (`applied-earlier`) | A11 standardizes sections present, not depth. |
| G19 | Playwright synchronization story narrower than headline. | `applied-this-session` (platform-overview.md update) | §7.4. |
| G20 | Verbose security configuration; no convention-based discovery. | `defer-out-of-set` | Honored as proposed (quality-of-life future). |
| G21 | Stack constraints (Blazor Server, MudBlazor, .NET 10, SQL Server). | `applied-this-session` (platform-overview.md "Stack and audience" callout) | §7.4. |
| G22 | Security model rigidity (no role inheritance, compiled-in baked-in privileges). | `applied-this-session` (platform-overview.md Authorization model bullets) | §7.4. |
| G23 | REST-only vs `UseEfCoreStore` clarification. | `applied-this-session` (platform-overview.md REST-only note) | §7.4. |

**Totals across A* and G*:** 18 + 23 = 41 distinct adjustments.
By status:

| Status | Count |
|--------|-------|
| `applied-earlier` (Session 4 / Session 5 inline) | 8 (A5, A9, A10-partial, A15, A16, A17, G2, G16) |
| `applied-this-session` (Session 6 lands the change) | 21 (A4 csproj scaffold, A8, A11×8, A14, A18, G3×8, G5, G6, G7, G8, G10, G12, G13, G17, G19, G21, G22, G23) — note overlapping counts: A11 covers 8 READMEs, G3 covers the same 8 READMEs as a removal pass, A8 / A14 / A18 are net-new docs, the platform-overview.md G* updates land as one batched edit. |
| `frozen-here` (decision locked, Session 7+ implements) | 9 (A1, A2, A3, A6, A7, A8-metapackages-sub-track, A10-implementation, A12, A13) — note overlap with `frozen-here` rows that have an `applied-this-session` component (A4 + A11 already counted in `applied-this-session`). |
| `defer-out-of-set` | 2 (G15, G20) |
| `accept-as-limitation` (no action) | 1 (G18) |

The arithmetic intentionally double-counts items that span phases
(e.g., A4 has both a Session 6 csproj scaffold and a Session 7
content move; A8 has a Session 6 doc and a Session 7 optional sub-track;
A11 lands README *structure* in Session 6 and depth refinements may
continue in Session 7). The double-counting is deliberate to make the
phasing visible per-row; do not interpret the column totals as a clean
disposition tally — for that, see §11 of `packaging-approach-review.md`
and §4 of `platform-value-review.md`.

---

## 3. Packable surface (frozen)

This is the normative list of every package that ships from this
repository under the `packaging-and-template-readiness` effort.

### 3.1 Library packages — eight + one Compat package

| # | Package ID | Source path | Tier | Packs in Session |
|---|-----------|-------------|------|-------------------|
| 1 | `Dabbler.Components` | `src/Libs/Dabbler.Components/` | Foundation | 7 |
| 2 | `Dabbler.Components.Compat` (new) | `src/Libs/Dabbler.Components.Compat/` (carved from Components in Session 7 per A4) | Foundation (deprecated) | 7 |
| 3 | `Dabbler.Platform.Stubs.Components` (new) | `src/Libs/Dabbler.Platform.Stubs.Components/` | Stubs sub-package | 7 |
| 4 | `Dabbler.Platform.Stubs.Reports` (new) | `src/Libs/Dabbler.Platform.Stubs.Reports/` | Stubs sub-package | 7 |
| 5 | `Dabbler.Platform.Stubs.Security` (new) | `src/Libs/Dabbler.Platform.Stubs.Security/` | Stubs sub-package | 7 |
| 6 | `Dabbler.Platform.Stubs` (meta-package, restructured) | `src/Libs/Dabbler.Platform.Stubs/` (becomes meta-package referencing the three sub-packages) | Stubs umbrella | 7 |
| 7 | `Dabbler.Reports.Core` | `src/Libs/Dabbler.Reports.Core/` | Reports | 7 |
| 8 | `Dabbler.Reports.Export` | `src/Libs/Dabbler.Reports.Export/` | Reports | 7 |
| 9 | `Dabbler.Reports.Blazor` | `src/Libs/Dabbler.Reports.Blazor/` | Reports | 7 |
| 10 | `Dabbler.Security` | `src/Libs/Dabbler.Security/` | Security | 7 |
| 11 | `Dabbler.Security.Blazor` | `src/Libs/Dabbler.Security.Blazor/` | Security | 7 |
| 12 | `Dabbler.Testing.Blazor` | `src/Libs/Dabbler.Testing.Blazor/` | Testing | 7 |

Plus two non-library packages:

| # | Package ID | Source path | Tier | Packs in Session |
|---|-----------|-------------|------|-------------------|
| 13 | `Dabbler.Templates` (new) | `templates/Dabbler.Templates/` | Template | 8 |
| 14 | `Dabbler.Security.DevIdp.Tool` (new) | `tools/Dabbler.Security.DevIdp.Tool/` | CLI tool (`dotnet tool`) | 8 |

**Total: 12 library/sub-package NuGet packages + 1 template package + 1 CLI tool package = 14 packages.**

### 3.2 What does NOT pack

- `src/Libs/Dabbler.Security.DevIdp/` (the ASP.NET Core *app* — runnable
  loopback IdP). The CLI tool wrapper `Dabbler.Security.DevIdp.Tool`
  ships in package #14 and reuses this app's source via project reference.
- `src/Apps/AcmeCoffeeOnline.{Api,Web}/` — the demo application.
- All `tests/*` projects.
- All `tools/*` projects (except `Dabbler.Security.DevIdp.Tool` per #14).

### 3.3 Why this count

The eight library projects under `src/Libs/` matched real adoption seams
in Session 5 review §2.1 (all three reviewers endorsed or did not
challenge). The carve-outs (Compat from Components; three Stubs
sub-packages from one Stubs package) address tri-provider review
findings A3 and A4. The Templates package (#13) is the consumer install
path. The DevIdp.Tool package (#14) resolves the repo-leakage concern
from A6.

### 3.4 Compatibility surfaces — moving to `Dabbler.Components.Compat`

Per A4, the following types move out of `Dabbler.Components` into a
new `Dabbler.Components.Compat` package in Session 7:

- `DabblerShell` (compat shell)
- `DabblerNavigationPanel` (compat nav)
- `DabblerTabbedWorkspace` (compat tabs)
- `DabblerTopMenuBar` (compat menu)
- `DabblerCatalogPage` (compat catalog)

Sunset version: **0.4.0** — the `Dabbler.Components.Compat` package
publishes alongside `Dabbler.Components` through the final
`0.3.x-preview.N` release in the pre-1.0 line. At the `0.4.0`
(`-preview.N`) cut, `Dabbler.Components.Compat` **ceases publication**.
Consumers who still need these surfaces stay pinned to the last
published `Dabbler.Components.Compat 0.3.x-preview.N` they restored
(per §3.6, every pre-1.0 release carries the `-preview.N` suffix; no
plain `0.3.x` non-preview version exists). NuGet is append-only, so
the historical previews remain restoreable indefinitely.

The carve-out is mechanical: move the `.razor` / `.razor.cs` files into
the new project, retarget their namespace from `Dabbler.Components.*`
to `Dabbler.Components.Compat.*`, and the consumer-facing `using
Dabbler.Components` no longer pulls them. The Compat package depends
on `Dabbler.Components` (it's a value-add overlay, not a replacement).

### 3.5 Security and Security.Blazor csproj package metadata

Session 6 lands the missing csproj package metadata for these two
libraries (the other six already have it). See §9.2 for the applied
csproj changes.

### 3.6 Versioning policy (final)

Per A5 + A17 (Session 5 inline edits to proposal §3.3):

| Phase | Version range | Publish allowed? |
|-------|---------------|-------------------|
| Pre-1.0 | `0.x.y-preview.N` for **every** public release | Only after Session 7 metadata + §6.1 smoke pass |
| 1.0 release | `1.0.0` after Session 9 consumer validation passes end-to-end with no outstanding `fix-during-packaging` items | Yes |
| Post-1.0 | `1.x.y` / `2.x.y` per SemVer 2.0 | Yes |

**Lock-step versioning** across all packages via the
`$(DabblerVersion)` MSBuild property (proposal §3.1).

**Publish gate (binding, not advisory):** no `dotnet nuget push` to a
public feed (nuget.org or otherwise) until Session 7's metadata-tighten
work completes AND the `Dabbler.Packaging.Smoke` test passes against a
freshly packed local feed AND `Dabbler.PublicSurface.Tests` (with
artifact-content assertions per A10) is clean.

### 3.7 License posture

- **All Dabbler libraries are MIT.** This is set as `<PackageLicenseExpression>MIT</PackageLicenseExpression>`
  via `Directory.Build.props` (proposal §3.1).
- **`Dabbler.Reports.{Core,Export,Blazor}` carry a `THIRD-PARTY-NOTICES.txt`**
  documenting their transitive `DabblerPdf` dependency, which is
  **LGPL-2.1-only OR MPL-2.0** (DabblerPdf is a C# port of OpenPDF;
  surfaced in Session 4 G2 fix-now investigation).
- The Reports.* `<Description>` field includes a one-line license
  disclosure so the NuGet package page surfaces the implication before
  install (A7).
- The `dabbler-app` template README carries the same disclosure for
  adopters who scaffold a reports-enabled app.

### 3.8 Optional Session 7 metapackages (A8 sub-track)

The scenarios matrix (`docs/packaging/scenarios.md`, A8) is authored in
this session as the primary disclosure of common adoption shapes. If
that document alone is insufficient — measured by Session 7's smoke
testing surfacing more than ~3 consumer "what should I install?"
questions — the following metapackages publish in Session 7:

- `Dabbler.Reports` — references `Dabbler.Reports.Core` + `.Export` + `.Blazor`.
- `Dabbler.Security.All` — references `Dabbler.Security` + `.Security.Blazor`.
- `Dabbler.Platform` — references the full platform set (Components +
  Security.All + Reports + Stubs + Testing.Blazor).

The decision to ship these is **deferred to Session 7** and is **not
frozen here.** This ledger records the trigger condition for that
decision; Session 7 owns the call.

---

## 4. Stubs opt-in contract (frozen)

### 4.1 Sub-package decomposition (Session 7 implementation)

`Dabbler.Platform.Stubs` becomes a meta-package after A3:

```
Dabbler.Platform.Stubs  (meta-package; PackageReferences all three)
├── Dabbler.Platform.Stubs.Components  (Components stubs only)
│   └── depends on Dabbler.Components
├── Dabbler.Platform.Stubs.Reports     (Reports stubs)
│   └── depends on Dabbler.Reports.Core (and transitively DabblerPdf)
└── Dabbler.Platform.Stubs.Security    (Security stubs)
    └── depends on Dabbler.Security
```

A consumer who wants only `StubGridDataProvider` for bUnit Components
tests installs `Dabbler.Platform.Stubs.Components` and does **not** pull
DabblerPdf transitively. A consumer who wants everything installs the
meta-package and gets the same surface as the pre-split package.

Provider helper distribution across sub-packages:

| Sub-package | Helpers |
|-------------|---------|
| `.Components` | `AddStubGridDataProvider`, `AddStubComboBoxItemProvider`, `AddStubEntityMetadataProvider`, `AddStubNavigationDataProvider` |
| `.Reports` | `AddStubReportDataProvider`, `AddStubArtifactCatalogProvider`, `AddStubUsageTrackingProvider` |
| `.Security` | `AddStubAuthentication` |

The umbrella helper `AddDabblerPlatformStubs(definition)` continues to
work — it lives in the meta-package and forwards to per-sub-package
helpers.

### 4.2 The contract: stubs never silently activate

- **No auto-discovery.** Referencing `Dabbler.Platform.Stubs` (or any
  sub-package) does not register stubs. The consumer's `Program.cs`
  must explicitly call `AddDabblerPlatformStubs(...)` or a focused
  per-provider helper.
- **No environment-variable shortcut.** A production deployment that
  accidentally has the package referenced will not silently fall back
  to stubs. If a real provider is missing, DI throws — which is the
  right failure mode.
- **No production fallback.** Stubs are not a "use real if registered,
  otherwise stub" pattern. They are explicit opt-in.

### 4.3 The wiring pattern (canonical)

```csharp
// Production app (real providers)
builder.Services
    .AddDabblerSecurity(options => { /* real Security */ })
    .AddDabblerSecurityBlazor()
    .AddDabblerReports();
builder.Services.AddScoped<IGridDataProvider<Customer>, CustomerGridDataProvider>();

// Test or demo host (stub providers — opt-in via explicit calls)
builder.Services
    .AddDabblerPlatformStubs(definition)
    .AddStubGridDataProvider<Customer>(new[] { /* in-memory rows */ });
builder.Services.AddStubAuthentication(new StubUserContext { /* … */ });
```

Same component code runs against both real and stub providers without
conditional branches in the component.

### 4.4 bUnit test wiring

The Stubs library exposes **two** related entry points; they are not
interchangeable and the test author picks based on context:

| Entry point | Where it lives | When to use |
|-------------|----------------|-------------|
| `services.AddDabblerPlatformStubs(definition)` | Service-collection extension in the meta-package (also in each sub-package for its own provider slice). | Production-shaped DI registration — used in `Program.cs` of a *test or demo host* (e.g., a stub-backed demo app), and inside bUnit when the test wants the full umbrella registration via the standard `services` collection. |
| `ctx.AddDabblerStubs(definition)` | `TestContext` extension shipped by the same package; thin wrapper that calls `AddDabblerPlatformStubs` on the test's `Services` collection plus any bUnit-specific render-helper registrations. | The conventional bUnit shortcut. Use this in bUnit tests when you don't need to customize the DI builder pipeline before calling the helper. |

```csharp
// Option A — bUnit shortcut (recommended for typical tests):
ctx.AddDabblerStubs(definition);

// Option B — explicit equivalent (use when you need to interleave
// other DI customizations between platform-stub registration and
// component render):
ctx.Services.AddDabblerPlatformStubs(definition);
ctx.Services.AddStubGridDataProvider<Customer>(new[] { /* rows */ });
```

The `dabbler-app` template's scaffolded bUnit project uses **Option A**
by default; teams that need finer control switch to Option B.

### 4.5 G5 + G10 platform-overview update

Session 6 updates `platform-overview.md` with the side-by-side
production-vs-stub `Program.cs` snippet (G5 carry-over) plus the
Organization / multi-tenancy subsection (G10 carry-over). See §7.4.

---

## 5. VS template contract (frozen)

### 5.1 Template count and shape

**One NuGet package, two templates:** `Dabbler.Templates` installs
`dabbler-app` and `dabbler-min`.

| Short name | Reports? | Security? | Stubs in `Program.cs`? | Stubs in scaffolded test project? | bUnit project? | Testing.Blazor? |
|------------|----------|-----------|-------------------------|------------------------------------|----------------|------------------|
| `dabbler-app` | Yes (one sample report) | Yes (cookie + JSON store, no MFA, no OIDC by default) | **No — real providers only** | Yes (`Dabbler.Platform.Stubs` meta-package referenced; bUnit `TestContext` calls `ctx.AddDabblerStubs(...)` — the canonical bUnit shortcut from §4.4) | Yes | Yes |
| `dabbler-min` | No | Yes (Security only) | **No — real providers only** | No | No | No |

**Stub-mode opt-in (per §4.2): the scaffolded `Program.cs` does NOT
register stubs.** The production application registers real providers
exclusively; DI throws if a real provider is missing. The
`dabbler-app` template's *bUnit test project* references the Stubs
meta-package and opts in via the canonical `ctx.AddDabblerStubs(...)`
bUnit shortcut from §4.4 (which wraps the service-collection
`AddDabblerPlatformStubs` helper) — that is the only place stubs
activate in the template scaffold. This preserves the "stubs never
silently activate in production" contract (§4.2) while still
demonstrating the stub pattern in the scaffolded test code.

A future `dabbler-api` template is **not** in this set. Defer until
external demand surfaces.

### 5.2 Target framework

Single target: `net10.0` (current LTS). No multi-targeting per
proposal §11.4.

### 5.3 Reference shape

Templates reference **published NuGet versions** of the platform
libraries, not project references. The template's purpose is to model
the external adoption path; project references are for the inner-loop
dev experience inside this repo.

### 5.4 Seeded data and admin user (A12 + A13)

`dabbler-app` scaffolds:

- A seeded admin user `admin@local.test` with magic-link sign-in only
  (no password, no MFA in the template defaults).
- A one-file SQLite or in-memory seed populating the sample query page
  (the `Customer` entity stub) and the sample report with non-empty
  data on first run.
- An onboarding README inside the scaffolded `MyApp/` showing how to
  take over via real authentication (replace `admin@local.test` with a
  real persona; wire SMTP / OIDC; etc.).

### 5.5 First-app tutorial (G11)

A separate `docs/getting-started.md` lands in Session 8 alongside the
template. It walks an adopter through `dotnet new dabbler-app -o MyApp`
→ build → run → sign in as `admin@local.test` → reach the sample query
page → run the sample report → run the scaffolded bUnit tests → run
Playwright. This is the missing "build your first app in 30 minutes"
path.

The tutorial is **not authored in Session 6** because the template
doesn't exist yet — authoring the tutorial against a hypothetical
template is wasted effort.

### 5.6 Production-default-cargo-cult guardrails (A16)

`dabbler-app`'s `appsettings.json` block markers include explicit
`// PRODUCTION: replace before deploy` comments around every dev-only
setting. The template README has a "Before you deploy" section
enumerating each setting and its production replacement:

- Magic-link secret (must rotate)
- Email sender (must switch from console output to SMTP)
- JSON file store path (consider switching to EF Core for multi-instance)
- DevIdp / OIDC (must wire real IdP)
- Allowed origins / CORS
- Logging level

### 5.7 CLI consumer compatibility

Per proposal §5.4, the template's generated code uses the platform's
primitives directly (`DeclarativeFilterPanel<TEntity>` +
`MasterDetailExpansionShell`) rather than custom abstractions. A future
Access metadata-harvesting CLI scaffolds on top of `dabbler-app` (or
`dabbler-min`) with its own code-gen layer; `security-config.json`
lives at the app root for non-source-modifying edits;
`Program.cs` Security registration is region-commented for CLI insert
points.

---

## 6. Consumer-install validation (acceptance criteria, frozen)

### 6.1 Smoke depth (A2)

The `tests/Dabbler.Packaging.Smoke/` project runs this sequence as a
CI gate, in Session 7 / Session 8:

1. Pack every Dabbler.* library + the template package into a local
   feed at `pack-output/`.
2. `dotnet new install ./pack-output/Dabbler.Templates.<version>.nupkg`.
3. In a temp directory:
   `dotnet new dabbler-app -o SmokeApp` with `nuget.config` pointing at
   the local feed.
4. `dotnet restore && dotnet build` — must succeed.
5. Start the scaffolded app on a free port; wait for readiness.
6. **Playwright smoke (expanded per A2):**
   - (a) Sign in via magic link as `admin@local.test`.
   - (b) Reach a **secured** query page (not just `/`).
   - (c) Render the sample report page.
   - (d) Exercise PDF export and assert non-empty PDF bytes.
   - (e) Run the scaffolded bUnit project against the template's
         component tests; assert all pass.
7. Tear down.
8. Repeat with `dabbler-min`: build + render-home-page + sign-in +
   reach **one secured page** (not just `/`).

Both templates passing this smoke is the gate for `dotnet nuget push`.

### 6.2 Artifact-content assertions (A10)

`tests/Dabbler.PublicSurface.Tests/` asserts public API surface diff
against a checked-in baseline AND asserts each produced `.nupkg`
contains:

- `README.md` at package root.
- `icon.png` at package root.
- `<PackageLicenseExpression>` metadata (MIT for libraries;
  `THIRD-PARTY-NOTICES.txt` for Reports.* per A7).
- SourceLink metadata resolvable to the published commit
  (`<RepositoryUrl>` + `<RepositoryCommit>` present;
  `<PublishRepositoryUrl>` honored).
- Dependency groups matching `<TargetFramework>` — no empty or
  duplicated groups.
- Static-web-asset completeness for `.Razor`-SDK packages (Components,
  Reports.Blazor, Security.Blazor): every `.razor` compile output and
  `wwwroot/` asset present in the package's static-web-assets manifest.

Diff-against-baseline failures block CI; updating the baseline is a
deliberate `git add` step that surfaces the decision in PR review.

### 6.3 Hermetic pack pipeline (A1)

`tools/pack.ps1` (Session 7 deliverable) MUST:

- Isolate `NUGET_PACKAGES` to a per-build scratch directory so the
  pack run does not benefit from (or contaminate) the developer's
  global NuGet cache.
- **Scope the hermetic restriction to the explicit allowlist of
  package IDs this repo produces.** External third-party packages
  (MudBlazor, bUnit, Playwright, EF Core, SourceLink, etc.) and
  sibling-repo-produced packages with `Dabbler.` prefix (notably
  `Dabbler.Uat.Dsl.*` from the W0-sibling repo) restore normally
  from their approved feeds. The hermetic intent of A1 is to
  prevent a **dabbler-platform-produced** package from silently
  resolving against nuget.org's published copy during a pack run —
  it is NOT a mandate to mirror every external dependency, nor a
  glob over the `Dabbler.*` namespace (which would over-include
  sibling-repo packages).
- The **canonical in-repo package allowlist** (kept in
  `tools/pack.ps1` and the hermetic nuget.config; matches ledger §3.1
  rows 1-12 + Templates + DevIdp.Tool):
  ```
  Dabbler.Components
  Dabbler.Components.Compat
  Dabbler.Platform.Stubs
  Dabbler.Platform.Stubs.Components
  Dabbler.Platform.Stubs.Reports
  Dabbler.Platform.Stubs.Security
  Dabbler.Reports.Core
  Dabbler.Reports.Export
  Dabbler.Reports.Blazor
  Dabbler.Security
  Dabbler.Security.Blazor
  Dabbler.Security.DevIdp.Tool
  Dabbler.Testing.Blazor
  Dabbler.Templates
  ```
- Implement the allowlist-only hermeticity via NuGet **source
  mapping** (`packageSourceMapping` in `nuget.config`) that pins
  each ID in the allowlist above to the local pack-output feed
  exclusively. All other package IDs (including
  `Dabbler.Uat.Dsl.*`) map to their normal approved feeds.
- Use a pre-pack check (e.g., a custom script that scans the
  resolved-packages graph and asserts every package ID in the
  allowlist resolved from the local feed) to guarantee no
  in-repo-Dabbler contamination from nuget.org or a stale global
  cache.

### 6.4 Rollback model (A9, restated)

The release pipeline is **staged**: publish first to a staging feed
(e.g., GitHub Packages or a local file feed), validate install from
there (§6.1 smoke against the staging feed), then push to nuget.org in
dependency order. Components first; Security/Reports.Core/Stubs.Components
next; Security.Blazor/Reports.Export/Stubs.Reports/Stubs.Security next;
Reports.Blazor / Components.Compat / Stubs meta-package next;
Testing.Blazor last; finally Templates + DevIdp.Tool.

Rollback paths:

- **Bad public version.** Unlist on nuget.org (do NOT rely on delete —
  nuget.org is not transactional and feed caches may have already
  resolved). Publish a fixed `0.x.y-preview.N+1`.
- **Bad template.** Templates are forward-installed — adopters who
  already scaffolded an app are unaffected (their scaffolded app
  references the published Dabbler.* package versions, not the
  template's source). Publish a fixed template version.
- **Partial publish.** Unlist the partially-published versions; fix
  the cause; re-pack with the next preview version; re-push the full
  set.

### 6.5 Pre-push checklist (Session 7 deliverable)

Before any `dotnet nuget push`:

- [ ] All non-Playwright tests green against the ProjectReference path.
- [ ] All Playwright tests green against the ProjectReference path.
- [ ] `tools/pack.ps1 -Version <next>` runs clean (every library +
      Compat + Stubs sub-packages + meta-package + Templates +
      DevIdp.Tool pack).
- [ ] `Dabbler.Packaging.Smoke` passes (expanded sequence per §6.1).
- [ ] `Dabbler.PublicSurface.Tests` clean (no unexplained baseline
      diff; artifact-content assertions pass).
- [ ] `change-log.md` entry exists for the new version (canonical
      filename used across all session-set deliverables).

Session 7 lands this checklist as `docs/packaging/release-checklist.md`.

---

## 7. Documentation deliverables

### 7.1 What lands in Session 6 (this session)

The documents authored or updated in this session:

| Document | Type | Provenance |
|----------|------|-----------|
| `packaging-readiness-ledger.md` (this file) | New | Spec Session 6 deliverable |
| `docs/packaging/scenarios.md` | New | A8 |
| `docs/packaging/retrofit-existing-app.md` | New | A14 |
| `docs/COMPATIBILITY.md` (Dabbler.* section appended) | Update | A18 |
| `docs/reports/rdf-schema.md` | New | G9 |
| `docs/platform-overview.md` (G* updates batched) | Update | G5, G6 cross-link, G8, G10, G12, G13, G17, G19, G21, G22, G23 |
| `src/Libs/Dabbler.Components/README.md` (Quick Start subordination + structural standardization) | Update | G3, A11 |
| `src/Libs/Dabbler.Components.Compat/README.md` | New (placeholder scaffold) | A4 |
| `src/Libs/Dabbler.Platform.Stubs/README.md` (Quick Start subordination + structural standardization) | Update | G3, A11 |
| `src/Libs/Dabbler.Reports.Core/README.md` (Quick Start subordination + structural standardization) | Update | G3, A11 |
| `src/Libs/Dabbler.Reports.Export/README.md` (Quick Start subordination + structural standardization) | Update | G3, A11 |
| `src/Libs/Dabbler.Reports.Blazor/README.md` (Quick Start subordination + structural standardization) | Update | G3, A11 |
| `src/Libs/Dabbler.Security/README.md` (Quick Start subordination + G6/G7 taxonomy + structural standardization) | Update | G3, G6, G7, A11 |
| `src/Libs/Dabbler.Security.Blazor/README.md` (Quick Start subordination + structural standardization) | Update | G3, A11 |
| `src/Libs/Dabbler.Testing.Blazor/README.md` (Quick Start subordination + structural standardization) | Update | G3, A11 |
| `Directory.Build.props` (package metadata defaults per proposal §3.1) | Update | Foundation for Session 7 |
| `src/Libs/Dabbler.Components/Dabbler.Components.csproj` (description tighten + ./../icon.png path fix already correct, verify) | Update | Proposal §3.6 + §3.2 verification |
| `src/Libs/Dabbler.Security/Dabbler.Security.csproj` (add full package metadata) | Update | Proposal §3.5 |
| `src/Libs/Dabbler.Security.Blazor/Dabbler.Security.Blazor.csproj` (add full package metadata) | Update | Proposal §3.5 |
| `src/icon.png` | New (128×128 PNG placeholder) | Proposal §3.2 |

### 7.2 What stays in Session 7 (package validation)

- `tools/pack.ps1` (hermetic) — A1
- `tests/Dabbler.Packaging.Smoke/` (expanded smoke) — A2
- `Dabbler.Platform.Stubs.{Components,Reports,Security}` projects + meta-package restructure — A3
- `THIRD-PARTY-NOTICES.txt` in Reports.* packages — A7
- Optional metapackages decision (`Dabbler.Reports`, `Dabbler.Security.All`, `Dabbler.Platform`) — A8 sub-track
- `tests/Dabbler.PublicSurface.Tests/` (artifact-content assertions) — A10 implementation
- Compat surface content move (the actual file moves; csproj scaffolds in Session 6) — A4 follow-on
- `docs/packaging/release-checklist.md` — §6.5

### 7.3 What stays in Session 8 (template + DevIdp tool)

- `templates/Dabbler.Templates/` template project — full deliverable
- `tools/Dabbler.Security.DevIdp.Tool/` `dotnet tool` package — A6
- Template seeded admin + sample data — A12 + A13
- `docs/getting-started.md` first-app tutorial — G11

### 7.4 Platform-overview.md G* updates (Session 6 batched)

The overview update batches the following changes into one routed
documentation pass at Step "Route platform-overview.md updates":

- **G5** — Add a side-by-side production-vs-stub `Program.cs` snippet
  to the existing Stub Mode section. Make it explicit that swapping is
  a registration-time decision.
- **G8** — Add a "Security posture and defaults" subsection spelling out:
  default-permissive user-type roles, `FailOpen` for unregistered
  resources, baked-in role bypass of `DefaultMask`. Defaults do NOT
  change; documentation only.
- **G10** — Add an "Organization / multi-tenancy" subsection: where
  `OrganizationId` is persisted, how row-scoping is applied, how
  role-switching interacts with org membership.
- **G12** — Elevate the UAT DSL pipeline (W0 / W2) to its own
  subsection, not buried in testing-hierarchy. Elevate
  `Dabbler.Security.Blazor`'s reusable admin surface (dashboard,
  navigation, role switching, restricted field/grid renderers).
  Elevate `Dabbler.Testing.Blazor`'s watermark with a "why this matters
  for Blazor Server adopters" explainer.
- **G13** — Split the existing "Where to go next" section into two
  sub-sections: "For external adopters" (per-library deep-dives, UAT
  DSL → Playwright pipeline, compatibility matrix, license, scenarios,
  retrofit guide) and "For contributors / repo maintainers" (workflow
  doc, governance two-pager, AI router, lessons-learned).
- **G17** — Add a "Governance and contact" pointer subsection under
  the contributor side of "Where to go next": where to file issues,
  how to contact maintainers, contribution policy.
- **G19** — Tighten the Playwright synchronization paragraph: clarify
  the watermark is opt-in instrumentation (consumers inherit
  `TrackedComponentBase` and call `TrackAction`), animations are
  excluded from the quiet contract.
- **G21** — Add a "Stack and audience" one-paragraph callout near the
  top so adopters can self-disqualify in 60 seconds: Blazor Server-only,
  MudBlazor-coupled, .NET 10 current LTS, SQL Server-centric security
  persistence.
- **G22** — Add bullets to the Authorization-model section explicitly
  flagging: no role inheritance; baked-in role privileges are
  compiled-in and cannot be customized at deployment.
- **G23** — Add a one-sentence clarification to the "REST-only for
  business data" principle: the rule applies to **business** data
  access; platform-internal stores (security config, audit log)
  legitimately need host-startup persistence via `options.UseEfCoreStore()`
  or equivalent.

### 7.5 Per-library README structure (A11 + G3)

Every packaged library's README follows this seven-section structure
after Session 6:

1. **Audience** — who is this library for? Which Dabbler personas
   (external adopter, integrated-source user, library contributor)?
2. **Prerequisites and stack constraints** — Blazor Server (where
   applicable), MudBlazor 8.x, .NET 10, SQL Server (where applicable),
   any library prerequisites within the Dabbler family.
3. **Install** — until preview ships: "Reference `src/Libs/Dabbler.<Module>/`
   from your project; published NuGet packages forthcoming." After
   preview ships: `dotnet add package Dabbler.<Module> --prerelease`.
   (G3 lands the pre-publish phrasing in this session.)
4. **Minimal registration snippet** — the `Program.cs` lines to wire
   the library's services into DI.
5. **Smallest working example** — one canonical usage scenario showing
   the library's primary primitive in action.
6. **Related packages** — pointers to sibling Dabbler.* libraries that
   compose with this one.
7. **Warnings / caveats** — license disclosures (Reports.* DabblerPdf),
   stub-vs-production posture (Stubs), deprecation status (Components.Compat),
   any non-obvious operational behavior.

The two Security READMEs additionally include:

- **Security** README: G6 `RegisterPages/Entities/Endpoints/Controls`
  taxonomy explanation + G7 end-to-end enforcement worked example
  (row restriction in EF query → API enforcement → role-switcher
  effect on effective authz). Lands as part of the "Smallest working
  example" + a new "Advanced: composing the four registration
  primitives" subsection.

The `Dabbler.Reports.Core` README additionally includes a pointer to
the new `docs/reports/rdf-schema.md` (G9, §7.6).

### 7.6 `docs/reports/rdf-schema.md` (G9)

A new doc covering the report-definition (RDF) format schema:

- Top-level structure (report, page, header, footer, group bands,
  detail, subreports).
- Element types and their property contracts (text, image, line,
  rectangle, aggregate, subreport).
- Aggregate / running-total / grouping / `PrintWhen` / `CanGrow`
  semantics.
- One worked example: a single-page RDF that demonstrates banded
  layout, a grouped column with running totals, and a footer
  aggregate.

Full RDF authoring tooling (designer, sample library) stays deferred
per Session 4 G9 disposition ("full authoring tooling stays deferred").

### 7.7 `docs/packaging/scenarios.md` (A8)

The scenarios matrix maps common adoption shapes to package sets:

- "Just CRUD helpers" — Components only.
- "Full platform (typical app)" — Components + Security + Security.Blazor +
  Reports.* + Testing.Blazor + (optionally) Stubs meta.
- "Reports only" — Reports.Core + Reports.Export + Reports.Blazor +
  Components (transitive dep).
- "Security only (API consumer)" — Security alone (no Blazor).
- "Security only (Blazor app, no business CRUD)" — Security + Security.Blazor +
  Components.
- "Testing utility only" — Testing.Blazor alone.
- "Retrofit existing Blazor Server app" — pointer to
  `retrofit-existing-app.md` (A14).
- "bUnit-only test wiring" — Stubs.Components.

Each scenario lists the exact `<PackageReference>` block, the
registration snippet, and gotchas.

### 7.8 `docs/packaging/retrofit-existing-app.md` (A14)

The retrofit recipe for an existing Blazor Server app adopting
Dabbler.* incrementally:

- Step 1: Add Components for the CRUD helpers.
- Step 2: Add Security + Security.Blazor for authorization (preserving
  the existing auth pipeline; Dabbler.Security as the inner authz
  policy, not the outer auth scheme).
- Step 3: Add Reports.* if reporting is in scope.
- Step 4: Add Testing.Blazor for Playwright watermark.
- Step 5: Add Stubs sub-packages for bUnit-only tests.

Each step includes a registration-order matrix, common pitfalls (e.g.,
existing `AddAuthentication` configurations), and a verification
checklist.

### 7.9 `docs/COMPATIBILITY.md` Dabbler.* section (A18)

A new table listing each Dabbler.* library version against the
MudBlazor / W0 toolchain / .NET version known to work together.
Initial entries cover the current 0.x development line; the table
grows at every release.

---

## 8. Intra-set parallelization opportunities

The spec §"Parallelization Notes" already states:

> Sessions 4 (value review) and 5 (packaging-approach articulation +
> review) are independent and could theoretically run in parallel
> worktrees. In practice, run them sequentially — the value review's
> gap list can usefully inform what the packaging approach must surface.
>
> Once the packable surface and template contract are frozen
> (Session 6), NuGet smoke proof (Session 7) and template wiring
> (Session 8) can proceed in parallel across disjoint paths.

This ledger refines the post-Session-6 parallelization map:

### 8.1 Sessions 7 and 8 — parallel-safe

| Session | Path scope (worktree) | Disjoint? |
|---------|------------------------|-----------|
| 7 (package validation, smoke) | `tools/pack.ps1`, `tests/Dabbler.Packaging.Smoke/`, `tests/Dabbler.PublicSurface.Tests/`, `src/Libs/Dabbler.Components.Compat/` (content move), `src/Libs/Dabbler.Platform.Stubs*` (sub-package split), `Directory.Build.props`, `THIRD-PARTY-NOTICES.txt` in Reports.* | Yes — touches packaging/smoke infra + library project structure |
| 8 (VS template + DevIdp tool) | `templates/Dabbler.Templates/`, `tools/Dabbler.Security.DevIdp.Tool/`, `docs/getting-started.md` | Yes — touches template + tool projects |

Conflict risk: both sessions touch `Directory.Packages.props` and
potentially `Dabbler.Platform.sln`. The session-set's parallel-worktree
trigger phrase handles the merge-back to main; expect one router-
metrics reconciliation commit per parallel branch (already documented
in `docs/ai-led-session-workflow.md`).

**Recommended parallelization shape:** run Session 7 first, then start
Session 8 in a worktree as soon as Session 7's csproj-restructure work
is committed (so Session 8 builds against the new Stubs sub-package
shape).

### 8.2 Session 9 — depends on both 7 and 8

`Session 9` (consumer validation + template smoke) requires both
Session 7's smoke infrastructure AND Session 8's template content.
**Not parallelizable.**

### 8.3 Session 10 — sequential after 9

`Session 10` (Playwright E2E for packaging features) needs Session 9's
consumer validation paths in place to know what to test.

### 8.4 Session 11 — sequential after 10

`Session 11` (human UAT checklist + closeout) is the final session and
runs strictly after Session 10's Playwright work passes.

### 8.5 Conservative reading

If parallelization risks merge contention, the safe sequence is
**7 → 8 → 9 → 10 → 11** in the current working tree. The parallel
shape (`7 || 8 → 9 → 10 → 11`) saves wall time but adds a router-
metrics merge per parallel branch.

---

## 9. Mechanical fixes applied in this session

### 9.1 Compat package scaffold

A new `src/Libs/Dabbler.Components.Compat/` project directory is
created in this session with:

- `Dabbler.Components.Compat.csproj` (Razor SDK, references
  `Dabbler.Components`, full package metadata for NuGet emission).
- `README.md` (placeholder; full content lands in Session 7 with the
  content move).

The actual file moves (the compat shell / nav / workspace / menu /
catalog razor files) happen in Session 7 — Session 6 lands only the
empty project scaffolding so the Session 7 move is mechanical.

### 9.2 Security and Security.Blazor csproj metadata (proposal §3.5)

Both csprojs gain `PackageId`, `Description`, `PackageTags`,
`PackageReadmeFile`, `PackageIcon`, `PackAsDependency`-conditional
swap arms (consistent with the other six packable libraries' shape),
and the `<None Include="README.md"…>` / `<None Include="..\..\icon.png"…>` pack items.

### 9.3 Components description tighten (proposal §3.6)

The `<Description>` field of `Dabbler.Components.csproj` updates from
the stale "FilterGrid, ComboBox, Navigation Shell, composite pages,
metadata-driven configuration. Migrated from Microsoft Access." to:

> "MudBlazor-native CRUD primitives for Blazor Server: declarative QBE
> filter panel, dirty-tracking edit session, master/detail expansion
> shell, and the underlying expression-engine + data-provider
> abstractions. Composition-over-configuration: pages declare their
> own MudDataGrid columns and MudForm fields."

The `<PackageTags>` field tightens similarly to drop retired terms
(`filtergrid`, `combobox`, `navigation`, `access-migration`) and add
current ones (`mudblazor`, `qbe`, `edit-session`, `master-detail`).

### 9.4 `Directory.Build.props` package metadata foundation (proposal §3.1)

`Directory.Build.props` extends with:

- Common package-identity properties (`Authors`, `Company`, `Copyright`,
  `RepositoryUrl`, `RepositoryType`, `PackageProjectUrl`).
- `<PackageLicenseExpression>MIT</PackageLicenseExpression>`.
- Reproducible-build / SourceLink properties (`PublishRepositoryUrl`,
  `EmbedUntrackedSources`, `IncludeSymbols`, `SymbolPackageFormat`,
  `ContinuousIntegrationBuild`).
- The `$(DabblerVersion)` MSBuild property (default `0.1.0-preview.1`,
  overridable via `/p:DabblerVersion=…`).
- The `Microsoft.SourceLink.GitHub` package reference conditional on
  `IsPackable`.

Per-library csprojs keep only their distinctive metadata (`PackageId`,
`Description`, `PackageTags`, `PackageReadmeFile`).

### 9.5 `src/icon.png` placeholder

A 128×128 PNG is committed at `src/icon.png` so the existing
`<None Include="..\..\icon.png" Pack="true" PackagePath="" />` element
in every packable csproj resolves correctly. A neutral platform icon
is fine for the placeholder; Session 7 may replace with a
brand-finalized version.

The placeholder is committed as part of this session's changes.

---

## 10. Acceptance criteria for `packaging-and-template-readiness`

The set closes when Session 9 consumer validation passes AND:

| # | Criterion | Owned by |
|---|-----------|----------|
| 1 | All 12 library/sub-package NuGet packages pack cleanly via `tools/pack.ps1`. | Session 7 |
| 2 | `Dabbler.Packaging.Smoke` (expanded sequence per §6.1) passes for both `dabbler-app` and `dabbler-min`. | Sessions 7 + 8 |
| 3 | `Dabbler.PublicSurface.Tests` (with artifact-content assertions per §6.2) passes. | Session 7 |
| 4 | The template package `Dabbler.Templates` installs and scaffolds working `dabbler-app` and `dabbler-min` apps. | Session 8 |
| 5 | `Dabbler.Security.DevIdp.Tool` installs as a `dotnet tool` and runs locally. | Session 8 |
| 6 | `docs/getting-started.md` walks a fresh adopter through `dotnet new dabbler-app → build → run → first sign-in → sample report` in under 30 minutes. | Session 8 |
| 7 | All pre-existing Playwright tests pass alongside the new packaging-feature Playwright tests. | Session 10 |
| 8 | Human UAT checklist (per `requiresUAT: true`) authored against the packaged install + scaffolded app, with W2 runner green AND the human has **executed and signed off** the checklist through the [checklist editor](https://darndestdabbler.github.io/uat-checklist-editor/) with results saved inline. Per workflow rule #9 + ledger §0 Reading guide, "authored only" is not closure — the human's sign-off is the closure event for `requiresUAT: true` + `uatScope: per-set`. | Session 11 + human-UAT execution |
| 9 | All inline `fix-during-packaging` adjustments (A1, A2, A3, A4, A6, A7, A8 doc, A10, A11, A12, A13, A14, A18) closed. | Session 11 verification |
| 10 | All Session 4 value-review gaps closed by their disposition: `fix-during-packaging` items (G3-G14, G17, G19, G23) implemented in this set; `accept-as-limitation` items (G18, G21, G22) **disposition stays "accept the constraint"**, with the Session 6 documentation acts being the *disclosure* surface for those accepted limitations — not a reclassification. The §2.2 `applied-this-session` status for G21/G22 names the documentation work, not a change in disposition. | Session 11 verification |

If criterion #1 or #2 fails at Session 7, the set pauses at Session 7
until the underlying issue is fixed — Session 8's template work
depends on a passing smoke. If criterion #3 fails, the surface diff
must be reviewed and either deliberately accepted (baseline update)
or fixed before publish gate.

If criterion #6 reads as "more than 30 minutes" in practice, the
template / docs need a Session 8 follow-up before Session 9 can claim
consumer-validation closure.

The set's commit closes with `change-log.md` aggregating all 11
sessions' work into a single consumer-facing release-note (Session
11 deliverable).

---

## 11. What this ledger does NOT do

- **It does not write the per-library README *content*.** Section 7.5
  freezes the seven-section *structure*; the content authoring happens
  separately in this session's routed work.
- **It does not commit Compat package content.** Section 9.1 scaffolds
  the project; the file moves happen in Session 7.
- **It does not implement the Stubs sub-package split.** Section 4
  freezes the contract; Session 7 implements.
- **It does not write `tools/pack.ps1` or any test project.** Sessions
  7 and 8 own those.
- **It does not author the first-app tutorial.** Session 8 owns it.
- **It does not re-litigate dispositions from Session 4 or 5.** Per
  Precedence rule, this ledger inherits and locks in.
- **It does not relax the publish gate.** First public publish is
  binding-gated on Session 7 metadata + §6.1 smoke pass, per A17.

---

## 12. Cross-references

- [Spec](./spec.md) Session 6 plan
- [Packaging Approach Proposal](./packaging-approach-proposal.md) §0-13
- [Packaging Approach Review](./packaging-approach-review.md) §0-9
- [Platform Value Review](./platform-value-review.md) §0-9
- [Spec § Session Set Configuration](./spec.md#session-set-configuration)
  (`requiresUAT: true`, `requiresE2E: true`, `uatScope: per-set`)
- `docs/ai-led-session-workflow.md` § Delegation Discipline (the
  binding rule that this ledger's authoring used the direct path, not
  `route()`, because the orchestrator already had the full Session 4 +
  Session 5 context loaded)
- `docs/planning/project-guidance.md` Principles → Architecture (LTS
  rule; informed the single `net10.0` target)
- `docs/planning/project-guidance.md` Conventions → Workflow
  Expectations (`feedback_orchestrator_self_contributes_to_review_panel`
  is the codified version of the same discipline applied here)

---

*End of ledger. Sessions 7-11 read this top-to-bottom; do not deviate
without an operator override or a new session-set spec.*
