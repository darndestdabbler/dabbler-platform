using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;

namespace AcmeCoffeeOnline.Playwright;

[Collection(PlaywrightCollection.Name)]
public sealed class SmokeTests
{
    private readonly PlaywrightFixture _fixture;

    public SmokeTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task HomePage_ShowsApiConnectivityAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await page.GotoAsync(_fixture.BaseUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "AcmeCoffeeOnline foundation" }))
                .ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("[data-testid='api-status']"))
                .ToContainTextAsync("API reachable: ready");
            await Assertions.Expect(page.Locator("[data-testid='api-stage']"))
                .ToContainTextAsync("session-2-foundation");
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}
