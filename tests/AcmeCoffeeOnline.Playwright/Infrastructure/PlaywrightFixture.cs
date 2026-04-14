using System.Diagnostics;
using System.Net.Http;
using Microsoft.Playwright;

namespace AcmeCoffeeOnline.Playwright.Infrastructure;

public sealed class PlaywrightFixture : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private Process? _apiProcess;
    private Process? _webProcess;

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

        return await _browser.NewPageAsync();
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
}
