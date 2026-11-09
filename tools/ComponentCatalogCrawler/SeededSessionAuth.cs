using Microsoft.Playwright;

namespace ComponentCatalogCrawler;

/// <summary>
/// Authenticates a Playwright page against the AcmeCoffeeOnline.Web
/// <c>/test-auth/seeded-session</c> endpoint using the same shared-secret
/// header (<c>X-Test-Auth-Secret</c>) that
/// <c>tests/AcmeCoffeeOnline.Playwright/Infrastructure/PlaywrightFixture.cs</c>
/// uses. After sign-in the page is navigated to <c>/dashboard</c> so the
/// caller can interact with the app shell immediately — this mirrors the
/// fixture's pattern (helpers leave the browser where a real user would be).
/// </summary>
public sealed class SeededSessionAuth
{
    private readonly string _baseUrl;
    private readonly string _testAuthSecret;

    public SeededSessionAuth(string baseUrl, string testAuthSecret)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _testAuthSecret = testAuthSecret;
    }

    public async Task SignInAsync(IPage page, string email, string role)
    {
        await page.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
        {
            ["X-Test-Auth-Secret"] = _testAuthSecret
        });

        var url = $"{_baseUrl}/test-auth/seeded-session"
                  + $"?email={Uri.EscapeDataString(email)}"
                  + $"&roles={Uri.EscapeDataString(role)}";

        await page.GotoAsync(url, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });

        await page.SetExtraHTTPHeadersAsync(new Dictionary<string, string>());

        // The sign-in endpoint returns JSON. Land on the dashboard so the
        // shell is rendered before the crawler attempts a per-route navigation.
        await page.GotoAsync($"{_baseUrl}/dashboard", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });
    }
}
