using FluentAssertions;
using System.Diagnostics;
using System.Text;
using Xunit;

namespace Dabbler.Templates.Tests;

/// <summary>
/// End-to-end consumer smoke test for the dabbler-crud-page template (Session 3 of
/// crud-scaffolding). Pre-stages the SmokeApp fixture (Smoke entity, SmokeDto,
/// SmokeDbContext, minimal Program.cs, csproj that pulls MudBlazor + Dabbler.Components
/// + Dabbler.Security.Blazor), installs Dabbler.Templates.*.nupkg into an isolated
/// dotnet-new hive, runs `dotnet new dabbler-crud-service` AND `dotnet new dabbler-crud-page`
/// against the same project, runs `pwsh Apply-SmokeSchema.ps1` to fill the schema-driven
/// placeholder regions, then `dotnet build`s the resulting project. Skips when
/// pack-output/ has not been produced yet.
///
/// What this proves (per Session 3 spec, "Ends with"):
///   * The two templates compose into a single project skeleton with no hand-edits.
///   * The page slice (route + list + detail + ApiClient + GridDataProvider + DI
///     extension) compiles cleanly against the published Dabbler.Components contract.
///   * The schema-driven post-process script populates GRID-COLUMNS / FORM-FIELDS /
///     FILTER-PROPERTIES regions from the .dabbler-entity.json fixture.
///   * The generated AddSmokeScaffolding(IServiceCollection, IConfiguration) is the
///     single line a consumer's Program.cs needs to wire both halves of the slice.
/// </summary>
public class CrudPageTemplateTests
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

    [SkippableFact]
    public void DabblerCrudPage_PairedWithCrudService_ScaffoldsAndBuildsClean()
    {
        Skip.If(!Directory.Exists(PackOutputDir),
            "Pack output directory not found. Run tools/pack.ps1 first.");

        var templateNupkg = FindExactNupkg(PackOutputDir, "Dabbler.Templates");
        Skip.If(templateNupkg is null,
            "Dabbler.Templates nupkg not found in pack-output/. Run tools/pack.ps1 first.");
        Skip.If(FindExactNupkg(PackOutputDir, "Dabbler.Api.Querying") is null,
            "Dabbler.Api.Querying nupkg not found in pack-output/. Run tools/pack.ps1 first.");
        Skip.If(FindExactNupkg(PackOutputDir, "Dabbler.Components") is null,
            "Dabbler.Components nupkg not found in pack-output/. Run tools/pack.ps1 first.");
        Skip.If(FindExactNupkg(PackOutputDir, "Dabbler.Security.Blazor") is null,
            "Dabbler.Security.Blazor nupkg not found in pack-output/. Run tools/pack.ps1 first.");

        var pwshTool = ResolvePowerShellExecutable();
        Skip.If(pwshTool is null,
            "Neither pwsh (PowerShell 7+) nor Windows powershell.exe was found on PATH; cannot run the schema post-process.");

        var tempDir = Path.Combine(Path.GetTempPath(), $"dabbler-crud-page-smoke-{Guid.NewGuid():N}");
        var hiveDir = Path.Combine(Path.GetTempPath(), $"dabbler-crud-page-hive-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            CopyDirectory(Path.Combine(FixtureDir, "SmokeApp"), tempDir);
            File.Copy(
                Path.Combine(FixtureDir, "smoke.dabbler-entity.json"),
                Path.Combine(tempDir, "smoke.dabbler-entity.json"));
            WriteLocalNugetConfig(tempDir, PackOutputDir);

            // 1. Install the template package into an isolated hive.
            var (installExit, installOut) = RunTool("dotnet",
                $"new install \"{templateNupkg}\" --debug:custom-hive \"{hiveDir}\"",
                tempDir);
            installExit.Should().Be(0, $"template install should succeed. Output:\n{installOut}");

            // 2. Scaffold the SERVICE tier (controller + service + interface).
            var serviceArgs =
                "--entity Smoke " +
                "--entity-plural Smokes " +
                "--api-namespace SmokeApp.Api " +
                "--dto-namespace SmokeApp.Shared.DTOs " +
                "--shared-namespace SmokeApp.Shared " +
                "--entity-namespace SmokeApp.Shared.Models " +
                "--db-context-type SmokeDbContext " +
                "--db-context-namespace SmokeApp.Api.Data " +
                "--schema smoke.dabbler-entity.json";
            var (svcExit, svcOut) = RunTool("dotnet",
                $"new dabbler-crud-service {serviceArgs} --debug:custom-hive \"{hiveDir}\"",
                tempDir);
            svcExit.Should().Be(0, $"crud-service scaffold should succeed. Output:\n{svcOut}");

            // 3. Scaffold the PAGE tier (page + list + detail + ApiClient + GridDataProvider + extension + script).
            var pageArgs =
                "--entity Smoke " +
                "--entity-plural Smokes " +
                "--web-namespace SmokeApp " +
                "--shared-namespace SmokeApp.Shared " +
                "--dto-namespace SmokeApp.Shared.DTOs " +
                "--schema smoke.dabbler-entity.json";
            var (pgExit, pgOut) = RunTool("dotnet",
                $"new dabbler-crud-page {pageArgs} --debug:custom-hive \"{hiveDir}\"",
                tempDir);
            pgExit.Should().Be(0, $"crud-page scaffold should succeed. Output:\n{pgOut}");

            // 4. Verify the file rename + folder rename produced the expected layout.
            var pagesDir = Path.Combine(tempDir, "Components", "Pages", "Smokes");
            File.Exists(Path.Combine(pagesDir, "SmokesPage.razor"))
                .Should().BeTrue("SmokesPage.razor should be emitted under Components/Pages/Smokes/");
            File.Exists(Path.Combine(pagesDir, "SmokeDetailPage.razor"))
                .Should().BeTrue("SmokeDetailPage.razor should be emitted");
            File.Exists(Path.Combine(pagesDir, "SmokeNewPage.razor"))
                .Should().BeTrue("SmokeNewPage.razor (the /new route wrapper) should be emitted");
            File.Exists(Path.Combine(pagesDir, "SmokeList.razor"))
                .Should().BeTrue("SmokeList.razor should be emitted");
            File.Exists(Path.Combine(pagesDir, "SmokeDetail.razor"))
                .Should().BeTrue("SmokeDetail.razor should be emitted");
            File.Exists(Path.Combine(pagesDir, "Apply-SmokeSchema.ps1"))
                .Should().BeTrue("Apply-SmokeSchema.ps1 post-process script should be emitted");
            File.Exists(Path.Combine(tempDir, "Data", "SmokeApiClient.cs"))
                .Should().BeTrue("SmokeApiClient.cs should be emitted under Data/");
            File.Exists(Path.Combine(tempDir, "Data", "SmokeGridDataProvider.cs"))
                .Should().BeTrue("SmokeGridDataProvider.cs should be emitted");
            File.Exists(Path.Combine(tempDir, "Data", "SmokeScaffoldingExtensions.cs"))
                .Should().BeTrue("SmokeScaffoldingExtensions.cs should be emitted");

            // 5. Run the schema-driven post-process script.
            var scriptPath = Path.Combine(pagesDir, "Apply-SmokeSchema.ps1");
            var schemaPath = Path.Combine(tempDir, "smoke.dabbler-entity.json");
            var (psExit, psOut) = RunTool(pwshTool!,
                $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -SchemaPath \"{schemaPath}\"",
                tempDir);
            psExit.Should().Be(0, $"Apply-SmokeSchema.ps1 should run cleanly. Output:\n{psOut}");

            // 6. Assert schema-driven content was applied.
            var listSource = File.ReadAllText(Path.Combine(pagesDir, "SmokeList.razor"));
            listSource.Should().Contain("Property=\"e => e.Name\"",
                "the post-process should emit a Name grid column from the schema");
            listSource.Should().Contain("Property=\"e => e.IsActive\"",
                "the post-process should emit an IsActive grid column from the schema");
            listSource.Should().Contain("FilterProperty<SmokeDto>.Text",
                "the post-process should emit a Text filter property for Name");
            listSource.Should().Contain("FilterProperty<SmokeDto>.Bool",
                "the post-process should emit a Bool filter property for IsActive");

            var detailSource = File.ReadAllText(Path.Combine(pagesDir, "SmokeDetail.razor"));
            detailSource.Should().Contain("Value=\"@_record.Name\"",
                "the post-process should emit a Name form field with Value binding");
            detailSource.Should().NotContain("@bind-Value",
                "@bind-Value is forbidden in generated form fields (it bypasses EditSession dirty tracking)");
            detailSource.Should().Contain("ValueChanged=\"@((string v) => { _record.Name = v; _session?.NotifyChanged(); })\"",
                "form fields should use Value + ValueChanged + NotifyChanged() per the CRUD Form Conventions");

            // The smoke fixture's schema marks Id as isInForm: false, so the form should
            // NOT contain a field bound to _record.Id.
            detailSource.Should().NotContain("Value=\"@_record.Id\"",
                "Id should be excluded from the form per the schema's isInForm: false flag (and PKs are excluded by default)");

            // 7. Build the resulting project end-to-end.
            var projPath = Path.Combine(tempDir, "SmokeApp.csproj");
            var (restoreExit, restoreOut) = RunTool("dotnet",
                $"restore \"{projPath}\" --source \"{PackOutputDir}\" --source \"https://api.nuget.org/v3/index.json\"",
                tempDir);
            restoreExit.Should().Be(0, $"restore should succeed. Output:\n{restoreOut}");

            var (buildExit, buildOut) = RunTool("dotnet",
                $"build \"{projPath}\" --no-restore --nologo -v q",
                tempDir);
            buildExit.Should().Be(0,
                $"scaffolded dabbler-crud-page + dabbler-crud-service slice should build clean. Output:\n{buildOut}");

            // 8. Assert the DI extension wires both halves (AddSmokeScaffolding) and
            //    is actually callable from the consumer's Program.cs surface. The
            //    fixture's Program.cs calls AddSmokeScaffolding(builder.Configuration)
            //    directly — the successful `dotnet build` at step 7 is the DI-resolution
            //    proof. Source-level greps below cross-check the extension's shape so a
            //    misnamed-but-still-compiling overload is caught.
            var extensionSource = File.ReadAllText(Path.Combine(tempDir, "Data", "SmokeScaffoldingExtensions.cs"));
            extensionSource.Should().Contain("public static IHttpClientBuilder AddSmokeScaffolding(",
                "the scaffolded DI extension should be named AddSmokeScaffolding");
            extensionSource.Should().Contain("services.AddHttpClient<SmokeApiClient>",
                "AddSmokeScaffolding should register the typed HttpClient<SmokeApiClient>");
            extensionSource.Should().Contain("services.AddTransient<IGridDataProvider<SmokeDto>, SmokeGridDataProvider>",
                "AddSmokeScaffolding should register the IGridDataProvider<SmokeDto> binding");

            var programSource = File.ReadAllText(Path.Combine(tempDir, "Program.cs"));
            programSource.Should().Contain("builder.Services.AddSmokeScaffolding(builder.Configuration)",
                "fixture's Program.cs should exercise AddSmokeScaffolding so the build is a DI-resolution proof");
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

    private static string? ResolvePowerShellExecutable()
    {
        foreach (var candidate in new[] { "pwsh", "powershell" })
        {
            try
            {
                var psi = new ProcessStartInfo(candidate, "-NoProfile -Command \"$PSVersionTable.PSVersion.Major\"")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                };
                using var p = Process.Start(psi);
                if (p is null) continue;
                if (!p.WaitForExit(TimeSpan.FromSeconds(10))) { try { p.Kill(); } catch { } continue; }
                if (p.ExitCode == 0) return candidate;
            }
            catch
            {
                // Process.Start throws when the executable is not on PATH; try the next candidate.
            }
        }
        return null;
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
