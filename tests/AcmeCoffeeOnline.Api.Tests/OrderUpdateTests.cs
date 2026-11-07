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
/// Order update behavior for OrderService.UpdateAsync, hitting the real seeded
/// database. Covers the UAT finding where SystemAdmin could not edit an order
/// from the detail page.
/// </summary>
[Collection(SeededOrderMutationCollection.Name)]
public sealed class OrderUpdateTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public OrderUpdateTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SystemAdmin_CanUpdateOrderNotesAndShipDateAsync()
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
        var original = await orderService.GetByIdAsync(ctx.OrderId);
        Assert.NotNull(original);

        var newNotes = $"UAT edit {Guid.NewGuid():N}";
        var newShipDate = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

        var updated = await orderService.UpdateAsync(ctx.OrderId, new OrderDto
        {
            Id = ctx.OrderId,
            CustomerId = original.CustomerId,
            OrderDate = original.OrderDate,
            ShipDate = newShipDate,
            Status = original.Status,
            Total = original.Total,
            Notes = newNotes,
            ModifiedBy = "admin@acmecoffee.com"
        });

        Assert.NotNull(updated);
        Assert.Equal(newNotes, updated!.Notes);
        Assert.Equal(newShipDate, updated.ShipDate);

        await using var verifyScope = factory.Services.CreateAsyncScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<AcmeCoffeeDbContext>();
        var persisted = await db.Orders.AsNoTracking().FirstAsync(o => o.Id == ctx.OrderId);
        Assert.Equal(newNotes, persisted.Notes);
        Assert.Equal(newShipDate, persisted.ShipDate);

        // Restore the order to its original state so subsequent tests are not affected.
        await using var restoreScope = factory.Services.CreateAsyncScope();
        SetHttpContext(
            restoreScope.ServiceProvider,
            userId: ctx.SystemAdminUserId,
            organizationId: ctx.InternalOrganizationId,
            organizationKey: ctx.InternalOrganizationKey,
            roleNames: "SystemAdmin");
        var restoreService = restoreScope.ServiceProvider.GetRequiredService<IOrderService>();
        await restoreService.UpdateAsync(ctx.OrderId, new OrderDto
        {
            Id = ctx.OrderId,
            CustomerId = original.CustomerId,
            OrderDate = original.OrderDate,
            ShipDate = original.ShipDate,
            Status = original.Status,
            Total = original.Total,
            Notes = original.Notes,
            ModifiedBy = original.ModifiedBy
        });
    }

    // Note: Customer is blocked from UpdateOrder at TWO layers.
    // 1. [DabblerEndpointAuthorize("UpdateOrder")] on the controller action — Customer
    //    is not in security-config.json UpdateOrder.AllowedRoles, so the HTTP call
    //    returns 403 before reaching the service. That layer is enforced by the ASP.NET
    //    pipeline and is covered by the endpoint-authorization tests in Dabbler.Security.
    // 2. OrderService.ApplyRowFilter — even if the endpoint ever allowed Customer,
    //    the row filter confines them to their own CustomerId. This test exercises
    //    layer (2) directly by bypassing the controller and calling the service.
    [Fact]
    public async Task Customer_CannotUpdateOtherCustomerOrderAsync()
    {
        using var factory = _factory.WithWebHostBuilder(b => b.UseEnvironment("Development"));
        var ctx = await ResolveContextAsync(factory.Services);

        await using var scope = factory.Services.CreateAsyncScope();
        SetHttpContext(
            scope.ServiceProvider,
            userId: ctx.OtherCustomerAppUserId,
            organizationId: ctx.OtherCustomerOrganizationId,
            organizationKey: ctx.OtherCustomerOrganizationKey,
            roleNames: "Customer");

        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

        var result = await orderService.UpdateAsync(ctx.OrderId, new OrderDto
        {
            Id = ctx.OrderId,
            Notes = "Should not persist"
        });

        // The row filter must hide the order from a Customer whose CustomerId does not match.
        Assert.Null(result);

        await using var verifyScope = factory.Services.CreateAsyncScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<AcmeCoffeeDbContext>();
        var persisted = await db.Orders.AsNoTracking().FirstAsync(o => o.Id == ctx.OrderId);
        Assert.NotEqual("Should not persist", persisted.Notes);
    }

    private static async Task<TestContext> ResolveContextAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var appDb = scope.ServiceProvider.GetRequiredService<AcmeCoffeeDbContext>();
        var securityDb = scope.ServiceProvider.GetRequiredService<DabblerSecurityDbContext>();

        var firstCustomer = await appDb.Customers
            .AsNoTracking()
            .OrderBy(c => c.Id)
            .FirstAsync();

        var firstCustomerOrder = await appDb.Orders
            .AsNoTracking()
            .OrderBy(o => o.Id)
            .FirstAsync(o => o.CustomerId == firstCustomer.Id);

        var otherCustomer = await appDb.Customers
            .AsNoTracking()
            .OrderBy(c => c.Id)
            .FirstAsync(c => c.Id != firstCustomer.Id);

        var otherCustomerOrganization = await securityDb.AppOrganizations
            .AsNoTracking()
            .FirstAsync(org => org.Key == $"customer-{otherCustomer.Id}");

        var otherCustomerAppUser = await securityDb.AppUsers
            .AsNoTracking()
            .FirstAsync(u => u.Email == otherCustomer.Email);

        var internalOrganization = await securityDb.AppOrganizations
            .AsNoTracking()
            .FirstAsync(org => org.IsInternal);

        var systemAdmin = await securityDb.AppUsers
            .AsNoTracking()
            .FirstAsync(u => u.Email == "admin@acmecoffee.com");

        return new TestContext(
            OrderId: firstCustomerOrder.Id,
            SystemAdminUserId: systemAdmin.Id,
            InternalOrganizationId: internalOrganization.Id,
            InternalOrganizationKey: internalOrganization.Key,
            OtherCustomerAppUserId: otherCustomerAppUser.Id,
            OtherCustomerOrganizationId: otherCustomerOrganization.Id,
            OtherCustomerOrganizationKey: otherCustomerOrganization.Key);
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
        int OrderId,
        int SystemAdminUserId,
        int InternalOrganizationId,
        string InternalOrganizationKey,
        int OtherCustomerAppUserId,
        int OtherCustomerOrganizationId,
        string OtherCustomerOrganizationKey);
}
