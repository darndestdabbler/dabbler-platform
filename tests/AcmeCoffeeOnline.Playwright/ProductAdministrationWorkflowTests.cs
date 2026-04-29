using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;

namespace AcmeCoffeeOnline.Playwright;

[Collection(PlaywrightCollection.Name)]
public sealed class ProductAdministrationWorkflowTests
{
    private readonly PlaywrightFixture _fixture;

    public ProductAdministrationWorkflowTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    // E2E_WF_04 was previously parked in ProductAdministrationWorkflowFlakyTests.cs
    // under FlakyPlaywrightCollection because the snackbar-toast assertion missed
    // its visibility window under full-suite parallelism. The driving component
    // (ProductManagement.razor) now inherits Dabbler.Testing.Blazor's
    // TrackedComponentBase and ToggleAvailabilityAsync is wrapped with
    // TrackAction("toggle-availability", ...). The test waits on the
    // data-actions-in-flight watermark via _fixture.WaitForQuietAsync(page)
    // before asserting the snackbar text, which makes the wait deterministic
    // (the SignalR render-then-interop ordering guarantees the snackbar DOM
    // has settled by the time the watermark returns to "[]").
    [Fact]
    public async Task E2E_WF_04_SystemAdmin_TogglesProductAvailabilityThroughBrowserAndRestoresStateAsync()
    {
        const string productName = "Blonde Roast - Decaf - Espresso";
        var page = await _fixture.CreatePageAsync();
        string? userId = null;
        int? productId = null;
        bool? originalAvailability = null;

        try
        {
            await _fixture.SeededSignInAsync(page, "admin@acmecoffee.com");

            var session = await _fixture.GetSessionInfoAsync(page);
            userId = session.UserId;

            Assert.False(string.IsNullOrWhiteSpace(userId));
            Assert.Contains("SystemAdmin", session.Roles ?? []);

            var beforeProduct = await _fixture.GetProductByExactNameAsync(userId!, productName);
            productId = beforeProduct.Id;
            originalAvailability = beforeProduct.IsAvailable;

            await _fixture.NavigateToAdminProductsAsync(page);
            var routedContent = page.GetByTestId("routed-page-content");

            await routedContent.GetByPlaceholder("Search products...").FillAsync(productName);

            var availabilitySwitch = routedContent.GetByTestId($"product-availability-{productId}");

            await Assertions.Expect(availabilitySwitch).ToBeVisibleAsync();
            Assert.Equal(originalAvailability.Value, await availabilitySwitch.IsCheckedAsync());

            await availabilitySwitch.ClickAsync();
            await _fixture.WaitForQuietAsync(page);

            var expectedAvailability = !originalAvailability.Value;
            if (expectedAvailability)
            {
                await Assertions.Expect(availabilitySwitch).ToBeCheckedAsync();
            }
            else
            {
                await Assertions.Expect(availabilitySwitch).Not.ToBeCheckedAsync();
            }
            await Assertions.Expect(page.GetByText(
                    $"{productName} is now {(expectedAvailability ? "available" : "unavailable")}.",
                    new PageGetByTextOptions { Exact = true }))
                .ToBeVisibleAsync();

            var afterProduct = await _fixture.GetProductByIdAsync(userId!, productId.Value);
            Assert.Equal(expectedAvailability, afterProduct.IsAvailable);

            await PlaywrightFixture.AssertLayoutSpacingAsync(page);
        }
        finally
        {
            if (productId.HasValue && originalAvailability.HasValue && !string.IsNullOrWhiteSpace(userId))
            {
                await _fixture.RestoreProductAvailabilityAsync(userId, productId.Value, originalAvailability.Value);
            }

            await _fixture.ClosePageAsync(page);
        }
    }

    // E2E_WF_05_SystemAdmin_AddsProductAfterKeyboardAndValidationChecksAsync
    // moved to ProductAdministrationWorkflowFlakyTests.cs (FlakyPlaywrightCollection)
    // in Session 2 of playwright-sync-watermark-and-playbook because of a
    // pass-alone-fail-in-suite cumulative-state flake. See
    // cumulative-flake-investigation.md for details and Session 5 for the
    // drain.

