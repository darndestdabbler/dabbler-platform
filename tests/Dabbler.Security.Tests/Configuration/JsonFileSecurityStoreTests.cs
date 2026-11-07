using Dabbler.Security.Configuration;
using Dabbler.Security.Persistence;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dabbler.Security.Tests.Configuration;

public sealed class JsonFileSecurityStoreTests : IDisposable
{
    private readonly string _tempFile;
    private readonly JsonFileSecurityStore _store;

    public JsonFileSecurityStoreTests()
    {
        _tempFile = Path.GetTempFileName();
        File.Delete(_tempFile); // Let the store create it fresh
        _store = new JsonFileSecurityStore(_tempFile, NullLogger<JsonFileSecurityStore>.Instance);
    }

    public void Dispose() => File.Delete(_tempFile);

    [Fact]
    public async Task LoadAsync_WhenFileDoesNotExist_ReturnsEmptyConfiguration()
    {
        var config = await _store.LoadAsync();

        config.Roles.Should().BeEmpty();
        config.PagePermissions.Should().BeEmpty();
        config.EntityPermissions.Should().BeEmpty();
        config.FieldPermissions.Should().BeEmpty();
        config.EndpointPermissions.Should().BeEmpty();
        config.ControlPermissions.Should().BeEmpty();
        config.RowFilters.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RoundTripsFullConfiguration()
    {
        var original = BuildFullConfiguration();

        await _store.SaveAsync(original);
        var reloaded = await _store.LoadAsync();

        AssertConfigurationsEqual(original, reloaded);
    }

    [Fact]
    public async Task SaveAsync_CreatesFileInNonExistentDirectory()
    {
        var subDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "sub", "security.json");
        var store = new JsonFileSecurityStore(subDir, NullLogger<JsonFileSecurityStore>.Instance);
        try
        {
            await store.SaveAsync(new SecurityConfiguration { Roles = [new RoleDefinition { RoleName = "Admin" }] });

            File.Exists(subDir).Should().BeTrue();
            var loaded = await store.LoadAsync();
            loaded.Roles.Should().ContainSingle(r => r.RoleName == "Admin");
        }
        finally
        {
            File.Delete(subDir);
        }
    }

    [Fact]
    public async Task SaveAsync_IsAtomicViaTemp_DoesNotCorruptOnReload()
    {
        // Write initial state
        var first = new SecurityConfiguration { Roles = [new RoleDefinition { RoleName = "First" }] };
        await _store.SaveAsync(first);

        // Overwrite with second state
        var second = new SecurityConfiguration { Roles = [new RoleDefinition { RoleName = "Second" }] };
        await _store.SaveAsync(second);

        var loaded = await _store.LoadAsync();
        loaded.Roles.Should().ContainSingle(r => r.RoleName == "Second");
    }

    [Fact]
    public async Task RoundTrip_PreservesEnumValues()
    {
        var config = new SecurityConfiguration
        {
            FieldPermissions =
            [
                new FieldPermission
                {
                    EntityKey = "Order",
                    FieldKey = "CreditLimit",
                    RolePermissions =
                    [
                        new FieldRolePermission { RoleName = "Viewer", AccessLevel = FieldAccessLevel.Hidden },
                        new FieldRolePermission { RoleName = "Manager", AccessLevel = FieldAccessLevel.ReadOnly }
                    ]
                }
            ],
            ControlPermissions =
            [
                new ControlPermission
                {
                    PageKey = "Orders",
                    ControlKey = "DeleteButton",
                    RolePermissions =
                    [
                        new ControlRolePermission { RoleName = "Viewer", State = ControlState.Hidden },
                        new ControlRolePermission { RoleName = "Manager", State = ControlState.Disabled }
                    ]
                }
            ]
        };

        await _store.SaveAsync(config);
        var loaded = await _store.LoadAsync();

        loaded.FieldPermissions[0].RolePermissions
            .Single(r => r.RoleName == "Viewer").AccessLevel.Should().Be(FieldAccessLevel.Hidden);
        loaded.FieldPermissions[0].RolePermissions
            .Single(r => r.RoleName == "Manager").AccessLevel.Should().Be(FieldAccessLevel.ReadOnly);
        loaded.ControlPermissions[0].RolePermissions
            .Single(r => r.RoleName == "Viewer").State.Should().Be(ControlState.Hidden);
        loaded.ControlPermissions[0].RolePermissions
            .Single(r => r.RoleName == "Manager").State.Should().Be(ControlState.Disabled);
    }

