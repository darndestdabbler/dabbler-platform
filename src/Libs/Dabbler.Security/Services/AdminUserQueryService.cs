using System.Security.Claims;
using Dabbler.Security.Authorization;
using Dabbler.Security.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Dabbler.Security.Services;

/// <summary>
/// Default <see cref="IAdminUserQueryService"/> implementation backed by
/// <see cref="DabblerSecurityDbContext"/>. Reads the acting user from
/// <see cref="IHttpContextAccessor"/> and applies the UserAdmin organization
/// scope at query time.
/// </summary>
public sealed class AdminUserQueryService : IAdminUserQueryService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IRoleSwitcher _roleSwitcher;
    private readonly IBakedInRoleProvider _bakedInRoleProvider;
    private readonly IUserRoleStore _userRoleStore;
    private readonly DabblerSecurityDbContext _db;

    /// <summary>Initializes a new instance of the <see cref="AdminUserQueryService"/> class.</summary>
    public AdminUserQueryService(
        IHttpContextAccessor httpContextAccessor,
        IRoleSwitcher roleSwitcher,
        IBakedInRoleProvider bakedInRoleProvider,
        IUserRoleStore userRoleStore,
        DabblerSecurityDbContext db)
    {
        _httpContextAccessor = httpContextAccessor;
        _roleSwitcher = roleSwitcher;
        _bakedInRoleProvider = bakedInRoleProvider;
        _userRoleStore = userRoleStore;
        _db = db;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminUserRow>> GetUsersAsync(CancellationToken ct = default)
    {
        var actorActiveRole = GetActorActiveAdminRole();
        var scopeOrganizationIds = await GetActorAssignedOrganizationIdsAsync(ct);
        var actorSeesAllOrganizations = ActorSeesAllOrganizations(actorActiveRole);

        // Fail closed: a caller without an active baked-in admin role gets an
        // empty list even if they happen to have org memberships. Outer UI
        // gating (DabblerSecurityDashboard.ResolveVisibleTabs) already
        // prevents such callers from reaching this tab, but the service
        // itself must enforce the contract so it cannot be misused.
        if (!actorSeesAllOrganizations && string.IsNullOrEmpty(actorActiveRole))
        {
            return [];
        }

        var query = _db.AppUsers
            .AsNoTracking()
            .Where(user => user.DeletedAt == null);

        if (!actorSeesAllOrganizations)
        {
            if (scopeOrganizationIds.Count == 0)
            {
                return [];
            }

            query = query.Where(user => user.OrganizationMemberships.Any(membership =>
                membership.DeletedAt == null
                && membership.IsActive
                && scopeOrganizationIds.Contains(membership.OrganizationId)));
        }

        var projected = await query
            .OrderBy(user => user.DisplayName)
            .Select(user => new
            {
                user.Id,
                user.DisplayName,
                user.Email,
                user.IsActive,
                // Restrict the projected memberships to the actor's scope when
                // the actor cannot see all organizations. Otherwise UserAdmin
                // would learn org names/keys for orgs they do not administer.
                Organizations = user.OrganizationMemberships
                    .Where(membership => membership.DeletedAt == null
                        && membership.IsActive
                        && membership.Organization != null
                        && membership.Organization.IsActive
                        && (actorSeesAllOrganizations
                            || scopeOrganizationIds.Contains(membership.OrganizationId)))
                    .OrderByDescending(membership => membership.IsDefault)
                    .ThenBy(membership => membership.Organization!.Name)
                    .Select(membership => new AdminUserOrganization(
                        membership.OrganizationId,
                        membership.Organization!.Key,
                        membership.Organization.Name,
                        membership.IsDefault,
                        membership.Organization.IsInternal))
                    .ToList()
            })
            .ToListAsync(ct);

        var assignments = await _userRoleStore.GetAllAsync(ct);

        var rows = new List<AdminUserRow>(projected.Count);
        foreach (var user in projected)
        {
            var userId = user.Id.ToString();
            var roles = assignments.TryGetValue(userId, out var stored)
                ? stored.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray()
                : [];

            rows.Add(new AdminUserRow(
                userId,
                user.DisplayName,
                user.Email,
                user.IsActive,
                user.Organizations,
                roles));
        }

        return rows;
    }

    /// <inheritdoc />
    public async Task<IReadOnlySet<int>> GetActorAssignedOrganizationIdsAsync(CancellationToken ct = default)
    {
        var userId = ResolveActorUserIdInt();
        if (userId is null)
        {
            return new HashSet<int>();
        }

        var ids = await _db.AppUserOrganizationMemberships
            .AsNoTracking()
            .Where(membership => membership.UserId == userId.Value
                && membership.DeletedAt == null
                && membership.IsActive
                && membership.Organization != null
                && membership.Organization.IsActive)
            .Select(membership => membership.OrganizationId)
            .ToListAsync(ct);

        return ids.ToHashSet();
    }

    /// <inheritdoc />
    public string? GetActorActiveAdminRole()
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal is null || principal.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        // Returns the first baked-in admin-class role the caller holds. Both
        // "can assign users to roles" (SystemAdmin / CrossOrgUserAdmin /
        // UserAdmin) and "can see all organizations" (CrossOrgMonitor adds
        // here, plus the previous three) qualify. Pure user-type roles and
        // unknown role names are skipped so the caller is not falsely classed
        // as an admin actor.
        foreach (var roleName in _roleSwitcher.EffectiveRoles(principal))
        {
            var bakedIn = _bakedInRoleProvider.TryGetBakedInRole(roleName);
            if (bakedIn is null)
            {
                continue;
            }

            if (bakedIn.Privileges.CanAssignUsersToRoles
                || bakedIn.Privileges.CanSeeAllOrganizations
                || string.Equals(bakedIn.Name, BakedInRoles.SystemAdmin, StringComparison.OrdinalIgnoreCase))
            {
                return bakedIn.Name;
            }
        }

        return null;
    }

    /// <inheritdoc />
    public string GetActorUserId()
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal is null || principal.Identity?.IsAuthenticated != true)
        {
            return string.Empty;
        }

        return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }

    private int? ResolveActorUserIdInt()
    {
        var raw = GetActorUserId();
        return int.TryParse(raw, out var value) ? value : null;
    }

    private bool ActorSeesAllOrganizations(string? activeRoleName)
    {
        if (string.IsNullOrEmpty(activeRoleName))
        {
            return false;
        }

        var bakedIn = _bakedInRoleProvider.TryGetBakedInRole(activeRoleName);
        return bakedIn?.Privileges.CanSeeAllOrganizations == true;
    }
}
