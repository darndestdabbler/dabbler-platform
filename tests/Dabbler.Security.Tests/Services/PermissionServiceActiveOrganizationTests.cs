using Dabbler.Security.Data;
using Dabbler.Security.Entities;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Dabbler.Security.Tests.Services;

public sealed class PermissionServiceActiveOrganizationTests : IDisposable
{
    private readonly DabblerSecurityDbContext _db;
    private readonly DefaultHttpContext _httpContext;
    private readonly PermissionService _service;

    public PermissionServiceActiveOrganizationTests()
    {
        var options = new DbContextOptionsBuilder<DabblerSecurityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new DabblerSecurityDbContext(options);
        _httpContext = new DefaultHttpContext();
        _service = new PermissionService(
            _db,
            new HttpContextAccessor
            {
                HttpContext = _httpContext
            });
    }

    [Fact]
    public async Task HasPermissionAsync_UsesOnlyRolesForTheActiveOrganization()
    {
        var retailWest = new AppOrganization { Id = 1, Key = "customer-11", Name = "Retail West", IsActive = true, CreatedAt = DateTime.UtcNow };
        var retailEast = new AppOrganization { Id = 2, Key = "customer-12", Name = "Retail East", IsActive = true, CreatedAt = DateTime.UtcNow };
        var user = new AppUser { Id = 40, Email = "user@test.local", DisplayName = "Scoped User", IsActive = true, CreatedAt = DateTime.UtcNow };
        var westMembership = new AppUserOrganizationMembership { Id = 101, UserId = user.Id, OrganizationId = retailWest.Id, IsDefault = true, IsActive = true, JoinedAt = DateTime.UtcNow };
        var eastMembership = new AppUserOrganizationMembership { Id = 102, UserId = user.Id, OrganizationId = retailEast.Id, IsDefault = false, IsActive = true, JoinedAt = DateTime.UtcNow };
        var viewerRole = new AppRole { Id = 5, Name = "Viewer", CreatedAt = DateTime.UtcNow };
        var managerRole = new AppRole { Id = 6, Name = "Manager", CreatedAt = DateTime.UtcNow };

        _db.AppOrganizations.AddRange(retailWest, retailEast);
        _db.AppUsers.Add(user);
        _db.AppUserOrganizationMemberships.AddRange(westMembership, eastMembership);
        _db.AppRoles.AddRange(viewerRole, managerRole);
        _db.AppRolePermissions.AddRange(
            new AppRolePermission { RoleId = viewerRole.Id, EntityName = "Widget", PermissionType = "Read" },
            new AppRolePermission { RoleId = managerRole.Id, EntityName = "Widget", PermissionType = "Delete" });
        _db.AppUserRoles.AddRange(
            new AppUserRole
            {
                UserId = user.Id,
                RoleId = viewerRole.Id,
                OrganizationMembershipId = westMembership.Id,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = user.Id
            },
            new AppUserRole
            {
                UserId = user.Id,
                RoleId = managerRole.Id,
                OrganizationMembershipId = eastMembership.Id,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = user.Id
            });
        await _db.SaveChangesAsync();

        _httpContext.Items["ActiveOrganizationId"] = retailWest.Id;
        var canReadInWest = await _service.HasPermissionAsync(user.Id, "Widget", "Read");
        var canDeleteInWest = await _service.HasPermissionAsync(user.Id, "Widget", "Delete");

        _httpContext.Items["ActiveOrganizationId"] = retailEast.Id;
        var canDeleteInEast = await _service.HasPermissionAsync(user.Id, "Widget", "Delete");

        canReadInWest.Should().BeTrue();
        canDeleteInWest.Should().BeFalse();
        canDeleteInEast.Should().BeTrue();
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}
