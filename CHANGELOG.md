# Changelog

All `Dabbler.*` packages, the `Dabbler.Templates` template pack and the tools
share one version (`DabblerVersion` in `Directory.Build.props`) and are
published to nuget.org together.

## 1.0.1

- No API changes since `1.0.0`.
- The `DabblerPdf` dependency moves from `1.1.2` to `1.1.3`, which changes no
  DabblerPdf API either.

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
