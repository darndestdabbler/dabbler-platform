# Reusable Platform Stub Guide

> **Purpose:** Document the supported DI-driven stub capability frozen by
> `accessibility-and-stubbing` Session 3.

The supported stub story is now the `Dabbler.Platform.Stubs` library under
`src/Libs/`.

Use it when a host needs reusable platform behavior before real providers are
available, not as a one-off test hack.

## Supported Consumers

- bUnit and direct reusable-surface tests
- stub or demo app modes
- early UAT hosts that need navigation or report proof without backend wiring
- future generated apps that should start from the same provider seams

## Supported Stub Seams

- query and grid data via `StubGridDataProvider<T>`
- lookup and cascading data via `StubComboBoxItemProvider<T>`
- report execution and parameter lookups via `StubReportDataProvider`
- metadata, navigation, report-nav items, and home-page routing via `StubPlatformDefinition`
- optional compatibility catalog and usage state via `StubArtifactCatalogProvider`
  and `StubUsageTrackingProvider`
- role-aware and organization-aware auth state via `StubAuthenticationStateProvider`
  and `StubUserContext`

## DI Patterns

Use the full-host registration when the entire shell or generated app should
swap to stub metadata/navigation services:

```csharp
var definition = new StubPlatformDefinition
{
    Entities = [customerMetadata],
    Navigation = new StubNavigationDataset()
        .AddRecords("Customer", "Recent", [new NavigationRecord(101, "Ada Lovelace")]),
    ReportItems = [new NavigationReportItem("CustomerSummary", "Customer Summary")]
};

services.AddDabblerPlatformStubs(definition);
services.AddStubGridDataProvider(customerRows);
services.AddStubReportDataProvider(provider =>
{
    provider.AddQueryResult(queryText, customerSummaryTable);
});
services.AddStubAuthentication(new StubUserContext
{
    UserId = "stub-user",
    Roles = ["BackofficeUser"],
    OrganizationId = "org-demo"
});
```

Use focused concrete registrations when a larger app already has real
application services and only one stub-backed page or demo route should opt in.
The Acme demo route at `/platform/stub` follows this pattern so it does not
replace the real app-wide metadata or navigation providers.

## App Demo Mode

`src/Apps/AcmeCoffeeOnline.Web/` now includes an opt-in demo route:

- route: `/platform/stub`
- guard: Development or Test host with `PlatformStubMode:Enabled=true`
- purpose: demonstrate shell, workspace, query-page, and report-page
  consumption of the reusable stubs without changing the real app path

## Design Boundaries

- Keep the stub contracts platform-generic. The future Access metadata CLI is a
  consumer of these seams, not their design center.
- Prefer real provider interfaces and DI registration over bespoke component
  parameters where the platform already has a stable contract.
- Keep role and organization context in claims or auth-state shape that later
  tests and generated apps can reuse directly.
