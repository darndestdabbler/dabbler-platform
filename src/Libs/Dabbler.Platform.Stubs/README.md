# Dabbler.Platform.Stubs

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
