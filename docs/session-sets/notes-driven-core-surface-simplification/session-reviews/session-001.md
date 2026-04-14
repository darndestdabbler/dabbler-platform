# Verification Round 1



# Session 1 Verification: Supported Surface Definitions And Coverage Gate Baseline

## Progress Key Checklist

| Progress Key | Status | Evidence |
|---|---|---|
| `session-1/supported-surface` | ✅ Pass | `docs/planning/notes-driven-supported-library-surface.md` defines explicit supported/non-core boundaries for all 5 major areas |
| `session-1/coverage-gate-baseline` | ✅ Pass | `Dabbler.Components.Tests` and `Dabbler.Reports.Blazor.Tests` created, added to solution, baseline smoke tests pass |
| `session-1/docs-narrowing` | ✅ Pass | `src/Libs/README.md`, `Dabbler.Components/README.md`, and `Dabbler.Reports.Blazor/README.md` all narrowed to lead with retained surface and demote non-core |

## Step-by-Step Verification

### Step 1: Supported-Surface Definitions

**Requirement:** Explicit definitions for grid/filter/query, combo/detail/popup, shell/dashboard/catalog, reports, and security UI.

| Area | Supported Core Defined | Non-Core/Retirement Defined |
|---|---|---|
| Grid, Filter, Query | ✅ `DabblerQueryPage<T>`, `DabblerDataGrid<T>`, `DabblerFilterGrid<T>` | ✅ Grouping, aggregates, clipboard, CSV, pinning, quick filter, presets |
| Combo, Detail, Popup | ✅ `DabblerComboBox<T>`, multiselect, `DabblerDetailForm<T>`, `DabblerRecordPopup<T>` | ✅ Add-new flows, grouping headers, `DabblerMultiComboBox<T>` as required long-term API |
| Shell, Dashboard, Catalog | ✅ `DabblerShell`, nav/runtime providers, `DabblerDashboardPage` | ✅ Favorites/recent widgets, catalog browsing, `DabblerCatalogPage` retirement candidate |
| Reports | ✅ `DabblerReportPage`, `ParameterPromptForm`, `ReportViewer` | ✅ `DabblerQueryReportPage<T>` retirement candidate |
| Security UI | ✅ Runtime auth, role-aware UI, existing security test suites | ✅ MFA/external-login breadth scoped out of main contract |

**Verdict:** All 5 areas have explicit supported-core and non-core/retirement boundaries. ✅

### Step 2: Contract-Test Homes

**`Dabbler.Components.Tests`:**
- ✅ `.csproj` exists with correct SDK (`Microsoft.NET.Sdk.Razor`), TFM (`net10.0`), bunit/xunit/FluentAssertions references
- ✅ `ProjectReference` to `Dabbler.Components`
- ✅ `_Imports.razor` with `@using Dabbler.Components`
- ✅ `ContractHomeSmokeTests.cs` asserts all 8 retained core types are resolvable
- ✅ `README.md` lists coverage roadmap for later sessions
- ✅ Added to `Dabbler.Platform.sln` with GUID `{FDD7533B-...}` and all build configurations

**`Dabbler.Reports.Blazor.Tests`:**
- ✅ `.csproj` exists with correct SDK, TFM, dependencies
- ✅ `ProjectReference` to `Dabbler.Reports.Blazor`
- ✅ `_Imports.razor` with both `Dabbler.Components` and `Dabbler.Reports.Blazor` usings
- ✅ `ContractHomeSmokeTests.cs` asserts all 3 retained report types are resolvable
- ✅ `README.md` lists coverage roadmap
- ✅ Added to solution with GUID `{4B3ABFF8-...}` and all build configurations

**Build/Test confirmation:** Both new test projects build and pass. Existing suite unbroken. ✅

### Step 3: Docs Narrowing

| Document | Leads with retained surface | Demotes non-core | Retirement candidates called out |
|---|---|---|---|
| `src/Libs/README.md` | ✅ Lists supported contract doc, all 4 test homes | ✅ No longer advertises broad historical surface | N/A (index doc) |
| `Dabbler.Components/README.md` | ✅ Supported Core table with 9 entries | ✅ "Advanced Or Non-Core" section | ✅ `DabblerCatalogPage` |
| `Dabbler.Reports.Blazor/README.md` | ✅ Supported Core table with 3 entries | ✅ Export demoted to supporting behavior | ✅ `DabblerQueryReportPage<T>` |

**Verdict:** Docs no longer advertise out-of-scope features as first-class defaults. ✅

## Structural Integrity

- **Solution file:** Both new test projects registered with full platform configuration matrix (Debug/Release × AnyCPU/x64/x86). No nesting under solution folders specified, which is consistent—test projects sit at solution root alongside existing test projects.
- **Note on `DabblerReportPage` location:** The smoke test resolves `Dabbler.Components.DabblerReportPage` (fully qualified), confirming the type lives in the Components assembly even though it's tested from the Reports test home. The `_Imports.razor` correctly imports both namespaces.
- **`Dabbler.Reports.Export` not in solution:** Noted but pre-existing—not a Session 1 concern.

## End-State Confirmation

> **"Ends with: The repo describes the intended supported contract and has a dedicated test home ready before retirements begin"**

✅ **Confirmed.** The supported-surface document is authoritative, both contract-test homes exist and pass, library-facing docs are narrowed, and the full solution builds and tests green. Sessions 2–5 can simplify against an explicit, test-owned contract.

---

# Verification Round 2

VERIFIED

All three spec steps are satisfied by the delivered artifacts:

1. **Supported-surface definitions** -- `docs/planning/notes-driven-supported-library-surface.md` provides explicit retained/non-core classifications for all five major areas (grid/filter/query, combo/detail/popup, shell/dashboard/catalog, reports, security UI), with clear "not part of the primary supported default" and retirement-candidate callouts.

2. **Contract-test homes** -- `tests/Dabbler.Components.Tests` and `tests/Dabbler.Reports.Blazor.Tests` are created with proper `.csproj` files, `_Imports.razor`, READMEs, and smoke tests that assert type availability for all 8 retained component types and 3 retained report types respectively. Both projects are wired into `Dabbler.Platform.sln` with full configuration platform entries. Build and test both succeeded.

3. **Docs narrowing** -- `src/Libs/README.md`, `src/Libs/Dabbler.Components/README.md`, and `src/Libs/Dabbler.Reports.Blazor/README.md` all lead with the retained contract, reference the surface definition doc, and demote non-core/retirement candidates (`DabblerCatalogPage`, `DabblerQueryReportPage<T>`, advanced grid/filter/combo extras) from first-class defaults.

Additional consistency checks:
- `DabblerReportPage` lives in `Dabbler.Components` namespace (referenced as `Dabbler.Components.DabblerReportPage` in the reports smoke test) -- consistent with the `_Imports.razor` importing both namespaces.
- Solution file includes both new test projects with correct GUIDs matching the project declarations.
- No `Dabbler.Reports.Export` project reference is needed in the test project since it tests Blazor UI types only.
- Progress keys (`session-1/supported-surface`, `session-1/coverage-gate-baseline`, `session-1/docs-narrowing`) are all addressed by the deliverables.
