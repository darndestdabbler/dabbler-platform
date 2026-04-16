using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Text.Json;
using Microsoft.Playwright;
using Xunit;

namespace AcmeCoffeeOnline.Playwright.Infrastructure;

public sealed class PlaywrightFixture : IAsyncLifetime
{
    public const string TestAuthSecret = "acme-local-seeded-session";
    private const string InternalApiKey = "dev-internal-key-acme";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private Process? _apiProcess;
    private Process? _webProcess;
    private readonly ConcurrentQueue<string> _apiProcessLog = new();
    private readonly ConcurrentQueue<string> _webProcessLog = new();
    private readonly ConcurrentDictionary<IPage, ConcurrentQueue<string>> _pageIssues = new();
    private readonly ConcurrentDictionary<IPage, IBrowserContext> _pageContexts = new();

    public string BaseUrl { get; } = Environment.GetEnvironmentVariable("PLAYWRIGHT_BASE_URL")
        ?? "http://localhost:5010";

    public string ApiBaseUrl { get; } = Environment.GetEnvironmentVariable("PLAYWRIGHT_API_BASE_URL")
        ?? "http://localhost:5011";

    public async Task InitializeAsync()
    {
        if (!await IsReachableAsync($"{ApiBaseUrl}/health"))
        {
            _apiProcess = StartProject("src/Apps/AcmeCoffeeOnline.Api", _apiProcessLog);
        }

        if (!await IsReachableAsync($"{BaseUrl}/health"))
        {
            _webProcess = StartProject("src/Apps/AcmeCoffeeOnline.Web", _webProcessLog);
        }

        await WaitForServerAsync($"{ApiBaseUrl}/health", "API", _apiProcess, _apiProcessLog);
        await WaitForServerAsync($"{BaseUrl}/health", "Web", _webProcess, _webProcessLog);

        _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
    }

    public async Task<IPage> CreatePageAsync()
    {
        if (_browser is null)
        {
            throw new InvalidOperationException("Playwright browser is not initialized.");
        }

        var context = await _browser.NewContextAsync();
        var page = await context.NewPageAsync();
        var issues = new ConcurrentQueue<string>();
        _pageIssues[page] = issues;
        _pageContexts[page] = context;

        page.Console += (_, message) =>
        {
            if (message.Type == "error")
            {
                issues.Enqueue($"Console error: {message.Text}");
            }
        };

        page.PageError += (_, exception) =>
        {
            issues.Enqueue($"Page error: {exception}");
        };

        page.RequestFailed += (_, request) =>
        {
            if (ShouldIgnoreRequestFailure(request))
            {
                return;
            }

            issues.Enqueue($"Request failed: {request.Method} {request.Url} ({request.Failure})");
        };

        page.Response += (_, response) =>
        {
            if (response.Status < 400 || ShouldIgnoreResponseIssue(response))
            {
                return;
            }

            issues.Enqueue($"HTTP {response.Status}: {response.Request.Method} {response.Url}");
        };

        page.Close += (_, _) =>
        {
            _pageIssues.TryRemove(page, out _);
            _pageContexts.TryRemove(page, out _);
        };

        return page;
    }

    public async Task ClosePageAsync(IPage page)
    {
        await AssertNoClientErrorsAsync(page);
        if (_pageContexts.TryGetValue(page, out var context))
        {
            await context.CloseAsync();
        }
        else
        {
            await page.CloseAsync();
        }
    }

    public async Task SeededSignInAsync(
        IPage page,
        string email,
        string? roles = null,
        string? displayName = null)
    {
        await page.SetExtraHTTPHeadersAsync(BuildTestHeaders());

        var url = $"{BaseUrl}/test-auth/seeded-session?email={Uri.EscapeDataString(email)}";
        if (!string.IsNullOrWhiteSpace(roles))
        {
            url += $"&roles={Uri.EscapeDataString(roles)}";
        }

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            url += $"&displayName={Uri.EscapeDataString(displayName)}";
        }

