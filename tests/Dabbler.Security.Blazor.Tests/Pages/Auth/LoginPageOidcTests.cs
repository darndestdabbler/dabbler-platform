using Bunit;
using Dabbler.Security.Blazor.Tests.TestHelpers;
using Dabbler.Security.Options;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;

namespace Dabbler.Security.Blazor.Tests.Pages.Auth;

public class LoginPageOidcTests : IDisposable
{
    private readonly TestContext _ctx;
    private readonly MockHttpMessageHandler _mock;

    public LoginPageOidcTests()
    {
        _mock = new MockHttpMessageHandler();
        _ctx = new TestContext();
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _mock.Dispose();
    }

    private void Setup(Action<DabblerSecurityOptions>? configureOptions = null)
    {
        _ctx.SetupForDabbler(configureServices: services =>
        {
            var client = _mock.ToHttpClient();
            client.BaseAddress = new Uri("http://localhost");
            services.AddSingleton(client);

            if (configureOptions is not null)
            {
                services.Configure(configureOptions);
            }
        });

        var navMan = _ctx.Services.GetRequiredService<NavigationManager>();
        navMan.NavigateTo("http://localhost/login");
    }

    [Fact]
    public void LoginPage_WithEntraId_ShowsOidcButton()
    {
        Setup(opts => opts.AddEntraId(o =>
        {
            o.Authority = "https://login.test/v2.0";
            o.ClientId = "test-client-id";
        }));

        var cut = _ctx.RenderComponent<Dabbler.Security.Blazor.Pages.Auth.LoginPage>();

        cut.Markup.Should().Contain("Sign in with your organization account");
    }

    [Fact]
    public void LoginPage_WithCustomDisplayName_ShowsCustomText()
    {
        Setup(opts => opts.AddEntraId(o =>
        {
            o.Authority = "https://login.test/v2.0";
            o.ClientId = "test-client-id";
            o.DisplayName = "Sign in with Contoso";
        }));

        var cut = _ctx.RenderComponent<Dabbler.Security.Blazor.Pages.Auth.LoginPage>();

        cut.Markup.Should().Contain("Sign in with Contoso");
    }

    [Fact]
    public void LoginPage_NoProviders_HidesOidcButton()
    {
        Setup();

        var cut = _ctx.RenderComponent<Dabbler.Security.Blazor.Pages.Auth.LoginPage>();

        cut.Markup.Should().NotContain("Sign in with your organization account");
        cut.Markup.Should().Contain("Send Login Link");
    }

    [Fact]
    public void LoginPage_WithEntraId_ShowsDivider()
    {
        Setup(opts => opts.AddEntraId(o =>
        {
            o.Authority = "https://login.test/v2.0";
            o.ClientId = "test-client-id";
        }));

        var cut = _ctx.RenderComponent<Dabbler.Security.Blazor.Pages.Auth.LoginPage>();

        cut.Markup.Should().Contain("or sign in with email");
    }

    [Fact]
    public void LoginPage_NoProviders_HidesDivider()
    {
        Setup();

        var cut = _ctx.RenderComponent<Dabbler.Security.Blazor.Pages.Auth.LoginPage>();

        cut.Markup.Should().NotContain("or sign in with email");
    }

    [Fact]
    public void LoginPage_MultipleOAuthProviders_ShowsAllButtons()
    {
        Setup(opts =>
        {
            opts.AddExternalOAuth("Google", o =>
            {
                o.ClientId = "google-id";
                o.DisplayName = "Sign in with Google";
            });
            opts.AddExternalOAuth("GitHub", o =>
            {
                o.ClientId = "github-id";
                o.DisplayName = "Sign in with GitHub";
            });
        });

        var cut = _ctx.RenderComponent<Dabbler.Security.Blazor.Pages.Auth.LoginPage>();

        cut.Markup.Should().Contain("Sign in with Google");
        cut.Markup.Should().Contain("Sign in with GitHub");
    }

    [Fact]
    public void LoginPage_OidcButtonClick_NavigatesToChallengeEndpoint()
    {
        Setup(opts => opts.AddEntraId(o =>
        {
            o.Authority = "https://login.test/v2.0";
            o.ClientId = "test-client-id";
        }));

        var cut = _ctx.RenderComponent<Dabbler.Security.Blazor.Pages.Auth.LoginPage>();

        var providerButton = cut.FindAll("button")
            .FirstOrDefault(b => b.TextContent.Contains("Sign in with your organization account"));
        providerButton.Should().NotBeNull("OIDC button should be present");
        providerButton!.Click();

        var navMan = _ctx.Services.GetRequiredService<NavigationManager>();
        navMan.Uri.Should().Contain("/auth/challenge/EntraId");
    }

    [Fact]
    public void LoginPage_WithEntraId_StillShowsMagicLinkForm()
    {
        Setup(opts => opts.AddEntraId(o =>
        {
            o.Authority = "https://login.test/v2.0";
            o.ClientId = "test-client-id";
        }));

        var cut = _ctx.RenderComponent<Dabbler.Security.Blazor.Pages.Auth.LoginPage>();

        cut.Markup.Should().Contain("Send Login Link");
        cut.Markup.Should().Contain("Email Address");
    }
}
