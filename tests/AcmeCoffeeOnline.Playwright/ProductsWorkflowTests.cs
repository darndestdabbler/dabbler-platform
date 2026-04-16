using System.Text.RegularExpressions;
using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;

namespace AcmeCoffeeOnline.Playwright;

[Collection(PlaywrightCollection.Name)]
public sealed class ProductsWorkflowTests
{
    private readonly PlaywrightFixture _fixture;

    public ProductsWorkflowTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task SignInAndNavigateToProductsAsync(IPage page)
    {
        await _fixture.SeededSignInAsync(page, "admin@acmecoffee.com");
        await page.GotoAsync($"{_fixture.BaseUrl}/dashboard", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });
        await _fixture.NavigateViaDrawerAsync(page, "Products", ".*/products$");
    }

    private static int ExtractProductIdFromUrl(string url)
    {
        var match = Regex.Match(url, @"/products/(\d+)$");
        Assert.True(match.Success, $"Expected URL to match /products/{{id}}, got {url}");
        return int.Parse(match.Groups[1].Value);
    }

    [Fact]
    public async Task ProductsList_LoadsWithDataAndColumnsVisibleAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await SignInAndNavigateToProductsAsync(page);
            await PlaywrightFixture.WaitForGridDataAsync(page);

            // Verify column headers
            var headerRow = page.Locator(".mud-table-head");
            await Assertions.Expect(headerRow.GetByText("Name")).ToBeVisibleAsync();
            await Assertions.Expect(headerRow.GetByText("Roast Level")).ToBeVisibleAsync();
            await Assertions.Expect(headerRow.GetByText("Caffeine")).ToBeVisibleAsync();
            await Assertions.Expect(headerRow.GetByText("Grind Type")).ToBeVisibleAsync();
            await Assertions.Expect(headerRow.GetByText("Unit Price")).ToBeVisibleAsync();
            await Assertions.Expect(headerRow.GetByText("Available")).ToBeVisibleAsync();

            // Verify data rows exist
            var rowCount = await PlaywrightFixture.GetGridRowCountAsync(page);
            Assert.True(rowCount > 0, "Products grid should have at least one data row.");

            // Verify status text shows record count
            await Assertions.Expect(page.GetByText(new Regex(@"\d+ records"))).ToBeVisibleAsync();
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task ProductGrid_RowClickNavigatesToDetailPageAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await SignInAndNavigateToProductsAsync(page);
            await PlaywrightFixture.WaitForGridDataAsync(page);

            await PlaywrightFixture.ClickGridRowAsync(page, 0);

            await Assertions.Expect(page).ToHaveURLAsync(new Regex(@".*/products/\d+$"));
            await Assertions.Expect(page.GetByTestId("product-detail-title")).ToBeVisibleAsync();
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task ProductDetail_LoadsExistingRecord_AllFieldsPopulatedAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            var userId = await _fixture.GetSeededUserIdAsync("admin@acmecoffee.com");
            await SignInAndNavigateToProductsAsync(page);
            await PlaywrightFixture.WaitForGridDataAsync(page);

            await PlaywrightFixture.ClickGridRowAsync(page, 0);

            var productId = ExtractProductIdFromUrl(page.Url);
            var product = await _fixture.GetProductByIdAsync(userId, productId);

            // Verify key fields are populated from the API data
            var nameInput = await PlaywrightFixture.GetFormInputAsync(page, "field-name");
            await Assertions.Expect(nameInput).ToHaveValueAsync(product.Name);

            var priceInput = await PlaywrightFixture.GetFormInputAsync(page, "field-unit-price");
            var priceValue = await priceInput.InputValueAsync();
            Assert.False(string.IsNullOrWhiteSpace(priceValue), "Unit price field should be populated.");

            // Verify save is disabled (no unsaved changes)
            await PlaywrightFixture.AssertSaveDisabledAsync(page);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task ProductDetail_EditAndSave_PersistsViaApiAsync()
    {
        var page = await _fixture.CreatePageAsync();
        var userId = await _fixture.GetSeededUserIdAsync("admin@acmecoffee.com");
        ProductSnapshot? original = null;
        int? productId = null;

        try
        {
            await SignInAndNavigateToProductsAsync(page);
            await PlaywrightFixture.WaitForGridDataAsync(page);

            await PlaywrightFixture.ClickGridRowAsync(page, 0);

            productId = ExtractProductIdFromUrl(page.Url);
            original = await _fixture.GetProductByIdAsync(userId, productId.Value);

            // Edit the name
            var editedName = $"{original.Name} Edited";
            await PlaywrightFixture.FillFormFieldAsync(page, "field-name", editedName);

            // Wait for Blazor to process the change and enable save
            await PlaywrightFixture.AssertSaveEnabledAsync(page);

            // Save
            await PlaywrightFixture.ClickSaveAsync(page);

            // Verify save button returns to disabled after successful save
            await PlaywrightFixture.AssertSaveDisabledAsync(page);

            // Verify via API
            var updated = await _fixture.GetProductByIdAsync(userId, productId.Value);
            Assert.Equal(editedName, updated.Name);
        }
        finally
        {
            if (original is not null && productId.HasValue)
            {
                await _fixture.UpdateProductAsync(userId, original);
            }

            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task ProductDetail_EditAndUndo_RevertsFieldsAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await SignInAndNavigateToProductsAsync(page);
            await PlaywrightFixture.WaitForGridDataAsync(page);

            await PlaywrightFixture.ClickGridRowAsync(page, 0);

            // Record original name
            var nameInput = await PlaywrightFixture.GetFormInputAsync(page, "field-name");
            var originalName = await nameInput.InputValueAsync();

            // Edit the name
            await PlaywrightFixture.FillFormFieldAsync(page, "field-name", originalName + " Modified");
            await PlaywrightFixture.AssertSaveEnabledAsync(page);

            // Click Undo
            await PlaywrightFixture.ClickUndoAsync(page);

            // Verify fields reverted and save is disabled
            await Assertions.Expect(nameInput).ToHaveValueAsync(originalName);
            await PlaywrightFixture.AssertSaveDisabledAsync(page);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task ProductDetail_ToggleAvailability_VerifiedViaApiAsync()
    {
        var page = await _fixture.CreatePageAsync();
        var userId = await _fixture.GetSeededUserIdAsync("admin@acmecoffee.com");
        int? productId = null;
        bool? originalAvailability = null;

        try
        {
            await SignInAndNavigateToProductsAsync(page);
            await PlaywrightFixture.WaitForGridDataAsync(page);

            await PlaywrightFixture.ClickGridRowAsync(page, 0);

            productId = ExtractProductIdFromUrl(page.Url);
            var before = await _fixture.GetProductByIdAsync(userId, productId.Value);
            originalAvailability = before.IsAvailable;

            // Click deactivate/activate button
            var toggleButton = page.GetByTestId("toggle-availability-button");
            await Assertions.Expect(toggleButton).ToBeVisibleAsync();
            await Assertions.Expect(toggleButton).ToBeEnabledAsync();

            var expectedLabel = before.IsAvailable ? "Deactivate" : "Activate";
            await Assertions.Expect(toggleButton).ToContainTextAsync(expectedLabel);

            await toggleButton.ClickAsync();
            // Wait for the Blazor Server round-trip to complete
            await Task.Delay(1000);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Verify the button label changed to reflect the new state
            var newExpectedLabel = before.IsAvailable ? "Activate" : "Deactivate";
            await Assertions.Expect(toggleButton).ToContainTextAsync(newExpectedLabel, new() { Timeout = 5000 });

            // Verify via API
            var after = await _fixture.GetProductByIdAsync(userId, productId.Value);
            Assert.Equal(!originalAvailability.Value, after.IsAvailable);
        }
        finally
        {
            if (productId.HasValue && originalAvailability.HasValue)
            {
                await _fixture.RestoreProductAvailabilityAsync(userId, productId.Value, originalAvailability.Value);
            }

            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task ProductDetail_BackButton_ReturnsToProductsListAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await SignInAndNavigateToProductsAsync(page);
            await PlaywrightFixture.WaitForGridDataAsync(page);

            await PlaywrightFixture.ClickGridRowAsync(page, 0);
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(@".*/products/\d+$"));

            // Click back button
            var backButton = page.Locator("button[aria-label='Back to products']");
            await Assertions.Expect(backButton).ToBeVisibleAsync();
            await backButton.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await Assertions.Expect(page).ToHaveURLAsync(new Regex(@".*/products$"));
            await PlaywrightFixture.WaitForGridDataAsync(page);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task ProductsFilter_ByRoastLevel_FiltersGridAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await SignInAndNavigateToProductsAsync(page);
            await PlaywrightFixture.WaitForGridDataAsync(page);

            // Record initial status text
            var statusText = page.Locator(".mud-typography-body2").First;
            var initialText = await statusText.TextContentAsync();

            // Select a roast level filter — scope to the filter panel to avoid
            // matching the grid header column with the same label
            var filterPanel = page.Locator("[role='search']");
            var roastSelect = filterPanel.GetByLabel("Roast Level").First;
            await roastSelect.ClickAsync();
            var option = page.Locator(".mud-popover-open")
                .GetByText("Dark", new() { Exact = true }).First;
            await Assertions.Expect(option).ToBeVisibleAsync();
            await option.ClickAsync();

            // Wait for debounce + grid reload
            await Task.Delay(500);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Verify filter chips appeared (proves filter was applied)
            await Assertions.Expect(page.Locator(".mud-chip").First).ToBeVisibleAsync(new() { Timeout = 5000 });

            // Grid should still have data (filtered results)
            await PlaywrightFixture.WaitForGridDataAsync(page);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task ProductsFilter_ByPriceRange_FiltersGridAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await SignInAndNavigateToProductsAsync(page);
            await PlaywrightFixture.WaitForGridDataAsync(page);

            // Set min price filter — scope to filter panel
            var filterPanel = page.Locator("[role='search']");
            var minPriceInput = filterPanel.GetByPlaceholder("Min $");
            await minPriceInput.FillAsync("15");

            // Set max price filter
            var maxPriceInput = filterPanel.GetByPlaceholder("Max $");
            await maxPriceInput.FillAsync("25");

            // Wait for debounce (300ms MudNumericField + 200ms page debounce) + grid reload
            await Task.Delay(800);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Verify filter chips appeared (proves filters were applied)
            await Assertions.Expect(page.Locator(".mud-chip").First).ToBeVisibleAsync(new() { Timeout = 5000 });

            // Grid should have filtered results
            await PlaywrightFixture.WaitForGridDataAsync(page);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }
}
