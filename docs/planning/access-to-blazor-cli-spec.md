# Access-to-Blazor CLI & Stubbed UI Pipeline Spec

> **Purpose:** Build the CLI application that extracts layout metadata from Access databases and generates a stubbed Blazor Server UI with real navigation, simulated search, and toast event placeholders — plus the Playwright test suite that validates layout and navigation against the layout contracts.
> **Created:** 2026-04-13
> **Session Set:** `docs/session-sets/access-to-blazor-cli/`
> **Prerequisite:** The existing `Dabbler.Discovery` project (inspectors, models, metadata DB) must be building. The `Dabbler.Components` NuGet packages must be available. The Reports session set must be complete.
> **Workflow:** Claude Chat (Opus) planned → Claude Code (Sonnet) implements per session → Cross-provider verification each session

---

## Project Overview

A two-phase pipeline — **Extract** and **Generate** — that produces a fully navigable stubbed Blazor Server application from an Access database, ready for stakeholder review and automated layout validation.

| Phase | Tool | Input | Output |
|-------|------|-------|--------|
| Extract | `AccessToBlazor.Extractor` CLI | `.accdb` file | Layout contracts (JSON), navigation manifest (JSON), sample data (JSON) |
| Generate | `AccessToBlazor.Generator` CLI | Layout contracts + navigation manifest | Stubbed Blazor Server project with pages, DI services, and Playwright tests |

**What "stubbed" means in this spec:** Navigation between pages is real (Blazor `NavigationManager`). Search forms return filterable sample data generated from the Access metadata. Clicking a search result navigates to the correct detail form populated with sample data for that record. All other event handlers (Save, Delete, custom buttons) display toast messages describing what would happen. The DI container registers stub service implementations that can later be swapped for real implementations via configuration.

### Design Philosophy

- **Layout contracts are the single source of truth** — both the Blazor generator and the Playwright test generator read the same JSON contracts. No information is encoded only in the generated Razor.
- **Form classification drives template selection** — the extractor classifies each form (Search, Detail, Switchboard, Dialog, ReportLauncher) and the generator selects from a small set of well-tested Blazor page templates rather than doing freeform layout inference.
- **Navigation manifest captures the full form graph** — every `DoCmd.OpenForm`, `DoCmd.OpenReport`, and macro-based navigation action is an edge in the graph. The stubbed UI implements all edges as real `NavigationManager.NavigateTo` calls.
- **Stub services are real implementations** — they filter sample data, return plausible results, and maintain enough state that a stakeholder can click through the entire application flow. The DI swap from stub to real is a single configuration change per service.
- **Dabbler components are used wherever possible** — search forms use `DabblerQueryPage<T>`, detail forms use `DabblerDetailForm<T>`, report launchers use `DabblerReportPage`, popups use `DabblerRecordPopup<T>`, and the shell uses `DabblerShell` with `DabblerNavigationPanel` and `DabblerTabbedWorkspace`.

---

## Current Codebase Summary

### Existing Code This Spec Builds On

| Component | Location | Purpose |
|-----------|----------|---------|
| `ComAutomationInspector` | `src/Dabbler.Discovery/Inspectors/` | COM-based extraction of forms, reports, queries, macros, VBA |
| `OleDbSchemaInspector` | `src/Dabbler.Discovery/Inspectors/` | OleDb-based schema extraction (tables, columns, indexes, relationships) |
| `MsysObjectsInspector` | `src/Dabbler.Discovery/Inspectors/` | MSysObjects enumeration of all Access objects |
| `MetadataDbBuilder` | `src/Dabbler.Discovery/` | Creates and initializes the SQLite metadata database |
| `MetadataPopulator` | `src/Dabbler.Discovery/` | Populates metadata DB from `AccessInventory` |
| `MacroDependencyExtractor` | `src/Dabbler.Discovery/` | Extracts navigation dependencies from macros |
| `AccessInventory` / models | `src/Dabbler.Discovery/Models/` | In-memory model for all extracted metadata |
| `metadata-schema.sql` | `src/Dabbler.Discovery/` | SQLite schema for the metadata repository |

### Existing Dabbler Components Used by Generated Pages

| Component | Purpose in Generated UI |
|-----------|------------------------|
| `DabblerShell` | Top-level application shell with nav panel + tabbed workspace |
| `DabblerNavigationPanel` | Left-hand entity navigation with smart filters |
| `DabblerTabbedWorkspace` | Tab management for open forms |
| `DabblerQueryPage<T>` | Search/list form template (filter grid + data grid) |
| `DabblerDetailForm<T>` | Detail/edit form template (metadata-driven field layout) |
| `DabblerRecordPopup<T>` | Popup editing dialog |
| `DabblerReportPage` | Report launcher template (parameter form + viewer) |
| `DabblerDashboardPage` | Switchboard/navigation form template |
| `DabblerFilterGrid<T>` | Filter UI for search forms |
| `DabblerDataGrid<T>` | Data grid for search results and subform datasheets |
| `DabblerComboBox<T>` | Combo box controls mapped from Access combo boxes |
| `DabblerPageToolbar` | Standardized toolbar for page actions |

### Key Interfaces for Stub/Real DI Swap

| Interface | Stub Provides | Real Provides |
|-----------|--------------|---------------|
| `IGridDataProvider<T>` | Filterable in-memory sample data | REST API calls to service layer |
| `IReportDataProvider` | Sample report data | REST API calls to reporting service |
| `IEntityMetadataProvider` | Metadata from layout contracts | Metadata from database/configuration |
| `INavigationDataProvider` | Sample navigation records | REST API calls for related records |
| `IUsageTrackingProvider` | In-memory recent/favorites | Persisted usage tracking via API |
| `IArtifactCatalogProvider` | Static catalog from contracts | Dynamic catalog via API |

---

## Phase 1: Extractor CLI — Enhanced Metadata Extraction

### New Properties to Harvest from Access Controls

