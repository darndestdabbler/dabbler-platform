# Verification Round 1

**ISSUES_FOUND**

---

### 1. **Major — Keyboard wrapper `div` with `tabindex="-1"` will not receive keydown events from natural tab-strip interaction**

**Location:** `DabblerSecurityDashboard.razor`, the wrapper `<div>` with `tabindex="-1"` and `@onkeydown`.

**Issue:** `tabindex="-1"` means the element is focusable programmatically but is **not** in the tab order. A user tabbing into the tab strip will focus MudTabs' internal elements (the `<button>` elements inside MudTabs), not this wrapper div. Because the `@onkeydown` handler is on the wrapper div, it relies on keydown events **bubbling** from MudTabs' internal tab buttons up through the DOM. In Blazor Server/WASM, `@onkeydown` uses Blazor's event delegation which does capture bubbled events — so the handler will fire. However, MudTabs' own internal tab buttons may independently handle ArrowLeft/ArrowRight or other keys, leading to **double navigation** (MudTabs moves its own focus indicator AND this handler changes `_activePanelIndex`). The interaction between the two is untested and likely to produce erratic behavior (e.g., MudTabs moves focus ring to tab N+1, but the panel jumps to N+2 because both handlers fire).

**Fix:** Either (a) intercept events only on MudTabs' tab header elements via JS interop, (b) suppress MudTabs' own keyboard handling, or (c) verify empirically that MudTabs does **not** handle arrow keys on its own tab buttons (if confirmed, document this assumption). Currently MudTabs v7+ does NOT have built-in arrow-key support, so this may be fine — but the assumption needs validation against the actual MudBlazor version in use.

---

### 2. **Major — `_preventDefaultTabKey` is set but never reset for subsequent non-matching keystrokes**

**Location:** `DabblerSecurityDashboard.razor`, `HandleTabKeyDown` method.

**Issue:** `_preventDefaultTabKey` is set to `true` when an arrow/Home/End key is pressed, and to `false` when any other key is pressed. Since `@onkeydown:preventDefault="@_preventDefaultTabKey"` is evaluated **before** the handler runs (Blazor evaluates the directive attribute at render time, not per-event), the `preventDefault` applied to the **current** event is based on the value from the **previous** render. This means:

- First ArrowRight: `_preventDefaultTabKey` is still `false` → browser default is NOT prevented on this event. The handler then sets it to `true` and re-renders.
- Second ArrowRight: Now `_preventDefaultTabKey` is `true` → preventDefault applies.
- A normal "Tab" key after arrows: `_preventDefaultTabKey` is still `true` from the previous arrow press → **Tab key default is incorrectly prevented**, breaking keyboard navigation out of the tab strip.

This is a known Blazor gotcha with `@onkeydown:preventDefault`. The off-by-one nature means the first arrow press is not prevented and subsequent Tab presses are incorrectly prevented.

**Fix:** Use a JavaScript-based approach, or always set `@onkeydown:preventDefault="true"` and manually handle Tab passthrough, or use `@onkeydown:stopPropagation` selectively. The cleanest Blazor-idiomatic fix is an event callback that calls `preventDefault` via JS interop only for the relevant keys.

---

### 3. **Minor — `Save()` method on `RoleFormDialog` is `internal void` but should be `internal async Task` or callers need care**

**Location:** `RoleFormDialog.razor`, `Save()` method.

**Issue:** `Save()` is synchronous (`void`), but it calls `StateHasChanged()` directly. Since it's invoked from a button `OnClick` (which expects `Task` or `void`), the `void` return is technically fine for the Blazor event pipeline. However, if `MudDialog.Close()` ever becomes async or if someone later adds async logic, this will silently swallow exceptions. Low risk currently but worth noting.

---

### 4. **Minor — bUnit keyboard tests use `tabStrip.KeyDown()` on a `div[tabindex=-1]`, testing synthetic dispatch rather than real user interaction**

**Location:** `DabblerSecurityDashboardKeyboardTests.cs`, all integration tests.

**Issue:** bUnit's `KeyDown()` dispatches the event directly to the targeted element, bypassing the real DOM focus model. The tests pass because bUnit doesn't enforce focus rules, but they don't validate the real-world scenario where the user's focus is on a MudTabs tab button and the event must bubble to the wrapper. The tests prove the handler logic works when directly invoked, but not that the UX works.

**Fix:** Add a comment acknowledging this limitation, or add a Playwright E2E test that validates the actual keyboard flow. The `NextPanelIndex` unit tests (Theory) are the real safety net; the integration tests add marginal value.

---

### 5. **Minor — `ExistingRoleNames` not passed in `OpenEditDialogAsync`**

**Location:** `DabblerRoleManager.razor`, `OpenEditDialogAsync`.

**Issue:** When editing a role, `ExistingRoleNames` is not set in the dialog parameters. It defaults to an empty list (`[]` in the parameter declaration). While `IsDuplicateRoleName` returns `false` when `isEdit` is `true` (so no functional bug today), this is fragile — if the dialog is later extended to allow role renaming during edit, the duplicate check would silently pass because the list is empty.

**Fix:** Pass `ExistingRoleNames` in the edit path as well, for defense-in-depth consistency.

---

### 6. **Minor — Race in `RoleFormDialogTests.AddingDuplicateRoleName_ShowsInlineAlert_AndDoesNotCloseDialog`**

**Location:** `RoleFormDialogTests.cs`, the `await host.InvokeAsync(() => saveButton.Click())` line.

