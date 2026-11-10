# Dabbler Platform — Independent Value Review

You are reviewing the **Dabbler Platform** — a set of reusable .NET / Blazor
Server libraries — to answer a single concrete question:

> **Does this platform provide significant value for someone trying to build
> an application? What is its value proposition? What gaps would deter an
> external adopter?**

Treat this as a self-contained review. Assume you have no prior context. The
artifacts you should base your assessment on are bundled below — read them
all before answering. Do not invent capabilities the artifacts do not claim.
When you flag a gap, ground it in something specific you read.

The platform is in a transitional state: it has been validated through the
rebuild of an internal app (`AcmeCoffeeOnline`) and is now being prepared
for outside consumption (NuGet packages, a VS template). The packaging
work has **not yet shipped**; today's supported adoption model is integrated
source under `src/Libs/`. The reason this review exists is to confirm — or
deny — that what is being packaged is actually worth packaging before that
work proceeds.

## What to produce

Structure your response as:

1. **Verdict.** One sentence: does this platform provide significant value
   to someone trying to build an application? (yes / yes-with-caveats /
   no / not-enough-information). Then 2-4 sentences of reasoning.
2. **Value proposition.** What is the strongest value this platform
   delivers? Concrete capabilities, named libraries, named patterns —
   not marketing language. If you think the platform's stated narrowness
   (MudBlazor-native, Blazor Server only, composition over configuration,
   restriction-based authorization, accessibility-as-default-bar) is a
   strength, say so explicitly. If it is too narrow to be valuable, say
   that too.
3. **Gaps that would deter an external adopter.** A numbered list. For
   each gap, name:
   - the specific gap (one sentence),
   - which artifact reveals it (so the orchestrator can find it),
   - the severity (Critical / Major / Minor) where Critical = would
     render packaging premature, Major = would meaningfully reduce
     adoption, Minor = polish or future work.
4. **Disposition suggestions.** For each gap, suggest one of:
   `fix-now`, `fix-during-packaging`, `accept-as-limitation`, or
   `defer`. Brief justification per item.
5. **Anything the artifacts contradict, oversell, or undersell.** Be
   blunt. The honest critique is the point of this review.

Length: aim for 1500-3000 words. Less is OK if the gap list is short;
more is OK if you have specific, well-grounded findings. Do not pad.

## Artifacts

The remainder of this prompt is the artifacts under review.

---

### `docs/platform-overview.md` (canonical entry-point doc, 595 lines)

```markdown
# Dabbler Platform Overview

> A set of reusable .NET libraries for building secure, data-driven Blazor
> Server applications, with a deliberately narrow contract: each library
> earns its place by clearly improving consistency, supported capability,
> testability, or developer experience over what MudBlazor and ASP.NET
> Core already provide on their own.

The Dabbler Platform accelerates enterprise application development by
providing pre-built solutions for a small set of recurring problems —
runtime-configurable authorization, banded reporting with PDF export,
declarative QBE filtering, deterministic Playwright synchronization, and
opt-in stub providers for tests and demos. The platform is built on a
**MudBlazor-native** UI layer, preferring direct composition with
MudBlazor components over heavy wrapper abstractions.

This overview is the doc a future consumer should read first. After
reading it, you should understand whether the platform fits your problem,
what ships in the box, how to adopt it today, and where to go next for
hands-on detail.

If you are already inside this repo and want the operational view (build
commands, project layout, compose stack), the top-level
[`README.md`](../README.md) is the right entry point. If you are
evaluating the platform for an external project, start here.

---

## What the platform is — and what it is not

The Dabbler Platform is **a set of libraries**, not a full-stack
framework. It does not generate applications, dictate your domain model,
or replace ASP.NET Core's hosting story. It augments a Blazor Server
application with capabilities that are either (a) repeatedly reinvented
across enterprise apps, or (b) hard to get right in a way that survives
production load and audit.

The platform is also **deliberately scoped**. It is the codified result
of multiple rebuilds (`AcmeCoffeeOnline`, the integrated `Dabbler.*`
libraries) that converged on a narrower, MudBlazor-native contract after
abandoning broader earlier abstractions. Architecture decisions follow
that narrowed contract; the libraries do not silently reintroduce
historical defaults that have been retired.

What this means in practice:

- **Composition over configuration.** Pages declare their own
  `MudDataGrid` columns, `MudForm` fields, and layout. There is no
  metadata-driven UI generator that turns config objects into entire
  pages. The `composable-crud-helpers` initiative validated this stance
  across two independent cross-provider reviews — a config DSL forces
  every page into one rigid shape; small à-la-carte helpers do not.
- **Restriction-based authorization.** User-type roles are
  default-permissive and shaped entirely by explicit restrictions
  (`ColumnRestriction`, `RowRestriction`, `UiRestriction`). Five
  baked-in roles carry compiled-in privileges that handlers consult
  before any configuration lookup. Role inheritance is intentionally
  **not** supported.
- **Accessibility is a first-class bar.** WCAG 2.1 AA is the default
  accessibility target for reusable components and application pages,
  validated with automated checks before human UAT — not after.
- **The web app is not a second data layer.** All database access goes
  through the REST service layer. The web app calls the API; it does
  not quietly grow its own DbContext.

What the platform is **not**:

- Not a CMS, not a low-code platform, not a no-code platform.
- Not a generic UI kit competing with MudBlazor — it consumes MudBlazor
  directly and adds a small, focused layer on top.
- Not opinionated about your business domain. Bring your own entities,
  services, and validation rules.

---

## What problems does it solve?

The libraries target a small set of recurring enterprise application
needs:

- **Complex data filtering.** A declarative Query-by-Example (QBE)
  filter panel with AND/OR group logic, operator selection, presets,
  active filter chips, and a separately-factored expression engine
  that translates UI descriptors into server-side queries. Domain
  logic and UI are kept apart, so the UI can be rebuilt independently.
- **Runtime-configurable authorization.** A restriction-based model
  for column visibility, row scoping, UI element disablement,
  sensitive-field masking, and per-role mask exemptions — all
  configurable at runtime against an EF Core or JSON-file store.
- **Banded reporting and PDF export.** A reporting engine that
  consumes JSON Report Definition Files (`.rdf.json`), supports
  grouping/aggregates/running totals/subreports/conditional visibility,
  composes paginated layouts, and exports to PDF.
- **Deterministic E2E testing.** A DOM "watermark" system that signals
  to Playwright when Blazor Server has finished an async render,
  eliminating the timing-flake failure mode that otherwise plagues
  Blazor Server E2E suites.
- **Standardized CRUD patterns.** Small, composable helpers
  (`EditSession<T>`, `<DirtyAwareActionRow>`,
  `<MasterDetailExpansionShell>`) that pages wire up independently
  rather than being generated from a `MasterDetailsConfig<TEntity>`.
- **Testable application architecture.** A supported, opt-in stubbing
  story for dependency injection that simplifies bUnit tests, demos,
  and the initial development of generated applications — without
  becoming a default production behavior.

If your application has none of these needs, you do not need the
platform. If it has one or two, the relevant libraries are designed
to be adopted in isolation rather than as a single monolithic stack.

---

## Platform libraries

The platform ships eight consumable libraries plus one development
helper. They are designed to compose; you can take only the ones you
need.

### 1. `Dabbler.Components` — UI primitives and filter infrastructure

Reusable filtering, expression evaluation, and data-provider
abstractions for MudBlazor-native pages. Application pages use
MudBlazor components directly (MudDataGrid, MudForm, etc.) with
explicit page composition; `Dabbler.Components` adds higher-level
primitives that pages compose à la carte.

**CRUD master/details composition** (the active core for new pages):

| Surface | Role |
|---------|------|
| `DeclarativeFilterPanel<TEntity>` | Catalog-friendly QBE filter panel with AND/OR group logic, operator selection, presets, and active filter chips. The current default for new query pages. |
| `EditSession<T>` | Dirty-tracking snapshot for a detail-page record (NotifyChanged / Commit / CommitFrom / Cancel). Pairs with `<DirtyAwareActionRow>`. |
| `<DirtyAwareActionRow>` | Save / Cancel / Delete action row that reads `EditSession<T>` state, with a `CustomActions` slot for page-specific buttons. |
| `<MasterDetailExpansionShell>` | Generic master-detail composite (Add-Only, Search+Update, Pick-or-Add, M:N-with-payload) selected via capability parameters (`ShowGrid`, `ShowLinkData`, `TargetEditability`). |
| `FilterPanel<T>` | Original MudBlazor-native QBE filter panel; retained for legacy callers. New pages prefer `DeclarativeFilterPanel<TEntity>`. |

**Filter and data-provider primitives** (used by the helpers above and
available directly when a page needs finer control):

| Surface | Role |
|---------|------|
| `FilteringCoordinator` | Manages filter state, group lifecycle, and change notification. |
| `FilterColumnDefinition` | Defines filterable columns with types, operators, and editor metadata. |
| `FilterGroupCollection` / `FilterGroup` | AND/OR group model for composable filter expressions. |
| `CompositeFilterDescriptor` / `FilterDescriptor` | Filter expression descriptors consumed by the expression engine. |
| `FilterSummaryBuilder` | Generates human-readable filter summary text. |
| `ExpressionEngine` | Expression-based query and report support infrastructure. |
| `IGridDataProvider<T>` | Data provider abstraction with `ToServerDataFunc` for MudDataGrid `ServerData` callbacks. |

A typical query page wires these together explicitly:

```razor
<DeclarativeFilterPanel TEntity="Customer"
                        FilterColumns="@filterColumns"
                        Coordinator="@coordinator" />

