# Changelog

All `Dabbler.*` packages, the `Dabbler.Templates` template pack and the tools
share one version (`DabblerVersion` in `Directory.Build.props`) and are
published to nuget.org together.

## 2.0.0

Breaking changes:

- `MasterDetailExpansionShell`, its context types (`DetailsContext<TEntity>`,
  `LinkContext<TLink>`, `DetailsActionsContext<TEntity, TLink>`,
  `SaveRequest<TEntity, TLink>`, `SaveAndUseResult<TEntity, TLink>`,
  `TargetEditability`) and `PickOrAddDialog` moved from
  `Dabbler.Security.Blazor` (namespace
  `Dabbler.Security.Blazor.Components.Admin.Shared`) to `Dabbler.Components`
  (namespace `Dabbler.Components.MasterDetail`). Migration: reference
  `Dabbler.Components` and replace `@using
  Dabbler.Security.Blazor.Components.Admin.Shared` with `@using
  Dabbler.Components.MasterDetail`.
- `ShowAddAction` on the shell and on `PickOrAddDialog` now renders an Add
  button (raising `OnAddRequested`, or `OnAdd` on the dialog) and defaults to
  `false`; `Mode`, `ShellMode` and the shell's and the dialog's
  `OnRowSelected` are removed. Migration: set `ShowAddAction="true"` where an
  Add button is wanted, and delete any `Mode` or `OnRowSelected` attribute.
- `DeclarativeFilterPanel` emits the `FilterOperators` constants
  (`Contains`, `Equals`, `GreaterThanOrEqual`, `LessThanOrEqual`), not
  `"contains"`, `"eq"`, `"gte"` and `"lte"`. Migration: compare a
  `FilterDescriptor.Operator` with the `FilterOperators` constants instead of
  those literals.
- `Dabbler.Api.Querying` throws `GridQueryException` for an unknown property
  in a filter or a sort, an unknown operator, an operator the property's type
  does not support and a value that cannot be converted, where it used to skip
  the filter. Migration: catch `GridQueryException` (it derives from
  `ArgumentException`) and answer it with `400 Bad Request`.

Added:

- `AddLabel` on `MasterDetailExpansionShell`, the Add button's text (default
  `"Add"`).
- `DateOnly`, `TimeOnly`, `DateTimeOffset`, `Guid` and enum filters, and the
  `In` operator for an array value, in `Dabbler.Api.Querying`.
- `GridQueryException` in `Dabbler.Api.Querying`.
- `DownloadFileAsync`, an `IJSRuntime` extension in `Dabbler.Components` that
  saves bytes in the browser as a named file.
- `ReportFonts` (`Dabbler.Reports.Core`) and `AddDabblerReportFonts`
  (`Dabbler.Reports.Blazor`), which register Unicode TrueType font families
  for report text.

Changed:

- PDF report text keeps runs of spaces, breaks a word wider than its field,
  and is measured with the font and size it is drawn with.
- `DabblerReportPage` measures `CanGrow` and `CanShrink` fields, so their
  bands grow and shrink in the shipped page.
- The `DabblerPdf` dependency moves from `1.1.2` to `1.3.0`.

## 1.0.0

- No API changes since `0.2.0`.
- The first stable release. The operator stands behind the twelve packages
  listed in `tools/pack.ps1`, the gates that guard them (the `unit` suite,
  PublicSurface, Packaging.Smoke and the Playwright E2E gate) and
  `docs/MAINTAINING.md`, which says where each gate lives and how a release is
  made.

## 0.2.0

- This version was never published to nuget.org: `1.0.0` is the first
  release after `0.1.0-preview.1`.

Breaking changes:

- The report expression engine moved from `Dabbler.Components` to
  `Dabbler.Reports.Core`. Its namespace is now
  `Dabbler.Reports.Core.Expressions` (was `Dabbler.Components.Expressions`).
- MudBlazor 9 is the floor (the platform is built against 9.12.0).
  `GridDataProviderExtensions.ToServerDataFunc` now returns MudDataGrid's
  `ServerData` delegate, `Func<GridState<T>, CancellationToken,
  Task<GridData<T>>>`, and passes the grid's `CancellationToken` to
  `IGridDataProvider<T>.FetchAsync`. Implementations of `IGridDataProvider<T>`
  should honour the token.
- `Dabbler.Components.Compat` is retired, with no replacement package. The
  `Dabbler.Platform.Stubs.Components`, `Dabbler.Platform.Stubs.Reports` and
  `Dabbler.Platform.Stubs.Security` providers are retired and folded into
  `Dabbler.Platform.Stubs`. They keep the `Dabbler.Platform.Stubs` namespace,
  so replacing those three references with one to `Dabbler.Platform.Stubs` is
  the only change needed.

## 0.1.0-preview.2

- This version was never published to nuget.org: `1.0.0` is the first
  release after `0.1.0-preview.1`.
- No API changes.
- `Dabbler.CrudSlice.Tool` (the `dabbler-crud-slice` .NET tool) is published
  to nuget.org for the first time. Install it at the same version as the
  templates it invokes:

  ```bash
  dotnet tool install --global Dabbler.CrudSlice.Tool --version 0.1.0-preview.2
  dotnet new install Dabbler.Templates::0.1.0-preview.2
  ```

## 0.1.0-preview.1

- First preview release of the `Dabbler.*` packages.
