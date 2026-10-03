using FluentAssertions;
using System.Diagnostics;
using System.Text;
using Xunit;

namespace Dabbler.Templates.Tests;

/// <summary>
/// Consumer smoke tests for the dabbler-crud-service template (crud-scaffolding Session 2).
///
/// Each test pre-stages a tiny SmokeApi fixture (Smoke entity, SmokeDto, SmokeDbContext) into
/// a temp directory, installs Dabbler.Templates.*.nupkg into an isolated dotnet-new hive, runs
/// `dotnet new dabbler-crud-service --entity Smoke ...`, then dotnet build's the resulting
/// project and asserts the compile succeeds. Skips when pack-output/ has not been produced yet.
///
/// Three scaffold cases are covered, all using the spec-compliant kebab-case CLI form
/// (`--api-namespace`, `--entity-plural`, `--auth-policy-prefix`, …) wired through the
/// template's `.template.config/dotnetcli.host.json` alias map:
///   * Default policy prefix — entity flows into [DabblerEndpointAuthorize] policy strings
///     via the coalesce-driven effectiveAuthPolicyPrefix symbol when --auth-policy-prefix
///     is omitted. Smoke entity has an int PK; controller route templates use `{id:int}`.
///   * Explicit --auth-policy-prefix override — exercises the spec line 92 auth-policy-prefix
///     feature; the controller's policy strings should incorporate the override instead of
///     defaulting to the entity name.
///   * Non-int primary key (--pk-type long) — exercises the derivedPkRouteConstraint switch
///     generator; the controller route templates should emit `{id:long}` rather than the
///     default `{id:int}`.
/// </summary>
public class CrudServiceTemplateTests
{
    private static readonly string PackOutputDir = GetPackOutputDir();
    private static readonly string FixtureDir = Path.Combine(AppContext.BaseDirectory, "Fixtures");
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

    private const string CommonKebabArgs =
        "--entity Smoke " +
        "--entity-plural Smokes " +
        "--api-namespace SmokeApi.Api " +
        "--dto-namespace SmokeApi.Shared.DTOs " +
        "--shared-namespace SmokeApi.Shared " +
        "--entity-namespace SmokeApi.Shared.Models " +
        "--db-context-type SmokeDbContext " +
        "--db-context-namespace SmokeApi.Api.Data " +
        "--schema smoke.dabbler-entity.json";

    [SkippableFact]
    public void DabblerCrudService_Template_DefaultPolicyPrefix_ScaffoldsAndBuildsClean()
    {
        RunSmokeWithArgs(
            CommonKebabArgs,
            inspectControllerBeforeCleanup: controllerSource =>
            {
                controllerSource.Should().Contain("\"QuerySmoke\"",
                    "the default policy prefix should be the entity name when --auth-policy-prefix is omitted");
                controllerSource.Should().Contain("\"GetSmoke\"");
                controllerSource.Should().Contain("\"CreateSmoke\"");
                controllerSource.Should().Contain("\"UpdateSmoke\"");
                controllerSource.Should().Contain("\"DeleteSmoke\"");
                controllerSource.Should().Contain("{id:int}",
                    "the default pk-type (int) should produce {id:int} route constraints");
            });
    }

    /// <summary>
    /// Exercises the spec line 92 --auth-policy-prefix feature. The controller's policy
    /// strings should incorporate the override instead of falling back to the entity name.
    /// </summary>
    [SkippableFact]
    public void DabblerCrudService_Template_WithAuthPolicyPrefixOverride_ScaffoldsAndBuildsClean()
    {
        RunSmokeWithArgs(
            CommonKebabArgs + " --auth-policy-prefix BackofficeSmoke",
            inspectControllerBeforeCleanup: controllerSource =>
            {
                controllerSource.Should().Contain("\"QueryBackofficeSmoke\"",
                    "the explicit --auth-policy-prefix override should be honoured in policy strings");
                controllerSource.Should().Contain("\"GetBackofficeSmoke\"");
                controllerSource.Should().Contain("\"CreateBackofficeSmoke\"");
                controllerSource.Should().Contain("\"UpdateBackofficeSmoke\"");
                controllerSource.Should().Contain("\"DeleteBackofficeSmoke\"");
                controllerSource.Should().NotContain("\"QuerySmoke\"",
                    "the entity-name-derived fallback should NOT appear when --auth-policy-prefix is set");
            });
    }

    /// <summary>
    /// Exercises the derivedPkRouteConstraint switch generator with --pk-type long.
    /// Verifies the generated controller emits {id:long} routes, not the default {id:int}.
    /// </summary>
    [SkippableFact]
    public void DabblerCrudService_Template_WithLongPkType_EmitsCorrectRouteConstraint()
    {
        RunSmokeWithArgs(
            CommonKebabArgs + " --pk-type long",
            // The SmokeApi fixture's Smoke.cs declares Id as int; building the scaffolded
            // service against that fixture would fail because long != int. So skip build
            // for this case and inspect only the generated source for the route-constraint
            // assertion. The build path is covered by the int-PK tests above.
            buildAfterScaffold: false,
            inspectControllerBeforeCleanup: controllerSource =>
            {
                controllerSource.Should().Contain("{id:long}",
                    "--pk-type long should drive the derived route constraint to ':long'");
                controllerSource.Should().NotContain("{id:int}",
                    "the default :int constraint should be overridden when --pk-type long is set");
            });
    }