The existing `ComAutomationInspector.EnrichForms` reads `Name`, `ControlType`, `ControlSource`, `DefaultValue`, `ValidationRule`, `InputMask`, and event procedures. The following properties must be added to enable layout generation:

#### Spatial Properties (all in twips)

| Access Property | Model Field | Purpose |
|----------------|-------------|---------|
| `Control.Left` | `Left` | Column inference, same-row detection |
| `Control.Top` | `Top` | Row inference, vertical ordering |
| `Control.Width` | `Width` | Relative width calculation |
| `Control.Height` | `Height` | Relative height, multiline detection |
| `Control.TabIndex` | `TabIndex` | Tab order mapping |

#### Display Properties

| Access Property | Model Field | Purpose |
|----------------|-------------|---------|
| `Control.Visible` | `Visible` | Skip hidden controls in layout |
| `Control.Enabled` | `Enabled` | Map to `Disabled` parameter |
| `Control.Locked` | `Locked` | Map to `ReadOnly` parameter |
| `Control.EnterKeyBehavior` | `EnterKeyBehavior` | Detect multiline text boxes |
| `Control.ScrollBars` | `ScrollBars` | Detect multiline text boxes |
| `Control.RowSource` | `RowSource` | Combo/list box data source |
| `Control.RowSourceType` | `RowSourceType` | Table/List/ValueList, Query |
| `Control.ColumnCount` | `ColumnCount` | Multi-column combo boxes |
| `Control.BoundColumn` | `BoundColumn` | Which column provides the value |
| `Control.ColumnWidths` | `ColumnWidths` | Column width distribution |

#### Form-Level Properties

| Access Property | Model Field | Purpose |
|----------------|-------------|---------|
| `Form.InsideWidth` | `FormInsideWidth` | Base for relative width calculations |
| `Form.InsideHeight` | `FormInsideHeight` | Base for relative height calculations |
| `Form.DefaultView` | `DefaultView` | Detect datasheet vs single-form vs continuous |
| `Form.PopUp` | `PopUp` | Detect popup/dialog forms |
| `Form.Modal` | `Modal` | Detect modal dialogs |
| `Form.BorderStyle` | `BorderStyle` | `Dialog` border style = popup |
| `Form.NavigationButtons` | `NavigationButtons` | Whether record navigation is shown |
| `Form.AllowAdditions` | `AllowAdditions` | Detect read-only vs editable forms |
| `Form.AllowEdits` | `AllowEdits` | Detect read-only forms |
| `Form.AllowDeletions` | `AllowDeletions` | Whether delete is permitted |

#### Label Association

For each control, attempt to find its associated label via:
1. `Controls(i).Controls(0)` — Access pairs labels with their field control
2. Spatial proximity fallback — label immediately left of or above a field within threshold

Store as `AssociatedLabelCaption` on `FormControlInfo`.

#### Tab Control Pages

For `acTabCtl` controls, enumerate `.Pages` and record which controls belong to each page (via `Control.Parent.Name` matching the page name). Store as a `TabPages` list of `TabPageInfo { Name, Caption, ControlNames[] }`.

### Form Classification Algorithm

Each form is classified during extraction based on observable properties:

| Classification | Detection Criteria |
|---------------|-------------------|
| `Search` | Has a subform in datasheet view whose record source is a parameterized query, plus unbound filter controls at the top |
| `Detail` | Has a record source, bound controls for most fields, Save/Delete/Close buttons or `AllowEdits = true` |
| `Switchboard` | Mostly command buttons, few or no bound controls, no record source |
| `Dialog` | `PopUp = True`, `Modal = True`, or `BorderStyle = Dialog` |
| `ReportLauncher` | Has parameter controls and a button whose event handler calls `DoCmd.OpenReport` |
| `Continuous` | `DefaultView = Continuous Forms` — maps to a search-style grid |
| `Datasheet` | `DefaultView = Datasheet` — maps to `DabblerQueryPage<T>` |

The classification is stored in the layout contract as `formClassification` and used by the generator to select the appropriate Blazor page template.

### Navigation Manifest

A separate JSON file (`navigation-manifest.json`) capturing the complete form-to-form navigation graph, derived from:
- `DoCmd.OpenForm` calls in macros, embedded macros, and VBA procedures
- `DoCmd.OpenReport` calls (same sources)
- Subform `SourceObject` references
- Switchboard button click handlers

Schema:

```json
{
  "$schema": "navigation-manifest-v1",
  "database": "Northwind.accdb",
  "extractedAt": "2026-04-13T10:00:00Z",
  "routes": [
    {
      "source": "frmSwitchboard",
      "target": "frmCustomerSearch",
      "trigger": "cmdCustomers_Click",
      "triggerType": "macro|vba|embedded",
      "targetType": "Form|Report",
      "parameters": {},
      "openArgs": null
    }
  ],
  "entryPoints": ["frmSwitchboard"],
  "formClassifications": {
    "frmSwitchboard": "Switchboard",
    "frmCustomerSearch": "Search",
    "frmCustomerDetail": "Detail",
    "frmQuickEdit": "Dialog"
  }
}
```

### Sample Data Generation

The extractor generates realistic sample data based on:
- **Field name heuristics**: `Email` → fake emails, `Phone` → formatted phone numbers, `Name`/`Company` → fake names, `Date` → dates in reasonable range, `Amount`/`Total`/`Price` → currency values
- **Data type**: `int` → sequential IDs, `date` → recent dates, `currency` → reasonable amounts, `boolean` → mix of true/false
- **Validation rules**: `maxLength` constrains generated string length, `inputMask` drives format
- **Combo box options**: If `RowSourceType = "Value List"`, use actual values; if table/query-based, generate plausible options from field metadata
- **Row count**: Generate 10–25 sample rows per search form, 3–5 for subform grids

Sample data is embedded in each layout contract under `sampleData` (per field) and `sampleRows` (per grid/search form).

### Layout Contract Schema Additions

Beyond what's defined in `AccessToBlazor-LayoutPipeline.md`, each layout contract includes:

