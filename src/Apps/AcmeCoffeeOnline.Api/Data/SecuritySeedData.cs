using Dabbler.Security.Abstractions;
using Dabbler.Security.Configuration;

namespace AcmeCoffeeOnline.Api.Data;

/// <summary>
/// Provides the default runtime security configuration (roles, permissions, row filters)
/// for the sample distributor application. Called during database initialization to seed
/// the JSON file store if it has not been configured yet.
/// </summary>
public static class SecuritySeedData
{
    /// <summary>
    /// Returns the default SecurityConfiguration with the sample app role hierarchy
    /// and permission matrix.
    /// </summary>
    public static SecurityConfiguration GetDefaultConfiguration()
    {
        return new SecurityConfiguration
        {
            Roles = GetRoleDefinitions(),
            PagePermissions = GetPagePermissions(),
            EntityPermissions = GetEntityPermissions(),
            FieldPermissions = GetFieldPermissions(),
            EndpointPermissions = GetEndpointPermissions(),
            ControlPermissions = GetControlPermissions(),
            RowFilters = GetRowFilters(),
            RoleSwitching = new RoleSwitchingPermission
            {
                AllowedRoles = ["SystemAdmin"]
            }
        };
    }

    /// <summary>
    /// Seeds the security configuration store if it contains no role definitions.
    /// </summary>
    public static async Task SeedAsync(IServiceProvider services)
    {
        var store = services.GetRequiredService<ISecurityConfigurationStore>();
        var existing = await store.LoadAsync();

        if (existing.Roles.Count > 0)
            return; // Already seeded

        var config = GetDefaultConfiguration();
        await store.SaveAsync(config);
    }

    private static List<RoleDefinition> GetRoleDefinitions() =>
    [
        new()
        {
            RoleName = "Customer",
            Description = "Retail-business account with self-service access to its own orders and profile.",
            InheritsFrom = null
        },
        new()
        {
            RoleName = "BackofficeUser",
            Description = "Internal staff with read/update access to all retail-business accounts and orders.",
            InheritsFrom = "Customer"
        },
        new()
        {
            RoleName = "BackofficeUserAdmin",
            Description = "Internal operations admin who can manage staff accounts and roles.",
            InheritsFrom = "BackofficeUser"
        },
        new()
        {
            RoleName = "SystemAdmin",
            Description = "Full system administrator with unrestricted access.",
            InheritsFrom = "BackofficeUserAdmin"
        }
    ];

    private static List<PagePermission> GetPagePermissions() =>
    [
        new() { PageKey = "/", AllowedRoles = ["Customer"], DeniedRoles = [] },
        new() { PageKey = "/products", AllowedRoles = ["Customer"], DeniedRoles = [] },
        new() { PageKey = "/customers", AllowedRoles = ["BackofficeUser"], DeniedRoles = [] },
        new() { PageKey = "/orders", AllowedRoles = ["Customer"], DeniedRoles = [] },
        new() { PageKey = "/admin/users", AllowedRoles = ["BackofficeUserAdmin"], DeniedRoles = [] },
        new() { PageKey = "/admin/products", AllowedRoles = ["SystemAdmin"], DeniedRoles = [] },
        new() { PageKey = "/admin/security", AllowedRoles = ["SystemAdmin"], DeniedRoles = [] }
    ];

