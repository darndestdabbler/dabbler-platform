using System.Net;
using AcmeCoffeeOnline.Shared.DTOs;
using AcmeCoffeeOnline.Web.Components.Pages.Admin;
using AcmeCoffeeOnline.Web.Services;
using AcmeCoffeeOnline.Web.Tests.Infrastructure;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace AcmeCoffeeOnline.Web.Tests.Pages.Admin;

public sealed class UserManagementPageTests : TestContext
{
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

    private static HttpClient CreateHttpClient(HttpMessageHandler handler)
        => new(handler)
        {
            BaseAddress = new Uri("https://acme.test/")
        };

    private IRenderedFragment RenderPage()
        => Render(builder =>
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
