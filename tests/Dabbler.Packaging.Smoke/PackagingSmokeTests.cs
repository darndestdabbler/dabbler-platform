using FluentAssertions;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using Xunit;

namespace Dabbler.Packaging.Smoke;

/// <summary>
/// Consumer smoke tests for the packaging and template paths (ledger §6.1 / A2).
///
/// Tests are skipped when pack-output/ does not exist. Run tools/pack.ps1 first.
/// Set DABBLER_PACK_OUTPUT env var to override the default pack-output path.
/// </summary>
public class PackagingSmokeTests
{
    private static readonly string PackOutputDir = GetPackOutputDir();
    private static readonly TimeSpan DefaultToolTimeout = TimeSpan.FromMinutes(5);

    private static string GetPackOutputDir()
    {
        var fromEnv = Environment.GetEnvironmentVariable("DABBLER_PACK_OUTPUT");
        if (!string.IsNullOrWhiteSpace(fromEnv) && Directory.Exists(fromEnv))
            return fromEnv;

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "pack-output");
            if (Directory.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        return Path.Combine(AppContext.BaseDirectory, "pack-output");
    }

    [SkippableFact]
    public void PackOutput_ContainsAllRequiredLibraryNupkgs()
    {
        Skip.If(!Directory.Exists(PackOutputDir),
            "Pack output directory not found. Run tools/pack.ps1 first.");

        // Use a digit guard after the packageId prefix so Dabbler.Components.Compat from
        // satisfying a lookup for Dabbler.Components (and similar sub-package ambiguities).
        var nupkgs = Directory.GetFiles(PackOutputDir, "*.nupkg")
            .Select(Path.GetFileName)
            .OfType<string>()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var requiredIds = new[]
        {
            "Dabbler.Api.Querying",
            "Dabbler.Components",
            "Dabbler.Components.Compat",
            "Dabbler.Platform.Stubs",
            "Dabbler.Platform.Stubs.Components",
            "Dabbler.Platform.Stubs.Reports",
            "Dabbler.Platform.Stubs.Security",
            "Dabbler.Reports.Core",
            "Dabbler.Reports.Export",
            "Dabbler.Reports.Blazor",
            "Dabbler.Security",
            "Dabbler.Security.Blazor",
            "Dabbler.Testing.Blazor",
            // Session 8 deliverables
            "Dabbler.Security.DevIdp.Tool",
            "Dabbler.Templates",
        };

        static bool HasExactPackage(ISet<string> nupkgs, string packageId)
        {
            var prefix = packageId + ".";
            return nupkgs.Any(n =>
                n.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                n.Length > prefix.Length &&
                char.IsDigit(n[prefix.Length]));
        }

        var missing = requiredIds
            .Where(id => !HasExactPackage(nupkgs, id))
            .ToList();

        missing.Should().BeEmpty(
            $"All library packages must be present in pack-output/. Missing package IDs: {string.Join(", ", missing)}");
    }

