using System.Text.Json;
using Microsoft.Playwright;

namespace ComponentCatalogCrawler;

public sealed record CrawlerOptions(
    string PagesDirectory,
    string BaseUrl,
    string ApiBaseUrl,
    string TestAuthSecret,
    string InternalApiKey,
    string OutputDirectory,
    bool Headless,
    string AppName,
    IReadOnlyList<string> Roles,
    string SeedEmail);

public sealed class CrawlerOrchestrator
{
    private static readonly JsonSerializerOptions ManifestJsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly CrawlerOptions _opts;

    public CrawlerOrchestrator(CrawlerOptions opts)
    {
        _opts = opts;
    }

    public async Task<int> RunAsync()
    {
        Directory.CreateDirectory(_opts.OutputDirectory);

        var enumerator = new RouteEnumerator(_opts.PagesDirectory, _opts.ApiBaseUrl, _opts.InternalApiKey);
        var auth = new SeededSessionAuth(_opts.BaseUrl, _opts.TestAuthSecret);
        var scraper = new AccessibilityTreeScraper();

        // Resolve route parameters via the system-admin user so the API
        // returns rows from every organization. Per-role authentication
        // happens during the actual crawl below.
        var seedUserIdForResolution = "1"; // SystemAdmin per src/Apps/AcmeCoffeeOnline.Web/user-roles.json
        var routes = await enumerator.EnumerateAsync(seedUserIdForResolution);
        Console.WriteLine($"[orchestrator] Resolved {routes.Count} routes:");
        foreach (var r in routes)
        {
            Console.WriteLine($"  - {r.TemplatePath} -> {r.ResolvedPath}");
        }

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = _opts.Headless,
        });

        var allElements = new List<CatalogElement>();

        foreach (var role in _opts.Roles)
        {
            Console.WriteLine($"[orchestrator] === Crawling as role={role} ===");
            var roleElements = new List<CatalogElement>();

            await using var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();

            try
            {
                await auth.SignInAsync(page, _opts.SeedEmail, role);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[orchestrator] WARN: sign-in failed for {role}: {ex.Message}. Skipping role.");
                continue;
            }

            foreach (var route in routes)
            {
                try
                {
                    var url = $"{_opts.BaseUrl.TrimEnd('/')}{route.ResolvedPath}";
                    var response = await page.GotoAsync(url, new PageGotoOptions
                    {
                        WaitUntil = WaitUntilState.NetworkIdle,
                        Timeout = 15000,
                    });

                    if (page.Url.Contains("/access-denied", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"[orchestrator] {role} access-denied for {route.TemplatePath}");
                        continue;
                    }

                    // Wait for the app shell to render before snapshotting.
                    var shell = page.GetByTestId("main-shell");
                    await Assertions.Expect(shell).ToBeVisibleAsync(new() { Timeout = 5000 });

                    var pageElements = await scraper.ScrapeAsync(
                        page,
                        RouteEnumerator.NormalizeTemplatePath(route.TemplatePath),
                        role);
                    roleElements.AddRange(pageElements);
                    Console.WriteLine($"[orchestrator] {role} {route.TemplatePath} -> {pageElements.Count} elements");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[orchestrator] WARN: {role} {route.TemplatePath} failed: {ex.Message}");
                }
            }

            // Per-role manifest.
            var perRolePath = Path.Combine(_opts.OutputDirectory, $"{role}.json");
            WriteManifest(perRolePath, roleElements);
            Console.WriteLine($"[orchestrator] Wrote {roleElements.Count} elements to {perRolePath}");
            allElements.AddRange(roleElements);
        }

        var indexPath = Path.Combine(_opts.OutputDirectory, "index.json");
        WriteManifest(indexPath, allElements);
        Console.WriteLine($"[orchestrator] Wrote merged manifest with {allElements.Count} elements to {indexPath}");

        return 0;
    }

    private void WriteManifest(string path, IReadOnlyList<CatalogElement> elements)
    {
        var manifest = new CatalogManifest(
            AppName: _opts.AppName,
            GeneratedAt: DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            SchemaVersion: "1.0",
            Entries: elements);

        File.WriteAllText(path, JsonSerializer.Serialize(manifest, ManifestJsonOptions));
    }
}
