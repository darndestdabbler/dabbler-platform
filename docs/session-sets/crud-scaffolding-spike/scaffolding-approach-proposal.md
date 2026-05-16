# Scaffolding Approach Proposal

> **Purpose:** Design a code-generation scaffolding system for Dabbler CRUD pages, service endpoints, and feature slices that outputs code indistinguishable from what a careful developer would write by hand.

> **Status:** Tri-provider review applied (Session 3). See [`scaffolding-approach-review.md`](scaffolding-approach-review.md) for the consolidated review. INLINE adjustments below have been applied to this document; larger BLOCKING adjustments (MVP self-sufficiency, request-contract unification, schema-file entity discovery, drop the post-generation reflection hook) are captured as MVP gates in the implementation-set spec at [`../crud-scaffolding/spec.md`](../crud-scaffolding/spec.md) and intentionally not reworked here.

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

**Explicit trade-off acknowledgement (added after Session 3 review):** Adding a new field after the initial scaffold currently means hand-editing the Razor list, detail form, filter config, service, DTO, and controller — exactly the boilerplate the scaffolder was designed to reduce. Both tri-provider reviewers flagged this as the design's main rough edge. The MVP accepts the trade-off (in-place regeneration is genuinely worse — composition principle wins), but the implementation set's non-MVP roadmap includes a **Schema Evolution / Drift Support** item: either a generate-to-temp + diff workflow, or a Roslyn analyzer that flags entity properties not represented in the scaffolded surface area. Both options preserve ownership without pretending merge-safe regeneration exists.

### 5. Entity Discovery: Schema File (Primary, Per-Property) + Explicit Parameters (Identity Basics); Reflection Deferred

**Decision (revised after Session 3 tri-provider review):** MVP entity discovery is split:

- **Explicit CLI parameters** carry the identity / namespace basics that every generated file needs: `--entity`, `--namespace` (and its variants per template), `--pk-type` (default `int`).
- **A schema file (`--schema <path>`) is the primary per-property input** for any template that enumerates fields — the page template's MudDataGrid columns and MudForm fields, the filter-config field list, the service's per-property filter/sort handling. The schema is a small JSON document (e.g., `customer.dabbler-entity.json`) listing each property with its type and per-surface flags (`IsInGrid`, `IsInForm`, `IsFilterable`, `IsSortable`, `IsReadOnly`). The implementation set ships a JSON-schema document so editors validate it.

Reflection-over-loaded-assembly is **deferred** to a future `dotnet tool` variant; DbContext introspection is **deferred**. The post-generation `ITemplateOperationProvider` reflection hook described in the original proposal is dropped from MVP scope.

**Why the revision:**

Both tri-provider reviewers correctly flagged that reflection-over-loaded-assembly is fragile in the common cases the platform targets:

- **Pre-build scaffolding:** Developers (and AI agents) frequently scaffold *before* the project compiles — design the entity shape first, then generate the code that uses it. Reflection requires a successful build first.
- **Build order:** Reflection introduces an implicit dependency on the build order that `dotnet new` is otherwise free of.
- **Multi-assembly solutions:** When entities, DTOs, API project, and web project live in separate assemblies (the common real-world case), an `--entity Customer` short name is ambiguous and the reflection loader needs explicit `--project` / `--assembly` / `--type` inputs to find the type.

Additionally, mixing a post-generation reflection hook into `dotnet new` straddles two CLI shapes: `dotnet new` is good at deterministic file emission from declared parameters, while reflection-driven discovery is `dotnet tool` territory. The hybrid produces a `dotnet new` template that internally behaves like a tool — confusing to maintain and harder for AI agents to invoke reliably.

**How it works (MVP):**

1. The developer (or AI agent) authors a schema file for the entity, then runs both templates with the schema as input:
   ```bash
   # Schema file: customer.dabbler-entity.json (authored once per entity)
   dotnet new dabbler-crud-service \
     --entity Customer \
     --api-namespace AcmeCoffeeOnline.Api \
     --dto-namespace AcmeCoffeeOnline.Shared.DTOs \
     --shared-namespace AcmeCoffeeOnline.Shared \
     --pk-type int \
     --schema customer.dabbler-entity.json
   dotnet new dabbler-crud-page \
     --entity Customer \
     --web-namespace AcmeCoffeeOnline.Web \
     --shared-namespace AcmeCoffeeOnline.Shared \
     --schema customer.dabbler-entity.json
   ```
