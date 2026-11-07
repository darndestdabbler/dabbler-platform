using System.Text.RegularExpressions;
using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;

namespace AcmeCoffeeOnline.Playwright;

[Collection(PlaywrightCollection.Name)]
public sealed class DashboardWorkflowTests
{
    private readonly PlaywrightFixture _fixture;

    public DashboardWorkflowTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task DashboardNavCard_NavigatesToOrdersAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await _fixture.SeededSignInAsync(page, "jdoe@acmecoffee.com");
            await page.GotoAsync($"{_fixture.BaseUrl}/dashboard", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });
            await Assertions.Expect(page.GetByTestId("main-shell")).ToBeVisibleAsync();

            await _fixture.ClickDashboardNavCardAsync(page, "nav-card-orders", ".*/orders$");

            await Assertions.Expect(page.GetByTestId("routed-page-content")).ToBeVisibleAsync();
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task DashboardNavCard_NavigatesToProductsAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await _fixture.SeededSignInAsync(page, "jdoe@acmecoffee.com");
            await page.GotoAsync($"{_fixture.BaseUrl}/dashboard", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });
            await Assertions.Expect(page.GetByTestId("main-shell")).ToBeVisibleAsync();

            await _fixture.ClickDashboardNavCardAsync(page, "nav-card-products", ".*/products$");

            await Assertions.Expect(page.GetByTestId("routed-page-content")).ToBeVisibleAsync();
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task DashboardReportLink_NavigatesToOrdersByRetailBusinessAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await _fixture.SeededSignInAsync(page, "jdoe@acmecoffee.com");
            await page.GotoAsync($"{_fixture.BaseUrl}/dashboard", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });
            await Assertions.Expect(page.GetByTestId("main-shell")).ToBeVisibleAsync();

            var reportLink = page.GetByTestId("report-link-orders-by-customer");
            await Assertions.Expect(reportLink).ToBeVisibleAsync();
            await reportLink.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await Assertions.Expect(page).ToHaveURLAsync(new Regex(".*/reports/orders-by-customer$"));
            await Assertions.Expect(page.GetByTestId("routed-page-content")).ToBeVisibleAsync();
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }
}
