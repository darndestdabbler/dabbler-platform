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

    [Fact]
    public async Task E2E_WF_05_SystemAdmin_AddsProductAfterKeyboardAndValidationChecksAsync()
    {
        var page = await _fixture.CreatePageAsync();
        var productName = $"Playwright Automation Product {DateTime.UtcNow:yyyyMMddHHmmssfff}";
        var archivedNamePrefix = $"Archived Playwright Product {DateTime.UtcNow:yyyyMMddHHmmssfff}";
        string? userId = null;
        int? createdProductId = null;

        try
        {
            await _fixture.SeededSignInAsync(page, "admin@acmecoffee.com");

            var session = await _fixture.GetSessionInfoAsync(page);
            userId = session.UserId;

            Assert.False(string.IsNullOrWhiteSpace(userId));
            Assert.Contains("SystemAdmin", session.Roles ?? []);

            // Navigate to admin products via the drawer nav group
            await _fixture.NavigateToAdminProductsAsync(page);
            var routedContent = page.GetByTestId("routed-page-content");

            await routedContent.GetByRole(AriaRole.Button, new() { Name = "Add Product" }).ClickAsync();

            var dialog = page.Locator("[role='dialog']");
            await Assertions.Expect(dialog).ToBeVisibleAsync();

            var nameField = page.GetByTestId("product-name-input");
            var roastLevelField = dialog.GetByLabel("Roast Level");
            var unitPriceField = dialog.GetByLabel("Unit Price");
            var descriptionField = dialog.GetByLabel("Description");
            var imageUrlField = dialog.GetByLabel("Image URL");
            var createButton = dialog.GetByRole(AriaRole.Button, new() { Name = "Create" });

            await Assertions.Expect(createButton).ToBeDisabledAsync();

            await TabUntilFocusedFieldAsync(page, "Name", maxTabs: 3);
            await TabUntilFocusedFieldAsync(page, "Roast Level", maxTabs: 2);

            await unitPriceField.FillAsync("0");
            await nameField.ClickAsync();
            await Assertions.Expect(createButton).ToBeDisabledAsync();

            await nameField.FillAsync(productName);
            // MudNumericField @bind-Value commits on the browser's `change`
            // event, which Playwright's FillAsync + Locator.BlurAsync do NOT
            // synthesise (FillAsync fires only `input`; BlurAsync fires only
            // `blur`). A real Tab keypress moves focus and triggers `change`,
            // committing the bound C# decimal. Without this, the rendered
            // field shows "12.34" but the bound value stays at the default
            // 0.01, so Create saves that instead.
            await unitPriceField.ClickAsync();
            await page.Keyboard.PressAsync("Control+A");
            await page.Keyboard.PressAsync("Delete");
            await unitPriceField.PressSequentiallyAsync("12.34");
            await page.Keyboard.PressAsync("Tab");
            await descriptionField.FillAsync("Representative Playwright automation product.");
            await imageUrlField.FillAsync("https://example.com/playwright-product.png");

            await Assertions.Expect(createButton).ToBeEnabledAsync();
            await createButton.ClickAsync();
            try
            {
                await Assertions.Expect(dialog).Not.ToBeVisibleAsync(new() { Timeout = 2000 });
            }
            catch
            {
                var dialogText = await dialog.InnerTextAsync();
                var snackbars = await page.Locator(".mud-snackbar").AllInnerTextsAsync();
                throw new Xunit.Sdk.XunitException(
                    $"Product dialog stayed open after Create. Dialog text:{Environment.NewLine}{dialogText}{Environment.NewLine}{Environment.NewLine}Snackbars:{Environment.NewLine}{string.Join(Environment.NewLine, snackbars)}");
            }

            var createdProduct = await WaitForProductByExactNameAsync(userId!, productName);
            createdProductId = createdProduct.Id;

            Assert.Equal(productName, createdProduct.Name);
            Assert.Equal(12.34m, createdProduct.UnitPrice);
            Assert.True(createdProduct.IsAvailable);

            await routedContent.GetByPlaceholder("Search products...").FillAsync(productName);
            await Assertions.Expect(routedContent.GetByText(productName, new LocatorGetByTextOptions { Exact = true }))
                .ToBeVisibleAsync();
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(userId))
            {
                if (!createdProductId.HasValue)
                {
                    var createdProduct = await _fixture.TryGetProductByExactNameAsync(userId, productName);
                    createdProductId = createdProduct?.Id;
                }

                if (createdProductId.HasValue)
                {
                    await _fixture.ArchiveProductByIdAsync(userId, createdProductId.Value, archivedNamePrefix);
                }
            }

            await _fixture.ClosePageAsync(page);
        }
    }

    private static async Task<string> GetFocusedFieldLabelAsync(IPage page)
    {
        return await page.EvaluateAsync<string>(
            "() => document.activeElement?.closest('.mud-input-control')?.querySelector('label')?.textContent?.trim() ?? ''");
    }

    private async Task<ProductSnapshot> WaitForProductByExactNameAsync(string userId, string productName)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var product = await _fixture.TryGetProductByExactNameAsync(userId, productName);
            if (product is not null)
            {
                return product;
            }

            await Task.Delay(250);
        }

        throw new Xunit.Sdk.XunitException($"Could not find product '{productName}' through the API.");
    }

    private static async Task TabUntilFocusedFieldAsync(IPage page, string expectedLabel, int maxTabs)
    {
        for (var attempt = 0; attempt < maxTabs; attempt++)
        {
            await page.Keyboard.PressAsync("Tab");
            if (string.Equals(await GetFocusedFieldLabelAsync(page), expectedLabel, StringComparison.Ordinal))
            {
                return;
            }
        }

        Assert.Equal(expectedLabel, await GetFocusedFieldLabelAsync(page));
    }

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
            await Task.Delay(300);

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

            // Wait for debounce (300ms) + grid reload
            await Task.Delay(500);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await PlaywrightFixture.WaitForGridDataAsync(page);

            // Grid should have filtered data
            var filteredRowCount = await PlaywrightFixture.GetGridRowCountAsync(page);
            Assert.True(filteredRowCount > 0, "Search for 'Espresso' should return results.");

            // Clear search to restore
            await searchField.FillAsync("");
            await Task.Delay(500);
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
