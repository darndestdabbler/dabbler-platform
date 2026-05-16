# CRUD Page Template Documentation

## Template Name

`dabbler-crud-page` (shortName in template.json)

## Purpose

Scaffolds a complete Blazor Server web-tier CRUD slice for a single entity, including:
- **List Page** (`{EntityPlural}Page.razor`) — the main page with MudDataGrid and DeclarativeFilterPanel
- **List Component** (`{Entity}List.razor`) — reusable list component (grid + filter panel)
- **Detail/Edit Component** (`{Entity}Detail.razor`) — form with `EditSession<T>` dirty tracking
- **Detail Page** (`{Entity}DetailPage.razor`) — router wrapper for detail editing
- **New Page** (`{Entity}NewPage.razor`) — router wrapper for creating new records
- **API Client** (`{Entity}ApiClient.cs`) — typed `HttpClient` wrapper around the service endpoints
- **Grid Data Provider** (`{Entity}GridDataProvider.cs`) — implements `IGridDataProvider<{Entity}Dto>`
- **Scaffolding Extensions** (`{Entity}ScaffoldingExtensions.cs`) — DI registration helper
- **Schema Application Script** (`Apply-{Entity}Schema.ps1`) — post-process script to fill per-property details

The generated code is self-contained, compiles immediately, and the page displays at `/{plural-lowercase}` (e.g., `/suppliers`).

## File Map

| File | Description |
|---|---|
| `Components/Pages/{EntityPlural}/{EntityPlural}Page.razor` | Main list-page route wrapper; calls `{Entity}List` component |
| `Components/Pages/{EntityPlural}/{Entity}DetailPage.razor` | Route wrapper for detail editing; accepts `id` parameter |
| `Components/Pages/{EntityPlural}/{Entity}NewPage.razor` | Route wrapper for creating new records |
| `Components/Pages/{EntityPlural}/{Entity}List.razor` | Reusable list component: MudDataGrid + DeclarativeFilterPanel |
| `Components/Pages/{EntityPlural}/{Entity}Detail.razor` | Reusable detail component: MudForm + `EditSession<T>` |
| `Components/Pages/{EntityPlural}/Apply-{Entity}Schema.ps1` | PowerShell post-process script; idempotent, reads schema file and fills in per-property details |
| `Data/{Entity}ApiClient.cs` | Typed `HttpClient` wrapper; implements CRUD methods |
| `Data/{Entity}GridDataProvider.cs` | Implements `IGridDataProvider<{Entity}Dto>`; bridges grid requests to API calls |
| `Data/{Entity}ScaffoldingExtensions.cs` | DI extension method: `Add{Entity}Scaffolding(this IServiceCollection)` |

## Parameters

### Required Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `--entity` | string | PascalCase entity name (e.g., `Supplier`). Drives type/file/component names. |
| `--entity-plural` | string | PascalCase plural form (e.g., `Suppliers`). Drives folder name and route (lowercased: `/suppliers`). |
| `--web-namespace` | string | Web project root namespace (e.g., `MyApp.Web`). Generated namespaces hang off this root. |
| `--shared-namespace` | string | Shared project root namespace (e.g., `MyApp.Shared`). Accepted for symmetry with the service template. |
| `--dto-namespace` | string | Namespace containing the DTO class (e.g., `MyApp.Shared.DTOs`). |
| `--schema` | string | Path (relative to invocation directory) to a `.dabbler-entity.json` schema file. **Required.** Used by the `Apply-{Entity}Schema.ps1` script to enumerate per-property grid columns, form fields, and filter configuration. |

### Optional Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `--pk-type` | string | `int` | Primary-key CLR type (e.g., `int`, `long`, `Guid`, `System.Guid`). Must match the DTO property type. |
| `--api-base-url-config-key` | string | `Api:BaseUrl` | Configuration key for the API base URL. The generated DI extension (`Add{Entity}Scaffolding`) reads this key to configure the `HttpClient`. |

## Usage

```bash
dotnet new dabbler-crud-page \
  --entity Supplier \
  --entity-plural Suppliers \
  --web-namespace MyApp.Web \
  --shared-namespace MyApp.Shared \
  --dto-namespace MyApp.Shared.DTOs \
  --schema supplier.dabbler-entity.json
```

### With Non-Int Primary Key

```bash
dotnet new dabbler-crud-page \
  --entity Invoice \
  --entity-plural Invoices \
  --pk-type Guid \
  --web-namespace MyApp.Web \
  --shared-namespace MyApp.Shared \
  --dto-namespace MyApp.Shared.DTOs \
  --schema invoice.dabbler-entity.json
```

### With Custom API Base URL Config Key

```bash
dotnet new dabbler-crud-page \
  --entity Supplier \
  --entity-plural Suppliers \
  --api-base-url-config-key "ServiceApi:SupplierEndpoint" \
  --web-namespace MyApp.Web \
  --shared-namespace MyApp.Shared \
  --dto-namespace MyApp.Shared.DTOs \
  --schema supplier.dabbler-entity.json
```

## Generated Code Overview

### Pages

