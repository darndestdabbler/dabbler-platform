# Verification Round 1

## Session 6 Review: Organization Master Details Tab + OrganizationScopeFilter Seam

---

### 1. Bugs / Regressions

**No blocking bugs found.** The tab renders correctly, CRUD operations are structurally sound, and the dashboard tab-visibility rules are consistent with the privilege model.

One latent issue:

| Severity | Issue | Location | Detail |
|----------|-------|----------|--------|
| Low | `ApplyFilter` ignores actual operator from `FilterDescriptor` | `OrganizationMasterGrid.razor` L143–190 | Text columns always use `Contains`, booleans always use `Equals`, regardless of `entry.Operator`. If `FilterPanel` ever exposes operator switching for these columns, the grid won't respect it. Matches the Role tab pattern, so this is a known limitation, not a regression. |

---

### 2. Test Adequacy

**Coverage is adequate for the happy paths but has gaps on the edit path and validation edge cases.**

| Severity | Gap | File |
|----------|-----|------|
| **Medium** | No test for **editing** an existing organization (changing Name, toggling IsInternal) | `OrganizationDetailsFormTests.cs` |
| **Medium** | No test for **duplicate key rejection** — `SaveAsync` has an explicit duplicate-key check with a Snackbar error, but no test exercises it | `OrganizationDetailsFormTests.cs` |
| Low | No test for `ResetAsync` returning form to persisted state | `OrganizationDetailsFormTests.cs` |
| Low | No test for `SelectedOrganizationIdChanged` callback on row selection | `OrganizationMasterGridTests.cs` |
| Low | Verification `DabblerSecurityDbContext` returned from `RegisterDbContext` is never disposed | `OrganizationDetailsFormTests.cs` L63 |

Recommended additions (highest-value two):

```csharp
// 1. Edit existing organization
[Fact]
public void DetailsForm_EditExistingOrganization_PersistsChanges()
{
    // Seed org with Id=5, Name="Original"
    // Render with ForExisting(5)
    // Change name input to "Updated"
    // Click save
    // Assert db row has Name == "Updated"
}

// 2. Duplicate key rejected
[Fact]
public void DetailsForm_DuplicateKey_ShowsErrorAndDoesNotPersist()
{
    // Seed org with Key="existing-key"
    // Render with CreateNew()
    // Set key to "existing-key", name to anything
    // Click save
    // Assert only 1 row in db with that key
}
```

---

### 3. OrganizationScopeFilter Seam — Honest and Coherent

**Verdict: The deferral is well-justified and the implementation is clean.**

Evidence:
- `DeferredReason` constant explicitly names "Session Set C" and "OrganizationId" — anyone reading the code knows what's missing and why.
- `CanBypassOrganizationScope` encapsulates the cross-org bypass rule using `BakedInRolePrivileges.CanSeeAllOrganizations`, which is the exact decision Set C's `HasQueryFilter` implementation will need.
- `AcmeCoffeeDbContext` has **no fake filter** — it was left untouched rather than adding a no-op `HasQueryFilter`, which would have been misleading.
- Product/Customer/Order models have no `OrganizationId` column, confirming the filter truly cannot be applied yet.
- The class is intentionally **not registered in DI** — it's a pure logic seam, not a wired service. Set C will register it when consumed.

No flag raised.

---

### 4. Missing Work Still Required for Session 6

| Item | Status | Notes |
|------|--------|-------|
| OrganizationMasterDetailsTab with FilterPanel | ✅ Complete | Four filter columns: Key, Name, IsInternal, IsActive |
| MudDataGrid via DabblerSecurityDbContext | ✅ Complete | Includes ActiveMembershipCount projection |
| Details form: create/edit/archive | ✅ Complete | Create with duplicate-key guard, edit, archive with button gating |
| OrganizationScopeFilter.cs | ✅ Complete | Seam with bypass logic and deferred-reason constant |
| Global EF query filter on AcmeCoffeeDbContext | ⏳ Deferred to Set C | Correctly deferred — models lack OrganizationId |
| Org-scope integration tests | ⏳ Deferred to Set C | `OrganizationScopeFilterTests` covers the seam; real filter tests need OrganizationId |

**No work is missing that could be completed without pulling Set C forward.**

---

### 5. Severity-Ranked Findings