**Issue:** `saveButton` is an `IElement` captured before `InvokeAsync`. If `Click()` triggers a re-render that removes and re-adds the button (e.g., MudDialog re-renders), the element reference could become stale. In practice MudBlazor dialogs don't typically do this, but the pattern is fragile. Additionally, mixing `await host.InvokeAsync(...)` with `saveButton.Click()` (which itself dispatches synchronously through bUnit) is unusual — normally you'd just call `saveButton.Click()` directly and then assert.

**Fix:** Call `saveButton.Click()` outside of `InvokeAsync`, or re-query the element after the async operation.

---

### 7. **Nit — `role="presentation"` on the keyboard wrapper div is semantically misleading**

**Location:** `DabblerSecurityDashboard.razor`, `<div ... role="presentation">`.

**Issue:** `role="presentation"` tells assistive technology to ignore the element's semantics. Combined with `tabindex="-1"` and an `@onkeydown` handler, this creates a confusing accessibility signal — the element is interactive (can be focused programmatically and handles keyboard events) but declared as presentational. The WAI-ARIA spec says `role="presentation"` should not be applied to focusable elements.

**Fix:** Remove `role="presentation"` or use `role="toolbar"` with appropriate `aria-label="Tab navigation"`.

---

### 8. **Nit — `IsDuplicateRoleName` accepts `IEnumerable<string>` but guards against null with a runtime check**

**Location:** `RoleFormDialog.razor`, `IsDuplicateRoleName`.

**Issue:** The project has `<Nullable>enable</Nullable>`, but the parameter is typed as non-nullable `IEnumerable<string>` while containing an explicit `existingRoleNames is null` check. Either mark the parameter as `IEnumerable<string>?` or remove the null check to be consistent with nullability annotations.

---

### Summary

| # | Severity | Issue |
|---|----------|-------|
| 1 | Major | Wrapper div may conflict with MudTabs' own key handling; no validation of interaction |
| 2 | Major | `@onkeydown:preventDefault` is off-by-one; will break Tab key navigation after arrow presses |
| 3 | Minor | `Save()` is synchronous void; fragile for future changes |
| 4 | Minor | bUnit tests bypass real focus model; limited integration value |
| 5 | Minor | `ExistingRoleNames` not passed in edit dialog path |
| 6 | Minor | Potential stale element reference in dialog test |
| 7 | Nit | `role="presentation"` on focusable element violates ARIA spec |
| 8 | Nit | Nullable annotation inconsistency in `IsDuplicateRoleName` |

---

# Verification Round 2

# Verification Round 2

## Re-check of fixed issues

### Issue 2 (Major — `preventDefault` off-by-one): **VERIFIED FIXED**
The `@onkeydown:preventDefault="@_preventDefaultTabKey"` directive and the `_preventDefaultTabKey` field have been removed entirely. The wrapper div now has only `@onkeydown="HandleTabKeyDown"`. Arrow keys, Home, and End do not have browser default behavior on a `<div>`, so omitting `preventDefault` is correct. No off-by-one, no Tab-key interference.

### Issue 5 (Minor — `ExistingRoleNames` not passed in edit path): **VERIFIED FIXED**
`OpenEditDialogAsync` now builds `existingRoleNames` and passes it via `{ x => x.ExistingRoleNames, existingRoleNames }`. The list correctly includes **all** role names (not filtered to exclude the current role), which is the right behavior since `IsDuplicateRoleName` short-circuits on `isEdit == true` anyway, and if rename support is added later, the full list is what you'd want.

### Issue 7 (Nit — `role="presentation"` on focusable element): **VERIFIED FIXED**
The wrapper div no longer has `tabindex="-1"` or `role="presentation"`. It is now a plain `<div>` with only `data-testid` and `@onkeydown`, which is semantically neutral and ARIA-compliant.

### Issue 8 (Nit — nullable annotation inconsistency): **VERIFIED FIXED**
`IsDuplicateRoleName` parameter is now `IEnumerable<string>? existingRoleNames`, consistent with the `is null` guard inside the method and the project's `<Nullable>enable</Nullable>` setting.

## Scan for NEW Critical/Major issues introduced by fixes

### Wrapper div no longer focusable — keyboard handler reachability
The removal of `tabindex="-1"` means the wrapper div is not focusable at all. The `@onkeydown` handler relies entirely on event bubbling from MudTabs' internal `<button>` elements. In Blazor, `@onkeydown` uses delegated event handling attached at the document root, so events from child elements **will** trigger the handler regardless of the wrapper's focusability. No functional regression.

### `ExistingRoleNames` parameter type mismatch
`RoleFormDialog.razor` declares `[Parameter] public List<string> ExistingRoleNames { get; set; } = [];` (non-nullable `List<string>`), but `IsDuplicateRoleName` accepts `IEnumerable<string>?`. This is internally consistent — the parameter is never null at the call site (`Save()` passes `ExistingRoleNames` which has a non-null default). The static method's nullable parameter type is defensive for direct/test callers. No issue.

### No other new issues detected
- No new fields or state that could drift.
- No new async-void patterns.
- No new accessibility regressions.
- Test files unchanged; existing tests target `[data-testid="security-dashboard-tabstrip"]` which still matches the wrapper div's `data-testid`.

---

## Verdict

**VERIFIED**

All four addressed issues (2, 5, 7, 8) are correctly fixed. No new Critical or Major issues were introduced by the changes. The four deferred items (1, 3, 4, 6) have sound rationale and do not need re-evaluation.