    [SkippableFact]
    public void DabblerApp_Template_InstallsAndBuildsClean()
    {
        Skip.If(!Directory.Exists(PackOutputDir),
            "Pack output not found. Run tools/pack.ps1 first.");

        var templateNupkg = FindExactNupkg(PackOutputDir, "Dabbler.Templates");
        Skip.If(templateNupkg is null,
            "Dabbler.Templates nupkg not found in pack-output/. Run tools/pack.ps1 first.");

        var tempDir = Path.Combine(Path.GetTempPath(), $"dabbler-smoke-app-{Guid.NewGuid():N}");
        // Isolated dotnet new template hive — prevents mutation of the developer's global hive.
        var hiveDir = Path.Combine(Path.GetTempPath(), $"dabbler-hive-app-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var (installExit, installOut) = RunTool("dotnet",
                $"new install \"{templateNupkg}\" --debug:custom-hive \"{hiveDir}\"");
            installExit.Should().Be(0, $"template install should succeed. Output:\n{installOut}");

            // --name DabblerApp preserves the sourceName so solution and project
            // names stay consistent inside src/ and tests/.
            var (scaffoldExit, scaffoldOut) = RunTool("dotnet",
                $"new dabbler-app -o \"{tempDir}\" --name DabblerApp --force --debug:custom-hive \"{hiveDir}\"");
            scaffoldExit.Should().Be(0, $"dabbler-app scaffold should succeed. Output:\n{scaffoldOut}");

            AssertDabblerAppSplitTopology(tempDir, "DabblerApp");

            var projPath = Path.Combine(tempDir, "DabblerApp.slnx");

            // Restore explicitly with the local feed so Dabbler.* packages resolve
            var (restoreExit, restoreOut) = RunTool("dotnet",
                $"restore \"{projPath}\" --source \"{PackOutputDir}\" --source \"https://api.nuget.org/v3/index.json\"");
            restoreExit.Should().Be(0, $"restore should succeed. Output:\n{restoreOut}");

            var (buildExit, buildOut) = RunTool("dotnet",
                $"build \"{projPath}\" --no-restore --nologo -v q");
            buildExit.Should().Be(0, $"scaffolded dabbler-app should build clean. Output:\n{buildOut}");
        }
        finally
        {
            TryDeleteDirectory(tempDir);
            TryDeleteDirectory(hiveDir);
        }
    }

    [SkippableFact]
    public void DabblerMin_Template_InstallsAndBuildsClean()
    {
        Skip.If(!Directory.Exists(PackOutputDir),
            "Pack output not found. Run tools/pack.ps1 first.");

        var templateNupkg = FindExactNupkg(PackOutputDir, "Dabbler.Templates");
        Skip.If(templateNupkg is null,
            "Dabbler.Templates nupkg not found in pack-output/. Run tools/pack.ps1 first.");

        var tempDir = Path.Combine(Path.GetTempPath(), $"dabbler-smoke-min-{Guid.NewGuid():N}");
        var hiveDir = Path.Combine(Path.GetTempPath(), $"dabbler-hive-min-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var (installExit, installOut) = RunTool("dotnet",
                $"new install \"{templateNupkg}\" --debug:custom-hive \"{hiveDir}\"");
            installExit.Should().Be(0, $"template install should succeed. Output:\n{installOut}");

            var (scaffoldExit, scaffoldOut) = RunTool("dotnet",
                $"new dabbler-min -o \"{tempDir}\" --name DabblerApp --force --debug:custom-hive \"{hiveDir}\"");
            scaffoldExit.Should().Be(0, $"dabbler-min scaffold should succeed. Output:\n{scaffoldOut}");

            var projPath = Path.Combine(tempDir, "DabblerApp", "DabblerApp.csproj");

            var (restoreExit, restoreOut) = RunTool("dotnet",
                $"restore \"{projPath}\" --source \"{PackOutputDir}\" --source \"https://api.nuget.org/v3/index.json\"");
            restoreExit.Should().Be(0, $"restore should succeed. Output:\n{restoreOut}");

            var (buildExit, buildOut) = RunTool("dotnet",
                $"build \"{projPath}\" --no-restore --nologo -v q");
            buildExit.Should().Be(0, $"scaffolded dabbler-min should build clean. Output:\n{buildOut}");
        }
        finally
        {
            TryDeleteDirectory(tempDir);
            TryDeleteDirectory(hiveDir);
        }
    }

    [SkippableFact]
    public void DabblerApp_ScaffoldedBUnit_AllTestsPass()
    {
        Skip.If(!Directory.Exists(PackOutputDir),
            "Pack output not found. Run tools/pack.ps1 first.");

        var templateNupkg = FindExactNupkg(PackOutputDir, "Dabbler.Templates");
        Skip.If(templateNupkg is null,
            "Dabbler.Templates nupkg not found in pack-output/. Run tools/pack.ps1 first.");

        var tempDir = Path.Combine(Path.GetTempPath(), $"dabbler-smoke-bunit-{Guid.NewGuid():N}");
        var hiveDir = Path.Combine(Path.GetTempPath(), $"dabbler-hive-bunit-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var (installExit, installOut) = RunTool("dotnet",
                $"new install \"{templateNupkg}\" --debug:custom-hive \"{hiveDir}\"");
            installExit.Should().Be(0, $"template install should succeed. Output:\n{installOut}");

            var (scaffoldExit, scaffoldOut) = RunTool("dotnet",
                $"new dabbler-app -o \"{tempDir}\" --name DabblerApp --force --debug:custom-hive \"{hiveDir}\"");
            scaffoldExit.Should().Be(0, $"dabbler-app scaffold should succeed. Output:\n{scaffoldOut}");

            AssertDabblerAppSplitTopology(tempDir, "DabblerApp");

            var testProjPath = Path.Combine(tempDir, "DabblerApp.slnx");

            // Write nuget.config at the solution root so auto-restore finds local Dabbler.* packages.
            WriteLocalNugetConfig(tempDir, PackOutputDir);

            var (testExit, testOut) = RunTool("dotnet",
                $"test \"{testProjPath}\" --nologo -v q");
            testExit.Should().Be(0, $"scaffolded dabbler-app solution tests should pass. Output:\n{testOut}");
            testOut.Should().ContainEquivalentOf("passed",
                because: "at least one bUnit test must have run and passed");
        }
        finally
        {
            TryDeleteDirectory(tempDir);
            TryDeleteDirectory(hiveDir);
        }
    }

    // Session 10 covers the running-app tests via Playwright E2E:

    [Fact(Skip = "Session 10: sign-in via magic link requires running app docker stack; covered by Playwright E2E in Session 10")]
    public void DabblerApp_SignIn_ReachesSecuredPage() { }

    [Fact(Skip = "Session 10: sample report PDF export requires running app docker stack; covered by Playwright E2E in Session 10")]
    public void DabblerApp_SampleReport_PdfExportSucceeds() { }

    /// <summary>
    /// Regression gate for B1 (packaging-and-template-readiness-uat-remediation Session 1):
    /// the scaffolded dabbler-app host must respond with a non-5xx status on the first
    /// HTTP request to the root path.
    ///
    /// Root cause: security-config.json shipped with <c>"BakedInRoleAliasOverrides": []</c>
    /// (JSON array) while the runtime deserializes the property as
    /// <c>Dictionary&lt;string, string&gt;</c> (JSON object). On the first request the
    /// authorization handler calls <c>SecurityConfigurationManager.EnsureLoadedAsync()</c>,
    /// which calls <c>JsonFileSecurityStore.LoadAsync()</c>, which throws
    /// <c>JsonException</c> — producing an HTTP 500 before the login page can render.
    ///
    /// This test is intentionally <c>[Fact]</c>, not <c>[SkippableFact]</c>, because the
    /// existing SkippableFacts silently skipped in CI (pack-output/ was absent) and allowed
    /// the B1 regression to reach human UAT. CI must run <c>tools/pack.ps1</c> before
    /// <c>dotnet test</c> for this gate to fire.
    ///
    /// Requires: <c>pack-output/</c> (run <c>tools/pack.ps1</c> first) and SQL Server
    /// LocalDB (standard on Windows development machines).
    /// </summary>
    [Fact]
    public async Task DabblerApp_ScaffoldedHost_ServesBelowFiveHundredOnRootRequest()
    {
        Assert.True(Directory.Exists(PackOutputDir),
            $"Pack output directory not found at '{PackOutputDir}'. Run tools/pack.ps1 first. " +
            "This test is a required regression gate for B1 (security-config.json deserialization fix) " +
            "and must not be silently skipped.");

        var templateNupkg = FindExactNupkg(PackOutputDir, "Dabbler.Templates");
        Assert.True(templateNupkg is not null,
            "Dabbler.Templates nupkg not found in pack-output/. Run tools/pack.ps1 first.");

        var tempDir = Path.Combine(Path.GetTempPath(), $"dabbler-runtime-smoke-{Guid.NewGuid():N}");
        var hiveDir = Path.Combine(Path.GetTempPath(), $"dabbler-hive-runtime-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        Process? hostProcess = null;
        try
        {
            var (installExit, installOut) = RunTool("dotnet",
                $"new install \"{templateNupkg}\" --debug:custom-hive \"{hiveDir}\"");
            Assert.True(installExit == 0, $"Template install failed:\n{installOut}");

            var (scaffoldExit, scaffoldOut) = RunTool("dotnet",
                $"new dabbler-app -o \"{tempDir}\" --name DabblerApp --force --debug:custom-hive \"{hiveDir}\"");
            Assert.True(scaffoldExit == 0, $"Scaffold failed:\n{scaffoldOut}");

            AssertDabblerAppSplitTopology(tempDir, "DabblerApp");

            var projPath = Path.Combine(tempDir, "DabblerApp.slnx");
            WriteLocalNugetConfig(tempDir, PackOutputDir);

            var (restoreExit, restoreOut) = RunTool("dotnet",
                $"restore \"{projPath}\" --source \"{PackOutputDir}\" --source \"https://api.nuget.org/v3/index.json\"");
            Assert.True(restoreExit == 0, $"Restore failed:\n{restoreOut}");

            var (buildExit, buildOut) = RunTool("dotnet",
                $"build \"{projPath}\" --no-restore --nologo -v q");
            Assert.True(buildExit == 0, $"Build failed:\n{buildOut}");

            // Start the host on a free ephemeral port.
            // Override the DB name so migration creates an isolated database and does not
            // overwrite the developer's DabblerApp_Dev instance.
            var port = FindFreePort();
            var listenUrl = $"http://localhost:{port}";
            var smokeConnStr = "Server=(localdb)\\mssqllocaldb;Database=DabblerApp_SmokeTest;" +
                               "Trusted_Connection=True;MultipleActiveResultSets=true";

            var startupOutput = new StringBuilder();
            bool serverStarted = false;

            var webProjDir = Path.Combine(tempDir, "src", "DabblerApp.Web");
            var psi = new ProcessStartInfo("dotnet", $"run --no-build --urls \"{listenUrl}\"")
            {
                WorkingDirectory = webProjDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            psi.Environment["ConnectionStrings__DefaultConnection"] = smokeConnStr;
            psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";

            hostProcess = Process.Start(psi)!;
            hostProcess.OutputDataReceived += (_, e) =>
            {
                if (e.Data is null) return;
                lock (startupOutput) startupOutput.AppendLine(e.Data);
                if (e.Data.Contains("Now listening on:", StringComparison.OrdinalIgnoreCase))
                    serverStarted = true;
            };
            hostProcess.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is not null)
                    lock (startupOutput) startupOutput.AppendLine("[err] " + e.Data);
            };
            hostProcess.BeginOutputReadLine();
            hostProcess.BeginErrorReadLine();

            // Allow up to 90 seconds for LocalDB migration + Kestrel startup.
            var deadline = DateTime.UtcNow.AddSeconds(90);
            while (!serverStarted && !hostProcess.HasExited && DateTime.UtcNow < deadline)
                await Task.Delay(250);

            if (hostProcess.HasExited)
            {
                Assert.Fail(
                    $"Scaffolded host exited before signalling startup (exit code {hostProcess.ExitCode}). " +
                    $"Ensure SQL Server LocalDB is available.\n" +
                    $"Output:\n{startupOutput}");
            }

            Assert.True(serverStarted,
                $"Host did not emit 'Now listening on:' within 90 seconds.\n" +
                $"Output:\n{startupOutput}");

            // GET / — before B1 fix: HTTP 500 (JsonException during auth-config load).
            //         after B1 fix:  HTTP 302 (redirect to /login).
            // AllowAutoRedirect=false so we see the raw 302 rather than following it.
            using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
            {
                Timeout = TimeSpan.FromSeconds(15),
            };

            var response = await http.GetAsync(listenUrl + "/");

            Assert.True((int)response.StatusCode < 500,
                $"GET / returned HTTP {(int)response.StatusCode} — expected < 500. " +
                "If 'BakedInRoleAliasOverrides' in the template's security-config.json is " +
                "'[]' (JSON array) instead of '{}' (JSON object), fix it and rerun " +
                "tools/pack.ps1 — this is the B1 regression.\n" +
                $"Server output:\n{startupOutput}");
        }
        finally
        {
            if (hostProcess is not null)
            {
                try { hostProcess.Kill(entireProcessTree: true); } catch { /* best-effort */ }
                hostProcess.WaitForExit(5000);
                hostProcess.Dispose();
            }
            TryDeleteDirectory(tempDir);
            TryDeleteDirectory(hiveDir);
        }
    }

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private static int FindFreePort()
    {
        using var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static void AssertDabblerAppSplitTopology(string rootDir, string appName)
    {
        File.Exists(Path.Combine(rootDir, $"{appName}.slnx")).Should().BeTrue(
            "the full template should scaffold a solution file at the root");
        File.Exists(Path.Combine(rootDir, "src", $"{appName}.Web", $"{appName}.Web.csproj")).Should().BeTrue(
            "the full template should scaffold a Blazor Server web project under src/");
        File.Exists(Path.Combine(rootDir, "src", $"{appName}.Api", $"{appName}.Api.csproj")).Should().BeTrue(
            "the full template should scaffold a Web API project under src/");
        File.Exists(Path.Combine(rootDir, "src", $"{appName}.Shared", $"{appName}.Shared.csproj")).Should().BeTrue(
            "the full template should scaffold a shared library project under src/");
        File.Exists(Path.Combine(rootDir, "tests", $"{appName}.Web.Tests", $"{appName}.Web.Tests.csproj")).Should().BeTrue(
            "the full template should scaffold a test project under tests/");

        File.Exists(Path.Combine(rootDir, appName, $"{appName}.csproj")).Should().BeFalse(
            "the legacy monolithic app project should no longer be emitted by dabbler-app");
        File.Exists(Path.Combine(rootDir, $"{appName}.Tests", $"{appName}.Tests.csproj")).Should().BeFalse(
            "the legacy top-level test project should no longer be emitted by dabbler-app");
    }

    /// <summary>
    /// Returns the first .nupkg whose filename exactly matches <paramref name="packageId"/>.
    /// Uses a digit-guard after the packageId prefix so a sub-package like
    /// Dabbler.Templates.Compat from satisfying a lookup for Dabbler.Templates.
    /// </summary>
    private static string? FindExactNupkg(string packOutputDir, string packageId)
    {
        if (!Directory.Exists(packOutputDir))
            return null;
        var prefix = packageId + ".";
        return Directory.GetFiles(packOutputDir, $"{packageId}.*.nupkg")
            .FirstOrDefault(f =>
            {
                var name = Path.GetFileName(f);
                return name is not null &&
                       name.Length > prefix.Length &&
                       char.IsDigit(name[prefix.Length]);
            });
    }

    /// <summary>
    /// Writes a nuget.config at <paramref name="projectDir"/> that adds the local pack-output
    /// as an additional feed. Preserves existing user/machine sources so framework packages
    /// restore normally; Dabbler.* packages at 0.1.0-preview.1 only exist in the local feed.
    /// </summary>
    private static void WriteLocalNugetConfig(string projectDir, string packOutputDir)
    {
        var localFeedUri = packOutputDir.Replace('\\', '/');
        var content = $"""
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="local-pack-output" value="{localFeedUri}" />
  </packageSources>
</configuration>
""";
        File.WriteAllText(Path.Combine(projectDir, "nuget.config"), content);
    }

    /// <summary>Runs a CLI tool and returns (exitCode, combined stdout+stderr output).</summary>
    private static (int exitCode, string output) RunTool(string tool, string args)
    {
        var psi = new ProcessStartInfo(tool, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
        };
        using var process = Process.Start(psi)!;
        var sb = new StringBuilder();
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) sb.AppendLine(e.Data); };
        process.ErrorDataReceived  += (_, e) => { if (e.Data is not null) sb.AppendLine(e.Data); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        bool exited = process.WaitForExit(DefaultToolTimeout);
        if (!exited)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* best-effort */ }
            process.WaitForExit(TimeSpan.FromSeconds(5)); // drain output after kill
            return (-1, $"TIMEOUT after {DefaultToolTimeout.TotalMinutes}m. Partial output:\n{sb}");
        }

        // Parameter-less WaitForExit ensures async BeginOutputReadLine/BeginErrorReadLine
        // event handlers have fully drained before we read the output string.
        process.WaitForExit();
        return (process.ExitCode, sb.ToString());
    }

    private static void TryDeleteDirectory(string path)
    {
        try { Directory.Delete(path, recursive: true); }
        catch { /* best-effort */ }
    }
}
