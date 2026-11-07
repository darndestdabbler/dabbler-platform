# Helper and Pattern Inventory

**Session 1 of 3: Helper And Pattern Inventory**  
**Purpose:** Establish a comprehensive baseline of existing CRUD helpers, API patterns, and reference pages that any scaffolding tool must produce code matching.

---

## Part I: Inventory (Orchestrator-Authored)

### CRUD Helpers in `src/Libs/Dabbler.Components/`

#### 1. EditSession\<T\>
**Location:** [src/Libs/Dabbler.Components/MasterDetail/EditSession.cs](../../../src/Libs/Dabbler.Components/MasterDetail/EditSession.cs)

- **Purpose:** Tracks edit state for a single entity instance.
- **Key Features:**
  - Captures a snapshot when the session opens
  - Exposes a live reference that forms bind to directly
  - Reports dirty state via JSON serialization comparison
  - Supports undo (reverts Live to snapshot via reflection over public read/write properties)
  - Supports commit (freezes current Live as new baseline)
  - Supports CommitFrom (overwrites Live from server-returned instance, then freezes)
- **Type Constraints:** Generic over T where T : class. Intended for flat DTOs with public read/write scalar/enum/string/nullable properties.
- **Snapshot Semantics:** JSON-based; collections and computed properties are not round-tripped. Get-only properties and fields are not touched by Undo/CommitFrom.
- **Pages Using It:** ProductDetail.razor, CustomerDetail.razor, OrderDetail.razor

---

#### 2. DirtyAwareActionRow (Component)
**Location:** [src/Libs/Dabbler.Components/MasterDetail/DirtyAwareActionRow.razor](../../../src/Libs/Dabbler.Components/MasterDetail/DirtyAwareActionRow.razor)

- **Purpose:** Renders Save/Cancel/Delete primary action buttons with state-driven visibility and confirmation dialogs.
- **Parameters:**
  - `IsDirty` (bool): Drives Save button enabled state and Cancel/Close label flip
  - `Saving` (bool): When true, disables all buttons to prevent double-click during flight
  - `ShowCancel` (bool, default true): Whether to render Cancel/Close button
  - `OnSave`, `OnCancel`, `OnDelete` (EventCallback): Action handlers
  - `CanDelete` (bool, default true): Gate for Delete button enabled state
  - `CustomActions` (RenderFragment): Page-specific buttons (Undo, Toggle Status, etc.)
  - `CustomActionsPosition` (ActionRowSlot, default End): Slot position for custom buttons (Start or End)
  - `SaveLabel`, `CancelLabel`, `CloseLabel`, `DeleteLabel`: Customizable button labels
  - Dialog title and message templates for Cancel and Delete confirmations
- **Confirmation Dialogs:**
  - Cancel dialog only shows if IsDirty is true; user must confirm to proceed
  - Delete dialog always shows; user must confirm to proceed
  - Callback fires only after confirmation
- **Slot Model:** CustomActions can be rendered at either end of the action row.
- **Pages Using It:** ProductDetail.razor, CustomerDetail.razor, OrderDetail.razor

---

#### 3. DeclarativeFilterPanel\<TEntity\> (Component)
**Location:** [src/Libs/Dabbler.Components/MasterDetail/DeclarativeFilterPanel.razor](../../../src/Libs/Dabbler.Components/MasterDetail/DeclarativeFilterPanel.razor)

- **Purpose:** QBE (Query-By-Example) filter UI driven by a FilterConfig<TEntity> declaration.
- **Supported Filter Field Types:**
  - **Text**: String search with "contains" operator
  - **Enum**: Dropdown with all enum values, emits "eq" operator
  - **Bool**: Tri-state checkbox (null = unfiltered, true/false), emits "eq"
  - **Numeric**: Single value or range (min/max pair) with "gte"/"lte" operators
  - **Date**: Single value or range (start/end pair) with "gte"/"lte" operators
  - **ForeignKey**: Dropdown loaded async via LoadLookupItemsAsync callback
- **Features:**
  - Filter state dictionary with optional pre-seeded InitialValues
  - URL query-string persistence (ServerSide mode) with invariant-culture serialization
  - Active filter chips with one-click clear
  - Clear All button
  - JS interop for history.replaceState (gracefully swallows prerendering failures)
