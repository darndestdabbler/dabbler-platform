using Dabbler.Security.Configuration;
using Dabbler.Security.Data;
using Dabbler.Security.Entities;
using Dabbler.Security.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dabbler.Security.Tests.Configuration;

public sealed class EfCoreSecurityStoreTests
{
    private static IDbContextFactory<DabblerSecurityDbContext> CreateFactory()
    {
        var options = new DbContextOptionsBuilder<DabblerSecurityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TestDbContextFactory(options);
    }

    private static async Task<(IDbContextFactory<DabblerSecurityDbContext>, AppRole manager, AppRole viewer)>
        CreateFactoryWithRolesAsync()
    {
        var factory = CreateFactory();
        await using var db = await factory.CreateDbContextAsync();
        await db.Database.EnsureCreatedAsync();

        var manager = new AppRole { Name = "Manager", Description = "Ops" };
        var viewer = new AppRole { Name = "Viewer", Description = "Read-only" };
        db.AppRoles.AddRange(manager, viewer);
        await db.SaveChangesAsync();

        return (factory, manager, viewer);
    }

    [Fact]
    public async Task LoadAsync_WhenNoData_ReturnsEmptyConfiguration()
    {
        var factory = CreateFactory();
        await using (var db = await factory.CreateDbContextAsync())
            await db.Database.EnsureCreatedAsync();

        var store = new EfCoreSecurityStore(factory);
        var config = await store.LoadAsync();

        config.Roles.Should().BeEmpty();
        config.EntityPermissions.Should().BeEmpty();
        config.PagePermissions.Should().BeEmpty();
        config.FieldPermissions.Should().BeEmpty();
        config.EndpointPermissions.Should().BeEmpty();
        config.ControlPermissions.Should().BeEmpty();
        config.RowFilters.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_MapsRolesFromDatabase()
    {
        var (factory, _, _) = await CreateFactoryWithRolesAsync();
        var store = new EfCoreSecurityStore(factory);

        var config = await store.LoadAsync();

        config.Roles.Should().HaveCount(2);
        config.Roles.Should().Contain(r => r.RoleName == "Manager");
        config.Roles.Should().Contain(r => r.RoleName == "Viewer");
    }

    [Fact]
    public async Task LoadAsync_MapsRoleInheritance()
    {
        var factory = CreateFactory();
        await using (var db = await factory.CreateDbContextAsync())
        {
            await db.Database.EnsureCreatedAsync();
            db.AppRoles.Add(new AppRole { Name = "Editor", Description = "Edits", InheritsFromRoleName = "Viewer" });
            db.AppRoles.Add(new AppRole { Name = "Viewer", Description = "Reads" });
            await db.SaveChangesAsync();
        }

        var store = new EfCoreSecurityStore(factory);
        var config = await store.LoadAsync();

        config.Roles.Single(r => r.RoleName == "Editor").InheritsFrom.Should().Be("Viewer");
        config.Roles.Single(r => r.RoleName == "Viewer").InheritsFrom.Should().BeNull();
    }

    [Fact]
    public async Task SaveAsync_ThenLoad_RoundTripsEntityPermissions()
    {
        var (factory, _, _) = await CreateFactoryWithRolesAsync();
        var store = new EfCoreSecurityStore(factory);

        var config = new SecurityConfiguration
        {
            EntityPermissions =
            [
                new EntityPermission
                {
                    EntityKey = "Order",
                    RolePermissions =
                    [
                        new EntityRolePermission { RoleName = "Manager", AllowedOperations = ["Create", "Read", "Update"] },
                        new EntityRolePermission { RoleName = "Viewer", AllowedOperations = ["Read"] }
                    ]
                }
            ]
        };

        await store.SaveAsync(config);
        var loaded = await store.LoadAsync();

        var order = loaded.EntityPermissions.Should().ContainSingle(e => e.EntityKey == "Order").Subject;
        order.RolePermissions.Should().HaveCount(2);
        order.RolePermissions.Single(r => r.RoleName == "Manager").AllowedOperations
            .Should().BeEquivalentTo(["Create", "Read", "Update"]);
        order.RolePermissions.Single(r => r.RoleName == "Viewer").AllowedOperations
            .Should().BeEquivalentTo(["Read"]);
    }

    [Fact]
    public async Task SaveAsync_ThenLoad_RoundTripsEndpointPermissions()
    {
        var (factory, _, _) = await CreateFactoryWithRolesAsync();
        var store = new EfCoreSecurityStore(factory);

        var config = new SecurityConfiguration
        {
            EndpointPermissions =
            [
                new EndpointPermission
                {
                    EndpointKey = "DELETE /api/orders/{id}",
                    AllowedRoles = ["Manager"]
                }
            ]
        };

        await store.SaveAsync(config);
        var loaded = await store.LoadAsync();

        var ep = loaded.EndpointPermissions.Should().ContainSingle().Subject;
        ep.EndpointKey.Should().Be("DELETE /api/orders/{id}");
        ep.AllowedRoles.Should().ContainSingle("Manager");
    }

    [Fact]
    public async Task SaveAsync_ThenLoad_RoundTripsEndpointPermissions_WithAllowAndDeny()
    {
        var (factory, _, _) = await CreateFactoryWithRolesAsync();
        var store = new EfCoreSecurityStore(factory);

        var config = new SecurityConfiguration
        {
            EndpointPermissions =
            [
                new EndpointPermission
                {
                    EndpointKey = "POST /api/orders",
                    AllowedRoles = ["Manager"],
                    DeniedRoles = ["Viewer"]
                }
            ]
        };

        await store.SaveAsync(config);
        var loaded = await store.LoadAsync();

        var ep = loaded.EndpointPermissions.Should().ContainSingle().Subject;
        ep.EndpointKey.Should().Be("POST /api/orders");
        ep.AllowedRoles.Should().ContainSingle("Manager");
        ep.DeniedRoles.Should().ContainSingle("Viewer");
    }

    [Fact]
    public async Task SaveAsync_ThenLoad_RoundTripsControlPermissions()
    {
        var (factory, _, _) = await CreateFactoryWithRolesAsync();
        var store = new EfCoreSecurityStore(factory);

        var config = new SecurityConfiguration
        {
            ControlPermissions =
            [
                new ControlPermission
                {
                    PageKey = "Orders",
                    ControlKey = "DeleteButton",
                    RolePermissions =
                    [
                        new ControlRolePermission { RoleName = "Viewer", State = ControlState.Hidden }
                    ]
                }
            ]
        };

        await store.SaveAsync(config);
        var loaded = await store.LoadAsync();

        var cp = loaded.ControlPermissions.Should().ContainSingle().Subject;
        cp.PageKey.Should().Be("Orders");
        cp.ControlKey.Should().Be("DeleteButton");
        cp.RolePermissions.Single(r => r.RoleName == "Viewer").State.Should().Be(ControlState.Hidden);
    }

    [Fact]
    public async Task SaveAsync_ThenLoad_RoundTripsRowFilters()
    {
        var (factory, _, _) = await CreateFactoryWithRolesAsync();
        var store = new EfCoreSecurityStore(factory);

        var config = new SecurityConfiguration
        {
            RowFilters =
            [
                new RowFilter
                {
                    EntityKey = "Order",
                    RoleName = "Viewer",
                    FilterExpression = "OwnerId == @CurrentUserId",
                    Description = "Own orders only"
                }
            ]
        };

        await store.SaveAsync(config);
        var loaded = await store.LoadAsync();

        var rf = loaded.RowFilters.Should().ContainSingle().Subject;
        rf.EntityKey.Should().Be("Order");
        rf.RoleName.Should().Be("Viewer");
        rf.FilterExpression.Should().Be("OwnerId == @CurrentUserId");
    }

    [Fact]
    public async Task SaveAsync_ReplacesExistingData_NotAccumulates()
    {
        var (factory, _, _) = await CreateFactoryWithRolesAsync();
        var store = new EfCoreSecurityStore(factory);

        var first = new SecurityConfiguration
        {
            EndpointPermissions = [new EndpointPermission { EndpointKey = "GET /api/old", AllowedRoles = ["Manager"] }]
        };
        await store.SaveAsync(first);

        var second = new SecurityConfiguration
        {
            EndpointPermissions = [new EndpointPermission { EndpointKey = "GET /api/new", AllowedRoles = ["Viewer"] }]
        };
        await store.SaveAsync(second);

        var loaded = await store.LoadAsync();
        loaded.EndpointPermissions.Should().ContainSingle(e => e.EndpointKey == "GET /api/new");
        loaded.EndpointPermissions.Should().NotContain(e => e.EndpointKey == "GET /api/old");
    }

    [Fact]
    public async Task SaveAsync_SkipsPermissionsForUnknownRoles()
    {
        var factory = CreateFactory();
        await using (var db = await factory.CreateDbContextAsync())
            await db.Database.EnsureCreatedAsync();

        var store = new EfCoreSecurityStore(factory);

        var config = new SecurityConfiguration
        {
            EndpointPermissions =
            [
                new EndpointPermission
                {
                    EndpointKey = "GET /api/test",
                    AllowedRoles = ["NonExistentRole"]
                }
            ]
        };

        // Should not throw — unknown roles are silently skipped
        await store.SaveAsync(config);

        var loaded = await store.LoadAsync();
        // The endpoint key exists but with no roles (since the role didn't exist)
        loaded.EndpointPermissions.Should().BeEmpty();
    }

    // --- test helper ---

    private sealed class TestDbContextFactory(DbContextOptions<DabblerSecurityDbContext> options)
        : IDbContextFactory<DabblerSecurityDbContext>
    {
        public DabblerSecurityDbContext CreateDbContext() => new(options);
    }
}
