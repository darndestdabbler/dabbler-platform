# Dabbler.Platform.Stubs.Components

Stub grid-data, combo-box, entity-metadata, and navigation providers for **Dabbler.Components** bUnit and demo-host consumers.

Install this sub-package when you want stub-backed Components wiring **without** pulling DabblerPdf transitively.
Full-platform stub wiring is available via `Dabbler.Platform.Stubs` (the meta-package).

## Audience

External adopters and integrated-source users who write bUnit tests or demo hosts for Dabbler.Components-powered pages.

## Prerequisites

- .NET 10
- `Dabbler.Components`

## Install

Until preview ships: reference `src/Libs/Dabbler.Platform.Stubs.Components/` from your test or demo project.
After preview ships: `dotnet add package Dabbler.Platform.Stubs.Components --prerelease`.

## Minimal registration

```csharp
// bUnit shortcut (via Dabbler.Platform.Stubs meta-package):
ctx.AddDabblerStubs(definition);

// Focused sub-package wiring:
services.AddStubGridDataProvider<Customer>(customers);
services.AddStubEntityMetadataProvider(definition.Entities);
services.AddStubNavigationDataProvider(definition.Navigation);
```

## Related packages

- `Dabbler.Components` — the library being stubbed
- `Dabbler.Platform.Stubs` — meta-package; install this for full-platform stub coverage
- `Dabbler.Platform.Stubs.Reports` — report + catalog + usage-tracking stubs
- `Dabbler.Platform.Stubs.Security` — authentication-state stub