<MudDataGrid T="Customer"
             ServerData="@dataProvider.ToServerDataFunc(coordinator.GetCompositeFilter)">
    <Columns>
        <PropertyColumn Property="x => x.Name" Title="Name" />
        <PropertyColumn Property="x => x.City" Title="City" />
    </Columns>
</MudDataGrid>
```

There is no project-wide DI extension method — components and helpers
are used directly by the pages that need them, alongside
`builder.Services.AddMudServices()`.

[Deep dive: `Dabbler.Components` README](../src/Libs/Dabbler.Components/README.md)

### 2. `Dabbler.Platform.Stubs` — opt-in stub providers for tests and demos

A dedicated library of in-memory stub implementations of the platform's
provider abstractions. **Strictly opt-in.** Not registered by default
in a production application; a consumer chooses to wire it up via DI
for one of the supported scenarios:

- **bUnit tests** that need a working grid / combo-box / report / metadata
  provider without standing up real infrastructure.
- **Stub or demo app modes** for early design review and presentations.
- **Future generated apps** that need a working UI before real providers
  exist behind it.

Shipped stub providers include `StubGridDataProvider<T>`,
`StubComboBoxItemProvider<T>`, `StubReportDataProvider`,
`StubEntityMetadataProvider`, `StubNavigationDataProvider`,
`StubArtifactCatalogProvider`, `StubUsageTrackingProvider`, and
`StubAuthenticationStateProvider`. DI helpers
(`AddDabblerPlatformStubs`, plus focused per-provider helpers) let you
take all of them or just one.

[Deep dive: `Dabbler.Platform.Stubs` README](../src/Libs/Dabbler.Platform.Stubs/README.md)

### 3. `Dabbler.Reports.Core` — banded report layout engine

The core reporting engine. Consumes JSON Report Definition Files
(`.rdf.json`) and composes paginated banded layouts. No UI; pure backend.

Supported capabilities:

- **Banded layout** — detail, group header/footer, page header/footer,
  and report header/footer bands.
- **Grouping and sorting** — multi-level grouping with aggregate
  summaries.
- **Running totals** that accumulate across detail rows or group
  boundaries.
- **Hide duplicates** to suppress repeated values within groups.
- **Subreports** embedded within parent report bands.
- **PrintWhen expressions** for conditional element visibility.
- **CanGrow / CanShrink** for dynamic element sizing based on content.
- **Named styles** for font, color, alignment, borders, and padding.
- **Expression evaluation** for field references, parameters,
  built-in functions, and aggregate calculations.

[Deep dive: `Dabbler.Reports.Core` README](../src/Libs/Dabbler.Reports.Core/README.md)

### 4. `Dabbler.Reports.Export` — PDF export

Renders composed report pages (the output of `Dabbler.Reports.Core`'s
`BandLayoutEngine`) into PDF documents using the `DabblerPdf` library.
Handles fonts, styles, images, borders, and multi-page output.

```csharp
var exporter = new PdfExporter(report)
{
    Title = "Sales Summary",
    Author = "Report System"
};
byte[] pdfBytes = exporter.Export(pages);
```

[Deep dive: `Dabbler.Reports.Export` README](../src/Libs/Dabbler.Reports.Export/README.md)

### 5. `Dabbler.Reports.Blazor` — report UI

The Blazor UI for the retained reporting workflow. The supported
contract centers on prompting for parameters, executing a report, and
rendering the composed output:

| Component | Description |
|-----------|-------------|
| `DabblerReportPage` | Full report lifecycle: load RDF, prompt parameters, execute query, compose pages, and display the result. |
| `ParameterPromptForm` | Parameter-entry surface for report runs. |
| `ReportViewer` | Render composed report pages with zoom and layout fidelity. |

PDF export support is available but treated as supporting behavior, not
the main contract story for the UI library.

[Deep dive: `Dabbler.Reports.Blazor` README](../src/Libs/Dabbler.Reports.Blazor/README.md)

### 6. `Dabbler.Security` — authentication and restriction-based authorization

A comprehensive authentication and authorization library for ASP.NET
Core and Blazor Server applications. The supported contract is broader
than most other libraries here because identity and authorization
genuinely need this breadth in enterprise scenarios.

**Authentication:**

- **Magic link** — passwordless login via email with clickable links
  or 6-digit codes.
- **TOTP MFA** — time-based One-Time Password multi-factor
  authentication (e.g., Microsoft Authenticator).
- **External identity providers** — Microsoft Entra ID (OIDC) and
  generic OAuth.
- **Recovery codes** for users who lose their MFA device.
- **Cookie-based sessions** with sliding expiration.

**Authorization model:**

- **Five baked-in roles** carry compiled-in privilege rules that
  handlers consult before any configuration lookup. Applications
  cannot alter a baked-in role's privileges; they may only set a
  per-deployment `DisplayAlias`:
  - `SystemAdmin`
  - `CrossOrgUserAdmin`
  - `CrossOrgMonitor`
  - `UserAdmin`
  - `User`
- **User-type roles** are application-defined, default-permissive,
  and shaped entirely by explicit restrictions. Configured in JSON;
  baked-in roles are not.
- **Restrictions** scope what a user-type role can see and do:
  - `ColumnRestriction` — view-only or hidden field per role.
  - `RowRestriction` — filter expression scoping data access per role.
  - `UiRestriction` — disabled or hidden page or control per role.
- **Sensitive-field masking** is modelled separately: platform-wide
  `DefaultMask` rules render values masked even when the underlying
  value is null. Per-role `MaskExemption` (with optional `ExpiresAt`
  and separation-of-duties authority rules) reveals the original
  value to authorized users. **Baked-in roles bypass DefaultMask**
  enforcement entirely.
- **No role inheritance.** A user assigned to multiple roles must
  explicitly switch to operate under each one.

**Infrastructure:**

- Flexible persistence — JSON file store for development, EF Core SQL
  Server store for production.
- SQL Server temporal-table auditing.
- Built-in rate limiting against brute-force attacks (in-memory or
  distributed cache).
- Integrated email sender (SMTP and development MailHog).

[Deep dive: `Dabbler.Security` README](../src/Libs/Dabbler.Security/README.md)

### 7. `Dabbler.Security.Blazor` — Blazor UI for security

The Blazor UI surface for the security library. The supported core
centers on secured rendering, role-aware navigation, runtime permission
editing, and the security dashboard / admin pages.

**Authentication pages:**

| Component | Route | Description |
|---|---|---|
| `LoginPage` | `/login` | Magic link login with optional external OIDC/OAuth entry points. |
| `MfaPage` | `/mfa` | TOTP setup and verification when a product policy enables MFA. |

**Admin pages:**

| Component | Route | Description |
|---|---|---|
| `UserListPage` | `/admin/users` | List and manage application users. |
| `UserEditPage` | `/admin/users/{id}` | Edit user details and metadata. |
| `RoleListPage` | `/admin/roles` | View and manage security roles. |
| `RoleAssignmentPage` | `/admin/roles/assign` | Assign and remove roles from users. |
| `AuditLogPage` | `/admin/audit` | View a chronological audit trail of security events. |

**Supported core components:**

| Component | Description |
|---|---|
| `DabblerSecured` | Wrapper that declaratively enforces page, field, and control permissions in markup. |
| `DabblerSecurityNav` | Navigation menu item for admin pages; automatically appears for authorized users. |
| `DabblerRoleSwitcher` | Dropdown for users with multiple roles to switch their active role. |
| `DabblerSecurityDashboard` | Central admin panel exposing five tabs: Role Master Details, Default Masks, Organization Master Details, Users and Role Assignment, and Role Documentation. Each tab is gated by the active baked-in role. |
| `RestrictedFormField` / `RestrictedGridColumn` | Render fields and grid columns subject to the active role's `ColumnRestriction` rules and any `DefaultMask` + `MaskExemption` resolution. |

[Deep dive: `Dabbler.Security.Blazor` README](../src/Libs/Dabbler.Security.Blazor/README.md)

### 8. `Dabbler.Testing.Blazor` — deterministic Playwright synchronization

Testing utilities focused on eliminating timing-flake failures in
Playwright E2E tests against Blazor Server applications.

Blazor Server renders asynchronously, which means a Playwright test
that clicks a button cannot rely on the next-frame DOM being settled.
`Dabbler.Testing.Blazor` solves this with an opt-in DOM watermark:

- `TrackedComponentBase` — opt-in base class. Components wrap async
  event handlers and data-loading callbacks in `TrackAction(...)`.
- A JS interop shim maintains DOM attributes
  (`data-actions-in-flight`, `data-last-action`, `data-render-version`)
  that expose when components have settled after user actions.
- `TestReadinessProbe` signals initial page-load completion via a
  `data-blazor-ready` attribute.
- Configuration is gated by `Testing:ActionTracking:Enabled` in
  `appsettings.json` — **`false` by default** so there is no
  production overhead, **`true` in `appsettings.Development.json`**.

Playwright tests wait on these attributes:

```csharp
// After clicking a button that triggers an async handler:
await Assertions.Expect(page.Locator("html"))
    .ToHaveAttributeAsync("data-actions-in-flight", "[]");
