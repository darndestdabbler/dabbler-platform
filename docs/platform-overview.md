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
