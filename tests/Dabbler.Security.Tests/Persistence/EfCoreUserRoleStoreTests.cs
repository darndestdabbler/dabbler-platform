using Dabbler.Security.Data;
using Dabbler.Security.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dabbler.Security.Tests.Persistence;

/// <summary>
/// Round-trip coverage for the EF-backed IUserRoleStore implementation. The admin UI
/// (DabblerUserRoleManager) writes through this store when the host app is configured
/// with UseEfCoreStore, so AddRoleAsync/GetRolesAsync must persist to and read back
/// from the AppUserRoleAssignments table.
/// </summary>
public sealed class EfCoreUserRoleStoreTests
{
    private static IDbContextFactory<DabblerSecurityDbContext> BuildFactory()
    {
        var options = new DbContextOptionsBuilder<DabblerSecurityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestDbContextFactory(options);
    }

    [Fact]
    public async Task AddRoleAsync_PersistsAssignmentAndGetRolesReturnsIt()
    {
        var store = new EfCoreUserRoleStore(BuildFactory());

        await store.AddRoleAsync("42", "SystemAdmin");

        var roles = await store.GetRolesAsync("42");
        roles.Should().ContainSingle().Which.Should().Be("SystemAdmin");
    }

    [Fact]
    public async Task AddRoleAsync_IsIdempotentForDuplicateAssignments()
    {
        var store = new EfCoreUserRoleStore(BuildFactory());

        await store.AddRoleAsync("7", "BackofficeUser");
        await store.AddRoleAsync("7", "BackofficeUser");

        var roles = await store.GetRolesAsync("7");
        roles.Should().ContainSingle().Which.Should().Be("BackofficeUser");
    }

    [Fact]
    public async Task GetAllAsync_GroupsAssignmentsByUserId()
    {
        var store = new EfCoreUserRoleStore(BuildFactory());

        await store.AddRoleAsync("1", "SystemAdmin");
        await store.AddRoleAsync("2", "BackofficeUser");
        await store.AddRoleAsync("2", "BackofficeUserAdmin");

        var all = await store.GetAllAsync();

        all["1"].Should().ContainSingle().Which.Should().Be("SystemAdmin");
        all["2"].Should().BeEquivalentTo(["BackofficeUser", "BackofficeUserAdmin"]);
    }

    [Fact]
    public async Task RemoveRoleAsync_DeletesOnlyTheMatchingAssignment()
    {
        var store = new EfCoreUserRoleStore(BuildFactory());
        await store.AddRoleAsync("9", "SystemAdmin");
        await store.AddRoleAsync("9", "BackofficeUser");

        await store.RemoveRoleAsync("9", "SystemAdmin");

        var roles = await store.GetRolesAsync("9");
        roles.Should().ContainSingle().Which.Should().Be("BackofficeUser");
    }

    private sealed class TestDbContextFactory : IDbContextFactory<DabblerSecurityDbContext>
    {
        private readonly DbContextOptions<DabblerSecurityDbContext> _options;

        public TestDbContextFactory(DbContextOptions<DabblerSecurityDbContext> options)
        {
            _options = options;
        }

        public DabblerSecurityDbContext CreateDbContext() => new(_options);
    }
}
