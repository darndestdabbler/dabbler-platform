namespace AcmeCoffeeOnline.Playwright.Infrastructure;

/// <summary>
/// Collection for Playwright tests that have proven flaky under the full-
/// suite's cross-collection parallelism. Tests in this collection share the
/// main <see cref="PlaywrightFixture"/> but run serially AND with parallelism
/// disabled relative to other collections (<see cref="DisableParallelization"/>
/// = true), so they don't contend with the main collection's browser + circuit
/// + SQL Server load.
///
/// A test belongs here when it has a known race condition whose proper fix is
/// scoped to a future focused session. Each flaky test MUST carry a TODO
/// comment pointing at the tracked root cause so it doesn't sit here
/// indefinitely.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class FlakyPlaywrightCollection : ICollectionFixture<PlaywrightFixture>
{
    public const string Name = "flaky-playwright";
}
