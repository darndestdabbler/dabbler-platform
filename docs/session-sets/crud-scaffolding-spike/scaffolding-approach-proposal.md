# Scaffolding Approach Proposal

> **Purpose:** Design a code-generation scaffolding system for Dabbler CRUD pages, service endpoints, and feature slices that outputs code indistinguishable from what a careful developer would write by hand.

> **Status:** Proposal ready for tri-provider review (Session 3).

> **Scope:** This document resolves all open design questions from the spike spec and proposes the MVP shape for the first implementation set.

---

## Executive Summary

The scaffolding system will be a **`dotnet new` template suite** (not a separate tool, not a source generator) offering **three granularities**:
- **Granularity 1 (MVP):** Single Razor CRUD page (list + detail)
- **Granularity 2 (MVP):** Service endpoint trio (controller + service interface + implementation)
- **Granularity 3 (Post-MVP):** Full feature slice (both granularities + data provider + API client + DI wiring + smoke tests + nav/security)

This shape aligns with the "composition over configuration" principle: the generator produces explicit, customizable code the developer owns, not a runtime configuration composite. Developers run the template once, customize the output, and commit it—no regeneration story required. The implementation set will ship Granularities 1 & 2, deferring 3 to a later set.

---

## Design Question Resolutions

### 1. Scaffolding Granularities

**Decision:** Three granularities, nested, with Granularities 1 & 2 shipping in MVP.

**Rationale:**
- **Granularity 1 (Single Razor page):** Handles the UI layer (ProductList.razor + ProductDetail.razor). Developers customize complex column rendering, form validation, and entity-specific UI actions.
- **Granularity 2 (Service endpoint trio):** Handles the API and service layers (IProductService, ProductService, ProductsController). Developers customize business logic, validation rules, and non-standard endpoints (e.g., PATCH /products/{id}/availability).
- **Granularity 3 (Full slice):** Wraps both, adding infrastructure (ProductGridDataProvider, ProductApiClient, DI registration, smoke tests, navigation, security policy definitions).

**Why this granularity breakdown:**
- The inventory shows that pages and endpoints follow distinct patterns. Developers often work on one layer at a time; decoupling them reduces regeneration friction.
- Granularity 1 alone is valuable for the UI team iterating on forms.
- Granularities 1 + 2 together deliver a complete CRUD feature; Granularity 3 is the "deluxe package" that includes tests and wiring.
- The composition principle demands that generated code be explicit and customizable. Three granularities mean smaller, more focused outputs that developers understand immediately.

**Non-goals:**
- No single "super-scaffold" that generates everything at once (defeats customization).
- No reflection-driven UI at runtime (stays explicit).
- No replacement for MudDataGrid column definitions (columns are generated once, not at render time).

### 2. CLI Shape: `dotnet new` Template Suite

**Decision:** Implement as `dotnet new` templates in `Dabbler.Templates`, not a separate tool.

**Rationale:**
- **Consistency:** Developers already use `dotnet new` to scaffold projects. CRUD scaffolding is a natural extension.
- **Simplicity:** No new tool installation or separate CLI invocation. `dotnet new` is built-in.
- **Discoverability:** `dotnet new list` shows all available templates, including CRUD scaffolds.
- **Customization:** `dotnet new` supports `.template.config/template.json` for variables (EntityName, Route, Namespace, etc.), eliminating the need for a custom CLI parser.

**Example usage:**
```bash
dotnet new dabbler-crud-page --entity Customer --namespace AcmeCoffeeOnline.Web.Pages.Customers
dotnet new dabbler-crud-service --entity Customer --namespace AcmeCoffeeOnline.Api --dto-namespace AcmeCoffeeOnline.Shared.DTOs
dotnet new dabbler-crud-slice --entity Customer --namespace AcmeCoffeeOnline --web-namespace AcmeCoffeeOnline.Web --api-namespace AcmeCoffeeOnline.Api
```

**Rejected alternatives:**
- **Roslyn source generator:** Generates code at build time, making customization harder (developers must regenerate to change, or maintain parallel hand-edits). Not suitable for templates that are meant to be customized once and owned.
- **Separate `dotnet tool`:** Adds a new installation step and becomes less discoverable than `dotnet new`.

