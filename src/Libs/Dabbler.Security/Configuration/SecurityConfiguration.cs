namespace Dabbler.Security.Configuration;

/// <summary>
/// The root aggregate for runtime authorization configuration. Held in-memory by
/// SecurityConfigurationManager and persisted by an ISecurityConfigurationStore.
/// </summary>
public sealed class SecurityConfiguration
{
    /// <summary>
    /// A collection of all defined roles, including their names, descriptions, and optional inheritance relationships.
    /// </summary>
    public List<RoleDefinition> Roles { get; set; } = [];
    
    /// <summary>
    /// A collection of page-level access permissions, specifying which roles can access specific UI pages.
    /// </summary>
    public List<PagePermission> PagePermissions { get; set; } = [];
    
    /// <summary>
    /// A collection of entity-level permissions, specifying CRUD operations allowed for roles on specific entities.
    /// </summary>
    public List<EntityPermission> EntityPermissions { get; set; } = [];
    
    /// <summary>
    /// A collection of field-level permissions, specifying access levels (Editable, ReadOnly, Hidden) for roles on specific entity fields.
    /// </summary>
    public List<FieldPermission> FieldPermissions { get; set; } = [];
    
    /// <summary>
    /// A collection of endpoint-level permissions, specifying which roles can access specific API endpoints.
    /// </summary>
    public List<EndpointPermission> EndpointPermissions { get; set; } = [];
    
    /// <summary>
    /// A collection of UI control-level permissions, specifying visibility or interactivity for roles on specific UI controls.
    /// </summary>
    public List<ControlPermission> ControlPermissions { get; set; } = [];
    
    /// <summary>
    /// A collection of row filters, defining data access restrictions based on business rules for specific entities and roles.
    /// </summary>
    public List<RowFilter> RowFilters { get; set; } = [];
    
    /// <summary>
    /// Configuration for role switching capabilities, allowing administrators or privileged users to assume other roles.
    /// </summary>
    public RoleSwitchingPermission RoleSwitching { get; set; } = new();
}