```json
{
  "formClassification": "Search|Detail|Switchboard|Dialog|ReportLauncher|Continuous|Datasheet",
  "formProperties": {
    "insideWidth": 9000,
    "insideHeight": 6000,
    "defaultView": "SingleForm",
    "popUp": false,
    "modal": false,
    "allowAdditions": true,
    "allowEdits": true,
    "allowDeletions": true,
    "navigationButtons": true
  },
  "recordSource": {
    "name": "qryCustomers",
    "type": "Query|Table",
    "parameterFields": ["CustomerType"]
  },
  "primaryKey": {
    "field": "CustomerID",
    "dataType": "int"
  }
}
```

### Metadata Database Schema Extensions

New columns on `FormControls` table:

```sql
ALTER TABLE FormControls ADD COLUMN left_twips INTEGER;
ALTER TABLE FormControls ADD COLUMN top_twips INTEGER;
ALTER TABLE FormControls ADD COLUMN width_twips INTEGER;
ALTER TABLE FormControls ADD COLUMN height_twips INTEGER;
ALTER TABLE FormControls ADD COLUMN tab_index INTEGER;
ALTER TABLE FormControls ADD COLUMN visible INTEGER DEFAULT 1;
ALTER TABLE FormControls ADD COLUMN enabled INTEGER DEFAULT 1;
ALTER TABLE FormControls ADD COLUMN locked INTEGER DEFAULT 0;
ALTER TABLE FormControls ADD COLUMN associated_label TEXT;
ALTER TABLE FormControls ADD COLUMN row_source TEXT;
ALTER TABLE FormControls ADD COLUMN row_source_type TEXT;
```

New columns on `Forms` table:

```sql
ALTER TABLE Forms ADD COLUMN inside_width INTEGER;
ALTER TABLE Forms ADD COLUMN inside_height INTEGER;
ALTER TABLE Forms ADD COLUMN default_view TEXT;
ALTER TABLE Forms ADD COLUMN is_popup INTEGER DEFAULT 0;
ALTER TABLE Forms ADD COLUMN is_modal INTEGER DEFAULT 0;
ALTER TABLE Forms ADD COLUMN border_style TEXT;
ALTER TABLE Forms ADD COLUMN allow_additions INTEGER DEFAULT 1;
ALTER TABLE Forms ADD COLUMN allow_edits INTEGER DEFAULT 1;
ALTER TABLE Forms ADD COLUMN allow_deletions INTEGER DEFAULT 1;
ALTER TABLE Forms ADD COLUMN form_classification TEXT;
```

New table for tab control pages:

```sql
CREATE TABLE IF NOT EXISTS FormTabPages (
    tab_page_id     INTEGER PRIMARY KEY AUTOINCREMENT,
    form_object_id  INTEGER NOT NULL REFERENCES AccessObjects(object_id),
    tab_control_name TEXT NOT NULL,
    page_index      INTEGER NOT NULL,
    page_name       TEXT,
    page_caption     TEXT,
    control_names_json TEXT  -- JSON array of control names on this page
);
```

---

## Phase 2: Generator CLI — Blazor Project Generation

### Generated Project Structure

```
GeneratedApp/
├── GeneratedApp.csproj
├── Program.cs                          # DI registration with stub/real swap
├── App.razor
├── _Imports.razor
├── Properties/
│   └── launchSettings.json             # "Stubbed" and "Full" profiles
│
├── Models/                             # Generated from table/query metadata
│   ├── CustomerRecord.cs
│   ├── OrderRecord.cs
│   └── ...
│
├── Services/
│   ├── Abstractions/                   # Generated interfaces
│   │   ├── ICustomerDataProvider.cs    # extends IGridDataProvider<CustomerRecord>
│   │   ├── IOrderDataProvider.cs
│   │   └── ...
│   ├── Stub/                           # Stubbed implementations
│   │   ├── StubCustomerDataProvider.cs # In-memory filterable sample data
│   │   ├── StubOrderDataProvider.cs
│   │   ├── StubEntityMetadataProvider.cs
│   │   ├── StubNavigationDataProvider.cs
│   │   ├── StubCatalogProvider.cs
│   │   └── SampleData.cs              # All sample data in one place
│   └── Real/                           # Empty stubs for later implementation
│       └── (generated as empty classes inheriting interfaces)
│
├── Pages/
│   ├── Index.razor                     # Dashboard/switchboard
│   ├── CustomerSearch.razor            # DabblerQueryPage<CustomerRecord>
│   ├── CustomerDetail.razor            # DabblerDetailForm<CustomerRecord>
│   ├── OrderSearch.razor
│   ├── OrderDetail.razor
│   └── ...
│
├── Shared/
│   ├── MainLayout.razor                # DabblerShell + navigation
│   ├── EntityMetadata.cs               # Generated EntityMetadata definitions
│   └── NavigationConfig.cs             # Route mappings from navigation manifest
│
└── wwwroot/
    └── css/
        └── app.css
```

### DI Registration with Configuration Swap

```csharp
// Program.cs
var useStubs = builder.Configuration.GetValue<bool>("UseStubs", true);

if (useStubs)
{
    builder.Services.AddScoped<IGridDataProvider<CustomerRecord>, StubCustomerDataProvider>();
    builder.Services.AddScoped<IGridDataProvider<OrderRecord>, StubOrderDataProvider>();
    builder.Services.AddScoped<IEntityMetadataProvider, StubEntityMetadataProvider>();
    builder.Services.AddScoped<INavigationDataProvider, StubNavigationDataProvider>();
    builder.Services.AddScoped<IArtifactCatalogProvider, StubCatalogProvider>();
    // ... per entity
}
else
{
    builder.Services.AddScoped<IGridDataProvider<CustomerRecord>, CustomerDataProvider>();
    builder.Services.AddScoped<IGridDataProvider<OrderRecord>, OrderDataProvider>();
    // ... real implementations
}
```

### Launch Profiles

