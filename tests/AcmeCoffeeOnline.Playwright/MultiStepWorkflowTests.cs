using System.Text.RegularExpressions;
using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;

namespace AcmeCoffeeOnline.Playwright;

/// <summary>
/// Multi-step workflow tests that prove cross-page user journeys hold together.
/// Each test navigates through real UI controls across multiple pages.
/// </summary>
[Collection(PlaywrightCollection.Name)]
public sealed class MultiStepWorkflowTests
{
    private readonly PlaywrightFixture _fixture;

    public MultiStepWorkflowTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task SignInAndLoadDashboardAsync(IPage page, string email = "admin@acmecoffee.com")
    {
        await _fixture.SeededSignInAsync(page, email);
        await page.GotoAsync($"{_fixture.BaseUrl}/dashboard", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });
        await Assertions.Expect(page.GetByTestId("main-shell")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task ProductLifecycle_DrawerToEditSaveBackVerifyGridAsync()
    {
        var page = await _fixture.CreatePageAsync();
        var userId = await _fixture.GetSeededUserIdAsync("admin@acmecoffee.com");
        ProductSnapshot? original = null;
        int? productId = null;

        try
        {
            await SignInAndLoadDashboardAsync(page);

            // Step 1: Navigate to products via drawer
            await _fixture.NavigateViaDrawerAsync(page, "Products", ".*/products$");
            await PlaywrightFixture.WaitForGridDataAsync(page);

            // Step 2: Click a grid row to navigate to detail
            await PlaywrightFixture.ClickGridRowAsync(page, 0);
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(@".*/products/\d+$"));

            var match = Regex.Match(page.Url, @"/products/(\d+)$");
            productId = int.Parse(match.Groups[1].Value);
            original = await _fixture.GetProductByIdAsync(userId, productId.Value);

            // Step 3: Edit the name
            var editedName = $"{original.Name} Lifecycle";
            await PlaywrightFixture.FillFormFieldAsync(page, "field-name", editedName);
            await PlaywrightFixture.AssertSaveEnabledAsync(page);

            // Step 4: Save
            await PlaywrightFixture.ClickSaveAsync(page);
            await PlaywrightFixture.AssertSaveDisabledAsync(page);

            // Verify via API
            var updated = await _fixture.GetProductByIdAsync(userId, productId.Value);
            Assert.Equal(editedName, updated.Name);

            // Step 5: Navigate back to products list
            var backButton = page.Locator("button[aria-label='Back to products']");
            await backButton.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(@".*/products$"));

            // Step 6: Verify the grid shows the updated name
            await PlaywrightFixture.WaitForGridDataAsync(page);
            await Assertions.Expect(page.GetByText(editedName)).ToBeVisibleAsync();
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
    public async Task CustomerLifecycle_DrawerToEditSaveApiVerifyBackAsync()
    {
        var page = await _fixture.CreatePageAsync();
        var userId = await _fixture.GetSeededUserIdAsync("admin@acmecoffee.com");
        CustomerSnapshot? original = null;
        int? customerId = null;

        try
        {
            await SignInAndLoadDashboardAsync(page);

            // Step 1: Navigate to customers via drawer
            await _fixture.NavigateViaDrawerAsync(page, "Retail Businesses", ".*/customers$");
            await PlaywrightFixture.WaitForGridDataAsync(page);

            // Step 2: Click a grid row to navigate to detail
            await PlaywrightFixture.ClickGridRowAsync(page, 0);
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(@".*/customers/\d+$"));

            var match = Regex.Match(page.Url, @"/customers/(\d+)$");
            customerId = int.Parse(match.Groups[1].Value);
            original = await _fixture.GetCustomerByIdAsync(userId, customerId.Value);

            // Step 3: Edit the phone field
            var editedPhone = $"555-{DateTime.UtcNow:mmssfff}";
            await PlaywrightFixture.FillFormFieldAsync(page, "field-phone", editedPhone);
            await PlaywrightFixture.AssertSaveEnabledAsync(page);

            // Step 4: Save
            await PlaywrightFixture.ClickSaveAsync(page);
            await PlaywrightFixture.AssertSaveDisabledAsync(page);

            // Step 5: Verify via API
            var updated = await _fixture.GetCustomerByIdAsync(userId, customerId.Value);
            Assert.Equal(editedPhone, updated.Phone);

            // Step 6: Navigate back to customers list
            var backButton = page.Locator("button[aria-label='Back to retail businesses']");
            await backButton.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(@".*/customers$"));
            await PlaywrightFixture.WaitForGridDataAsync(page);
        }
        finally
        {
            if (original is not null && customerId.HasValue)
            {
                await _fixture.UpdateCustomerAsync(userId, original);
            }

            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task OrderTabNavigation_DrawerToDetailItemsNotesBackAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await SignInAndLoadDashboardAsync(page);

            // Step 1: Navigate to orders via drawer
            await _fixture.NavigateViaDrawerAsync(page, "Orders", ".*/orders$");
            await PlaywrightFixture.WaitForGridDataAsync(page);

            // Step 2: Click a row to navigate to order detail
            await PlaywrightFixture.ClickGridRowAsync(page, 0);
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(@".*/orders/\d+$"));
            await Assertions.Expect(page.GetByTestId("order-detail-title")).ToBeVisibleAsync();

            // Step 3: Verify Items tab is active with grid visible
            var itemsTabHeader = page.GetByText(new Regex(@"Items\s*\(\d+\)"));
            await Assertions.Expect(itemsTabHeader).ToBeVisibleAsync();

            var itemsGrid = page.Locator(".mud-table").First;
            await Assertions.Expect(itemsGrid).ToBeVisibleAsync();