### 3. Filter Representation: QBE, Not OData

**Decision:** Use the platform's existing QBE filter model (CompositeFilterDescriptor + DeclarativeFilterPanel). No OData support in MVP.

**Rationale:**
- **Existing:** The platform already ships `DeclarativeFilterPanel<TEntity>` with a mature QBE implementation (text, enum, bool, numeric, date, foreign-key filters).
- **Consistency:** All CRUD pages in the demo app (ProductList, CustomerList, OrderList) use QBE. Generated pages should match.
- **Simplicity:** QBE is simpler for the client-side UI (no query syntax learning curve) and the server-side implementation (maps directly to LINQ predicates).
- **Avoids variation axis trap:** The prior runtime composite (`MasterDetailsConfig<TEntity>`) was rejected because it tried to support multiple filter representations (QBE, OData, custom). The spike spec explicitly calls for "validation against tri-provider review and documentation of reasoning so the implementation set does not relitigate."

**OData—Why not:**
- Creates a second filter representation competing with QBE.
- Requires more complex server-side parsing and validation.
- Not currently used anywhere in the platform.
- Deferring OData is a conscious trade-off for simplicity in MVP. If OData demand emerges, it lands in a future set after the platform has proven the value of QBE-only CRUD scaffolding.

**Documented non-goal:** The generated endpoints will NOT include OData support. If a future set wishes to add it, the implementation must choose one representation and stick with it (QBE or OData), not both. This decision is documented here to prevent the implementation set from drifting back to "maybe we should support both."

### 4. Idempotency and Regeneration: No Regeneration, Generate Once and Own

**Decision:** The generator is a **one-time scaffold tool**, not a re-runnable code formatter. Once generated, the developer owns the code.

**Rationale:**
- **Composition principle:** Generated code is indistinguishable from hand-written code. Developers should commit it, customize it, and own it—just like they own any other code.
- **Avoids merge conflicts:** Re-running a generator on customized code either overwrites changes (data loss) or requires complex merging (friction). Both are anti-patterns.
- **Explicit customization:** Pages and endpoints are customized frequently (validation logic, new fields, entity-specific actions). The generated code is the starting point, not the template.

**Non-goal:** No `--regenerate` flag, no merge strategy, no detection of "safe zones" within the generated file.