```json
{
  "profiles": {
    "Stubbed": {
      "commandName": "Project",
      "launchBrowser": true,
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "UseStubs": "true"
      }
    },
    "Full": {
      "commandName": "Project",
      "launchBrowser": true,
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "UseStubs": "false"
      }
    }
  }
}
```

### Template Selection by Form Classification

| Classification | Blazor Template | Dabbler Component |
|---------------|----------------|-------------------|
| `Search` | Search page with filter grid + data grid, row click navigates to detail | `DabblerQueryPage<T>` |
| `Detail` | Detail form with field layout matching Access spatial positions | `DabblerDetailForm<T>` |
| `Switchboard` | Dashboard with navigation cards per button | `DabblerDashboardPage` |
| `Dialog` | Modal popup form | `DabblerRecordPopup<T>` |
| `ReportLauncher` | Report page with parameter form | `DabblerReportPage` |
| `Continuous` | Grid with inline editing, similar to Search | `DabblerQueryPage<T>` with `Editable=true` |
| `Datasheet` | Pure data grid | `DabblerQueryPage<T>` |

### Stub Service Behavior

Stub services are more than simple mocks — they simulate realistic behavior:

**Search/Grid Data Providers:**
- Hold 10–25 sample records in memory (from layout contract `sampleRows`)
- Implement `IGridDataProvider<T>.FetchAsync` with real filtering: string fields use `Contains`, date fields use range comparison, numeric fields use equality
- Support sorting and paging against the in-memory collection
- Return accurate `TotalCount` in `GridDataResponse<T>`

**Detail Data Providers:**
- Return a specific sample record by primary key
- On "Save", show a toast: "Save would persist {N} modified fields for {EntityName} #{PrimaryKey}"
- On "Delete", show a toast with confirmation: "Delete would remove {EntityName} #{PrimaryKey}"

**Navigation Providers:**
- Return sample `NavigationRecord` lists derived from the sample data
- Smart filter queries return filtered subsets of sample data

**Entity Metadata Provider:**
- Return `EntityMetadata` objects generated from the layout contracts
- Field definitions, display names, data types, and relationships all populated from Access metadata

### Search → Detail Navigation Flow

For each Search form, the generator creates a row-click handler that:
1. Extracts the primary key from the clicked row
2. Calls `NavigationManager.NavigateTo($"/{entityRoute}/{primaryKey}")`
3. The detail page reads the primary key from the route parameter
4. The stub data provider returns the matching sample record
5. The detail form renders with sample data populated

This means a stakeholder can: open the app → see the switchboard → click "Customers" → see the customer search with sample data → filter by name → click a row → see the customer detail form with that record's data → click the browser back button → return to the search. All of this works before any real service is written.

---

## Phase 3: Playwright Test Generation

### Test Architecture

Two tiers of tests, generated from the same layout contracts:

**Tier 1: Layout Tests** (run against stubbed UI, continue passing after real services are wired up)
- Spatial relationship assertions (same row, vertical order, full width)
- Tab order verification
- Component rendering (correct control types, column counts)
- Section grouping and fieldset boundaries
- Container width constraints
- Responsive breakpoint behavior

**Tier 2: Navigation Tests** (run against stubbed UI)
- Every route in the navigation manifest is reachable
- Search → Detail click-through for each search form
- Switchboard button navigation
- Browser back/forward preserves state
- Toast messages appear for non-navigation events
- Dialog/popup forms open and close correctly

### Generated Test Structure

```
tests/
├── AccessToBlazor.LayoutTests/
│   ├── AccessToBlazor.LayoutTests.csproj
│   ├── Infrastructure/
│   │   ├── LayoutTestBase.cs           # Shared setup, Playwright browser launch
│   │   ├── LayoutAssertions.cs         # Custom assertions: SameRow, VerticalOrder, FullWidth
│   │   └── TestServerFixture.cs        # Starts the Blazor app for testing
│   │
│   ├── Layout/                         # Tier 1: generated from layout contracts
│   │   ├── CustomerSearchLayoutTests.cs
│   │   ├── CustomerDetailLayoutTests.cs
│   │   ├── OrderSearchLayoutTests.cs
│   │   └── ...
│   │
│   └── Navigation/                     # Tier 2: generated from navigation manifest
│       ├── NavigationManifestTests.cs   # All routes reachable
│       ├── SearchToDetailTests.cs      # Click-through for each search form
│       ├── SwitchboardNavigationTests.cs
│       └── ToastEventTests.cs          # Non-navigation events show toasts
```

### Test Base Class

```csharp
public abstract class LayoutTestBase : IAsyncLifetime
{
    protected IPage Page { get; private set; } = null!;
    protected string BaseUrl { get; private set; } = null!;
    private IBrowser _browser = null!;
    private IBrowserContext _context = null!;

    public async Task InitializeAsync()
    {
        var playwright = await Playwright.CreateAsync();
        _browser = await playwright.Chromium.LaunchAsync();
        _context = await _browser.NewContextAsync();
        Page = await _context.NewPageAsync();
        BaseUrl = TestServerFixture.Instance.BaseUrl;
    }

    // Shared assertion helpers
    protected async Task AssertSameRow(string testId1, string testId2, double tolerancePx = 5)
    {
        var box1 = await Page.Locator($"[data-testid='{testId1}']").BoundingBoxAsync();
        var box2 = await Page.Locator($"[data-testid='{testId2}']").BoundingBoxAsync();
        Assert.True(Math.Abs(box1!.Y - box2!.Y) < tolerancePx,
            $"{testId1} (Y={box1.Y}) and {testId2} (Y={box2.Y}) are not on the same row");
    }

    protected async Task AssertVerticalOrder(params string[] testIds)
    {
        float lastY = -1;
        foreach (var testId in testIds)
        {
            var box = await Page.Locator($"[data-testid='{testId}']").BoundingBoxAsync();
            Assert.True(box!.Y > lastY,
                $"{testId} (Y={box.Y}) should be below previous field (Y={lastY})");
            lastY = box.Y;
        }
    }

    protected async Task AssertFullWidth(string fieldTestId, string containerTestId, double minRatio = 0.9)
    {
        var fieldBox = await Page.Locator($"[data-testid='{fieldTestId}']").BoundingBoxAsync();
        var containerBox = await Page.Locator($"[data-testid='{containerTestId}']").BoundingBoxAsync();
        var ratio = fieldBox!.Width / containerBox!.Width;
        Assert.True(ratio > minRatio,
            $"{fieldTestId} width ratio {ratio:F2} — expected > {minRatio}");
    }

    protected async Task AssertToastOnClick(string buttonTestId)
    {
        await Page.Locator($"[data-testid='{buttonTestId}']").ClickAsync();
        var toast = Page.Locator(".mud-snackbar");
        await Expect(toast.First).ToBeVisibleAsync();
        await Page.WaitForTimeoutAsync(1500);
    }
}
```

