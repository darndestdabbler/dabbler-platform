using System.Security.Claims;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Authorization.Handlers;
using Dabbler.Security.Authorization.Requirements;
using Dabbler.Security.Configuration;
using Dabbler.Security.Options;
using Dabbler.Security.Persistence;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace Dabbler.Security.Tests.Integration;

/// <summary>
/// Verifies that the AcmeCoffeeOnline security configuration enforces
/// the expected page, entity, field, control, and endpoint permissions
/// at runtime — including runtime modification and persistence round-trips.
/// </summary>
public class AcmeCoffeeSecurityEnforcementTests : IDisposable
{
    private readonly string _tempConfigPath;

    public AcmeCoffeeSecurityEnforcementTests()
    {
        _tempConfigPath = Path.Combine(Path.GetTempPath(), $"acme-security-{Guid.NewGuid()}.json");
    }

    public void Dispose()
    {
        if (File.Exists(_tempConfigPath))
            File.Delete(_tempConfigPath);
    }

    // ─── Page Enforcement ──────────────────────────────────────────────

    [Theory]
    [InlineData("SystemAdmin", "/admin/security", true)]
    [InlineData("BackofficeUserAdmin", "/admin/security", false)]
    [InlineData("BackofficeUser", "/admin/security", false)]
    [InlineData("Customer", "/admin/security", false)]
    [InlineData("SystemAdmin", "/admin/users", true)]
    [InlineData("BackofficeUserAdmin", "/admin/users", true)]
    [InlineData("BackofficeUser", "/admin/users", false)]
    [InlineData("Customer", "/admin/users", false)]
    [InlineData("SystemAdmin", "/products", true)]
    [InlineData("Customer", "/products", true)]
    [InlineData("SystemAdmin", "/customers", true)]
    [InlineData("BackofficeUser", "/customers", true)]
    [InlineData("Customer", "/customers", false)]
    public async Task PageAccess_EnforcesAcmeCoffeePermissions(
        string role, string pageKey, bool shouldSucceed)
    {
        var handler = CreatePageHandler(BuildAcmeCoffeeConfig());
        var user = UserWithRoles(role);
        var context = CreateContext(user, new PagePermissionRequirement(pageKey));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().Be(shouldSucceed);
    }

