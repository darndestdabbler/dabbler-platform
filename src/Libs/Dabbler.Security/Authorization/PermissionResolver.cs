using Dabbler.Security.Configuration;

namespace Dabbler.Security.Authorization;

/// <summary>
/// Walks the <see cref="RoleDefinition.InheritsFrom"/> chain to determine whether a
/// permission entry for a given role is explicit (set on that role itself) or
/// inherited (set on an ancestor role) — used by admin UI surfaces to render
/// explicit vs. inherited cells differently.
/// </summary>
public static class PermissionResolver
{
    /// <summary>
    /// Result of walking the inheritance chain for a single permission cell.
    /// </summary>
    /// <param name="Found">True if the chain yielded an entry (either on the role or an ancestor).</param>
    /// <param name="InheritedFromRole">
    /// Null when the entry was set directly on the requested role (explicit), or
    /// when no entry exists in the chain. Otherwise the name of the nearest
    /// ancestor role that supplied the entry.
    /// </param>
    public readonly record struct RoleChainResolution(bool Found, string? InheritedFromRole);

    /// <summary>
    /// Walks the inheritance chain starting at <paramref name="roleName"/> and returns
    /// the nearest role (the requested role itself, or a transitive parent) for which
    /// <paramref name="hasValue"/> returns true. If the requested role itself has a
    /// value, the result is "explicit" (<c>InheritedFromRole</c> is null).
    /// </summary>
    /// <param name="roleName">The role the admin UI is rendering.</param>
    /// <param name="hasValue">Predicate that returns true if the role name passed to it
    /// has an explicit entry for the cell under consideration (a permission, an op, etc.).</param>
    /// <param name="allRoles">The full role catalog, used to build the inheritance graph.</param>
    public static RoleChainResolution ResolveRoleChain(
        string roleName,
        Func<string, bool> hasValue,
        IReadOnlyList<RoleDefinition> allRoles)
    {
        if (hasValue(roleName))
        {
            return new RoleChainResolution(true, null);
        }

        var roleMap = allRoles.ToDictionary(r => r.RoleName, r => r, StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { roleName };

        var current = roleMap.TryGetValue(roleName, out var role) ? role.InheritsFrom : null;
        while (!string.IsNullOrEmpty(current) && visited.Add(current))
        {
            if (hasValue(current))
            {
                return new RoleChainResolution(true, current);
            }

            current = roleMap.TryGetValue(current, out var parent) ? parent.InheritsFrom : null;
        }

        return new RoleChainResolution(false, null);
    }
}
