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
            await Assertions.Expect(page.GetByTestId("main-shell"))
                .ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("link[data-dabbler-asset='css/dabbler-foundation.css']"))
                .ToHaveCountAsync(1);
            await Assertions.Expect(page.Locator("[data-testid='api-status']"))
                .ToContainTextAsync("API reachable: ready");
            await Assertions.Expect(page.Locator("[data-testid='api-stage']"))
                .ToContainTextAsync("session-4-guardrails");
            await Assertions.Expect(page.Locator("[data-testid='auth-delivery-mode']"))
                .ToContainTextAsync("MagicLink");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task LoginPage_RendersAnonymousAuthShellAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await page.GotoAsync($"{_fixture.BaseUrl}/login", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            await Assertions.Expect(page.GetByTestId("auth-shell"))
                .ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("login-page"))
                .ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("login-delivery-mode"))
                .ToContainTextAsync("MagicLink");
            await Assertions.Expect(page.GetByTestId("main-shell"))
                .ToHaveCountAsync(0);
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}