### Navigation Manifest Test Generation

For each route in the manifest, generate:

```csharp
[Fact]
public async Task Navigate_frmSwitchboard_To_frmCustomerSearch()
{
    await Page.GotoAsync(BaseUrl + "/");  // switchboard is entry point
    await Page.Locator("[data-testid='nav-cmdCustomers']").ClickAsync();
    await Page.WaitForURLAsync("**/customer-search");
    var heading = Page.Locator("[data-testid='page-title']");
    await Expect(heading).ToContainTextAsync("Customer");
}
```

For each search form with detail navigation:

```csharp
[Fact]
public async Task SearchToDetail_CustomerSearch_ClickRow_OpensDetail()
{
    await Page.GotoAsync(BaseUrl + "/customer-search");
    // Wait for sample data to load
    var firstRow = Page.Locator("[data-testid='grid-customers'] tbody tr").First;
    await Expect(firstRow).ToBeVisibleAsync();
    // Click the first row
    await firstRow.ClickAsync();
    // Should navigate to detail page
    await Page.WaitForURLAsync("**/customer/*");
    // Detail form should have data populated
    var nameField = Page.Locator("[data-testid='field-CustomerName'] input");
    var value = await nameField.InputValueAsync();
    Assert.False(string.IsNullOrEmpty(value), "Customer name should be populated");
}
```

---

## CLI Interface

### Extractor CLI

```bash
# Extract all forms
dotnet run --project src/AccessToBlazor.Extractor -- \
    --input "C:/databases/Northwind.accdb" \
    --output "./contracts/" \
    --all

# Extract specific forms
dotnet run --project src/AccessToBlazor.Extractor -- \
    --input "C:/databases/Northwind.accdb" \
    --output "./contracts/" \
    --forms "frmCustomer,frmOrders,frmSwitchboard"

# Extract with sample data from actual database (anonymized)
dotnet run --project src/AccessToBlazor.Extractor -- \
    --input "C:/databases/Northwind.accdb" \
    --output "./contracts/" \
    --all \
    --sample-real-data \
    --sample-rows 15
```

**Outputs:**
- `contracts/{formName}.layout.json` — one per form
- `contracts/navigation-manifest.json` — navigation graph
- `contracts/metadata.db` — SQLite metadata database (enhanced schema)

### Generator CLI

```bash
# Generate stubbed Blazor project
dotnet run --project src/AccessToBlazor.Generator -- \
    --contracts "./contracts/" \
    --output "./GeneratedApp/" \
    --namespace "Northwind.Web" \
    --dabbler-version "1.0.0"

# Generate Playwright tests
dotnet run --project src/AccessToBlazor.Generator -- \
    --contracts "./contracts/" \
    --output "./tests/GeneratedApp.LayoutTests/" \
    --generate-tests \
    --test-base-url "https://localhost:5001"

# Generate both
dotnet run --project src/AccessToBlazor.Generator -- \
    --contracts "./contracts/" \
    --output-app "./GeneratedApp/" \
    --output-tests "./tests/GeneratedApp.LayoutTests/" \
    --namespace "Northwind.Web" \
    --dabbler-version "1.0.0"
```

---

## File Structure

```
src/
├── AccessToBlazor.Extractor/
│   ├── AccessToBlazor.Extractor.csproj
│   ├── Program.cs                      # CLI entry point, argument parsing
│   ├── LayoutExtractor.cs              # Orchestrates extraction pipeline
│   ├── LayoutAnalyzer.cs               # Row/column inference from twips
│   ├── FormClassifier.cs               # Classifies forms by archetype
│   ├── NavigationManifestBuilder.cs    # Builds navigation graph from macros/VBA
│   ├── SampleDataGenerator.cs          # Generates realistic fake data
│   ├── LabelAssociator.cs              # Associates labels with fields
│   ├── TabControlEnumerator.cs         # Enumerates tab control pages
│   └── ContractWriter.cs              # Writes layout contracts + manifest as JSON
│
├── AccessToBlazor.Generator/
│   ├── AccessToBlazor.Generator.csproj
│   ├── Program.cs                      # CLI entry point
│   ├── ProjectGenerator.cs             # Orchestrates project scaffolding
│   ├── PageGenerators/
│   │   ├── SearchPageGenerator.cs      # Generates DabblerQueryPage-based search
│   │   ├── DetailPageGenerator.cs      # Generates DabblerDetailForm-based detail
│   │   ├── SwitchboardPageGenerator.cs # Generates DabblerDashboardPage
│   │   ├── DialogPageGenerator.cs      # Generates DabblerRecordPopup usage
│   │   └── ReportLauncherPageGenerator.cs
│   ├── ServiceGenerators/
│   │   ├── StubDataProviderGenerator.cs
│   │   ├── StubMetadataProviderGenerator.cs
│   │   ├── InterfaceGenerator.cs
│   │   └── DiRegistrationGenerator.cs
│   ├── TestGenerators/
│   │   ├── LayoutTestGenerator.cs
│   │   ├── NavigationTestGenerator.cs
│   │   └── TestInfraGenerator.cs
│   ├── ModelGenerator.cs               # Generates C# record types from table metadata
│   ├── EntityMetadataGenerator.cs      # Generates EntityMetadata definitions
│   └── RouteMapper.cs                  # Maps Access form names to Blazor routes
│
└── AccessToBlazor.Contracts/           # Shared contract types
    ├── AccessToBlazor.Contracts.csproj
    ├── LayoutContract.cs               # Strongly-typed layout contract model
    ├── NavigationManifest.cs           # Strongly-typed navigation manifest model
    └── FormClassification.cs           # Classification enum
```

