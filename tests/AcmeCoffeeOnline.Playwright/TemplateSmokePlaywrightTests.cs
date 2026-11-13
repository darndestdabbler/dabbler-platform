using Microsoft.Playwright;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace AcmeCoffeeOnline.Playwright;

/// <summary>
/// Browser smoke tests for the packaged dabbler-app template at runtime.
///
/// These tests verify that a scaffolded template app boots correctly, serves static assets,
/// and renders the authentication shell — proving runtime DI correctness that goes beyond
/// the build-only validation in Dabbler.Packaging.Smoke (PackagingSmokeTests).
///
/// Skip condition: PLAYWRIGHT_TEMPLATE_APP_URL is not set OR the app is not reachable.
/// Setup to run:
///   1. Run tools/pack.ps1 to produce pack-output/.
///   2. dotnet new dabbler-app -o /tmp/SmokeApp --debug:custom-hive /tmp/SmokeHive (feed pointing at pack-output).
///   3. Configure DefaultConnection to a local SQL Server / LocalDB instance.
///   4. dotnet run --project /tmp/SmokeApp -- (starts on e.g. http://localhost:5099).
///   5. export PLAYWRIGHT_TEMPLATE_APP_URL=http://localhost:5099
///   6. dotnet test --filter "FullyQualifiedName~TemplateSmokePlaywrightTests"
/// </summary>
public sealed class TemplateSmokePlaywrightTests : IAsyncLifetime
{
    private static readonly string? TemplateAppUrl =
        Environment.GetEnvironmentVariable("PLAYWRIGHT_TEMPLATE_APP_URL");

    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async Task InitializeAsync()
    {
        if (string.IsNullOrWhiteSpace(TemplateAppUrl))
            return;

        _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
    }

    public async Task DisposeAsync()
    {
        if (_browser is not null) await _browser.DisposeAsync();
        _playwright?.Dispose();
    }

    /// <summary>
    /// Confirms that the Blazor framework JS bundle is served at the expected path.
    /// This is the earliest and cheapest signal that the template app's static-asset
    /// pipeline is wired correctly (no HTTP-level failures, MapStaticAssets() present).
    /// </summary>
    [SkippableFact]
    public async Task DabblerApp_BlazorFrameworkJs_IsServedAsync()
    {
        SkipIfNotConfigured();

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        var jsUrl = TemplateAppUrl!.TrimEnd('/') + "/_framework/blazor.server.js";

        System.Net.Http.HttpResponseMessage? response = null;
        try
        {
            response = await http.GetAsync(jsUrl);
        }
        catch (HttpRequestException ex)
        {
            // Only skip on connection-establishment failures (refused, DNS, reset).
            // Request timeouts (TaskCanceledException) indicate a reachable-but-hung app
            // and should surface as a test failure rather than a silent skip.
            Skip.If(true,
                $"Template app at {TemplateAppUrl} is not reachable ({ex.GetType().Name}). " +
                "Ensure the app is running before running template browser smoke tests.");
        }

        // If we reach here without an exception, the app is reachable.
        // A non-success status code is a real failure (app running but static assets broken).
        Assert.True(response!.IsSuccessStatusCode,
            $"blazor.server.js not found at {jsUrl} (HTTP {(int)response.StatusCode}) — " +
            "static asset delivery is broken in the scaffolded template app.");
    }

