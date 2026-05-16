# Dabbler.Platform.Stubs

> Part of the Dabbler Platform. See
> [`docs/platform-overview.md`](../../../docs/platform-overview.md) for
> the platform-level view of how this library fits in.

Opt-in stub providers for the Dabbler platform surface. Reusable
DI-friendly stubs for bUnit tests, demo / stub-app hosts, and early
design review when real providers aren't yet wired up.

## Audience

Teams who want the same component code to run against both real
providers (production) and in-memory stubs (tests, demos, design
review) without conditional branches inside the components themselves.

> **Sub-package structure.** This
> package is structured as a **meta-package** that references three focused
> sub-packages: `Dabbler.Platform.Stubs.Components`,
> `Dabbler.Platform.Stubs.Reports`, `Dabbler.Platform.Stubs.Security`.
> Consumers who want only the component stubs (bUnit-only test wiring)
> can install
> `Dabbler.Platform.Stubs.Components` and avoid the transitive
> `Dabbler.Reports.Core` (and therefore `DabblerPdf` LGPL/MPL) dependency
> that a single-package shape would require. The meta-package
> documented below provides the umbrella experience for consumers who want
> everything wired at once.

## Prerequisites and stack constraints

- Blazor Server (matches the platform target)
- MudBlazor 8.x (transitively via `Dabbler.Components`)
- .NET 10 (current LTS)
- Reference to
  [`Dabbler.Components`](../Dabbler.Components/README.md) and
  [`Dabbler.Reports.Core`](../Dabbler.Reports.Core/README.md) (required by
  the meta-package; see sub-package note above for selective alternatives)

## Install

> **Adoption note:**
> The recommended adoption model for internal projects is **integrated source** under
> `src/Libs/Dabbler.Platform.Stubs/`. NuGet packages are available as a meta-package plus three sub-packages;
> to install:
> `dotnet add package Dabbler.Platform.Stubs --prerelease` for the
> umbrella, or `dotnet add package Dabbler.Platform.Stubs.<Sub> --prerelease`
> for one sub-package only.

## Minimal registration

```csharp
// Build a stub-platform definition
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
        }
    ]
};

// Register the umbrella (or pick focused helpers below)
services.AddDabblerPlatformStubs(definition);
```

## Smallest working example

A bUnit test that wires a stub grid provider for `Customer`:

```csharp
[Fact]
public void CustomersGrid_RendersStubRows()
{
    using var ctx = new TestContext();
    ctx.AddDabblerStubs(StubDefinitions.MinimalCustomer());
    ctx.Services.AddStubGridDataProvider<Customer>(new[]
    {
        new Customer(101, "Ada Lovelace"),
        new Customer(102, "Grace Hopper"),
    });

    var cut = ctx.RenderComponent<CustomersGrid>();
    Assert.Contains("Ada Lovelace", cut.Markup);
}
```

The same `CustomersGrid` component renders against the real
`CustomerGridDataProvider` in production — no conditional code paths
inside the component.

## Supported stub surface

| Helper | Provider |
|--------|----------|
| `AddDabblerPlatformStubs(definition)` | Bulk umbrella registration. |
| `AddStubGridDataProvider<T>(rows)` | `IGridDataProvider<T>` for grid + query pages. |
| `AddStubComboBoxItemProvider<T>(items)` | `IComboBoxItemProvider<T>` for lookups. |
| `AddStubReportDataProvider()` | `IReportDataProvider` returning seeded rows + parameter values. |
| `AddStubEntityMetadataProvider(definition)` | Metadata for `DeclarativeFilterPanel` + nav. |
| `AddStubNavigationDataProvider(definition)` | Navigation records for shell-driven hosts. |
| `AddStubArtifactCatalogProvider(items)` | Optional artifact-catalog state. |
| `AddStubUsageTrackingProvider()` | Optional usage tracker. |
| `AddStubAuthentication(userContext)` | `AuthenticationStateProvider` for role-aware / org-aware test contexts. |

## The contract: stubs never silently activate

Per ledger §4.2, stub mode is **explicit opt-in**:

- No auto-discovery. Referencing the package does not register stubs.
- No environment-variable shortcut. Stubs never silently take over.
- No production fallback. If a real provider isn't registered, DI
  throws — which is the correct failure mode for a production app.

## Related packages

- [`Dabbler.Components`](../Dabbler.Components/README.md) — the surfaces
  these stubs back.
- [`Dabbler.Reports.Core`](../Dabbler.Reports.Core/README.md) — backs
  `StubReportDataProvider`. (Now available as sub-package
  `Dabbler.Platform.Stubs.Reports`.)
- [`Dabbler.Security`](../Dabbler.Security/README.md) — backs
  `StubAuthentication`. (Now available as sub-package
  `Dabbler.Platform.Stubs.Security`.)
- [`Dabbler.Testing.Blazor`](../Dabbler.Testing.Blazor/README.md) —
  pairs with the stub providers for Playwright watermark
  synchronization during E2E tests.

## Warnings and caveats

- **DabblerPdf transitive license (meta-package shape) — non-MIT.** Through
  `Dabbler.Reports.Core`, this package (meta-package) transitively pulls in
  `DabblerPdf` (LGPL-2.1-only OR MPL-2.0). Consumers who need only
  `Dabbler.Platform.Stubs.Components` can avoid this dependency by installing
  that sub-package directly.
- **Stubs are not a production posture.** Per ledger §4.2, accidental
  use of stubs in production is prevented by requiring explicit
  registration — but the package is named "Stubs" for a reason; do
  not lean on it as a "real provider not implemented yet" stand-in
  outside of test / demo / design-review contexts.
- **License: MIT** (see [LICENSE](https://licenses.nuget.org/MIT)).