            // Step 4: Switch to Notes tab
            var notesTabHeader = page.GetByText(new Regex(@"Notes\s*\(\d+\)"));
            await Assertions.Expect(notesTabHeader).ToBeVisibleAsync();
            await notesTabHeader.ClickAsync();

            // Verify Notes grid is visible — wait for the grid to render after tab switch
            var notesGrid = page.Locator(".mud-tabpanel:visible .mud-table, .mud-tabs-panels .mud-table").Last;
            await Assertions.Expect(notesGrid).ToBeVisibleAsync();
            var notesHeaderRow = notesGrid.Locator(".mud-table-head");
            await Assertions.Expect(notesHeaderRow.GetByText("Author")).ToBeVisibleAsync();

            // Step 5: Navigate back to orders list
            var backButton = page.Locator("button[aria-label='Back to orders']");
            await backButton.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(@".*/orders$"));
            await PlaywrightFixture.WaitForGridDataAsync(page);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task DashboardToDetail_CardToListToDetailBackToDashboardAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await SignInAndLoadDashboardAsync(page);

            // Step 1: Click Products nav card on dashboard
            await _fixture.ClickDashboardNavCardAsync(page, "nav-card-products", ".*/products$");
            await PlaywrightFixture.WaitForGridDataAsync(page);

            // Step 2: Click a grid row to navigate to detail
            await PlaywrightFixture.ClickGridRowAsync(page, 0);
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(@".*/products/\d+$"));
            await Assertions.Expect(page.GetByTestId("product-detail-title")).ToBeVisibleAsync();

            // Step 3: Navigate back to products list
            var backButton = page.Locator("button[aria-label='Back to products']");
            await backButton.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(@".*/products$"));
            await PlaywrightFixture.WaitForGridDataAsync(page);

            // Step 4: Navigate back to dashboard via drawer
            await _fixture.NavigateViaDrawerAsync(page, "Dashboard", ".*/dashboard$");

            // Verify dashboard cards are visible
            await Assertions.Expect(page.GetByTestId("nav-card-orders")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("nav-card-products")).ToBeVisibleAsync();
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task FilterSearchNavigate_ProductsFilterRowClickDetailBackAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await SignInAndLoadDashboardAsync(page);

            // Step 1: Navigate to products
            await _fixture.NavigateViaDrawerAsync(page, "Products", ".*/products$");
            await PlaywrightFixture.WaitForGridDataAsync(page);

            // Step 2: Apply a roast level filter
            var filterPanel = page.Locator("[role='search']");
            var roastSelect = filterPanel.GetByLabel("Roast Level").First;
            await roastSelect.ClickAsync();
            var option = page.Locator(".mud-popover-open")
                .GetByText("Dark", new() { Exact = true }).First;
            await Assertions.Expect(option).ToBeVisibleAsync();
            await option.ClickAsync();

            // Debounce delay: filter select triggers component debounce + grid reload, not render-correlated.
            await Task.Delay(500);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Verify filter chip appeared
            await Assertions.Expect(page.Locator(".mud-chip").First)
                .ToBeVisibleAsync(new() { Timeout = 5000 });

            // Record the filtered row count
            await PlaywrightFixture.WaitForGridDataAsync(page);
            var filteredRowCount = await PlaywrightFixture.GetGridRowCountAsync(page);
            Assert.True(filteredRowCount > 0, "Filtered products should have results.");

            // Step 3: Click a row to go to detail
            await PlaywrightFixture.ClickGridRowAsync(page, 0);
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(@".*/products/\d+$"));

            // Step 4: Verify the detail page loaded
            await Assertions.Expect(page.GetByTestId("product-detail-title")).ToBeVisibleAsync();

            // Step 5: Go back to the products list
            var backButton = page.Locator("button[aria-label='Back to products']");
            await backButton.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(@".*/products$"));

            // Step 6: Verify the products list reloaded with data
            await PlaywrightFixture.WaitForGridDataAsync(page);
            var returnRowCount = await PlaywrightFixture.GetGridRowCountAsync(page);
            Assert.True(returnRowCount > 0, "Products list should have data after returning from detail.");
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task RoleScopedNavigation_CustomerRole_AdminLinksHiddenAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            // Sign in as Customer role user
            await _fixture.SeededSignInAsync(page, "james.smith@example.com");
            await page.GotoAsync($"{_fixture.BaseUrl}/dashboard", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });
            await Assertions.Expect(page.GetByTestId("main-shell")).ToBeVisibleAsync();

            var drawer = page.GetByTestId("nav-drawer");
            await Assertions.Expect(drawer).ToBeVisibleAsync();

            // Verify standard nav links ARE visible for Customer role
            await Assertions.Expect(drawer.GetByText("Dashboard", new() { Exact = true }).First)
                .ToBeVisibleAsync();
            await Assertions.Expect(drawer.GetByText("Orders", new() { Exact = true }).First)
                .ToBeVisibleAsync();
            await Assertions.Expect(drawer.GetByText("Products", new() { Exact = true }).First)
                .ToBeVisibleAsync();

            // Verify Admin group is NOT visible for Customer role
            var adminGroupLinks = drawer.GetByText("Admin", new() { Exact = true });
            await Assertions.Expect(adminGroupLinks).ToHaveCountAsync(0);

            // Verify specific admin links are not reachable
            await Assertions.Expect(drawer.GetByText("User Management", new() { Exact = true }))
                .ToHaveCountAsync(0);
            await Assertions.Expect(drawer.GetByText("Product Management", new() { Exact = true }))
                .ToHaveCountAsync(0);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }
}