---

## Session Plan

**Note:** Each session ends with a **mandatory verification step** where all changed files are routed to a cross-provider verifier. The verifier's raw output is saved to `session-reviews/session-NNN.md`. If issues are found, the orchestrator fixes them and tracks each issue in `issue-logs/session-NNN.json`. This is handled automatically per the CLAUDE.md workflow — it is not listed as a separate step below but is always performed.

### Session 1 of 6: Contract Types + Enhanced Model + Schema Extensions

**Steps:**
1. Read progress from router-state.db if it exists. Verify prerequisite: `Dabbler.Discovery` builds successfully.
2. Create `src/AccessToBlazor.Contracts/` project with strongly-typed models for:
   - `LayoutContract` (mirrors the JSON schema from the layout pipeline doc, plus new `formClassification`, `formProperties`, `recordSource`, and `primaryKey` fields)
   - `NavigationManifest` (routes, entry points, form classifications)
   - `FormClassification` enum (`Search`, `Detail`, `Switchboard`, `Dialog`, `ReportLauncher`, `Continuous`, `Datasheet`)
   - `FieldContract`, `SectionContract`, `ButtonContract`, `GridColumnContract`, `TabPageContract`
   - JSON serialization attributes for all models
3. Extend `FormControlInfo` in `Dabbler.Discovery.Models` with: `Left`, `Top`, `Width`, `Height`, `TabIndex`, `Visible`, `Enabled`, `Locked`, `AssociatedLabelCaption`, `RowSource`, `RowSourceType`, `ColumnCount`, `BoundColumn`, `ColumnWidths`, `EnterKeyBehavior`, `ScrollBars`.
4. Extend `FormInfo` with: `InsideWidth`, `InsideHeight`, `DefaultView`, `PopUp`, `Modal`, `BorderStyle`, `AllowAdditions`, `AllowEdits`, `AllowDeletions`, `NavigationButtons`, `FormClassification`.
5. Add `TabPages` list property to `FormControlInfo` for tab controls: `List<TabPageInfo>` where `TabPageInfo { Name, Caption, ControlNames[] }`.
6. Update `metadata-schema.sql` with the new columns on `FormControls` and `Forms` tables, plus the new `FormTabPages` table.
7. Update `MetadataPopulator` to persist the new fields to the database.
8. Build and verify: `dotnet build` succeeds for the full solution.

**Creates:** `AccessToBlazor.Contracts/` project with all contract models
**Touches:** `FormControlInfo.cs`, `FormInfo.cs`, `metadata-schema.sql`, `MetadataPopulator.cs`
**Ends with:** `dotnet build` succeeds
**Progress keys:** `session-1/contracts`, `session-1/model-extensions`, `session-1/schema-extensions`, `session-1/populator-update`

### Session 2 of 6: Enhanced COM Extraction + Layout Analysis + Form Classification

**Steps:**
1. Verify Session 1 outputs exist.
2. Update `ComAutomationInspector.EnrichForms` to harvest all new spatial properties (`Left`, `Top`, `Width`, `Height`, `TabIndex`) and display properties (`Visible`, `Enabled`, `Locked`, `EnterKeyBehavior`, `ScrollBars`, `RowSource`, `RowSourceType`, `ColumnCount`, `BoundColumn`, `ColumnWidths`). Each property read is wrapped in try/catch (consistent with existing pattern).
3. Add form-level property extraction: `InsideWidth`, `InsideHeight`, `DefaultView`, `PopUp`, `Modal`, `BorderStyle`, `AllowAdditions`, `AllowEdits`, `AllowDeletions`, `NavigationButtons`.
4. Implement `LabelAssociator` — attempts `ctl.Controls(0)` for paired labels, falls back to spatial proximity matching (label within threshold left-of or above a field).
5. Implement `TabControlEnumerator` — for `acTabCtl` controls, enumerate `.Pages`, record page name/caption, and determine which controls belong to each page.
6. Implement `FormClassifier` — classifies each form based on the criteria in the spec (record source presence, control types, button events, form properties).
7. Implement `LayoutAnalyzer` — the row/column inference algorithm from the layout pipeline doc: normalize baseline height, group into rows by Top proximity, sort by Left within rows, calculate relative widths/heights, detect column spans, detect sections via standalone labels and vertical gaps.
8. Build and verify: `dotnet build` succeeds.

**Creates:** `FormClassifier.cs`, `LayoutAnalyzer.cs`, `LabelAssociator.cs`, `TabControlEnumerator.cs`
**Touches:** `ComAutomationInspector.cs`
**Ends with:** `dotnet build` succeeds
**Progress keys:** `session-2/com-extraction`, `session-2/label-association`, `session-2/tab-enumeration`, `session-2/form-classification`, `session-2/layout-analysis`

### Session 3 of 6: Navigation Manifest + Sample Data + Extractor CLI

**Steps:**
1. Verify Sessions 1–2 outputs exist.
2. Implement `NavigationManifestBuilder` — walks all macros, embedded macros, and VBA procedures to find `DoCmd.OpenForm` and `DoCmd.OpenReport` calls. For each, records source form, target form/report, trigger (control + event), parameters, and open args. Uses existing `MacroDependencyExtractor` output as a starting point and enriches with parameter details.
3. Implement `SampleDataGenerator` — generates realistic sample data based on field name heuristics, data types, validation rules, and combo box options. Produces per-field sample values and per-search-form sample row sets. Uses deterministic seeding for reproducibility.
4. Implement `ContractWriter` — takes the analyzed layout, sample data, form classification, and navigation manifest and writes:
   - One `.layout.json` file per form (conforming to `LayoutContract` model)
   - One `navigation-manifest.json` file (conforming to `NavigationManifest` model)
