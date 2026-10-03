# Retrofitting Dabbler.* Into an Existing Blazor Server App

> **Companion docs:**
> [`scenarios.md`](./scenarios.md) (per-scenario package sets) and
> [`packaging-readiness-ledger.md`](../session-sets/packaging-and-template-readiness/packaging-readiness-ledger.md)
> §3 (packable surface), §4 (stubs contract).
> **Purpose:** A step-by-step recipe for teams who already run a Blazor
> Server app and want to adopt Dabbler.* libraries incrementally
> without rewriting their existing app.

This recipe assumes the existing app is on **.NET 10**, **Blazor
Server**, and uses **MudBlazor 8.x**. Apps on different stacks need
to upgrade to that baseline before adopting Dabbler.*; the ledger
explicitly does not multi-target older runtimes.

> All install commands assume the future packaged install — see
> [ledger §3.6](../session-sets/packaging-and-template-readiness/packaging-readiness-ledger.md#36-versioning-policy-final).
> Until the first preview ships (end of Session 7), substitute the
> integrated-source path: add a `<ProjectReference Include="…src/Libs/Dabbler.<Module>/…csproj" />`
> instead of `<PackageReference>`.

## Pre-flight checklist

Before starting the retrofit, confirm:

- [ ] App targets `net10.0`.
- [ ] App is a Blazor Server app (Server interactivity, not WASM).
- [ ] App uses MudBlazor 8.x.
- [ ] An ASP.NET Core auth pipeline already exists OR you are willing
      to add Dabbler.Security's magic-link flow as the primary auth
      story.
- [ ] You have a SQL Server instance available for the EF Core store
      (alternative: JSON file store for dev / single-server
      deployments).

If any box is unchecked, address that prerequisite first — the recipe
below assumes them.

## Step 1 — Adopt `Dabbler.Components` for the CRUD helpers

The smallest first step. Add the QBE filter panel + dirty-tracking
edit session to one existing page; once that page works, the rest of
the platform's primitives become familiar.

**Install:**

```xml
<PackageReference Include="Dabbler.Components" />
```

**Register (no library-wide extension; just ensure MudBlazor services
are registered):**

```csharp
builder.Services.AddMudServices(); // probably already registered
builder.Services.AddScoped<IGridDataProvider<Customer>, CustomerGridDataProvider>();
```

**Migrate one query page:**

Replace the existing filter-panel / grid composition with:

```razor
<DeclarativeFilterPanel TEntity="Customer"
                        FilterColumns="@_filterColumns"
                        Coordinator="@_coordinator" />

<MudDataGrid T="Customer"
             Bordered="true"
             ServerData="@_dataProvider.ToServerDataFunc(_coordinator.GetCompositeFilter)">
    <Columns>
        <PropertyColumn Property="x => x.Name" Title="Name" />
        <PropertyColumn Property="x => x.City" Title="City" />
    </Columns>
</MudDataGrid>
```

**Verification before moving on:**

- The migrated page renders.
- Filtering works (AND / OR groups, operators, presets).
- The grid pages and sorts as expected.

## Step 2 — Add `Dabbler.Security` + `Dabbler.Security.Blazor` for authorization

This step is where the bulk of the retrofit work lives, because the
existing app's auth pipeline determines whether Dabbler.Security is
the **outer** auth scheme (replacing what the app had) or an **inner**
authorization policy layered onto an existing auth pipeline.

### Step 2a — Decide: outer auth replacement or inner authz layer?

| Choice | When it fits | Effort |
|--------|--------------|--------|
| **Outer auth replacement** | App's existing auth was cookie-only and you're willing to switch to magic-link as the auth ceremony. | High — you replace `AddAuthentication(...)` calls and migrate users to the magic-link table. |
| **Inner authz layer** | App already has stable auth (Microsoft Entra, OAuth, custom JWT, etc.) and you want only the runtime restriction-based authorization on top. | Lower — keep existing `AddAuthentication`, add `Dabbler.Security` for `ColumnRestriction` / `RowRestriction` / `UiRestriction` enforcement. |

For most retrofits the **inner authz layer** is the right answer.
This recipe assumes that path.

### Step 2b — Install

```xml
<PackageReference Include="Dabbler.Security" />
<PackageReference Include="Dabbler.Security.Blazor" />
```

### Step 2c — Register Dabbler.Security AFTER the existing auth scheme

```csharp
// Existing app's auth (unchanged)
builder.Services.AddAuthentication(/* existing scheme */).AddCookie(/* ... */);

// Dabbler.Security as the inner authorization layer
builder.Services.AddDabblerSecurity(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("Default")!;

    // Don't enable magic-link if you're not using it
    // options.MagicLinkExpiry = TimeSpan.FromMinutes(10);

    options.RegisterPages(p => p.Add("Customers", "/customers"));
    options.RegisterEntities(e => e.Add("Customer"));
    options.RegisterEndpoints(ep => ep.Add("UpdateCustomer", "PUT /api/customers/{id}"));
    options.RegisterControls(c => c.Add("Customers", "DeleteButton"));
});

builder.Services.UseEfCoreStore();
builder.Services.AddDabblerSecurityBlazor();

var app = builder.Build();

// If using inner-authz, you typically do NOT call MapDabblerSecurityEndpoints
// (the magic-link endpoints aren't needed).
// app.MapDabblerSecurityEndpoints();
```

### Step 2d — Apply restrictions

For each protected resource, add the `RegisterX` entry and the matching
`security-config.json` rule. Pages get a `<DabblerSecured>` wrap or a
`DabblerRoleSwitcher` placement. Grid columns get
`<RestrictedGridColumn>` instead of `MudDataGrid`'s native
`<PropertyColumn>`.

### Step 2e — Migration pitfall: existing user identity → Dabbler `AppUser` table

The EF Core store has its own `AppUser` table. If the app already has a
user table, decide:

- **Map existing user IDs onto `AppUser.Id`.** Add a one-time migration
  that seeds `AppUser` from your existing user table; reuse the same
  primary key.
- **Run side-by-side.** Keep your existing user table for app-specific
  user data; use `AppUser` only for the role/restriction binding. Join
  on email or another stable identifier.

The choice matters for the role-switcher: `DabblerRoleSwitcher` reads
the active role from the cookie's auth state, which is wired to the
`AppUser` row via the `AuthenticationStateProvider`.

### Verification before moving on:

- A test user assigned to `BackofficeUser` (user-type role) sees the
  restricted column behavior on the migrated page.
- The `DabblerSecurityDashboard` renders for users in
  `SystemAdmin`/`UserAdmin` and is hidden for everyone else.
- The role switcher appears for multi-role users and changes the
  active context.

## Step 3 — Add Reports.* if reporting is in scope

For apps that have an existing reporting story, this step often does
not apply — but if the team wants to migrate from a brittle in-house
report generator, Dabbler.Reports is a natural drop-in.

**Install:**

```xml
<PackageReference Include="Dabbler.Reports.Core" />
<PackageReference Include="Dabbler.Reports.Export" /> <!-- if PDF export is in scope -->
<PackageReference Include="Dabbler.Reports.Blazor" /> <!-- if in-browser viewer is in scope -->
```

**Register:**

```csharp
builder.Services.AddScoped<IReportDataProvider, YourReportDataProvider>();
```

**Author one report:**

1. Create an `.rdf.json` describing one report (see
   [`docs/reports/rdf-schema.md`](../reports/rdf-schema.md) for the
   schema reference + worked example).
2. Mount `<DabblerReportPage RdfPath="reports/your-report.rdf.json" .../>`
   on a page.
3. Implement `IReportDataProvider` to return rows for that report's
   query.
4. Verify the report renders with parameter prompts → execute → view.

**Migration pitfall — DabblerPdf license.** The Reports.* packages
carry a transitive dependency on `DabblerPdf`, which is
**dual-licensed LGPL-2.1-only OR MPL-2.0** — *not* MIT. Get compliance
sign-off before adopting Reports.* in an app whose distribution
policy is sensitive to copyleft transitive dependencies. The package's
NuGet metadata calls this out, and a `THIRD-PARTY-NOTICES.txt`
ships in the package (Session 7 deliverable per A7).

## Step 4 — Add `Dabbler.Testing.Blazor` for Playwright synchronization

If the team has Playwright tests and is frustrated by flaky
"click-then-wait-1-second" patterns, the watermark library is a high-
leverage drop-in.

**Install:**

```xml
<PackageReference Include="Dabbler.Testing.Blazor" />
```

**Register:**

```csharp
builder.Services.AddTrackedActions(builder.Configuration);
```

Enable in `appsettings.Development.json`:

```json
{
  "Testing": {
    "ActionTracking": {
      "Enabled": true
    }
  }
}
```

**Wire into the app:** see
[`Dabbler.Testing.Blazor/README.md`](../../src/Libs/Dabbler.Testing.Blazor/README.md)
§ "Smallest working example" (steps 1-4 of the wiring; steps 5-6 are
the test side).

**Migration plan:**

- Components most prone to flake (those with async event handlers
  driving a re-render) opt in by inheriting `TrackedComponentBase` and
  wrapping their handlers with `TrackAction("…", async () => { … })`.
- Playwright tests replace ad-hoc `await page.WaitForTimeoutAsync(1000)`
  with `await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("data-actions-in-flight", "[]")`.

## Step 5 — Add stub sub-packages for bUnit tests

For bUnit tests of components that depend on Dabbler.Components'
data-provider abstractions, the focused stub providers eliminate the
need for hand-rolled in-memory test doubles.

**Install (post-Session 7):**

```xml
<PackageReference Include="Dabbler.Platform.Stubs.Components" />
```

Until the Session 7 sub-package split lands, the only option is the
single `Dabbler.Platform.Stubs` package (with the transitive Reports
stack).

**Register (test setup):**

```csharp
ctx.Services.AddStubGridDataProvider<Customer>(new[]
{
    new Customer(101, "Ada Lovelace"),
    new Customer(102, "Grace Hopper"),
});
```

The same component code renders against stub providers in tests and
real providers in production — no conditional code paths.

## Common pitfalls

### "My existing `AddAuthentication` got shadowed"

`Dabbler.Security` does not call `AddAuthentication` by default; the
inner-authz path leaves your existing auth pipeline intact. If you
**did** opt into the outer-auth-replacement path and now `Dabbler.Security`'s
magic-link auth is the only scheme, your previous auth code paths need
to be removed cleanly — leaving them in produces double-cookie races.

### "Role enforcement isn't kicking in on EF queries"

The `RowRestriction` mechanism requires the app's EF query path to call
`IRowRestrictionEvaluator.Apply<T>(...)` on each query. If the team's
existing repository layer doesn't have an injection point for this,
the row restriction will not apply. Solution: add the binder call at
the repository layer, not the controller layer (then every consumer
benefits automatically).

### "DabblerSecured isn't hiding the button"

`<DabblerSecured PageKey="…" ControlKey="…">` reads from the
`UiRestriction` table for the currently active role. Check:

- Did you call `options.RegisterControls(controls => controls.Add("PageName", "ControlName"))` in
  `Program.cs`?
- Is the control registered under the matching `PageKey` (not the
  route path)?
- Does the active role's user-type-role definition in
  `security-config.json` include a `UiRestriction` row for this
  `(PageKey, ControlKey)` pair?
- Is the active role a user-type role? Baked-in roles are not
  restriction-enforced (per ledger / `Dabbler.Security/README.md` §
  Warnings).

### "Reports renders but PDF export is missing fonts"

DabblerPdf handles font subsetting if the RDF references standard
system fonts. Custom fonts require the RDF to point at a font file
path. Add font assets to your app's `wwwroot/` and reference them in
the RDF.

### "bUnit tests fail with `IJSRuntime is not yet dispatchable`"

Dabbler.Testing.Blazor's `TrackAction(...)` uses `TryInvokeJsAsync` and
silently no-ops on prerender boundaries. This is the safe behavior;
the test-time signal degrades to "no signal" rather than crashing. If
a bUnit test expects watermark behavior, ensure it's rendering past
the prerender boundary before asserting.

## What this recipe does NOT cover

- **Adopting the VS template scaffold.** The template
  (`dotnet new dabbler-app -o MyApp`) is for new apps, not existing
  ones. Use the per-step recipe above for brownfield retrofits.
- **Migrating from `[Authorize(Roles="…")]` attributes.** Dabbler's
  restriction model is data-driven, not attribute-driven. The two
  models coexist (your existing `[Authorize]` still works), but the
  durable platform pattern is to migrate `[Authorize(Roles="…")]`
  guards into `RegisterEndpoints` + endpoint-permission rules over
  time.
- **The Access metadata-harvesting CLI consumer path.** A future CLI
  scaffolds on top of the platform with its own code-gen layer; that
  story is not part of this recipe.

## Verification checklist (after the retrofit)

- [ ] All pre-existing tests pass.
- [ ] All Dabbler.* libraries are registered in `Program.cs` per the
      recipe steps that apply.
- [ ] One end-to-end flow exercising the platform's primitives works
      in a browser:
      sign in → reach a query page → filter the grid → save a detail
      record → view a report → sign out.
- [ ] The `DabblerSecurityDashboard` renders for `SystemAdmin` and is
      hidden for non-admin baked-in roles.
- [ ] At least one Playwright test uses the watermark instead of
      arbitrary `WaitForTimeoutAsync` calls.