**Implication:** If a developer needs to re-scaffold (e.g., add a new field), they edit the generated file manually or delete and re-run the template (accepting that they'll need to re-apply their customizations from the prior version).

### 5. Entity Discovery: C# Class Reflection (Primary) + Schema File (Future)

**Decision:** MVP uses **C# class reflection** over a loaded assembly. Future sets may add a schema-file option.

**Rationale:**
- **Simplicity:** The developer has already written (or the platform already defines) the `ProductDto` and `Product` classes. The generator reads them from the compiled assembly via reflection.
- **Single source of truth:** The class definition IS the schema. No separate `.json` schema file to keep in sync.
- **Works with EF Core:** The generator can also introspect `DbSet<Product>` via the DbContext to discover all properties and their types.
- **Familiar to .NET developers:** Reflection-over-metadata is a native .NET pattern.

**How it works (MVP):**
1. The developer has already created `Product` (EF entity) and `ProductDto` (DTO).
2. They run: `dotnet new dabbler-crud-service --entity Customer --dto CustomerDto`
3. The template's post-generation script (via `ITemplateOperationProvider`) or a setup hook loads the project's assembly, reflects over `Customer` and `CustomerDto`, and emits the service interface/implementation, controller, and tests.

**Alternative (deferred):** A schema-file option (e.g., `--schema customers.dabbler-entity.json`) would allow scaffolding without a compiled assembly. This is valuable for teams who want to design the entity shape first, then code-gen without building. Deferred to a follow-on set after the MVP validates the core scaffold workflow.

**DbContext introspection (future):** A more automated option would introspect the AppDbContext directly to discover the entity graph. This is powerful but requires the DbContext to be in the same solution, which is not always the case. Deferred.

### 6. Non-Goals (Explicit)

- **No runtime configuration composite.** The generator output is static code, not a config object that drives behavior at runtime.
- **No reflection-driven UI at runtime.** Generated page components do not discover properties or fields at render time; they emit explicit PropertyColumn and form field definitions.
- **No replacement for MudDataGrid column definitions.** Columns are generated once at scaffold time. Complex rendering (status badges, formatted values) is hand-customized.
- **No OData support in MVP.** QBE only. OData is a future consideration, not part of Granularities 1 & 2.
- **No regeneration or re-running.** Generate once, own the code. No `--regenerate` flag or automatic re-sync.
- **No multi-entity slices in MVP.** Granularity 3 (full slice) handles one entity at a time. Master-detail or one-to-many relationship scaffolding is deferred.
- **No automatic test generation beyond smoke tests.** Developers write their own business-logic and integration tests.

---

## MVP Shipping Shape

### What Lands in Set `crud-scaffolding` (Implementation Set 1)

**Templates:**
1. `dabbler-crud-page` — Granularity 1 (list + detail pages)
2. `dabbler-crud-service` — Granularity 2 (service + controller)

**Delivery:**
- `Dabbler.Templates` project gets two new template folders: `templates/dabbler-crud-page/` and `templates/dabbler-crud-service/`.
- Each folder contains:
  - `.template.config/template.json` (variables: EntityName, Namespace, DtoNamespace, Route, PluralName, etc.)
  - Generated file templates (`.razor`, `.cs`, `.csproj.user` or a hook for DI registration).
- Tooling: A post-generation hook (`ITemplateOperationProvider`) that calls reflection to discover the entity and DTO, then fills in method bodies and field bindings.

**Documentation:**
- `docs/scaffolding-templates/crud-page-template.md` — Usage guide, customization patterns, examples.
- `docs/scaffolding-templates/crud-service-template.md` — Service implementation customization, business logic patterns.
- Inline comments in generated code (minimal; explain WHY, not WHAT).

### What's Deferred (Post-MVP)

**Granularity 3 (full slice):**
- Requires Granularities 1 & 2 to be proven via real usage in implementation set.
- Adds ProductGridDataProvider, ProductApiClient, DI registration, smoke tests, navigation, security policy definitions.
- Dedicated set after `crud-scaffolding` completes.

**OData variant:**
- A separate template or configuration option supporting OData queries (e.g., `--filter-mode odata`).
- Requires reconciliation of how OData coexists with (or replaces) QBE in the generated endpoints.
- Deferred pending real demand signals.

**Multi-entity slices (one-to-many, master-detail):**
- Customer → Orders relationship scaffolding.
- Product → Variants nested grid scaffolding.
- Deferred; first sets focus on single-entity CRUD.

**Source generator alternative:**
- An alternative implementation path where code is generated at build time via Roslyn, not at template-time.
- Deferred pending evaluation of regeneration friction vs. build-time convenience.

---

## Generated-Code Samples

### Granularity 1 Sample: ProductList.razor (Generated)

```razor
@page "/products"
@attribute [DabblerPageAuthorize("/products")]
@using AcmeCoffeeOnline.Shared.DTOs
@using AcmeCoffeeOnline.Shared.Models
@using Dabbler.Components.Data
@using MudBlazor

<PageTitle>Products — AcmeCoffeeDistributors</PageTitle>

<MudGrid Class="ga-4">
    <MudItem xs="12">
        <DeclarativeFilterPanel 
            TEntity="ProductDto"
            Config="_filterConfig"
            FilterChanged="@OnFilterChangedAsync"
            InitialValues="@_initialFilterValues" />
    </MudItem>
    
    <MudItem xs="12">
        <MudToolBar>
            <MudText Typo="Typo.h6">@(_grid?.GetRowsPerPage() == 0 ? "Loading..." : $"Total: {_totalCount}")</MudText>
            <MudSpacer />
            <MudButton 
                Variant="Variant.Filled" 
                Color="Color.Primary"
                Href="/products/new"
                StartIcon="@Icons.Material.Filled.Add">
                New Product
            </MudButton>
        </MudToolBar>
    </MudItem>

    @if (_error != null)
    {
        <MudItem xs="12">
            <MudAlert Severity="Severity.Error" CloseButton="true" OnClose="@(() => _error = null)">
                @_error
            </MudAlert>
        </MudItem>
    }

    <MudItem xs="12">
        <MudDataGrid 
            @ref="_grid"
            ServerData="@ServerDataFuncAsync"
            Hover="true"
            Dense="true"
            Bordered="true"
            ReadOnly="true"
            OnRowClick="@HandleRowClickAsync"
            RowsPerPage="25">
            <Columns>
                <PropertyColumn Property="x => x.Id" Title="ID" />
                <PropertyColumn Property="x => x.Name" Title="Name" />
                <PropertyColumn Property="x => x.Price" Title="Price" Format="C2" />
                <PropertyColumn Property="x => x.IsAvailable" Title="Available">
                    <CellTemplate>
                        <MudCheckBox Value="@context.Item.IsAvailable" ReadOnly="true" />
                    </CellTemplate>
                </PropertyColumn>
            </Columns>
        </MudDataGrid>
    </MudItem>
</MudGrid>

@code {
    [Parameter, SupplyParameterFromQuery]
    public string? Search { get; set; }

    [Inject]
    private IGridDataProvider<ProductDto> DataProvider { get; set; } = default!;

    [Inject]
    private NavigationManager Nav { get; set; } = default!;

    private MudDataGrid<ProductDto> _grid = default!;
    private CompositeFilterDescriptor? _filter;
    private Dictionary<string, object?> _initialFilterValues = new();
    private string? _error;
    private int _totalCount;

    protected override void OnInitialized()
    {
        if (!string.IsNullOrEmpty(Search))
        {
            _initialFilterValues["Name"] = Search;
        }
    }

    private async Task<GridData<ProductDto>> ServerDataFuncAsync(GridState<ProductDto> state)
    {
        try
        {
            var request = new GridDataRequest
            {
                PageIndex = state.Page,
                PageSize = state.PageSize,
                SortFields = state.SortDefinitions.Select(s => new SortField { Property = s.SortBy, Order = s.Descending ? SortOrder.Descending : SortOrder.Ascending }).ToArray(),
                Filter = _filter
            };

            var response = await DataProvider.FetchAsync(request);
            _totalCount = response.TotalCount;
            _error = null;
            return new GridData<ProductDto> { Items = response.Items, TotalItems = response.TotalCount };
        }
        catch (Exception ex)
        {
            _error = $"Failed to load products: {ex.Message}";
            return new GridData<ProductDto> { Items = Array.Empty<ProductDto>(), TotalItems = 0 };
        }
    }

    private async Task OnFilterChangedAsync(CompositeFilterDescriptor? filter)
    {
        _filter = filter;
        if (_grid != null)
        {
            await _grid.ReloadServerData();
        }
    }

    private Task HandleRowClickAsync(DataGridRowClickEventArgs<ProductDto> args)
    {
        Nav.NavigateTo($"/products/{args.Item.Id}");
        return Task.CompletedTask;
    }

    // Filter configuration (developer customizes this)
    private FilterConfig<ProductDto> _filterConfig = new()
    {
        Fields = new()
        {
            new TextFilterField { Property = nameof(ProductDto.Name), Label = "Name", Placeholder = "Search by name" },
            new BoolFilterField { Property = nameof(ProductDto.IsAvailable), Label = "Available" },
            new NumericFilterField { Property = nameof(ProductDto.Price), Label = "Price", AllowRange = true }
        }
    };
}
```

**Customization points:**
- `_filterConfig` — Add/remove fields, change labels.
- Grid columns — Add rich rendering (status badges, icons), remove columns.
- Toolbar — Add custom buttons, change button labels.
- Error handling — Customize error messages or log them.

---

### Granularity 1 Sample: ProductDetail.razor (Generated)

```razor
@page "/products/{Id:int}"
@page "/products/new"
@attribute [DabblerPageAuthorize("/products")]
@using AcmeCoffeeOnline.Shared.DTOs
@using Dabbler.Components.MasterDetail
@using MudBlazor

<PageTitle>@(_session?.Live?.Name ?? "New Product") — AcmeCoffeeDistributors</PageTitle>

@if (_loading)
{
    <MudContainer MaxWidth="MaxWidth.Small" Class="d-flex justify-center align-center" Style="min-height: 400px;">
        <MudProgressCircular Indeterminate="true" />
    </MudContainer>
}
else if (_session != null)
{
    <MudForm @ref="_form" @bind-IsValid="_formValid">
        <MudGrid>
            <MudItem xs="12">
                <DirtyAwareActionRow
                    IsDirty="@_session.IsDirty"
                    Saving="@_saving"
                    ShowCancel="false"
                    OnSave="@HandleSaveAsync"
                    CanDelete="@(Id.HasValue)"
                    OnDelete="@HandleDeleteAsync"
                    CustomActions="@CustomActionsFragment"
                    CustomActionsPosition="ActionRowSlot.End" />
            </MudItem>

            <MudItem xs="12" sm="6">
                <MudTextField
                    @bind-Value="_record.Name"
                    Label="Name"
                    Placeholder="Product name"
                    Required="true"
                    RequiredError="Name is required"
                    For="@(() => _record.Name)"
                    Immediate="true"
                    Validation="@(new Func<string, IEnumerable<string>>(ValidateName))"
                    ValueChanged="@(() => _session.NotifyChanged())" />
            </MudItem>

            <MudItem xs="12" sm="6">
                <MudNumericField
                    @bind-Value="_record.Price"
                    Label="Price"
                    Format="C2"
                    Required="true"
                    RequiredError="Price is required"
                    For="@(() => _record.Price)"
                    Min="0"
                    ValueChanged="@(() => _session.NotifyChanged())" />
            </MudItem>

            <MudItem xs="12" sm="6">
                <MudCheckBox
                    @bind-Value="_record.IsAvailable"
                    Label="Available for Sale"
                    For="@(() => _record.IsAvailable)"
                    ValueChanged="@(() => _session.NotifyChanged())" />
            </MudItem>
        </MudGrid>
    </MudForm>
}
else
{
    <MudAlert Severity="Severity.Error">Failed to load product.</MudAlert>
}

@code {
    [Parameter]
    public int? Id { get; set; }

    [Inject]
    private ProductApiClient ApiClient { get; set; } = default!;

    [Inject]
    private NavigationManager Nav { get; set; } = default!;

    private EditSession<ProductDto> _session = default!;
    private MudForm _form = default!;
    private bool _loading = true;
    private bool _saving = false;
    private bool _formValid = true;

    private ProductDto _record => _session.Live;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            ProductDto product;
            if (Id.HasValue)
            {
                product = await ApiClient.GetByIdAsync(Id.Value) ?? new ProductDto();
            }
            else
            {
                product = new ProductDto();
            }

            _session = new EditSession<ProductDto>(product);
            _loading = false;
        }
        catch (Exception ex)
        {
            _loading = false;
            // Log error, show user message
        }
    }

    private async Task HandleSaveAsync()
    {
        if (_form is not null && !await _form.Validate())
            return;

        _saving = true;
        try
        {
            ProductDto saved;
            if (Id.HasValue)
            {
                saved = await ApiClient.UpdateAsync(Id.Value, _record);
            }
            else
            {
                saved = await ApiClient.CreateAsync(_record);
            }

            _session.CommitFrom(saved);
            _saving = false;

            if (!Id.HasValue)
            {
                Nav.NavigateTo($"/products/{saved.Id}");
            }
        }
        catch (Exception ex)
        {
            _saving = false;
            // Log error, show user message
        }
    }

    private async Task HandleDeleteAsync()
    {
        if (!Id.HasValue) return;

        _saving = true;
        try
        {
            await ApiClient.DeleteAsync(Id.Value);
            Nav.NavigateTo("/products");
        }
        catch (Exception ex)
        {
            _saving = false;
            // Log error, show user message
        }
    }

    private IEnumerable<string> ValidateName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            yield return "Name is required";
        if (value?.Length > 200)
            yield return "Name cannot exceed 200 characters";
    }

    private RenderFragment CustomActionsFragment => builder =>
    {
        builder.OpenComponent<MudButton>(0);
        builder.AddAttribute(1, "Variant", Variant.Text);
        builder.AddAttribute(2, "OnClick", EventCallback.Factory.Create(this, () => _session.Undo()));
        builder.AddAttribute(3, "StartIcon", Icons.Material.Filled.Undo);
        builder.AddAttribute(4, "ChildContent", new RenderFragment(b => b.AddContent(5, "Undo")));
        builder.CloseComponent();
    };
}
```

**Customization points:**
- Add/remove fields (MudTextField, MudNumericField, etc.).
- Custom validation logic (ValidateName method).
- Cross-field validation (compare two fields, check ranges).
- Entity-specific actions in CustomActionsFragment (Deactivate, Archive, etc.).

---

### Granularity 2 Sample: Service and Controller (Generated)

**IProductService.cs:**
```csharp
namespace AcmeCoffeeOnline.Api.Services
{
    public interface IProductService
    {
        Task<PagedResponse<ProductDto>> GetAsync(PagedRequest request, CancellationToken cancellationToken = default);
        Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<ProductDto> CreateAsync(ProductDto dto, CancellationToken cancellationToken = default);
        Task<ProductDto?> UpdateAsync(int id, ProductDto dto, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    }
}
```

**ProductService.cs:**
```csharp
namespace AcmeCoffeeOnline.Api.Services
{
    public class ProductService : IProductService
    {
        private readonly AppDbContext _db;
        private readonly IMapper _mapper;

        public ProductService(AppDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<PagedResponse<ProductDto>> GetAsync(PagedRequest request, CancellationToken cancellationToken = default)
        {
            var query = _db.Products.AsNoTracking();

            // Apply filter if present
            if (request.Filter?.Filters is not null && request.Filter.Filters.Count > 0)
            {
                // TODO: Apply filter expression here (use QBE filter engine)
            }

            // Apply sort
            foreach (var sort in request.SortFields ?? Array.Empty<SortField>())
            {
                query = sort.Order == SortOrder.Ascending
                    ? query.OrderBy(e => EF.Property<object>(e, sort.Property))
                    : query.OrderByDescending(e => EF.Property<object>(e, sort.Property));
            }

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip(request.PageIndex * request.PageSize)
                .Take(request.PageSize)
                .ProjectTo<ProductDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            return new PagedResponse<ProductDto> { Items = items.ToArray(), TotalCount = total };
        }

        public async Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var product = await _db.Products.FindAsync(new object[] { id }, cancellationToken);
            return product is null ? null : _mapper.Map<ProductDto>(product);
        }

        public async Task<ProductDto> CreateAsync(ProductDto dto, CancellationToken cancellationToken = default)
        {
            var product = _mapper.Map<Product>(dto);
            _db.Products.Add(product);
            await _db.SaveChangesAsync(cancellationToken);
            return _mapper.Map<ProductDto>(product);
        }

        public async Task<ProductDto?> UpdateAsync(int id, ProductDto dto, CancellationToken cancellationToken = default)
        {
            var product = await _db.Products.FindAsync(new object[] { id }, cancellationToken);
            if (product is null) return null;

            _mapper.Map(dto, product);
            _db.Products.Update(product);
            await _db.SaveChangesAsync(cancellationToken);

            return _mapper.Map<ProductDto>(product);
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var product = await _db.Products.FindAsync(new object[] { id }, cancellationToken);
            if (product is null) return false;

            _db.Products.Remove(product);
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
```

**ProductsController.cs:**
```csharp
namespace AcmeCoffeeOnline.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _service;
        private readonly IPermissionService _permissionService;
        private readonly ICurrentUserService _currentUserService;

        public ProductsController(IProductService service, IPermissionService permissionService, ICurrentUserService currentUserService)
        {
            _service = service;
            _permissionService = permissionService;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        [DabblerEndpointAuthorize("GetProducts")]
        public async Task<ActionResult<PagedResponse<ProductDto>>> GetAsync([FromQuery] PagedRequest request, CancellationToken cancellationToken = default)
        {
            var response = await _service.GetAsync(request, cancellationToken);
            
            if (!_currentUserService.TryGetUserId(out var userId))
                return Unauthorized();

            foreach (var item in response.Items)
            {
                await _permissionService.ApplyFieldPermissionsAsync(item, userId, nameof(Product));
            }

            return Ok(response);
        }

        [HttpGet("{id:int}")]
        [DabblerEndpointAuthorize("GetProductById")]
        public async Task<ActionResult<ProductDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var product = await _service.GetByIdAsync(id, cancellationToken);
            if (product is null)
                return NotFound();

            if (!_currentUserService.TryGetUserId(out var userId))
                return Unauthorized();

            await _permissionService.ApplyFieldPermissionsAsync(product, userId, nameof(Product));

            return Ok(product);
        }

        [HttpPost]
        [DabblerEndpointAuthorize("CreateProduct")]
        public async Task<ActionResult<ProductDto>> CreateAsync([FromBody] ProductDto dto, CancellationToken cancellationToken = default)
        {
            var product = await _service.CreateAsync(dto, cancellationToken);
            
            if (!_currentUserService.TryGetUserId(out var userId))
                return Unauthorized();

            await _permissionService.ApplyFieldPermissionsAsync(product, userId, nameof(Product));

            return CreatedAtAction(nameof(GetByIdAsync), new { id = product.Id }, product);
        }

        [HttpPut("{id:int}")]
        [DabblerEndpointAuthorize("UpdateProduct")]
        public async Task<ActionResult<ProductDto>> UpdateAsync(int id, [FromBody] ProductDto dto, CancellationToken cancellationToken = default)
        {
            var product = await _service.UpdateAsync(id, dto, cancellationToken);
            if (product is null)
                return NotFound();

            if (!_currentUserService.TryGetUserId(out var userId))
                return Unauthorized();

            await _permissionService.ApplyFieldPermissionsAsync(product, userId, nameof(Product));

            return Ok(product);
        }

        [HttpDelete("{id:int}")]
        [DabblerEndpointAuthorize("DeleteProduct")]
        public async Task<ActionResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var success = await _service.DeleteAsync(id, cancellationToken);
            return success ? NoContent() : NotFound();
        }
    }
}
```

**Customization points:**
- GetAsync filter expression (currently has a TODO; developer fills in QBE → LINQ translation).
- Business logic and validation (currently smoke-test stubs).
- Non-standard endpoints (POST /products/{id}/availability to toggle, etc.).
- Authorization attributes (can be changed from `"CreateProduct"` to a different policy).

---

## Non-MVP Roadmap

### Granularity 3: Full Feature Slice

**Scope:** Granularities 1 & 2 + infrastructure wiring.

**Additions:**
- `ProductGridDataProvider.cs` — Implements IGridDataProvider<ProductDto>, bridges web UI to API client.
- `ProductApiClient.cs` — Typed HttpClient wrapper around the REST endpoints.
- DI registration in Program.cs (AddScoped, AddHttpClient, etc.).
- Navigation menu entry (static definition or DI-based registration).
- Security policy definitions (if not already present).
- Smoke tests: `ProductServiceTests.cs` and `ProductsControllerTests.cs` with basic Arrange-Act-Assert stubs.

**Why deferred:**
- Granularities 1 & 2 are proven individually. Adding infrastructure wiring requires field-testing to understand customization friction points.
- Tests and navigation are highly context-specific (each app has different nav structure, test organization). Better to observe real usage patterns before templating.

**Estimated timing:** Dedicated `crud-scaffolding-feature-slice` set after `crud-scaffolding` implementation closes.

### OData Variant

**Scope:** Alternative endpoint and filter representation.

**Decision deferred:**
- Would require a choice: QBE-only (current design) OR OData-only OR both (anti-pattern).
- Demand signal needed: Is there a use case that QBE cannot serve?
- Implementation complexity: QBE → LINQ is straightforward; OData requires URI parsing and validation.

**If demand emerges:**
- A follow-on set `crud-scaffolding-odata` authors a separate template suite with OData endpoints.
- Documentation clarifies: "Use QBE templates for simple filtering. Use OData templates for complex queries and third-party tool integration."
- The two templates are NOT mixed in one generated artifact.

### Multi-Entity Slices (One-to-Many, Master-Detail)

**Scope:** Customer → Orders nested grid, Product → Variants drill-down.

**Deferred because:**
- Single-entity CRUD is the foundation; proven patterns here inform multi-entity design.
- Relationship cardinality affects generated code significantly (1:1, 1:N, N:N).
- Nested grids and drill-down navigation have UI complexity (paging, sorting across relationships) not yet explored in a template context.

**Estimated timing:** Post-`crud-scaffolding-feature-slice`.

### Source Generator Alternative

**Scope:** Code generation at build time via Roslyn, not at template time.

**Deferred because:**
- `dotnet new` (immediate, one-time) is simpler than source generators (build-time, recurring inference).
- Source generators excel at generating repetitive code where updates propagate automatically. CRUD templates are meant to be customized once and owned.
- Switching to source generators later is possible if build-time generation demand emerges; switching from source generators back to templates is harder.

**If Roslyn demand emerges:**
- Requires a design review (how to mark entities as "scaffold me"? What does re-running do?).
- A dedicated investigation set, not a follow-on to `crud-scaffolding`.

---

## QBE vs. OData: Design Decision Documentation

This section is written explicitly so the implementation set does **not** reopen the question.

### Why QBE (Not OData) in MVP

| Aspect | QBE | OData |
|--------|-----|-------|
| **Client UI** | Declarative form (text, enum, number ranges, etc.) | Query syntax (URI strings, learning curve) |
| **Server parsing** | Direct DTO mapping + LINQ predicate | URI parsing, compliance validation, custom handlers |
| **Complexity** | Low; filters map 1:1 to LINQ | Medium-high; URI grammar, operator support |
| **Already in platform** | Yes (DeclarativeFilterPanel, CompositeFilterDescriptor) | No |
| **Existing CRUD pages** | ProductList, CustomerList, OrderList all use QBE | None use OData |
| **Maintenance burden** | Low; extend existing helpers | High; new parsing + validation layer |
| **Alignment with composition principle** | Explicit, customizable, understood by developers | Can become a runtime configuration black box |

### Rejection of "Both QBE and OData"

The prior runtime composite (`MasterDetailsConfig<TEntity>`) was rejected because it tried to support multiple filter representations simultaneously. The spike spec calls for "validation against tri-provider review and documentation of reasoning so the implementation set does not relitigate."

**Explicit decision:** Granularities 1 & 2 ship QBE-only. If OData is needed, it lands in a dedicated set as an **alternative** template suite, not mixed into the same generated code.

### Future Reconsideration

OData may be reconsidered if:
1. A real user demand emerges (e.g., "our API needs to support Postman's OData generation" or "our BI tool consumes OData").
2. A cost-benefit analysis shows OData complexity is worth the value (unlikely at this stage).
3. The implementation set completes Granularities 1 & 2 and a follow-on set is prioritized for OData.

Until then, QBE is the filter representation. No OData in MVP or Granularity 3.

---

## Conclusion

This proposal resolves all six open design questions and charts a course for a simple, straightforward implementation:

- **Granularities 1 & 2** (single page, service endpoints) ship in the MVP, leaving Granularity 3 (full slice) for a later set.
- **`dotnet new` templates** keep scaffolding consistent with .NET tooling, discoverable, and customizable.
- **QBE-only filters** avoid duplicating what the platform already offers and sidestep the runtime-configuration trap.
- **No regeneration story** enforces ownership; developers scaffold once and customize.
- **C# reflection** over a loaded assembly keeps entity discovery simple and source-of-truth.
- **Explicit non-goals** document why certain features (OData, source generators, multi-entity slices) are deferred, not forgotten.

Generated code is **indistinguishable from hand-written code**. The scaffold is the starting point; customization and ownership are the developer's responsibility. This aligns with the composition-over-configuration principle and ensures the scaffolding system remains maintainable and focused.

---

## Opus Contribution Context (Session 3 Disclosure)

This proposal was authored directly by the Opus orchestrator with full context from Session 1's inventory and the spike spec's open design questions. Per the feedback memory `feedback_orchestrator_self_contributes_to_review_panel`, this contribution uses **complete proposal-author context**, not an isolated prompt. 

Session 3's tri-provider review will receive this proposal alongside an explicit disclosure that the Opus analysis included access to prior session work, recommendations, and reasoning. The other two providers (GPT-5.4, Gemini Pro) will receive the proposal text alone without this contextual asymmetry, ensuring independent evaluation.
