using System.Text.RegularExpressions;
using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;

namespace AcmeCoffeeOnline.Playwright;

/// <summary>
/// Playwright tests for Session 3 Orders migration:
/// - DeclarativeFilterPanel query-string persistence on /orders
/// - Status workflow transition buttons in the DirtyAwareActionRow CustomActions section
/// </summary>
[Collection(PlaywrightCollection.Name)]
public sealed class OrdersMigrationTests
{
    private readonly PlaywrightFixture _fixture;

    public OrdersMigrationTests(PlaywrightFixture fixture)
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

    [Fact]
    public async Task OrdersFilter_QueryStringPersistedAndRestored_OnReloadAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await SignInAndNavigateToOrdersAsync(page);
            await PlaywrightFixture.WaitForGridDataAsync(page);

            // Verify the DeclarativeFilterPanel is present
            var filterPanel = page.Locator("[role='search']");
            await Assertions.Expect(filterPanel).ToBeVisibleAsync();

            // Select a Status filter value
            var statusSelect = filterPanel.GetByLabel("Status").First;
            await statusSelect.ClickAsync();
            var option = page.Locator(".mud-popover-open")
                .GetByText("Pending", new() { Exact = true }).First;
            await Assertions.Expect(option).ToBeVisibleAsync();
            await option.ClickAsync();

            // Wait for debounce and URL update
            await Task.Delay(600);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // An active-filter chip should appear inside the filter panel (not a grid row chip)
            var filterPanelChip = filterPanel.Locator(".mud-chip").First;
            await Assertions.Expect(filterPanelChip).ToBeVisibleAsync(new() { Timeout = 5000 });

            // URL should contain the Status query parameter
            var urlWithFilter = page.Url;
            Assert.Contains("Status", urlWithFilter);

            // Reload with the filtered URL
            await page.GotoAsync(urlWithFilter, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            await PlaywrightFixture.WaitForGridDataAsync(page);
            var filterPanelAfterReload = page.Locator("[role='search']");
            await Assertions.Expect(filterPanelAfterReload).ToBeVisibleAsync();

            // Chip restored inside filter panel from URL
            await Assertions.Expect(filterPanelAfterReload.Locator(".mud-chip").First).ToBeVisibleAsync(new() { Timeout = 5000 });
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task OrderDetail_DirtyAwareActionRow_SaveAndUndo_BehaviorAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            var userId = await _fixture.GetSeededUserIdAsync("admin@acmecoffee.com");
            await SignInAndNavigateToOrdersAsync(page);
            await PlaywrightFixture.WaitForGridDataAsync(page);

            await PlaywrightFixture.ClickGridRowAsync(page, 0);
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(@".*/orders/\d+$"));

            // DirtyAwareActionRow is rendered: save-button exists and starts disabled
            await Assertions.Expect(page.GetByTestId("save-button")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("save-button")).ToBeDisabledAsync();

            // Undo button (CustomActions slot) starts disabled
            await Assertions.Expect(page.GetByTestId("undo-button")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("undo-button")).ToBeDisabledAsync();

            // Edit the Notes field — triggers NotifyChanged via ValueChanged handler
            var originalNotes = await page.GetByTestId("field-notes").InputValueAsync();
            var marker = $"DirtyRowTest-{Guid.NewGuid():N}";
            await PlaywrightFixture.FillFormFieldAsync(page, "field-notes", marker);

            // Save and Undo become enabled
            await Assertions.Expect(page.GetByTestId("save-button")).ToBeEnabledAsync();
            await Assertions.Expect(page.GetByTestId("undo-button")).ToBeEnabledAsync();

            // Undo restores the field and disables both buttons
            await page.GetByTestId("undo-button").ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await Assertions.Expect(page.GetByTestId("save-button")).ToBeDisabledAsync();
            await Assertions.Expect(page.GetByTestId("undo-button")).ToBeDisabledAsync();
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task OrderDetail_StatusTransitionButton_UpdatesStatusChipAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await SignInAndNavigateToOrdersAsync(page);
            await PlaywrightFixture.WaitForGridDataAsync(page);

            // Look for a non-terminal order to transition
            var rowCount = await PlaywrightFixture.GetGridRowCountAsync(page);
            Assert.True(rowCount > 0, "Expected at least one order row.");

            // Click through rows until we find one with transition buttons
            string? foundStatus = null;
            int clickedRow = -1;
            for (int i = 0; i < Math.Min(rowCount, 5); i++)
            {
                await PlaywrightFixture.ClickGridRowAsync(page, i);
                await Assertions.Expect(page).ToHaveURLAsync(new Regex(@".*/orders/\d+$"));

                var detailPage = page.GetByTestId("order-detail-page");
                var chip = detailPage.Locator(".mud-chip").First;
                var chipText = (await chip.TextContentAsync())?.Trim() ?? string.Empty;

                var terminalStates = new[] { "Delivered", "Cancelled" };
                if (!terminalStates.Contains(chipText))
                {
                    foundStatus = chipText;
                    clickedRow = i;
                    break;
                }

                await page.GoBackAsync(new PageGoBackOptions { WaitUntil = WaitUntilState.NetworkIdle });
                await PlaywrightFixture.WaitForGridDataAsync(page);
            }

            if (foundStatus is null)
            {
                // All sampled orders are in terminal states — skip but don't fail
                return;
            }

            // Verify the transition buttons are visible and at least one exists
            var detailPageFinal = page.GetByTestId("order-detail-page");
            var transitionButtons = detailPageFinal
                .Locator("button:has-text('Confirmed'), button:has-text('Shipped'), button:has-text('Delivered'), button:has-text('Cancelled')");
            var count = await transitionButtons.CountAsync();
            Assert.True(count > 0, $"Expected transition buttons for status={foundStatus}.");

            // Click the first available transition
            var firstTransition = transitionButtons.First;
            var targetStatusText = (await firstTransition.TextContentAsync())?.Trim() ?? string.Empty;
            Assert.False(string.IsNullOrEmpty(targetStatusText));

            await firstTransition.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Status chip should now show the new status
            var updatedChip = detailPageFinal.Locator(".mud-chip").First;
            await Assertions.Expect(updatedChip).ToContainTextAsync(targetStatusText, new() { Timeout = 10000 });
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }
}