| # | Severity | Finding | File | Line(s) | Recommendation |
|---|----------|---------|------|---------|----------------|
| 1 | **Medium** | No test for the edit/update path through `SaveAsync` | `OrganizationDetailsFormTests.cs` | — | Add a test that seeds an org, renders `ForExisting(id)`, changes Name, clicks Save, asserts DB. |
| 2 | **Medium** | No test for duplicate-key rejection on create | `OrganizationDetailsFormTests.cs` | — | Add a test that seeds an org with key "x", tries to create another with key "x", asserts single row. |
| 3 | **Low** | `ApplyFilter` ignores `FilterOperators` value; hardcodes Contains/Equals | `OrganizationMasterGrid.razor` | 143–190 | Acceptable if FilterPanel doesn't expose operator changes for these columns; document or add operator dispatch if it does. |
| 4 | **Low** | Verification DbContext not disposed in test helper | `OrganizationDetailsFormTests.cs` | 63 | Wrap in `using` or implement `IDisposable` on the test class to dispose after each test. |
| 5 | **Info** | `OrganizationScopeFilter` not registered in DI | `OrganizationScopeFilter.cs` | — | Intentional for a seam. Set C should register it when the filter is wired. |

---

# Verification Round 2

## Session 6 Review — Severity-Ranked Findings

### S1 — Medium: No database-level unique constraint on `AppOrganization.Key`

**Location:** `OrganizationDetailsForm.razor` `SaveAsync`, and likely `DabblerSecurityDbContext.OnModelCreating` / `ApplyDabblerSecurity()`

**Issue:** The duplicate-key guard is application-level only (`AnyAsync` + conditional insert). Two concurrent create requests for the same key can both pass the check and both insert. The `DabblerSecurityDbContext` configuration (via `ApplyDabblerSecurity()`) should declare a unique index on `AppOrganization.Key`.

**Fix:** In whatever `IEntityTypeConfiguration<AppOrganization>` is called from `ApplyDabblerSecurity()`:
```csharp
builder.HasIndex(o => o.Key).IsUnique();
```
If no configuration class exists for `AppOrganization`, create one. The application-level check remains useful for a friendly Snackbar message but should not be the only guard.

---

### S2 — Low: Grid selection-changed callback has no test coverage

**Location:** `OrganizationMasterGridTests.cs`

**Issue:** `SelectedOrganizationIdChanged` is never exercised in any test. The grid's `OnSelectionChangedAsync` is the primary way the parent tab learns which organization to load in the details form.

**Fix:** Add a test that programmatically triggers a row selection and asserts the callback fires with the correct `int?`:
```csharp
[Fact]
public void Grid_RowSelection_RaisesSelectedOrganizationIdChanged()
{
    using var ctx = new TestContext();
    int? receivedId = null;
    ctx.SetupForDabbler(configureServices: svc => RegisterDbContext(svc, SeedOrganizations));

    var cut = ctx.RenderComponent<OrganizationMasterGrid>(p => p
        .Add(x => x.SelectedOrganizationIdChanged, id => receivedId = id));

    // trigger selection via MudDataGrid row click or InvokeAsync
    // assert receivedId == expected
}
```

---

### S3 — Low: Reset button has no test coverage

**Location:** `OrganizationDetailsFormTests.cs`

**Issue:** The Reset button (`ResetAsync`) reloads from the database and discards unsaved edits. No test verifies this behavior.

---

### S4 — Low: Form validation (empty Key/Name) has no test coverage

**Location:** `OrganizationDetailsFormTests.cs`

**Issue:** `MudTextField` has `Required="true"` and `RequiredError="..."`, and `SaveAsync` calls `_form.Validate()` and exits early if invalid. No test confirms that submitting with a blank key or name is rejected.

---

### S5 — Info: Duplicate-key check is case-sensitive in application code

**Location:** `OrganizationDetailsForm.razor:SaveAsync` line:
```csharp
.AnyAsync(organization => organization.Key == key);
```

**Issue:** Default `==` in EF against SQL Server uses the database collation (typically `CI`), but the InMemoryDatabase provider is case-sensitive. The production behavior and test behavior silently differ. Not a bug today (all tests use identical casing), but a test fragility risk.

**Fix (optional):** Normalize key casing at creation time (e.g., `key = key.ToLowerInvariant()`) or use `EF.Functions.Collate` / `StringComparison` to make intent explicit.

---

### S6 — Info: `OrganizationScopeFilter` is not registered in DI

**Location:** `Services/OrganizationScopeFilter.cs`

