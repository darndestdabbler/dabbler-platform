using AcmeCoffeeOnline.Shared.DTOs;
using AcmeCoffeeOnline.Shared.Enums;
using Dabbler.Security.Data;
using Dabbler.Security.Entities;
using Microsoft.EntityFrameworkCore;

namespace AcmeCoffeeOnline.Api.Services;

public interface IAdminUserScopeService
{
    Task<AdminUserScopeDto> GetScopeAsync(CancellationToken cancellationToken = default);

    Task<AdminUserScopeDto> GrantScopedUserAdminAsync(int organizationMembershipId, CancellationToken cancellationToken = default);

    Task<AdminUserScopeDto> RevokeScopedUserAdminAsync(int organizationMembershipId, CancellationToken cancellationToken = default);
}

public sealed class AdminUserScopeService : IAdminUserScopeService
{
    private const string ScopedAdminRoleName = "BackofficeUserAdmin";

    private readonly DabblerSecurityDbContext _securityDb;
    private readonly ICurrentUserService _currentUser;

    public AdminUserScopeService(
        DabblerSecurityDbContext securityDb,
        ICurrentUserService currentUser)
    {
        _securityDb = securityDb;
        _currentUser = currentUser;
    }

    public Task<AdminUserScopeDto> GetScopeAsync(CancellationToken cancellationToken = default)
        => BuildScopeAsync(cancellationToken);

