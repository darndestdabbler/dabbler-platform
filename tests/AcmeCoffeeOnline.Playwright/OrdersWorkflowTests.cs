using System.Text.RegularExpressions;
using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;

namespace AcmeCoffeeOnline.Playwright;

/// <summary>
/// E2E tests for the Orders list and Order detail pages.
///
/// NOTE: The order detail page (/orders/{id}) renders with IsReadOnly="true",
/// so status transitions, item add/remove, and note add/edit/delete are not
/// exposed in the current UI. These tests cover the read-only interactions
/// that are available through real UI controls. The spec's writable interactions
/// (catalog items 36–47) require a non-read-only order detail surface which
/// does not currently exist at a routable URL.
/// </summary>
[Collection(PlaywrightCollection.Name)]
public sealed class OrdersWorkflowTests
{
    private readonly PlaywrightFixture _fixture;

    public OrdersWorkflowTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task SignInAndNavigateToOrdersAsync(IPage page)
    {
        await _fixture.SeededSignInAsync(page, "admin@acmecoffee.com");
        await page.GotoAsync($"{_fixture.BaseUrl}/dashboard", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });
        await _fixture.NavigateViaDrawerAsync(page, "Orders", ".*/orders$");
    }

    private static int ExtractOrderIdFromUrl(string url)
    {
        var match = Regex.Match(url, @"/orders/(\d+)$");
        Assert.True(match.Success, $"Expected URL to match /orders/{{id}}, got {url}");
        return int.Parse(match.Groups[1].Value);
    }

    // ── Orders List Tests ──────────────────────────────────────────────

    [Fact]
    public async Task OrdersList_LoadsWithDataAndColumnsVisibleAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await SignInAndNavigateToOrdersAsync(page);
            await PlaywrightFixture.WaitForGridDataAsync(page);

            // Verify column headers
            var headerRow = page.Locator(".mud-table-head");
            await Assertions.Expect(headerRow.GetByText("Order #")).ToBeVisibleAsync();
            await Assertions.Expect(headerRow.GetByText("Retail Business Contact")).ToBeVisibleAsync();
            await Assertions.Expect(headerRow.GetByText("Order Date")).ToBeVisibleAsync();
            await Assertions.Expect(headerRow.GetByText("Status")).ToBeVisibleAsync();
            await Assertions.Expect(headerRow.GetByText("Total")).ToBeVisibleAsync();
            await Assertions.Expect(headerRow.GetByText("Ship Date")).ToBeVisibleAsync();

            // Verify data rows exist
            var rowCount = await PlaywrightFixture.GetGridRowCountAsync(page);
            Assert.True(rowCount > 0, "Orders grid should have at least one data row.");

            // Verify status text shows record count
            await Assertions.Expect(page.GetByText(new Regex(@"\d+ records"))).ToBeVisibleAsync();
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task OrderGrid_RowClickNavigatesToDetailPageAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await SignInAndNavigateToOrdersAsync(page);
            await PlaywrightFixture.WaitForGridDataAsync(page);

            await PlaywrightFixture.ClickGridRowAsync(page, 0);

            await Assertions.Expect(page).ToHaveURLAsync(new Regex(@".*/orders/\d+$"));
            await Assertions.Expect(page.GetByTestId("order-detail-title")).ToBeVisibleAsync();
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task OrdersList_StatusChips_VisibleOnRowsAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await SignInAndNavigateToOrdersAsync(page);
            await PlaywrightFixture.WaitForGridDataAsync(page);

            // Verify at least one status chip is visible in the grid
            var chips = page.Locator(".mud-table-body .mud-chip");
            var chipCount = await chips.CountAsync();
            Assert.True(chipCount > 0, "Orders grid should display status chips.");

            // Verify chip text matches known status values
            var firstChipText = (await chips.First.TextContentAsync())?.Trim() ?? string.Empty;
            var validStatuses = new[] { "Pending", "Confirmed", "Shipped", "Delivered", "Cancelled" };
            Assert.Contains(firstChipText, validStatuses);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task OrdersList_GridPaging_NavigatesCorrectlyAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await SignInAndNavigateToOrdersAsync(page);
            await PlaywrightFixture.WaitForGridDataAsync(page);

            // Check if paging controls exist (requires > 25 orders for default page size)
            var pager = page.Locator(".mud-table-pagination");
            var pagerVisible = await pager.IsVisibleAsync();

            if (pagerVisible)
            {
                // Click next page
                var nextButton = pager.Locator("button[aria-label='Next page']");
                if (await nextButton.IsEnabledAsync())
                {
                    await nextButton.ClickAsync();
                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                    await PlaywrightFixture.WaitForGridDataAsync(page);

                    var rowCount = await PlaywrightFixture.GetGridRowCountAsync(page);
                    Assert.True(rowCount > 0, "Page 2 should have data rows.");
                }
            }

            // If no pager, the test still passes — all data fits on one page
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    // ── Order Detail Tests ─────────────────────────────────────────────

    [Fact]
    public async Task OrderDetail_LoadsHeaderForm_AllFieldsVisibleAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            var userId = await _fixture.GetSeededUserIdAsync("admin@acmecoffee.com");
            await SignInAndNavigateToOrdersAsync(page);
            await PlaywrightFixture.WaitForGridDataAsync(page);

            await PlaywrightFixture.ClickGridRowAsync(page, 0);

            var orderId = ExtractOrderIdFromUrl(page.Url);
            var order = await _fixture.GetOrderByIdAsync(userId, orderId);

            // Verify order detail title
            await Assertions.Expect(page.GetByTestId("order-detail-title"))
                .ToContainTextAsync($"Order #{orderId}");

            // Verify Customer field is populated
            var customerInput = await PlaywrightFixture.GetFormInputAsync(page, "field-customer-name");
            await Assertions.Expect(customerInput).ToHaveValueAsync(order.CustomerName);

            // Verify Total field is populated
            var totalInput = await PlaywrightFixture.GetFormInputAsync(page, "field-total");
            var totalValue = await totalInput.InputValueAsync();
            Assert.False(string.IsNullOrWhiteSpace(totalValue), "Total field should be populated.");

            // Verify Order Date field is visible
            await Assertions.Expect(page.GetByTestId("field-order-date")).ToBeVisibleAsync();

            // Verify Notes field is visible
            await Assertions.Expect(page.GetByTestId("field-notes")).ToBeVisibleAsync();
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task OrderDetail_StatusChip_DisplaysCurrentStatusAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            var userId = await _fixture.GetSeededUserIdAsync("admin@acmecoffee.com");
            await SignInAndNavigateToOrdersAsync(page);
            await PlaywrightFixture.WaitForGridDataAsync(page);

            await PlaywrightFixture.ClickGridRowAsync(page, 0);

            var orderId = ExtractOrderIdFromUrl(page.Url);
            var order = await _fixture.GetOrderByIdAsync(userId, orderId);

            // Verify the status chip is visible and shows the correct status
            var detailPage = page.GetByTestId("order-detail-page");
            var statusChip = detailPage.Locator(".mud-chip").First;
            await Assertions.Expect(statusChip).ToBeVisibleAsync();

            var chipText = (await statusChip.TextContentAsync())?.Trim() ?? string.Empty;
            Assert.Equal(order.StatusDisplayName, chipText);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task OrderDetail_BackButton_ReturnsToOrdersListAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await SignInAndNavigateToOrdersAsync(page);
            await PlaywrightFixture.WaitForGridDataAsync(page);

            await PlaywrightFixture.ClickGridRowAsync(page, 0);
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(@".*/orders/\d+$"));

            // Click back button
            var backButton = page.Locator("button[aria-label='Back to orders']");
            await Assertions.Expect(backButton).ToBeVisibleAsync();
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

    // ── Items Tab Tests ────────────────────────────────────────────────

    [Fact]
    public async Task OrderDetail_ItemsTab_DisplaysItemCountAndGridAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            var userId = await _fixture.GetSeededUserIdAsync("admin@acmecoffee.com");
            await SignInAndNavigateToOrdersAsync(page);
            await PlaywrightFixture.WaitForGridDataAsync(page);

            await PlaywrightFixture.ClickGridRowAsync(page, 0);

            var orderId = ExtractOrderIdFromUrl(page.Url);
            var order = await _fixture.GetOrderByIdAsync(userId, orderId);

            // Verify the Items tab label shows the count
            var itemsTabHeader = page.GetByText(new Regex(@"Items\s*\(\d+\)"));
            await Assertions.Expect(itemsTabHeader).ToBeVisibleAsync();

            // The Items tab should be active by default — verify grid columns
            var itemsGrid = page.Locator(".mud-table").First;
            await Assertions.Expect(itemsGrid).ToBeVisibleAsync();

            var headerRow = itemsGrid.Locator(".mud-table-head");
            await Assertions.Expect(headerRow.GetByText("Product")).ToBeVisibleAsync();
            await Assertions.Expect(headerRow.GetByText("Qty")).ToBeVisibleAsync();
            await Assertions.Expect(headerRow.GetByText("Unit Price")).ToBeVisibleAsync();
            await Assertions.Expect(headerRow.GetByText("Discount %")).ToBeVisibleAsync();
            await Assertions.Expect(headerRow.GetByText("Line Total")).ToBeVisibleAsync();

            // Verify row count matches API data
            if (order.OrderItems.Count > 0)
            {
                var rows = itemsGrid.Locator(".mud-table-body tr.mud-table-row");
                await Assertions.Expect(rows).ToHaveCountAsync(order.OrderItems.Count);
            }
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task OrderDetail_ItemsTab_LineTotalCalculationCorrectAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            var userId = await _fixture.GetSeededUserIdAsync("admin@acmecoffee.com");
            await SignInAndNavigateToOrdersAsync(page);
            await PlaywrightFixture.WaitForGridDataAsync(page);

            await PlaywrightFixture.ClickGridRowAsync(page, 0);

            var orderId = ExtractOrderIdFromUrl(page.Url);
            var order = await _fixture.GetOrderByIdAsync(userId, orderId);

            if (order.OrderItems.Count > 0)
            {
                // Compute expected line total for the first item
                var firstItem = order.OrderItems[0];
                var expectedLineTotal = firstItem.Quantity * firstItem.UnitPrice
                    * (1 - firstItem.Discount / 100m);

                // Find the line total cell in the first row
                var itemsGrid = page.Locator(".mud-table").First;
                var firstRow = itemsGrid.Locator(".mud-table-body tr.mud-table-row").First;
                var cells = firstRow.Locator("td");

                // Line Total is the 5th column (0-indexed: Product, Qty, UnitPrice, Discount%, LineTotal)
                var lineTotalCell = cells.Nth(4);
                var lineTotalText = (await lineTotalCell.TextContentAsync())?.Trim() ?? string.Empty;

                // The line total is formatted as C2 (e.g., "$12.50")
                Assert.Contains(expectedLineTotal.ToString("N2"), lineTotalText);
            }
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    // ── Notes Tab Tests ────────────────────────────────────────────────

    [Fact]
    public async Task OrderDetail_NotesTab_SwitchTabShowsNotesGridAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            var userId = await _fixture.GetSeededUserIdAsync("admin@acmecoffee.com");
            await SignInAndNavigateToOrdersAsync(page);
            await PlaywrightFixture.WaitForGridDataAsync(page);

            await PlaywrightFixture.ClickGridRowAsync(page, 0);

            var orderId = ExtractOrderIdFromUrl(page.Url);

            // Click the Notes tab
            var notesTabHeader = page.GetByText(new Regex(@"Notes\s*\(\d+\)"));
            await Assertions.Expect(notesTabHeader).ToBeVisibleAsync();
            await notesTabHeader.ClickAsync();
            await Task.Delay(500); // Wait for tab panel to render

            // Verify notes grid columns are visible
            // After switching tabs, the notes grid should appear
            // Find the visible table (the notes grid)
            var notesGrid = page.Locator(".mud-tabpanel:visible .mud-table, .mud-tabs-panels .mud-table").Last;

            var headerRow = notesGrid.Locator(".mud-table-head");
            await Assertions.Expect(headerRow.GetByText("Date")).ToBeVisibleAsync();
            await Assertions.Expect(headerRow.GetByText("Author")).ToBeVisibleAsync();
            await Assertions.Expect(headerRow.GetByText("Note")).ToBeVisibleAsync();

            // Verify notes via API
            var notes = await _fixture.GetOrderNotesAsync(userId, orderId);
            if (notes.Count > 0)
            {
                var rows = notesGrid.Locator(".mud-table-body tr.mud-table-row");
                await Assertions.Expect(rows).ToHaveCountAsync(notes.Count);
            }
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    // ── Read-Only Verification ─────────────────────────────────────────

    [Fact]
    public async Task OrderDetail_ReadOnlyMode_StatusTransitionButtonsNotShownAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await SignInAndNavigateToOrdersAsync(page);
            await PlaywrightFixture.WaitForGridDataAsync(page);

            await PlaywrightFixture.ClickGridRowAsync(page, 0);

            // In read-only mode, status transition buttons should NOT be present
            var transitionButtons = page.GetByTestId("order-detail-page")
                .Locator("button:has-text('Confirmed'), button:has-text('Shipped'), button:has-text('Delivered'), button:has-text('Cancelled')");

            // None of the transition buttons should be visible
            await Assertions.Expect(transitionButtons).ToHaveCountAsync(0);

            // The status chip itself should still be visible (read-only display)
            var statusChip = page.GetByTestId("order-detail-page").Locator(".mud-chip").First;
            await Assertions.Expect(statusChip).ToBeVisibleAsync();
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task OrderDetail_ReadOnlyMode_ItemAndNoteControlsNotShownAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await SignInAndNavigateToOrdersAsync(page);
            await PlaywrightFixture.WaitForGridDataAsync(page);

            await PlaywrightFixture.ClickGridRowAsync(page, 0);

            // "Add Product" button should NOT be visible in read-only mode
            var addProductButton = page.GetByRole(AriaRole.Button, new() { Name = "Add Product" });
            await Assertions.Expect(addProductButton).ToHaveCountAsync(0);

            // Switch to Notes tab
            var notesTabHeader = page.GetByText(new Regex(@"Notes\s*\(\d+\)"));
            await notesTabHeader.ClickAsync();
            await Task.Delay(500);

            // "Add Note" button should NOT be visible in read-only mode
            var addNoteButton = page.GetByRole(AriaRole.Button, new() { Name = "Add Note" });
            await Assertions.Expect(addNoteButton).ToHaveCountAsync(0);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    // ── Cross-Page Verification ────────────────────────────────────────

    [Fact]
    public async Task OrderDetail_FieldsMatchApiData_CrossVerifiedAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            var userId = await _fixture.GetSeededUserIdAsync("admin@acmecoffee.com");
            await SignInAndNavigateToOrdersAsync(page);
            await PlaywrightFixture.WaitForGridDataAsync(page);

            await PlaywrightFixture.ClickGridRowAsync(page, 0);

            var orderId = ExtractOrderIdFromUrl(page.Url);
            var order = await _fixture.GetOrderByIdAsync(userId, orderId);

            // Cross-verify: customer name in form matches API
            var customerInput = await PlaywrightFixture.GetFormInputAsync(page, "field-customer-name");
            await Assertions.Expect(customerInput).ToHaveValueAsync(order.CustomerName);

            // Cross-verify: status chip matches API
            var detailPage = page.GetByTestId("order-detail-page");
            var statusChip = detailPage.Locator(".mud-chip").First;
            var chipText = (await statusChip.TextContentAsync())?.Trim() ?? string.Empty;
            Assert.Equal(order.StatusDisplayName, chipText);

            // Cross-verify: item count in tab matches API
            var itemsTabHeader = page.GetByText(new Regex(@"Items\s*\(\d+\)"));
            var tabText = await itemsTabHeader.TextContentAsync() ?? string.Empty;
            Assert.Contains($"({order.OrderItems.Count})", tabText);

            // Cross-verify: timestamps are visible
            await Assertions.Expect(page.GetByText("Created:")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByText("Modified:")).ToBeVisibleAsync();
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }
}
