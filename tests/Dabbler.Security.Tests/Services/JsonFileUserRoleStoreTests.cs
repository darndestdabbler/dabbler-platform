using Dabbler.Security.Persistence;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dabbler.Security.Tests.Services;

public sealed class JsonFileUserRoleStoreTests : IDisposable
{
    private readonly string _tempFile;
    private readonly JsonFileUserRoleStore _store;

    public JsonFileUserRoleStoreTests()
    {
        _tempFile = Path.GetTempFileName();
        File.Delete(_tempFile); // Let the store create it fresh
        _store = new JsonFileUserRoleStore(_tempFile, NullLogger<JsonFileUserRoleStore>.Instance);
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile)) File.Delete(_tempFile);
    }

    [Fact]
    public async Task GetRolesAsync_WhenFileDoesNotExist_ReturnsEmpty()
    {
        var roles = await _store.GetRolesAsync("user-1");

        roles.Should().BeEmpty();
    }

    [Fact]
    public async Task AddRoleAsync_ThenGetRoles_ReturnsAddedRole()
    {
        await _store.AddRoleAsync("user-1", "Admin");

        var roles = await _store.GetRolesAsync("user-1");

        roles.Should().ContainSingle().Which.Should().Be("Admin");
    }

    [Fact]
    public async Task AddRoleAsync_DuplicateRole_DoesNotAddTwice()
    {
        await _store.AddRoleAsync("user-1", "Admin");
        await _store.AddRoleAsync("user-1", "Admin");

        var roles = await _store.GetRolesAsync("user-1");

        roles.Should().ContainSingle();
    }

    [Fact]
    public async Task RemoveRoleAsync_RemovesOnlyTargetRole()
    {
        await _store.AddRoleAsync("user-1", "Admin");
        await _store.AddRoleAsync("user-1", "Viewer");

        await _store.RemoveRoleAsync("user-1", "Admin");

        var roles = await _store.GetRolesAsync("user-1");
        roles.Should().ContainSingle().Which.Should().Be("Viewer");
    }

    [Fact]
    public async Task RemoveRoleAsync_WhenUserHasNoRolesLeft_RemovesUserEntry()
    {
        await _store.AddRoleAsync("user-1", "Admin");
        await _store.RemoveRoleAsync("user-1", "Admin");

        var all = await _store.GetAllAsync();

        all.Should().NotContainKey("user-1");
    }

    [Fact]
    public async Task SetRolesAsync_ReplacesExistingRoles()
    {
        await _store.AddRoleAsync("user-1", "Admin");
        await _store.AddRoleAsync("user-1", "Manager");

        await _store.SetRolesAsync("user-1", ["Viewer"]);

        var roles = await _store.GetRolesAsync("user-1");
        roles.Should().ContainSingle().Which.Should().Be("Viewer");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllUsersAndRoles()
    {
        await _store.AddRoleAsync("user-1", "Admin");
        await _store.AddRoleAsync("user-2", "Viewer");
        await _store.AddRoleAsync("user-2", "Manager");

        var all = await _store.GetAllAsync();

        all.Should().HaveCount(2);
        all["user-1"].Should().ContainSingle().Which.Should().Be("Admin");
        all["user-2"].Should().HaveCount(2);
    }

    [Fact]
    public async Task ConcurrentAddRole_DoesNotCorruptFile()
    {
        // Run two concurrent AddRole operations against the same store instance
        await Task.WhenAll(
            _store.AddRoleAsync("user-1", "Admin"),
            _store.AddRoleAsync("user-1", "Viewer"));

        // File must be valid and contain both roles (no corruption)
        var roles = await _store.GetRolesAsync("user-1");
        roles.Should().HaveCount(2);
        roles.Should().Contain("Admin");
        roles.Should().Contain("Viewer");
    }
}
