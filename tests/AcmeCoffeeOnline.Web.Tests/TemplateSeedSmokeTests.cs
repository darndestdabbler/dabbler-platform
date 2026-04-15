namespace AcmeCoffeeOnline.Web.Tests;

public sealed class TemplateSeedSmokeTests
{
    [Fact]
    public void TemplateSeed_IncludesWorkflowAssetsWithoutApplicationScaffolding()
    {
        var repoRoot = GetRepoRoot();
        var templateRoot = Path.Combine(repoRoot, "template-seed");

        string[] requiredFiles =
        [
            "README.md",
            "AGENTS.md",
            "CLAUDE.md",
            "GEMINI.md",
            "docs/ai-led-session-workflow.md",
            "docs/session-sets/starter-feature/spec.md",
            "scripts/replace-placeholders.ps1",
            "ai-router/__init__.py",
            "ai-router/router-config.yaml",
            "ai-router/session_log.py"
        ];

        foreach (var requiredFile in requiredFiles)
        {
            Assert.True(
                File.Exists(Path.Combine(templateRoot, requiredFile)),
                $"Missing template workflow asset: {requiredFile}");
        }

        Assert.False(Directory.Exists(Path.Combine(templateRoot, "src")));
        Assert.False(Directory.Exists(Path.Combine(templateRoot, "tests")));
        Assert.False(Directory.Exists(Path.Combine(templateRoot, "docker")));
    }

    [Fact]
    public void TemplateSeedReadme_MakesMinimalStarterContractExplicit()
    {
        var repoRoot = GetRepoRoot();
        var readme = NormalizeWhitespace(ReadRepoFile(repoRoot, "template-seed", "README.md"));

        Assert.Contains("This seed stays workflow-focused.", readme);
        Assert.Contains("It does not scaffold application projects, layouts, pages, or default `Program.cs` wiring.", readme);
        Assert.Contains("start from focused shell, dashboard, query, detail, popup, report, and", readme);
        Assert.Contains("single-select lookup workflows", readme);
        Assert.Contains("do not silently scaffold `DabblerCatalogPage`", readme);
        Assert.Contains("`DabblerSecurityNav`", readme);
        Assert.Contains("`IArtifactCatalogProvider`", readme);
        Assert.Contains("favorites/recent-items", readme);
        Assert.Contains("query CSV export as an explicit app-level opt-in", readme);
        Assert.Contains("accepted PDF-export report baseline", readme);
        Assert.Contains("`DabblerMultiComboBox<T>` only when a concrete scaffolded workflow", readme);
    }

    [Fact]
    public void TemplateSeedInstructionFiles_RepeatTheNarrowedStarterDefaults()
    {
        var repoRoot = GetRepoRoot();

        foreach (var instructionFile in new[] { "AGENTS.md", "CLAUDE.md", "GEMINI.md" })
        {
            var text = NormalizeWhitespace(ReadRepoFile(repoRoot, "template-seed", instructionFile));

            Assert.Contains("starter contract", text);
            Assert.Contains("do not treat `DabblerCatalogPage`, `DabblerQueryReportPage<T>`", text);
            Assert.Contains("`PageViewMode`, `DabblerSecurityNav`, catalog/usage-tracking provider", text);
            Assert.Contains("favorites/recent-items widgets as starter defaults", text);
            Assert.Contains("query CSV export and multiselect lookup support as explicit", text);
            Assert.Contains("report defaults centered on PDF export", text);
        }
    }

    [Fact]
    public void TemplateSeedStarterSpec_RequiresExplicitJustificationForAdvancedDefaults()
    {
        var repoRoot = GetRepoRoot();
        var spec = NormalizeWhitespace(ReadRepoFile(repoRoot, "template-seed", "docs", "session-sets", "starter-feature", "spec.md"));

        Assert.Contains("## Bootstrap Standards", spec);
        Assert.Contains("retained core workflow surfaces", spec);
        Assert.Contains("do not reintroduce compatibility-only defaults", spec);
        Assert.Contains("`DabblerCatalogPage`, `DabblerQueryReportPage<T>`, `PageViewMode`", spec);
        Assert.Contains("`DabblerSecurityNav`, catalog/usage-tracking shell wiring, or dashboard", spec);
        Assert.Contains("query CSV export as an app-level opt-in", spec);
        Assert.Contains("report defaults centered on PDF export", spec);
        Assert.Contains("`DabblerMultiComboBox<T>` only when a concrete starter workflow", spec);
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

    private static string ReadRepoFile(string repoRoot, params string[] relativePath)
    {
        return File.ReadAllText(Path.Combine([repoRoot, .. relativePath]));
    }

    private static string NormalizeWhitespace(string text)
    {
        return text.Replace("\r", " ").Replace("\n", " ");
    }
}