    // ─── Shared smoke runner ────────────────────────────────────────────────

    private static string RunSmokeWithArgs(
        string scaffoldArgs,
        Action<string>? inspectControllerBeforeCleanup = null,
        bool buildAfterScaffold = true)
    {
        Skip.If(!Directory.Exists(PackOutputDir),
            "Pack output directory not found. Run tools/pack.ps1 first.");

        var templateNupkg = FindExactNupkg(PackOutputDir, "Dabbler.Templates");
        Skip.If(templateNupkg is null,
            "Dabbler.Templates nupkg not found in pack-output/. Run tools/pack.ps1 first.");

        var querying = FindExactNupkg(PackOutputDir, "Dabbler.Api.Querying");
        Skip.If(querying is null,
            "Dabbler.Api.Querying nupkg not found in pack-output/. Run tools/pack.ps1 first.");

        var tempDir = Path.Combine(Path.GetTempPath(), $"dabbler-crud-service-smoke-{Guid.NewGuid():N}");
        var hiveDir = Path.Combine(Path.GetTempPath(), $"dabbler-crud-service-hive-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            CopyDirectory(Path.Combine(FixtureDir, "SmokeApi"), tempDir);
            File.Copy(
                Path.Combine(FixtureDir, "smoke.dabbler-entity.json"),
                Path.Combine(tempDir, "smoke.dabbler-entity.json"));
            WriteLocalNugetConfig(tempDir, PackOutputDir);

            var (installExit, installOut) = RunTool("dotnet",
                $"new install \"{templateNupkg}\" --debug:custom-hive \"{hiveDir}\"",
                tempDir);
            installExit.Should().Be(0, $"template install should succeed. Output:\n{installOut}");

            var (scaffoldExit, scaffoldOut) = RunTool("dotnet",
                $"new dabbler-crud-service {scaffoldArgs} --debug:custom-hive \"{hiveDir}\"",
                tempDir);
            scaffoldExit.Should().Be(0, $"scaffold should succeed. Output:\n{scaffoldOut}");

            File.Exists(Path.Combine(tempDir, "Services", "ISmokeService.cs"))
                .Should().BeTrue("ISmokeService.cs should be emitted by the template");
            File.Exists(Path.Combine(tempDir, "Services", "SmokeService.cs"))
                .Should().BeTrue("SmokeService.cs should be emitted by the template");
            File.Exists(Path.Combine(tempDir, "Controllers", "SmokesController.cs"))
                .Should().BeTrue("SmokesController.cs should be emitted by the template");

            if (buildAfterScaffold)
            {
                var projPath = Path.Combine(tempDir, "SmokeApi.csproj");
                var (restoreExit, restoreOut) = RunTool("dotnet",
                    $"restore \"{projPath}\" --source \"{PackOutputDir}\" --source \"https://api.nuget.org/v3/index.json\"",
                    tempDir);
                restoreExit.Should().Be(0, $"restore should succeed. Output:\n{restoreOut}");

                var (buildExit, buildOut) = RunTool("dotnet",
                    $"build \"{projPath}\" --no-restore --nologo -v q",
                    tempDir);
                buildExit.Should().Be(0,
                    $"scaffolded dabbler-crud-service slice should build clean. Output:\n{buildOut}");
            }

            if (inspectControllerBeforeCleanup is not null)
            {
                var controllerSource = File.ReadAllText(Path.Combine(tempDir, "Controllers", "SmokesController.cs"));
                inspectControllerBeforeCleanup(controllerSource);
            }
            return tempDir;
        }
        finally
        {
            TryDeleteDirectory(tempDir);
            TryDeleteDirectory(hiveDir);
        }
    }

    // ─── Helpers ────────────────────────────────────────────────────────────

    private static string? FindExactNupkg(string packOutputDir, string packageId)
    {
        if (!Directory.Exists(packOutputDir)) return null;
        var prefix = packageId + ".";
        return Directory.GetFiles(packOutputDir, $"{packageId}.*.nupkg")
            .FirstOrDefault(f =>
            {
                var name = Path.GetFileName(f);
                return name is not null
                    && name.Length > prefix.Length
                    && char.IsDigit(name[prefix.Length]);
            });
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);
        foreach (var dir in Directory.GetDirectories(source))
            CopyDirectory(dir, Path.Combine(destination, Path.GetFileName(dir)));
    }

    private static void WriteLocalNugetConfig(string projectDir, string packOutputDir)
    {
        var localFeedUri = packOutputDir.Replace('\\', '/');
        var content = $"""
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local-pack-output" value="{localFeedUri}" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
""";
        File.WriteAllText(Path.Combine(projectDir, "nuget.config"), content);
    }

    private static (int exitCode, string output) RunTool(string tool, string args, string workingDir)
    {
        var psi = new ProcessStartInfo(tool, args)
        {
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        using var process = Process.Start(psi)!;
        var sb = new StringBuilder();
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) sb.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) sb.AppendLine(e.Data); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        if (!process.WaitForExit(DefaultToolTimeout))
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            process.WaitForExit(TimeSpan.FromSeconds(5));
            return (-1, $"TIMEOUT after {DefaultToolTimeout.TotalMinutes}m. Partial output:\n{sb}");
        }
        process.WaitForExit();
        return (process.ExitCode, sb.ToString());
    }

    private static void TryDeleteDirectory(string path)
    {
        try { Directory.Delete(path, recursive: true); } catch { }
    }
}
