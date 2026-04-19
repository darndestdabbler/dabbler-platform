namespace AcmeCoffeeOnline.Web.Tests;

public sealed class WorkflowAssetSmokeTests
{
    [Fact]
    public void AiRouterConfig_LoadsNestedPromptTemplates()
    {
        var repoRoot = GetRepoRoot();
        var configText = ReadRepoFile(repoRoot, "ai-router", "config.py");
        var routerConfig = ReadRepoFile(repoRoot, "ai-router", "router-config.yaml");
        var routerInit = ReadRepoFile(repoRoot, "ai-router", "__init__.py");

        Assert.Contains("config_dir / \"prompt-templates\"", configText);
        Assert.Contains("config_dir.parent / \"prompt-templates\"", configText);
        Assert.Contains("system_prompt_file: prompt-templates/google-system.md", routerConfig);
        Assert.Contains("prompt_template_file: prompt-templates/verification.md", routerConfig);
        Assert.Contains("session_verification_started", routerInit);
        Assert.Contains("session_verification_completed", routerInit);
        Assert.Contains("_log_session_verification_event", routerInit);

        string[] requiredFiles =
        [
            "ai-router/prompt-templates/google-system.md",
            "ai-router/prompt-templates/anthropic-system.md",
            "ai-router/prompt-templates/task-types/documentation.md",
            "ai-router/prompt-templates/verification.md",
            "ai-router/utils/load_env.py",
        ];

        foreach (var requiredFile in requiredFiles)
        {
            Assert.True(
                File.Exists(Path.Combine(repoRoot, requiredFile)),
                $"Missing workflow asset: {requiredFile}");
        }
    }

    [Fact]
    public void RepoRoot_ResolvesToSolutionDirectory()
    {
        var repoRoot = GetRepoRoot();
        Assert.True(
            File.Exists(Path.Combine(repoRoot, "Dabbler.Platform.sln")),
            "The workflow asset smoke tests must resolve the repository root to the solution directory.");
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
}
