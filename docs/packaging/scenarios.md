# Dabbler Platform — Adoption Scenarios

> **Companion doc:** [`packaging-readiness-ledger.md`](../session-sets/packaging-and-template-readiness/packaging-readiness-ledger.md)
> §3 (packable surface) and §4 (Stubs opt-in contract).
> **Purpose:** Translate the library matrix into common adoption shapes
> so a prospective consumer can answer "what should I install?" in under
> two minutes.

The platform ships as a set of focused libraries that compose à la
carte. There is no single mandatory entry point: depending on what an
app needs, install one library, a few, or the whole set. This doc
enumerates the recurring shapes and the matching `<PackageReference>`
sets.

> All commands assume the future packaged install — see
> [ledger §3.6](../session-sets/packaging-and-template-readiness/packaging-readiness-ledger.md#36-versioning-policy-final).
> Until the first preview ships (end of Session 7), substitute the
> integrated-source path: add a `<ProjectReference Include="…src/Libs/Dabbler.<Module>/…csproj" />`
> instead of `<PackageReference>`.

## At-a-glance scenarios matrix

| Scenario | Audience | Packages | Notes |
|----------|----------|----------|-------|
| Just CRUD helpers | Internal-tool teams wanting QBE filtering + dirty tracking on a single page | `Dabbler.Components` | No authorization, no reporting. |
| Full platform (typical app) | Teams building a multi-page back-office app | `Dabbler.Components` + `Dabbler.Security` + `Dabbler.Security.Blazor` + `Dabbler.Reports.{Core,Export,Blazor}` + `Dabbler.Testing.Blazor` + (optional) `Dabbler.Platform.Stubs` | The `dabbler-app` template scaffolds this shape. |
| Reports only | Teams adding banded PDF reports to an existing app | `Dabbler.Reports.Core` + `Dabbler.Reports.Export` + `Dabbler.Reports.Blazor` + `Dabbler.Components` (transitive) | Carries the DabblerPdf LGPL/MPL license — see §6. |
| Security only (API consumer) | API-only consumers needing restriction-based authz, no Blazor UI | `Dabbler.Security` | No Blazor dependencies. |
| Security only (Blazor app, no business CRUD) | Admin / governance dashboards | `Dabbler.Security` + `Dabbler.Security.Blazor` + `Dabbler.Components` (transitive) | Pulls in Components for the form/grid primitives the dashboard uses. |
| Testing utility only | Teams using the Playwright watermark on a non-Dabbler Blazor Server app | `Dabbler.Testing.Blazor` | No Dabbler.* dependencies — drops in cleanly. |
| Retrofit existing Blazor Server app | Teams incrementally adopting Dabbler.* into a brownfield app | See [`retrofit-existing-app.md`](./retrofit-existing-app.md) | Step-by-step recipe with registration order. |
| bUnit-only test wiring | Teams adopting only Dabbler.Components and writing bUnit tests against it | `Dabbler.Platform.Stubs` | Carries DabblerPdf transitively through `Dabbler.Reports.Core`. |

## 1. Just CRUD helpers

A team wants the QBE filter panel + dirty-tracking edit session + the
master-detail expansion shell on a single page. No authorization model.
No reports.

**Install:**

```xml
<PackageReference Include="Dabbler.Components" />
```

**Register:**

```csharp
builder.Services.AddMudServices();
builder.Services.AddScoped<IGridDataProvider<Customer>, CustomerGridDataProvider>();
```

**Usage:**

See [`Dabbler.Components/README.md`](../../src/Libs/Dabbler.Components/README.md)
§ "Smallest working example".

**Gotchas:**

- MudBlazor 8.x `data-testid` lives on the `<input>`, not the wrapper —
  test selectors should account for this.
- `MudTextField ReadOnly="true"` does NOT set the HTML `readonly`
  attribute; use behavior proxies (toggle-button text) instead.

## 2. Full platform (typical app)

A team is building a back-office app with authentication, runtime
authorization, banded reports, and Playwright E2E synchronization.
This is what the `dabbler-app` VS template scaffolds.

**Install:**

```xml
<PackageReference Include="Dabbler.Components" />
<PackageReference Include="Dabbler.Security" />
<PackageReference Include="Dabbler.Security.Blazor" />
<PackageReference Include="Dabbler.Reports.Core" />
<PackageReference Include="Dabbler.Reports.Export" />
<PackageReference Include="Dabbler.Reports.Blazor" />
<PackageReference Include="Dabbler.Testing.Blazor" />
<!-- Optional: pre-wire stubs for bUnit tests -->
<PackageReference Include="Dabbler.Platform.Stubs" />
```

**Register (Program.cs, abridged):**

```csharp
builder.Services.AddMudServices();
builder.Services.AddDabblerSecurity(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("Default")!;
    options.RegisterPages(p => p.Add("Dashboard", "/dashboard"));
    options.RegisterEntities(e => e.Add("Order"));
    options.RegisterEndpoints(ep => ep.Add("CreateOrder", "POST /api/orders"));
    options.RegisterControls(c => c.Add("Dashboard", "ExportButton"));
});
builder.Services.UseEfCoreStore();
builder.Services.AddDabblerSecurityBlazor();
builder.Services.AddTrackedActions(builder.Configuration);

var app = builder.Build();
app.MapDabblerSecurityEndpoints();
app.Run();
```

**Gotchas:**

- The `Dabbler.Reports.*` packages carry a transitive `DabblerPdf`
  dependency that is **dual-licensed LGPL-2.1-only OR MPL-2.0**, not
  MIT. Compliance-reviewed deployments should accept this before
  adopting. See §6.
- `Dabbler.Security`'s `UnregisteredResourcePolicy` defaults to
  `FailOpen`. Compliance-oriented deployments should consider
  `FailClosed` and accept the friction of registering every resource
  the runtime should authorize.

## 3. Reports only

A team adds banded PDF reports to an existing app whose auth and CRUD
flows already exist independently.

**Install:**

```xml
<PackageReference Include="Dabbler.Reports.Core" />
<PackageReference Include="Dabbler.Reports.Export" />
<PackageReference Include="Dabbler.Reports.Blazor" /> <!-- optional, for in-browser viewer -->
<!-- Dabbler.Components is pulled transitively for the expression engine -->
```

**Register:**

```csharp
builder.Services.AddMudServices();
builder.Services.AddScoped<IReportDataProvider, YourReportDataProvider>();
```

**Usage:**

See [`Dabbler.Reports.Core/README.md`](../../src/Libs/Dabbler.Reports.Core/README.md)
§ "Smallest working example" for the engine; see
[`Dabbler.Reports.Blazor/README.md`](../../src/Libs/Dabbler.Reports.Blazor/README.md)
for the in-browser viewer and `DabblerReportPage` wiring.

**Gotchas:**

- DabblerPdf license is the headline concern; see §6.
- If only the PDF byte-stream is needed (no in-app viewer), the
  Reports.Blazor package can be omitted entirely.

## 4. Security only (API consumer)

An API-only service needs restriction-based authorization with magic-link
authentication and EF Core persistence — but no Blazor UI.

**Install:**

```xml
<PackageReference Include="Dabbler.Security" />
```

**Register:**

```csharp
builder.Services.AddDabblerSecurity(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("Default")!;
    options.RegisterEntities(e => e.Add("Order"));
    options.RegisterEndpoints(ep => ep.Add("CreateOrder", "POST /api/orders"));
});
builder.Services.UseEfCoreStore();

var app = builder.Build();
app.MapDabblerSecurityEndpoints();
app.MapPost("/api/orders", CreateOrderHandler).RequireEndpointPermission("CreateOrder");
app.Run();
```

**Gotchas:**

- No `RegisterPages` / `RegisterControls` calls — there's no UI to
  restrict.
- The `MapDabblerSecurityEndpoints()` call still wires the magic-link
  flow; if the API consumer doesn't need any auth ceremony for its
  own clients (e.g., service-to-service only), use `InternalApiKey`
  and skip the magic-link endpoints.

## 5. Security only (Blazor app, no business CRUD)

An admin / governance dashboard whose sole job is to manage Dabbler.*
roles and configuration for sibling apps. No business entities of its
own.

**Install:**

```xml
<PackageReference Include="Dabbler.Security" />
<PackageReference Include="Dabbler.Security.Blazor" />
<!-- Dabbler.Components is pulled transitively for the dashboard's grids -->
```

**Register:**

```csharp
builder.Services.AddDabblerSecurity(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("Default")!;
    options.RegisterPages(p =>
    {
        p.Add("Security", "/admin/security");
        p.Add("Users",    "/admin/users");
        p.Add("Roles",    "/admin/roles");
    });
});
builder.Services.UseEfCoreStore();
builder.Services.AddDabblerSecurityBlazor();
```

**Usage:**

Mount `<DabblerSecurityDashboard />` on a page; see
[`Dabbler.Security.Blazor/README.md`](../../src/Libs/Dabbler.Security.Blazor/README.md)
for the admin-pages routes.

**Gotchas:**

- The dashboard's user-management pages require an `AppUser` table —
  the EF Core store provides it. JSON file store works but is not
  recommended for multi-server deployments.

## 6. Testing utility only

A team using a non-Dabbler Blazor Server app wants the deterministic
Playwright synchronization without adopting the rest of the platform.

**Install:**

```xml
<PackageReference Include="Dabbler.Testing.Blazor" />
```

**Register:**

```csharp
builder.Services.AddTrackedActions(builder.Configuration);
```

**Usage:**

See [`Dabbler.Testing.Blazor/README.md`](../../src/Libs/Dabbler.Testing.Blazor/README.md)
§ "Smallest working example".

**Gotchas:**

- This package has **no Dabbler.\* dependencies** — it drops in
  cleanly to any Blazor Server app.
- Components opt in to the watermark by inheriting
  `TrackedComponentBase` and wrapping handlers with `TrackAction(...)`.
  Uninstrumented handlers are invisible to the watermark.

## 7. Retrofit existing Blazor Server app

A team has an existing app and wants to adopt Dabbler.* incrementally.

See [`retrofit-existing-app.md`](./retrofit-existing-app.md) for the
recipe.

## 8. bUnit-only test wiring

A team adopts only `Dabbler.Components` (e.g., scenario #1) and writes
bUnit tests against it, using the stub providers.

**Install:**

```xml
<PackageReference Include="Dabbler.Platform.Stubs" />
```

The package transitively pulls `Dabbler.Reports.Core` and `DabblerPdf`. See
[ledger §4](../session-sets/packaging-and-template-readiness/packaging-readiness-ledger.md#4-stubs-opt-in-contract-frozen).

**Register (bUnit test setup):**

```csharp
[Fact]
public void CustomersGrid_RendersStubRows()
{
    using var ctx = new TestContext();
    ctx.Services.AddStubGridDataProvider<Customer>(new[] { /* in-memory rows */ });
    var cut = ctx.RenderComponent<CustomersGrid>();
    // assertions...
}
```

## Optional Session 7 metapackages

The ledger §3.8 leaves room for three optional metapackages that ship
in Session 7 if the matrix above turns out to be insufficient:

- `Dabbler.Reports` — references `Dabbler.Reports.{Core,Export,Blazor}`.
- `Dabbler.Security.All` — references `Dabbler.Security` +
  `Dabbler.Security.Blazor`.
- `Dabbler.Platform` — references everything a typical full app would
  need (the "Full platform" scenario above).

The Session 7 decision is based on whether this matrix alone reads as
self-explanatory in Session 7's smoke testing. If consumers reach for
the matrix and still ask "but which set is the 'just the platform'
shortcut?", the metapackages ship. Otherwise they don't.

## License posture across scenarios

- **All Dabbler.\* libraries are MIT.**
- **`Dabbler.Reports.{Core,Export,Blazor}` carry a transitive
  dependency on `DabblerPdf` (LGPL-2.1-only OR MPL-2.0)** — *not* MIT.
  Scenarios #2, #3, and the Reports-bearing template scaffold should
  expect this; the package's NuGet metadata calls it out, and a
  `THIRD-PARTY-NOTICES.txt` file ships in the package (Session 7
  deliverable per A7).
- `Dabbler.Components`, `Dabbler.Security`, `Dabbler.Security.Blazor`
  and `Dabbler.Testing.Blazor` carry **no copyleft transitive
  dependencies** — scenarios that exclude Reports avoid the
  LGPL/MPL license question entirely.

## Stub mode is opt-in (regardless of scenario)

Per [ledger §4.2](../session-sets/packaging-and-template-readiness/packaging-readiness-ledger.md#42-the-contract-stubs-never-silently-activate):

- Referencing a `Dabbler.Platform.Stubs*` package never auto-registers
  stubs.
- A production deployment that accidentally has the package referenced
  does not silently fall back to stubs — DI throws when a real
  provider is missing.
- There is no environment-variable shortcut. Stubs activate only when
  the consumer's `Program.cs` calls `AddDabblerPlatformStubs(...)` or
  a focused per-provider helper.

This contract is binding across all scenarios; do not reach for stubs
as a "real provider not implemented yet" stand-in outside of test /
demo / design-review contexts.
