# Dabbler.Templates

`dotnet new` templates for building on the Dabbler platform. The generated
projects reference the published `Dabbler.*` packages.

| Short name | Kind | What it creates |
|---|---|---|
| `dabbler-app` | Solution | Blazor Server web app, Web API, shared contracts library and bUnit tests, with security, one sample CRUD entity, one sample report and a Docker compose stack. |
| `dabbler-min` | Project | Single Blazor Server project with Dabbler Security only. |
| `dabbler-crud-service` | Item | A CRUD service, its EF Core implementation, DI helper and `POST /query` controller for one entity. |
| `dabbler-crud-page` | Item | The Blazor list and detail pages, typed `HttpClient` and grid data provider for one entity. |

## Install and use

```bash
dotnet new install Dabbler.Templates
dotnet new dabbler-app -n MyApp
```

The `dabbler-crud-slice` tool (`Dabbler.CrudSlice.Tool`) drives the two
item templates for you. See
[`docs/getting-started.md`](https://github.com/darndestdabbler/dabbler-platform/blob/master/docs/getting-started.md)
for a walkthrough.

## If this library stops being maintained

These templates are MIT-licensed, and their source is in
[`templates/Dabbler.Templates`](https://github.com/darndestdabbler/dabbler-platform/tree/master/templates/Dabbler.Templates).
The code a template generates is already yours and needs nothing from the
template package. If the templates stop being maintained, copy their source
into your own repository, pack or install them from your copy, and own them
from there. Keep the MIT license notice with the copied code.