5. Create `AccessToBlazor.Extractor` console app project with `Program.cs`:
   - Argument parsing: `--input`, `--output`, `--forms` / `--all`, `--sample-real-data`, `--sample-rows`
   - Pipeline orchestration: open Access → run inspectors → analyze layout → classify forms → build navigation manifest → generate sample data → write contracts
   - Error handling and progress reporting to console
6. Build and verify: `dotnet build` succeeds.

**Creates:** `AccessToBlazor.Extractor/` project, `NavigationManifestBuilder.cs`, `SampleDataGenerator.cs`, `ContractWriter.cs`
**Touches:** None (all new files)
**Ends with:** `dotnet build` succeeds
**Progress keys:** `session-3/navigation-manifest`, `session-3/sample-data`, `session-3/contract-writer`, `session-3/extractor-cli`

### Session 4 of 6: Blazor Project Generator — Pages + Models + Shell

**Steps:**
1. Verify Sessions 1–3 outputs exist.
2. Create `AccessToBlazor.Generator` console app project with `Program.cs` and argument parsing.
3. Implement `ModelGenerator` — reads layout contracts and generates C# record types for each entity (one per form with a record source). Fields map from contract field definitions with correct .NET types.
4. Implement `EntityMetadataGenerator` — generates `EntityMetadata` definitions from layout contracts, including `FieldMetadata` for each field, `ChildRelationshipMetadata` for subform links, and display names from labels.
5. Implement `RouteMapper` — maps Access form names to clean Blazor routes (e.g., `frmCustomerSearch` → `/customer-search`, `frmCustomerDetail` → `/customer/{id}`).
6. Implement `SearchPageGenerator` — generates `.razor` / `.razor.cs` files using `DabblerQueryPage<T>` with `FilterColumnDefinition`s from the contract, `GridColumns` render fragment, row click handler that navigates to the detail route, and `data-testid` attributes on all elements.
7. Implement `DetailPageGenerator` — generates `.razor` / `.razor.cs` files using `DabblerDetailForm<T>` with `EntityMetadata` from the contract, Save/Delete handlers as toast stubs, and `data-testid` attributes.
8. Implement `SwitchboardPageGenerator` — generates `DabblerDashboardPage` with `DabblerNavigationCard` for each button, click handlers from the navigation manifest.
9. Implement `DialogPageGenerator` — generates `DabblerRecordPopup<T>` usage for dialog-classified forms.
10. Implement `ReportLauncherPageGenerator` — generates `DabblerReportPage` with parameter definitions from the contract.
11. Implement `ProjectGenerator` — orchestrates all generators and produces: `.csproj`, `Program.cs`, `App.razor`, `_Imports.razor`, `MainLayout.razor` (with `DabblerShell`), `launchSettings.json`, `app.css`, and all page/model/metadata files.
12. Build and verify: `dotnet build` succeeds.

**Creates:** `AccessToBlazor.Generator/` project, all page generators, model generator, entity metadata generator, route mapper, project generator
**Touches:** None (all new files)
**Ends with:** `dotnet build` succeeds
**Progress keys:** `session-4/generator-cli`, `session-4/model-gen`, `session-4/metadata-gen`, `session-4/search-gen`, `session-4/detail-gen`, `session-4/switchboard-gen`, `session-4/dialog-gen`, `session-4/report-gen`, `session-4/project-gen`

### Session 5 of 6: Stub Services + DI Registration + Navigation Wiring

**Steps:**
1. Verify Sessions 1–4 outputs exist.
2. Implement `InterfaceGenerator` — generates `IXxxDataProvider` interfaces extending `IGridDataProvider<T>` for each entity, plus any entity-specific methods (e.g., `GetByIdAsync`).
3. Implement `StubDataProviderGenerator` — generates stub implementations that:
   - Hold sample data from the layout contracts in a `List<T>`
   - Implement `FetchAsync` with real in-memory filtering (string `Contains`, date range, numeric equality), sorting (via reflection or compiled expressions), and paging
   - Implement `GetByIdAsync` by primary key lookup
   - Return accurate `GridDataResponse<T>` with correct `TotalCount`
4. Implement `StubMetadataProviderGenerator` — generates `StubEntityMetadataProvider` returning `EntityMetadata` for all entities, and `StubNavigationDataProvider` returning sample navigation records.
5. Implement `StubCatalogProviderGenerator` — generates `StubCatalogProvider` returning static catalog entries for all forms, queries, and reports.
6. Implement `DiRegistrationGenerator` — generates the `Program.cs` DI registration block with the `UseStubs` configuration switch, registering all stub or real implementations per entity.
7. Wire navigation in the shell: `DabblerShell` factory functions (`DetailFormFactory`, `SearchPageFactory`, `NewRecordFactory`, `ReportPageFactory`) route to the correct generated pages based on entity name. The navigation manifest drives the `DabblerNavigationPanel` entity list and smart filter configuration.
8. Wire search-to-detail navigation: row click on search grid → `NavigationManager.NavigateTo` → detail page reads route parameter → stub provider returns matching sample record.
9. Wire all non-navigation events to toast handlers: Save, Delete, custom buttons all call `ISnackbar.Add(...)` with descriptive messages including the event name, entity, and any relevant field values.
10. Build and verify: `dotnet build` succeeds.

**Creates:** All interface, stub service, and DI registration generators
**Touches:** `ProjectGenerator.cs` (integrates service generators into project output)
**Ends with:** `dotnet build` succeeds
**Progress keys:** `session-5/interfaces`, `session-5/stub-providers`, `session-5/stub-metadata`, `session-5/di-registration`, `session-5/navigation-wiring`, `session-5/toast-events`

