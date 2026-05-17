using System.Diagnostics;
using System.Text;
using FluentAssertions;
using Xunit;

namespace Dabbler.CrudSlice.Tests;

/// <summary>
/// Orchestration tests that invoke the actual <c>Dabbler.CrudSlice</c> executable
/// end-to-end.
///
/// <para>
/// <see cref="EmitRegistrationsOnly_writes_both_helper_files_with_convention_correct_content"/>
/// — pure-orchestration test. Uses <c>--skip-templates</c> + <c>--skip-schema-applicator</c>
/// so it does NOT require the template nupkg in pack-output/ and runs in any environment.
/// Verifies the wrapper's Program.cs entry point correctly drives the registration emitter
/// and writes the two helper files under <c>Web/Data/</c> and <c>Api/Services/</c>.
/// </para>
///
/// <para>
/// <see cref="FullPipeline_endToEnd_against_smoke_fixture"/> — SkippableFact that runs the
/// full pipeline (templates + schema applicator + registrations) against the SmokeApp
/// fixture used by Dabbler.Templates.Tests. Skipped when <c>pack-output/</c> is missing.
/// </para>
/// </summary>
public class WrapperOrchestrationTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);

    [Fact]
    public void EmitRegistrationsOnly_writes_both_helper_files_with_convention_correct_content()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"crud-slice-emit-only-{Guid.NewGuid():N}");
        var webRoot = Path.Combine(tempDir, "Web");
        var apiRoot = Path.Combine(tempDir, "Api");
        Directory.CreateDirectory(webRoot);
        Directory.CreateDirectory(apiRoot);
        var schemaPath = Path.Combine(tempDir, "widget.dabbler-entity.json");
        File.WriteAllText(schemaPath, "{\"entity\":\"Widget\",\"properties\":[]}");

        try
        {
            var args = new StringBuilder();
            args.Append($"--entity Widget --entity-plural Widgets ");
            args.Append($"--schema \"{schemaPath}\" ");
            args.Append($"--web-root \"{webRoot}\" --api-root \"{apiRoot}\" ");
            args.Append($"--web-namespace SampleApp.Web --api-namespace SampleApp.Api ");
            args.Append($"--shared-namespace SampleApp.Shared --dto-namespace SampleApp.Shared.DTOs ");
            args.Append($"--entity-namespace SampleApp.Shared.Models ");
            args.Append($"--dbcontext-type SampleDbContext --dbcontext-namespace SampleApp.Api.Data ");
            args.Append($"--skip-templates --skip-schema-applicator");

            var (exit, output) = RunCrudSlice(args.ToString());
            exit.Should().Be(0, $"Dabbler.CrudSlice should exit 0 in emit-only mode. Output:\n{output}");

            var webExtPath = Path.Combine(webRoot, "Data", "WidgetScaffoldingExtensions.cs");
            File.Exists(webExtPath).Should().BeTrue("Web extension file should be emitted");
            var webSrc = File.ReadAllText(webExtPath);
            webSrc.Should().Contain("\"Services:ApiBaseUrl\"");
            webSrc.Should().Contain("\"X-Internal-Api-Key\"");
            webSrc.Should().Contain(".AddHttpMessageHandler<UserIdentityHandler>()");
            webSrc.Should().NotContain("\"Api:BaseUrl\"");
            webSrc.Should().Contain("namespace SampleApp.Web.Data;");
            webSrc.Should().Contain("public static IHttpClientBuilder AddWidgetScaffolding(");

            var apiExtPath = Path.Combine(apiRoot, "Services", "WidgetApiRegistrationExtensions.cs");
            File.Exists(apiExtPath).Should().BeTrue("API extension file should be emitted");
            var apiSrc = File.ReadAllText(apiExtPath);
            apiSrc.Should().Contain("public static IServiceCollection AddWidgetApiServices(");
            apiSrc.Should().Contain("services.AddScoped<IWidgetService, WidgetService>();");
            apiSrc.Should().Contain("namespace SampleApp.Api.Services;");
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    [Fact]
    public void Missing_required_args_exits_nonzero_with_usage()
    {
        var (exit, output) = RunCrudSlice("--entity Widget");
        exit.Should().NotBe(0);
        output.Should().Contain("Required:");
        output.Should().Contain("Usage:");
    }

    [SkippableFact]
    public void FullPipeline_endToEnd_against_smoke_fixture()
    {
        // Locate pack-output (shared with Dabbler.Templates.Tests).
        var packOutputDir = LocatePackOutputDir();
        Skip.If(packOutputDir is null,
            "pack-output/ not found. Run tools/pack.ps1 first to enable this end-to-end test.");

        var templateNupkg = FindExactNupkg(packOutputDir!, "Dabbler.Templates");
        Skip.If(templateNupkg is null, "Dabbler.Templates nupkg not in pack-output/. Run tools/pack.ps1 first.");

        // Use the SmokeApp fixture from Dabbler.Templates.Tests as the test consumer.
        var smokeFixtureDir = LocateSmokeAppFixture();
        Skip.If(smokeFixtureDir is null,
            "SmokeApp fixture not found alongside Dabbler.Templates.Tests build output.");

        var smokeSchemaPath = LocateSmokeSchema();
        Skip.If(smokeSchemaPath is null,
            "smoke.dabbler-entity.json not found alongside Dabbler.Templates.Tests build output.");

        var tempDir = Path.Combine(Path.GetTempPath(), $"crud-slice-e2e-{Guid.NewGuid():N}");
        var hiveDir = Path.Combine(Path.GetTempPath(), $"crud-slice-e2e-hive-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            CopyDirectory(smokeFixtureDir!, tempDir);
            var copiedSchemaPath = Path.Combine(tempDir, "smoke.dabbler-entity.json");
            File.Copy(smokeSchemaPath!, copiedSchemaPath, overwrite: true);
            WriteLocalNugetConfig(tempDir, packOutputDir!);

            // Install template into custom hive (out-of-band; Dabbler.CrudSlice itself does
            // not handle install — operator does that once per workstation).
            var (installExit, installOut) = RunTool("dotnet",
                $"new install \"{templateNupkg}\" --debug:custom-hive \"{hiveDir}\"",
                tempDir);
            installExit.Should().Be(0, $"template install. Output:\n{installOut}");

            // For the smoke fixture, the Web project and API project are the same directory
            // (single-project consumer). The wrapper's templates target subfolders within
            // the same root, which is fine.
            var args = new StringBuilder();
            args.Append("--entity Smoke --entity-plural Smokes ");
            args.Append($"--schema \"{copiedSchemaPath}\" ");
            args.Append($"--web-root \"{tempDir}\" --api-root \"{tempDir}\" ");
            args.Append("--web-namespace SmokeApp --api-namespace SmokeApp.Api ");
            args.Append("--shared-namespace SmokeApp.Shared --dto-namespace SmokeApp.Shared.DTOs ");
            args.Append("--entity-namespace SmokeApp.Shared.Models ");
            args.Append("--dbcontext-type SmokeDbContext --dbcontext-namespace SmokeApp.Api.Data ");
            args.Append($"--template-hive \"{hiveDir}\"");

            var (exit, output) = RunCrudSlice(args.ToString());
            exit.Should().Be(0, $"Dabbler.CrudSlice end-to-end should exit 0. Output:\n{output}");

            // a. Template outputs exist.
            var pagesDir = Path.Combine(tempDir, "Components", "Pages", "Smokes");
            File.Exists(Path.Combine(pagesDir, "SmokesPage.razor")).Should().BeTrue();
            File.Exists(Path.Combine(pagesDir, "SmokeList.razor")).Should().BeTrue();
            File.Exists(Path.Combine(pagesDir, "SmokeDetail.razor")).Should().BeTrue();
            File.Exists(Path.Combine(tempDir, "Data", "SmokeApiClient.cs")).Should().BeTrue();
            File.Exists(Path.Combine(tempDir, "Data", "SmokeGridDataProvider.cs")).Should().BeTrue();
            File.Exists(Path.Combine(tempDir, "Services", "ISmokeService.cs")).Should().BeTrue();
            File.Exists(Path.Combine(tempDir, "Services", "SmokeService.cs")).Should().BeTrue();
            File.Exists(Path.Combine(tempDir, "Controllers", "SmokesController.cs")).Should().BeTrue();

            // b. Schema substitution was applied (replaces the template's empty regions).
            var listSrc = File.ReadAllText(Path.Combine(pagesDir, "SmokeList.razor"));
            listSrc.Should().Contain("Property=\"e => e.Name\"",
                "the wrapper-invoked schema applicator should populate grid columns from the schema");

            // c. Web ScaffoldingExtensions is the convention-correct overwrite, not the
            //    template's wrong-convention default.
            var webExt = File.ReadAllText(Path.Combine(tempDir, "Data", "SmokeScaffoldingExtensions.cs"));
            webExt.Should().Contain("\"Services:ApiBaseUrl\"");
            webExt.Should().Contain("\"X-Internal-Api-Key\"");
            webExt.Should().Contain(".AddHttpMessageHandler<UserIdentityHandler>()");
            webExt.Should().NotContain("\"Api:BaseUrl\"",
                "the wrapper must overwrite the template's wrong-convention emit");

            // d. API registration extension was emitted (not produced by the template at all).
            var apiExt = File.ReadAllText(Path.Combine(tempDir, "Services", "SmokeApiRegistrationExtensions.cs"));
            apiExt.Should().Contain("public static IServiceCollection AddSmokeApiServices(");
            apiExt.Should().Contain("services.AddScoped<ISmokeService, SmokeService>();");
        }
        finally
        {
            TryDeleteDirectory(tempDir);
            TryDeleteDirectory(hiveDir);
        }
    }

    // ─── Helpers ────────────────────────────────────────────────────────────

    private static (int exitCode, string output) RunCrudSlice(string args)
    {
        var dllPath = LocateCrudSliceDll();
        Assert.True(dllPath is not null, "Dabbler.CrudSlice.dll not found alongside the test assembly.");
        return RunTool("dotnet", $"\"{dllPath}\" {args}", AppContext.BaseDirectory);
    }

    private static string? LocateCrudSliceDll()
    {
        // The test project ProjectReferences Dabbler.CrudSlice so the dll is copied into
        // the test bin/ output. Find it next to the test assembly.
        var path = Path.Combine(AppContext.BaseDirectory, "Dabbler.CrudSlice.dll");
        return File.Exists(path) ? path : null;
    }

    private static string? LocatePackOutputDir()
    {
        var fromEnv = Environment.GetEnvironmentVariable("DABBLER_PACK_OUTPUT");
        if (!string.IsNullOrWhiteSpace(fromEnv) && Directory.Exists(fromEnv))
            return fromEnv;
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "pack-output");
            if (Directory.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        return null;
    }

    private static string? LocateSmokeAppFixture()
    {
        // tests/Dabbler.Templates.Tests/Fixtures/SmokeApp lives next to the source tree.
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "tests", "Dabbler.Templates.Tests", "Fixtures", "SmokeApp");
            if (Directory.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        return null;
    }

    private static string? LocateSmokeSchema()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "tests", "Dabbler.Templates.Tests", "Fixtures", "smoke.dabbler-entity.json");
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        return null;
    }

    private static string? FindExactNupkg(string packOutputDir, string packageId)
    {
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
        foreach (var d in Directory.GetDirectories(source))
            CopyDirectory(d, Path.Combine(destination, Path.GetFileName(d)));
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
        using var p = Process.Start(psi)!;
        var sb = new StringBuilder();
        p.OutputDataReceived += (_, e) => { if (e.Data is not null) sb.AppendLine(e.Data); };
        p.ErrorDataReceived  += (_, e) => { if (e.Data is not null) sb.AppendLine(e.Data); };
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();
        if (!p.WaitForExit(DefaultTimeout))
        {
            try { p.Kill(entireProcessTree: true); } catch { /* best effort */ }
            return (-1, $"TIMEOUT.\n{sb}");
        }
        p.WaitForExit();
        return (p.ExitCode, sb.ToString());
    }

    private static void TryDeleteDirectory(string path)
    {
        try { Directory.Delete(path, recursive: true); } catch { /* best effort */ }
    }
}
