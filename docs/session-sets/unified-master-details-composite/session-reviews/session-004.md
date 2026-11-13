# Verification Round 1

I see 3 material gaps.

1. **`ProductLinePickerDialog` does not actually select/hydrate a product on row-use click**
   - File: `ProductLinePickerDialog.razor`
   - `HandleUsePickedAsync(ProductDto row)` only does `_pickOrAdd?.NotifyRowSelected(row)` and returns `null`.
   - It **does not call** `HandleRowSelectedAsync(row)`, so `_target` and `_link` are not populated there.
   - That means the add-product flow only works if the row-use button click happens to bubble into `MudDataGrid.RowClick`. That's brittle and component-internal state should not depend on DOM bubbling side effects.
   - Fix: have `HandleUsePickedAsync` explicitly hydrate the buffers before notifying the shell.

2. **The 4b “Review line” dialog is not wired to the clicked line**
   - Files: `OrderDetail.razor`, `ProductLinePickerDialog.razor`
   - `OpenProductLineInspectorAsync(OrderItemDto item)` passes only `OrderId` and `AllowTargetEdit=true`.
   - `ProductLinePickerDialog` has no parameter for the existing `OrderItemDto` (or initial product), so it always opens with:
     - `_target = NewTarget()`
     - `_link = NewLink()` → `Quantity = 1`, `Discount = 0`
   - So “Review line” is really opening a fresh picker, not the selected line. The comment says the existing quantity/discount are surfaced, but the code does not do that.
   - Fix: add initial line/product parameters and populate `_target` / `_link` from them.

3. **`ShowGrid=false` never renders the target card or toggle**
   - File: `MasterDetailExpansionShell.razor`
   - In the `ShowGrid=false` branch, `ShowLinkData` renders the link card, then emits raw `DetailsContent`.
   - Unlike the `ShowGrid=true` path, there is:
     - no target card,
     - no `TargetCardHeading`,
     - no edit-toggle button for `ReadOnlyWithToggle`.
   - That misses the stated “link card above target card” behavior and makes toggle mode unusable in add-only mode.
   - This directly affects the `/admin/order-item-tuner` Scenario 4a layout.

Other than those, the `AnyDirty` / label / dirty-flag plumbing looks consistent.

Also: **I can’t independently confirm the router override to GPT-5.4 from inside this review**; no model-routing metadata is exposed to me in-band.