        await page.GotoAsync(url, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });

        await page.SetExtraHTTPHeadersAsync(new Dictionary<string, string>());
    }

    public async Task SignOutAsync(IPage page)
    {
        await page.SetExtraHTTPHeadersAsync(BuildTestHeaders());
        await page.GotoAsync($"{BaseUrl}/test-auth/sign-out", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });
        await page.SetExtraHTTPHeadersAsync(new Dictionary<string, string>());
    }

    public async Task ClearMagicLinksAsync(string? email = null)
    {
        var url = $"{BaseUrl}/test-auth/magic-links?clear=true";
        if (!string.IsNullOrWhiteSpace(email))
        {
            url += $"&email={Uri.EscapeDataString(email)}";
        }

        using var client = CreateWebHelperClient();
        var payload = await client.GetStringAsync(url);
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new InvalidOperationException("Failed to clear magic links.");
        }
    }

    public async Task<MagicLinkMessage> WaitForMagicLinkAsync(
        string email,
        int timeoutMs = 15000)
    {
        using var client = CreateWebHelperClient();
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        var url = $"{BaseUrl}/test-auth/magic-links?email={Uri.EscapeDataString(email)}";

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var payload = await client.GetStringAsync(url);
                var messages = JsonSerializer.Deserialize<List<MagicLinkMessage>>(payload, JsonOptions)
                    ?? [];

                var message = messages.FirstOrDefault(link => !string.IsNullOrWhiteSpace(link.LinkUrl));
                if (message is not null)
                {
                    return message;
                }
            }
            catch (HttpRequestException)
            {
            }
            catch (TaskCanceledException)
            {
            }
            catch (JsonException)
            {
            }

            await Task.Delay(250);
        }

        throw new TimeoutException($"Timed out waiting for a magic link for {email}.");
    }

    public async Task<SessionInfo> GetSessionInfoAsync(IPage page)
    {
        var payload = await ReadJsonFromHelperPageAsync(page.Context, $"{BaseUrl}/test-auth/session-info");
        return JsonSerializer.Deserialize<SessionInfo>(payload, JsonOptions)
            ?? throw new InvalidOperationException("Session info payload was empty.");
    }

    public async Task NavigateToAdminProductsAsync(IPage page)
    {
        await page.GotoAsync($"{BaseUrl}/admin/products", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });

        var routedContent = page.GetByTestId("routed-page-content");

        await Assertions.Expect(page)
            .ToHaveURLAsync(new Regex(".*/admin/products$"));
        await Assertions.Expect(page.GetByTestId("access-denied-page"))
            .ToHaveCountAsync(0);
        await Assertions.Expect(routedContent.GetByRole(AriaRole.Heading, new() { Name = "Product Administration" }))
            .ToBeVisibleAsync();
    }

    public async Task<ProductSnapshot> GetProductByExactNameAsync(string userId, string productName)
    {
        using var payload = await GetApiJsonAsync(
            $"api/products?page=1&pageSize=100&searchTerm={Uri.EscapeDataString(productName)}",
            userId);

        var product = payload.RootElement
            .GetProperty("items")
            .EnumerateArray()
            .FirstOrDefault(item => string.Equals(
                item.GetProperty("name").GetString(),
                productName,
                StringComparison.Ordinal));

        if (product.ValueKind == JsonValueKind.Undefined)
        {
            throw new Xunit.Sdk.XunitException($"Could not find product '{productName}' through the API.");
        }

        return MapProduct(product);
    }

    public async Task<ProductSnapshot?> TryGetProductByExactNameAsync(string userId, string productName)
    {
        using var payload = await GetApiJsonAsync(
            $"api/products?page=1&pageSize=100&searchTerm={Uri.EscapeDataString(productName)}",
            userId);

        var product = payload.RootElement
            .GetProperty("items")
            .EnumerateArray()
            .FirstOrDefault(item => string.Equals(
                item.GetProperty("name").GetString(),
                productName,
                StringComparison.Ordinal));

        return product.ValueKind == JsonValueKind.Undefined ? null : MapProduct(product);
    }

    public async Task<ProductSnapshot> GetProductByIdAsync(string userId, int productId)
    {
        using var payload = await GetApiJsonAsync($"api/products/{productId}", userId);
        return MapProduct(payload.RootElement);
    }

    public async Task<ProductSnapshot> UpdateProductAsync(string userId, ProductSnapshot product)
    {
        using var payload = await PutApiJsonAsync(
            $"api/products/{product.Id}",
            userId,
            new
            {
                id = product.Id,
                name = product.Name,
                roastLevel = product.RoastLevel,
                caffeineContent = product.CaffeineContent,
                grindType = product.GrindType,
                unitPrice = product.UnitPrice,
                isAvailable = product.IsAvailable,
                description = product.Description,
                imageUrl = product.ImageUrl
            });

        return MapProduct(payload.RootElement);
    }

    public async Task RestoreProductAvailabilityAsync(string userId, int productId, bool expectedAvailability)
    {
        var current = await GetProductByIdAsync(userId, productId);
        if (current.IsAvailable != expectedAvailability)
        {
            await PatchApiAsync($"api/products/{productId}/availability", userId);
        }
    }

    public async Task<ProductSnapshot?> ArchiveProductByIdAsync(
        string userId,
        int productId,
        string archivedNamePrefix)
    {
        ProductSnapshot current;
        try
        {
            current = await GetProductByIdAsync(userId, productId);
        }
        catch (HttpRequestException)
        {
            return null;
        }

        var archived = current with
        {
            Name = $"{archivedNamePrefix}-{current.Id}",
            IsAvailable = false
        };

        return await UpdateProductAsync(userId, archived);
    }

    public async Task<JsonDocument> GetApiJsonAsync(string relativeUrl, string userId)
    {
        return await SendApiJsonAsync(HttpMethod.Get, relativeUrl, userId);
    }

    public async Task<JsonDocument> PostApiJsonAsync(string relativeUrl, string userId, object payload)
    {
        return await SendApiJsonAsync(HttpMethod.Post, relativeUrl, userId, payload);
    }

    public async Task<JsonDocument> PutApiJsonAsync(string relativeUrl, string userId, object payload)
    {
        return await SendApiJsonAsync(HttpMethod.Put, relativeUrl, userId, payload);
    }

    public async Task PatchApiAsync(string relativeUrl, string userId)
    {
        using var client = new HttpClient
        {
            BaseAddress = new Uri(ApiBaseUrl),
            Timeout = TimeSpan.FromSeconds(10)
        };

        client.DefaultRequestHeaders.Add("X-Internal-Api-Key", InternalApiKey);
        client.DefaultRequestHeaders.Add("X-App-User-Id", userId);

        using var request = new HttpRequestMessage(HttpMethod.Patch, relativeUrl);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public Task AssertNoClientErrorsAsync(IPage page)
    {
        if (_pageIssues.TryGetValue(page, out var issues) && !issues.IsEmpty)
        {
            var details = string.Join(Environment.NewLine, issues);
            throw new Xunit.Sdk.XunitException($"Browser-side failures were detected:{Environment.NewLine}{details}");
        }

        return Task.CompletedTask;
    }

    private async Task<JsonDocument> SendApiJsonAsync(
        HttpMethod method,
        string relativeUrl,
        string userId,
        object? payload = null)
    {
        using var client = new HttpClient
        {
            BaseAddress = new Uri(ApiBaseUrl),
            Timeout = TimeSpan.FromSeconds(10)
        };

        client.DefaultRequestHeaders.Add("X-Internal-Api-Key", InternalApiKey);
        client.DefaultRequestHeaders.Add("X-App-User-Id", userId);

        using var request = new HttpRequestMessage(method, relativeUrl);
        if (payload is not null)
        {
            request.Content = JsonContent.Create(payload);
        }

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }

    public async Task DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
        StopProcess(_webProcess);
        StopProcess(_apiProcess);
    }

    private static Process StartProject(string relativeProjectPath, ConcurrentQueue<string> output)
    {
        var repoRoot = GetRepoRoot();
        var projectDir = Path.Combine(repoRoot, relativeProjectPath);
        var projectName = Path.GetFileName(projectDir);
        var binDir = Path.Combine(projectDir, "bin");
        if (!Directory.Exists(binDir)
            || !Directory.EnumerateFiles(binDir, $"{projectName}.dll", SearchOption.AllDirectories).Any())
        {
            throw new InvalidOperationException(
                $"No build output was found for {relativeProjectPath}. Run 'dotnet build Dabbler.Platform.sln' before starting Playwright infrastructure.");
        }

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "run --no-build --launch-profile http",
                WorkingDirectory = projectDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        process.StartInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        process.StartInfo.Environment["DOTNET_ENVIRONMENT"] = "Development";

        process.OutputDataReceived += (_, args) => CaptureProcessOutput(output, args.Data);
        process.ErrorDataReceived += (_, args) => CaptureProcessOutput(output, args.Data);
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (process.WaitForExit(2000))
        {
            var details = BuildProcessDiagnostics(output);
            throw new InvalidOperationException(
                $"The {relativeProjectPath} process exited immediately with code {process.ExitCode}.{details}");
        }

        return process;
    }

    private static async Task<bool> IsReachableAsync(string url)
    {
        try
        {
            using var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(2)
            };

            using var response = await client.GetAsync(url);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static async Task WaitForServerAsync(
        string url,
        string name,
        Process? process,
        ConcurrentQueue<string> output)
    {
        using var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(2)
        };

        var deadline = DateTime.UtcNow.AddSeconds(60);
        while (DateTime.UtcNow < deadline)
        {
            if (process is not null && process.HasExited)
            {
                var details = BuildProcessDiagnostics(output);
                throw new TimeoutException(
                    $"{name} exited before becoming healthy at {url} (exit code {process.ExitCode}).{details}");
            }

            try
            {
                using var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch
            {
            }

            await Task.Delay(500);
        }

        throw new TimeoutException($"{name} did not become healthy at {url}.{BuildProcessDiagnostics(output)}");
    }

    private static void StopProcess(Process? process)
    {
        if (process is null || process.HasExited)
        {
            return;
        }

        process.Kill(entireProcessTree: true);
        process.WaitForExit(5000);
        process.Dispose();
    }

    private static string GetRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (directory.EnumerateFiles("Dabbler.Platform.sln").Any())
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root from the current test output path.");
    }

    private static Dictionary<string, string> BuildTestHeaders() => new()
    {
        ["X-Test-Auth-Secret"] = TestAuthSecret
    };

    private static async Task<string> ReadJsonFromHelperPageAsync(IBrowserContext context, string url)
    {
        var helperPage = await context.NewPageAsync();
        try
        {
            await helperPage.SetExtraHTTPHeadersAsync(BuildTestHeaders());
            await helperPage.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 5000
            });

            return await helperPage.TextContentAsync("body") ?? string.Empty;
        }
        finally
        {
            await helperPage.CloseAsync();
        }
    }

    private static bool ShouldIgnoreResponseIssue(IResponse response)
    {
        var url = response.Url;

        if (url.Contains("/test-auth/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (url.Contains("/access-denied", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (url.Contains("/_blazor/disconnect", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static bool ShouldIgnoreRequestFailure(IRequest request)
    {
        return request.Url.Contains("/_blazor/disconnect", StringComparison.OrdinalIgnoreCase);
    }

    private static HttpClient CreateWebHelperClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };
        client.DefaultRequestHeaders.Add("X-Test-Auth-Secret", TestAuthSecret);
        return client;
    }

    private static void CaptureProcessOutput(ConcurrentQueue<string> output, string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        output.Enqueue(line);
        while (output.Count > 40 && output.TryDequeue(out _))
        {
        }
    }

    private static string BuildProcessDiagnostics(ConcurrentQueue<string> output)
    {
        if (output.IsEmpty)
        {
            return string.Empty;
        }

        var lines = output.ToArray();
        return $"{Environment.NewLine}Recent process output:{Environment.NewLine}{string.Join(Environment.NewLine, lines)}";
    }

    private static ProductSnapshot MapProduct(JsonElement element) => new(
        element.GetProperty("id").GetInt32(),
        element.GetProperty("name").GetString() ?? string.Empty,
        element.GetProperty("roastLevel").GetInt32(),
        element.GetProperty("caffeineContent").GetInt32(),
        element.GetProperty("grindType").GetInt32(),
        element.GetProperty("unitPrice").GetDecimal(),
        element.GetProperty("isAvailable").GetBoolean(),
        element.TryGetProperty("description", out var description) && description.ValueKind != JsonValueKind.Null
            ? description.GetString()
            : null,
        element.TryGetProperty("imageUrl", out var imageUrl) && imageUrl.ValueKind != JsonValueKind.Null
            ? imageUrl.GetString()
            : null);
}

public sealed record MagicLinkMessage(
    string EmailType,
    string Email,
    string? Code,
    string? LinkUrl,
    string? QrCodeDataUri,
    DateTimeOffset CreatedAtUtc);

public sealed record SessionInfo(
    bool IsAuthenticated,
    string? UserId,
    string? Name,
    string? Email,
    string? CustomerId,
    string[]? Roles);

public sealed record ProductSnapshot(
    int Id,
    string Name,
    int RoastLevel,
    int CaffeineContent,
    int GrindType,
    decimal UnitPrice,
    bool IsAvailable,
    string? Description,
    string? ImageUrl);
