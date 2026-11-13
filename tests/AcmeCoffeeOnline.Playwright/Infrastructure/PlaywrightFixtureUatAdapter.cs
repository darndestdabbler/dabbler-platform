using Dabbler.Uat.Dsl.Emitters.Playwright.CSharp;
using Microsoft.Playwright;

namespace AcmeCoffeeOnline.Playwright.Infrastructure;

/// <summary>
/// Bridges the existing <see cref="PlaywrightFixture"/> to the
/// <see cref="IPlaywrightUatFixture"/> contract that emitted W0 test code
/// expects.
/// </summary>
/// <remarks>
/// WaitForQuietAsync and WaitForReadyAsync delegate to the underlying
/// fixture so DSL-compiled tests get the real synchronization. The previous
/// stub-as-Task.CompletedTask shape predated the watermark / Blazor-ready
/// signals being reliable; today both are wired and required. The W2
/// emitter places WaitForQuietAsync after every step, and pre-auth pages
/// like /login need at least the Blazor-ready gate to keep FillAsync from
/// racing the SignalR circuit attach (the bound @bind-Value handler isn't
/// subscribed yet, so the typed input never reaches the C# field and the
/// downstream form-validation gate stays closed).
/// SeededSignInAsync translates the W0 contract's IReadOnlyList&lt;string&gt;
/// roles into the comma-separated form the existing fixture endpoint accepts.
/// </remarks>
internal sealed class PlaywrightFixtureUatAdapter : IPlaywrightUatFixture
{
    private readonly PlaywrightFixture _inner;

    public PlaywrightFixtureUatAdapter(PlaywrightFixture inner) => _inner = inner;

    public Task WaitForQuietAsync(IPage page) => _inner.WaitForQuietAsync(page);

    public Task WaitForReadyAsync(IPage page) => _inner.WaitForReadyAsync(page);

    public Task SeededSignInAsync(IPage page, string email, IReadOnlyList<string>? roles = null)
    {
        var rolesCsv = roles is { Count: > 0 } ? string.Join(",", roles) : null;
        return _inner.SeededSignInAsync(page, email, rolesCsv);
    }
}