```

The watermark pattern is paired with a DSL verb
(`WAIT FOR ACTIONS QUIET`) in the W0 UAT/E2E toolchain — see the
`uat-checklist-editor` and `dabbler-uat-dsl` repositories for the
authoring side.

[Deep dive: `Dabbler.Testing.Blazor` README](../src/Libs/Dabbler.Testing.Blazor/README.md)

### Development helpers (not consumable libraries)

`Dabbler.Security.DevIdp` is a small ASP.NET Core web project — **not**
a NuGet-style library — that runs a local OIDC identity provider for
testing external-login flows during development. It is intentionally
loopback-only and secret-guarded. Use it when you want to exercise the
external-login code path without depending on a real Entra ID tenant;
do not deploy it to production.

---

## How to adopt the platform

### Today: integrated source under `src/Libs/`

The current supported adoption model is **integrated source**. The
libraries live as projects under `src/Libs/` in this repository, and
applications include them in their own solution via project references.
This is how the bundled `AcmeCoffeeOnline.Web` and
`AcmeCoffeeOnline.Api` apps consume them today.

Concretely:

1. Clone or vendor the relevant `src/Libs/<Library>/` directories into
   your repository.
2. Add `<ProjectReference>` entries from your app projects to the
   library `.csproj` files.
3. Register services in `Program.cs` as each library's README
   describes (e.g., `AddDabblerSecurity(...)`,
   `AddDabblerSecurityBlazor()`, `AddMudServices()`).
4. For tests and demos that should run against stub providers,
   reference `Dabbler.Platform.Stubs` and call
   `AddDabblerPlatformStubs(...)`.

The integrated-source model is deliberate: it allows rapid, coordinated
development across the libraries and consuming applications while the
contract continues to harden. Project-guidance principles explicitly
direct new work to use the integrated source path rather than
sibling-repo coupling.

### Future: NuGet packages

Publishing the libraries as versioned NuGet packages, plus a Visual
Studio template that scaffolds a new consuming app, is **planned work**
— it is the explicit deliverable of the
`packaging-and-template-readiness` session set this overview was
authored under. **It has not shipped yet.** When it does, this section
will be updated with the package names, versioning policy, and the
template scaffold story. Until then, integrated source is the only
supported model.

The packaging work is intentionally cautious: it must not silently
reintroduce historical defaults that the rebuild retired (broader
abstractions, metadata-driven UI generation, role inheritance, etc.).
Future generated apps and the Access metadata-harvesting CLI are
important downstream consumers, but neither becomes the design center
— the packaged contract has to remain generic across consumers.

---

## Opt-in stub mode

`Dabbler.Platform.Stubs` is a **supported platform feature, not a test
trick**. The intent is that bUnit tests, stub/demo hosts, early design
review, and future generated apps can all use the same DI-toggled
behavior to swap real providers for in-memory ones. Done right, the
same component code runs in both modes without conditional branching
in the component itself.

The contract is strictly opt-in:

- A production application registers real providers; stubs are absent.
- A test or demo host explicitly calls `AddDabblerPlatformStubs(...)`
  and the focused helpers it needs.
- The application never silently falls back to stub behavior at
  runtime — opt-in is opt-in.

Example wiring in a test or demo `Program.cs`:

```csharp
var definition = new StubPlatformDefinition
{
    Entities =
    [
        new EntityMetadata
        {
            EntityName = "Customer",
            DisplayName = "Customers",
            DisplayNameSingular = "Customer",
            ShowInNavigation = true
        }
    ]
};
definition.Navigation.AddRecords(
    "Customer",
    "Recent",
    [new NavigationRecord(101, "Ada Lovelace")]);