### Session 6 of 6: Playwright Test Generation + Demo + Documentation

**Steps:**
1. Verify Sessions 1–5 outputs exist.
2. Implement `TestInfraGenerator` — generates the test project scaffolding: `.csproj` (with Playwright and xUnit dependencies), `LayoutTestBase.cs` (shared assertions), `TestServerFixture.cs` (starts Blazor app in-process for testing), and `LayoutAssertions.cs`.
3. Implement `LayoutTestGenerator` — for each layout contract, generates a test class with:
   - Container width constraint test
   - Same-row assertions for all `sameRow` relationships
   - Full-width assertions for all `fullWidth` fields
   - Vertical order assertions matching contract field order
   - Tab order test following `tabOrderSequence`
   - Component type assertions (correct control renders for each field)
   - Grid column count assertions for subform sections
   - Section grouping assertions (fieldsets contain expected fields)
4. Implement `NavigationTestGenerator` — for each route in the navigation manifest, generates:
   - Route reachability test (navigate and assert page loads)
   - Search-to-detail click-through test for each search form
   - Switchboard button navigation tests
   - Toast event tests for all non-navigation buttons
5. Create a demo: use the generator against a sample set of hand-crafted layout contracts (representing a simplified Northwind-like database with Customers, Orders, Products, and a Switchboard). Run the generator to produce a complete demo project. Verify it builds and pages render.
6. Add XML doc comments to all public types in all three projects.
7. Create a `README.md` in `src/AccessToBlazor.Extractor/` and `src/AccessToBlazor.Generator/` documenting CLI usage, contract schemas, and the overall pipeline.
8. Run full solution build, fix any warnings.
9. Generate `change-log.md` in the session set folder.

**Creates:** Test generators, test infrastructure, demo contracts, demo generated project, README files, `change-log.md`
**Touches:** All generator files (doc comments)
**Ends with:** `dotnet build` succeeds with no warnings; demo project builds and pages render; generated tests compile
**Progress keys:** `session-6/test-infra`, `session-6/layout-tests`, `session-6/navigation-tests`, `session-6/demo`, `session-6/documentation`, `session-6/change-log`

---

## Code Style

- **Namespace:** `AccessToBlazor.Contracts` for shared contract types; `AccessToBlazor.Extractor` for extraction code; `AccessToBlazor.Generator` for generation code
- **Naming:** PascalCase for public members, `_camelCase` for private fields
- **Async:** All I/O methods return `Task`, suffixed with `Async`
- **Null safety:** Nullable reference types enabled. Check nulls defensively.
- **COM interop:** Every COM property read is wrapped in individual try/catch (consistent with existing `ComAutomationInspector` pattern)
- **JSON serialization:** Use `System.Text.Json` with `JsonPropertyName` attributes for contract models. Use `JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }`
- **Code generation:** Generated `.razor` and `.cs` files include a header comment indicating they are auto-generated, the source contract file, and generation timestamp
- **Test assertions:** Use `Microsoft.Playwright.Assertions` (`Expect`) for async assertions, `Xunit.Assert` for synchronous assertions
- **data-testid convention:** `page-{name}` for page containers, `section-{name}` for sections, `field-{name}` for field wrappers, `grid-{name}` for data grids, `btn-{name}` for buttons, `nav-{name}` for navigation elements

## Testing

```bash
# Build everything
dotnet build

# Run the extractor (requires Access on Windows)
dotnet run --project src/AccessToBlazor.Extractor -- \
    --input "path/to/database.accdb" --output "./contracts/" --all

# Run the generator
dotnet run --project src/AccessToBlazor.Generator -- \
    --contracts "./contracts/" --output-app "./GeneratedApp/" \
    --output-tests "./tests/" --namespace "MyApp.Web"

# Build and run the generated app
cd GeneratedApp && dotnet run

# Run layout/navigation tests
cd tests/GeneratedApp.LayoutTests && dotnet test
```

The generated demo project is the primary verification mechanism for this session set. The extractor is validated by inspecting the output contracts for completeness. The generator is validated by building and running the generated project.

---

## AI Router

This project includes an AI Router module (`ai-router/`) that routes reasoning tasks to cheaper AI models and verifies critical results cross-provider. Use it to save costs and catch errors.

### When to use the router

Use `ai_router.route()` for tasks that are pure reasoning — no file I/O needed.

- Code review → `route(code, task_type="code-review")`
- Documentation → `route(code, task_type="documentation")`
- Analysis → `route(question, task_type="analysis")`

Do NOT use the router for:
- Creating or modifying files (do it directly)
- Running shell commands (do it directly)
- Anything that needs the filesystem (do it directly)

### How to use it

```python
from ai_router import route, verify, print_cost_report

result = route(
    content="<the text to analyze or process>",
    task_type="code-review",
    context="<optional background>"
)
print(result.content)
print(result.model_name)
print(result.total_cost_usd)

# At session end
print_cost_report()
```

### Session management

1. **At session start:** Read which session you're executing from the human's instruction. Check `router-state.db` for progress from prior sessions. Verify that prerequisites exist.
2. **During the session:** After each step, update progress:
   ```python
   from ai_router.state import StateDB
   db = StateDB("router-state.db")
   db.set_progress("session-1/contracts", "complete")
   ```
3. **At session end:** Run build and tests. Set the session-level progress key. Call `print_cost_report()`.
4. **Do NOT try to do more than your assigned session.**

### Spec-driven workflow

1. Read this spec file and the session plan (given by the human)
2. Identify which session you are executing
3. Check router-state.db for prior session progress
4. For each step: implement directly or route to a cheap model
5. Track progress in router-state.db after each step
6. Run build and tests — must succeed before the session is considered done
7. At the end: call print_cost_report()
8. On the LAST session only: generate change-log.md in the session set folder

## Implementation Specs
- `docs/specs/access-to-blazor-cli-spec.md` — this file