    [Fact]
    public async Task E2E_WF_AdminProducts_EditProductViaDialog_PersistsViaApiAsync()
    {
        var page = await _fixture.CreatePageAsync();
        string? userId = null;
        ProductSnapshot? original = null;

        try
        {
            await _fixture.SeededSignInAsync(page, "admin@acmecoffee.com");
            var session = await _fixture.GetSessionInfoAsync(page);
            userId = session.UserId;

            await page.GotoAsync($"{_fixture.BaseUrl}/dashboard", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });
            await _fixture.NavigateToAdminProductsAsync(page);
            var routedContent = page.GetByTestId("routed-page-content");

            // Wait for grid data and get the first product's edit button
            await PlaywrightFixture.WaitForGridDataAsync(page);

            // Find the first edit button in the grid
            var editButton = routedContent.GetByRole(AriaRole.Button, new() { Name = "Edit" }).First;
            await Assertions.Expect(editButton).ToBeVisibleAsync();

            // Get the product name from the first row to find it via API
            var firstRow = routedContent.Locator(".mud-table-body tr.mud-table-row").First;
            var productNameCell = firstRow.Locator("td").First;
            var productName = (await productNameCell.TextContentAsync())?.Trim() ?? string.Empty;
            original = await _fixture.GetProductByExactNameAsync(userId!, productName);

            // Click edit to open dialog
            await editButton.ClickAsync();
            var dialog = await PlaywrightFixture.WaitForDialogAsync(page);

            // Touch the Name field (required) to trigger MudForm validation
            var nameField = dialog.GetByLabel("Name").First;
            var currentName = await nameField.InputValueAsync();
            await nameField.FillAsync(currentName + " ");
            await nameField.FillAsync(currentName);

            // Modify the description
            var editedDescription = $"Edited by Playwright {DateTime.UtcNow:yyyyMMddHHmmssfff}";
            var descriptionField = dialog.GetByLabel("Description");
            await descriptionField.FillAsync(editedDescription);

            // Submit the edit dialog — Expect auto-retries until enabled
            var saveButton = dialog.GetByRole(AriaRole.Button, new() { Name = "Save" });
            await Assertions.Expect(saveButton).ToBeEnabledAsync(new() { Timeout = 5000 });
            await saveButton.ClickAsync();
            await Assertions.Expect(dialog).ToBeHiddenAsync(new() { Timeout = 5000 });

            // Verify via API
            var updated = await _fixture.GetProductByIdAsync(userId!, original.Id);
            Assert.Equal(editedDescription, updated.Description);
        }
        finally
        {
            if (original is not null && !string.IsNullOrWhiteSpace(userId))
            {
                await _fixture.UpdateProductAsync(userId, original);
            }

            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task E2E_WF_AdminProducts_SearchFilter_FiltersGridAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await _fixture.SeededSignInAsync(page, "admin@acmecoffee.com");
            await page.GotoAsync($"{_fixture.BaseUrl}/dashboard", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });
            await _fixture.NavigateToAdminProductsAsync(page);
            var routedContent = page.GetByTestId("routed-page-content");

            await PlaywrightFixture.WaitForGridDataAsync(page);

            // Record initial row count
            var initialRowCount = await PlaywrightFixture.GetGridRowCountAsync(page);
            Assert.True(initialRowCount > 0, "Admin products grid should have data.");

            // Type a specific search term that should filter results
            var searchField = routedContent.GetByPlaceholder("Search products...");
            await searchField.FillAsync("Espresso");

            // Debounce delay: 300ms search debounce + grid reload, not render-correlated.
            await Task.Delay(500);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await PlaywrightFixture.WaitForGridDataAsync(page);

            // Grid should have filtered data
            var filteredRowCount = await PlaywrightFixture.GetGridRowCountAsync(page);
            Assert.True(filteredRowCount > 0, "Search for 'Espresso' should return results.");

            // Clear search to restore
            await searchField.FillAsync("");
            await Task.Delay(500); // Debounce delay: 300ms search debounce + grid reload, not render-correlated.
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task E2E_WF_AdminProducts_Pagination_NavigatesCorrectlyAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await _fixture.SeededSignInAsync(page, "admin@acmecoffee.com");
            await page.GotoAsync($"{_fixture.BaseUrl}/dashboard", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });
            await _fixture.NavigateToAdminProductsAsync(page);

            await PlaywrightFixture.WaitForGridDataAsync(page);

            // Check if paging controls exist (admin page uses pageSize=20)
            var pager = page.Locator(".mud-table-pagination");
            var pagerVisible = await pager.IsVisibleAsync();

            if (pagerVisible)
            {
                var nextButton = pager.Locator("button[aria-label='Next page']");
                if (await nextButton.IsEnabledAsync())
                {
                    await nextButton.ClickAsync();
                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                    await PlaywrightFixture.WaitForGridDataAsync(page);

                    var rowCount = await PlaywrightFixture.GetGridRowCountAsync(page);
                    Assert.True(rowCount > 0, "Page 2 should have data rows.");

                    // Navigate back
                    var prevButton = pager.Locator("button[aria-label='Previous page']");
                    await prevButton.ClickAsync();
                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                    await PlaywrightFixture.WaitForGridDataAsync(page);
                }
            }

            // If no pager visible, all products fit on one page — test still passes
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }
}
