using Dabbler.Security.Authorization;

namespace Dabbler.Security.Services;

/// <summary>
/// Temporary seam for the global organization-scope policy. Session Set B does
/// not apply EF Core query filters yet because the transactional models do not
/// gain <c>OrganizationId</c> until Session Set C. This helper only centralizes
/// the cross-organization bypass rule that Set C will reuse when the real
/// global filter lands.
/// </summary>
public sealed class OrganizationScopeFilter
{
    /// <summary>
    /// Explains why the transactional EF filter is still deferred.
    /// </summary>
    public const string DeferredReason =
        "Deferred to Session Set C: transactional entities do not expose OrganizationId yet.";

    /// <summary>
    /// Returns whether any effective role carries the compiled privilege to see
    /// records across all organizations once the global filter is introduced.
    /// </summary>
    /// <param name="effectiveRoleNames">The current effective role names.</param>
    /// <param name="bakedInRoleProvider">The baked-in role provider.</param>
    public bool CanBypassOrganizationScope(
        IEnumerable<string> effectiveRoleNames,
        IBakedInRoleProvider bakedInRoleProvider)
    {
        ArgumentNullException.ThrowIfNull(effectiveRoleNames);
        ArgumentNullException.ThrowIfNull(bakedInRoleProvider);

        foreach (var roleName in effectiveRoleNames)
        {
            var bakedInRole = bakedInRoleProvider.TryGetBakedInRole(roleName);
            if (bakedInRole?.Privileges.CanSeeAllOrganizations == true)
            {
                return true;
            }
        }

        return false;
    }
}