    private static List<EntityPermission> GetEntityPermissions() =>
    [
        new()
        {
            EntityKey = "Product",
            RolePermissions =
            [
                new() { RoleName = "Customer", AllowedOperations = ["Read"] },
                new() { RoleName = "BackofficeUser", AllowedOperations = ["Read", "Update"] },
                new() { RoleName = "SystemAdmin", AllowedOperations = ["Create", "Read", "Update", "Delete"] }
            ]
        },
        new()
        {
            EntityKey = "Customer",
            RolePermissions =
            [
                new() { RoleName = "Customer", AllowedOperations = ["Read", "Update"] },
                new() { RoleName = "BackofficeUser", AllowedOperations = ["Read", "Update"] },
                new() { RoleName = "BackofficeUserAdmin", AllowedOperations = ["Create", "Read", "Update"] },
                new() { RoleName = "SystemAdmin", AllowedOperations = ["Create", "Read", "Update", "Delete"] }
            ]
        },
        new()
        {
            EntityKey = "Order",
            RolePermissions =
            [
                new() { RoleName = "Customer", AllowedOperations = ["Create", "Read"] },
                new() { RoleName = "BackofficeUser", AllowedOperations = ["Read", "Update"] },
                new() { RoleName = "SystemAdmin", AllowedOperations = ["Create", "Read", "Update", "Delete"] }
            ]
        },
        new()
        {
            EntityKey = "OrderItem",
            RolePermissions =
            [
                new() { RoleName = "Customer", AllowedOperations = ["Create", "Read"] },
                new() { RoleName = "BackofficeUser", AllowedOperations = ["Read", "Update"] },
                new() { RoleName = "SystemAdmin", AllowedOperations = ["Create", "Read", "Update", "Delete"] }
            ]
        },
        new()
        {
            EntityKey = "OrderNote",
            RolePermissions =
            [
                new() { RoleName = "Customer", AllowedOperations = ["Create", "Read"] },
                new() { RoleName = "BackofficeUser", AllowedOperations = ["Create", "Read", "Update"] },
                new() { RoleName = "SystemAdmin", AllowedOperations = ["Create", "Read", "Update", "Delete"] }
            ]
        }
    ];

    private static List<FieldPermission> GetFieldPermissions() =>
    [
        // Product: only SystemAdmin can edit pricing
        new()
        {
            EntityKey = "Product",
            FieldKey = "UnitPrice",
            RolePermissions =
            [
                new() { RoleName = "Customer", AccessLevel = FieldAccessLevel.ReadOnly },
                new() { RoleName = "BackofficeUser", AccessLevel = FieldAccessLevel.ReadOnly },
                new() { RoleName = "SystemAdmin", AccessLevel = FieldAccessLevel.Editable }
            ]
        },
        // Product: BackofficeUser+ can toggle availability
        new()
        {
            EntityKey = "Product",
            FieldKey = "IsAvailable",
            RolePermissions =
            [
                new() { RoleName = "Customer", AccessLevel = FieldAccessLevel.ReadOnly },
                new() { RoleName = "BackofficeUser", AccessLevel = FieldAccessLevel.Editable },
                new() { RoleName = "SystemAdmin", AccessLevel = FieldAccessLevel.Editable }
            ]
        },
        // Customer: email is read-only for the customer themselves
        new()
        {
            EntityKey = "Customer",
            FieldKey = "Email",
            RolePermissions =
            [
                new() { RoleName = "Customer", AccessLevel = FieldAccessLevel.ReadOnly },
                new() { RoleName = "BackofficeUser", AccessLevel = FieldAccessLevel.Editable }
            ]
        },
        // Customer: IsActive only editable by BackofficeUserAdmin+
        new()
        {
            EntityKey = "Customer",
            FieldKey = "IsActive",
            RolePermissions =
            [
                new() { RoleName = "Customer", AccessLevel = FieldAccessLevel.Hidden },
                new() { RoleName = "BackofficeUser", AccessLevel = FieldAccessLevel.ReadOnly },
                new() { RoleName = "BackofficeUserAdmin", AccessLevel = FieldAccessLevel.Editable }
            ]
        },
        // Order: Status field is read-only for customers
        new()
        {
            EntityKey = "Order",
            FieldKey = "Status",
            RolePermissions =
            [
                new() { RoleName = "Customer", AccessLevel = FieldAccessLevel.ReadOnly },
                new() { RoleName = "BackofficeUser", AccessLevel = FieldAccessLevel.Editable }
            ]
        }
    ];

