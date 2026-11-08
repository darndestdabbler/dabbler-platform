# Verification Round 1

## Session 4 Verification Report

### Checklist Results

| # | Item | Status | Notes |
|---|------|--------|-------|
| 1 | Default Masks tab exists, inline-editable, persists via `SaveAsync` | ✅ Pass | `DefaultMasksGrid` edits MaskFormat inline via debounced `MudTextField` in a `TemplateColumn`; Add/Remove/Edit all route through `PersistAsync → ConfigManager.SaveAsync` |
| 2 | `FilterPanel<T>` present on grid | ✅ Pass | Three filter columns defined (EntityKey, ColumnKey, MaskFormat) with `Contains` operator; `ApplyFilter` processes the `CompositeFilterDescriptor` |
| 3 | Safety-net regex matches spec tokens exactly | ✅ Pass | `"ssn|taxid|ein|dob|password|ccnum|salary"` with `IgnoreCase \| Compiled` matches spec's `/ssn|taxid|ein|dob|password|ccnum|salary/i`; declared columns excluded via `HashSet` with case-insensitive `PairComparer` |
| 4 | "Add to Default Masks" creates blank-format rule | ✅ Pass | `AddFromSafetyNetAsync` sets `_newMaskFormat = string.Empty` then calls `AddAsync`; test asserts `rule.MaskFormat.Should().Be(string.Empty)` |
| 5 | SystemAdmin-only gating | ✅ Pass | Tab descriptor: `AllowedBakedInRoles: [BakedInRoles.SystemAdmin]`; dashboard tests confirm exclusion for UserAdmin, CrossOrgUserAdmin, and User |
| 6 | MaskExemptions sub-tab reacts in-circuit to Default Masks changes | ✅ Pass | `MaskExemptionsSubTab` subscribes to `AuthorizationStateNotifier`; `OnConfigChangedAsync` calls `RebuildTableKeys()` + `InvokeAsync(StateHasChanged)`; `ExternalConfigChange_RescansSafetyNet_InCircuit` test validates the flow |
| 7 | Dashboard tests survive new real-component tree | ✅ Pass | `RegisterDashboardServices` adds `MetadataRegistry.Empty`; all 572 non-Playwright tests pass |
| 8 | No build/test regression | ✅ Pass | 0 build errors; 141/141 Security.Blazor tests pass (+14 new); single pre-existing unrelated failure |
| 9 | No unresolved Critical/Major issues | ⚠️ See below | One spec-deviation and one test gap identified |

---

### Issues

#### Issue 1 — Safety-net panel rendered above the grid (spec deviation)

- **Severity**: Minor
- **Location**: `DefaultMasksTab.razor`, lines 12–16
- **Spec**: "a collapsible `MudCollapse` panel **below the grid**"
- **Actual**: `<SensitiveNameSafetyNet>` is rendered before `<DefaultMasksGrid>`

```razor
<!-- Current order (safety net above grid) -->
<SensitiveNameSafetyNet @ref="_safetyNet" ... />
<DefaultMasksGrid @ref="_grid" ... />
```

- **Fix**: Swap the two child components:

```razor
<DefaultMasksGrid @ref="_grid"
                  OnPersisted="OnGridPersistedAsync" />

<SensitiveNameSafetyNet @ref="_safetyNet"
                        OnAddMatch="OnAddFromSafetyNetAsync" />
```

---

#### Issue 2 — No bUnit test for filter interaction on the grid

- **Severity**: Minor
- **Location**: `DefaultMasksGridTests.cs`
- **Spec**: "Write bUnit tests for the safety-net flow **and filter interaction**."
- **Actual**: 14 tests cover safety-net flow, add/remove, idempotency, cross-tab notification, and rendering. No test exercises the `FilterPanel → OnFilterChanged → FilteredRows` path (e.g., verifying that typing "Customer" in the Table filter narrows visible rows, or that the count label updates from "2 of 2" to "1 of 2").
- **Fix**: Add a test such as:

