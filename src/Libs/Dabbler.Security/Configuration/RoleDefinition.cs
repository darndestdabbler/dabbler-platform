namespace Dabbler.Security.Configuration;

/// <summary>
/// Defines a security role within the Dabbler application. Roles are fundamental
/// units for assigning permissions across various application resources.
/// </summary>
public sealed class RoleDefinition
{
    /// <summary>
    /// The canonical, immutable name of the role. This name is used in claims,
    /// audit records, and handler logic and must never change once deployed.
    /// Per-deployment rebranding goes through <see cref="DisplayAlias"/>.
    /// </summary>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// Optional per-deployment display label shown in the dashboard, role
    /// switcher, and audit UI. When null, consumers fall back to
    /// <see cref="RoleName"/>. Baked-in roles use
    /// <c>BakedInRoleDefinition.DefaultDisplayAlias</c> as the default and
    /// honour any override in <c>SecurityConfiguration.BakedInRoleAliasOverrides</c>.
    /// </summary>
    public string? DisplayAlias { get; set; }

    /// <summary>
    /// A human-readable description of the role's purpose or typical users.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Optional. When set, this role inherits all permissions of the named parent role.
    /// Inheritance is transitive, meaning a role can inherit from a role that also inherits.
    /// </summary>
    /// <remarks>
    /// Role inheritance is removed from the authorization model (see
    /// <c>docs/session-sets/rearchitect-role-administration-foundations/spec.md</c>).
    /// This field remains on the type only for JSON round-trip compatibility
    /// during the transition and is ignored at runtime; it will be deleted
    /// in Session 2 of the Foundations set.
    /// </remarks>
    public string? InheritsFrom { get; set; }
}
