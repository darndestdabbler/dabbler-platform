using Dabbler.Security.Abstractions;
using Dabbler.Security.Configuration;

namespace AcmeCoffeeOnline.Api.Data;

/// <summary>
/// Provides the default runtime <see cref="SecurityConfiguration"/> for the sample
/// distributor application under the restriction-based authorization model
/// introduced by the <c>rearchitect-role-administration-foundations</c> session set.
/// </summary>
/// <remarks>
/// The two seeded user-type roles (<c>BackofficeUser</c> and <c>Customer</c>) match
/// the migrated on-disk <c>security-config.json</c>; the file is the source of truth
/// for runtime behaviour, and this seeder only fires for environments that have not
/// yet been configured.
/// </remarks>
public static class SecuritySeedData
{
    /// <summary>
    /// Returns the default <see cref="SecurityConfiguration"/> with the restriction-based
    /// user-type roles, organization-switching defaults, and (currently empty) default
    /// masks and mask exemptions.
    /// </summary>
    public static SecurityConfiguration GetDefaultConfiguration()
    {
        return new SecurityConfiguration
        {
            UserTypeRoles = GetUserTypeRoles(),
            OrganizationSwitching = new OrganizationSwitching { AllowedRoles = [] },
            DefaultMasks = [],
            MaskExemptions = [],
            BakedInRoleAliasOverrides = []
        };
    }

    /// <summary>
    /// Seeds the security configuration store if it has not yet been configured under
    /// the restriction-based model.
    /// </summary>
    public static async Task SeedAsync(IServiceProvider services)
    {
        var store = services.GetRequiredService<ISecurityConfigurationStore>();
        var existing = await store.LoadAsync();

        if (existing.UserTypeRoles.Count > 0)
            return;

        var config = GetDefaultConfiguration();
        await store.SaveAsync(config);
    }

    private static List<RoleDefinition> GetUserTypeRoles() =>
    [
        new()
        {
            RoleName = "BackofficeUser",
            DisplayAlias = "Backoffice User",
            Description = "Internal staff with read/update access to all retail-business accounts and orders.",
            ColumnRestrictions =
            [
                new() { EntityKey = "Product", ColumnKey = "UnitPrice", RestrictionKind = ColumnRestrictionKind.ViewOnly },
                new() { EntityKey = "Customer", ColumnKey = "IsActive", RestrictionKind = ColumnRestrictionKind.ViewOnly }
            ],
            UiRestrictions =
            [
                new() { PageKey = "/products", ControlKey = "*", Kind = UiRestrictionKind.Hidden },
                new() { PageKey = "/admin/products", ControlKey = "*", Kind = UiRestrictionKind.Hidden },
                new() { PageKey = "/admin/security", ControlKey = "*", Kind = UiRestrictionKind.Hidden },
                new() { PageKey = "/customers", ControlKey = "DeleteCustomerButton", Kind = UiRestrictionKind.Hidden }
            ]
        },
        new()
        {
            RoleName = "Customer",
            DisplayAlias = "Customer",
            Description = "Retail-business account with self-service access to its own orders and profile.",
            ColumnRestrictions =
            [
                new() { EntityKey = "Product", ColumnKey = "UnitPrice", RestrictionKind = ColumnRestrictionKind.ViewOnly },
                new() { EntityKey = "Customer", ColumnKey = "Email", RestrictionKind = ColumnRestrictionKind.ViewOnly },
                new() { EntityKey = "Customer", ColumnKey = "IsActive", RestrictionKind = ColumnRestrictionKind.Hidden },
                new() { EntityKey = "Order", ColumnKey = "Status", RestrictionKind = ColumnRestrictionKind.ViewOnly }
            ],
            RowRestrictions =
            [
                new() { EntityKey = "Order", FilterExpression = "CustomerId == @CurrentUserCustomerId" }
            ],
            UiRestrictions =
            [
                new() { PageKey = "/customers", ControlKey = "*", Kind = UiRestrictionKind.Hidden },
                new() { PageKey = "/admin/users", ControlKey = "*", Kind = UiRestrictionKind.Hidden },
                new() { PageKey = "/admin/products", ControlKey = "*", Kind = UiRestrictionKind.Hidden },
                new() { PageKey = "/admin/security", ControlKey = "*", Kind = UiRestrictionKind.Hidden },
                new() { PageKey = "/products", ControlKey = "CreateProductButton", Kind = UiRestrictionKind.Hidden },
                new() { PageKey = "/products", ControlKey = "EditPriceButton", Kind = UiRestrictionKind.Hidden },
                new() { PageKey = "/orders", ControlKey = "DeleteOrderButton", Kind = UiRestrictionKind.Hidden },
                new() { PageKey = "/orders", ControlKey = "UpdateStatusButton", Kind = UiRestrictionKind.Hidden }
            ]
        }
    ];
}