```csharp
[Fact]
public void Filter_ByTableKey_NarrowsVisibleRows()
{
    var config = new SecurityConfiguration
    {
        DefaultMasks =
        [
            new DefaultMaskRule { EntityKey = "Customer", ColumnKey = "SocialSecurityNumber", MaskFormat = "" },
            new DefaultMaskRule { EntityKey = "Employee", ColumnKey = "Salary", MaskFormat = "" },
        ],
    };
    var (ctx, _) = Build(config);
    using var _1 = ctx;

    var cut = ctx.RenderComponent<DefaultMasksGrid>();

    // Simulate filter application
    cut.InvokeAsync(() =>
    {
        var filter = new CompositeFilterDescriptor
        {
            Filters = { new FilterDescriptor
            {
                PropertyName = nameof(DefaultMaskRule.EntityKey),
                Value = "Employee",
            }},
        };
        // Access the private OnFilterChanged or expose a test seam
    });

    cut.Find("[data-testid='default-mask-count']")
       .TextContent.Should().Contain("1 of 2");
}
```

This may require either exposing `OnFilterChanged` as `internal` or driving the `FilterPanel` via markup interaction, depending on how `FilterPanel<T>` renders.

---

### Observations (informational, no action required)

1. **`StateHasChanged()` in `Rebuild()`** — Called unconditionally, including from `OnInitialized` / `OnParametersSet` where Blazor already queues a render. Harmless but redundant in those paths.

2. **`ein` false-positive surface** — The pattern `ein` will match column names containing that substring (e.g., `Reinstatement`, `BeingProcessed`). The spec explicitly lists `ein`, and the safety net is advisory (one-click add, not blocking), so this is acceptable by design.

3. **Regex fallback to `DisplayName`** — `SensitiveNameSafetyNet` checks `col.Key` first, then `col.DisplayName`. This is what makes `SocialSecurityNumber` (key) match via `"SSN"` (display name) against the `ssn` token. Correct and intentional, but worth noting for anyone expecting key-only matching.

4. **Table/Column not editable in existing grid rows** — The spec's "inline-editable `MudDataGrid`" is satisfied by MaskFormat being editable in-place. Changing Table/Column on an existing row would semantically be a different rule, so display-only rendering for those columns is a sound design choice.

---

### Verdict

**Pass with two minor findings.** All functional requirements are met. The layout ordering (Issue 1) is a one-line swap. The missing filter test (Issue 2) is a coverage gap against the spec's explicit call-out but does not affect runtime behavior.

---

# Verification Round 2

# Verification Round 2

## Session 4 Verification Report

### Round-1 Finding Resolution

| Finding | Status | Evidence |
|---------|--------|----------|
| Issue 1 — Safety-net panel rendered above the grid | ✅ Resolved | `DefaultMasksTab.razor` now renders `<DefaultMasksGrid>` first, `<SensitiveNameSafetyNet>` second — panel is below the grid per spec |
| Issue 2 — No bUnit test for filter interaction | ✅ Resolved | `Filter_ByTableKey_NarrowsVisibleRows` added; exercises `OnFilterChanged` with a `CompositeFilterDescriptor`, asserts count label transition `3 of 3` → `2 of 3` → `3 of 3`, verifies correct rows visible/hidden, and validates filter-clear restoration; `OnFilterChanged` promoted to `internal` with existing `InternalsVisibleTo` coverage |

### New-Issue Scan

| Area | Check | Result |
|------|-------|--------|
| `OnFilterChanged` visibility change | `internal` is the minimum visibility needed; `FilterChanged="OnFilterChanged"` Blazor binding still resolves (same component scope) | ✅ No issue |
| `ApplyFilter` operator handling | Method performs `Contains` unconditionally regardless of `entry.Operator` — consistent with all three `FilterColumnDefinition` entries defaulting to `FilterOperators.Contains`; pre-existing, not introduced by these changes | ✅ No issue |
| New test data setup | Seeds 3 rules (Customer×2, Employee×1); `Build` helper registers both entities with matching fields; all `data-testid` selectors align with the grid's `$"default-mask-remove-{EntityKey}-{ColumnKey}"` pattern | ✅ No issue |
| `FilterDescriptor` constructor | 3-arg constructor `(PropertyName, Operator, Value)` used in test matches the `PropertyName`/`Value` properties consumed by `ApplyFilter` | ✅ No issue |
| Build + test counts | 0 build errors; 573 passed (+1); 142/142 Security.Blazor.Tests (+1); single pre-existing unrelated failure unchanged | ✅ No regression |
| Razor component ordering side-effects | `_safetyNet` now initializes after `_grid` in the render tree; `OnGridPersistedAsync` calls `_safetyNet?.Rebuild()` (null-safe), so initialization order is harmless | ✅ No issue |

### Verdict

**Pass.** Both round-1 minor findings are resolved. No new issues introduced.