**{EntityPlural}Page.razor** — Main entry point. Displays the title and renders `{Entity}List`:

```razor
@page "/{routeName}"
@attribute [Authorize]

<PageTitle>Suppliers</PageTitle>

<SupplierList />
```

**{Entity}DetailPage.razor** — Detail editing route. Passes the ID route parameter to the detail component. The route constraint (`:int`, `:long`, `:guid`, etc.) is derived from the `--pk-type` parameter:

```razor
@page "/{routeName}/{id:int}"  // Route constraint depends on --pk-type (int by default)
@attribute [Authorize]

<SupplierDetail Id="@id" />
```

For non-int PKs (e.g., `--pk-type Guid`), the constraint changes:
```razor
@page "/{routeName}/{id:guid}"
```

**{Entity}NewPage.razor** — New record creation. Calls detail with a zero/default ID (sentinel for new). The exact value depends on the PK type:

```razor
@page "/{routeName}/new"
@attribute [Authorize]

<SupplierDetail Id="0" />  // For int PK; use 'Guid.Empty' for Guid, '0L' for long, etc.
```

### Components

**{Entity}List.razor** — Contains the MudDataGrid and DeclarativeFilterPanel. Supports:

- Column filtering (per-property filters in the panel)
- Multi-column sorting (click column headers)
- Pagination (MudDataGrid's built-in paging)
- Create/Edit/Delete actions (buttons in the grid)

**{Entity}Detail.razor** — Form for creating/editing. Uses:

- `EditSession<SupplierDto>` for dirty tracking
- `DirtyAwareActionRow` for conditional Save/Cancel buttons
- Per-property form fields (text inputs, checkboxes, etc.)
- Generated via `Apply-{Entity}Schema.ps1` for per-property fields

### Data Layer

**{Entity}ApiClient.cs** — Typed HTTP wrapper:

```csharp
public class SupplierApiClient : HttpClientBase
{
    public SupplierApiClient(HttpClient httpClient) : base(httpClient) { }

    public async Task<SupplierDto> GetAsync(int id)
        => await GetJsonAsync<SupplierDto>($"api/suppliers/{id}");

    public async Task<GridQueryResponse<SupplierDto>> QueryAsync(GridQueryRequest request)
        => await PostJsonAsync<GridQueryResponse<SupplierDto>>("api/suppliers/query", request);

    public async Task<SupplierDto> CreateAsync(SupplierDto dto)
        => await PostJsonAsync<SupplierDto>("api/suppliers", dto);

    public async Task UpdateAsync(int id, SupplierDto dto)
        => await PutJsonAsync($"api/suppliers/{id}", dto);

    public async Task DeleteAsync(int id)
        => await DeleteAsync($"api/suppliers/{id}");
}
```

**{Entity}GridDataProvider.cs** — Implements `IGridDataProvider<SupplierDto>`:

```csharp
public class SupplierGridDataProvider : IGridDataProvider<SupplierDto>
{
    private readonly SupplierApiClient _client;

    public async Task<GridQueryResponse<SupplierDto>> GetDataAsync(GridDataRequest request)
    {
        var queryRequest = new GridQueryRequest { /* mapping */ };
        return await _client.QueryAsync(queryRequest);
    }
}
```

**{Entity}ScaffoldingExtensions.cs** — DI registration:

```csharp
public static class SupplierScaffoldingExtensions
{
    public static IServiceCollection AddSupplierScaffolding(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var baseUrl = configuration["Api:BaseUrl"];
        services.AddHttpClient<SupplierApiClient>(client =>
            client.BaseAddress = new Uri(baseUrl));

        services.AddScoped<IGridDataProvider<SupplierDto>, SupplierGridDataProvider>();
        return services;
    }
}
```

### Schema Application Script

**Apply-{Entity}Schema.ps1** — Post-process script that reads the `.dabbler-entity.json` schema and fills in:

- MudDataGrid columns per `isInGrid` property
- MudForm fields per `isInForm` property
- DeclarativeFilterPanel filter fields per `isFilterable` property
- Column sorting configuration per `isSortable` property
- Read-only marking per `isReadOnly` property

The script is **idempotent** and uses BEGIN/END placeholder markers:

```razor
<!-- BEGIN AutoGenerated Columns -->
<PropertyColumn Property="x => x.Name" Sortable="_supportsSort.Name" />
<!-- END AutoGenerated Columns -->
```

Re-running the script on the same file updates the region without affecting surrounding customizations.

## Customization Patterns

### Custom Column Rendering

After generation, replace a simple column definition with custom rendering:

```razor
<!-- Instead of: -->
<PropertyColumn Property="x => x.Name" />

<!-- Use: -->
<TemplateColumn Title="Name" Sortable>
    @{
        var supplier = context as SupplierDto;
        <MudChip Color="Color.Primary">@supplier.Name</MudChip>
    }
</TemplateColumn>
```

### Custom Form Field Validation

Extend the detail component's form logic after generation:

```csharp
private async Task ValidateNameAsync(string value)
{
    if (string.IsNullOrWhiteSpace(value))
        _modelErrors["Name"] = "Name is required.";
    else if (value.Length > 255)
        _modelErrors["Name"] = "Name must not exceed 255 characters.";
    else
        _modelErrors.Remove("Name");

    _session.NotifyChanged();
}
```

### Custom Filter Fields

Add specialized filter controls to the DeclarativeFilterPanel:

```razor
<MudStack>
    <!-- Auto-generated filters -->
    @foreach (var filter in _filterFields)
    {
        <MudTextField @bind-Value="filter.Value" Label="@filter.Label" />
    }

    <!-- Custom filter -->
    <MudCheckBox @bind-Checked="_showActiveOnly" Label="Active Only" />
</MudStack>
```

### Custom Actions in the Grid

Add buttons beyond the default Create/Edit/Delete:

```razor
<MudIconButton Icon="@Icons.Material.Filled.Archive"
               Title="Archive"
               Size="Size.Small"
               OnClick="@(() => ArchiveAsync(context))" />
```

## Integration Points

After generation, three required integration points need hand-edits:

1. **DI Registration** (in `Program.cs`):
   ```csharp
   builder.Services.AddSupplierScaffolding(builder.Configuration);
   ```

2. **Navigation Menu** (in your layout or nav component):
   ```razor
   <NavLink href="/suppliers">Suppliers</NavLink>
   ```

3. **Schema Application** (one-time, after generation):
   ```powershell
   .\Components\Pages\Suppliers\Apply-SupplierSchema.ps1 -schema supplier.dabbler-entity.json
   ```

### Optional: Seed Data

If you want pre-populated data, add seed logic in your `Program.cs` or a dedicated seed service:

```csharp
context.Suppliers.AddRange(new[] {
    new Supplier { Id = 1, Name = "Acme Inc.", ... },
    new Supplier { Id = 2, Name = "Global Supplies", ... }
});
context.SaveChangesAsync();
```

This is optional and outside the scope of the scaffolding templates.

## Design Decisions

### Form Binding

Generated form fields use explicit `Value`/`ValueChanged` binding (not `@bind-Value`) to enable dirty tracking via `EditSession<T>`:

```razor
<MudTextField Value="@_record.Name"
              ValueChanged="@((string v) => {
                  _record.Name = v;
                  _session.NotifyChanged();
              })"
              Label="Name" />
```

This approach:
- Prevents accidental omission of dirty-change notifications
- Centralizes change tracking in one place
- Supports conditional save/cancel actions

### Route Structure

The page structure uses three separate route pages for clarity:
- **{EntityPlural}Page** (`/{plural}`) — list
- **{Entity}DetailPage** (`/{plural}/{id}`) — edit existing
- **{Entity}NewPage** (`/{plural}/new`) — create new

This separates concerns and makes navigation deterministic.

### API Client Pattern

The `{Entity}ApiClient` extends `HttpClientBase` and uses typed generic methods (`GetJsonAsync<T>`, `PostJsonAsync<T>`, etc.) to:
- Reduce repetition (DRY principle)
- Ensure consistent error handling
- Support easy testing via mocking

### Grid Data Provider

The `IGridDataProvider<SupplierDto>` abstraction decouples the grid UI from the HTTP layer, enabling:
- Easy unit testing (mock the provider)
- Swapping HTTP for gRPC, GraphQL, or other transports
- Caching or other middleware patterns

## Routes

After generation, the following routes are available:

| Route | Method | Handler | Purpose |
|-------|--------|---------|---------|
| `/suppliers` | GET | `{EntityPlural}Page` | List all suppliers |
| `/suppliers/{id}` | GET | `{Entity}DetailPage` | Edit supplier by ID |
| `/suppliers/new` | GET | `{Entity}NewPage` | Create new supplier |

Note: The `/suppliers/new` route must appear **before** `/suppliers/{id}` in the route registration order (Blazor routing is top-to-bottom), so it matches before the catch-all `{id}` route.

## Performance Considerations

- **Lazy-loaded grids** — MudDataGrid only fetches data when the page is rendered
- **Async/await throughout** — HTTP calls and form operations are non-blocking
- **Virtualization optional** — for large grids, enable MudVirtualize to render only visible rows
- **API caching** — the `{Entity}GridDataProvider` can be extended to add response caching

## Testing

The generated components are tested via bUnit. Example:

```csharp
[Fact]
public async Task SupplierList_OnMount_FetchesSuppliers()
{
    var httpMock = new Mock<HttpClient>();
    var suppliers = new[] { new SupplierDto { Id = 1, Name = "Acme" } };
    httpMock.Setup(h => h.PostJsonAsync(...))
        .ReturnsAsync(new GridQueryResponse<SupplierDto> { Data = suppliers });

    var cut = RenderComponent<SupplierList>(
        parameter => parameter.Add(p => p.Client, httpMock.Object)
    );

    Assert.NotNull(cut.Find("table"));
    Assert.Contains("Acme", cut.Markup);
}
```

## See Also

- [README](README.md)
- [CRUD Service Template Documentation](crud-service-template.md)
- [Non-MVP Roadmap](non-mvp-roadmap.md)