- **Output:**
  - Emits `FilterChanged` event with CompositeFilterDescriptor (or null if no filters)
  - FilterDescriptor[] with property name, operator, and value
- **Modes:** ServerSide (URL persistence) or ClientSide (parent controls initial state)
- **Pages Using It:** ProductList.razor, CustomerList.razor, OrderList.razor

---

#### 4. IGridDataProvider\<T\> (Interface)
**Location:** [src/Libs/Dabbler.Components/Data/IGridDataProvider.cs](../../../src/Libs/Dabbler.Components/Data/IGridDataProvider.cs)

- **Purpose:** Abstraction over data sources supporting server-side sort, filter, and paging.
- **Contract:**
  ```csharp
  Task<GridDataResponse<T>> FetchAsync(GridDataRequest request, CancellationToken cancellationToken = default);
  ```
- **GridDataRequest Structure:**
  - Sort fields and order direction
  - Filter parameters (CompositeFilterDescriptor)
  - Page index and size
- **GridDataResponse Structure:**
  - Items: T[]
  - TotalCount: int (for pagination)
- **Usage:** Passed to MudDataGrid's ServerData delegate via GridDataProviderExtensions.ToServerDataFunc\<T\>
- **Implementations:** LocalDataProvider (in-memory), REST API adapters

---

### API Endpoint Patterns in `src/Apps/AcmeCoffeeOnline.Api/`

#### ProductsController (Representative CRUD API)
**Location:** [src/Apps/AcmeCoffeeOnline.Api/Controllers/ProductsController.cs](../../../src/Apps/AcmeCoffeeOnline.Api/Controllers/ProductsController.cs)

**Route:** `[Route("api/[controller]")] → /api/products`

**Endpoints:**
1. **GET /api/products** (GetAllAsync)
   - Parameter: `[FromQuery] PagedRequest request` (contains sort, filter, paging)
   - Returns: `ActionResult<PagedResponse<ProductDto>>`
   - Authorizes with `[DabblerEndpointAuthorize("GetProducts")]`
   - Applies field permissions to each item in response

2. **GET /api/products/{id}** (GetByIdAsync)
   - Parameter: `int id`
   - Returns: `ActionResult<ProductDto>` or 404
   - Authorizes with `[DabblerEndpointAuthorize("GetProductById")]`
   - Applies field permissions

3. **POST /api/products** (CreateAsync)
   - Body: `[FromBody] ProductDto dto`
   - Returns: `ActionResult<ProductDto>` with 201 Created
   - Authorizes with `[DabblerEndpointAuthorize("CreateProduct")]`
   - Applies field permissions

4. **PUT /api/products/{id}** (UpdateAsync)
   - Parameters: `int id`, `[FromBody] ProductDto dto`
   - Returns: `ActionResult<ProductDto>` or 404
   - Authorizes with `[DabblerEndpointAuthorize("UpdateProduct")]`
   - Applies field permissions

5. **PATCH /api/products/{id}/availability** (ToggleAvailabilityAsync)
   - Parameter: `int id`
   - Returns: `NoContent()` (204) or 404
   - Authorizes with `[DabblerEndpointAuthorize("ToggleProductAvailability")]`
   - Custom action (not generic CRUD)

**Shared Patterns:**
- Constructor injection of service, permission service, current user service
- userId extraction with TryGetUserId() guard
- Permission service call: `ApplyFieldPermissionsAsync(item, userId, entityName)`
- Async service layer delegation for all logic

**Service Layer Pattern (IProductService):**
```csharp
Task<PagedResponse<ProductDto>> GetAllAsync(PagedRequest request, CancellationToken cancellationToken);
Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken);
Task<ProductDto> CreateAsync(ProductDto dto, CancellationToken cancellationToken);
Task<ProductDto?> UpdateAsync(int id, ProductDto dto, CancellationToken cancellationToken);
Task<bool> ToggleAvailabilityAsync(int id, CancellationToken cancellationToken);
```

---

### CRUD Page Patterns in `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/`

