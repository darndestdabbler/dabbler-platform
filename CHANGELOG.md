# Changelog

All `Dabbler.*` packages, the `Dabbler.Templates` template pack and the tools
share one version (`DabblerVersion` in `Directory.Build.props`) and are
published to nuget.org together.

## 0.2.0

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
