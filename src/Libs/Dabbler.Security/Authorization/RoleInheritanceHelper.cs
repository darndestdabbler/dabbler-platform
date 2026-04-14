using Dabbler.Security.Configuration;

namespace Dabbler.Security.Authorization;

/// <summary>
/// Expands a user's direct roles to include all transitively inherited roles,
/// based on the <see cref="RoleDefinition.InheritsFrom"/> chain in the security configuration.
/// </summary>
internal static class RoleInheritanceHelper
{
    /// <summary>
    /// Returns the set of all effective role names for a user, including every role
    /// inherited (directly or transitively) from the user's direct roles.
    /// </summary>
    /// <param name="userRoles">The collection of roles directly assigned to the user.</param>
    /// <param name="allRoles">The complete collection of all role definitions in the system, used to build the inheritance graph.</param>
    /// <returns>
    /// A read-only set of strings containing all effective roles for the user (direct and inherited).
    /// Cycles in the inheritance chain are handled gracefully.
    /// </returns>
    public static IReadOnlySet<string> ExpandRoles(
        IEnumerable<string> userRoles,
        IEnumerable<RoleDefinition> allRoles)
    {
        var roleMap = allRoles
            .Where(r => r.InheritsFrom != null)
            .ToDictionary(r => r.RoleName, r => r.InheritsFrom!, StringComparer.OrdinalIgnoreCase);

        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var role in userRoles)
        {
            Visit(role, roleMap, result);
        }

        return result;
    }

    private static void Visit(string role, Dictionary<string, string> inheritanceMap, HashSet<string> visited)
    {
        if (!visited.Add(role))
            return;

        if (inheritanceMap.TryGetValue(role, out var parent))
        {
            Visit(parent, inheritanceMap, visited);
        }
    }
}