#### ProductsPage (List Page Entry Point)
**Location:** [src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Products/ProductsPage.razor](../../../src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Products/ProductsPage.razor)

```razor
@page "/products"
@attribute [DabblerPageAuthorize("/products")]
@using AcmeCoffeeOnline.Shared.DTOs
@using Dabbler.Components.Data

<PageTitle>Products — AcmeCoffeeDistributors</PageTitle>

<ProductList DataProvider="DataProvider" Title="Products" InitialSearchTerm="@Search" />

@code {
    [SupplyParameterFromQuery]
    public string? Search { get; set; }

    [Inject]
    private IGridDataProvider<ProductDto> DataProvider { get; set; } = default!;
}
```

- **Route:** `/products`
- **Authorization:** `[DabblerPageAuthorize("/products")]`
- **Dependencies:** IGridDataProvider\<ProductDto\> (injected)
- **Query Parameters:** ?Search=term (forwarded to ProductList as InitialSearchTerm)
- **Layout:** PageTitle + ProductList component
- **Pattern:** Thin wrapper delegating to ProductList component

---

#### ProductList (Reusable Grid + Filter Component)
**Location:** [src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Products/ProductList.razor](../../../src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Products/ProductList.razor) (excerpt)

**Parameters:**
- `DataProvider`: IGridDataProvider<ProductDto>
- `Title`: Display title
- `InitialSearchTerm`: Pre-seeded search value
- `AutoLoadOnFilterChange`: Whether to auto-fetch on filter change (vs. manual Search button)

**Composition:**
1. **Title:** MudText with page heading
2. **DeclarativeFilterPanel:**
   - TEntity=ProductDto
   - Config=_filterConfig (built in @code)
   - FilterChanged callback → OnFilterChangedAsync
   - InitialValues seeded from InitialSearchTerm
3. **Toolbar:**
   - Status text
   - Search button (if AutoLoadOnFilterChange is false)
   - Add Product button (Href="/admin/products")
4. **Error Banner:** MudAlert with close icon, shown on data fetch error
5. **MudDataGrid:**
   - ServerData delegate bound to ServerDataFuncAsync
   - Dense, Hover, Bordered
   - RowsPerPage=25
   - ReadOnly=true
   - RowClick handler for detail navigation
   - Columns: Dynamic via PropertyColumn and RestrictedGridColumn
   - Columns decorated with `[RestrictedGridColumn]` for field permission enforcement

**Data Fetching:**
- ServerDataFuncAsync calls DataProvider.FetchAsync with GridDataRequest
- Request built from current filter state, sort, page
- Response unpacked to MudDataGrid

---

#### ProductDetail (Single-Record Edit Page)
**Location:** [src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Products/ProductDetail.razor](../../../src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Products/ProductDetail.razor) (excerpt)

**Dependencies:**
- EditSession\<ProductDto\>
- DirtyAwareActionRow component
- ProductApiClient (injected)

**Page Structure:**
1. **Loading State:** MudProgressCircular while fetching
2. **DirtyAwareActionRow:**
   - IsDirty from _session.IsDirty
   - Saving from _saving bool
   - ShowCancel=false (Back button or Undo handles return)
   - OnSave → HandleSaveAsync
   - CustomActionsPosition=ActionRowSlot.End
   - CustomActions slot with Undo button and Deactivate/Activate toggle button
3. **MudForm:**
   - @ref bound for validation
   - Grid layout (typically xs="12" md="6" columns)
   - Form fields bound via _record (live reference)
   - ValueChanged handlers call _session.NotifyChanged() to update dirty state
   - Immediate="true" and DebounceInterval for responsive UX

**Edit Flow:**
1. Fetch product from API
2. Create EditSession\<ProductDto\> with loaded record
3. Render form with live bindings
4. User edits; ValueChanged calls _session.NotifyChanged() after each change
5. Save button enabled iff _session.IsDirty is true
6. On Save click, HandleSaveAsync calls API update
7. On success, _session.CommitFrom(apiResult) to refresh with server values (timestamps, etc.)
8. Undo button calls _session.Undo() to revert to last committed state

---

## Part II: Structural Analysis via AI Router

