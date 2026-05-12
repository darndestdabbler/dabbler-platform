using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;

namespace AcmeCoffeeOnline.Playwright;

[Collection(PlaywrightCollection.Name)]
public sealed class OrderItemTunerPageWorkflowTests
{
    private readonly PlaywrightFixture _fixture;

    public OrderItemTunerPageWorkflowTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task TunerPage_Loads_CorrectLayoutAsync()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, "admin@acmecoffee.com");
            await page.GotoAsync($"{_fixture.BaseUrl}/admin/order-item-tuner", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

            await Assertions.Expect(page.GetByTestId("order-item-tuner-title")).ToHaveTextAsync("Order item tuner");
            
            // Both details and link cards should be visible
            await Assertions.Expect(page.GetByTestId("order-item-tuner-shell-details")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("order-item-tuner-shell-link-card")).ToBeVisibleAsync();
            
            // Action buttons should be present
            await Assertions.Expect(page.GetByTestId("order-item-tuner-shell-save-button")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("order-item-tuner-shell-cancel-button")).ToBeVisibleAsync();
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task TunerPage_DirtyState_FlipsSaveMessageAsync()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, "admin@acmecoffee.com");
            await page.GotoAsync($"{_fixture.BaseUrl}/admin/order-item-tuner", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

            // 1. Edit only link data (Quantity)
            await PlaywrightFixture.FillFormFieldAsync(page, "order-item-tuner-quantity", "5");
            await page.GetByTestId("order-item-tuner-shell-save-button").ClickAsync();
            await Assertions.Expect(page.Locator(".mud-snackbar")).ToContainTextAsync("Saved line details (demo mode).");
            await page.Locator(".mud-snackbar .mud-icon-button").ClickAsync(); // Dismiss snackbar

            // 2. Edit only target data (Product Name)
            await PlaywrightFixture.FillFormFieldAsync(page, "order-item-tuner-product-name", "New Product Name");
            await page.GetByTestId("order-item-tuner-shell-save-button").ClickAsync();
            await Assertions.Expect(page.Locator(".mud-snackbar")).ToContainTextAsync("Saved product (demo mode).");
            await page.Locator(".mud-snackbar .mud-icon-button").ClickAsync(); // Dismiss snackbar

            // 3. Edit both target and link data
            await PlaywrightFixture.FillFormFieldAsync(page, "order-item-tuner-product-name", "Another Product Name");
            await PlaywrightFixture.FillFormFieldAsync(page, "order-item-tuner-discount", "25");
            await page.GetByTestId("order-item-tuner-shell-save-button").ClickAsync();
            await Assertions.Expect(page.Locator(".mud-snackbar")).ToContainTextAsync("Saved product + line details (demo mode).");
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }
}