    public async Task<AdminUserScopeDto> GrantScopedUserAdminAsync(int organizationMembershipId, CancellationToken cancellationToken = default)
    {
        var actorContext = await ResolveActorContextAsync(cancellationToken);
        var membership = await LoadMembershipAsync(organizationMembershipId, cancellationToken);

        EnsureMembershipCanBeManaged(actorContext, membership.OrganizationId);

        var scopedAdminRole = await _securityDb.AppRoles
            .SingleAsync(role => role.Name == ScopedAdminRoleName && role.DeletedAt == null, cancellationToken);

        var activeAssignment = await _securityDb.AppUserRoles
            .FirstOrDefaultAsync(
                assignment => assignment.OrganizationMembershipId == organizationMembershipId
                    && assignment.RoleId == scopedAdminRole.Id
                    && assignment.DeletedAt == null,
                cancellationToken);

        if (activeAssignment is null)
        {
            var deletedAssignment = await _securityDb.AppUserRoles
                .Where(assignment => assignment.OrganizationMembershipId == organizationMembershipId
                    && assignment.RoleId == scopedAdminRole.Id
                    && assignment.DeletedAt != null)
                .OrderByDescending(assignment => assignment.AssignedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (deletedAssignment is not null)
            {
                deletedAssignment.DeletedAt = null;
                deletedAssignment.DeletedBy = null;
                deletedAssignment.AssignedAt = DateTime.UtcNow;
                deletedAssignment.AssignedBy = actorContext.ActorUserId;
            }
            else
            {
                _securityDb.AppUserRoles.Add(new AppUserRole
                {
                    UserId = membership.UserId,
                    RoleId = scopedAdminRole.Id,
                    OrganizationMembershipId = membership.Id,
                    AssignedAt = DateTime.UtcNow,
                    AssignedBy = actorContext.ActorUserId
                });
            }

            await _securityDb.SaveChangesAsync(cancellationToken);
        }

        return await BuildScopeAsync(cancellationToken);
    }

    public async Task<AdminUserScopeDto> RevokeScopedUserAdminAsync(int organizationMembershipId, CancellationToken cancellationToken = default)
    {
        var actorContext = await ResolveActorContextAsync(cancellationToken);
        var membership = await LoadMembershipAsync(organizationMembershipId, cancellationToken);

        EnsureMembershipCanBeManaged(actorContext, membership.OrganizationId);

        var scopedAdminRole = await _securityDb.AppRoles
            .SingleAsync(role => role.Name == ScopedAdminRoleName && role.DeletedAt == null, cancellationToken);

        var activeAssignment = await _securityDb.AppUserRoles
            .FirstOrDefaultAsync(
                assignment => assignment.OrganizationMembershipId == organizationMembershipId
                    && assignment.RoleId == scopedAdminRole.Id
                    && assignment.DeletedAt == null,
                cancellationToken);

        if (activeAssignment is not null)
        {
            var activeAdminCount = await _securityDb.AppUserRoles
                .CountAsync(
                    assignment => assignment.OrganizationMembershipId != null
                        && assignment.OrganizationMembership!.OrganizationId == membership.OrganizationId
                        && assignment.RoleId == scopedAdminRole.Id
                        && assignment.DeletedAt == null,
                    cancellationToken);

            if (activeAdminCount <= 1)
            {
                throw new InvalidOperationException("Cannot remove the last organization-scoped user admin.");
            }

            activeAssignment.DeletedAt = DateTime.UtcNow;
            activeAssignment.DeletedBy = actorContext.ActorUserId;
            await _securityDb.SaveChangesAsync(cancellationToken);
        }

        return await BuildScopeAsync(cancellationToken);
    }

    private async Task<AdminUserScopeDto> BuildScopeAsync(CancellationToken cancellationToken)
    {
        var actorContext = await ResolveActorContextAsync(cancellationToken);

        if (!actorContext.ActiveOrganizationId.HasValue)
        {
            return new AdminUserScopeDto
            {
                IsSystemAdmin = actorContext.IsSystemAdmin,
                CanManageUsers = false
            };
        }

        var organization = await _securityDb.AppOrganizations
            .AsNoTracking()
            .FirstAsync(
                candidate => candidate.Id == actorContext.ActiveOrganizationId.Value && candidate.IsActive,
                cancellationToken);

        var memberships = await _securityDb.AppUserOrganizationMemberships
            .Where(membership => membership.OrganizationId == organization.Id
                && membership.DeletedAt == null
                && membership.IsActive
                && membership.User != null
                && membership.User.DeletedAt == null)
            .Include(membership => membership.User)
                .ThenInclude(user => user!.Roles)
                    .ThenInclude(userRole => userRole.Role)
            .Include(membership => membership.User)
                .ThenInclude(user => user!.OrganizationMemberships)
                    .ThenInclude(otherMembership => otherMembership.Organization)
            .OrderBy(membership => membership.User!.DisplayName)
            .ToListAsync(cancellationToken);

        var users = memberships.Select(membership =>
        {
            var scopedRoles = membership.User!.Roles
                .Where(userRole => userRole.OrganizationMembershipId == membership.Id
                    && userRole.DeletedAt == null
                    && userRole.Role is not null
                    && userRole.Role.DeletedAt == null)
                .Select(userRole => userRole.Role!.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(roleName => roleName)
                .ToArray();

            var otherOrganizations = membership.User.OrganizationMemberships
                .Where(otherMembership => otherMembership.Id != membership.Id
                    && otherMembership.DeletedAt == null
                    && otherMembership.IsActive
                    && otherMembership.Organization is { IsActive: true })
                .Select(otherMembership => $"{otherMembership.Organization!.Name} ({otherMembership.Organization.Key})")
                .OrderBy(name => name)
                .ToArray();

            var hasScopedUserAdmin = scopedRoles.Contains(ScopedAdminRoleName, StringComparer.OrdinalIgnoreCase);

            return new AdminUserScopeUserDto
            {
                UserId = membership.UserId,
                OrganizationMembershipId = membership.Id,
                DisplayName = membership.User.DisplayName,
                Email = membership.User.Email,
                IsActive = membership.User.IsActive,
                ScopedRoles = scopedRoles,
                OtherOrganizations = otherOrganizations,
                HasScopedUserAdmin = hasScopedUserAdmin,
                CanGrantUserAdmin = actorContext.CanManageUsers && !hasScopedUserAdmin,
                CanRevokeUserAdmin = actorContext.CanManageUsers
                    && hasScopedUserAdmin
                    && membership.UserId != actorContext.ActorUserId
            };
        }).ToArray();

        return new AdminUserScopeDto
        {
            IsSystemAdmin = actorContext.IsSystemAdmin,
            CanManageUsers = actorContext.CanManageUsers,
            ActiveOrganizationId = organization.Id,
            ActiveOrganizationKey = organization.Key,
            ActiveOrganizationName = organization.Name,
            Users = users
        };
    }

    private async Task<AppUserOrganizationMembership> LoadMembershipAsync(int organizationMembershipId, CancellationToken cancellationToken)
    {
        return await _securityDb.AppUserOrganizationMemberships
            .Include(membership => membership.Organization)
            .Include(membership => membership.User)
            .FirstAsync(
                membership => membership.Id == organizationMembershipId
                    && membership.DeletedAt == null
                    && membership.IsActive
                    && membership.Organization != null
                    && membership.Organization.IsActive
                    && membership.User != null
                    && membership.User.DeletedAt == null,
                cancellationToken);
    }

    private ActorContext EnsureMembershipCanBeManaged(ActorContext actorContext, int organizationId)
    {
        if (actorContext.IsSystemAdmin)
        {
            return actorContext;
        }

        if (!actorContext.CanManageUsers || actorContext.ActiveOrganizationId != organizationId)
        {
            throw new UnauthorizedAccessException("The current user cannot manage this organization scope.");
        }

        return actorContext;
    }

    private Task<ActorContext> ResolveActorContextAsync(CancellationToken cancellationToken)
    {
        if (!int.TryParse(_currentUser.UserId, out var actorUserId))
        {
            throw new UnauthorizedAccessException("Authenticated user identifier is missing.");
        }

        _ = cancellationToken;

        var isSystemAdmin = _currentUser.Role == UserRole.SystemAdmin;
        var canManageUsers = isSystemAdmin || _currentUser.Role == UserRole.BackofficeUserAdmin;

        return Task.FromResult(new ActorContext(
            actorUserId,
            _currentUser.ActiveOrganizationId,
            isSystemAdmin,
            canManageUsers));
    }

    private sealed record ActorContext(
        int ActorUserId,
        int? ActiveOrganizationId,
        bool IsSystemAdmin,
        bool CanManageUsers);
}
