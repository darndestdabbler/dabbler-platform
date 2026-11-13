using System.Net;
using Bunit;
using Dabbler.Security.Blazor.Tests.TestHelpers;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;

namespace Dabbler.Security.Blazor.Tests.Pages.Auth;

public class LoginPageTests : IAsyncLifetime
{
    private readonly BunitContext _ctx;
    private readonly MockHttpMessageHandler _mock;

    public LoginPageTests()
    {
        _mock = new MockHttpMessageHandler();
        _ctx = new BunitContext();
        _ctx.SetupForDabbler(configureServices: services =>
        {
            var client = _mock.ToHttpClient();
            client.BaseAddress = new Uri("http://localhost");
            services.AddSingleton(client);
        });
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _ctx.DisposeAsync();
        _mock.Dispose();
    }

    private void NavigateToLogin(string? token = null)
    {
        var navMan = _ctx.Services.GetRequiredService<NavigationManager>();
        var url = token == null
            ? "http://localhost/login"
            : $"http://localhost/login?Token={Uri.EscapeDataString(token)}";
        navMan.NavigateTo(url);
    }

    [Fact]
    public void LoginPage_InitialRender_ShowsEmailInput()
    {
        NavigateToLogin();
        var cut = _ctx.Render<Dabbler.Security.Blazor.Pages.Auth.LoginPage>();

        cut.Find("input").Should().NotBeNull();
        cut.Markup.Should().Contain("Sign In");
    }

    [Fact]
    public void LoginPage_InitialRender_ShowsSendLoginLinkButton()
    {
        NavigateToLogin();
        var cut = _ctx.Render<Dabbler.Security.Blazor.Pages.Auth.LoginPage>();

        cut.Markup.Should().Contain("Send Login Link");
    }

    [Fact]
    public async Task LoginPage_SendMagicLink_Success_ShowsCodeEntry()
    {
        _mock.When(HttpMethod.Post, "/auth/send-magic-link")
            .Respond(HttpStatusCode.OK, "application/json", "{}");

        NavigateToLogin();
        var cut = _ctx.Render<Dabbler.Security.Blazor.Pages.Auth.LoginPage>();

        cut.Find("input").Change("user@test.local");
        await cut.InvokeAsync(() => cut.Find("form").Submit());

        cut.WaitForAssertion(() =>
            (cut.Markup.Contains("Check your email") || cut.Markup.Contains("6-Digit Code"))
                .Should().BeTrue("email sent step should show the code entry"));
    }

    [Fact]
    public async Task LoginPage_SendMagicLink_Failure_StaysOnEmailStep()
    {
        _mock.When(HttpMethod.Post, "/auth/send-magic-link")
            .Respond(HttpStatusCode.InternalServerError);

        NavigateToLogin();
        var cut = _ctx.Render<Dabbler.Security.Blazor.Pages.Auth.LoginPage>();

        cut.Find("input").Change("user@test.local");
        await cut.InvokeAsync(() => cut.Find("form").Submit());

        cut.WaitForAssertion(() =>
            cut.Markup.Should().Contain("Send Login Link"));
    }

    [Fact]
    public async Task LoginPage_TokenQueryParam_CallsVerifyEndpoint()
    {
        _mock.Expect(HttpMethod.Post, "/auth/verify")
            .Respond(HttpStatusCode.OK, "application/json",
                """{"requiresMfa":false,"success":true}""");

        NavigateToLogin(token: "auto-token-abc123");
        _ctx.Render<Dabbler.Security.Blazor.Pages.Auth.LoginPage>();

        // Wait briefly for OnInitializedAsync to call /auth/verify
        await Task.Delay(200);
        // Verify the endpoint was called
        _mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task LoginPage_VerifyCode_MfaRequired_NavigatesToMfaPage()
    {
        _mock.When(HttpMethod.Post, "/auth/send-magic-link")
            .Respond(HttpStatusCode.OK, "application/json", "{}");
        _mock.When(HttpMethod.Post, "/auth/verify")
            .Respond(HttpStatusCode.OK, "application/json",
                """{"requiresMfa":true,"userId":7}""");

        NavigateToLogin();
        var cut = _ctx.Render<Dabbler.Security.Blazor.Pages.Auth.LoginPage>();

        // Step 1: request magic link
        cut.Find("input").Change("mfa@test.local");
        await cut.InvokeAsync(() => cut.Find("form").Submit());
        cut.WaitForAssertion(() =>
            (cut.Markup.Contains("Check your email") || cut.Markup.Contains("6-Digit Code"))
                .Should().BeTrue());

        // Step 2: enter code
        cut.Find("input").Change("123456");
        await cut.InvokeAsync(() => cut.Find("form").Submit());

        cut.WaitForAssertion(() =>
            _ctx.Services.GetRequiredService<NavigationManager>()
                .Uri.Should().Contain("/login/mfa"));
    }

    [Fact]
    public async Task LoginPage_VerifyCode_Success_NavigatesToHome()
    {
        _mock.When(HttpMethod.Post, "/auth/send-magic-link")
            .Respond(HttpStatusCode.OK, "application/json", "{}");
        _mock.When(HttpMethod.Post, "/auth/verify")
            .Respond(HttpStatusCode.OK, "application/json",
                """{"requiresMfa":false,"success":true}""");

        NavigateToLogin();
        var cut = _ctx.Render<Dabbler.Security.Blazor.Pages.Auth.LoginPage>();

        cut.Find("input").Change("user@test.local");
        await cut.InvokeAsync(() => cut.Find("form").Submit());
        cut.WaitForAssertion(() =>
            (cut.Markup.Contains("Check your email") || cut.Markup.Contains("6-Digit Code"))
                .Should().BeTrue());

        cut.Find("input").Change("123456");
        await cut.InvokeAsync(() => cut.Find("form").Submit());

        cut.WaitForAssertion(() =>
        {
            var uri = _ctx.Services.GetRequiredService<NavigationManager>().Uri;
            (uri == "http://localhost/" || uri == "http://localhost")
                .Should().BeTrue("successful login navigates to home");
        });
    }
}