**Issue:** The class is instantiated directly in tests (`new OrganizationScopeFilter()`). No DI registration exists. This is correct for a deferred seam, but Set C will need to register it (likely as singleton) when the real EF filter wires up.

**Non-issue:** Acknowledged as intentional deferral.

---

## Deferred-Seam Assessment

The `OrganizationScopeFilter` seam is **honest and coherent**:

| Check | Status |
|---|---|
| `AcmeCoffeeDbContext` unchanged | ✅ No global query filter added |
| `Product`/`Customer`/`Order` lack `OrganizationId` | ✅ Confirmed in source |
| `DeferredReason` constant names Set C explicitly | ✅ |
| `CanBypassOrganizationScope` implements real logic Set C will reuse | ✅ Tested against all 5 baked-in roles |
| No stub/placeholder filter that would silently return wrong results | ✅ |

---

## Test Adequacy Summary

| File | Tests | Gaps |
|---|---|---|
| `OrganizationMasterGridTests` | 3 | Selection callback, text-filter paths, `RefreshAsync` with selected ID |
| `OrganizationDetailsFormTests` | 4 | Reset, validation rejection, parameter-switch reload |
| `OrganizationScopeFilterTests` | 3 | Adequate for seam scope |
| `DabblerSecurityDashboardTests` | 9 | Updated correctly for 3 CrossOrgUserAdmin tabs |

The round-2 additions (edit + duplicate-key tests) close the most important gaps. The remaining untested paths (S2–S4) are lower-risk UI interactions.

---

## Verdict

**No blocking bugs found.** The S1 unique-constraint gap is the only finding that could cause data corruption in production under concurrent use; everything else is test coverage hardening. The Set C deferral is well-documented and does not leave the codebase in a dishonest state.

---

# Verification Round 3

## Session 6 Verification — Organization Master Details Tab

---

### 1. Bugs / Regressions

#### 1a. Case-sensitive duplicate-key check diverges from DB constraint

**Severity: Medium**
**File:** `OrganizationDetailsForm.razor`, `SaveAsync()` lines within the create branch

```csharp
var duplicateExists = await DbContext.AppOrganizations
    .AsNoTracking()
    .AnyAsync(organization => organization.Key == key);
```

In production (SQL Server, default collation = case-insensitive), this LINQ translates to a case-insensitive comparison, and the unique index on `Key` is also case-insensitive. In tests (InMemoryDatabase), both the app-level check and the "index" are case-sensitive C# string equality — and InMemoryDatabase does not enforce unique indexes at all. A test inserting `"existing-key"` then attempting `"Existing-Key"` would silently succeed, masking a real duplicate.

**Fix:**
```csharp
var normalizedKey = key.ToUpperInvariant();
var duplicateExists = await DbContext.AppOrganizations
    .AsNoTracking()
    .AnyAsync(organization => organization.Key.ToUpper() == normalizedKey);
```

Alternatively, normalize `Key` to a canonical case on create so the invariant is explicit.

#### 1b. `ApplyFilter` ignores the operator from `FilterDescriptor`

**Severity: Low**
**File:** `OrganizationMasterGrid.razor`, `ApplyFilter()` method

The filter method hard-codes `Contains` for text properties and `Equals` for booleans, never reading `entry.Operator`. If the `FilterPanel` ever lets users switch to `StartsWith`, `Equals`, etc., the grid silently applies the wrong predicate.

**Fix:** Either check `entry.Operator` and branch, or document that only the default operator is supported per column. Given `ShowAddGroup="false"`, this is low risk but brittle.

#### 1c. Whitespace-only Key/Name can slip past validation

**Severity: Low**
**File:** `OrganizationDetailsForm.razor`, `SaveAsync()`

`_model.Key.Trim()` runs *after* `_form.Validate()`. MudBlazor's `Required` validation checks the pre-trim bound value. Depending on MudBlazor version, `"   "` may pass `Required` but trim to `""`, producing an empty key that the DB column constraint would reject with an unhandled exception.

**Fix:** Trim the value before validation, or add a custom validator that rejects whitespace-only input:
```csharp
Validation="@(new Func<string, string?>(v => 
    string.IsNullOrWhiteSpace(v) ? "Key is required." : null))"
```

---

### 2. Test Adequacy