    [Fact]
    public async Task RoundTrip_PreservesRoleInheritance()
    {
        var config = new SecurityConfiguration
        {
            Roles =
            [
                new RoleDefinition { RoleName = "Viewer", Description = "Read-only" },
                new RoleDefinition { RoleName = "Editor", Description = "Can edit", InheritsFrom = "Viewer" }
            ]
        };

        await _store.SaveAsync(config);
        var loaded = await _store.LoadAsync();

        var editor = loaded.Roles.Single(r => r.RoleName == "Editor");
        editor.InheritsFrom.Should().Be("Viewer");

        var viewer = loaded.Roles.Single(r => r.RoleName == "Viewer");
        viewer.InheritsFrom.Should().BeNull();
    }

    // --- helpers ---

    private static SecurityConfiguration BuildFullConfiguration() => new()
    {
        Roles =
        [
            new RoleDefinition { RoleName = "SystemAdmin", Description = "Full access" },
            new RoleDefinition { RoleName = "Manager", Description = "Operations", InheritsFrom = "Viewer" },
            new RoleDefinition { RoleName = "Viewer", Description = "Read-only" }
        ],
        PagePermissions =
        [
            new PagePermission
            {
                PageKey = "Dashboard",
                AllowedRoles = ["SystemAdmin", "Manager", "Viewer"],
                DeniedRoles = []
            },
            new PagePermission
            {
                PageKey = "Settings",
                AllowedRoles = ["SystemAdmin"],
                DeniedRoles = ["Viewer"]
            }
        ],
        EntityPermissions =
        [
            new EntityPermission
            {
                EntityKey = "Order",
                RolePermissions =
                [
                    new EntityRolePermission { RoleName = "Manager", AllowedOperations = ["Create", "Read", "Update", "Delete"] },
                    new EntityRolePermission { RoleName = "Viewer", AllowedOperations = ["Read"] }
                ]
            }
        ],
        FieldPermissions =
        [
            new FieldPermission
            {
                EntityKey = "Order",
                FieldKey = "Total",
                RolePermissions =
                [
                    new FieldRolePermission { RoleName = "Viewer", AccessLevel = FieldAccessLevel.ReadOnly }
                ]
            }
        ],
        EndpointPermissions =
        [
            new EndpointPermission
            {
                EndpointKey = "DELETE /api/orders/{id}",
                AllowedRoles = ["Manager", "SystemAdmin"]
            }
        ],
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
        ],
        RowFilters =
        [
            new RowFilter
            {
                EntityKey = "Order",
                RoleName = "Viewer",
                FilterExpression = "OwnerId == @CurrentUserId",
                Description = "Viewers see only their own orders"
            }
        ],
        RoleSwitching = new RoleSwitchingPermission { AllowedRoles = ["SystemAdmin"] }
    };

    private static void AssertConfigurationsEqual(SecurityConfiguration expected, SecurityConfiguration actual)
    {
        actual.Roles.Should().HaveCount(expected.Roles.Count);
        actual.PagePermissions.Should().HaveCount(expected.PagePermissions.Count);
        actual.EntityPermissions.Should().HaveCount(expected.EntityPermissions.Count);
        actual.FieldPermissions.Should().HaveCount(expected.FieldPermissions.Count);
        actual.EndpointPermissions.Should().HaveCount(expected.EndpointPermissions.Count);
        actual.ControlPermissions.Should().HaveCount(expected.ControlPermissions.Count);
        actual.RowFilters.Should().HaveCount(expected.RowFilters.Count);
        actual.RoleSwitching.AllowedRoles.Should().BeEquivalentTo(expected.RoleSwitching.AllowedRoles);

        // Spot-check key values
        actual.Roles.Select(r => r.RoleName).Should().BeEquivalentTo(expected.Roles.Select(r => r.RoleName));
        actual.EndpointPermissions[0].EndpointKey.Should().Be(expected.EndpointPermissions[0].EndpointKey);
        actual.RowFilters[0].FilterExpression.Should().Be(expected.RowFilters[0].FilterExpression);
    }
}
