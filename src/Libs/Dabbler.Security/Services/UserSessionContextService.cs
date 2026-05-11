using System.Security.Claims;
using Dabbler.Security.Data;
using Dabbler.Security.Entities;
using Dabbler.Security.Options;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Dabbler.Security.Services;

/// <summary>
/// Centralizes the claim names and forwarded-header names used for active-organization session state.
/// </summary>
public static class UserSessionContextConstants
{
    /// <summary>
    /// The claim type that stores the active organization identifier.
    /// </summary>
    public const string ActiveOrganizationIdClaimType = "dabbler:active-org-id";

    /// <summary>
    /// The claim type that stores the active organization key.
    /// </summary>
    public const string ActiveOrganizationKeyClaimType = "dabbler:active-org-key";

    /// <summary>
    /// The claim type that stores the active organization display name.
    /// </summary>
    public const string ActiveOrganizationNameClaimType = "dabbler:active-org-name";

    /// <summary>
    /// The claim type that stores the active organization membership identifier when the user has one.
    /// </summary>
    public const string ActiveOrganizationMembershipIdClaimType = "dabbler:active-org-membership-id";

    /// <summary>
    /// The header used by trusted internal app-to-api calls to forward the active organization identifier.
    /// </summary>
    public const string ActiveOrganizationIdHeaderName = "X-Active-Organization-Id";
}

/// <summary>
/// Describes one organization that a user can operate within during the current session.
/// </summary>
/// <param name="OrganizationId">The organization identifier.</param>
/// <param name="OrganizationKey">The stable organization key.</param>
/// <param name="OrganizationName">The organization display name.</param>
/// <param name="OrganizationMembershipId">The membership identifier when the user belongs to the organization.</param>
/// <param name="IsDefault">Whether this is the user's default organization.</param>
/// <param name="IsInternal">Whether the organization is the configured internal organization.</param>
/// <param name="RoleNames">The user's direct role assignments for this organization.</param>
public sealed record UserSessionOrganization(
    int OrganizationId,
    string OrganizationKey,
    string OrganizationName,
    int? OrganizationMembershipId,
    bool IsDefault,
    bool IsInternal,
    IReadOnlyList<string> RoleNames);

/// <summary>
/// Captures the active-organization session context that sign-in and internal API forwarding use.
/// </summary>
/// <param name="UserId">The application user identifier.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="DisplayName">The user's display name.</param>
/// <param name="EffectiveRoles">The roles that should be active for the current organization context.</param>
/// <param name="OrganizationMemberships">The organizations available to the user.</param>
/// <param name="ActiveOrganizationId">The active organization identifier, if any.</param>
/// <param name="ActiveOrganizationMembershipId">The active organization membership identifier, if any.</param>
/// <param name="ActiveOrganizationKey">The active organization key, if any.</param>
/// <param name="ActiveOrganizationName">The active organization display name, if any.</param>
/// <param name="ActiveCustomerId">The bridged customer identifier for the active organization, if any.</param>
public sealed record UserSessionContext(
    int UserId,
    string Email,
    string DisplayName,
    IReadOnlyList<string> EffectiveRoles,
    IReadOnlyList<UserSessionOrganization> OrganizationMemberships,
    int? ActiveOrganizationId,
    int? ActiveOrganizationMembershipId,
    string? ActiveOrganizationKey,
    string? ActiveOrganizationName,
    int? ActiveCustomerId);

/// <summary>
/// Builds active-organization session state and claims for authenticated users.
/// </summary>
public interface IUserSessionContextService
{
    /// <summary>
    /// Builds the session context for a user.
    /// </summary>
    /// <param name="userId">The application user identifier.</param>
    /// <param name="requestedOrganizationId">An optional requested organization override.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The built session context, or <c>null</c> when the user cannot be loaded.</returns>
    Task<UserSessionContext?> BuildAsync(int userId, int? requestedOrganizationId = null, CancellationToken ct = default);

    /// <summary>
    /// Creates an authenticated principal whose role claims reflect the active-organization context.
    /// </summary>
    /// <param name="userId">The application user identifier.</param>
    /// <param name="authenticationScheme">The authentication scheme to stamp on the identity.</param>
    /// <param name="requestedOrganizationId">An optional requested organization override.</param>
    /// <param name="overrideRoles">Optional role claims to stamp instead of the context-derived effective roles.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The authenticated principal, or <c>null</c> when the user cannot be loaded.</returns>
    Task<ClaimsPrincipal?> CreatePrincipalAsync(
        int userId,
        string authenticationScheme = CookieAuthenticationDefaults.AuthenticationScheme,
        int? requestedOrganizationId = null,
        IReadOnlyList<string>? overrideRoles = null,
        CancellationToken ct = default);
}

/// <summary>
/// Resolves the active organization, effective roles, and bridge claims that should be stamped onto a user session.
/// </summary>
public sealed class UserSessionContextService : IUserSessionContextService
{
    private const string SystemAdminRoleName = "SystemAdmin";