services.AddDabblerPlatformStubs(definition);
services.AddStubGridDataProvider(
    [
        new CustomerRow(101, "Ada Lovelace"),
        new CustomerRow(102, "Grace Hopper")
    ]);
services.AddStubAuthentication(new StubUserContext
{
    UserId = "stub-user",
    DisplayName = "Stub User",
    Email = "stub@example.test",
    Roles = ["BackofficeUser"],
    OrganizationId = "org-demo"
});
```

Use the focused per-provider helpers when you only want one stubbed
concern; use `AddDabblerPlatformStubs(...)` when you want a host that
swaps to stub metadata/navigation/catalog services as a whole.

---

## Accessibility posture

The platform targets **WCAG 2.1 Level AA** for reusable components and
application pages. This is the **default bar**, not an aspiration: the
testing hierarchy includes automated checks that assert on visible
focus, selected-state contrast, readable text against backgrounds,
layout overlap, and other WCAG-adjacent presentation risks before
human UAT is invoked.

Concrete consequences:

- Reusable components have interaction-contract coverage, not just
  markup-presence coverage. A button, chip, toggle, menu, or calendar
  icon that exists for user interaction is tested by exercising the
  interaction, not by asserting that the element rendered.
- Styling defects that materially affect usability are caught
  automatically when practical; the goal is that human UAT reviews
  judgment (clarity, coherence, trust, visual quality), not
  first-pass operability defects.
- Theme and styling changes go through reusable, testable guardrails
  so foreground/background combinations, selected states, and
  dynamic color changes remain readable and accessible.

The testing hierarchy itself (strict unit → integration → bUnit → true
E2E → human UAT) is described in the project guidance — see *Where to
go next* below.

---

## Testing hierarchy at a glance

The platform's testing posture matters because the libraries themselves
were validated under it, and because the same hierarchy is the right
default for applications consuming the platform:

1. **Strict unit tests** for logic that can be isolated from I/O.
2. **Integration tests** for server-side and service-boundary
   behavior — API, persistence, security boundaries.
3. **bUnit tests** for UI-seam proof. `Dabbler.Platform.Stubs` is
   designed to make these simpler and more representative of the
   supported platform shape.
4. **True E2E tests** (Playwright, against the host-mode compose
   stack) for real browser and data proof.
   `Dabbler.Testing.Blazor`'s watermark eliminates the timing-flake
   failure mode that otherwise dominates Blazor Server E2E suites.
5. **Human UAT** for confidence and judgment — clarity, visual
   quality, trust. Not a first-pass operability gate.

Step N of a human UAT checklist corresponds to Step N of a
Playwright test driven by the same UAT DSL — under the DSL pipeline,
the human checklist and the Playwright test are compiled from the
same source, so drift is structurally impossible.

---

## Where to go next

This overview is the entry point. Once you know which library or
concern you care about, follow these onward links:

**Repo orientation:**

- [Top-level `README.md`](../README.md) — repo layout, build commands,
  Docker compose stack.
- [`src/Libs/README.md`](../src/Libs/README.md) — the integrated
  library home index.

**Workflow and governance:**

- [`docs/ai-led-session-workflow.md`](./ai-led-session-workflow.md) —
  how the platform itself is built using an AI-led, session-based
  workflow (orchestrator + AI router + cross-provider verification).
  This is the single source of truth for the workflow.
- [`docs/COMPATIBILITY.md`](./COMPATIBILITY.md) — the W0 toolchain ↔
  component catalog ↔ W2 runner compatibility matrix.
- [`docs/planning/governance-two-pager.md`](./planning/governance-two-pager.md) —
  governance posture for platform changes and reviews.

**Per-library deep dives:**

- [`Dabbler.Components`](../src/Libs/Dabbler.Components/README.md) —
  filter UI, expression engine, CRUD helpers, data-provider
  abstractions.
- [`Dabbler.Platform.Stubs`](../src/Libs/Dabbler.Platform.Stubs/README.md) —
  opt-in stub providers for tests, demos, and future generated apps.
- [`Dabbler.Reports.Core`](../src/Libs/Dabbler.Reports.Core/README.md) —
  banded report engine, RDF format, expression evaluation.
- [`Dabbler.Reports.Export`](../src/Libs/Dabbler.Reports.Export/README.md) —
  PDF export.
- [`Dabbler.Reports.Blazor`](../src/Libs/Dabbler.Reports.Blazor/README.md) —
  report UI (parameter prompt, viewer).
- [`Dabbler.Security`](../src/Libs/Dabbler.Security/README.md) —
  authentication, authorization, restrictions, masking.
- [`Dabbler.Security.Blazor`](../src/Libs/Dabbler.Security.Blazor/README.md) —
  Blazor UI for security (login, admin pages, dashboard, secured
  rendering).
- [`Dabbler.Testing.Blazor`](../src/Libs/Dabbler.Testing.Blazor/README.md) —
  Playwright synchronization watermark, bUnit helpers.

```

---

### `README.md` (repo-root orientation doc)