**Routed via:** `route(task_type="analysis", content=...)`  
**Model:** Gemini Pro (Tier 2, $0.0276 USD)  
**Result:** Comprehensive prescriptive guidance on what generators must emit at three granularities

---

### Generator Input: Entity Metadata Definition

The generator's primary input must be an **Entity Metadata Definition** including:
- Entity Name (e.g., `Product`)
- DTO Name (e.g., `ProductDto`)
- Plural Name (e.g., `Products`)
- Route Segment (e.g., `products`)
- A list of properties with:
  - Type
  - Display name
  - Flags: IsFilterable, IsSortable, IsInGrid, IsOnForm, IsReadOnly

---

### Granularity 1: Single Razor CRUD Page

#### A. List Page (e.g., `ProductList.razor`)

**What Must Be Generated:**
- Route: `@page "/products"`
- Authorization: `@attribute [DabblerPageAuthorize("/products")]`
- Injection: `@inject IGridDataProvider<ProductDto> DataProvider`
- Query parameters: `[Parameter, SupplyParameterFromQuery] public string? Search`
- Markup structure:
  1. `MudText` with page title
  2. `DeclarativeFilterPanel<ProductDto>` bound to `OnFilterChangedAsync` and seeded with InitialSearchTerm
  3. `MudToolBar` with "New [Entity]" button (Href="/products/new")
  4. `MudDataGrid` with:
     - `ServerData="@ServerDataFuncAsync"`
     - Standard properties: Dense, Hover, Bordered, ReadOnly
     - `OnRowClick` handler for detail navigation
     - Generated PropertyColumn and RestrictedGridColumn per entity metadata
- Code block with:
  - `_grid` MudDataGrid reference
  - `_filter` CompositeFilterDescriptor state
  - `ServerDataFuncAsync` implementation
  - `OnFilterChangedAsync` handler calling `_grid.ReloadServerData()`
  - `HandleRowClick` handler using NavigationManager

**What Must Be Customized:**
- Complex column rendering (status chips with colors, etc.)
- Custom toolbar actions beyond "Add"

**What Must Pre-Exist:**
- `ProductDto` class
- `IGridDataProvider<ProductDto>` implementation
- `/products` page authorization policy

---

#### B. Detail Page (e.g., `ProductDetail.razor`)

**What Must Be Generated:**
- Routes: `@page "/products/{Id:int}"` and `@page "/products/new"`
- Injections: `ProductApiClient`, `NavigationManager`
- Loading state: MudProgressCircular while fetching
- `MudForm` with `@ref="_form"`
- `DirtyAwareActionRow`:
  - IsDirty=_session.IsDirty
  - Saving=_saving
  - OnSave=HandleSaveAsync
  - CustomActions slot with Undo button
- `MudGrid` layout with generated form fields:
  - `MudTextField` for strings, bound via `_session.Live` property
  - Field ValueChanged handlers call `_session.NotifyChanged()`
  - For="() => expression" for validation
  - Required, RequiredError per metadata
- Code block with:
  - `[Parameter] public int Id` and route parameter binding
  - `EditSession<ProductDto> _session` state
  - `ProductDto _record => _session.Live`
  - `bool _saving` and `MudForm _form` references
  - `OnInitializedAsync`: Fetch via ApiClient or create new, initialize EditSession
  - `HandleSaveAsync`: Validate form, set saving, call API (Create or Update), CommitFrom result, navigate for new records
  - `HandleUndo`: Call `_session.Undo()`

**What Must Be Customized:**
- Complex cross-field validation logic
- Entity-specific custom actions (Deactivate, Run Report, etc.)

**What Must Pre-Exist:**
- `ProductDto` class
- Typed `ProductApiClient`
- `EditSession<T>` and `DirtyAwareActionRow` components from platform

---

### Granularity 2: Service Endpoint Trio (Controller + Service + Impl)

**What Must Be Generated:**

**`IProductService.cs`:**
```csharp
public interface IProductService
{
    Task<PagedResponse<ProductDto>> GetAsync(PagedRequest request, CancellationToken ct);
    Task<ProductDto> GetByIdAsync(int id, CancellationToken ct);
    Task<ProductDto> CreateAsync(ProductDto dto, CancellationToken ct);
    Task<ProductDto> UpdateAsync(int id, ProductDto dto, CancellationToken ct);
}
```

