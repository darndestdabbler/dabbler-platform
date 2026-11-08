using System.Security.Claims;
using AcmeCoffeeOnline.Api.Data;
using AcmeCoffeeOnline.Api.Services;
using AcmeCoffeeOnline.Shared.DTOs;
using AcmeCoffeeOnline.Shared.Enums;
using Dabbler.Security.Data;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AcmeCoffeeOnline.Api.Tests;

/// <summary>
/// Session 5 (composable-crud-helpers): OrderService.UpdateAsync merges the
/// dto.OrderItems collection against the tracked Order.OrderItems via EF
/// cascade. Items with Id == 0 are INSERTed, items missing from dto are
/// DELETEd, items with matching Id are UPDATEd. Order.Total is recalculated.
/// Non-modifiable statuses (Delivered, Cancelled) skip the merge entirely.
/// </summary>
public sealed class OrderServiceMergeItemsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public OrderServiceMergeItemsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UpdateAsync_WithNewItem_InsertsItemAsync()
    {
        using var factory = _factory.WithWebHostBuilder(b => b.UseEnvironment("Development"));
        var ctx = await ResolveContextAsync(factory.Services);
        var (orderService, original) = await GetOrderAndServiceAsync(factory, ctx, ctx.TestOrderId);
        var originalItemCount = original.OrderItems.Count;

        var dto = CreateDtoFromOriginal(original);
        dto.OrderItems.Add(new OrderItemDto
        {
            Id = 0, // new — merge will INSERT
            OrderId = original.Id,
            ProductId = ctx.SpareProductId,
            Quantity = 5,
            UnitPrice = 9.50m,
            Discount = 0
        });

        var updated = await orderService.UpdateAsync(original.Id, dto);

        Assert.NotNull(updated);
        Assert.Equal(originalItemCount + 1, updated!.OrderItems.Count);
        var newItem = updated.OrderItems.FirstOrDefault(i => i.ProductId == ctx.SpareProductId);
        Assert.NotNull(newItem);
        Assert.True(newItem!.Id > 0, "INSERTed item should have a generated Id.");

        await RestoreOriginalOrderAsync(factory, ctx, original);
    }

    [Fact]
    public async Task UpdateAsync_WithOmittedItem_DeletesItemAsync()
    {
        using var factory = _factory.WithWebHostBuilder(b => b.UseEnvironment("Development"));
        var ctx = await ResolveContextAsync(factory.Services);
        var (orderService, original) = await GetOrderAndServiceAsync(factory, ctx, ctx.TestOrderId);
        var originalItemCount = original.OrderItems.Count;
        Assert.True(originalItemCount > 1, "Test order must have at least two items for the delete merge test.");

        var dto = CreateDtoFromOriginal(original);
        dto.OrderItems.RemoveAt(0); // omit the first item — merge will DELETE

        var updated = await orderService.UpdateAsync(original.Id, dto);

        Assert.NotNull(updated);
        Assert.Equal(originalItemCount - 1, updated!.OrderItems.Count);

        await RestoreOriginalOrderAsync(factory, ctx, original);
    }

    [Fact]
    public async Task UpdateAsync_WithModifiedItem_UpdatesItemAsync()
    {
        using var factory = _factory.WithWebHostBuilder(b => b.UseEnvironment("Development"));
        var ctx = await ResolveContextAsync(factory.Services);
        var (orderService, original) = await GetOrderAndServiceAsync(factory, ctx, ctx.TestOrderId);
        var itemToUpdate = original.OrderItems.First();
        var newQuantity = itemToUpdate.Quantity + 10;

        var dto = CreateDtoFromOriginal(original);
        dto.OrderItems.First(i => i.Id == itemToUpdate.Id).Quantity = newQuantity;

        var updated = await orderService.UpdateAsync(original.Id, dto);

        Assert.NotNull(updated);
        Assert.Equal(original.OrderItems.Count, updated!.OrderItems.Count);
        var updatedItem = updated.OrderItems.First(i => i.Id == itemToUpdate.Id);
        Assert.Equal(newQuantity, updatedItem.Quantity);
        // Id is preserved through UPDATE
        Assert.Equal(itemToUpdate.Id, updatedItem.Id);

        await RestoreOriginalOrderAsync(factory, ctx, original);
    }

    [Fact]
    public async Task UpdateAsync_RecalculatesTotalToMergedSetAsync()
    {
        using var factory = _factory.WithWebHostBuilder(b => b.UseEnvironment("Development"));
        var ctx = await ResolveContextAsync(factory.Services);
        var (orderService, original) = await GetOrderAndServiceAsync(factory, ctx, ctx.TestOrderId);

        var dto = CreateDtoFromOriginal(original);
        // Double the quantity of every existing item to force a known new total.
        foreach (var item in dto.OrderItems)
            item.Quantity *= 2;

        var updated = await orderService.UpdateAsync(original.Id, dto);

        Assert.NotNull(updated);
        var expectedTotal = updated!.OrderItems.Sum(i => i.Quantity * i.UnitPrice * (1m - i.Discount));
        Assert.Equal(expectedTotal, updated.Total);

        await RestoreOriginalOrderAsync(factory, ctx, original);
    }

    [Fact]
    public async Task UpdateAsync_OnNonModifiableOrder_DoesNotMergeItemsAsync()
    {
        using var factory = _factory.WithWebHostBuilder(b => b.UseEnvironment("Development"));
        var ctx = await ResolveContextAsync(factory.Services);
        if (ctx.NonModifiableOrderId is null)
        {
            // No seeded order is already in a terminal state (Delivered/Cancelled).
            // Skip rather than mutate seed data — Cancelled is terminal and cannot be reversed.
            return;
        }

        var (orderService, original) = await GetOrderAndServiceAsync(factory, ctx, ctx.NonModifiableOrderId.Value);
        var originalItemCount = original.OrderItems.Count;

        var dto = CreateDtoFromOriginal(original);
        dto.OrderItems.Add(new OrderItemDto
        {
            Id = 0,
            OrderId = original.Id,
            ProductId = ctx.SpareProductId,
            Quantity = 1,
            UnitPrice = 1m,
            Discount = 0
        });

        var updated = await orderService.UpdateAsync(original.Id, dto);

        Assert.NotNull(updated);
        // Items collection is unchanged because IsModifiable(status) gated the merge.
        Assert.Equal(originalItemCount, updated!.OrderItems.Count);
    }

    // ── Helpers (mirror OrderUpdateTests.cs pattern) ───────────────

    private static async Task<(IOrderService service, OrderDto original)> GetOrderAndServiceAsync(
        WebApplicationFactory<Program> factory, TestContext ctx, int orderId)
    {
        var scope = factory.Services.CreateAsyncScope();
        SetHttpContext(
            scope.ServiceProvider,
            ctx.SystemAdminUserId,
            ctx.InternalOrganizationId,
            ctx.InternalOrganizationKey,
            "SystemAdmin");

        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
        var original = await orderService.GetByIdAsync(orderId);
        Assert.NotNull(original);
        return (orderService, original!);
    }

    private static async Task RestoreOriginalOrderAsync(
        WebApplicationFactory<Program> factory, TestContext ctx, OrderDto original)
    {
        await using var restoreScope = factory.Services.CreateAsyncScope();
        SetHttpContext(
            restoreScope.ServiceProvider,
            ctx.SystemAdminUserId,
            ctx.InternalOrganizationId,
            ctx.InternalOrganizationKey,
            "SystemAdmin");
        var restoreService = restoreScope.ServiceProvider.GetRequiredService<IOrderService>();
        await restoreService.UpdateAsync(original.Id, CreateDtoFromOriginal(original));
    }

    private static OrderDto CreateDtoFromOriginal(OrderDto original) => new()
    {
        Id = original.Id,
        Notes = original.Notes,
        ShipDate = original.ShipDate,
        ModifiedBy = "test_runner",
        OrderItems = original.OrderItems.Select(oi => new OrderItemDto
        {
            Id = oi.Id,
            OrderId = oi.OrderId,
            ProductId = oi.ProductId,
            ProductName = oi.ProductName,
            Quantity = oi.Quantity,
            UnitPrice = oi.UnitPrice,
            Discount = oi.Discount
        }).ToList()
    };

    private static async Task<TestContext> ResolveContextAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var appDb = scope.ServiceProvider.GetRequiredService<AcmeCoffeeDbContext>();
        var securityDb = scope.ServiceProvider.GetRequiredService<DabblerSecurityDbContext>();

        // Use an order that's Pending or Confirmed (modifiable) AND has >= 2 items
        // so the delete-merge test can drop one and still leave one behind.
        var testOrder = await appDb.Orders
            .AsNoTracking()
            .Where(o => (o.Status == OrderStatus.Pending || o.Status == OrderStatus.Confirmed)
                        && o.OrderItems.Count >= 2)
            .OrderBy(o => o.Id)
            .FirstAsync();

        // Optional: an order that's already terminal. If none exists, the
        // OnNonModifiableOrder test no-ops.
        var nonModifiableOrderId = await appDb.Orders
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.Delivered || o.Status == OrderStatus.Cancelled)
            .OrderBy(o => o.Id)
            .Select(o => (int?)o.Id)
            .FirstOrDefaultAsync();

        // A product that's not already on the test order — used for INSERT tests
        // so we don't get duplicate-product surprises.
        var orderProductIds = await appDb.OrderItems
            .AsNoTracking()
            .Where(oi => oi.OrderId == testOrder.Id)
            .Select(oi => oi.ProductId)
            .ToListAsync();
        var spareProductId = await appDb.Products
            .AsNoTracking()
            .Where(p => !orderProductIds.Contains(p.Id))
            .OrderBy(p => p.Id)
            .Select(p => p.Id)
            .FirstAsync();

        var internalOrganization = await securityDb.AppOrganizations
            .AsNoTracking()
            .FirstAsync(org => org.IsInternal);

        var systemAdmin = await securityDb.AppUsers
            .AsNoTracking()
            .FirstAsync(u => u.Email == "admin@acmecoffee.com");

        return new TestContext(
            TestOrderId: testOrder.Id,
            NonModifiableOrderId: nonModifiableOrderId,
            SpareProductId: spareProductId,
            SystemAdminUserId: systemAdmin.Id,
            InternalOrganizationId: internalOrganization.Id,
            InternalOrganizationKey: internalOrganization.Key);
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
        int TestOrderId,
        int? NonModifiableOrderId,
        int SpareProductId,
        int SystemAdminUserId,
        int InternalOrganizationId,
        string InternalOrganizationKey);
}