2. The templates use the schema to drive their per-property loops: PropertyColumns in the grid, MudTextField/MudNumericField/MudCheckBox in the detail form, filter fields in the FilterConfig, and (in the service) per-property filter/sort handling. The schema is the canonical entity-shape input; the explicit CLI parameters cover only the identity/namespace basics that the schema does not encode.

**Alternatives (deferred to follow-on sets):**

- **Reflection-over-loaded-assembly + DbContext introspection:** A future `dotnet tool` variant (`dabbler scaffold crud-service --entity Customer`) can pull the property list from a compiled assembly or a live DbContext. The tool CLI has the affordances (explicit project selection, multi-assembly resolution, runtime metadata access) that `dotnet new` lacks. This is the right home for reflection-driven discovery, not the `dotnet new` template.

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
1. `dabbler-crud-page` — Granularity 1 (list + detail pages + the **minimal `ProductApiClient` + `ProductGridDataProvider`** needed for the pages to run; this is the post-Session-3 adjustment — see [`scaffolding-approach-review.md`](scaffolding-approach-review.md) Adjustment B1).
2. `dabbler-crud-service` — Granularity 2 (service interface + service implementation + controller).
3. **Combined invocation:** running `dabbler-crud-service Customer` followed by `dabbler-crud-page Customer` (or the future `dabbler-crud-slice Customer` shorthand) produces a self-contained, compiling, runnable CRUD slice.

**Delivery:**
- `Dabbler.Templates` project gets two new template folders: `templates/dabbler-crud-page/` and `templates/dabbler-crud-service/`.
- Each folder contains:
  - `.template.config/template.json` (variables: EntityName, Namespace, DtoNamespace, Route, PluralName, primary-key type, etc.)
  - Generated file templates (`.razor`, `.cs`, plus optional DI-snippet output) parameterized by `dotnet new`'s standard substitution.
- **No post-generation reflection hook in MVP.** The original proposal had an `ITemplateOperationProvider` hook that loaded the project's assembly and reflected over the entity type to fill in field bindings. That was dropped after Session 3 review because (a) `dotnet new` + assembly-reflection is an incoherent CLI shape — the reflection workflow belongs in a `dotnet tool` if it ever ships, and (b) reflection-over-loaded-assembly is fragile in the common pre-build / multi-assembly cases. See Section 5 for the revised entity-discovery model.

**Documentation:**
- `docs/scaffolding-templates/crud-page-template.md` — Usage guide, customization patterns, examples.
- `docs/scaffolding-templates/crud-service-template.md` — Service implementation customization, business logic patterns.
- Inline comments in generated code (minimal; explain WHY, not WHAT).

### What's Deferred (Post-MVP)

