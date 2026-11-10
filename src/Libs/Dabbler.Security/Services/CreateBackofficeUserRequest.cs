namespace Dabbler.Security.Services;

/// <summary>
/// Input model for <see cref="IBackofficeUserCreationService.CreateAsync"/>.
/// Captures the fields an admin must supply when provisioning a new backoffice user.
/// </summary>
/// <param name="DisplayName">The new user's display name shown in the dashboard.</param>
/// <param name="Email">The new user's email address. Must be unique across AppUsers.</param>
/// <param name="InitialRoleName">
/// Canonical name of the role to assign at creation time. Must be a role the granting admin
/// is authorized to assign per <see cref="RoleAssignmentAuthority.Evaluate"/>.
/// </param>
/// <param name="InitialOrganizationId">
/// Organization the user will be a member of. Must exist and be active. For a UserAdmin granter,
/// this org must be within the granter's assigned-org scope.
/// </param>
public sealed record CreateBackofficeUserRequest(
    string DisplayName,
    string Email,
    string InitialRoleName,
    int InitialOrganizationId);
