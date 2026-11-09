using System.Security.Claims;
using AcmeCoffeeOnline.Api.Data;
using AcmeCoffeeOnline.Api.Services;
using AcmeCoffeeOnline.Shared.DTOs;
using Dabbler.Security.Authorization;
using Dabbler.Security.Authorization.Handlers;
using Dabbler.Security.Authorization.Requirements;
using Dabbler.Security.Configuration;
using Dabbler.Security.Data;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AcmeCoffeeOnline.Api.Tests;

/// <summary>
/// Session 5 of <c>rearchitect-role-administration-foundations</c>: proves that the
/// migrated <c>security-config.json</c> + <see cref="SecuritySeedData"/> produce the
/// expected restriction-based configuration, that the runtime handlers honour it,
/// and that legacy <c>InheritsFrom</c> data has no authorization effect.
/// </summary>
public sealed class MigratedSecurityConfigSeedTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public MigratedSecurityConfigSeedTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SeededConfiguration_ContainsExpectedUserTypeRoles_UnderNewShapeAsync()
    {
        using var factory = _factory.WithWebHostBuilder(b => b.UseEnvironment("Development"));
        await using var scope = factory.Services.CreateAsyncScope();

        var manager = scope.ServiceProvider.GetRequiredService<SecurityConfigurationManager>();
        await manager.EnsureLoadedAsync();
        var config = manager.Current;

        // WarehouseUser was added in Session 5 of
        // rearchitect-role-administration-dashboard as the Hidden-path
        // reference role for the data-bound restriction enforcement seam.
        Assert.Collection(
            config.UserTypeRoles.OrderBy(r => r.RoleName, StringComparer.Ordinal),
            role =>
            {
                Assert.Equal("BackofficeUser", role.RoleName);
                Assert.Equal("Backoffice User", role.DisplayAlias);
            },
            role =>
            {
                Assert.Equal("Customer", role.RoleName);
                Assert.Equal("Customer", role.DisplayAlias);
            },
            role =>
            {
                Assert.Equal("WarehouseUser", role.RoleName);
                Assert.Equal("Warehouse User", role.DisplayAlias);
                Assert.Contains(role.ColumnRestrictions, cr =>
                    cr.EntityKey == "Product"
                    && cr.ColumnKey == "UnitPrice"
                    && cr.RestrictionKind == ColumnRestrictionKind.Hidden);
            });
    }

    [Fact]
    public async Task SeededConfiguration_HidesCustomerIsActive_FromCustomerRoleAsync()
    {
        using var factory = _factory.WithWebHostBuilder(b => b.UseEnvironment("Development"));
        await using var scope = factory.Services.CreateAsyncScope();

        var manager = scope.ServiceProvider.GetRequiredService<SecurityConfigurationManager>();
        await manager.EnsureLoadedAsync();

        var customerRole = manager.Current.UserTypeRoles
            .Single(r => string.Equals(r.RoleName, "Customer", StringComparison.Ordinal));

        var isActiveRestriction = customerRole.ColumnRestrictions.Single(cr =>
            string.Equals(cr.EntityKey, "Customer", StringComparison.Ordinal) &&
            string.Equals(cr.ColumnKey, "IsActive", StringComparison.Ordinal));

        Assert.Equal(ColumnRestrictionKind.Hidden, isActiveRestriction.RestrictionKind);
    }

    [Fact]
    public async Task SeededConfiguration_CarriesCustomerOrderRowRestrictionAsync()
    {
        using var factory = _factory.WithWebHostBuilder(b => b.UseEnvironment("Development"));
        await using var scope = factory.Services.CreateAsyncScope();

        var manager = scope.ServiceProvider.GetRequiredService<SecurityConfigurationManager>();
        await manager.EnsureLoadedAsync();

        var customerRole = manager.Current.UserTypeRoles
            .Single(r => string.Equals(r.RoleName, "Customer", StringComparison.Ordinal));

        var orderRowRestriction = customerRole.RowRestrictions.Single(r =>
            string.Equals(r.EntityKey, "Order", StringComparison.Ordinal));

        Assert.Equal("CustomerId == @CurrentUserCustomerId", orderRowRestriction.FilterExpression);
    }

    [Fact]
    public async Task BakedInSystemAdmin_SucceedsForEveryPage_WithNoUserTypeRolesConfiguredAsync()
    {
        using var factory = _factory.WithWebHostBuilder(b => b.UseEnvironment("Development"));
        await using var scope = factory.Services.CreateAsyncScope();

        var handler = scope.ServiceProvider.GetServices<IAuthorizationHandler>()
            .OfType<PageAuthorizationHandler>()
            .Single();

        // Bypass the seeded configuration by pointing the manager at an empty config —
        // the handler still succeeds for baked-in SystemAdmin because the baked-in
        // privilege rules are compiled-in, not JSON-driven.
        var emptyManager = new SecurityConfigurationManager(new StaticConfigurationStore(new SecurityConfiguration()));
        var bakedInProvider = new PlatformBakedInRoleProvider();
        var roleSwitcher = new SessionRoleSwitcher();
        var handlerWithEmptyConfig = new PageAuthorizationHandler(emptyManager, roleSwitcher, bakedInProvider);

        var principal = Principal(BakedInRoles.SystemAdmin);
        var requirement = new PagePermissionRequirement("/admin/security");
        var context = new AuthorizationHandlerContext([requirement], principal, null);

        await handlerWithEmptyConfig.HandleAsync(context);

        Assert.True(context.HasSucceeded, "Baked-in SystemAdmin must succeed with no JSON configuration.");
    }

    [Fact]
    public async Task UserTypeRoleWithoutRestrictions_IsDefaultPermissiveAsync()
    {
        var config = new SecurityConfiguration
        {
            UserTypeRoles =
            {
                new RoleDefinition { RoleName = "Receptionist", DisplayAlias = "Receptionist" }
            }
        };
        var handler = BuildPageHandler(config);

        var context = new AuthorizationHandlerContext(
            [new PagePermissionRequirement("/orders")],
            Principal("Receptionist"),
            null);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded, "A user-type role without UiRestrictions should succeed on any page.");
    }

    [Fact]
    public async Task ControlHandler_HidesColumnControl_WhenUiRestrictionMatchesTheColumnControlKeyAsync()
    {
        // The spec asks for a hidden-column behaviour test. Column-level enforcement
        // lands in Session Set D. Today the nearest handler-level enforcement is the
        // ControlAuthorizationHandler: when a grid column renders a
        // control whose ControlKey matches a hidden UiRestriction for the role, the
        // handler denies. This test pins that cross-wiring so a future ColumnRestriction
        // handler does not regress the already-working UiRestriction-on-column path.
        var config = new SecurityConfiguration
        {
            UserTypeRoles =
            {
                new RoleDefinition
                {
                    RoleName = "Customer",
                    ColumnRestrictions =
                    {
                        new ColumnRestriction
                        {
                            EntityKey = "Customer",
                            ColumnKey = "IsActive",
                            RestrictionKind = ColumnRestrictionKind.Hidden
                        }
                    },
                    UiRestrictions =
                    {
                        new UiRestriction
                        {
                            PageKey = "/customers",
                            ControlKey = "CustomerIsActiveCell",
                            Kind = UiRestrictionKind.Hidden
                        }
                    }
                }
            }
        };

        var store = new StaticConfigurationStore(config);
        var manager = new SecurityConfigurationManager(store);
        var handler = new ControlAuthorizationHandler(
            manager,
            new SessionRoleSwitcher(),
            new PlatformBakedInRoleProvider());

        var context = new AuthorizationHandlerContext(
            [new ControlPermissionRequirement("/customers", "CustomerIsActiveCell")],
            Principal("Customer"),
            null);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded, "ControlAuthorizationHandler must deny a column control marked hidden for the role.");
    }

    [Fact]
    public async Task LegacyInheritsFrom_DoesNotPromoteRoleAccessAsync()
    {
#pragma warning disable CS0618 // deliberate legacy field use for the inheritance-not-applied guarantee
        var config = new SecurityConfiguration
        {
            UserTypeRoles =
            {
                new RoleDefinition
                {
                    RoleName = "JuniorClerk",
                    InheritsFrom = BakedInRoles.SystemAdmin,
                    UiRestrictions =
                    {
                        new UiRestriction
                        {
                            PageKey = "/admin/security",
                            ControlKey = "*",
                            Kind = UiRestrictionKind.Hidden
                        }
                    }
                }
            }
        };
#pragma warning restore CS0618

        var handler = BuildPageHandler(config);
        var context = new AuthorizationHandlerContext(
            [new PagePermissionRequirement("/admin/security")],
            Principal("JuniorClerk"),
            null);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded,
            "InheritsFrom must not promote JuniorClerk to SystemAdmin access — role inheritance is removed.");
    }

    [Fact]
    public async Task Broker_ResolvesCustomerOrderRestriction_AgainstSeededConfigAsync()
    {
        using var factory = _factory.WithWebHostBuilder(b => b.UseEnvironment("Development"));
        var ctx = await ResolveContextAsync(factory.Services);

        await using var scope = factory.Services.CreateAsyncScope();
        SetHttpContext(
            scope.ServiceProvider,
            userId: ctx.FirstCustomerAppUserId,
            organizationId: ctx.FirstCustomerOrganizationId,
            organizationKey: ctx.FirstCustomerOrganizationKey,
            customerId: ctx.FirstCustomerId,
            roleNames: "Customer");

        // The seeded configuration is loaded lazily via EnsureLoadedAsync on first
        // handler call; the broker's first use needs the same guarantee.
        var manager = scope.ServiceProvider.GetRequiredService<SecurityConfigurationManager>();
        await manager.EnsureLoadedAsync();

        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
        var page = await orderService.GetAllAsync(new PagedRequest { Page = 1, PageSize = 200 });

        Assert.True(page.TotalCount > 0, "First customer should have at least one seeded order.");
        Assert.True(page.TotalCount < ctx.TotalOrderCount,
            "Broker-resolved row restriction must exclude other customers' orders.");
        Assert.All(page.Items, order => Assert.Equal(ctx.FirstCustomerId, order.CustomerId));
    }

    private static PageAuthorizationHandler BuildPageHandler(SecurityConfiguration config)
    {
        var store = new StaticConfigurationStore(config);
        var manager = new SecurityConfigurationManager(store);
        return new PageAuthorizationHandler(
            manager,
            new SessionRoleSwitcher(),
            new PlatformBakedInRoleProvider());
    }

    private static ClaimsPrincipal Principal(params string[] roles)
    {
        var claims = roles.Select(r => new Claim(ClaimTypes.Role, r))
            .Append(new Claim(ClaimTypes.NameIdentifier, "1"))
            .ToArray();
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    private static async Task<TestContext> ResolveContextAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
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

        var firstCustomerAppUser = await securityDb.AppUsers
            .AsNoTracking()
            .FirstAsync(u => u.Email == firstCustomer.Email);

        return new TestContext(
            TotalOrderCount: totalOrderCount,
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

    private sealed record TestContext(
        int TotalOrderCount,
        int FirstCustomerAppUserId,
        int FirstCustomerId,
        int FirstCustomerOrganizationId,
        string FirstCustomerOrganizationKey);

    private sealed class StaticConfigurationStore : Dabbler.Security.Abstractions.ISecurityConfigurationStore
    {
        private SecurityConfiguration _configuration;

        public StaticConfigurationStore(SecurityConfiguration configuration)
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
}
