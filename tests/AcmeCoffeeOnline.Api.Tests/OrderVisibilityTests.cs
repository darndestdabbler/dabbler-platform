using System.Security.Claims;
using AcmeCoffeeOnline.Api.Data;
using AcmeCoffeeOnline.Api.Services;
using AcmeCoffeeOnline.Shared.DTOs;
using Dabbler.Security.Data;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AcmeCoffeeOnline.Api.Tests;

/// <summary>
/// Row-filter behavior for OrderService.GetAllAsync, hitting the real seeded
/// database. Covers the UAT finding where a BackofficeUser signed in against
/// their default (internal) organization saw zero orders.
/// </summary>
public sealed class OrderVisibilityTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public OrderVisibilityTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task BackofficeUser_WithInternalOrganizationActive_SeesAllOrdersAsync()
    {
        using var factory = _factory.WithWebHostBuilder(b => b.UseEnvironment("Development"));
        var ctx = await ResolveContextAsync(factory.Services);

        await using var scope = factory.Services.CreateAsyncScope();
        SetHttpContext(
            scope.ServiceProvider,
            userId: ctx.JdoeUserId,
            organizationId: ctx.InternalOrganizationId,
            organizationKey: ctx.InternalOrganizationKey,
            roleNames: "BackofficeUser");

        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
        var page = await orderService.GetAllAsync(new PagedRequest { Page = 1, PageSize = 25 });

        Assert.Equal(ctx.TotalOrderCount, page.TotalCount);
        Assert.NotEmpty(page.Items);
    }

    [Fact]
    public async Task Customer_WithMatchingCustomerId_SeesOnlyTheirOrdersAsync()
    {
        using var factory = _factory.WithWebHostBuilder(b => b.UseEnvironment("Development"));
        var ctx = await ResolveContextAsync(factory.Services);

        await using var scope = factory.Services.CreateAsyncScope();
        SetHttpContext(
            scope.ServiceProvider,
            userId: ctx.FirstCustomerAppUserId,
            organizationId: ctx.FirstCustomerOrganizationId,
            organizationKey: ctx.FirstCustomerOrganizationKey,
            roleNames: "Customer");

        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
        var page = await orderService.GetAllAsync(new PagedRequest { Page = 1, PageSize = 100 });

        Assert.True(page.TotalCount > 0, "The seeded first customer should have at least one order.");
        Assert.True(page.TotalCount < ctx.TotalOrderCount, "Customer scope must exclude other customers' orders.");
        Assert.All(page.Items, order => Assert.Equal(ctx.FirstCustomerId, order.CustomerId));
    }

    [Fact]
    public async Task SystemAdmin_SeesAllOrdersAsync()
    {
        using var factory = _factory.WithWebHostBuilder(b => b.UseEnvironment("Development"));
        var ctx = await ResolveContextAsync(factory.Services);

        await using var scope = factory.Services.CreateAsyncScope();
        SetHttpContext(
            scope.ServiceProvider,
            userId: ctx.SystemAdminUserId,
            organizationId: ctx.InternalOrganizationId,
            organizationKey: ctx.InternalOrganizationKey,
            roleNames: "SystemAdmin");

        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
        var page = await orderService.GetAllAsync(new PagedRequest { Page = 1, PageSize = 25 });

        Assert.Equal(ctx.TotalOrderCount, page.TotalCount);
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

        var internalOrganization = await securityDb.AppOrganizations
            .AsNoTracking()
            .FirstAsync(org => org.IsInternal);

        var jdoe = await securityDb.AppUsers
            .AsNoTracking()
            .FirstAsync(u => u.Email == "jdoe@acmecoffee.com");

        var systemAdmin = await securityDb.AppUsers
            .AsNoTracking()
            .FirstAsync(u => u.Email == "admin@acmecoffee.com");

        var firstCustomerAppUser = await securityDb.AppUsers
            .AsNoTracking()
            .FirstAsync(u => u.Email == firstCustomer.Email);

        return new TestContext(
            TotalOrderCount: totalOrderCount,
            JdoeUserId: jdoe.Id,
            SystemAdminUserId: systemAdmin.Id,
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
        params string[] roleNames)
    {
        var accessor = services.GetRequiredService<IHttpContextAccessor>();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(UserSessionContextConstants.ActiveOrganizationIdClaimType, organizationId.ToString()),
            new(UserSessionContextConstants.ActiveOrganizationKeyClaimType, organizationKey)
        };

        claims.AddRange(roleNames.Select(r => new Claim(ClaimTypes.Role, r)));

        accessor.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
        };
    }

    private sealed record TestContext(
        int TotalOrderCount,
        int JdoeUserId,
        int SystemAdminUserId,
        int InternalOrganizationId,
        string InternalOrganizationKey,
        int FirstCustomerAppUserId,
        int FirstCustomerId,
        int FirstCustomerOrganizationId,
        string FirstCustomerOrganizationKey);
}