**`ProductService.cs`:**
- Implements IProductService
- Constructor: Injected AppDbContext and IMapper
- GetAsync: Apply PagedRequest to DbSet\<Product\>, project to ProductDto
- GetByIdAsync: Find by id, map and return (or null)
- CreateAsync: Map to entity, Add, SaveChanges, map back to DTO
- UpdateAsync: Find entity, Map(dto, entity), SaveChanges

**`ProductsController.cs`:**
- `[Route("api/products")]`, `[ApiController]`
- Constructor: IProductService, IPermissionService
- GET / : [DabblerEndpointAuthorize("GetProducts")], call service.GetAsync, apply field permissions, return Ok
- GET /{id} : [DabblerEndpointAuthorize("GetProductById")], call service, apply permissions
- POST / : [DabblerEndpointAuthorize("CreateProduct")], call service.CreateAsync, return CreatedAtAction
- PUT /{id} : [DabblerEndpointAuthorize("UpdateProduct")], call service.UpdateAsync

**What Must Be Customized:**
- All business logic within service methods (validation, side effects, cross-service calls)
- Complex query logic requiring manual IQueryable construction
- Non-standard endpoints (PATCH, custom actions)

**What Must Pre-Exist:**
- `Product` EF Core entity
- `AppDbContext` with `DbSet<Product>`
- `ProductDto` class
- AutoMapper profile mapping Product ↔ ProductDto
- Authorization policy names
- Infrastructure: PagedRequest, PagedResponse, IPermissionService, DabblerEndpointAuthorizeAttribute

---

### Granularity 3: Full Feature Slice

**What Must Be Generated:**
- All artifacts from Granularities 1 and 2, plus:

**`ProductGridDataProvider.cs`:**
- Implements `IGridDataProvider<ProductDto>`
- Injects `ProductApiClient`
- FetchAsync: Map GridDataRequest to PagedRequest, call ApiClient, map response to GridDataResponse

**`ProductApiClient.cs`:**
- Typed HttpClient class
- Methods for each controller endpoint
- Handles serialization and HTTP calls

**DI Registrations:**
```csharp
services.AddScoped<IProductService, ProductService>();
services.AddScoped<IGridDataProvider<ProductDto>, ProductGridDataProvider>();
services.AddHttpClient<ProductApiClient>(client => { /* config */ });
```

**Basic Unit Tests:**
- `ProductServiceTests.cs`: Mock DbContext and IMapper; verify CreateAsync adds entity and calls SaveChanges
- `ProductsControllerTests.cs`: Mock IProductService; verify GET calls service and returns OkObjectResult

**Navigation Menu Entry:**
- Static definition or registration call to add "Products" link pointing to `/products`

**Security Policy Definitions:**
- Register policies like "GetProducts", "CreateProduct", "/products" with required claims/roles

**What Must Be Customized:**
- All customization from Granularities 1 and 2
- Tests are smoke tests; developers add comprehensive business logic, validation, and authorization tests
- Navigation icon, order, conditional visibility

**What Must Pre-Exist:**
- All pre-existing items from Granularities 1 and 2
- Testing framework (xUnit, Moq, etc.)
- Patterns for DI registration, navigation menu, security policy definition
- Solution/project structure

---

### Analysis Summary

The generator succeeds when its output **compiles and passes smoke tests without modification**, requiring **zero changes before initial commit**. The three granularities nest:
- Granularity 1 (page) handles UI rendering and form interaction
- Granularity 2 (endpoints) handles API and service layer
- Granularity 3 (slice) combines all three plus infrastructure wiring, tests, and navigation

Customization is **localized and unambiguous**: business logic goes in service methods, complex rendering in page markup, tests are expanded by developers (not overwritten by re-running the generator).

---

## Part III: Session Notes

- **Inventory Coverage:** Helpers, API patterns, and page patterns are now fully documented with line-number references and code excerpts.
- **Next Steps:** Session 2 will use this inventory to draft the scaffolding-approach-proposal, resolving open design questions and proposing CLI shape, granularities, and generated-code samples.
