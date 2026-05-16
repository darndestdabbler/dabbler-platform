# CRUD Service Template Documentation

## Template Name

`dabbler-crud-service` (shortName in template.json)

## Purpose

Scaffolds a complete service-tier CRUD slice for a single entity:
- **IServiceInterface** — the service contract
- **ServiceImplementation** — EF Core–backed implementation with QBE filtering and sorting
- **ApiController** — ASP.NET Core controller exposing CRUD endpoints

The generated code is self-contained, compiles immediately, and requires zero hand-authored scaffolding glue.

## File Map

| File | Description |
|---|---|
| `Services/I{Entity}Service.cs` | Service interface: `ISupplierService`, etc. |
| `Services/{Entity}Service.cs` | Service implementation, injected with `DbContext` and `ILogger` |
| `Controllers/{EntityPlural}Controller.cs` | REST controller with CRUD endpoints: GET /api/suppliers/{id}, POST /api/suppliers/query, POST /api/suppliers, PUT /api/suppliers/{id}, DELETE /api/suppliers/{id} |

## Parameters

### Required Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `--entity` | string | PascalCase entity name (e.g., `Supplier`). Drives interface/class/file names. |
| `--entity-plural` | string | PascalCase plural form (e.g., `Suppliers`). Drives controller class/file name and DbSet property name. |
| `--api-namespace` | string | Namespace for the service and controller (e.g., `MyApp.Api`). |
| `--dto-namespace` | string | Namespace containing the DTO class (e.g., `MyApp.Shared.DTOs`). |
| `--shared-namespace` | string | Shared project root namespace (e.g., `MyApp.Shared`). |
| `--entity-namespace` | string | Namespace containing the EF entity class (e.g., `MyApp.Shared.Models`). |
| `--db-context-type` | string | DbContext class name (e.g., `AppDbContext`). |
| `--db-context-namespace` | string | Namespace of the DbContext (e.g., `MyApp.Api.Data`). |

### Optional Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `--pk-type` | string | `int` | Primary-key CLR type (e.g., `int`, `long`, `Guid`, `System.Guid`). |
| `--auth-policy-prefix` | string | `{entity}` | Prefix for policy names in `[DabblerEndpointAuthorize]` attributes. If not specified, defaults to the `--entity` value. |
| `--schema` | string | (none) | Optional path to a `.dabbler-entity.json` schema file. The service template does not consume this parameter (it is accepted for symmetry with the page template). |

## Usage

```bash
dotnet new dabbler-crud-service \
  --entity Supplier \
  --entity-plural Suppliers \
  --api-namespace MyApp.Api \
  --dto-namespace MyApp.Shared.DTOs \
  --shared-namespace MyApp.Shared \
  --entity-namespace MyApp.Shared.Models \
  --db-context-type AppDbContext \
  --db-context-namespace MyApp.Api.Data
```

### With Non-Int Primary Key

```bash
dotnet new dabbler-crud-service \
  --entity Invoice \
  --entity-plural Invoices \
  --pk-type Guid \
  --api-namespace MyApp.Api \
  --dto-namespace MyApp.Shared.DTOs \
  --shared-namespace MyApp.Shared \
  --entity-namespace MyApp.Shared.Models \
  --db-context-type AppDbContext \
  --db-context-namespace MyApp.Api.Data
```

### With Custom Auth Policy Prefix

```bash
dotnet new dabbler-crud-service \
  --entity Supplier \
  --entity-plural Suppliers \
  --auth-policy-prefix BackofficeSupplier \
  --api-namespace MyApp.Api \
  --dto-namespace MyApp.Shared.DTOs \
  --shared-namespace MyApp.Shared \
  --entity-namespace MyApp.Shared.Models \
  --db-context-type AppDbContext \
  --db-context-namespace MyApp.Api.Data
```

This generates policy names `QueryBackofficeSupplier`, `GetBackofficeSupplier`, `CreateBackofficeSupplier`, `UpdateBackofficeSupplier`, `DeleteBackofficeSupplier` instead of the default `QuerySupplier`, `GetSupplier`, etc.

## Generated Code Overview

### Service Interface

The generated `ISupplierService` exposes:

```csharp
public interface ISupplierService
{
    Task<SupplierDto> GetAsync(int id);
    Task<GridQueryResponse<SupplierDto>> QueryAsync(GridQueryRequest request);
    Task<SupplierDto> CreateAsync(SupplierDto dto);
    Task UpdateAsync(int id, SupplierDto dto);
    Task DeleteAsync(int id);
}
```

### Service Implementation

The `SupplierService` class:

- Injects `AppDbContext` and `ILogger<SupplierService>`
- Implements all interface members
- Applies QBE filtering and sorting via `query.ApplyQbeFilter(request.Filter)` (the QBE helper from `Dabbler.Api.Querying`)
- Uses `OrderBy().ThenBy()` for multi-column sorts (not repeated `OrderBy`)
- Maps Entity ↔ DTO using reflection-based copy with PK-exclusion safeguards
- Throws `KeyNotFoundException` on missing records

### Controller

The `SuppliersController` exposes five endpoints:

| Endpoint | Method | Route | Purpose |
|----------|--------|-------|---------|
| `GetAsync` | GET | `/api/suppliers/{id}` (constraint derived from `--pk-type`) | Retrieve a single supplier by ID |
| `QueryAsync` | POST | `/api/suppliers/query` | Query suppliers with filtering and sorting |
| `CreateAsync` | POST | `/api/suppliers` | Create a new supplier |
| `UpdateAsync` | PUT | `/api/suppliers/{id}` (constraint derived from `--pk-type`) | Update an existing supplier |
| `DeleteAsync` | DELETE | `/api/suppliers/{id}` (constraint derived from `--pk-type`) | Delete a supplier |