    private readonly DabblerSecurityDbContext _db;
    private readonly DabblerSecurityOptions _options;
    private readonly IUserRoleStore? _userRoleStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserSessionContextService"/> class.
    /// </summary>
    /// <param name="db">The security DbContext.</param>
    /// <param name="optionsAccessor">The security options.</param>
    /// <param name="userRoleStore">
    /// Optional string-keyed role store. When registered (via <c>UseJsonFileStore</c> or
    /// <c>UseEfCoreStore</c>), its assignments are merged into the effective roles so that
    /// admin UI edits through <see cref="IUserRoleStore"/> are reflected in the claims
    /// built for the next authenticated request.
    /// </param>
    public UserSessionContextService(
        DabblerSecurityDbContext db,
        IOptions<DabblerSecurityOptions> optionsAccessor,
        IUserRoleStore? userRoleStore = null)
    {
        _db = db;
        _options = optionsAccessor.Value;
        _userRoleStore = userRoleStore;
    }

    /// <inheritdoc />
    public async Task<UserSessionContext?> BuildAsync(int userId, int? requestedOrganizationId = null, CancellationToken ct = default)
    {
        var user = await _db.AppUsers
            .AsNoTracking()
            .Include(appUser => appUser.Roles)
                .ThenInclude(userRole => userRole.Role)
            .Include(appUser => appUser.Roles)
                .ThenInclude(userRole => userRole.OrganizationMembership)
                    .ThenInclude(membership => membership!.Organization)
            .Include(appUser => appUser.OrganizationMemberships)
                .ThenInclude(membership => membership.Organization)
            .FirstOrDefaultAsync(appUser => appUser.Id == userId && appUser.DeletedAt == null && appUser.IsActive, ct);

        if (user is null)
        {
            return null;
        }

        var memberships = user.OrganizationMemberships
            .Where(membership => membership.DeletedAt == null
                && membership.IsActive
                && membership.Organization is { IsActive: true })
            .OrderByDescending(membership => membership.IsDefault)
            .ThenBy(membership => membership.Organization!.Name)
            .ToList();

        var globalRoles = user.Roles
            .Where(userRole => userRole.DeletedAt == null
                && userRole.OrganizationMembershipId == null
                && userRole.Role is not null
                && userRole.Role.DeletedAt == null)
            .Select(userRole => userRole.Role!.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Also load assignments from the string-keyed role store (populated by the admin
        // UI via IUserRoleStore). These are treated as global assignments for claim purposes
        // and are included in isSystemAdmin so a user elevated through the admin UI can
        // actually exercise SystemAdmin behavior (including cross-organization access).
        var storeRoles = _userRoleStore is null
            ? (IReadOnlyList<string>)[]
            : await _userRoleStore.GetRolesAsync(user.Email, ct);

        var availableOrganizations = memberships
            .Select(membership => new UserSessionOrganization(
                membership.OrganizationId,
                membership.Organization!.Key,
                membership.Organization.Name,
                membership.Id,
                membership.IsDefault,
                membership.Organization.IsInternal,
                user.Roles
                    .Where(userRole => userRole.DeletedAt == null
                        && userRole.OrganizationMembershipId == membership.Id
                        && userRole.Role is not null
                        && userRole.Role.DeletedAt == null)
                    .Select(userRole => userRole.Role!.Name)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(roleName => roleName)
                    .ToArray()))
            .ToList();

        var isSystemAdmin = globalRoles.Contains(SystemAdminRoleName, StringComparer.OrdinalIgnoreCase)
            || storeRoles.Contains(SystemAdminRoleName, StringComparer.OrdinalIgnoreCase);
        var activeOrganization = await ResolveActiveOrganizationAsync(
            requestedOrganizationId,
            memberships,
            isSystemAdmin,
            ct);

        var effectiveRoles = new HashSet<string>(globalRoles, StringComparer.OrdinalIgnoreCase);
        if (activeOrganization?.OrganizationMembershipId is int activeMembershipId)
        {
            foreach (var roleName in user.Roles
                         .Where(userRole => userRole.DeletedAt == null
                             && userRole.OrganizationMembershipId == activeMembershipId
                             && userRole.Role is not null
                             && userRole.Role.DeletedAt == null)
                         .Select(userRole => userRole.Role!.Name))
            {
                effectiveRoles.Add(roleName);
            }
        }

        foreach (var roleName in storeRoles)
        {
            effectiveRoles.Add(roleName);
        }

        if (memberships.Count == 0 && effectiveRoles.Count == 0)
        {
            foreach (var roleName in user.Roles
                         .Where(userRole => userRole.DeletedAt == null
                             && userRole.Role is not null
                             && userRole.Role.DeletedAt == null)
                         .Select(userRole => userRole.Role!.Name))
            {
                effectiveRoles.Add(roleName);
            }
        }

        var activeCustomerId = TryResolveCustomerId(activeOrganization?.OrganizationKey);

        return new UserSessionContext(
            user.Id,
            user.Email,
            user.DisplayName,
            effectiveRoles.OrderBy(roleName => roleName).ToArray(),
            availableOrganizations,
            activeOrganization?.OrganizationId,
            activeOrganization?.OrganizationMembershipId,
            activeOrganization?.OrganizationKey,
            activeOrganization?.OrganizationName,
            activeCustomerId);
    }

    /// <inheritdoc />
    public async Task<ClaimsPrincipal?> CreatePrincipalAsync(
        int userId,
        string authenticationScheme = CookieAuthenticationDefaults.AuthenticationScheme,
        int? requestedOrganizationId = null,
        IReadOnlyList<string>? overrideRoles = null,
        CancellationToken ct = default)
    {
        var sessionContext = await BuildAsync(userId, requestedOrganizationId, ct);
        if (sessionContext is null)
        {
            return null;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, sessionContext.UserId.ToString()),
            new(ClaimTypes.Email, sessionContext.Email),
            new(ClaimTypes.Name, sessionContext.DisplayName)
        };

        foreach (var roleName in (overrideRoles ?? sessionContext.EffectiveRoles).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            claims.Add(new Claim(ClaimTypes.Role, roleName));
        }

        if (sessionContext.ActiveOrganizationId.HasValue)
        {
            claims.Add(new Claim(
                UserSessionContextConstants.ActiveOrganizationIdClaimType,
                sessionContext.ActiveOrganizationId.Value.ToString()));
        }

        if (sessionContext.ActiveOrganizationMembershipId.HasValue)
        {
            claims.Add(new Claim(
                UserSessionContextConstants.ActiveOrganizationMembershipIdClaimType,
                sessionContext.ActiveOrganizationMembershipId.Value.ToString()));
        }

        if (!string.IsNullOrWhiteSpace(sessionContext.ActiveOrganizationKey))
        {
            claims.Add(new Claim(
                UserSessionContextConstants.ActiveOrganizationKeyClaimType,
                sessionContext.ActiveOrganizationKey));
        }

        if (!string.IsNullOrWhiteSpace(sessionContext.ActiveOrganizationName))
        {
            claims.Add(new Claim(
                UserSessionContextConstants.ActiveOrganizationNameClaimType,
                sessionContext.ActiveOrganizationName));
        }

        if (sessionContext.ActiveCustomerId.HasValue)
        {
            claims.Add(new Claim("CustomerId", sessionContext.ActiveCustomerId.Value.ToString()));
        }

        var identity = new ClaimsIdentity(claims, authenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    private async Task<UserSessionOrganization?> ResolveActiveOrganizationAsync(
        int? requestedOrganizationId,
        IReadOnlyList<AppUserOrganizationMembership> memberships,
        bool isSystemAdmin,
        CancellationToken ct)
    {
        if (requestedOrganizationId.HasValue)
        {
            var matchingMembership = memberships.FirstOrDefault(
                membership => membership.OrganizationId == requestedOrganizationId.Value);
            if (matchingMembership is not null)
            {
                return ToUserSessionOrganization(matchingMembership);
            }

            if (isSystemAdmin)
            {
                var requestedOrganization = await _db.AppOrganizations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        organization => organization.Id == requestedOrganizationId.Value
                            && organization.IsActive,
                        ct);

                if (requestedOrganization is not null)
                {
                    return new UserSessionOrganization(
                        requestedOrganization.Id,
                        requestedOrganization.Key,
                        requestedOrganization.Name,
                        null,
                        false,
                        requestedOrganization.IsInternal,
                        []);
                }
            }
        }

        if (memberships.Count == 0)
        {
            return null;
        }

        var selectedMembership = memberships.Count == 1
            ? memberships[0]
            : memberships.FirstOrDefault(membership => membership.IsDefault)
                ?? memberships.FirstOrDefault(membership =>
                    string.Equals(
                        membership.Organization?.Key,
                        _options.DefaultOrganizationKey,
                        StringComparison.OrdinalIgnoreCase))
                ?? memberships[0];

        return ToUserSessionOrganization(selectedMembership);
    }

    private static UserSessionOrganization ToUserSessionOrganization(AppUserOrganizationMembership membership)
    {
        return new UserSessionOrganization(
            membership.OrganizationId,
            membership.Organization!.Key,
            membership.Organization.Name,
            membership.Id,
            membership.IsDefault,
            membership.Organization.IsInternal,
            []);
    }

    private static int? TryResolveCustomerId(string? organizationKey)
    {
        const string customerPrefix = "customer-";

        if (string.IsNullOrWhiteSpace(organizationKey)
            || !organizationKey.StartsWith(customerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return int.TryParse(organizationKey[customerPrefix.Length..], out var customerId)
            ? customerId
            : null;
    }
}
