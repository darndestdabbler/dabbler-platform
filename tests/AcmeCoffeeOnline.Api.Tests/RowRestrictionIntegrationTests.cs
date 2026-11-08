using System.Security.Claims;
using AcmeCoffeeOnline.Api.Data;
using AcmeCoffeeOnline.Api.Services;
using AcmeCoffeeOnline.Shared.DTOs;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Configuration;
using Dabbler.Security.Data;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AcmeCoffeeOnline.Api.Tests;

/// <summary>
/// Exercises the Session 4 <c>IRowRestrictionBroker</c> path end-to-end by
/// swapping <c>ISecurityConfigurationStore</c> for an in-memory stub that
/// declares a user-type role with a <c>RowRestriction</c>. Proves the broker
/// resolves the restriction and that <c>OrderService.GetAllAsync</c> honours
/// it; and that a role without a matching restriction is default-permissive.
/// </summary>
public sealed class RowRestrictionIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string CustomerViewRole = "CustomerView";
    private const string UnrestrictedViewerRole = "UnrestrictedViewer";

    private readonly WebApplicationFactory<Program> _factory;

    public RowRestrictionIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UserTypeRoleWithRowRestriction_SeesOnlyMatchingRecordsAsync()
    {
        var ctx = await ResolveContextAsync();
        using var factory = BuildFactoryWithRowRestriction(ctx);

        await using var scope = factory.Services.CreateAsyncScope();
        SetHttpContext(
            scope.ServiceProvider,
            userId: ctx.FirstCustomerAppUserId,
            organizationId: ctx.FirstCustomerOrganizationId,
            organizationKey: ctx.FirstCustomerOrganizationKey,
            customerId: ctx.FirstCustomerId,
            roleNames: CustomerViewRole);

        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
        var page = await orderService.GetAllAsync(new PagedRequest { Page = 1, PageSize = 100 });

        Assert.True(page.TotalCount > 0, "The seeded first customer should have at least one order.");
        Assert.True(page.TotalCount < ctx.TotalOrderCount, "Broker must exclude other customers' orders.");
        Assert.All(page.Items, order => Assert.Equal(ctx.FirstCustomerId, order.CustomerId));
    }

    [Fact]
    public async Task UserTypeRoleWithoutMatchingRestriction_SeesAllRecordsAsync()
    {
        var ctx = await ResolveContextAsync();
        using var factory = BuildFactoryWithRowRestriction(ctx);

        await using var scope = factory.Services.CreateAsyncScope();
        // UnrestrictedViewer has no RowRestrictions for Order — the broker
        // must return null. We pair it with the legacy "BackofficeUser" claim
        // so the CurrentUserService enum-mapping raises the fallback branch
        // above the Customer deny-all guard. The broker still short-circuits
        // on UnrestrictedViewer because multi-role resolution picks the most
        // permissive role.
        SetHttpContext(
            scope.ServiceProvider,
            userId: ctx.JdoeUserId,
            organizationId: ctx.InternalOrganizationId,
            organizationKey: ctx.InternalOrganizationKey,
            customerId: null,
            roleNames: new[] { UnrestrictedViewerRole, "BackofficeUser" });

        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
        var page = await orderService.GetAllAsync(new PagedRequest { Page = 1, PageSize = 200 });

        Assert.Equal(ctx.TotalOrderCount, page.TotalCount);
    }

    private WebApplicationFactory<Program> BuildFactoryWithRowRestriction(TestContext ctx)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(services =>
            {
                // Replace the JSON-file store with an in-memory stub so the test
                // controls the SecurityConfiguration without touching disk state
                // that other integration tests depend on.
                services.RemoveAll<ISecurityConfigurationStore>();
                services.AddSingleton<ISecurityConfigurationStore>(
                    new InMemorySecurityConfigurationStore(BuildTestConfiguration()));
            });
        });
    }

    private static SecurityConfiguration BuildTestConfiguration()
    {
        return new SecurityConfiguration
        {
            UserTypeRoles =
            {
                new RoleDefinition
                {
                    RoleName = CustomerViewRole,
                    DisplayAlias = "Customer View",
                    Description = "Session 4 integration-test user-type role with a row restriction on Order.",
                    RowRestrictions =
                    {
                        new RowRestriction
                        {
                            EntityKey = "Order",
                            FilterExpression = "CustomerId == @CurrentUserCustomerId"
                        }
                    }
                },
                new RoleDefinition
                {
                    RoleName = UnrestrictedViewerRole,
                    DisplayAlias = "Unrestricted Viewer",
                    Description = "Session 4 integration-test user-type role that is default-permissive for Order."
                    // No RowRestrictions — broker should return null.
                }
            }
        };
    }

    private async Task<TestContext> ResolveContextAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var appDb = scope.ServiceProvider.GetRequiredService<AcmeCoffeeDbContext>();
        var securityDb = scope.ServiceProvider.GetRequiredService<DabblerSecurityDbContext>();

        var totalOrderCount = await appDb.Orders.CountAsync();

        var firstCustomer = await appDb.Customers
            .AsNoTracking()
            .OrderBy(c => c.Id)
            .FirstAsync();

        var firstCustomerOrganization = await securityDb.AppOrganizations
            .AsNoTracking()
            .FirstAsync(org => org.Key == $"customer-{firstCustomer.Id}");

        var internalOrganization = await securityDb.AppOrganizations
            .AsNoTracking()
            .FirstAsync(org => org.IsInternal);

        var jdoe = await securityDb.AppUsers
            .AsNoTracking()
            .FirstAsync(u => u.Email == "jdoe@acmecoffee.com");

        var firstCustomerAppUser = await securityDb.AppUsers
            .AsNoTracking()
            .FirstAsync(u => u.Email == firstCustomer.Email);

        return new TestContext(
            TotalOrderCount: totalOrderCount,
            JdoeUserId: jdoe.Id,
            InternalOrganizationId: internalOrganization.Id,
            InternalOrganizationKey: internalOrganization.Key,
            FirstCustomerAppUserId: firstCustomerAppUser.Id,
            FirstCustomerId: firstCustomer.Id,
            FirstCustomerOrganizationId: firstCustomerOrganization.Id,
            FirstCustomerOrganizationKey: firstCustomerOrganization.Key);
    }

    private static void SetHttpContext(
        IServiceProvider services,
        int userId,
        int organizationId,
        string organizationKey,
        int? customerId,
        params string[] roleNames)
    {
        var accessor = services.GetRequiredService<IHttpContextAccessor>();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(UserSessionContextConstants.ActiveOrganizationIdClaimType, organizationId.ToString()),
            new(UserSessionContextConstants.ActiveOrganizationKeyClaimType, organizationKey)
        };

        if (customerId is int cid)
        {
            claims.Add(new Claim("CustomerId", cid.ToString()));
        }

        claims.AddRange(roleNames.Select(r => new Claim(ClaimTypes.Role, r)));

        accessor.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
        };
    }

    private sealed class InMemorySecurityConfigurationStore : ISecurityConfigurationStore
    {
        private SecurityConfiguration _configuration;

        public InMemorySecurityConfigurationStore(SecurityConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task<SecurityConfiguration> LoadAsync(CancellationToken ct = default) =>
            Task.FromResult(_configuration);

        public Task SaveAsync(SecurityConfiguration configuration, CancellationToken ct = default)
        {
            _configuration = configuration;
            return Task.CompletedTask;
        }
    }

    private sealed record TestContext(
        int TotalOrderCount,
        int JdoeUserId,
        int InternalOrganizationId,
        string InternalOrganizationKey,
        int FirstCustomerAppUserId,
        int FirstCustomerId,
        int FirstCustomerOrganizationId,
        string FirstCustomerOrganizationKey);
}
