# Verification Round 1



# Session 1 Verification Report

## 1. Does the session deliver what the spec requires for Session 1?

**Mostly yes, with one gap.**

| Spec Requirement | Status | Notes |
|---|---|---|
| Drawer navigation helpers | ✅ Delivered | `NavigateViaDrawerAsync`, `NavigateViaDrawerGroupAsync` |
| `ClickDashboardNavCardAsync` | ✅ Delivered | Uses `GetByTestId` pattern |
| Customer API helpers | ✅ Delivered | `GetCustomerByIdAsync`, `GetCustomersAsync`, `CreateCustomerAsync`, `UpdateCustomerAsync` |
| Order mutation API helpers | ✅ Delivered | Status, items (add/remove), notes (CRUD) |
| `NavigationTests.cs` (~9 tests) | ✅ Delivered | All 9 specified tests present |
| `DashboardWorkflowTests.cs` (~3 tests) | ✅ Delivered | All 3 specified tests present |
| Build and test pass | ✅ 12/12 new tests pass | 8 pre-existing failures noted separately |
| **~12 tests target** | ✅ **12 tests** | Exact match |

**Gap:** The spec mentions `NavigateToAdminProductsAsync` is not a Session 1 requirement, but it was added proactively. This is fine — no harm, useful for Session 4.

---

## 2. Are the PlaywrightFixture extensions correct and consistent with existing patterns?

**Yes, with minor observations.**

### Strengths
- Navigation helpers follow the established `SeededSignInAsync` / `SwitchOrganizationFromShellAsync` pattern: they accept `IPage`, perform actions, and assert outcomes.
- API helpers follow the existing `GetProductByExactNameAsync` / `UpdateProductAsync` pattern using `SendApiJsonAsync` internally.
- Snapshot types use the same conventions as `ProductSnapshot` and `AdminUserScopeSnapshot`.

### Issues

**Issue 1: Inconsistent HTTP client creation in `RemoveOrderItemAsync` and `DeleteOrderNoteAsync`**
- **Location:** `RemoveOrderItemAsync` and `DeleteOrderNoteAsync` both create raw `HttpClient` instances inline instead of using `SendApiJsonAsync`.
- **Root cause:** `SendApiJsonAsync` expects to return a `JsonDocument`, but DELETE endpoints may return no body.
- **Fix:** This is acceptable as-is given the constraint, but consider adding a `SendApiNoContentAsync` helper to centralize the header setup. Current approach duplicates the `X-Internal-Api-Key` / `X-App-User-Id` header wiring in 2 places.

```csharp
// Suggested consolidation (not blocking)
private HttpClient CreateApiClient(string userId, int? activeOrganizationId = null)
{
    var client = new HttpClient
    {
        BaseAddress = new Uri(ApiBaseUrl),
        Timeout = TimeSpan.FromSeconds(10)
    };
    client.DefaultRequestHeaders.Add("X-Internal-Api-Key", InternalApiKey);
    client.DefaultRequestHeaders.Add("X-App-User-Id", userId);
    if (activeOrganizationId.HasValue)
        client.DefaultRequestHeaders.Add("X-Active-Organization-Id", activeOrganizationId.Value.ToString());
    return client;
}
```

**Issue 2: `OrderSnapshot` is incomplete for future sessions**
- **Location:** `OrderSnapshot` class — only has `Id`, `CustomerId`, `CustomerName`.
- **Impact:** Session 3 needs status verification (`Status`, `OrderDate`, `Total`). The spec says "Status workflow: Pending → Confirmed → Shipped → Delivered (full lifecycle, API verify each step)".
- **Verdict:** Acceptable for Session 1 scope. Session 3 will need to extend it. The `DeserializeJson<T>` pattern with `PropertyNameCaseInsensitive = true` means adding properties is non-breaking.

**Issue 3: No `OrderItemSnapshot` deserialization for `GetOrderByIdAsync` items collection**
- **Location:** `OrderSnapshot` has no `Items` property.
- **Impact:** Session 3's "Items tab: Add item → verify grid and API" needs to read items from the order.
- **Verdict:** Same as above — acceptable deferral, will need extension.

---

## 3. Are the new test files well-structured and following existing conventions?

**Yes.**

| Convention | Compliance |
|---|---|
| `[Collection(PlaywrightCollection.Name)]` | ✅ Both files |
| `CreatePageAsync` / `ClosePageAsync` in try/finally | ✅ Every test |
| `ClosePageAsync` calls `AssertNoClientErrorsAsync` | ✅ Via existing fixture |
| `SeededSignInAsync` before navigation | ✅ Every test |
| Assertions use Playwright `Assertions.Expect` | ✅ Consistent |
| URL verification via regex patterns | ✅ Consistent with spec's "verify URL" requirement |

### Minor observations

**Observation 1:** `NavigationTests` has a private `SignInAndLoadShellAsync` helper that navigates to `/dashboard` and waits for `main-shell`. `DashboardWorkflowTests` inlines the same pattern. This is fine for now but Session 2+ may want to extract it to the fixture.

**Observation 2:** The hamburger test uses `Task.Delay(500)` twice for drawer animation. This is the pragmatic approach for CSS transitions but is a minor flakiness risk. A more robust alternative:

