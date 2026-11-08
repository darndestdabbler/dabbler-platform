namespace Dabbler.Security.Configuration;

/// <summary>
/// Defines access permissions for a specific application page.
/// </summary>
/// <remarks>
/// Legacy grant-based type. Replaced by <see cref="UiRestriction"/> on each
/// user-type role plus compiled baked-in role privileges. Scheduled for
/// removal in Session 3 of <c>rearchitect-role-administration-foundations</c>
/// alongside the handler rewrite.
/// </remarks>
[Obsolete("Replaced by UiRestriction on each user-type RoleDefinition. Scheduled for removal in Session 3 of rearchitect-role-administration-foundations.")]
public sealed class PagePermission
{
    /// <summary>The page key as registered via options.RegisterPages().</summary>
    public string PageKey { get; set; } = string.Empty;

    /// <summary>Roles explicitly allowed to access this page.</summary>
    public List<string> AllowedRoles { get; set; } = [];

    /// <summary>
    /// Roles explicitly denied access. Deny takes precedence over allow when a user
    /// holds both an allowed and a denied role.
    /// </summary>
    public List<string> DeniedRoles { get; set; } = [];
}