| Area | Covered | Gaps |
|------|---------|------|
| Grid renders rows + counts | ✅ | — |
| Grid filter by IsActive | ✅ | No filter test for Key, Name, or IsInternal |
| Grid New button → CreateRequested | ✅ | — |
| Grid row selection → SelectedOrganizationIdChanged | ❌ | No test for selection callback |
| Form create → persists | ✅ | — |
| Form edit → persists | ✅ | — |
| Form archive → IsActive=false | ✅ | — |
| Form duplicate key → blocked | ✅ | No test for case-variant duplicate |
| Form reset button | ❌ | No test |
| Form key field disabled on edit | ❌ | No test |
| Form Persisted callback fires with correct ID | ❌ | Not directly asserted |
| Form loading missing org ID → falls back to create | ❌ | No test |
| OrganizationScopeFilter per-role bypass | ✅ | — |
| OrganizationScopeFilter mixed roles | ✅ | — |
| OrganizationScopeFilter null args throw | ❌ | `ArgumentNullException.ThrowIfNull` present but untested |
| Dashboard tab visibility for CrossOrgUserAdmin with Org tab | ✅ | — |

**Recommendation:** Add at minimum:
- Grid selection callback test (confirms wiring between grid and form).
- Form reset test.
- `OrganizationScopeFilter` null-argument test (two lines, documents the contract).

---

### 3. Deferred OrganizationScopeFilter Seam Assessment

**Verdict: Honest and coherent.**

| Checkpoint | Status |
|------------|--------|
| `DeferredReason` constant names Set C explicitly | ✅ |
| XML doc explains *what* is deferred and *why* | ✅ |
| Only exports the cross-org bypass predicate Set C needs | ✅ |
| `AcmeCoffeeDbContext.OnModelCreating` is unchanged | ✅ |
| Product/Customer/Order have no `OrganizationId` | ✅ Confirmed in provided files |
| No fake `HasQueryFilter()` pretending to work | ✅ |
| Filter class is not registered in DI (appropriate — nothing consumes it yet) | ✅ Noted for Set C |

---

### 4. Missing Work Within Session 6 Scope

| Spec item | Status | Comment |
|-----------|--------|---------|
| `OrganizationMasterDetailsTab` with tab, grid, form | ✅ Complete | |
| `FilterPanel` over {Key, Name, IsInternal, IsActive} | ✅ Complete | |
| `MudDataGrid<AppOrganization>` via `DabblerSecurityDbContext` | ✅ Complete | |
| Details form: create / edit / archive | ✅ Complete | |
| `OrganizationScopeFilter.cs` | ✅ Complete (seam) | |
| Unique index on `AppOrganization.Key` | ✅ `ModelBuilderExtensions.ConfigureAppOrganization` | |
| Global EF query filter on `AcmeCoffeeDbContext` | ⏳ Correctly deferred | Set C dependency |
| Org-scope integration tests | ⏳ Correctly deferred | Tests for the real filter require OrganizationId on entities |

**No missing work** that belongs to Session 6 given the disclosed Set C boundary.

---

### 5. Severity-Ranked Findings

| # | Severity | Finding | File | Action |
|---|----------|---------|------|--------|
| 1 | **Medium** | Duplicate-key check is case-sensitive in tests, diverges from production SQL Server collation; InMemoryDatabase doesn't enforce the unique index | `OrganizationDetailsForm.razor` → `SaveAsync` | Normalize to `ToUpper()`/`ToUpperInvariant()` in the LINQ predicate |
| 2 | **Low** | `ApplyFilter` ignores `entry.Operator`, always hard-codes Contains/Equals per property | `OrganizationMasterGrid.razor` → `ApplyFilter` | Read and branch on operator, or formally restrict operators per column |
| 3 | **Low** | Whitespace-only Key/Name passes `Required` validation, trims to empty, hits unhandled DB exception | `OrganizationDetailsForm.razor` → `SaveAsync` | Trim before validate, or add custom whitespace validator |
| 4 | **Low** | No test for grid row selection → `SelectedOrganizationIdChanged` callback | `OrganizationMasterGridTests.cs` | Add selection callback test |
| 5 | **Low** | No test for form Reset button restoring original values | `OrganizationDetailsFormTests.cs` | Add reset test |
| 6 | **Info** | `OrganizationScopeFilter` null-argument guard (`ThrowIfNull`) is untested | `OrganizationScopeFilterTests.cs` | Add two `[Fact]` tests |
| 7 | **Info** | `OrganizationScopeFilter` not registered in DI — correct for now, must be tracked for Set C | `OrganizationScopeFilter.cs` | Backlog item for Set C |
