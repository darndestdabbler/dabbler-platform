using System.Security.Claims;
using AcmeCoffeeOnline.Web.Configuration;
using AcmeCoffeeOnline.Web.Services.DevTools;
using AcmeCoffeeOnline.Web.Tests.Infrastructure;
using Dabbler.Security.Services;
using Microsoft.Extensions.Options;

namespace AcmeCoffeeOnline.Web.Tests.Services.DevTools;

public sealed class TestAuthClientTests
{
    [Fact]
    public async Task GetSessionInfoAsync_UnauthenticatedUser_ReturnsEmptySnapshotAsync()
    {
        var client = BuildClient(new ClaimsPrincipal(new ClaimsIdentity()));

        var snapshot = await client.GetSessionInfoAsync();

        Assert.False(snapshot.IsAuthenticated);
        Assert.Null(snapshot.UserId);
        Assert.Null(snapshot.Email);
        Assert.Empty(snapshot.Roles);
        Assert.Empty(snapshot.AvailableOrganizations);
    }

    [Fact]
    public async Task GetSessionInfoAsync_AuthenticatedUser_ReflectsClaimsAsync()
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "42"),
                new Claim(ClaimTypes.Name, "Test User"),
                new Claim(ClaimTypes.Email, "test@example.com"),
                new Claim(ClaimTypes.Role, "BackofficeUser"),
                new Claim(UserSessionContextConstants.ActiveOrganizationIdClaimType, "7"),
                new Claim(UserSessionContextConstants.ActiveOrganizationKeyClaimType, "internal"),
                new Claim(UserSessionContextConstants.ActiveOrganizationNameClaimType, "Internal Ops"),
            ],
            authenticationType: "TestCookie");
        var principal = new ClaimsPrincipal(identity);

        var sessionContextService = new StubUserSessionContextService(
            new UserSessionContext(
                UserId: 42,
                Email: "test@example.com",
                DisplayName: "Test User",
                EffectiveRoles: ["BackofficeUser"],
                OrganizationMemberships:
                [
                    new UserSessionOrganization(
                        OrganizationId: 7,
                        OrganizationKey: "internal",
                        OrganizationName: "Internal Ops",
                        OrganizationMembershipId: 17,
                        IsDefault: true,
                        IsInternal: true,
                        RoleNames: ["BackofficeUser"])
                ],
                ActiveOrganizationId: 7,
                ActiveOrganizationMembershipId: 17,
                ActiveOrganizationKey: "internal",
                ActiveOrganizationName: "Internal Ops",
                ActiveCustomerId: null));

        var client = BuildClient(principal, sessionContextService);

        var snapshot = await client.GetSessionInfoAsync();

        Assert.True(snapshot.IsAuthenticated);
        Assert.Equal("42", snapshot.UserId);
        Assert.Equal("test@example.com", snapshot.Email);
        Assert.Equal("Internal Ops", snapshot.ActiveOrganizationName);
        Assert.Contains("BackofficeUser", snapshot.Roles);
        var membership = Assert.Single(snapshot.AvailableOrganizations);
        Assert.Equal(7, membership.OrganizationId);
        Assert.Equal("internal", membership.OrganizationKey);
        Assert.True(membership.IsInternal);
    }

    [Fact]
    public void GetMagicLinks_FiltersByEmail_AndExcludesNonMagicLinkMessages()
    {
        var inbox = new DevEmailInbox();
        inbox.Add(new DevEmailMessage(
            "MagicLink", "alpha@example.com", "111", "https://app/alpha",
            null, DateTimeOffset.UtcNow.AddSeconds(-2)));
        inbox.Add(new DevEmailMessage(
            "MagicLink", "beta@example.com", "222", "https://app/beta",
            null, DateTimeOffset.UtcNow));
        inbox.Add(new DevEmailMessage(
            "VerificationCode", "alpha@example.com", "999", null,
            null, DateTimeOffset.UtcNow));

        var client = BuildClient(new ClaimsPrincipal(new ClaimsIdentity()), inbox: inbox);

        var filtered = client.GetMagicLinks("alpha@example.com");

        var single = Assert.Single(filtered);
        Assert.Equal("alpha@example.com", single.Email);
        Assert.Equal("MagicLink", single.EmailType);
    }

    [Fact]
    public void GetMagicLinks_OrdersByCreatedAtDescending()
    {
        var inbox = new DevEmailInbox();
        var now = DateTimeOffset.UtcNow;
        inbox.Add(new DevEmailMessage("MagicLink", "a@x", "1", "link-a", null, now.AddMinutes(-5)));
        inbox.Add(new DevEmailMessage("MagicLink", "b@x", "2", "link-b", null, now));
        inbox.Add(new DevEmailMessage("MagicLink", "c@x", "3", "link-c", null, now.AddMinutes(-2)));

        var client = BuildClient(new ClaimsPrincipal(new ClaimsIdentity()), inbox: inbox);

        var results = client.GetMagicLinks();

        Assert.Collection(results,
            first => Assert.Equal("b@x", first.Email),
            second => Assert.Equal("c@x", second.Email),
            third => Assert.Equal("a@x", third.Email));
    }

    [Fact]
    public void ClearMagicLinks_EmptiesInbox()
    {
        var inbox = new DevEmailInbox();
        inbox.Add(new DevEmailMessage(
            "MagicLink", "a@x", "1", "link", null, DateTimeOffset.UtcNow));
        var client = BuildClient(new ClaimsPrincipal(new ClaimsIdentity()), inbox: inbox);

        client.ClearMagicLinks();

        Assert.Empty(inbox.GetAll());
    }

    [Fact]
    public void IsEnabled_ReflectsSeededSessionOptions()
    {
        var enabledClient = BuildClient(new ClaimsPrincipal(new ClaimsIdentity()), enabled: true);
        var disabledClient = BuildClient(new ClaimsPrincipal(new ClaimsIdentity()), enabled: false);

        Assert.True(enabledClient.IsEnabled);
        Assert.False(disabledClient.IsEnabled);
    }

    private static TestAuthClient BuildClient(
        ClaimsPrincipal user,
        StubUserSessionContextService? sessionContextService = null,
        DevEmailInbox? inbox = null,
        bool enabled = true)
    {
        var authStateProvider = new TestAuthenticationStateProvider(user);
        sessionContextService ??= new StubUserSessionContextService(null);
        inbox ??= new DevEmailInbox();
        var options = Options.Create(new AppAuthenticationOptions
        {
            SeededSession = new SeededSessionOptions
            {
                Enabled = enabled,
                SharedSecret = "test",
                AllowedRoles = ["BackofficeUser"]
            }
        });
        return new TestAuthClient(authStateProvider, sessionContextService, inbox, options);
    }

    private sealed class StubUserSessionContextService : IUserSessionContextService
    {
        private readonly UserSessionContext? _context;

        public StubUserSessionContextService(UserSessionContext? context)
        {
            _context = context;
        }

        public Task<UserSessionContext?> BuildAsync(
            int userId, int? requestedOrganizationId = null, CancellationToken ct = default)
            => Task.FromResult(_context);

        public Task<ClaimsPrincipal?> CreatePrincipalAsync(
            int userId,
            string authenticationScheme = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme,
            int? requestedOrganizationId = null,
            IReadOnlyList<string>? overrideRoles = null,
            CancellationToken ct = default)
            => Task.FromResult<ClaimsPrincipal?>(null);
    }
}
