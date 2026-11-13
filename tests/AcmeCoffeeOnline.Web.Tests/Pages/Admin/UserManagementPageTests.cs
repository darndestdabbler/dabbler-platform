using System.Net;
using System.Security.Claims;
using AcmeCoffeeOnline.Shared.DTOs;
using AcmeCoffeeOnline.Web.Components.Pages.Admin;
using AcmeCoffeeOnline.Web.Services;
using AcmeCoffeeOnline.Web.Tests.Infrastructure;
using Bunit;
using Bunit.Rendering;
using Dabbler.Security.Authorization;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace AcmeCoffeeOnline.Web.Tests.Pages.Admin;

public sealed class UserManagementPageTests : BunitContext, IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();

    [Fact]
    public void RendersActiveScopeBannerAndScopedUsersFromApiClient()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();

        var handler = new StubHttpMessageHandler((request, _) =>
        {
            return Task.FromResult(request.RequestUri?.PathAndQuery == "/api/admin/users/scope"
                ? StubHttpMessageHandler.Json(HttpStatusCode.OK, CreateScope(canManageUsers: true, hasScopedAdmin: false))
                : StubHttpMessageHandler.Empty(HttpStatusCode.NotFound));
        });
        Services.AddScoped(_ => new AdminUserApiClient(CreateHttpClient(handler)));

        var cut = RenderPage();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("data-testid=\"admin-scope-banner\"", cut.Markup);
            Assert.Contains("Guardrail Retail Allowed", cut.Markup);
            Assert.Contains("guardrail.org.scope.member@acme.test", cut.Markup);
            Assert.Contains("Grant UserAdmin", cut.Markup);
        });
    }

    [Fact]
    public void GrantUserAdminAction_RefreshesRenderedScope()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();

        var hasScopedAdmin = false;
        var handler = new StubHttpMessageHandler((request, _) =>
        {
            if (request.RequestUri?.PathAndQuery == "/api/admin/users/scope")
            {
                return Task.FromResult(StubHttpMessageHandler.Json(
                    HttpStatusCode.OK,
                    CreateScope(canManageUsers: true, hasScopedAdmin: hasScopedAdmin)));
            }

            if (request.Method == HttpMethod.Post
                && request.RequestUri?.PathAndQuery == "/api/admin/users/memberships/41/grant-user-admin")
            {
                hasScopedAdmin = true;
                return Task.FromResult(StubHttpMessageHandler.Json(
                    HttpStatusCode.OK,
                    CreateScope(canManageUsers: true, hasScopedAdmin: true)));
            }

            return Task.FromResult(StubHttpMessageHandler.Empty(HttpStatusCode.NotFound));
        });
        Services.AddScoped(_ => new AdminUserApiClient(CreateHttpClient(handler)));

        var cut = RenderPage();
        cut.WaitForAssertion(() => Assert.Contains("Grant UserAdmin", cut.Markup));

        cut.FindAll("button")
            .Single(button => button.TextContent.Contains("Grant UserAdmin", StringComparison.Ordinal))
            .Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Remove UserAdmin", cut.Markup);
            Assert.Contains(handler.Requests, request =>
                request.Method == HttpMethod.Post
                && request.PathAndQuery == "/api/admin/users/memberships/41/grant-user-admin");
        });
    }

    [Fact]
    public void RendersWarningWhenActiveScopeCannotManageUsers()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();

        var handler = new StubHttpMessageHandler((request, _) =>
        {
            return Task.FromResult(request.RequestUri?.PathAndQuery == "/api/admin/users/scope"
                ? StubHttpMessageHandler.Json(HttpStatusCode.OK, CreateScope(canManageUsers: false, hasScopedAdmin: false))
                : StubHttpMessageHandler.Empty(HttpStatusCode.NotFound));
        });
        Services.AddScoped(_ => new AdminUserApiClient(CreateHttpClient(handler)));

        var cut = RenderPage();

        cut.WaitForAssertion(() =>
            Assert.Contains("Switch into an organization where you hold user-admin access", cut.Markup));
    }

    [Theory]
    [InlineData("SystemAdmin", true)]
    [InlineData("CrossOrgUserAdmin", true)]
    [InlineData("UserAdmin", true)]
    [InlineData("CrossOrgMonitor", false)]
    [InlineData(null, false)]
    public void DashboardBanner_VisibilityMatchesActorRole(string? activeRole, bool expectBanner)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();

        var handler = new StubHttpMessageHandler((request, _) =>
            Task.FromResult(request.RequestUri?.PathAndQuery == "/api/admin/users/scope"
                ? StubHttpMessageHandler.Json(HttpStatusCode.OK, CreateScope(canManageUsers: false, hasScopedAdmin: false))
                : StubHttpMessageHandler.Empty(HttpStatusCode.NotFound)));
        Services.AddScoped(_ => new AdminUserApiClient(CreateHttpClient(handler)));

        var cut = RenderPage(activeRole);

        cut.WaitForAssertion(() =>
        {
            if (expectBanner)
                Assert.Contains("data-testid=\"user-admin-dashboard-banner\"", cut.Markup);
            else
                Assert.DoesNotContain("data-testid=\"user-admin-dashboard-banner\"", cut.Markup);
        });
    }

    private static HttpClient CreateHttpClient(HttpMessageHandler handler)
        => new(handler)
        {
            BaseAddress = new Uri("https://acme.test/")
        };

    private IRenderedComponent<ContainerFragment> RenderPage(string? activeRole = null)
    {
        Services.AddSingleton<IBakedInRoleProvider, PlatformBakedInRoleProvider>();
        Services.AddSingleton<IRoleSwitcher>(new StubRoleSwitcher(activeRole));
        Services.AddScoped<AuthenticationStateProvider>(_ =>
            new StubAuthStateProvider(activeRole));
        return Render(builder =>
        {
            builder.OpenComponent<MudThemeProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<MudPopoverProvider>(1);
            builder.CloseComponent();
            builder.OpenComponent<MudSnackbarProvider>(2);
            builder.CloseComponent();
            builder.OpenComponent<UserManagement>(3);
            builder.CloseComponent();
        });
    }

    private sealed class StubRoleSwitcher(string? activeRole) : IRoleSwitcher
    {
        public bool IsRoleSwitched => activeRole is not null;
        public IEnumerable<string> EffectiveRoles(ClaimsPrincipal user)
            => activeRole is not null ? [activeRole] : [];
        public void SwitchToRole(string roleName) { }
        public void ClearSwitch() { }
    }

    private sealed class StubAuthStateProvider(string? role) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            ClaimsPrincipal user = role is not null
                ? new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.Name, "test"), new Claim(ClaimTypes.Role, role)],
                    "test"))
                : new ClaimsPrincipal(new ClaimsIdentity());
            return Task.FromResult(new AuthenticationState(user));
        }
    }

    private static AdminUserScopeDto CreateScope(bool canManageUsers, bool hasScopedAdmin)
        => new()
        {
            CanManageUsers = canManageUsers,
            IsSystemAdmin = false,
            ActiveOrganizationId = canManageUsers ? 7 : null,
            ActiveOrganizationKey = canManageUsers ? "guardrail-retail-allowed" : null,
            ActiveOrganizationName = canManageUsers ? "Guardrail Retail Allowed" : null,
            Users = canManageUsers
                ?
                [
                    new AdminUserScopeUserDto
                    {
                        UserId = 12,
                        OrganizationMembershipId = 41,
                        DisplayName = "Guardrail Scope Member",
                        Email = "guardrail.org.scope.member@acme.test",
                        IsActive = true,
                        ScopedRoles = hasScopedAdmin ? ["BackofficeUserAdmin"] : ["Customer"],
                        OtherOrganizations = [],
                        HasScopedUserAdmin = hasScopedAdmin,
                        CanGrantUserAdmin = !hasScopedAdmin,
                        CanRevokeUserAdmin = hasScopedAdmin
                    }
                ]
                : []
        };
}