    [Fact]
    public async Task PageAccess_InheritedRole_BackofficeUserAdmin_AccessesCustomers()
    {
        // BackofficeUserAdmin inherits from BackofficeUser, which has /customers access
        var handler = CreatePageHandler(BuildAcmeCoffeeConfig());
        var user = UserWithRoles("BackofficeUserAdmin");
        var context = CreateContext(user, new PagePermissionRequirement("/customers"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue(
            "BackofficeUserAdmin inherits BackofficeUser which has /customers access");
    }

    [Fact]
    public async Task PageAccess_DenyTakesPrecedence()
    {
        var config = BuildAcmeCoffeeConfig();
        // Add BackofficeUser to denied roles for /products
        var productsPage = config.PagePermissions.First(p => p.PageKey == "/products");
        productsPage.DeniedRoles.Add("BackofficeUser");

        var handler = CreatePageHandler(config);
        var user = UserWithRoles("BackofficeUser");
        var context = CreateContext(user, new PagePermissionRequirement("/products"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse(
            "deny takes precedence even when role is also in AllowedRoles");
    }

    // ─── Entity Enforcement ────────────────────────────────────────────

    [Theory]
    [InlineData("SystemAdmin", "Product", "Create", true)]
    [InlineData("SystemAdmin", "Product", "Delete", true)]
    [InlineData("BackofficeUser", "Product", "Read", true)]
    [InlineData("BackofficeUser", "Product", "Update", true)]
    [InlineData("BackofficeUser", "Product", "Create", false)]
    [InlineData("BackofficeUser", "Product", "Delete", false)]
    [InlineData("Customer", "Product", "Read", true)]
    [InlineData("Customer", "Product", "Create", false)]
    [InlineData("Customer", "Product", "Update", false)]
    [InlineData("Customer", "Product", "Delete", false)]
    [InlineData("Customer", "Order", "Create", true)]
    [InlineData("Customer", "Order", "Read", true)]
    [InlineData("Customer", "Order", "Update", false)]
    [InlineData("Customer", "Order", "Delete", false)]
    [InlineData("BackofficeUser", "Order", "Create", true)]
    [InlineData("BackofficeUser", "Order", "Update", true)]
    [InlineData("BackofficeUser", "Order", "Delete", false)]
    public async Task EntityAccess_EnforcesAcmeCoffeeCrudMatrix(
        string role, string entityKey, string operation, bool shouldSucceed)
    {
        var handler = CreateEntityHandler(BuildAcmeCoffeeConfig());
        var user = UserWithRoles(role);
        var context = CreateContext(user, new EntityPermissionRequirement(entityKey, operation));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().Be(shouldSucceed);
    }

    [Fact]
    public async Task EntityAccess_RuntimeModification_RevokesPermission()
    {
        var config = BuildAcmeCoffeeConfig();

        // Baseline: BackofficeUser can Update Product
        var handler = CreateEntityHandler(config);
        var user = UserWithRoles("BackofficeUser");
        var baseline = CreateContext(user, new EntityPermissionRequirement("Product", "Update"));
        await handler.HandleAsync(baseline);
        baseline.HasSucceeded.Should().BeTrue("baseline: BackofficeUser can Update Product");

        // Runtime modification: remove Update from BackofficeUser on Product
        // (Customer doesn't have Update, so inheritance won't re-grant it)
        var productEntity = config.EntityPermissions.First(e => e.EntityKey == "Product");
        var backofficePerms = productEntity.RolePermissions.First(r => r.RoleName == "BackofficeUser");
        backofficePerms.AllowedOperations.Remove("Update");

        // Re-evaluate with modified config
        var store = new Mock<ISecurityConfigurationStore>();
        store.Setup(s => s.LoadAsync(default)).ReturnsAsync(config);
        var manager = new SecurityConfigurationManager(store.Object);
        var options = MsOptions.Create(new DabblerSecurityOptions());
        var modifiedHandler = new EntityAuthorizationHandler(manager, options, new SessionRoleSwitcher());
        var modified = CreateContext(user, new EntityPermissionRequirement("Product", "Update"));
        await modifiedHandler.HandleAsync(modified);

        modified.HasSucceeded.Should().BeFalse(
            "after revoking Update, BackofficeUser should no longer Update Product");
    }

    // ─── Field Visibility Enforcement ──────────────────────────────────

    [Theory]
    [InlineData("SystemAdmin", "Product", "UnitPrice", true)]
    [InlineData("BackofficeUserAdmin", "Product", "UnitPrice", true)]
    [InlineData("BackofficeUser", "Product", "UnitPrice", true)]   // ReadOnly still succeeds
    [InlineData("Customer", "Product", "UnitPrice", true)]          // ReadOnly still succeeds
    public async Task FieldVisibility_EnforcesAcmeCoffeeFieldPermissions(
        string role, string entityKey, string fieldKey, bool shouldSucceed)
    {
        var handler = CreateFieldHandler(BuildAcmeCoffeeConfig());
        var user = UserWithRoles(role);
        var context = CreateContext(user, new FieldPermissionRequirement(entityKey, fieldKey));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().Be(shouldSucceed);
    }

    [Fact]
    public async Task FieldVisibility_RuntimeChange_HiddenField_DeniesAccess()
    {
        var config = BuildAcmeCoffeeConfig();

        // Set UnitPrice to Hidden for Customer
        var unitPriceField = config.FieldPermissions
            .First(f => f.EntityKey == "Product" && f.FieldKey == "UnitPrice");
        var customerPerm = unitPriceField.RolePermissions
            .First(r => r.RoleName == "Customer");
        customerPerm.AccessLevel = FieldAccessLevel.Hidden;

        var handler = CreateFieldHandler(config);
        var user = UserWithRoles("Customer");
        var context = CreateContext(user, new FieldPermissionRequirement("Product", "UnitPrice"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse(
            "Hidden fields should deny access");
    }

    [Fact]
    public async Task FieldVisibility_MostPermissiveWins_WhenUserHasMultipleRoles()
    {
        var config = BuildAcmeCoffeeConfig();

        // Add a Hidden permission for a new role, give user both roles
        config.FieldPermissions.First(f => f.FieldKey == "UnitPrice")
            .RolePermissions.Add(new FieldRolePermission
            {
                RoleName = "Viewer",
                AccessLevel = FieldAccessLevel.Hidden
            });

        var handler = CreateFieldHandler(config);
        // User has both SystemAdmin (Editable) and Viewer (Hidden)
        var user = UserWithRoles("SystemAdmin", "Viewer");
        var context = CreateContext(user, new FieldPermissionRequirement("Product", "UnitPrice"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue(
            "most permissive (Editable from SystemAdmin) should win over Hidden from Viewer");
    }

    // ─── Control Permission Enforcement ────────────────────────────────

    [Theory]
    [InlineData("SystemAdmin", "/products", "CreateProductButton", true)]
    [InlineData("BackofficeUserAdmin", "/products", "CreateProductButton", true)]
    [InlineData("BackofficeUser", "/products", "CreateProductButton", false)]  // Hidden
    [InlineData("Customer", "/products", "CreateProductButton", false)]         // Hidden
    [InlineData("SystemAdmin", "/products", "EditPriceButton", true)]
    [InlineData("BackofficeUser", "/products", "EditPriceButton", true)]       // Disabled but visible
    [InlineData("Customer", "/products", "EditPriceButton", false)]             // Hidden
    [InlineData("SystemAdmin", "/orders", "DeleteOrderButton", true)]
    [InlineData("BackofficeUser", "/orders", "DeleteOrderButton", false)]       // Hidden
    [InlineData("Customer", "/orders", "UpdateStatusButton", false)]            // Hidden
    [InlineData("BackofficeUser", "/orders", "UpdateStatusButton", true)]       // Enabled
    public async Task ControlPermissions_EnforcesAcmeCoffeeControlStates(
        string role, string pageKey, string controlKey, bool shouldSucceed)
    {
        var handler = CreateControlHandler(BuildAcmeCoffeeConfig());
        var user = UserWithRoles(role);
        var context = CreateContext(user, new ControlPermissionRequirement(pageKey, controlKey));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().Be(shouldSucceed);
    }

    [Fact]
    public async Task ControlPermissions_RuntimeChange_HideControl_DeniesAccess()
    {
        var config = BuildAcmeCoffeeConfig();

        // Change UpdateStatusButton from Enabled to Hidden for BackofficeUser
        var control = config.ControlPermissions
            .First(c => c.ControlKey == "UpdateStatusButton");
        var backofficeState = control.RolePermissions
            .First(r => r.RoleName == "BackofficeUser");
        backofficeState.State = ControlState.Hidden;

        var handler = CreateControlHandler(config);
        var user = UserWithRoles("BackofficeUser");
        var context = CreateContext(user, new ControlPermissionRequirement("/orders", "UpdateStatusButton"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse(
            "after changing to Hidden, BackofficeUser should be denied");
    }

    // ─── Endpoint Enforcement ──────────────────────────────────────────

    [Theory]
    [InlineData("SystemAdmin", "CreateProduct", true)]
    [InlineData("BackofficeUserAdmin", "CreateProduct", true)]
    [InlineData("BackofficeUser", "CreateProduct", false)]
    [InlineData("Customer", "CreateProduct", false)]
    [InlineData("Customer", "GetProducts", true)]
    [InlineData("Customer", "CreateOrder", true)]
    [InlineData("Customer", "UpdateOrderStatus", false)]
    [InlineData("BackofficeUser", "UpdateOrderStatus", true)]
    [InlineData("BackofficeUser", "RemoveOrderItem", false)]
    [InlineData("BackofficeUserAdmin", "RemoveOrderItem", true)]
    public async Task EndpointAccess_EnforcesAcmeCoffeeEndpointPermissions(
        string role, string endpointKey, bool shouldSucceed)
    {
        var handler = CreateEndpointHandler(BuildAcmeCoffeeConfig());
        var user = UserWithRoles(role);
        var context = CreateContext(user, new EndpointPermissionRequirement(endpointKey));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().Be(shouldSucceed);
    }

    // ─── Persistence Round-Trip ────────────────────────────────────────

    [Fact]
    public async Task JsonFileStore_RoundTrip_PreservesAcmeCoffeeConfig()
    {
        var original = BuildAcmeCoffeeConfig();
        var store = new JsonFileSecurityStore(_tempConfigPath, NullLogger<JsonFileSecurityStore>.Instance);

        await store.SaveAsync(original);
        var loaded = await store.LoadAsync();

        loaded.Roles.Should().HaveCount(original.Roles.Count);
        loaded.PagePermissions.Should().HaveCount(original.PagePermissions.Count);
        loaded.EntityPermissions.Should().HaveCount(original.EntityPermissions.Count);
        loaded.FieldPermissions.Should().HaveCount(original.FieldPermissions.Count);
        loaded.EndpointPermissions.Should().HaveCount(original.EndpointPermissions.Count);
        loaded.ControlPermissions.Should().HaveCount(original.ControlPermissions.Count);
        loaded.RowFilters.Should().HaveCount(original.RowFilters.Count);
    }

    [Fact]
    public async Task JsonFileStore_RuntimeModification_PersistsAndEnforces()
    {
        var config = BuildAcmeCoffeeConfig();
        var store = new JsonFileSecurityStore(_tempConfigPath, NullLogger<JsonFileSecurityStore>.Instance);

        // Save baseline
        await store.SaveAsync(config);

        // Modify: deny Customer access to /products
        var productsPage = config.PagePermissions.First(p => p.PageKey == "/products");
        productsPage.AllowedRoles.Remove("Customer");

        // Save modification
        await store.SaveAsync(config);

        // Reload from file
        var reloaded = await store.LoadAsync();

        // Verify the change persisted
        var reloadedProducts = reloaded.PagePermissions.First(p => p.PageKey == "/products");
        reloadedProducts.AllowedRoles.Should().NotContain("Customer");

        // Verify enforcement with reloaded config
        var handler = CreatePageHandler(reloaded);
        var customer = UserWithRoles("Customer");
        var context = CreateContext(customer, new PagePermissionRequirement("/products"));
        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse(
            "after persisting revocation, Customer should be denied /products");
    }

    [Fact]
    public async Task SecurityConfigurationManager_SaveAndRefresh_EnforcesNewPermissions()
    {
        var initialConfig = BuildAcmeCoffeeConfig();
        var store = new JsonFileSecurityStore(_tempConfigPath, NullLogger<JsonFileSecurityStore>.Instance);
        await store.SaveAsync(initialConfig);

        var manager = new SecurityConfigurationManager(store);
        await manager.EnsureLoadedAsync();

        // Verify baseline
        var baselineConfig = manager.Current;
        baselineConfig.PagePermissions
            .First(p => p.PageKey == "/admin/security")
            .AllowedRoles.Should().ContainSingle("SystemAdmin");

        // Modify and save through the manager
        var modifiedConfig = BuildAcmeCoffeeConfig();
        modifiedConfig.PagePermissions
            .First(p => p.PageKey == "/admin/security")
            .AllowedRoles.Add("BackofficeUserAdmin");
        await manager.SaveAsync(modifiedConfig);

        // Verify the manager reflects the change
        manager.Current.PagePermissions
            .First(p => p.PageKey == "/admin/security")
            .AllowedRoles.Should().Contain("BackofficeUserAdmin");

        // Verify enforcement with the new config
        var handler = CreatePageHandler(manager.Current);
        var user = UserWithRoles("BackofficeUserAdmin");
        var context = CreateContext(user, new PagePermissionRequirement("/admin/security"));
        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue(
            "after adding BackofficeUserAdmin to /admin/security, access should be granted");
    }

    // ─── Unauthenticated User ──────────────────────────────────────────

    [Fact]
    public async Task UnauthenticatedUser_DeniedEverywhere()
    {
        var config = BuildAcmeCoffeeConfig();
        var unauthenticated = new ClaimsPrincipal(new ClaimsIdentity());

        var pageHandler = CreatePageHandler(config);
        var pageCtx = CreateContext(unauthenticated, new PagePermissionRequirement("/"));
        await pageHandler.HandleAsync(pageCtx);
        pageCtx.HasSucceeded.Should().BeFalse("unauthenticated user cannot access pages");

        var entityHandler = CreateEntityHandler(config);
        var entityCtx = CreateContext(unauthenticated, new EntityPermissionRequirement("Product", "Read"));
        await entityHandler.HandleAsync(entityCtx);
        entityCtx.HasSucceeded.Should().BeFalse("unauthenticated user cannot access entities");
    }

    // ─── Helpers ───────────────────────────────────────────────────────

    private static SecurityConfiguration BuildAcmeCoffeeConfig() => new()
    {
        Roles =
        [
            new RoleDefinition { RoleName = "SystemAdmin", Description = "Full system administrator" },
            new RoleDefinition { RoleName = "BackofficeUserAdmin", Description = "Operations admin", InheritsFrom = "BackofficeUser" },
            new RoleDefinition { RoleName = "BackofficeUser", Description = "Internal staff", InheritsFrom = "Customer" },
            new RoleDefinition { RoleName = "Customer", Description = "Retail-business account" }
        ],
        PagePermissions =
        [
            new PagePermission { PageKey = "/", AllowedRoles = ["SystemAdmin", "BackofficeUserAdmin", "BackofficeUser", "Customer"] },
            new PagePermission { PageKey = "/products", AllowedRoles = ["SystemAdmin", "BackofficeUserAdmin", "BackofficeUser", "Customer"] },
            new PagePermission { PageKey = "/customers", AllowedRoles = ["SystemAdmin", "BackofficeUserAdmin", "BackofficeUser"] },
            new PagePermission { PageKey = "/orders", AllowedRoles = ["SystemAdmin", "BackofficeUserAdmin", "BackofficeUser", "Customer"] },
            new PagePermission { PageKey = "/admin/users", AllowedRoles = ["SystemAdmin", "BackofficeUserAdmin"] },
            new PagePermission { PageKey = "/admin/products", AllowedRoles = ["SystemAdmin"] },
            new PagePermission { PageKey = "/admin/security", AllowedRoles = ["SystemAdmin"] }
        ],
        EntityPermissions =
        [
            new EntityPermission
            {
                EntityKey = "Product",
                RolePermissions =
                [
                    new EntityRolePermission { RoleName = "SystemAdmin", AllowedOperations = ["Create", "Read", "Update", "Delete"] },
                    new EntityRolePermission { RoleName = "BackofficeUserAdmin", AllowedOperations = ["Create", "Read", "Update", "Delete"] },
                    new EntityRolePermission { RoleName = "BackofficeUser", AllowedOperations = ["Read", "Update"] },
                    new EntityRolePermission { RoleName = "Customer", AllowedOperations = ["Read"] }
                ]
            },
            new EntityPermission
            {
                EntityKey = "Customer",
                RolePermissions =
                [
                    new EntityRolePermission { RoleName = "SystemAdmin", AllowedOperations = ["Create", "Read", "Update", "Delete"] },
                    new EntityRolePermission { RoleName = "BackofficeUserAdmin", AllowedOperations = ["Create", "Read", "Update", "Delete"] },
                    new EntityRolePermission { RoleName = "BackofficeUser", AllowedOperations = ["Read", "Update"] },
                    new EntityRolePermission { RoleName = "Customer", AllowedOperations = ["Read"] }
                ]
            },
            new EntityPermission
            {
                EntityKey = "Order",
                RolePermissions =
                [
                    new EntityRolePermission { RoleName = "SystemAdmin", AllowedOperations = ["Create", "Read", "Update", "Delete"] },
                    new EntityRolePermission { RoleName = "BackofficeUserAdmin", AllowedOperations = ["Create", "Read", "Update", "Delete"] },
                    new EntityRolePermission { RoleName = "BackofficeUser", AllowedOperations = ["Create", "Read", "Update"] },
                    new EntityRolePermission { RoleName = "Customer", AllowedOperations = ["Create", "Read"] }
                ]
            },
            new EntityPermission
            {
                EntityKey = "OrderItem",
                RolePermissions =
                [
                    new EntityRolePermission { RoleName = "SystemAdmin", AllowedOperations = ["Create", "Read", "Update", "Delete"] },
                    new EntityRolePermission { RoleName = "BackofficeUserAdmin", AllowedOperations = ["Create", "Read", "Update", "Delete"] },
                    new EntityRolePermission { RoleName = "BackofficeUser", AllowedOperations = ["Create", "Read", "Update"] },
                    new EntityRolePermission { RoleName = "Customer", AllowedOperations = ["Read"] }
                ]
            },
            new EntityPermission
            {
                EntityKey = "OrderNote",
                RolePermissions =
                [
                    new EntityRolePermission { RoleName = "SystemAdmin", AllowedOperations = ["Create", "Read", "Update", "Delete"] },
                    new EntityRolePermission { RoleName = "BackofficeUserAdmin", AllowedOperations = ["Create", "Read", "Update", "Delete"] },
                    new EntityRolePermission { RoleName = "BackofficeUser", AllowedOperations = ["Create", "Read", "Update", "Delete"] },
                    new EntityRolePermission { RoleName = "Customer", AllowedOperations = ["Read"] }
                ]
            }
        ],
        FieldPermissions =
        [
            new FieldPermission
            {
                EntityKey = "Product",
                FieldKey = "UnitPrice",
                RolePermissions =
                [
                    new FieldRolePermission { RoleName = "SystemAdmin", AccessLevel = FieldAccessLevel.Editable },
                    new FieldRolePermission { RoleName = "BackofficeUserAdmin", AccessLevel = FieldAccessLevel.Editable },
                    new FieldRolePermission { RoleName = "BackofficeUser", AccessLevel = FieldAccessLevel.ReadOnly },
                    new FieldRolePermission { RoleName = "Customer", AccessLevel = FieldAccessLevel.ReadOnly }
                ]
            }
        ],
        EndpointPermissions =
        [
            new EndpointPermission { EndpointKey = "GetProducts", AllowedRoles = ["SystemAdmin", "BackofficeUserAdmin", "BackofficeUser", "Customer"] },
            new EndpointPermission { EndpointKey = "GetProductById", AllowedRoles = ["SystemAdmin", "BackofficeUserAdmin", "BackofficeUser", "Customer"] },
            new EndpointPermission { EndpointKey = "CreateProduct", AllowedRoles = ["SystemAdmin", "BackofficeUserAdmin"] },
            new EndpointPermission { EndpointKey = "UpdateProduct", AllowedRoles = ["SystemAdmin", "BackofficeUserAdmin", "BackofficeUser"] },
            new EndpointPermission { EndpointKey = "ToggleProductAvailability", AllowedRoles = ["SystemAdmin", "BackofficeUserAdmin"] },
            new EndpointPermission { EndpointKey = "GetCustomers", AllowedRoles = ["SystemAdmin", "BackofficeUserAdmin", "BackofficeUser"] },
            new EndpointPermission { EndpointKey = "GetCustomerById", AllowedRoles = ["SystemAdmin", "BackofficeUserAdmin", "BackofficeUser"] },
            new EndpointPermission { EndpointKey = "CreateCustomer", AllowedRoles = ["SystemAdmin", "BackofficeUserAdmin"] },
            new EndpointPermission { EndpointKey = "UpdateCustomer", AllowedRoles = ["SystemAdmin", "BackofficeUserAdmin", "BackofficeUser"] },
            new EndpointPermission { EndpointKey = "SearchCustomers", AllowedRoles = ["SystemAdmin", "BackofficeUserAdmin", "BackofficeUser"] },
            new EndpointPermission { EndpointKey = "GetOrders", AllowedRoles = ["SystemAdmin", "BackofficeUserAdmin", "BackofficeUser", "Customer"] },
            new EndpointPermission { EndpointKey = "GetOrderById", AllowedRoles = ["SystemAdmin", "BackofficeUserAdmin", "BackofficeUser", "Customer"] },
            new EndpointPermission { EndpointKey = "GetOrdersByCustomer", AllowedRoles = ["SystemAdmin", "BackofficeUserAdmin", "BackofficeUser", "Customer"] },
            new EndpointPermission { EndpointKey = "CreateOrder", AllowedRoles = ["SystemAdmin", "BackofficeUserAdmin", "BackofficeUser", "Customer"] },
            new EndpointPermission { EndpointKey = "UpdateOrderStatus", AllowedRoles = ["SystemAdmin", "BackofficeUserAdmin", "BackofficeUser"] },
            new EndpointPermission { EndpointKey = "AddOrderItem", AllowedRoles = ["SystemAdmin", "BackofficeUserAdmin", "BackofficeUser"] },
            new EndpointPermission { EndpointKey = "RemoveOrderItem", AllowedRoles = ["SystemAdmin", "BackofficeUserAdmin"] },
            new EndpointPermission { EndpointKey = "GetOrderNotes", AllowedRoles = ["SystemAdmin", "BackofficeUserAdmin", "BackofficeUser", "Customer"] },
            new EndpointPermission { EndpointKey = "CreateOrderNote", AllowedRoles = ["SystemAdmin", "BackofficeUserAdmin", "BackofficeUser"] },
            new EndpointPermission { EndpointKey = "UpdateOrderNote", AllowedRoles = ["SystemAdmin", "BackofficeUserAdmin", "BackofficeUser"] },
            new EndpointPermission { EndpointKey = "DeleteOrderNote", AllowedRoles = ["SystemAdmin", "BackofficeUserAdmin"] }
        ],
        ControlPermissions =
        [
            new ControlPermission
            {
                PageKey = "/products", ControlKey = "CreateProductButton",
                RolePermissions =
                [
                    new ControlRolePermission { RoleName = "SystemAdmin", State = ControlState.Enabled },
                    new ControlRolePermission { RoleName = "BackofficeUserAdmin", State = ControlState.Enabled },
                    new ControlRolePermission { RoleName = "BackofficeUser", State = ControlState.Hidden },
                    new ControlRolePermission { RoleName = "Customer", State = ControlState.Hidden }
                ]
            },
            new ControlPermission
            {
                PageKey = "/products", ControlKey = "EditPriceButton",
                RolePermissions =
                [
                    new ControlRolePermission { RoleName = "SystemAdmin", State = ControlState.Enabled },
                    new ControlRolePermission { RoleName = "BackofficeUserAdmin", State = ControlState.Enabled },
                    new ControlRolePermission { RoleName = "BackofficeUser", State = ControlState.Disabled },
                    new ControlRolePermission { RoleName = "Customer", State = ControlState.Hidden }
                ]
            },
            new ControlPermission
            {
                PageKey = "/customers", ControlKey = "DeleteCustomerButton",
                RolePermissions =
                [
                    new ControlRolePermission { RoleName = "SystemAdmin", State = ControlState.Enabled },
                    new ControlRolePermission { RoleName = "BackofficeUserAdmin", State = ControlState.Enabled },
                    new ControlRolePermission { RoleName = "BackofficeUser", State = ControlState.Hidden }
                ]
            },
            new ControlPermission
            {
                PageKey = "/orders", ControlKey = "DeleteOrderButton",
                RolePermissions =
                [
                    new ControlRolePermission { RoleName = "SystemAdmin", State = ControlState.Enabled },
                    new ControlRolePermission { RoleName = "BackofficeUserAdmin", State = ControlState.Enabled },
                    new ControlRolePermission { RoleName = "BackofficeUser", State = ControlState.Hidden },
                    new ControlRolePermission { RoleName = "Customer", State = ControlState.Hidden }
                ]
            },
            new ControlPermission
            {
                PageKey = "/orders", ControlKey = "UpdateStatusButton",
                RolePermissions =
                [
                    new ControlRolePermission { RoleName = "SystemAdmin", State = ControlState.Enabled },
                    new ControlRolePermission { RoleName = "BackofficeUserAdmin", State = ControlState.Enabled },
                    new ControlRolePermission { RoleName = "BackofficeUser", State = ControlState.Enabled },
                    new ControlRolePermission { RoleName = "Customer", State = ControlState.Hidden }
                ]
            }
        ],
        RowFilters =
        [
            new RowFilter
            {
                EntityKey = "Order",
                RoleName = "Customer",
                FilterExpression = "CustomerId == @CurrentUserId",
                Description = "Customers see only their own orders."
            }
        ],
        RoleSwitching = new RoleSwitchingPermission
        {
            AllowedRoles = ["SystemAdmin", "BackofficeUserAdmin"]
        }
    };

    private static PageAuthorizationHandler CreatePageHandler(SecurityConfiguration config)
    {
        var store = new Mock<ISecurityConfigurationStore>();
        store.Setup(s => s.LoadAsync(default)).ReturnsAsync(config);
        var manager = new SecurityConfigurationManager(store.Object);
        var options = MsOptions.Create(new DabblerSecurityOptions
        {
            UnregisteredResourcePolicy = UnregisteredResourcePolicy.FailOpen
        });
        return new PageAuthorizationHandler(manager, options, new SessionRoleSwitcher());
    }

    private static EntityAuthorizationHandler CreateEntityHandler(SecurityConfiguration config)
    {
        var store = new Mock<ISecurityConfigurationStore>();
        store.Setup(s => s.LoadAsync(default)).ReturnsAsync(config);
        var manager = new SecurityConfigurationManager(store.Object);
        var options = MsOptions.Create(new DabblerSecurityOptions());
        return new EntityAuthorizationHandler(manager, options, new SessionRoleSwitcher());
    }

    private static FieldAuthorizationHandler CreateFieldHandler(SecurityConfiguration config)
    {
        var store = new Mock<ISecurityConfigurationStore>();
        store.Setup(s => s.LoadAsync(default)).ReturnsAsync(config);
        var manager = new SecurityConfigurationManager(store.Object);
        var options = MsOptions.Create(new DabblerSecurityOptions());
        return new FieldAuthorizationHandler(manager, options, new SessionRoleSwitcher());
    }

    private static ControlAuthorizationHandler CreateControlHandler(SecurityConfiguration config)
    {
        var store = new Mock<ISecurityConfigurationStore>();
        store.Setup(s => s.LoadAsync(default)).ReturnsAsync(config);
        var manager = new SecurityConfigurationManager(store.Object);
        var options = MsOptions.Create(new DabblerSecurityOptions());
        return new ControlAuthorizationHandler(manager, options, new SessionRoleSwitcher());
    }

    private static EndpointAuthorizationHandler CreateEndpointHandler(SecurityConfiguration config)
    {
        var store = new Mock<ISecurityConfigurationStore>();
        store.Setup(s => s.LoadAsync(default)).ReturnsAsync(config);
        var manager = new SecurityConfigurationManager(store.Object);
        var options = MsOptions.Create(new DabblerSecurityOptions());
        return new EndpointAuthorizationHandler(manager, options, new SessionRoleSwitcher());
    }

    private static ClaimsPrincipal UserWithRoles(params string[] roles)
    {
        var claims = roles.Select(r => new Claim(ClaimTypes.Role, r))
            .Append(new Claim(ClaimTypes.NameIdentifier, "test-user-1"))
            .ToArray();
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    private static AuthorizationHandlerContext CreateContext<T>(
        ClaimsPrincipal user, T requirement) where T : IAuthorizationRequirement
        => new([requirement], user, null);
}
