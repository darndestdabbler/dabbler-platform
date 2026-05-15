using AcmeCoffeeOnline.Api.Data;
using AcmeCoffeeOnline.Shared.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AcmeCoffeeOnline.Api.Tests;

// Integration tests proving the five transactional entities are configured
// as SQL Server temporal tables: update/delete create history rows and
// EF Core's TemporalAll / TemporalAsOf APIs return them. These tests hit
// the real seeded SQL Server database the API uses in Development —
// in-memory and SQLite providers do not support temporal queries.
//
// Probe pattern: update a single field on a seeded row inside a try/finally
// and restore the original value. Avoid creating NEW Customer rows —
// DatabaseInitializer.BackfillSecuritySeedDataAsync derives AppUsers
// from customer rows, and a new customer leaks into the security seed
// when a parallel WebApplicationFactory boots, causing AppUser duplicate-
// key races. Probe-update of an existing row is safe because the seed
// is keyed on Customer.Email (unchanged by the probe).
public sealed class TemporalTablesTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public TemporalTablesTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AllFiveTransactionalTables_AreSystemVersionedAsync()
    {
        using var factory = _factory.WithWebHostBuilder(b => b.UseEnvironment("Development"));
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AcmeCoffeeDbContext>();

        var rows = await db.Database
            .SqlQueryRaw<TemporalTypeRow>(@"
                SELECT name AS TableName, temporal_type AS TemporalType
                FROM sys.tables
                WHERE name IN ('Products', 'Customers', 'Orders', 'OrderItems', 'OrderNotes')")
            .ToListAsync();

        Assert.Equal(5, rows.Count);
        Assert.All(rows, r => Assert.Equal(2, r.TemporalType));
    }

    [Fact]
    public async Task AllFiveHistoryTables_ExistWithExpectedNamesAsync()
    {
        using var factory = _factory.WithWebHostBuilder(b => b.UseEnvironment("Development"));
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AcmeCoffeeDbContext>();

        var expected = new[]
        {
            "ProductHistory", "CustomerHistory", "OrderHistory",
            "OrderItemHistory", "OrderNoteHistory"
        };

        var rows = await db.Database
            .SqlQueryRaw<HistoryTableRow>(@"
                SELECT h.name AS HistoryTableName, t.name AS CurrentTableName
                FROM sys.tables t
                INNER JOIN sys.tables h ON t.history_table_id = h.object_id
                WHERE t.name IN ('Products', 'Customers', 'Orders', 'OrderItems', 'OrderNotes')")
            .ToListAsync();

        var actual = rows.Select(r => r.HistoryTableName).OrderBy(n => n).ToArray();
        Assert.Equal(expected.OrderBy(n => n).ToArray(), actual);
    }

    [Fact]
    public async Task Product_Update_CreatesHistoryRowAsync()
    {
        using var factory = _factory.WithWebHostBuilder(b => b.UseEnvironment("Development"));
        var existing = await GetFirstSeededProductAsync(factory.Services);
        var originalDescription = existing.Description;
        var probeMarker = $"temporal-probe-{Guid.NewGuid():N}";

        try
        {
            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AcmeCoffeeDbContext>();
                var tracked = await db.Products.FirstAsync(p => p.Id == existing.Id);
                tracked.Description = probeMarker;
                tracked.ModifiedDate = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }

            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AcmeCoffeeDbContext>();
                var versions = await db.Products
                    .TemporalAll()
                    .Where(p => p.Id == existing.Id)
                    .OrderBy(p => EF.Property<DateTime>(p, "SysStart"))
                    .ToListAsync();

                Assert.True(
                    versions.Count >= 2,
                    $"Expected at least two versions of product {existing.Id} after probe update; got {versions.Count}.");
                Assert.Contains(versions, p => p.Description == probeMarker);
            }
        }
        finally
        {
            await RestoreProductDescriptionAsync(factory.Services, existing.Id, originalDescription);
        }
    }

    [Fact]
    public async Task Product_Delete_PreservesRowInHistoryAsync()
    {
        // This is the only test that mutates the DB by creating a NEW row —
        // a product whose deletion we verify. Products do NOT feed the
        // security seed (unlike Customers), so a transient product cannot
        // race the AppUsers backfill.
        using var factory = _factory.WithWebHostBuilder(b => b.UseEnvironment("Development"));
        Product? product = null;

        try
        {
            product = await CreateTemporaryProductAsync(factory.Services);
            await DeleteProductAsync(factory.Services, product.Id);

            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AcmeCoffeeDbContext>();

            var currentExists = await db.Products.AnyAsync(p => p.Id == product.Id);
            Assert.False(currentExists, "Deleted row must not be in the current temporal table.");

            var historicalVersions = await db.Products
                .TemporalAll()
                .Where(p => p.Id == product.Id)
                .ToListAsync();

            Assert.NotEmpty(historicalVersions);
            Assert.Contains(historicalVersions, p => p.Name == product.Name);
        }
        finally
        {
            if (product is not null)
            {
                await DeleteProductAsync(factory.Services, product.Id);
            }
        }
    }

    [Fact]
    public async Task Customer_TemporalAsOf_ReturnsValueAtPointInTimeAsync()
    {
        using var factory = _factory.WithWebHostBuilder(b => b.UseEnvironment("Development"));
        var existing = await GetFirstSeededCustomerAsync(factory.Services);
        var originalPhone = existing.Phone;
        var probeValue = $"555-PRB-{Guid.NewGuid().ToString("N")[..6]}";

        await Task.Delay(50);
        var beforeUpdate = DateTime.UtcNow;
        await Task.Delay(50);

        try
        {
            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AcmeCoffeeDbContext>();
                var tracked = await db.Customers.FirstAsync(c => c.Id == existing.Id);
                tracked.Phone = probeValue;
                tracked.ModifiedDate = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }

            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AcmeCoffeeDbContext>();

                var asOf = await db.Customers
                    .TemporalAsOf(beforeUpdate)
                    .FirstAsync(c => c.Id == existing.Id);

                Assert.Equal(originalPhone, asOf.Phone);

                var current = await db.Customers.FirstAsync(c => c.Id == existing.Id);
                Assert.Equal(probeValue, current.Phone);
            }
        }
        finally
        {
            await RestoreCustomerPhoneAsync(factory.Services, existing.Id, originalPhone);
        }
    }

    [Fact]
    public async Task Order_TemporalAll_ReturnsCurrentAndHistoricalVersionsAsync()
    {
        using var factory = _factory.WithWebHostBuilder(b => b.UseEnvironment("Development"));
        var existing = await GetFirstSeededOrderAsync(factory.Services);

        var originalNotes = existing.Notes;
        var probeMarker = $"temporal-probe-{Guid.NewGuid():N}";

        try
        {
            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AcmeCoffeeDbContext>();
                var tracked = await db.Orders.FirstAsync(o => o.Id == existing.Id);
                tracked.Notes = probeMarker;
                tracked.ModifiedDate = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }

            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AcmeCoffeeDbContext>();
                var versions = await db.Orders
                    .TemporalAll()
                    .Where(o => o.Id == existing.Id)
                    .ToListAsync();

                Assert.True(
                    versions.Count >= 2,
                    $"Expected at least two versions of order {existing.Id} after probe update; got {versions.Count}.");
                Assert.Contains(versions, o => o.Notes == probeMarker);
            }
        }
        finally
        {
            await RestoreOrderNotesAsync(factory.Services, existing.Id, originalNotes);
        }
    }

    private static async Task<Product> GetFirstSeededProductAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AcmeCoffeeDbContext>();
        return await db.Products.AsNoTracking().OrderBy(p => p.Id).FirstAsync();
    }

    private static async Task<Customer> GetFirstSeededCustomerAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AcmeCoffeeDbContext>();
        return await db.Customers.AsNoTracking().OrderBy(c => c.Id).FirstAsync();
    }

    private static async Task<Order> GetFirstSeededOrderAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AcmeCoffeeDbContext>();
        return await db.Orders.AsNoTracking().OrderBy(o => o.Id).FirstAsync();
    }

    private static async Task<Product> CreateTemporaryProductAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AcmeCoffeeDbContext>();

        var now = DateTime.UtcNow;
        var product = new Product
        {
            Name = $"Temporal Test Product {Guid.NewGuid():N}",
            RoastLevel = Shared.Enums.RoastLevel.Medium,
            CaffeineContent = Shared.Enums.CaffeineContent.Regular,
            GrindType = Shared.Enums.GrindType.WholeBean,
            UnitPrice = 9.99m,
            Description = "Initial description",
            IsAvailable = true,
            CreatedDate = now,
            ModifiedDate = now
        };

        db.Products.Add(product);
        await db.SaveChangesAsync();
        return product;
    }

    private static async Task DeleteProductAsync(IServiceProvider services, int productId)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AcmeCoffeeDbContext>();
        var tracked = await db.Products.FirstOrDefaultAsync(p => p.Id == productId);
        if (tracked is null)
        {
            return;
        }
        db.Products.Remove(tracked);
        await db.SaveChangesAsync();
    }

    private static async Task RestoreProductDescriptionAsync(
        IServiceProvider services, int productId, string? originalDescription)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AcmeCoffeeDbContext>();
        var tracked = await db.Products.FirstOrDefaultAsync(p => p.Id == productId);
        if (tracked is null)
        {
            return;
        }
        tracked.Description = originalDescription;
        tracked.ModifiedDate = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    private static async Task RestoreCustomerPhoneAsync(
        IServiceProvider services, int customerId, string? originalPhone)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AcmeCoffeeDbContext>();
        var tracked = await db.Customers.FirstOrDefaultAsync(c => c.Id == customerId);
        if (tracked is null)
        {
            return;
        }
        tracked.Phone = originalPhone;
        tracked.ModifiedDate = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    private static async Task RestoreOrderNotesAsync(
        IServiceProvider services, int orderId, string? originalNotes)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AcmeCoffeeDbContext>();
        var tracked = await db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        if (tracked is null)
        {
            return;
        }
        tracked.Notes = originalNotes;
        tracked.ModifiedDate = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    private sealed record TemporalTypeRow(string TableName, byte TemporalType);

    private sealed record HistoryTableRow(string HistoryTableName, string CurrentTableName);
}
