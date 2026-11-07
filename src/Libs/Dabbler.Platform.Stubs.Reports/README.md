# Dabbler.Platform.Stubs.Reports

Stub report-data, artifact-catalog, and usage-tracking providers for **Dabbler.Reports** and **Dabbler.Components** bUnit and demo-host consumers.

**Note:** This package carries a transitive dependency on **DabblerPdf** (LGPL-2.1-only OR MPL-2.0) via `Dabbler.Reports.Core`.
If you only need Components stubs without DabblerPdf, install `Dabbler.Platform.Stubs.Components` instead.

## Audience

External adopters and integrated-source users who write bUnit tests or demo hosts for Dabbler.Reports-powered pages.

## Prerequisites

- .NET 10
- `Dabbler.Reports.Core`
- `Dabbler.Components` (for artifact-catalog and usage-tracking types)

## Install

Until preview ships: reference `src/Libs/Dabbler.Platform.Stubs.Reports/` from your test or demo project.
After preview ships: `dotnet add package Dabbler.Platform.Stubs.Reports --prerelease`.

## Minimal registration

```csharp
services.AddStubReportDataProvider(provider =>
    provider.AddQueryResult("SELECT ...", myDataTable));

services.AddStubArtifactCatalogProvider(catalogEntries);
services.AddStubUsageTrackingProvider(usageRecords);
```

## Related packages

- `Dabbler.Reports.Core` — the library being stubbed
- `Dabbler.Platform.Stubs` — meta-package; install this for full-platform stub coverage
- `Dabbler.Platform.Stubs.Components` — Components stubs (no DabblerPdf)
- `Dabbler.Platform.Stubs.Security` — authentication-state stub

## Warnings

DabblerPdf is LGPL-2.1-only OR MPL-2.0. See `THIRD-PARTY-NOTICES.txt` in this package for disclosure details.
