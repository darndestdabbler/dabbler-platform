using System.Text.RegularExpressions;
using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;

namespace AcmeCoffeeOnline.Playwright;

/// <summary>
/// Playwright tests for the DeclarativeFilterPanel's URL query-string persistence.
/// Verifies that filter state set via the UI is reflected in the URL and restored
/// when the page is reloaded with that URL.
/// </summary>
[Collection(PlaywrightCollection.Name)]
public sealed class ProductsFilterQueryStringTests
{
    private readonly PlaywrightFixture _fixture;

    public ProductsFilterQueryStringTests(PlaywrightFixture fixture)
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

    [Fact]
    public async Task ProductsFilter_QueryStringPersistedAndRestored_OnReloadAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await SignInAndNavigateToProductsAsync(page);
            await PlaywrightFixture.WaitForGridDataAsync(page);

            // Verify the filter panel is present
            var filterPanel = page.Locator("[role='search']");
            await Assertions.Expect(filterPanel).ToBeVisibleAsync();

            // Select a Roast Level filter
            var roastSelect = filterPanel.GetByLabel("Roast Level").First;
            await roastSelect.ClickAsync();
            var option = page.Locator(".mud-popover-open")
                .GetByText("Dark", new() { Exact = true }).First;
            await Assertions.Expect(option).ToBeVisibleAsync();
            await option.ClickAsync();

            // Wait for the filter to debounce and the URL to update
            await Task.Delay(600);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Verify a chip appeared (filter is applied)
            await Assertions.Expect(page.Locator(".mud-chip").First).ToBeVisibleAsync(new() { Timeout = 5000 });

            // Capture the URL — it should now contain the RoastLevel query param
            var urlWithFilter = page.Url;
            Assert.Contains("RoastLevel", urlWithFilter);

            // Reload the page with the same URL (simulates F5 / browser reload)
            await page.GotoAsync(urlWithFilter, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            // After reload the filter panel should show the chip restored from the URL
            await PlaywrightFixture.WaitForGridDataAsync(page);
            await Assertions.Expect(page.Locator("[role='search']")).ToBeVisibleAsync();

            // Chip is present → filter was restored from URL
            await Assertions.Expect(page.Locator(".mud-chip").First).ToBeVisibleAsync(new() { Timeout = 5000 });
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }
}
