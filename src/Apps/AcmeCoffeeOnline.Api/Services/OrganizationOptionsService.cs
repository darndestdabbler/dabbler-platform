using AcmeCoffeeOnline.Shared.DTOs;
using AcmeCoffeeOnline.Shared.Enums;
using Dabbler.Security.Data;
using Dabbler.Security.Services;
using Microsoft.EntityFrameworkCore;

namespace AcmeCoffeeOnline.Api.Services;

public interface IOrganizationOptionsService
{
    Task<IReadOnlyList<OrganizationOptionDto>> GetOptionsAsync(CancellationToken cancellationToken = default);
}

public sealed class OrganizationOptionsService : IOrganizationOptionsService
{
    private readonly ICurrentUserService _currentUser;
    private readonly IUserSessionContextService _userSessionContextService;
    private readonly DabblerSecurityDbContext _securityDb;

    public OrganizationOptionsService(
        ICurrentUserService currentUser,
        IUserSessionContextService userSessionContextService,
        DabblerSecurityDbContext securityDb)
    {
        _currentUser = currentUser;
        _userSessionContextService = userSessionContextService;
        _securityDb = securityDb;
    }

    public async Task<IReadOnlyList<OrganizationOptionDto>> GetOptionsAsync(CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(_currentUser.UserId, out var userId))
        {
            throw new UnauthorizedAccessException("Authenticated user identifier is missing.");
        }

        var sessionContext = await _userSessionContextService.BuildAsync(
            userId,
            _currentUser.ActiveOrganizationId,
            cancellationToken);

        if (sessionContext is null)
        {
            return [];
        }

        if (_currentUser.Role == UserRole.SystemAdmin)
        {
            var availableOrganizationRoles = sessionContext.OrganizationMemberships
                .ToDictionary(
                    organization => organization.OrganizationId,
                    organization => organization.RoleNames);

            var organizations = await _securityDb.AppOrganizations
                .AsNoTracking()
                .Where(organization => organization.IsActive)
                .OrderBy(organization => organization.Name)
                .Select(organization => new
                {
                    organization.Id,
                    organization.Key,
                    organization.Name
                })
                .ToListAsync(cancellationToken);

            return organizations
                .Select(organization => new OrganizationOptionDto
                {
                    OrganizationId = organization.Id,
                    OrganizationKey = organization.Key,
                    OrganizationName = organization.Name,
                    RoleNames = availableOrganizationRoles.TryGetValue(organization.Id, out var roleNames)
                        ? roleNames.ToArray()
                        : [],
                    IsActive = sessionContext.ActiveOrganizationId == organization.Id
                })
                .ToArray();
        }

        return sessionContext.OrganizationMemberships
            .Select(organization => new OrganizationOptionDto
            {
                OrganizationId = organization.OrganizationId,
                OrganizationKey = organization.OrganizationKey,
                OrganizationName = organization.OrganizationName,
                RoleNames = organization.RoleNames.ToArray(),
                IsActive = sessionContext.ActiveOrganizationId == organization.OrganizationId
            })
            .ToArray();
    }
}
