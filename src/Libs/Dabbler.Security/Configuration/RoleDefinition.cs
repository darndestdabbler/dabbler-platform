namespace Dabbler.Security.Configuration;

/// <summary>
/// Defines a security role within the Dabbler application. Roles are fundamental
/// units for assigning permissions across various application resources.
/// </summary>
public sealed class RoleDefinition
{
    /// <summary>
    /// The unique name of the role. This name is used throughout the security configuration
    /// to assign permissions.
    /// </summary>
    public string RoleName { get; set; } = string.Empty;
    
    /// <summary>
    /// A human-readable description of the role's purpose or typical users.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Optional. When set, this role inherits all permissions of the named parent role.
    /// Inheritance is transitive, meaning a role can inherit from a role that also inherits.
    /// </summary>
    public string? InheritsFrom { get; set; }
}
