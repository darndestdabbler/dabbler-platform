using System.Net;
using System.Security.Claims;
using AcmeCoffeeOnline.Shared.DTOs;
using AcmeCoffeeOnline.Web.Services;
using AcmeCoffeeOnline.Web.Tests.Infrastructure;
using Dabbler.Security.Services;

namespace AcmeCoffeeOnline.Web.Tests.Services;

public sealed class ShellSessionStateLoaderTests
{
    [Fact]
    public async Task LoadAsync_ForUnauthenticatedUser_ReturnsDefaultShellState()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            throw new InvalidOperationException("Organization options should not be loaded for anonymous users."));
        var loader = CreateLoader(new ClaimsPrincipal(new ClaimsIdentity()), handler);

        var state = await loader.LoadAsync();

        Assert.Equal(string.Empty, state.UserId);
        Assert.Equal("User", state.UserName);
        Assert.Empty(state.UserRoles);
        Assert.False(state.IsAdmin);
        Assert.False(state.IsSystemAdmin);
        Assert.Null(state.ActiveOrganizationKey);
        Assert.Null(state.ActiveOrganizationName);
        Assert.Empty(state.OrganizationOptions);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task LoadAsync_ForSystemAdmin_LoadsOrganizationOptionsAndFlags()
    {
        var handler = new StubHttpMessageHandler((request, _) =>
        {
            return Task.FromResult(request.RequestUri?.PathAndQuery == "/api/session/organization-options"
                ? StubHttpMessageHandler.Json(HttpStatusCode.OK, new[]
                {
                    new OrganizationOptionDto
                    {
                        OrganizationId = 2,
                        OrganizationKey = "retail-2",
                        OrganizationName = "Retail 2",
                        IsActive = true
                    }
                })
                : StubHttpMessageHandler.Empty(HttpStatusCode.NotFound));
        });
        var user = CreateUser(
            authenticated: true,
            userId: "99",
            name: "System Admin",
            roles: ["BackofficeUserAdmin", "SystemAdmin", "SystemAdmin"],
            activeOrganizationKey: "retail-2",
            activeOrganizationName: "Retail 2");
        var loader = CreateLoader(user, handler);

        var state = await loader.LoadAsync();

        Assert.Equal("99", state.UserId);
        Assert.Equal("System Admin", state.UserName);
        Assert.Equal(["BackofficeUserAdmin", "SystemAdmin"], state.UserRoles);
        Assert.True(state.IsAdmin);
        Assert.True(state.IsSystemAdmin);
        Assert.Equal("retail-2", state.ActiveOrganizationKey);
        Assert.Equal("Retail 2", state.ActiveOrganizationName);
        Assert.Single(state.OrganizationOptions);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task LoadAsync_ForScopedAdmin_SetsAdminWithoutSystemAdmin()
    {
        var handler = new StubHttpMessageHandler((request, _) =>
        {
            return Task.FromResult(request.RequestUri?.PathAndQuery == "/api/session/organization-options"
                ? StubHttpMessageHandler.Json(HttpStatusCode.OK, Array.Empty<OrganizationOptionDto>())
                : StubHttpMessageHandler.Empty(HttpStatusCode.NotFound));
        });
        var user = CreateUser(
            authenticated: true,
            userId: "21",
            name: null,
            email: "scope.admin@acme.test",
            roles: ["BackofficeUserAdmin"],
            activeOrganizationKey: "internal",
            activeOrganizationName: "Internal Operations");
        var loader = CreateLoader(user, handler);

        var state = await loader.LoadAsync();

        Assert.Equal("scope.admin@acme.test", state.UserName);
        Assert.True(state.IsAdmin);
        Assert.False(state.IsSystemAdmin);
        Assert.Equal("internal", state.ActiveOrganizationKey);
        Assert.Equal("Internal Operations", state.ActiveOrganizationName);
    }

    [Fact]
    public async Task LoadAsync_SortsAndDeduplicatesRoles()
    {
        var handler = new StubHttpMessageHandler((request, _) =>
        {
            return Task.FromResult(request.RequestUri?.PathAndQuery == "/api/session/organization-options"
                ? StubHttpMessageHandler.Json(HttpStatusCode.OK, Array.Empty<OrganizationOptionDto>())
                : StubHttpMessageHandler.Empty(HttpStatusCode.NotFound));
        });
        var user = CreateUser(
            authenticated: true,
            userId: "8",
            name: "Casey",
            roles: ["Customer", "BackofficeUser", "Customer"]);
        var loader = CreateLoader(user, handler);

        var state = await loader.LoadAsync();

        Assert.Equal(["BackofficeUser", "Customer"], state.UserRoles);
        Assert.False(state.IsAdmin);
        Assert.False(state.IsSystemAdmin);
    }

    private static ShellSessionStateLoader CreateLoader(ClaimsPrincipal user, StubHttpMessageHandler handler)
    {
        var authStateProvider = new TestAuthenticationStateProvider(user);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://acme.test/")
        };
        var organizationOptionsClient = new OrganizationOptionsApiClient(httpClient);

        return new ShellSessionStateLoader(authStateProvider, organizationOptionsClient);
    }

    private static ClaimsPrincipal CreateUser(
        bool authenticated,
        string? userId = null,
        string? name = null,
        string? email = null,
        IReadOnlyList<string>? roles = null,
        string? activeOrganizationKey = null,
        string? activeOrganizationName = null)
    {
        var claims = new List<Claim>();

        if (!string.IsNullOrWhiteSpace(userId))
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            claims.Add(new Claim(ClaimTypes.Name, name));
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            claims.Add(new Claim(ClaimTypes.Email, email));
        }

        foreach (var role in roles ?? [])
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        if (!string.IsNullOrWhiteSpace(activeOrganizationKey))
        {
            claims.Add(new Claim(UserSessionContextConstants.ActiveOrganizationKeyClaimType, activeOrganizationKey));
        }

        if (!string.IsNullOrWhiteSpace(activeOrganizationName))
        {
            claims.Add(new Claim(UserSessionContextConstants.ActiveOrganizationNameClaimType, activeOrganizationName));
        }

        var identity = authenticated
            ? new ClaimsIdentity(claims, "TestAuth")
            : new ClaimsIdentity();

        return new ClaimsPrincipal(identity);
    }
}
