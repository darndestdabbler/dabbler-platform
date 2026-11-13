# Verification Round 1

ISSUES_FOUND

### Issue 1: Inconsistent Assertion Logic
â†’ **Location**: `tests/AcmeCoffeeOnline.Playwright/ReportsWorkflowTests.cs` â†’ `SalesByProduct_PdfExport_TriggersDownloadAsync`
â†’ **Fix**: Add an assertion to verify the "Export to PDF" button is disabled before the report is run. This improves consistency with the `OrdersByCustomer_PdfExport_TriggersDownloadAsync` test and more rigorously verifies the UI state.

```csharp
// In SalesByProduct_PdfExport_TriggersDownloadAsync method

await _fixture.NavigateViaDrawerGroupAsync(
    page, "Reports", "Sales by Product", ".*/reports/sales-by-product$");

// Add this block
var pdfButton = page.GetByRole(AriaRole.Button, new() { Name = "Export to PDF" });
await Assertions.Expect(pdfButton).ToBeDisabledAsync();

// Fill dates and run report
await FillReportDatesAsync(page);
// ... rest of the method
```

### Issue 2: Use of Arbitrary Delays (`Task.Delay`)
â†’ **Location**: Multiple tests across `MultiStepWorkflowTests.cs` and `ProductAdministrationWorkflowTests.cs`.
â†’ **Fix**: Replace `Task.Delay()` with explicit, condition-based waits (e.g., `WaitForLoadStateAsync`, `Assertions.Expect().ToBeEnabledAsync()`). Arbitrary delays create flaky tests that can fail unpredictably if the system is slow. The existing explicit waits are often sufficient, making the delays redundant.

**Example 1: `MultiStepWorkflowTests.cs` â†’ `OrderTabNavigation_DrawerToDetailItemsNotesBackAsync`**
```csharp
// From
await notesTabHeader.ClickAsync();
await Task.Delay(500);
var notesGrid = page.Locator(".mud-tabpanel:visible .mud-table, .mud-tabs-panels .mud-table").Last;

// To (replace delay with an explicit wait)
await notesTabHeader.ClickAsync();
var notesGrid = page.Locator(".mud-tabpanel:visible .mud-table, .mud-tabs-panels .mud-table").Last;
await Assertions.Expect(notesGrid).ToBeVisibleAsync();
```

**Example 2: `ProductAdministrationWorkflowTests.cs` â†’ `E2E_WF_AdminProducts_EditProductViaDialog_PersistsViaApiAsync`**
```csharp
// From
await descriptionField.FillAsync(editedDescription);
await Task.Delay(300);
var saveButton = dialog.GetByRole(AriaRole.Button, new() { Name = "Save" });
await Assertions.Expect(saveButton).ToBeEnabledAsync(new() { Timeout = 5000 });

// To (remove the delay; Expect() will auto-retry)
await descriptionField.FillAsync(editedDescription);
var saveButton = dialog.GetByRole(AriaRole.Button, new() { Name = "Save" });
await Assertions.Expect(saveButton).ToBeEnabledAsync(new() { Timeout = 5000 });
```

### Issue 3: Fragile CSS Locator
â†’ **Location**: `tests/AcmeCoffeeOnline.Playwright/MultiStepWorkflowTests.cs` â†’ `OrderTabNavigation_DrawerToDetailItemsNotesBackAsync`
â†’ **Fix**: The locator for the notes grid is complex and depends on DOM structure (`.mud-tabpanel:visible .mud-table, .mud-tabs-panels .mud-table`). This is brittle. The corresponding component in the application should be updated to include a `data-testid` for robust selection.

```csharp
// In the application's Razor component for the notes grid
// Add a test id, for example:
<MudTable data-testid="notes-grid" ...>
    ...
</MudTable>

// In OrderTabNavigation_DrawerToDetailItemsNotesBackAsync test method
// From
var notesGrid = page.Locator(".mud-tabpanel:visible .mud-table, .mud-tabs-panels .mud-table").Last;
var notesHeaderRow = notesGrid.Locator(".mud-table-head");

// To
var notesGrid = page.GetByTestId("notes-grid");
var notesHeaderRow = notesGrid.Locator(".mud-table-head");
```
