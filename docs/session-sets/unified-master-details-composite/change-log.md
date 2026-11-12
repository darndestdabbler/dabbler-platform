# Change Log — unified-master-details-composite

## What changed

`MasterDetailExpansionShell` is now a fully generic three-type-parameter composite
(`<TRow, TEntity, TLink>`) that drives four distinct data-entry workflows from one
component. Before this set, the shell was a single-purpose Search+Update component
tied to the Role admin tab.

## New capabilities

**Scenario 1 — Add Only.** `ShowGrid=false` drops the expansion panel entirely and
renders the form full-width with a session counter. The Customer Batch Entry page at
`/customers/batch-entry` demonstrates this workflow end-to-end.

**Scenario 2 — Search + Update.** The original shell behavior, now served by the
generic implementation. `RoleMasterDetailsTab` and any other admin tabs that were
using the old surface were migrated to the new slot pattern without behavior
regression.

**Scenario 3 — Pick-or-Add.** `PickOrAddDialog<TRow, TEntity, TLink>` wraps the
shell in Dialog mode. A row-wise Use button lets the user link an existing record
without editing; filling a field flips the primary button from "Use" to "Save and
Use". The Order → Customer picker implements this workflow and includes an
inspect-to-decide variant (`ReadOnlyWithToggle`) that lets users drill into a record
read-only before deciding whether to edit before linking.

**Scenario 4 — M:N with join-table payload.** `ShowLinkData=true` adds a visually
distinct Link Data card above the target card. `TargetEditability` controls three
modes: `Editable` (4a — both cards editable), `ReadOnly` (4b-locked — target
disabled, link card editable), and `ReadOnlyWithToggle` (4b-toggle — target starts
locked, an "Edit record" button unlocks it on demand). Order ↔ Product with
Quantity/Discount payload serves as the reference implementation; the
`/admin/order-item-tuner` demo page covers the 4a code path.

## Bug fixes (Session 5)

Five production bugs were found and fixed during the final Playwright run:

- `CustomersController` and `OrdersController` both used `CreatedAtActionResult` with
  a route name that didn't resolve, causing `InvalidOperationException → 409` on
  every customer or order creation. Fixed to `Ok(created)`.
- `OrderDetailState` was registered as `AddTransient`, giving `OrderCreatePage` and
  `OrderDetail` different instances. After saving a new order, `OrderCreatePage` saw
  `Id = 0` and never navigated. Fixed to `AddScoped`.
- `CustomerPickerDialog` dirty-state tracking required `Immediate="true"` on
  MudTextField fields and a local `_isTargetReadOnly` field managed via
  `OnParametersSet` and `OnTargetEditingToggled`, replacing the fragile
  `_pickOrAdd?.DetailsContext?.IsTargetReadOnly ?? false` pattern.
- `ProductLinePickerDialog.HandleUsePickedAsync` set `_target` but never triggered a
  re-render of the dialog component. Added `StateHasChanged()` after row selection.
- Test assertions: replaced `.Locator("input")` with direct `ToHaveValueAsync` on the
  testid (MudBlazor 8.x places `data-testid` directly on the `<input>` element);
  replaced `ToBeEditableAsync` with toggle-text proxy assertions (MudBlazor 8.x
  `ReadOnly` does not set the HTML `readonly` attribute that Playwright checks).

## Known issues / follow-up

- **Keyboard navigation (Enter-moves-next-field for Scenario 1).** Explicitly
  deferred to a follow-up set.
- **Full migration audit.** Only `RoleMasterDetailsTab` and the tabs needed to keep
  the build green were migrated. Remaining admin tabs migrate in a follow-on set.
- **Catalog re-crawl needed.** The new routes (`/customers/batch-entry`,
  `/admin/order-item-tuner`, `/orders/new`) are not yet in the component catalog.
  The W2 runner ran with CATALOG_STALE suppressions; a re-crawl will enable
  catalog-grounded UAT authoring for future sets that touch these pages.
