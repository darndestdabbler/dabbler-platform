using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ComponentCatalogCrawler;

/// <summary>
/// Resolves the set of routes the crawler will visit. Static-scans the
/// AcmeCoffeeOnline.Web Razor pages for <c>@page "..."</c> directives,
/// then resolves parameterised segments (e.g. <c>{CustomerId:int}</c>)
/// against seeded entity IDs from the API.
/// </summary>
public sealed class RouteEnumerator
{
    private static readonly Regex PageDirectiveRegex = new(
        """^\s*@page\s+"(?<route>[^"]+)"\s*$""",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex RouteParamRegex = new(
        @"\{(?<name>[^:}]+)(:[^}]+)?\}",
        RegexOptions.Compiled);

    // Routes a crawl-as-authenticated-user pass should not navigate to. Auth
    // entry points and framework error pages are handled by other tests.
    private static readonly HashSet<string> SkipRoutes = new(StringComparer.OrdinalIgnoreCase)
    {
        "/Error",
        "/login",
        "/register",
        "/account/mfa",
    };

    private readonly string _pagesDirectory;
    private readonly HttpClient _apiClient;
    private readonly string _internalApiKey;

    public RouteEnumerator(string pagesDirectory, string apiBaseUrl, string internalApiKey)
    {
        _pagesDirectory = pagesDirectory;
        _apiClient = new HttpClient
        {
            BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(10)
        };
        _internalApiKey = internalApiKey;
    }

    public async Task<IReadOnlyList<ResolvedRoute>> EnumerateAsync(string seedUserId)
    {
        var raw = ScanPageDirectives();
        var resolved = new List<ResolvedRoute>();

        foreach (var template in raw.OrderBy(r => r, StringComparer.Ordinal))
        {
            if (SkipRoutes.Contains(template))
            {
                continue;
            }

            var resolvedPath = await ResolveTemplateAsync(template, seedUserId);
            if (resolvedPath is null)
            {
                Console.WriteLine($"[route-enumerator] WARN: skipping {template} — could not resolve parameters");
                continue;
            }

            resolved.Add(new ResolvedRoute(template, resolvedPath));
        }

        return resolved;
    }

    private IReadOnlyList<string> ScanPageDirectives()
    {
        if (!Directory.Exists(_pagesDirectory))
        {
            throw new DirectoryNotFoundException(
                $"Razor pages directory not found at '{_pagesDirectory}'. Run the crawler from the repository root.");
        }

        var routes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in Directory.EnumerateFiles(_pagesDirectory, "*.razor", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            foreach (Match match in PageDirectiveRegex.Matches(text))
            {
                routes.Add(match.Groups["route"].Value);
            }
        }
        return routes.ToList();
    }

    private async Task<string?> ResolveTemplateAsync(string template, string seedUserId)
    {
        var matches = RouteParamRegex.Matches(template);
        if (matches.Count == 0)
        {
            return template;
        }

        var resolved = template;
        foreach (Match match in matches)
        {
            var paramName = match.Groups["name"].Value;
            var seededId = await ResolveSeededIdAsync(paramName, seedUserId);
            if (seededId is null)
            {
                return null;
            }
            resolved = resolved.Replace(match.Value, seededId);
        }
        return resolved;
    }

    /// <summary>
    /// Strips Blazor route-constraint suffixes (e.g. <c>{CustomerId:int}</c>
    /// → <c>{CustomerId}</c>) so the catalog's <c>routePath</c> uses the
    /// portable template form rather than the .NET-specific constraint
    /// syntax.
    /// </summary>
    public static string NormalizeTemplatePath(string template)
        => RouteParamRegex.Replace(template, m => "{" + m.Groups["name"].Value + "}");

    private async Task<string?> ResolveSeededIdAsync(string paramName, string seedUserId)
    {
        // Maps known route parameter names to their API list endpoint.
        // Each entry's resolver returns the first seeded entity's id.
        return paramName switch
        {
            "OrderId" => await GetFirstIdAsync("api/orders?page=1&pageSize=1", seedUserId),
            "CustomerId" => await GetFirstIdAsync("api/customers?page=1&pageSize=1", seedUserId),
            "ProductId" => await GetFirstIdAsync("api/products?page=1&pageSize=1", seedUserId),
            _ => null
        };
    }

    private async Task<string?> GetFirstIdAsync(string relativeUrl, string seedUserId)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
            request.Headers.Add("X-Internal-Api-Key", _internalApiKey);
            request.Headers.Add("X-App-User-Id", seedUserId);
            using var response = await _apiClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            if (!doc.RootElement.TryGetProperty("items", out var items)
                || items.ValueKind != JsonValueKind.Array
                || items.GetArrayLength() == 0)
            {
                return null;
            }
            var first = items[0];
            return first.TryGetProperty("id", out var id)
                ? id.GetInt32().ToString()
                : null;
        }
        catch
        {
            return null;
        }
    }
}

public sealed record ResolvedRoute(string TemplatePath, string ResolvedPath);
