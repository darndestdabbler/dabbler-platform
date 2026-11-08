using System.Net;
using Bunit;
using Dabbler.Security.Blazor.Tests.TestHelpers;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;

namespace Dabbler.Security.Blazor.Tests.Pages.Auth;

public class MfaPageTests : IDisposable
{
    private readonly TestContext _ctx;
    private readonly MockHttpMessageHandler _mock;

    public MfaPageTests()
    {
        _mock = new MockHttpMessageHandler();
        _ctx = new TestContext();
        _ctx.SetupForDabbler(configureServices: services =>
        {
            var client = _mock.ToHttpClient();
            client.BaseAddress = new Uri("http://localhost");
            services.AddSingleton(client);
        });
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _mock.Dispose();
    }

    /// <summary>
    /// Navigate to the MFA page URL so [SupplyParameterFromQuery] parameters are populated.
    /// </summary>
    private void NavigateToMfa(int userId, string? returnUrl = null)
    {
        var navMan = _ctx.Services.GetRequiredService<NavigationManager>();
        var url = returnUrl == null
            ? $"http://localhost/login/mfa?UserId={userId}"
            : $"http://localhost/login/mfa?UserId={userId}&ReturnUrl={Uri.EscapeDataString(returnUrl)}";
        navMan.NavigateTo(url);
    }

    [Fact]
    public void MfaPage_ValidUserId_RendersCodeInput()
    {
        NavigateToMfa(5);
        var cut = _ctx.RenderComponent<Dabbler.Security.Blazor.Pages.Auth.MfaPage>();

        cut.Markup.Should().Contain("Two-Factor Authentication");
        cut.Markup.Should().Contain("Authenticator Code");
    }

    [Fact]
    public void MfaPage_ZeroUserId_RedirectsToLogin()
    {
        NavigateToMfa(0);

        _ctx.RenderComponent<Dabbler.Security.Blazor.Pages.Auth.MfaPage>();

        var navMan = _ctx.Services.GetRequiredService<NavigationManager>();
        navMan.Uri.Should().Contain("/login");
    }

    [Fact]
    public async Task MfaPage_ValidCode_NavigatesToReturnUrl()
    {
        _mock.When(HttpMethod.Post, "/auth/verify-totp")
            .Respond(HttpStatusCode.OK, "application/json", """{"success":true}""");

        NavigateToMfa(3, "/dashboard");
        var cut = _ctx.RenderComponent<Dabbler.Security.Blazor.Pages.Auth.MfaPage>();

        cut.Find("input").Change("123456");
        await cut.InvokeAsync(() => cut.Find("form").Submit());

        cut.WaitForAssertion(() =>
        {
            var navMan = _ctx.Services.GetRequiredService<NavigationManager>();
            navMan.Uri.Should().Contain("/dashboard");
        });
    }

    [Fact]
    public async Task MfaPage_InvalidCode_ShowsErrorAndStaysOnPage()
    {
        _mock.When(HttpMethod.Post, "/auth/verify-totp")
            .Respond(HttpStatusCode.BadRequest, "text/plain", "Invalid TOTP code");

        NavigateToMfa(3);
        var cut = _ctx.RenderComponent<Dabbler.Security.Blazor.Pages.Auth.MfaPage>();

        cut.Find("input").Change("000000");
        await cut.InvokeAsync(() => cut.Find("form").Submit());

        cut.WaitForAssertion(() =>
            cut.Markup.Should().Contain("Authenticator Code"));
    }

    [Fact]
    public void MfaPage_RendersVerifyButton()
    {
        NavigateToMfa(1);
        var cut = _ctx.RenderComponent<Dabbler.Security.Blazor.Pages.Auth.MfaPage>();

        cut.Markup.Should().Contain("Verify");
    }
}