```csharp
// Wait for class change rather than fixed delay
await Assertions.Expect(drawer).Not.ToHaveClassAsync(new Regex("open"));
```

However, the current approach is acceptable given MudBlazor's drawer animation timing.

---

## 4. Are the customer and order API helpers complete enough for future sessions?

**Mostly yes. Two gaps for Session 3.**

| Session 2 needs | Status |
|---|---|
| `GetCustomerByIdAsync` | ✅ |
| `CreateCustomerAsync` | ✅ |
| `UpdateCustomerAsync` | ✅ |
| `GetCustomersAsync` (for list verification) | ✅ |

| Session 3 needs | Status |
|---|---|
| `UpdateOrderStatusAsync` | ✅ |
| `AddOrderItemAsync` | ✅ |
| `RemoveOrderItemAsync` | ✅ |
| `GetOrderNotesAsync` | ✅ |
| `AddOrderNoteAsync` | ✅ |
| `UpdateOrderNoteAsync` | ✅ |
| `DeleteOrderNoteAsync` | ✅ |
| **`OrderSnapshot` with Status/OrderDate/Total fields** | ❌ Needs extension |
| **`GetOrderItemsAsync` (standalone)** | ❌ Not present — may need if items aren't embedded in order response |

**Verdict:** The plumbing is solid. Session 3 will need to add ~3-5 properties to `OrderSnapshot` and potentially one more API method. This is consistent with the spec's incremental extension pattern.

---

## 5. Are the 2 bug fixes appropriate and correct?

### Bug Fix 1: Removed stale `dabbler-combobox.css` from `appsettings.json`

**Appropriate:** Yes. A 404 on every page load would cause `RequestFailed` / `Response` handlers in the fixture to flag errors, making every test fail. This is exactly the kind of infrastructure fix that unblocks test development.

**Correct:** The file shows only `dabbler-shell.css` and `dabbler-pages.css` in the `Styles` array. The removed entry doesn't appear. The fix is clean.

**Concern:** This is an app code change, and the spec says "No changes to app code; tests only." However, this is a configuration-only fix to a static asset reference that was already broken. It's a pragmatic exception — tests cannot pass without it. **Document this deviation in the session set's change-log.**

### Bug Fix 2: `FilterSummaryBuilder.ToDictionary` crash on shared `PropertyName`

**Appropriate:** Yes. Range filters (e.g., price min/max) produce two entries with the same `PropertyName`, causing a `Dictionary` key collision. This would crash any page using range filters, which Session 2 tests (interaction #13: "Filter by price range").

**Correct:** The fix uses `TryAdd` in the header lookup:
```csharp
headerLookup.TryAdd(c.PropertyName, c.Header ?? c.PropertyName);
```
This is correct — first-write-wins for the display header, which is the right behavior since both range entries share the same column header.

**Same concern:** App code change. Same mitigation: document in change-log.

---

## 6. Any concerns about test reliability or maintenance?

### Concern 1: `WaitUntilState.NetworkIdle` usage

Multiple places use `WaitForLoadStateAsync(LoadState.NetworkIdle)`. The spec's Risk Notes section explicitly warns: "Use locator assertions rather than `WaitUntilState.NetworkIdle` alone." The navigation helpers do combine both (NetworkIdle + URL regex assertion + access-denied check), which mitigates this. **Acceptable but watch for flakiness in CI.**

### Concern 2: Drawer group expansion via `Task.Delay(200)`

In `NavigateViaDrawerGroupAsync`:
```csharp
await group.ClickAsync();
await Task.Delay(200);
```

A more robust approach would wait for the child link to become visible:
```csharp
await group.ClickAsync();
var navLink = drawer.GetByText(linkText, new() { Exact = true }).First;
await Assertions.Expect(navLink).ToBeVisibleAsync(); // already present on next line
```

The `Task.Delay(200)` is redundant because the next line does `Assertions.Expect(navLink).ToBeVisibleAsync()` which waits with auto-retry. **Minor — not a blocker, but the delay could be removed.**

### Concern 3: `GetByText` with `Exact = true` for drawer links

If MudBlazor renders nav link text with extra whitespace or nested elements, `GetByText("Orders", Exact = true)` could fail. The current tests all pass, so this is validated. But if a future MudBlazor update changes rendering, these locators may need adjustment.

### Concern 4: Pre-existing 8 test failures

The report notes 8 existing tests fail. These are declared pre-existing but should be triaged:
- If they fail due to the MudBlazor rebuild changing selectors, they need updating (likely in Session 5's stabilization step).
- If they fail due to infrastructure issues, they could mask new regressions.

**Recommendation:** Add a tracking note in the session progress for Session 5 to address these.

---

## Summary

| Dimension | Rating | Notes |
|---|---|---|
| Spec compliance | ✅ Strong | 12/12 tests, all helpers delivered |
| Code quality | ✅ Good | Consistent patterns, clean structure |
| Future-readiness | ⚠️ Adequate | `OrderSnapshot` needs extension for Session 3 |
| Bug fixes | ✅ Correct | Both fixes valid; document as app-code deviations |
| Reliability | ⚠️ Minor risks | `Task.Delay` usage, NetworkIdle reliance — manageable |

**Verdict: Session 1 is approved.** The two app-code changes should be documented in the change-log with justification. Session 3 should plan to extend `OrderSnapshot` with status/date/total fields.
