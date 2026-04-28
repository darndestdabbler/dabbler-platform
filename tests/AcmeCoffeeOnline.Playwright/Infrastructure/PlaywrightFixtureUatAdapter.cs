using Dabbler.Uat.Dsl.Emitters.Playwright.CSharp;
using Microsoft.Playwright;

namespace AcmeCoffeeOnline.Playwright.Infrastructure;

/// <summary>
/// Bridges the existing <see cref="PlaywrightFixture"/> to the
/// <see cref="IPlaywrightUatFixture"/> contract that emitted W0 test code
/// expects. WaitForQuietAsync / WaitForReadyAsync are no-op stubs until W3
/// wires the watermark; the SeededSignInAsync overload here translates the
/// W0 contract's IReadOnlyList&lt;string&gt; roles into the comma-separated
/// form the existing fixture endpoint accepts.
/// </summary>
internal sealed class PlaywrightFixtureUatAdapter : IPlaywrightUatFixture
{
    private readonly PlaywrightFixture _inner;

    public PlaywrightFixtureUatAdapter(PlaywrightFixture inner) => _inner = inner;

    public Task WaitForQuietAsync(IPage page) => Task.CompletedTask;

    public Task WaitForReadyAsync(IPage page) => Task.CompletedTask;

    public Task SeededSignInAsync(IPage page, string email, IReadOnlyList<string>? roles = null)
    {
        var rolesCsv = roles is { Count: > 0 } ? string.Join(",", roles) : null;
        return _inner.SeededSignInAsync(page, email, rolesCsv);
    }
}
