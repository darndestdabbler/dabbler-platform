using FluentAssertions;
using System.Diagnostics;
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

            // --name DabblerApp preserves the sourceName so the project directory
            // stays DabblerApp/ rather than being renamed to the temp dir's basename.
            var (scaffoldExit, scaffoldOut) = RunTool("dotnet",
                $"new dabbler-app -o \"{tempDir}\" --name DabblerApp --force --debug:custom-hive \"{hiveDir}\"");
            scaffoldExit.Should().Be(0, $"dabbler-app scaffold should succeed. Output:\n{scaffoldOut}");

            var projPath = Path.Combine(tempDir, "DabblerApp", "DabblerApp.csproj");

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

            var testProjPath = Path.Combine(tempDir, "DabblerApp.Tests", "DabblerApp.Tests.csproj");

            // Write nuget.config at the solution root so auto-restore finds local Dabbler.* packages.
            WriteLocalNugetConfig(tempDir, PackOutputDir);

            var (testExit, testOut) = RunTool("dotnet",
                $"test \"{testProjPath}\" --nologo -v q");
            testExit.Should().Be(0, $"scaffolded DabblerApp.Tests should pass. Output:\n{testOut}");
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

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

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
