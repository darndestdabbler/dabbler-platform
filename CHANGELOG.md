# Changelog

All `Dabbler.*` packages, the `Dabbler.Templates` template pack and the tools
share one version (`DabblerVersion` in `Directory.Build.props`) and are
published to nuget.org together.

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
