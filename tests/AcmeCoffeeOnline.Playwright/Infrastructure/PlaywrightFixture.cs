using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Playwright;

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
    private readonly Dictionary<IPage, List<string>> _pageIssues = new();
    private readonly Dictionary<IPage, IBrowserContext> _pageContexts = new();

    public string BaseUrl { get; } = Environment.GetEnvironmentVariable("PLAYWRIGHT_BASE_URL")
        ?? "http://localhost:5010";

    public string ApiBaseUrl { get; } = Environment.GetEnvironmentVariable("PLAYWRIGHT_API_BASE_URL")
        ?? "http://localhost:5011";

    public async Task InitializeAsync()
    {
        if (!await IsReachableAsync(ApiBaseUrl))
        {
            _apiProcess = StartProject("src/Apps/AcmeCoffeeOnline.Api");
        }

        if (!await IsReachableAsync(BaseUrl))
        {
            _webProcess = StartProject("src/Apps/AcmeCoffeeOnline.Web");
        }

        await WaitForServerAsync($"{ApiBaseUrl}/health", "API");
        await WaitForServerAsync($"{BaseUrl}/health", "Web");

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
        var issues = new List<string>();
        _pageIssues[page] = issues;
        _pageContexts[page] = context;

        page.Console += (_, message) =>
        {
            if (message.Type == "error")
            {
                issues.Add($"Console error: {message.Text}");
            }
        };

        page.PageError += (_, exception) =>
        {
            issues.Add($"Page error: {exception}");
        };

        page.RequestFailed += (_, request) =>
        {
            if (ShouldIgnoreRequestFailure(request))
            {
                return;
            }

            issues.Add($"Request failed: {request.Method} {request.Url} ({request.Failure})");
        };

        page.Response += (_, response) =>
        {
            if (response.Status < 400 || ShouldIgnoreResponseIssue(response))
            {
                return;
            }

            issues.Add($"HTTP {response.Status}: {response.Request.Method} {response.Url}");
        };

        page.Close += (_, _) =>
        {
            _pageIssues.Remove(page);
            _pageContexts.Remove(page);
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

    public async Task ClearMagicLinksAsync(IPage page, string? email = null)
    {
        var url = $"{BaseUrl}/test-auth/magic-links?clear=true";
        if (!string.IsNullOrWhiteSpace(email))
        {
            url += $"&email={Uri.EscapeDataString(email)}";
        }

        var payload = await ReadJsonFromHelperPageAsync(page.Context, url);
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new InvalidOperationException("Failed to clear magic links.");
        }
    }

    public async Task<MagicLinkMessage> WaitForMagicLinkAsync(
        IPage page,
        string email,
        int timeoutMs = 15000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

        while (DateTime.UtcNow < deadline)
        {
            var payload = await ReadJsonFromHelperPageAsync(
                page.Context,
                $"{BaseUrl}/test-auth/magic-links?email={Uri.EscapeDataString(email)}");

            var messages = JsonSerializer.Deserialize<List<MagicLinkMessage>>(payload, JsonOptions)
                ?? [];

            var message = messages.FirstOrDefault(link => !string.IsNullOrWhiteSpace(link.LinkUrl));
            if (message is not null)
            {
                return message;
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

    public async Task<JsonDocument> GetApiJsonAsync(string relativeUrl, string userId)
    {
        return await SendApiJsonAsync(HttpMethod.Get, relativeUrl, userId);
    }

    public async Task<JsonDocument> PostApiJsonAsync(string relativeUrl, string userId, object payload)
    {
        return await SendApiJsonAsync(HttpMethod.Post, relativeUrl, userId, payload);
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
        if (_pageIssues.TryGetValue(page, out var issues) && issues.Count > 0)
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

    private static Process StartProject(string relativeProjectPath)
    {
        var repoRoot = GetRepoRoot();
        var projectDir = Path.Combine(repoRoot, relativeProjectPath);
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

        process.OutputDataReceived += (_, _) => { };
        process.ErrorDataReceived += (_, _) => { };
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

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

    private static async Task WaitForServerAsync(string url, string name)
    {
        using var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(2)
        };

        var deadline = DateTime.UtcNow.AddSeconds(60);
        while (DateTime.UtcNow < deadline)
        {
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

        throw new TimeoutException($"{name} did not become healthy at {url}.");
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
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
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
                WaitUntil = WaitUntilState.NetworkIdle
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