All endpoints are decorated with `[DabblerEndpointAuthorize("PolicyName")]` attributes matching the `--auth-policy-prefix`. Authorization checks occur **before mutations** (per platform conventions).

The route constraint (`:int`, `:long`, `:guid`) is derived from the `--pk-type` parameter.

## Customization Patterns

### Adding a Non-CRUD Endpoint

After generation, you can add custom endpoints to the controller:

```csharp
[HttpPost("{id}/archive")]
[DabblerEndpointAuthorize("ArchiveSupplier")]
public async Task<IActionResult> ArchiveAsync(int id)
{
    var supplier = await _service.GetAsync(id);
    supplier.IsArchived = true;
    await _service.UpdateAsync(id, supplier);
    return Ok(supplier);
}
```

### Custom Validation in the Service

The generated service includes a no-op placeholder for custom validation. Override or extend after generation:

```csharp
private void ValidateCreate(SupplierDto dto)
{
    if (string.IsNullOrWhiteSpace(dto.Name))
        throw new ArgumentException("Name is required.");
    
    if (!dto.ContactEmail.Contains("@"))
        throw new ArgumentException("ContactEmail must be valid.");
}
```

### Alternate Authorization Strategy

If your app uses a different authorization pattern (e.g., role-based rather than policy-based), remove the `[DabblerEndpointAuthorize]` attributes and substitute your own after generation:

```csharp
[Authorize(Roles = "Admin")]
public async Task<SupplierDto> CreateAsync(SupplierDto dto)
{
    // ...
}
```

### Logging and Instrumentation

The service injects `ILogger<SupplierService>` and can log at method boundaries:

```csharp
public async Task<SupplierDto> GetAsync(int id)
{
    _logger.LogInformation("Fetching supplier {Id}", id);
    var entity = await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == id);
    if (entity == null)
        throw new KeyNotFoundException($"Supplier {id} not found.");
    return Map(entity);
}
```

## Integration Points

After generation, four integration points require hand-edits:

1. **DbSet Registration** (in `AppDbContext.cs`):
   ```csharp
   public DbSet<Supplier> Suppliers { get; set; }
   ```

2. **DI Registration** (in `Program.cs`):
   ```csharp
   builder.Services.AddScoped<ISupplierService, SupplierService>();
   ```

3. **AutoMapper Configuration** (in your profiles):
   ```csharp
   CreateMap<Supplier, SupplierDto>().ReverseMap();
   ```

4. **DbContext Connection** (implicit; the DbContext injection handles it):
   The service is injected with the configured DbContext via DI, so no explicit connection string editing is needed.

## Design Decisions

### QBE Filtering

The generated service uses the platform's QBE (Query By Example) → LINQ expression engine via `query.ApplyQbeFilter(request.Filter)`. This approach:

- Provides a unified query mechanism across the application
- Avoids the need for per-property filter code in the service
- Is transport-agnostic (works with the POST `/query` contract)

If you need custom filtering logic for a specific property, extend the service after generation:

```csharp
public async Task<IEnumerable<SupplierDto>> QueryAsync(GridQueryRequest request)
{
    var query = _context.Suppliers.AsQueryable();
    
    // Apply QBE filter for standard properties
    if (request.Filter != null)
        query = query.ApplyQbeFilter(request.Filter);
    
    // Apply custom logic for derived properties
    if (!string.IsNullOrEmpty(request.CustomField))
        query = query.Where(s => /* custom condition */);
    
    return await query.Select(s => Map(s)).ToListAsync();
}
```

### Authorization Model

Generated endpoints use the `[DabblerEndpointAuthorize]` attribute to delegate authorization checks to the platform's authorization middleware. This approach:

- Centralizes policy definitions in the authorization configuration
- Keeps endpoints decoupled from specific role/claim logic
- Supports policy changes without recompiling code

### Reflection-Based Mapping

The service uses reflection-based entity ↔ DTO mapping with PK-exclusion safeguards to prevent:

- **Key injection during creation** — the DTO's Id is not copied when creating a new entity
- **Key mismatch during updates** — the entity's Id is not overwritten by the DTO's Id

This pattern is safe for untrusted input and requires no hand-authored mapping code per entity.

## Performance Considerations

- **Async/await throughout** — all data operations are async to avoid blocking thread pools
- **Multi-column sort via OrderBy().ThenBy()** — each sort column is a separate method call for clarity and correctness
- **IQueryable for filtering** — filters are applied at the database layer (not in memory) when possible

## Testing

The generated service is tested via unit tests that mock the DbContext. Example:

```csharp
[Fact]
public async Task GetAsync_WithValidId_ReturnsSupplier()
{
    var mockContext = new Mock<AppDbContext>();
    var supplier = new Supplier { Id = 1, Name = "Acme Inc." };
    mockContext.Setup(c => c.Suppliers)
        .ReturnsDbSet(new[] { supplier });
    
    var service = new SupplierService(mockContext.Object, _logger);
    var result = await service.GetAsync(1);
    
    Assert.NotNull(result);
    Assert.Equal("Acme Inc.", result.Name);
}
```

## See Also

- [README](README.md)
- [CRUD Page Template Documentation](crud-page-template.md)
- [Non-MVP Roadmap](non-mvp-roadmap.md)