**Granularity 3 (full slice — what's left after MVP absorbs the runnable-slice essentials):**
- The minimal `ApiClient` and `GridDataProvider` previously sketched for Granularity 3 moved into MVP after Session 3 review (Adjustment B1). What remains for Granularity 3:
  - DI registration *automation* (the MVP emits the DI snippet as a separate text file; G3 wires it into `Program.cs` automatically).
  - Navigation menu entry registration.
  - Security policy registration (the policy names already appear on the generated controllers; G3 registers them with `IAuthorizationOptions`).
  - Smoke-test scaffolding (`{Entity}ServiceTests.cs`, `{Entity}sControllerTests.cs`).
- Dedicated set after `crud-scaffolding` completes.

**OData variant:**
- A **separate template suite** (e.g., `dabbler-crud-page-odata`, `dabbler-crud-service-odata`) supporting OData queries. **Never** a flag on the existing templates; the variation-axis trap that wording was a re-litigation of is rejected throughout this proposal.
- Requires reconciliation of how OData coexists with (or replaces) QBE in the generated endpoints if both ever ship side-by-side in the same solution.
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

> Required `using` directives are emitted by the template; the imports list below is illustrative. The `IGridDataProvider<ProductDto>` and `ProductApiClient` types are generated alongside this page in MVP — see the implementation-set spec for the unified MVP scope (the original proposal's plan to defer those to Granularity 3 was reversed after Session 3 review since it produced non-runnable G1 output).

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

> Required `using` directives are emitted by the template; the imports list below is illustrative. Generated form fields use the explicit `Value` / `ValueChanged` binding the platform mandates so that `EditSession<T>.NotifyChanged()` is called on every edit (see `project-guidance.md` → Conventions → CRUD Form Conventions — `@bind-Value` is forbidden because it bypasses dirty tracking).

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
                    Value="@_record.Name"
                    ValueChanged="@((string v) => { _record.Name = v; _session.NotifyChanged(); })"
                    Label="Name"
                    Placeholder="Product name"
                    Required="true"
                    RequiredError="Name is required"
                    For="@(() => _record.Name)"
                    Immediate="true"
                    Validation="@(new Func<string, IEnumerable<string>>(ValidateName))" />
            </MudItem>

            <MudItem xs="12" sm="6">
                <MudNumericField T="decimal"
                    Value="@_record.Price"
                    ValueChanged="@((decimal v) => { _record.Price = v; _session.NotifyChanged(); })"
                    Label="Price"
                    Format="C2"
                    Required="true"
                    RequiredError="Price is required"
                    For="@(() => _record.Price)"
                    Min="0" />
            </MudItem>

            <MudItem xs="12" sm="6">
                <MudCheckBox T="bool"
                    Value="@_record.IsAvailable"
                    ValueChanged="@((bool v) => { _record.IsAvailable = v; _session.NotifyChanged(); })"
                    Label="Available for Sale"
                    For="@(() => _record.IsAvailable)" />
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
        if (_form is not null)
        {
            await _form.Validate();
            if (!_formValid) return;
        }

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

    private RenderFragment CustomActionsFragment => @<MudButton
        Variant="Variant.Text"
        StartIcon="@Icons.Material.Filled.Undo"
        OnClick="@(() => _session.Undo())">
        Undo
    </MudButton>;
}
```

**Customization points:**
- Add/remove fields (MudTextField, MudNumericField, etc.).
- Custom validation logic (ValidateName method).
- Cross-field validation (compare two fields, check ranges).
- Entity-specific actions in CustomActionsFragment (Deactivate, Archive, etc.).

---

### Granularity 2 Sample: Service and Controller (Generated)

> Required `using` directives (ASP.NET Core, EF Core, AutoMapper, Dabbler permission service, Dabbler authorization attributes, DTO namespaces) are emitted by the template; omitted from these samples for clarity. The MVP also generates the matching `ProductApiClient` and `ProductGridDataProvider` so Granularity 1 + Granularity 2 together produce a runnable slice.

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

> Required `using` directives (ASP.NET Core, EF Core, AutoMapper, Dabbler filter engine, DTO namespaces) are emitted by the template; omitted from this sample for clarity. The implementation set's MVP will lock the exact QBE→LINQ helper API; the call site shown below is illustrative.

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
            IQueryable<Product> query = _db.Products.AsNoTracking();

            // Apply QBE filter via the platform's filter expression engine.
            // The generated code calls into Dabbler.Components' filter→LINQ helper
            // so the same expression engine that powers DeclarativeFilterPanel<TEntity>
            // is applied server-side. The implementation set fixes the exact API.
            if (request.Filter is not null)
            {
                query = query.ApplyQbeFilter(request.Filter);
            }

            // Apply sort: OrderBy → ThenBy chain (repeated OrderBy would reset ordering).
            var sortFields = request.SortFields ?? Array.Empty<SortField>();
            IOrderedQueryable<Product>? ordered = null;
            for (int i = 0; i < sortFields.Length; i++)
            {
                var sort = sortFields[i];
                if (i == 0)
                {
                    ordered = sort.Order == SortOrder.Ascending
                        ? query.OrderBy(e => EF.Property<object>(e, sort.Property))
                        : query.OrderByDescending(e => EF.Property<object>(e, sort.Property));
                }
                else
                {
                    ordered = sort.Order == SortOrder.Ascending
                        ? ordered!.ThenBy(e => EF.Property<object>(e, sort.Property))
                        : ordered!.ThenByDescending(e => EF.Property<object>(e, sort.Property));
                }
            }
            if (ordered != null) query = ordered;

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
            // Auth check BEFORE mutation: defense-in-depth even though [DabblerEndpointAuthorize] is the primary gate.
            if (!_currentUserService.TryGetUserId(out var userId))
                return Unauthorized();

            var product = await _service.CreateAsync(dto, cancellationToken);
            await _permissionService.ApplyFieldPermissionsAsync(product, userId, nameof(Product));
            return CreatedAtAction(nameof(GetByIdAsync), new { id = product.Id }, product);
        }

        [HttpPut("{id:int}")]
        [DabblerEndpointAuthorize("UpdateProduct")]
        public async Task<ActionResult<ProductDto>> UpdateAsync(int id, [FromBody] ProductDto dto, CancellationToken cancellationToken = default)
        {
            // Auth check BEFORE mutation.
            if (!_currentUserService.TryGetUserId(out var userId))
                return Unauthorized();

            var product = await _service.UpdateAsync(id, dto, cancellationToken);
            if (product is null)
                return NotFound();

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
- Filter behavior beyond the platform's QBE expression engine (the generated `GetAsync` already calls `query.ApplyQbeFilter(request.Filter)` — developers extend by wrapping that call with entity-specific predicates, not by re-implementing the filter).
- Business logic and validation (the generated CRUD method bodies are deliberately minimal — they handle the persistence-layer mechanics; entity-specific rules, side effects, and cross-service calls are the developer's customization surface).
- Non-standard endpoints (POST /products/{id}/availability to toggle, etc.) — add new controller actions alongside the generated ones.
- Authorization attributes (the generated policy names like `"CreateProduct"` come from `--auth-policy-prefix`; the developer can rename or layer additional `[Authorize(...)]` constraints).

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

**Scope:** Alternative template suite with OData query support.

**Decision deferred:**
- Would require a choice: QBE-only (current design) OR an OData-only sibling template suite. **Not** a `--filter-mode` flag on the existing template — that wording was a variation-axis trap that GPT-5.4's review caught in the original draft, and it has been removed. A flag like `--filter-mode odata` would force the same generator to support all filter representations as enum-style options, which is precisely what the rejected `MasterDetailsConfig<TEntity>` did.
- Demand signal needed: Is there a use case that QBE cannot serve?
- Implementation complexity: QBE → LINQ is straightforward via the existing `Dabbler.Components` expression engine; OData requires URI parsing, operator validation, and a separate server-side stack.

**If demand emerges:**
- A follow-on set `crud-scaffolding-odata` authors a **separate template suite** (`dabbler-crud-page-odata`, `dabbler-crud-service-odata`) with OData endpoints. Different template names; different generated code; same explicit-composition philosophy.
- Documentation clarifies: "Use the QBE templates for simple filtering. Use the OData templates for complex queries and third-party tool integration."
- The two template suites are NEVER mixed in one generated artifact, and there is no shared template with a mode switch.

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

This proposal resolves all six open design questions and charts a course for a simple, straightforward implementation (as adjusted after Session 3 tri-provider review):

- **Granularities 1 & 2 ship a runnable slice in MVP** — the minimal `{Entity}ApiClient` and `{Entity}GridDataProvider` move into MVP scope so scaffolded pages are self-sufficient. What remains for Granularity 3 (DI/nav/policy/test automation) lands in a follow-on set.
- **Pure `dotnet new` templates** with no post-generation reflection hook keep scaffolding consistent with .NET tooling, deterministic, and within the CLI shape `dotnet new` was designed for.
- **QBE-only filters with a working `query.ApplyQbeFilter(...)` call** in the generated service code (not a TODO). The platform's existing expression engine does the QBE→LINQ translation server-side.
- **No regeneration story** enforces ownership; developers scaffold once and customize. The non-MVP roadmap includes Schema Evolution / Drift Support tooling for the "add a new field" workflow that both tri-provider reviewers correctly flagged as the design's main rough edge.
- **Schema-file as the per-property input** for templates that enumerate fields, with explicit `--entity` / `--namespace` / `--pk-type` parameters limited to identity and namespace basics. **Reflection-over-loaded-assembly is deferred** to a future `dotnet tool` variant where its CLI affordances (explicit project selection, multi-assembly resolution, runtime metadata access) actually fit.
- **OData remains a separate-template-suite roadmap item** — never a `--filter-mode` flag — explicitly to avoid re-introducing the variation-axis trap that abandoned the runtime composite.
- **Explicit non-goals** document why certain features (OData mixing, runtime composites, source generators, multi-entity slices) are deferred, not forgotten.

Generated code is **indistinguishable from hand-written code**. The scaffold is the starting point; customization and ownership are the developer's responsibility. This aligns with the composition-over-configuration principle and ensures the scaffolding system remains maintainable and focused.

---

## Opus Contribution Context (Session 3 Disclosure)

This proposal was authored directly by the Opus orchestrator with full context from Session 1's inventory and the spike spec's open design questions. Per the feedback memory `feedback_orchestrator_self_contributes_to_review_panel`, this contribution uses **complete proposal-author context**, not an isolated prompt. 

Session 3's tri-provider review will receive this proposal alongside an explicit disclosure that the Opus analysis included access to prior session work, recommendations, and reasoning. The other two providers (GPT-5.4, Gemini Pro) will receive the proposal text alone without this contextual asymmetry, ensuring independent evaluation.
