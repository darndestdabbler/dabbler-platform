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

        var watermarkedRoutes = WatermarkDetector.ScanWatermarkedRoutes(_opts.PagesDirectory);
        Console.WriteLine($"[orchestrator] Watermarked routes detected: {string.Join(", ", watermarkedRoutes.OrderBy(r => r))}");

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

        // Pre-compute the set of route templates we already plan to crawl. The
        // popup-following pass uses this to skip popups whose destination is
        // already covered by the top-level loop (e.g., /dev/email-inbox's
        // "Open" link lands on /, which we crawl directly).
        var knownRoutePaths = routes
            .Select(r => RouteEnumerator.NormalizeTemplatePath(r.TemplatePath))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

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

            // Tracks popup destinations crawled during THIS role's loop so a
            // route reachable via popup from multiple top-level routes is only
            // scraped once per role (the resulting catalog rows would be
            // identical and the dedup pass below would discard them anyway).
            var visitedPopupRoutes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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

                    var normalizedPath = RouteEnumerator.NormalizeTemplatePath(route.TemplatePath);
                    var isWatermarked = watermarkedRoutes.Contains(normalizedPath);
                    var pageElements = await scraper.ScrapeAsync(
                        page,
                        normalizedPath,
                        role,
                        isWatermarked);
                    roleElements.AddRange(pageElements);
                    var watermarkLabel = isWatermarked ? " [watermarked]" : "";
                    Console.WriteLine($"[orchestrator] {role} {route.TemplatePath}{watermarkLabel} -> {pageElements.Count} elements");

                    // Popup-following pass: discover target="_blank" links whose
                    // destination is a route the top-level loop won't visit
                    // directly, then crawl those routes too. The DSL's
                    // FOLLOW LINK IN NEW TAB verb references labels on those
                    // popup-destination pages; without this pass those labels
                    // are absent from the catalog and authoring tools have
                    // nothing to validate against. We enumerate hrefs without
                    // clicking when the destination is already known, and only
                    // pay the popup-roundtrip when the destination is new.
                    var popupCount = await CrawlPopupDestinationsAsync(
                        page, browser, scraper, role, knownRoutePaths,
                        visitedPopupRoutes, watermarkedRoutes, roleElements);
                    if (popupCount > 0)
                    {
                        Console.WriteLine($"[orchestrator] {role} {route.TemplatePath} -> {popupCount} popup destinations crawled");
                    }
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
            SchemaVersion: "1.1",
            Entries: elements);

        File.WriteAllText(path, JsonSerializer.Serialize(manifest, ManifestJsonOptions));
    }

    /// <summary>
    /// Discovers target="_blank" anchors on the rendered page whose href
    /// resolves to a same-origin route the top-level crawl wouldn't otherwise
    /// visit, follows each via Playwright's RunAndWaitForPopupAsync to capture
    /// the popup IPage, and scrapes the popup. Mirrors what the DSL's
    /// FOLLOW LINK IN NEW TAB verb does at runtime so labels on popup
    /// destinations land in the catalog and the authoring tools (and W0
    /// validator) can see them.
    /// </summary>
    /// <remarks>
    /// We restrict to same-origin destinations so an external "help" link or
    /// docs anchor doesn't spawn a third-party crawl. Same-origin popups whose
    /// route template is already in <paramref name="knownRoutePaths"/> are
    /// skipped because the top-level loop already catalogued them. Popups
    /// whose route is new to this role are crawled and added to
    /// <paramref name="roleElements"/>; their route is recorded in
    /// <paramref name="visitedPopupRoutes"/> so subsequent top-level routes
    /// don't re-crawl them.
    /// </remarks>
    private async Task<int> CrawlPopupDestinationsAsync(
        IPage page,
        IBrowser browser,
        AccessibilityTreeScraper scraper,
        string role,
        IReadOnlySet<string> knownRoutePaths,
        HashSet<string> visitedPopupRoutes,
        IReadOnlySet<string> watermarkedRoutes,
        List<CatalogElement> roleElements)
    {
        // Pull all target="_blank" anchors with same-origin hrefs.
        var baseOrigin = new Uri(_opts.BaseUrl).GetLeftPart(UriPartial.Authority);
        var popupHrefsRaw = await page.EvaluateAsync<JsonElement>(
            """
            (origin) => {
              const out = [];
              for (const a of document.querySelectorAll('a[target="_blank"][href]')) {
                const href = a.href;
                if (!href || !href.startsWith(origin)) continue;
                const text = (a.textContent || '').trim().slice(0, 100);
                out.push({ href, text });
              }
              return out;
            }
            """,
            baseOrigin);

        if (popupHrefsRaw.ValueKind != JsonValueKind.Array || popupHrefsRaw.GetArrayLength() == 0)
        {
            return 0;
        }

        // Decide which hrefs are worth following: skip if the route template
        // is already in knownRoutePaths (top-level loop covers it) or if we
        // already followed it in this role's pass.
        var candidates = new List<(string Href, string Text, string RoutePath)>();
        foreach (var entry in popupHrefsRaw.EnumerateArray())
        {
            var href = entry.GetProperty("href").GetString() ?? string.Empty;
            var text = entry.GetProperty("text").GetString() ?? string.Empty;
            if (!Uri.TryCreate(href, UriKind.Absolute, out var uri))
            {
                continue;
            }
            var routePath = uri.AbsolutePath;
            if (string.IsNullOrEmpty(routePath))
            {
                continue;
            }
            if (knownRoutePaths.Contains(routePath))
            {
                continue;
            }
            if (!visitedPopupRoutes.Add(routePath))
            {
                continue;
            }
            candidates.Add((href, text, routePath));
        }

        if (candidates.Count == 0)
        {
            return 0;
        }

        var crawled = 0;
        foreach (var c in candidates)
        {
            try
            {
                // Click the link (matched by visible text + target=_blank attr)
                // inside RunAndWaitForPopupAsync to capture the resulting IPage.
                // We re-find the anchor at this moment because Blazor may have
                // re-rendered between scrape and follow.
                var linkLocator = page.Locator($"a[target='_blank'][href='{c.Href.Replace("'", "\\'")}']").First;
                var popup = await page.RunAndWaitForPopupAsync(async () =>
                {
                    await linkLocator.ClickAsync(new() { Timeout = 5000 });
                });

                // Wait for the popup to settle. Most popup landings reach
                // NetworkIdle quickly; a 10s budget is generous for a dev
                // surface and bounded for the crawl.
                await popup.WaitForLoadStateAsync(LoadState.NetworkIdle, new()
                {
                    Timeout = 10000,
                });

                // Scrape under the popup's actual route. Watermark detection
                // uses the same rule as top-level: only pages whose top-level
                // component inherits TrackedComponentBase are watermarked.
                var isWatermarked = watermarkedRoutes.Contains(c.RoutePath);
                var popupElements = await scraper.ScrapeAsync(popup, c.RoutePath, role, isWatermarked);
                roleElements.AddRange(popupElements);
                Console.WriteLine($"[orchestrator]   popup {c.RoutePath} (text=\"{c.Text}\") -> {popupElements.Count} elements");
                crawled++;

                await popup.CloseAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[orchestrator]   WARN popup follow failed for {c.RoutePath}: {ex.Message}");
            }
        }
        return crawled;
    }
}
