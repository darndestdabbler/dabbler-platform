# Reusable Platform Stub Guide

> **Purpose:** Document the proven DI-driven stub capability accepted by
> `accessibility-and-stubbing` as a reusable platform baseline.

The supported stub story is now the `Dabbler.Platform.Stubs` library under
`src/Libs/`.

Use it when a host needs reusable platform behavior before real providers are
available, not as a one-off test hack. This is now an evidence-backed platform
capability that later Phase 2 work can rely on directly.

## Accepted Baseline

`accessibility-and-stubbing` Sessions 3 and 4 moved the stub architecture from
"supported in principle" to "proved in practice."

The accepted baseline is:

- reusable DI seams exist for query/grid data, lookups, report execution,
  metadata/navigation, and role-aware or organization-aware auth context
- the same seams support direct bUnit proof and an app-hosted stub/demo route
- `/platform/stub` is the reference app-consumption path for focused shell,
  workspace, query-page, and report-page proof
- the retained reusable surface now has direct contract and WCAG-oriented
  evidence that depends on these stub seams instead of private one-off fakes

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
        .AddRecords("Customer", "Recent Retail Businesses", [new NavigationRecord(101, "Harbor Roast Cafe Group")]),
    ReportItems = [new NavigationReportItem("CustomerSummary", "Retail Business Summary")]
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
    OrganizationId = "internal"
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

The route is also the starting point for early stub-backed UAT and focused
design or navigation review because it exercises the stub seams through the
real shell asset pipeline.

The current early-UAT reviewer assets live in:

- `docs/session-sets/stubbed-uat/stubbed-uat-checklist.json`
- `docs/session-sets/stubbed-uat/reviewer-guide.md`

## Evidence And Handoff

Later work can rely on these proved expectations:

- report prompting and viewing proof now flows through stub-backed providers in
  the dedicated report test homes
- reusable shell, detail-form, and filter-grid proof now depends on the same
  supported provider seams and auth-context shape
- the app-hosted `/platform/stub` route remains the durable cross-check that
  the stub architecture works outside isolated bUnit harnesses
- the focused accessibility checklist in
  `docs/planning/accessibility-and-stubbing-evidence.md` is the manual follow-up
  path whenever these retained surfaces change

## Design Boundaries

- Keep the stub contracts platform-generic. The future Access metadata CLI is a
  consumer of these seams, not their design center.
- Prefer real provider interfaces and DI registration over bespoke component
  parameters where the platform already has a stable contract.
- Keep role and organization context in claims or auth-state shape that later
  tests and generated apps can reuse directly.
- Treat app demo or UAT usage as consumers of the same contracts, not as a
  reason to add app-specific stub-only APIs.
