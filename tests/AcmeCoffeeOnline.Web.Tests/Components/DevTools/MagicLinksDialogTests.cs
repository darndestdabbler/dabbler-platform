using System.Security.Claims;
using AcmeCoffeeOnline.Web.Components.Shared.DevTools;
using AcmeCoffeeOnline.Web.Configuration;
using AcmeCoffeeOnline.Web.Services.DevTools;
using AcmeCoffeeOnline.Web.Tests.Infrastructure;
using Bunit;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MudBlazor;
using MudBlazor.Services;

namespace AcmeCoffeeOnline.Web.Tests.Components.DevTools;

public sealed class MagicLinksDialogTests : BunitContext, IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();

    private readonly DevEmailInbox _inbox = new();

    public MagicLinksDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();

        var authOptions = new AppAuthenticationOptions
        {
            SeededSession = new SeededSessionOptions
            {
                Enabled = true,
                SharedSecret = "test-secret",
                AllowedRoles = ["SystemAdmin", "BackofficeUser", "Customer"]
            }
        };
        Services.AddSingleton<IOptions<AppAuthenticationOptions>>(Options.Create(authOptions));
        Services.AddSingleton(_inbox);
        Services.AddSingleton<IUserSessionContextService, StubUserSessionContextService>();
        Services.AddSingleton<AuthenticationStateProvider>(
            new TestAuthenticationStateProvider(new ClaimsPrincipal(new ClaimsIdentity())));
        Services.AddScoped<TestAuthClient>();
    }

    [Fact]
    public void EmptyInbox_ShowsEmptyStateMessage()
    {
        var cut = OpenDialog();

        cut.WaitForAssertion(() =>
            Assert.NotNull(cut.Find("[data-testid='dev-tools-magic-empty']")));
    }

    [Fact]
    public void PopulatedInbox_RendersRows()
    {
        _inbox.Add(new DevEmailMessage(
            "MagicLink", "alpha@example.com", "123456",
            "https://app.local/login?token=alpha",
            QrCodeDataUri: null, DateTimeOffset.UtcNow));
        _inbox.Add(new DevEmailMessage(
            "MagicLink", "beta@example.com", "654321",
            "https://app.local/login?token=beta",
            QrCodeDataUri: null, DateTimeOffset.UtcNow));

        var cut = OpenDialog();

        cut.WaitForAssertion(() =>
            Assert.NotNull(cut.Find("[data-testid='dev-tools-magic-table']")));

        var markup = cut.Markup;
        Assert.Contains("alpha@example.com", markup);
        Assert.Contains("beta@example.com", markup);
    }

    [Fact]
    public void NonMagicLinkMessages_AreExcluded()
    {
        _inbox.Add(new DevEmailMessage(
            "VerificationCode", "alpha@example.com", "999999",
            LinkUrl: null, QrCodeDataUri: null, DateTimeOffset.UtcNow));

        var cut = OpenDialog();

        cut.WaitForAssertion(() =>
            Assert.NotNull(cut.Find("[data-testid='dev-tools-magic-empty']")));
    }

    [Fact]
    public void FilterInput_NarrowsVisibleRows()
    {
        _inbox.Add(new DevEmailMessage(
            "MagicLink", "alpha@example.com", "111111",
            "https://app.local/login?token=alpha",
            QrCodeDataUri: null, DateTimeOffset.UtcNow));
        _inbox.Add(new DevEmailMessage(
            "MagicLink", "beta@example.com", "222222",
            "https://app.local/login?token=beta",
            QrCodeDataUri: null, DateTimeOffset.UtcNow));

        var cut = OpenDialog();
        cut.WaitForAssertion(() =>
            Assert.NotNull(cut.Find("[data-testid='dev-tools-magic-table']")));

        var input = cut.Find("#dev-tools-magic-filter");
        input.Input("alpha");

        cut.WaitForAssertion(() =>
        {
            var markup = cut.Markup;
            Assert.Contains("alpha@example.com", markup);
            Assert.DoesNotContain("beta@example.com", markup);
        });
    }

    [Fact]
    public void ClearButton_EmptiesInbox()
    {
        _inbox.Add(new DevEmailMessage(
            "MagicLink", "alpha@example.com", "111111",
            "https://app.local/login?token=alpha",
            QrCodeDataUri: null, DateTimeOffset.UtcNow));

        var cut = OpenDialog();
        cut.WaitForAssertion(() =>
            Assert.NotNull(cut.Find("[data-testid='dev-tools-magic-table']")));

        cut.Find("[data-testid='dev-tools-magic-clear']").Click();

        cut.WaitForAssertion(() =>
            Assert.NotNull(cut.Find("[data-testid='dev-tools-magic-empty']")));
        Assert.Empty(_inbox.GetAll());
    }

    [Fact]
    public async Task CopyButton_InvokesClipboardJsInterop()
    {
        _inbox.Add(new DevEmailMessage(
            "MagicLink", "alpha@example.com", "111111",
            "https://app.local/login?token=alpha",
            QrCodeDataUri: null, DateTimeOffset.UtcNow));

        var copyInvocation = JSInterop.SetupVoid("navigator.clipboard.writeText", _ => true);

        var cut = OpenDialog();
        cut.WaitForAssertion(() =>
            Assert.NotNull(cut.Find("[data-testid='dev-tools-magic-table']")));

        cut.Find("[data-testid='dev-tools-magic-copy']").Click();

        await cut.InvokeAsync(() => { });
        var invocation = Assert.Single(copyInvocation.Invocations);
        Assert.Equal("https://app.local/login?token=alpha", invocation.Arguments[0]);
    }

    private IRenderedComponent<DialogHost> OpenDialog()
    {
        var host = Render<DialogHost>();
        host.InvokeAsync(() => host.Instance.Show()).GetAwaiter().GetResult();
        return host;
    }

    private sealed class StubUserSessionContextService : IUserSessionContextService
    {
        public Task<UserSessionContext?> BuildAsync(
            int userId, int? requestedOrganizationId = null, CancellationToken ct = default)
            => Task.FromResult<UserSessionContext?>(null);

        public Task<ClaimsPrincipal?> CreatePrincipalAsync(
            int userId,
            string authenticationScheme = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme,
            int? requestedOrganizationId = null,
            IReadOnlyList<string>? overrideRoles = null,
            CancellationToken ct = default)
            => Task.FromResult<ClaimsPrincipal?>(null);
    }

    public sealed class DialogHost : ComponentBase
    {
        [Inject] private IDialogService DialogService { get; set; } = null!;

        public async Task Show()
        {
            await DialogService.ShowAsync<MagicLinksDialog>(
                "Magic Links",
                new DialogOptions { FullWidth = true });
        }

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenComponent<MudThemeProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<MudPopoverProvider>(1);
            builder.CloseComponent();
            builder.OpenComponent<MudDialogProvider>(2);
            builder.CloseComponent();
            builder.OpenComponent<MudSnackbarProvider>(3);
            builder.CloseComponent();
        }
    }
}
