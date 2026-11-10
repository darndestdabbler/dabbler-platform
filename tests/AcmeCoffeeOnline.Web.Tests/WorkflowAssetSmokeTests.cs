namespace AcmeCoffeeOnline.Web.Tests;

public sealed class WorkflowAssetSmokeTests
{
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