```markdown
# Dabbler Platform

This repository is the integrated development home for the AcmeCoffeeOnline
rebuild, the required Dabbler libraries, and the AI-led workflow assets used
to run session-set development from one place.

## Current foundation

The repo currently includes:

- the rebuilt AcmeCoffeeOnline Web, API, and Shared projects
- a **MudBlazor-native UI layer** using MudBlazor components directly for
  layout, navigation, grids, forms, dialogs, and theming — replacing the
  prior custom wrapper abstractions
- consolidated `Dabbler.Security` and `Dabbler.Security.Blazor` source projects
- integrated `Dabbler.Components` library retaining the QBE filter model,
  expression engine, and data provider abstractions while delegating UI
  rendering to MudBlazor
- Dabbler Reports source projects under `src/Libs/`
- route-based navigation with `MudLayout`, `MudAppBar`, `MudDrawer`, and
  `MudNavMenu` replacing the prior shell/workspace/tab model
- explicit page composition: each page declares its own MudDataGrid columns,
  MudForm fields, and layout rather than generating UI from metadata
- central build and package management
- Docker Compose for web, api, sql server, MailHog, and Playwright
- xUnit, bUnit, and Playwright coverage including conformance tests, filter
  panel interaction tests, accessibility assertions, and browser workflows
- repo-root AI workflow assets: `requirements.txt`, `ai_router/`,
  `uat_runner/`, `AGENTS.md`, `CLAUDE.md`, and `GEMINI.md`
- durable cutover/guardrail docs

**New here?** Start with
[`docs/platform-overview.md`](docs/platform-overview.md) — the
canonical entry-point doc describing what the platform is, the
libraries it ships, and how to adopt them.

The single source of truth for the AI-led workflow is
[`docs/ai-led-session-workflow.md`](docs/ai-led-session-workflow.md)
(execution mechanics; it links onward to the spec-authoring guide).
Per-library READMEs live under [`src/Libs/`](src/Libs/); the W0
toolchain ↔ catalog ↔ W2 runner compatibility matrix lives at
[`docs/COMPATIBILITY.md`](docs/COMPATIBILITY.md).

## Project layout

- `src/Apps/AcmeCoffeeOnline.Web/` — rebuilt Blazor Server host with
  MudBlazor-native pages and layout
- `src/Apps/AcmeCoffeeOnline.Api/` — rebuilt REST API host
- `src/Apps/AcmeCoffeeOnline.Shared/` — shared DTOs, enums, and models
- `src/Libs/` — integrated Dabbler libraries (Components, Security, Reports)
- `tests/` — xUnit, bUnit, and Playwright coverage
- `docker/` — compose and container definitions
- `ai_router/` — operator-tuned `router-config.yaml` and the
  append-only `router-metrics.jsonl` cost telemetry. The router
  itself is `pip install dabbler-ai-router` (declared in
  `requirements.txt`); the canonical source lives at
  [darndestdabbler/dabbler-ai-orchestration](https://github.com/darndestdabbler/dabbler-ai-orchestration)
  and the canonical extension's **Dabbler: Install ai-router**
  command handles the install for new clones.
- `uat_runner/` — platform-specific UAT runner package (CLI, triage,
  remediations, test cases). Calls into the canonical router via
  `import ai_router`.
- `docs/` — workflow docs and session-set artifacts

## Local commands

```bash
dotnet build Dabbler.Platform.sln
dotnet test Dabbler.Platform.sln

# E2E (Playwright runs from the host against the app containers):
docker compose -f docker/docker-compose.infra.yml up -d        # sqlserver + mailhog (start once, leave up)
docker compose -f docker/docker-compose.app.yml   up --build -d # api + web (rebuild after code changes)
dotnet test tests/AcmeCoffeeOnline.Playwright/
```

The compose stack is split into two files. `docker-compose.infra.yml` runs
the persistent dev dependencies (SQL Server, MailHog, and the `dabbler-dev`
network); start it once and leave it up across sessions. `docker-compose.app.yml`
runs the rebuildable app services (api, web) with explicit `image:` tags so
rebuilds replace the tagged images instead of orphaning dangling layers.
Playwright is intentionally **not** a compose service — run it from the
host (`dotnet test` or VS Code Test Explorer) against `http://localhost:5010`.

```

---

### Per-library READMEs

### `src/Libs/Dabbler.Components/README.md`

```markdown
# Dabbler.Components

> Part of the Dabbler Platform. See
> [`docs/platform-overview.md`](../../../docs/platform-overview.md) for
> the platform-level view of how this library fits in.

Blazor UI component library providing reusable filtering, expression engine,
and data provider abstractions for MudBlazor-native application pages.

After the MudBlazor-native rebuild, application pages use MudBlazor components
directly (MudDataGrid, MudForm, MudLayout, MudDialog, etc.) with explicit page
composition rather than heavyweight wrapper abstractions.

## Active Core

### CRUD master/details composition

The library ships a small set of à-la-carte helpers that pages wire up
independently rather than being generated from a config object. Each
helper does one thing and is composed by the page itself.

| Surface | Role |
|---------|------|
| `DeclarativeFilterPanel<TEntity>` | Catalog-friendly QBE filter panel with AND/OR group logic, operator selection, presets, and active filter chips. The current default for new query pages. |
| `EditSession<T>` | Dirty-tracking snapshot for a detail-page record (NotifyChanged / Commit / CommitFrom / Cancel). Pairs with `<DirtyAwareActionRow>`. |
| `<DirtyAwareActionRow>` | Save / Cancel / Delete action row that reads `EditSession<T>` state, with a `CustomActions` slot for page-specific buttons. |
| `<MasterDetailExpansionShell>` | Generic master-detail composite (Add-Only, Search+Update, Pick-or-Add, M:N-with-payload) selected via capability parameters (`ShowGrid`, `ShowLinkData`, `TargetEditability`). |
| `FilterPanel<T>` | Original MudBlazor-native QBE filter panel; retained for legacy callers. New pages prefer `DeclarativeFilterPanel<TEntity>`. |

### Filter and data-provider primitives

| Surface | Role |
|---------|------|
| `FilteringCoordinator` | Manages filter state, group lifecycle, and change notification |
| `FilterColumnDefinition` | Defines filterable columns with types, operators, and editor metadata |
| `FilterGroupCollection` / `FilterGroup` | AND/OR group model for composable filter expressions |
| `CompositeFilterDescriptor` / `FilterDescriptor` | Filter expression descriptors consumed by the expression engine |
| `FilterSummaryBuilder` | Generates human-readable filter summary text |
| `ExpressionEngine` | Expression-based query and report support infrastructure |
| `IGridDataProvider<T>` | Data provider abstraction with `ToServerDataFunc` for MudDataGrid ServerData callbacks |

## Compatibility Surfaces

These components support the `PlatformStubDemo` scenario and are not used by
the main application, which has moved to MudBlazor-native layout and routing:

| Surface | Role |
|---------|------|
| `DabblerShell` | Left-nav + tabbed-workspace shell for stub and demo app modes |
| `DabblerNavigationPanel` | Entity/report navigation panel composed into DabblerShell |
| `DabblerTabbedWorkspace` | In-shell tab manager for search, detail, report, and dashboard content |
| `DabblerTopMenuBar` | Optional artifact-browsing menu bar when catalog/usage providers are supplied |
| `DabblerCatalogPage` | Compatibility-only artifact catalog page used by DabblerShell |

## Quick Start

Register MudBlazor services in `Program.cs` (Dabbler.Components types
have no project-wide DI extension — components and helpers are used
directly by the pages that need them):

```csharp
builder.Services.AddMudServices();
```

Use `DeclarativeFilterPanel<TEntity>` with `MudDataGrid<T>` for query pages:

```razor
<DeclarativeFilterPanel TEntity="Customer"
                        FilterColumns="@filterColumns"
                        Coordinator="@coordinator" />

<MudDataGrid T="Customer"
             ServerData="@dataProvider.ToServerDataFunc(coordinator.GetCompositeFilter)">
    <Columns>
        <PropertyColumn Property="x => x.Name" Title="Name" />
        <PropertyColumn Property="x => x.City" Title="City" />
    </Columns>
</MudDataGrid>
```

## Dependencies