    private static List<EndpointPermission> GetEndpointPermissions() =>
    [
        // Products
        new() { EndpointKey = "GetProducts", AllowedRoles = ["Customer"] },
        new() { EndpointKey = "GetProductById", AllowedRoles = ["Customer"] },
        new() { EndpointKey = "CreateProduct", AllowedRoles = ["SystemAdmin"] },
        new() { EndpointKey = "UpdateProduct", AllowedRoles = ["BackofficeUser"] },
        new() { EndpointKey = "ToggleProductAvailability", AllowedRoles = ["BackofficeUser"] },
        // Customers
        new() { EndpointKey = "GetCustomers", AllowedRoles = ["BackofficeUser"] },
        new() { EndpointKey = "GetCustomerById", AllowedRoles = ["Customer"] },
        new() { EndpointKey = "CreateCustomer", AllowedRoles = ["BackofficeUserAdmin"] },
        new() { EndpointKey = "UpdateCustomer", AllowedRoles = ["Customer"] },
        new() { EndpointKey = "SearchCustomers", AllowedRoles = ["BackofficeUser"] },
        // Orders
        new() { EndpointKey = "GetOrders", AllowedRoles = ["Customer"] },
        new() { EndpointKey = "GetOrderById", AllowedRoles = ["Customer"] },
        new() { EndpointKey = "GetOrdersByCustomer", AllowedRoles = ["Customer"] },
        new() { EndpointKey = "CreateOrder", AllowedRoles = ["Customer"] },
        new() { EndpointKey = "UpdateOrderStatus", AllowedRoles = ["BackofficeUser"] },
        new() { EndpointKey = "AddOrderItem", AllowedRoles = ["Customer"] },
        new() { EndpointKey = "RemoveOrderItem", AllowedRoles = ["Customer"] },
        // Order Notes
        new() { EndpointKey = "GetOrderNotes", AllowedRoles = ["Customer"] },
        new() { EndpointKey = "CreateOrderNote", AllowedRoles = ["Customer"] },
        new() { EndpointKey = "UpdateOrderNote", AllowedRoles = ["BackofficeUser"] },
        new() { EndpointKey = "DeleteOrderNote", AllowedRoles = ["BackofficeUser"] }
    ];

    private static List<ControlPermission> GetControlPermissions() =>
    [
        new()
        {
            PageKey = "/products",
            ControlKey = "CreateProductButton",
            RolePermissions =
            [
                new() { RoleName = "Customer", State = ControlState.Hidden },
                new() { RoleName = "BackofficeUser", State = ControlState.Hidden },
                new() { RoleName = "SystemAdmin", State = ControlState.Enabled }
            ]
        },
        new()
        {
            PageKey = "/products",
            ControlKey = "EditPriceButton",
            RolePermissions =
            [
                new() { RoleName = "Customer", State = ControlState.Hidden },
                new() { RoleName = "BackofficeUser", State = ControlState.Disabled },
                new() { RoleName = "SystemAdmin", State = ControlState.Enabled }
            ]
        },
        new()
        {
            PageKey = "/customers",
            ControlKey = "DeleteCustomerButton",
            RolePermissions =
            [
                new() { RoleName = "BackofficeUser", State = ControlState.Hidden },
                new() { RoleName = "BackofficeUserAdmin", State = ControlState.Hidden },
                new() { RoleName = "SystemAdmin", State = ControlState.Enabled }
            ]
        },
        new()
        {
            PageKey = "/orders",
            ControlKey = "DeleteOrderButton",
            RolePermissions =
            [
                new() { RoleName = "Customer", State = ControlState.Hidden },
                new() { RoleName = "BackofficeUser", State = ControlState.Hidden },
                new() { RoleName = "SystemAdmin", State = ControlState.Enabled }
            ]
        },
        new()
        {
            PageKey = "/orders",
            ControlKey = "UpdateStatusButton",
            RolePermissions =
            [
                new() { RoleName = "Customer", State = ControlState.Hidden },
                new() { RoleName = "BackofficeUser", State = ControlState.Enabled }
            ]
        }
    ];

    private static List<RowFilter> GetRowFilters() =>
    [
        new()
        {
            EntityKey = "Order",
            RoleName = "Customer",
            FilterExpression = "CustomerId == @CurrentUserId",
            Description = "Customers can only see their own orders"
        },
        new()
        {
            EntityKey = "OrderItem",
            RoleName = "Customer",
            FilterExpression = "Order.CustomerId == @CurrentUserId",
            Description = "Customers can only see items on their own orders"
        }
    ];
}
