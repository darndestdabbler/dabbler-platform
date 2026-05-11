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
    private readonly IDbContextFactory<DabblerSecurityDbContext> _dbFactory;

    /// <summary>Initializes a new instance of the <see cref="AdminUserQueryService"/> class.</summary>
    public AdminUserQueryService(
        IHttpContextAccessor httpContextAccessor,
        IRoleSwitcher roleSwitcher,
        IBakedInRoleProvider bakedInRoleProvider,
        IUserRoleStore userRoleStore,
        IDbContextFactory<DabblerSecurityDbContext> dbFactory)
    {
        _httpContextAccessor = httpContextAccessor;
        _roleSwitcher = roleSwitcher;
        _bakedInRoleProvider = bakedInRoleProvider;
        _userRoleStore = userRoleStore;
        _dbFactory = dbFactory;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AdminUserRow>> GetUsersAsync(CancellationToken ct = default)
        => GetUsersAsync(_httpContextAccessor.HttpContext?.User, ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminUserRow>> GetUsersAsync(ClaimsPrincipal actor, CancellationToken ct = default)
    {
        // Use a single fresh context for the entire call. Earlier revisions
        // reused a scoped DbContext, which crashed the Users tab with
        // "A second operation was started on this context instance" when
        // sibling Blazor components (UsersMasterGrid + UserRoleAssignmentDetails)
        // kicked off concurrent queries during their initial render.
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var actorActiveRole = GetActorActiveAdminRole(actor);
        var scopeOrganizationIds = await LoadActorAssignedOrganizationIdsAsync(db, actor, ct);
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

        var query = db.AppUsers
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
    public Task<IReadOnlySet<int>> GetActorAssignedOrganizationIdsAsync(CancellationToken ct = default)
        => GetActorAssignedOrganizationIdsAsync(_httpContextAccessor.HttpContext?.User, ct);

    /// <inheritdoc />
    public async Task<IReadOnlySet<int>> GetActorAssignedOrganizationIdsAsync(ClaimsPrincipal actor, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await LoadActorAssignedOrganizationIdsAsync(db, actor, ct);
    }

    private async Task<IReadOnlySet<int>> LoadActorAssignedOrganizationIdsAsync(
        DabblerSecurityDbContext db,
        ClaimsPrincipal? actor,
        CancellationToken ct)
    {
        var userId = ResolveActorUserIdInt(actor);
        if (userId is null)
        {
            return new HashSet<int>();
        }

        var ids = await db.AppUserOrganizationMemberships
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
        => GetActorActiveAdminRole(_httpContextAccessor.HttpContext?.User);

    /// <inheritdoc />
    public string? GetActorActiveAdminRole(ClaimsPrincipal? actor)
    {
        if (actor is null || actor.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        // Returns the highest-privilege baked-in admin-class role the caller
        // holds. "Highest" is defined as: a role that can assign users to roles
        // (SystemAdmin / CrossOrgUserAdmin / UserAdmin) wins over a role that
        // can only see all organizations (CrossOrgMonitor). Within those two
        // groups the iteration order of EffectiveRoles wins. This matters
        // because a SystemAdmin user whose principal also carries a
        // CrossOrgMonitor claim must be classed as SystemAdmin for the
        // assignment authority's granterRoleName check to pass — picking
        // CrossOrgMonitor would make the authority deny every assignment with
        // "The CrossOrgMonitor role cannot assign roles to users", which is
        // the 2026-05-11 admin-users-cross-links UAT-3 finding.
        //
        // Pure user-type roles and unknown role names are skipped so the
        // caller is not falsely classed as an admin actor.
        string? fallbackSeeAllRole = null;
        foreach (var roleName in _roleSwitcher.EffectiveRoles(actor))
        {
            var bakedIn = _bakedInRoleProvider.TryGetBakedInRole(roleName);
            if (bakedIn is null)
            {
                continue;
            }

            if (bakedIn.Privileges.CanAssignUsersToRoles)
            {
                return bakedIn.Name;
            }

            // Hold the first see-all role as a fallback in case no
            // assignment-capable role surfaces. CrossOrgMonitor is the only
            // baked-in role that hits this branch today.
            if (fallbackSeeAllRole is null && bakedIn.Privileges.CanSeeAllOrganizations)
            {
                fallbackSeeAllRole = bakedIn.Name;
            }
        }

        return fallbackSeeAllRole;
    }

    /// <inheritdoc />
    public string GetActorUserId()
        => GetActorUserId(_httpContextAccessor.HttpContext?.User);

    /// <inheritdoc />
    public string GetActorUserId(ClaimsPrincipal? actor)
    {
        if (actor is null || actor.Identity?.IsAuthenticated != true)
        {
            return string.Empty;
        }

        return actor.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminUserOrganization>> GetGrantableOrganizationsAsync(
        ClaimsPrincipal actor,
        CancellationToken ct = default)
    {
        var actorActiveRole = GetActorActiveAdminRole(actor);

        // CrossOrgMonitor and unauthenticated callers cannot create users.
        var bakedIn = string.IsNullOrEmpty(actorActiveRole)
            ? null
            : _bakedInRoleProvider.TryGetBakedInRole(actorActiveRole);
        if (bakedIn is null || !bakedIn.Privileges.CanAssignUsersToRoles)
        {
            return [];
        }

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var query = db.AppOrganizations
            .AsNoTracking()
            .Where(o => o.IsActive);

        if (!bakedIn.Privileges.CanSeeAllOrganizations)
        {
            var scopeIds = await LoadActorAssignedOrganizationIdsAsync(db, actor, ct);
            if (scopeIds.Count == 0)
            {
                return [];
            }
            query = query.Where(o => scopeIds.Contains(o.Id));
        }

        return await query
            .OrderBy(o => o.Name)
            .Select(o => new AdminUserOrganization(o.Id, o.Key, o.Name, false, o.IsInternal))
            .ToListAsync(ct);
    }

    private int? ResolveActorUserIdInt(ClaimsPrincipal? actor)
    {
        var raw = GetActorUserId(actor);
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