    /// <summary>
    /// Confirms the scaffolded app boots, redirects unauthenticated requests to /login,
    /// and renders the login page with the expected auth form — proving runtime DI correctness
    /// that build-only smoke tests cannot verify.
    /// </summary>
    [SkippableFact]
    public async Task DabblerApp_LoginPage_RendersFromScaffoldedTemplateAsync()
    {
        SkipIfNotConfigured();

        var page = await _browser!.NewPageAsync();

        try
        {
            // Navigate to root; auth guard redirects to /login.
            // Convert connectivity failures to Skip (env var set but app not running).
            try
            {
                await page.GotoAsync(TemplateAppUrl!, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle,
                    Timeout = 30_000
                });
            }
            catch (PlaywrightException ex) when (
                ex.Message.Contains("ERR_CONNECTION_REFUSED") ||
                ex.Message.Contains("ERR_CONNECTION_RESET") ||
                ex.Message.Contains("ECONNREFUSED"))
            {
                Skip.If(true,
                    $"Template app at {TemplateAppUrl} is not reachable ({ex.Message[..Math.Min(80, ex.Message.Length)]}). " +
                    "Ensure the app is running before running template browser smoke tests.");
            }

            // Confirmed redirect to /login (or /login?returnUrl=...)
            await Assertions.Expect(page).ToHaveURLAsync(
                new Regex(".*/login(\\?.*)?$"),
                new PageAssertionsToHaveURLOptions { Timeout = 10_000 });

            // Page title identifies the template app (not the demo app)
            await Assertions.Expect(page).ToHaveTitleAsync(
                new Regex("Sign In"),
                new PageAssertionsToHaveTitleOptions { Timeout = 5_000 });

            // Login page container is rendered (data-testid from Login.razor)
            await Assertions.Expect(page.GetByTestId("login-page"))
                .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10_000 });

            // Email input is enabled (DI wired correctly — component did not crash on render)
            await Assertions.Expect(page.GetByLabel("Email address"))
                .ToBeVisibleAsync();

            // Submit button is present and initially disabled (empty input guard)
            var sendButton = page.GetByRole(AriaRole.Button, new() { Name = "Send sign-in link" });
            await Assertions.Expect(sendButton).ToBeVisibleAsync();
            await Assertions.Expect(sendButton).ToBeDisabledAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    /// Confirms the minimal template (dabbler-min) also redirects to /login and renders
    /// the auth shell — a separate signal because dabbler-min excludes the sample-items
    /// and reports infrastructure from dabbler-app.
    /// Skip condition: PLAYWRIGHT_TEMPLATE_MIN_APP_URL is not set or not reachable.
    /// </summary>
    [SkippableFact]
    public async Task DabblerMinTemplate_LoginPage_RendersFromScaffoldedTemplateAsync()
    {
        var minUrl = Environment.GetEnvironmentVariable("PLAYWRIGHT_TEMPLATE_MIN_APP_URL");

        Skip.If(string.IsNullOrWhiteSpace(minUrl),
            "PLAYWRIGHT_TEMPLATE_MIN_APP_URL is not set. " +
            "Set it to a running dabbler-min instance to run minimal-template browser smoke.");

        if (_browser is null)
        {
            _playwright ??= await Microsoft.Playwright.Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });
        }

        var page = await _browser.NewPageAsync();

        try
        {
            try
            {
                await page.GotoAsync(minUrl, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle,
                    Timeout = 30_000
                });
            }
            catch (PlaywrightException ex) when (
                ex.Message.Contains("ERR_CONNECTION_REFUSED") ||
                ex.Message.Contains("ERR_CONNECTION_RESET") ||
                ex.Message.Contains("ECONNREFUSED"))
            {
                Skip.If(true,
                    $"Minimal template app at {minUrl} is not reachable. " +
                    "Ensure the app is running before running template browser smoke tests.");
            }

            await Assertions.Expect(page).ToHaveURLAsync(
                new Regex(".*/login(\\?.*)?$"),
                new PageAssertionsToHaveURLOptions { Timeout = 10_000 });

            await Assertions.Expect(page.GetByTestId("login-page"))
                .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10_000 });

            await Assertions.Expect(page.GetByLabel("Email address")).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    private static void SkipIfNotConfigured()
    {
        Skip.If(string.IsNullOrWhiteSpace(TemplateAppUrl),
            "PLAYWRIGHT_TEMPLATE_APP_URL is not set. " +
            "See the class summary for setup instructions.");
    }
}