- [MudBlazor](https://mudblazor.com/) 8.x
- .NET 10.0

## License

[MIT](https://licenses.nuget.org/MIT)

```

### `src/Libs/Dabbler.Platform.Stubs/README.md`

```markdown
# Dabbler.Platform.Stubs

> Part of the Dabbler Platform. See
> [`docs/platform-overview.md`](../../../docs/platform-overview.md) for
> the platform-level view of how this library fits in.

Reusable stub providers for the retained Dabbler platform surface.

This package turns the repo's existing in-memory testing patterns into a
supported DI story for:

- bUnit and direct component tests
- stub or demo app modes
- future generated apps that need a first-pass UI before real providers exist

## Supported Stub Surface

- `StubGridDataProvider<T>` for query and grid data
- `StubComboBoxItemProvider<T>` for lookup and combo-box flows
- `StubReportDataProvider` for report execution and lookup tables
- `StubEntityMetadataProvider` and `StubNavigationDataProvider` for shell and navigation flows
- `StubArtifactCatalogProvider` and `StubUsageTrackingProvider` for optional menu/catalog state
- `StubAuthenticationStateProvider` for role-aware and organization-aware test or demo hosts
- `AddDabblerPlatformStubs(...)` plus focused registration helpers for DI wiring

## Quick Start

```csharp
var definition = new StubPlatformDefinition
{
    Entities =
    [
        new EntityMetadata
        {
            EntityName = "Customer",
            DisplayName = "Customers",
            DisplayNameSingular = "Customer",
            ShowInNavigation = true,
            SmartFilters =
            [
                new SmartFilterDefinition
                {
                    Name = "Recent",
                    Category = SmartFilterCategory.Custom
                }
            ]
        }
    ]
};

definition.Navigation.AddRecords(
    "Customer",
    "Recent",
    [new NavigationRecord(101, "Ada Lovelace")]);

services.AddDabblerPlatformStubs(definition);
services.AddStubGridDataProvider(
    [
        new CustomerRow(101, "Ada Lovelace"),
        new CustomerRow(102, "Grace Hopper")
    ]);
services.AddStubAuthentication(new StubUserContext
{
    UserId = "stub-user",
    DisplayName = "Stub User",
    Email = "stub@example.test",
    Roles = ["BackofficeUser"],
    OrganizationId = "org-demo"
});
```

Use the focused helpers when you only want one stubbed concern, and use
`AddDabblerPlatformStubs(...)` when you want a host that swaps to stub
metadata/navigation/catalog services as a whole.

```

### `src/Libs/Dabbler.Reports.Core/README.md`

```markdown
# Dabbler.Reports.Core

> Part of the Dabbler Platform. See
> [`docs/platform-overview.md`](../../../docs/platform-overview.md) for
> the platform-level view of how this library fits in.

Banded report layout engine with RDF-based report definitions. Supports grouping, aggregates, running totals, hide duplicates, subreports, PrintWhen expressions, and CanGrow/CanShrink layout.

## Capabilities

- **Banded layout** &mdash; Detail, group header/footer, page header/footer, and report header/footer bands
- **Grouping and sorting** &mdash; Multi-level grouping with aggregate summaries
- **Running totals** &mdash; Accumulate values across detail rows or group boundaries
- **Hide duplicates** &mdash; Suppress repeated values within groups
- **Subreports** &mdash; Embed child reports within parent report bands
- **PrintWhen expressions** &mdash; Conditionally show/hide elements based on data
- **CanGrow / CanShrink** &mdash; Dynamic element sizing based on content
- **Styles** &mdash; Named styles with font, color, alignment, borders, and padding
- **Expression evaluation** &mdash; Field references, parameters, built-in functions, and aggregate calculations

## Quick Start

```csharp
// Parse an RDF report definition
var parseResult = RdfParser.Parse(rdfJson);
var report = parseResult.Report;

// Execute the data query
var engine = new ReportDataEngine(dataProvider);
var data = await engine.ExecuteAsync(report, parameterValues);

// Compose pages
var layoutEngine = new BandLayoutEngine(report, textMeasurer);
var pages = layoutEngine.Compose(data);
```

## RDF Format

Report definitions use a JSON-based format (`.rdf.json`) that describes bands, elements, data sources, parameters, grouping, sorting, and styles. See the repository documentation for the full RDF schema reference.

## Dependencies

- `Dabbler.Components` (expression engine)
- `DabblerPdf` (text measurement)
- .NET 10.0

## License

[MIT](https://licenses.nuget.org/MIT)

```

### `src/Libs/Dabbler.Reports.Export/README.md`

```markdown
# Dabbler.Reports.Export

> Part of the Dabbler Platform. See
> [`docs/platform-overview.md`](../../../docs/platform-overview.md) for
> the platform-level view of how this library fits in.

PDF export for Dabbler Reports. Renders composed report pages to PDF documents using the DabblerPdf library, with full support for fonts, styles, images, borders, and multi-page output.

## Quick Start

```csharp
// Compose report pages first
var layoutEngine = new BandLayoutEngine(report, textMeasurer);
var pages = layoutEngine.Compose(data);

// Export to PDF
var exporter = new PdfExporter(report)
{
    Title = "Sales Summary",
    Author = "Report System"
};

byte[] pdfBytes = exporter.Export(pages);
File.WriteAllBytes("output.pdf", pdfBytes);
```

## Dependencies

- `Dabbler.Reports.Core` (report definitions, layout engine)
- `DabblerPdf` (PDF rendering)
- .NET 10.0

## License

[MIT](https://licenses.nuget.org/MIT)

```

### `src/Libs/Dabbler.Reports.Blazor/README.md`

```markdown
# Dabbler.Reports.Blazor

> Part of the Dabbler Platform. See
> [`docs/platform-overview.md`](../../../docs/platform-overview.md) for
> the platform-level view of how this library fits in.

Blazor report UI for the retained reporting workflow. The supported contract
centers on prompting for parameters, executing a report, and rendering the
composed output.

## Supported Core

| Component | Description |
|-----------|-------------|
| `DabblerReportPage` | Full report lifecycle: load RDF, prompt parameters, execute query, compose pages, and display the result |
| `ParameterPromptForm` | Parameter-entry surface for report runs |
| `ReportViewer` | Render composed report pages with zoom and layout fidelity |

## Quick Start

```razor
<DabblerReportPage RdfPath="reports/sales-summary.rdf.json"
                   DataProvider="@dataProvider"
                   AutoRunOnLoad="true" />
```

Or embed just the viewer:

```razor
<ReportViewer Pages="@composedPages"
              Report="@reportDefinition"
              Scale="1.0" />
```

## Supported Workflow Themes

- prompt for parameters before running a report
- execute report queries against a report data provider
- view composed report output in the browser
- keep PDF/export support as supporting behavior, not the main contract story

## Dependencies

- `Dabbler.Reports.Core` (report engine)
- `Dabbler.Reports.Export` (PDF export)
- `Dabbler.Components` (base components, expression engine)
- [MudBlazor](https://mudblazor.com/) 8.x
- .NET 10.0

## License

[MIT](https://licenses.nuget.org/MIT)

```

### `src/Libs/Dabbler.Security/README.md`

```markdown
# Dabbler.Security

> Part of the Dabbler Platform. See
> [`docs/platform-overview.md`](../../../docs/platform-overview.md) for
> the platform-level view of how this library fits in.

Dabbler.Security is a comprehensive authentication and authorization library for ASP.NET Core and Blazor Server applications. It provides passwordless magic link authentication, TOTP-based MFA, external identity provider integration, and a powerful runtime-configurable authorization system for fine-grained access control.

## Quick Start

> **Adoption note (2026-05):** the supported adoption model today is
> **integrated source** under `src/Libs/` — add a `<ProjectReference>`
> from your application to `src/Libs/Dabbler.Security/`. The NuGet
> install command below is the future packaged path described in
> [`docs/platform-overview.md`](../../../docs/platform-overview.md);
> NuGet packages have not shipped yet. The packaging work is tracked
> by the `packaging-and-template-readiness` session set.

1. Install the NuGet package (future packaged path):

    ```sh
    dotnet add package Dabbler.Security
    ```

2. Configure services and map endpoints in your `Program.cs`:

    ```csharp
    // 1. Add Dabbler.Security services
    builder.Services.AddDabblerSecurity(options =>
    {
        options.ConnectionString = builder.Configuration.GetConnectionString("Default")!;
        options.MagicLinkExpiry = TimeSpan.FromMinutes(10);
        options.RequireMfa = false;
        options.Email.SenderType = EmailSenderType.DevMailHog;

        // Register your application's protected resources
        options.RegisterPages(pages => pages.Add("Dashboard", "/dashboard"));
        options.RegisterEntities(entities => entities.Add("Order"));
        options.RegisterEndpoints(endpoints => endpoints.Add("CreateOrder", "POST /api/orders"));
        options.RegisterControls(controls => controls.Add("Dashboard", "ExportButton"));
    });

    // 2. Choose a persistence backend
    // For development or single-server deployments:
    builder.Services.UseJsonFileStore("security-config.json");

    // For production or multi-server deployments:
    // builder.Services.UseEfCoreStore();

    var app = builder.Build();

    // 3. Map the authentication endpoints
    app.MapDabblerSecurityEndpoints();

    app.Run();
    ```

## Key Features

### Authentication

- **Magic Link** -- Passwordless login via email with clickable links or 6-digit codes.
- **TOTP MFA** -- Time-based One-Time Password multi-factor authentication (e.g., Microsoft Authenticator).
- **External Identity Providers** -- Supports Microsoft Entra ID (OIDC) and generic OAuth providers.
- **Recovery Codes** -- Backup codes for users who lose their MFA device.
- **Cookie-Based Sessions** -- Secure, configurable cookie authentication with sliding expiration.

### Authorization (Runtime-Configurable)

- **Five baked-in roles** -- `SystemAdmin`, `CrossOrgUserAdmin`,
  `CrossOrgMonitor`, `UserAdmin`, `User`. Privileges are compiled in;
  applications can set a per-deployment `DisplayAlias` but cannot alter
  baked-in privileges.
- **User-type roles (default-permissive, JSON-configured)** -- Additional
  application-defined roles shaped entirely by explicit restrictions.
- **`ColumnRestriction`** -- View-only or hidden field per role.
- **`RowRestriction`** -- Filter expression scoping data access per role.
- **`UiRestriction`** -- Disabled or hidden page or control per role.
- **Endpoint Permissions** -- Secure API endpoints based on role.
- **Sensitive-field masking** -- Platform-wide `DefaultMask` rules render
  sensitive values masked even when the underlying value is null. Per-role
  `MaskExemption` (with optional `ExpiresAt` and authority rules) reveals
  the original value to authorized users. Baked-in roles bypass DefaultMask.
- **No role inheritance.** A user assigned to multiple roles must
  explicitly switch to operate under each one.

### Infrastructure

- **Flexible Persistence** -- Use a JSON file store for development or an EF Core SQL Server store for production.
- **Auditing** -- SQL Server temporal table support for tracking changes.
- **Rate Limiting** -- Built-in protection against brute-force attacks, supporting in-memory or distributed cache.
- **Email Sending** -- Integrated email sender with providers for SMTP and development logging.

## Configuration

Key `DabblerSecurityOptions` properties:

| Option | Type | Description |
|---|---|---|
| `ConnectionString` | `string` | SQL Server connection string for the EF Core store and auditing. |
| `MagicLinkExpiry` | `TimeSpan` | How long magic links and codes remain valid. Default: 10 minutes. |
| `RequireMfa` | `bool` | A global setting to require MFA for all users. |
| `InternalApiKey` | `string` | A secret key for securing service-to-service communication. |
| `Email` | `EmailOptions` | Configures the email sender (type, host, port, credentials, from address). |
| `Mfa` | `MfaOptions` | Configures TOTP settings (lockout policies, encryption key, per-role requirements). |
| `RateLimiting` | `RateLimitingOptions` | Configures rate limiting, including an option to use `IDistributedCache`. |
| `UnregisteredResourcePolicy` | `UnregisteredResourcePolicy` | Determines access to unregistered resources. `FailOpen` (default) or `FailClosed`. |

## Auth Endpoints

`MapDabblerSecurityEndpoints()` adds the following routes:

| Method | Path | Description |
|---|---|---|
| `POST` | `/auth/send-magic-link` | Initiates the magic link login flow. |
| `POST` | `/auth/verify` | Verifies a magic link token or 6-digit code. |
| `POST` | `/auth/verify-totp` | Verifies a TOTP code during MFA challenge. |
| `POST` | `/auth/logout` | Signs the user out. |
| `GET` | `/auth/challenge/{scheme}` | Redirects to an external identity provider. |
| `GET` | `/auth/external-callback` | Handles the callback from an external provider. |

## Dependencies

- Microsoft.EntityFrameworkCore (>= 10.0.5)
- Microsoft.EntityFrameworkCore.SqlServer (>= 10.0.5)
- Otp.NET (>= 1.4.0)
- Microsoft.AspNetCore.Authentication.OpenIdConnect (>= 10.0.5)

## License

This package is licensed under the [MIT License](https://opensource.org/licenses/MIT).

```

### `src/Libs/Dabbler.Security.Blazor/README.md`

```markdown
# Dabbler.Security.Blazor

> Part of the Dabbler Platform. See
> [`docs/platform-overview.md`](../../../docs/platform-overview.md) for
> the platform-level view of how this library fits in.

Dabbler.Security.Blazor provides the notes-driven runtime authorization UI for
Blazor Server applications. The supported core surface centers on secured
rendering, role-aware navigation, runtime permission editing, and the security
dashboard/admin pages that the app and dedicated tests already prove.

Authentication pages for magic link login, MFA, and external-login workflows
remain available, but that broader identity breadth is product- and
policy-shaped rather than the main reusable-surface contract this repo is
currently simplifying.

## Quick Start

> **Adoption note (2026-05):** the supported adoption model today is
> **integrated source** — add `<ProjectReference>` entries from your
> app to `src/Libs/Dabbler.Security/` and `src/Libs/Dabbler.Security.Blazor/`.
> The first-party Dabbler NuGet packages are the future packaged path
> described in
> [`docs/platform-overview.md`](../../../docs/platform-overview.md);
> they have not shipped yet. Third-party dependencies listed under
> *Dependencies* below (MudBlazor, ASP.NET Core packages) are
> normal NuGet packages your app project already references.

Register the necessary services in your `Program.cs`:

```csharp
builder.Services.AddDabblerSecurityBlazor();
```

This registers MudBlazor services, cascading authentication state, and a `DelegatingHandler` for passing user identity to downstream API calls.

## Components

### Authentication Pages

| Component | Route | Description |
|---|---|---|
| `LoginPage` | `/login` | Magic link login with optional external OIDC/OAuth entry points. |
| `MfaPage` | `/mfa` | TOTP setup and verification when a product policy enables MFA. |

### Admin Pages

| Component | Route | Description |
|---|---|---|
| `UserListPage` | `/admin/users` | List and manage application users. |
| `UserEditPage` | `/admin/users/{id}` | Edit user details and metadata. |
| `RoleListPage` | `/admin/roles` | View and manage security roles. |
| `RoleAssignmentPage` | `/admin/roles/assign` | Assign and remove roles from users. |
| `AuditLogPage` | `/admin/audit` | View a chronological audit trail of security events. |

### Supported Core Components

| Component | Description |
|---|---|
| `DabblerSecurityNav` | Navigation menu item for admin pages; automatically appears for authorized users. |
| `DabblerSecured` | Wrapper component that declaratively enforces page, field, and control permissions. |
| `DabblerRoleSwitcher` | Dropdown for users with multiple roles to switch their active role. |

### Admin Dashboard Components

| Component | Description |
|---|---|
| `DabblerSecurityDashboard` | Central admin panel. Under the restriction-based model, it exposes five tabs: Role Master Details, Default Masks, Organization Master Details, Users and Role Assignment, and Role Documentation. Each tab is gated by the active baked-in role. |
| `RestrictedFormField` / `RestrictedGridColumn` | Render fields and grid columns subject to the active role's `ColumnRestriction` rules and any `DefaultMask` + `MaskExemption` resolution. |

## Current Contract Notes

- The retained supported contract is the runtime authorization story:
  secured rendering, role switching, security navigation, permission editing,
  and row filters.
- The admin/dashboard pages remain part of the supported core because the app
  and dedicated `tests/Dabbler.Security.Blazor.Tests/` suite exercise them
  directly.
- Broader external-login and MFA breadth remains available, but should be
  treated as product/policy configuration rather than the default reusable
  platform story.

## Admin Dashboard Setup

Add the central security dashboard to any page:

```razor
@page "/admin/security"

<DabblerSecurityDashboard />
```

## DabblerSecured Component Usage

Wrap any content that requires role-based permissions. The component automatically handles visibility and state based on the current user's role:

```razor
<DabblerSecured PageKey="Dashboard" ControlKey="ExportButton">
    <MudButton>Export</MudButton>
</DabblerSecured>
```

## Role Switcher Setup

Add the role switcher to your layout or navigation bar. It renders a dropdown for users assigned more than one role:

```razor
<DabblerRoleSwitcher />
```

## Dependencies

- `Dabbler.Security` — core package (integrated source today at
  `src/Libs/Dabbler.Security/`; future NuGet release per
  [`docs/platform-overview.md`](../../../docs/platform-overview.md))
- [MudBlazor](https://www.nuget.org/packages/MudBlazor/) (8.x)
- [Microsoft.AspNetCore.Components.Web](https://www.nuget.org/packages/Microsoft.AspNetCore.Components.Web/)

## License

This package is licensed under the [MIT License](https://opensource.org/licenses/MIT).

```

### `src/Libs/Dabbler.Testing.Blazor/README.md`

```markdown
# Dabbler.Testing.Blazor

> Part of the Dabbler Platform. See
> [`docs/platform-overview.md`](../../../docs/platform-overview.md) for
> the platform-level view of how this library fits in.

Testing utilities for Blazor Server applications, with emphasis on deterministic synchronization for Playwright E2E tests.

## Features

- **Action Watermark Tracking**: DOM attributes (`data-actions-in-flight`, `data-last-action`, `data-render-version`) that expose when Blazor components have settled after user actions.
- **Test Readiness Probe**: Signals when the initial page load is complete via `data-blazor-ready` attribute.
- **TrackedComponentBase**: Opt-in base class for components that perform async work in event handlers.
- **JS Interop Shim**: Low-overhead JavaScript module that maintains watermark attributes.

## Getting Started

### 1. Enable in Development

Add to `appsettings.Development.json`:

```json
{
  "Testing": {
    "ActionTracking": {
      "Enabled": true
    }
  }
}
```

### 2. Register Services

In `Program.cs`:

```csharp
using Dabbler.Testing.Blazor.Hosting;

builder.Services.AddTrackedActions(builder.Configuration);
```

### 3. Wire into App

In `App.razor`, conditionally load the JS shim:

```razor
@inject IOptions<TrackedActionsOptions> Options

@code {
    private bool Enabled => Options.Value.Enabled;
}

<!-- In body -->
@if (Enabled)
{
    <script src="_content/Dabbler.Testing.Blazor/dabbler-tracked-actions.js"></script>
}
```

### 4. Add Readiness Probe

In `MainLayout.razor`:

```razor
@using Dabbler.Testing.Blazor.Components

<TestReadinessProbe />
```

### 5. Opt-in Components

Inherit from `TrackedComponentBase` and wrap event handlers:

```csharp
public class MyComponent : TrackedComponentBase
{
    private async Task SaveAsync()
    {
        await TrackAction("save", async () =>
        {
            await _service.SaveAsync(_model);
        });
    }
}
```

### 6. Playwright Tests

Use the watermark attributes to wait for renders:

```csharp
// After clicking a button that triggers a handler:
await Assertions.Expect(page.Locator("html"))
    .ToHaveAttributeAsync("data-actions-in-flight", "[]");
```

Or wait for initial readiness:

```csharp
await Assertions.Expect(page.Locator("html"))
    .ToHaveAttributeAsync("data-blazor-ready", "true");
```

## Configuration

| Setting | Type | Default | Notes |
|---------|------|---------|-------|
| `Testing:ActionTracking:Enabled` | bool | false | Set to true in Development; false in Production (safe no-op) |

## Limitations

- Animation waits (CSS transitions) are not render-correlated and still require `Task.Delay()` with documentation.
- External login pages (e.g., DevIdp) don't set `data-blazor-ready` and require conditional handling in Playwright fixtures.
- Concurrent actions in the same component are tracked correctly but require coordinated teardown in tests.

## Testing

See `tests/Dabbler.Testing.Blazor.Tests/` for bUnit tests verifying watermark behavior.

## References

- [AI-Led Session Workflow](../../../docs/ai-led-session-workflow.md)
- [Project Guidance](../../../docs/planning/project-guidance.md) (Testing Hierarchy section)

```

---

### Repo layout (high-level)

- `src/Apps/AcmeCoffeeOnline.Web/` — Blazor Server demo app
- `src/Apps/AcmeCoffeeOnline.Api/` — REST API for the demo app
- `src/Apps/AcmeCoffeeOnline.Shared/` — shared DTOs/models
- `src/Libs/` — 8 platform libraries (the per-library READMEs above)
- `tests/` — xUnit / bUnit / Playwright coverage
- `docker/` — compose stack (sqlserver, mailhog, web, api)
- `docs/` — workflow / session sets / planning docs
- `ai_router/` + `uat_runner/` — internal AI-led workflow tooling

End of artifacts. Begin your review.
