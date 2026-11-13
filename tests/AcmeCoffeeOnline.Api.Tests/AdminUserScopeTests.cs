using System.Net;
using System.Security.Claims;
using AcmeCoffeeOnline.Api.Data;
using AcmeCoffeeOnline.Shared.DTOs;
using AcmeCoffeeOnline.Shared.Models;
using AcmeCoffeeOnline.Api.Services;
using Dabbler.Security.Data;
using Dabbler.Security.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AcmeCoffeeOnline.Api.Tests;

public sealed class AdminUserScopeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string GuardrailSystemAdminEmail = "api.guardrail.system.admin@acme.test";
    private const string GuardrailRegionalAdminEmail = "api.guardrail.regional.admin@acme.test";
    private const string GuardrailDualUserEmail = "api.guardrail.dual.user@acme.test";
    private const string GuardrailAllowedTargetEmail = "api.guardrail.allowed.customer@acme.test";
    private const string GuardrailDisallowedTargetEmail = "api.guardrail.disallowed.customer@acme.test";

    private readonly WebApplicationFactory<Program> _factory;

    public AdminUserScopeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RegionalSalesRep_ScopeAndElevationStayInsideActiveOrganizationAsync()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        var ids = await ResolveFixtureIdsAsync(factory.Services);

        await using var testScope = factory.Services.CreateAsyncScope();
        SetHttpContext(
            testScope.ServiceProvider,
            ids.RegionalSalesRepUserId,
            ids.AllowedRetailOrganizationId,
            ids.AllowedRetailOrganizationKey,
            "BackofficeUserAdmin");
        var adminUserScopeService = testScope.ServiceProvider.GetRequiredService<IAdminUserScopeService>();

        var scope = await adminUserScopeService.GetScopeAsync();

        Assert.NotNull(scope);
        Assert.True(scope.CanManageUsers);
        Assert.False(scope.IsSystemAdmin);
        Assert.Equal(ids.AllowedRetailOrganizationKey, scope.ActiveOrganizationKey);
        Assert.Contains(scope.Users, user => user.OrganizationMembershipId == ids.AllowedRetailTargetMembershipId);
        Assert.DoesNotContain(scope.Users, user => user.OrganizationMembershipId == ids.DisallowedRetailTargetMembershipId);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            adminUserScopeService.GrantScopedUserAdminAsync(ids.DisallowedRetailTargetMembershipId));
    }

    [Fact]
    public async Task SystemAdmin_CanGrantScopedUserAdmin_AndCustomerRowsFollowActiveOrganizationAsync()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        var ids = await ResolveFixtureIdsAsync(factory.Services);
        await ResetScopedAdminAssignmentAsync(factory.Services, ids.DisallowedRetailTargetMembershipId);

        await using var testScope = factory.Services.CreateAsyncScope();
        SetHttpContext(
            testScope.ServiceProvider,
            ids.SystemAdminUserId,
            ids.DisallowedRetailOrganizationId,
            ids.DisallowedRetailOrganizationKey,
            "SystemAdmin");
        var adminUserScopeService = testScope.ServiceProvider.GetRequiredService<IAdminUserScopeService>();
        var customerService = testScope.ServiceProvider.GetRequiredService<ICustomerService>();

        var scope = await adminUserScopeService.GrantScopedUserAdminAsync(ids.DisallowedRetailTargetMembershipId);

        Assert.NotNull(scope);
        Assert.True(scope.IsSystemAdmin);
        Assert.Equal(ids.DisallowedRetailOrganizationKey, scope.ActiveOrganizationKey);
        Assert.Contains(
            scope.Users,
            user => user.OrganizationMembershipId == ids.DisallowedRetailTargetMembershipId && user.HasScopedUserAdmin);

        await using (var verifyScope = factory.Services.CreateAsyncScope())
        {
            var securityDb = verifyScope.ServiceProvider.GetRequiredService<DabblerSecurityDbContext>();
            var adminRoleId = await securityDb.AppRoles
                .Where(role => role.Name == "BackofficeUserAdmin")
                .Select(role => role.Id)
                .SingleAsync();

            Assert.True(await securityDb.AppUserRoles.AnyAsync(
                assignment => assignment.OrganizationMembershipId == ids.DisallowedRetailTargetMembershipId
                    && assignment.RoleId == adminRoleId
                    && assignment.DeletedAt == null));
        }

        SetHttpContext(
            testScope.ServiceProvider,
            ids.DualMembershipUserId,
            ids.InternalOrganizationId,
            "internal",
            "BackofficeUser");
        var internalScopeCustomers = await customerService.GetAllAsync(new PagedRequest { Page = 1, PageSize = 10 });

        Assert.NotNull(internalScopeCustomers);
        // Session 4 of security-dashboard-uat-remediation (Finding 3): internal
        // user-type roles (BackofficeUser here) with no CustomerId claim used
        // to return 0 customers because CustomerService.ApplyOrganizationScope
        // silently hit `query.Where(_ => false)`. That starved the /customers
        // page of every row for CrossOrgUserAdmin and BackofficeUser alike.
        // OrganizationScopeFilter.DeferredReason documents that true per-org
        // scoping is a Set C deliverable (transactional entities don't carry
        // OrganizationId yet), so until Set C, internal user-type roles fall
        // through to the full set. The positive assertion here replaces the
        // prior 0-row assertion without masking the true deferral.
        Assert.True(internalScopeCustomers.TotalCount > 0,
            "BackofficeUser must see all customers until Set C adds per-org scoping — the prior 0-row assertion encoded the CrossOrgUserAdmin/BackofficeUser bug that Finding 3 fixed.");

        SetHttpContext(
            testScope.ServiceProvider,
            ids.DualMembershipUserId,
            ids.DualMembershipRetailOrganizationId,
            ids.DualMembershipRetailOrganizationKey,
            "Customer");
        var customerScopeCustomers = await customerService.GetAllAsync(new PagedRequest { Page = 1, PageSize = 10 });

        Assert.NotNull(customerScopeCustomers);
        Assert.Equal(1, customerScopeCustomers.TotalCount);
        Assert.Single(customerScopeCustomers.Items);
        Assert.Equal(ids.DualMembershipRetailCustomerId, customerScopeCustomers.Items[0].Id);
    }

    [Fact]
    public async Task RegionalUserAdmin_OrganizationOptionsStayInsideActiveMembershipsAsync()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        var ids = await ResolveFixtureIdsAsync(factory.Services);

        await using var testScope = factory.Services.CreateAsyncScope();
        SetHttpContext(
            testScope.ServiceProvider,
            ids.RegionalSalesRepUserId,
            ids.AllowedRetailOrganizationId,
            ids.AllowedRetailOrganizationKey,
            "BackofficeUserAdmin");
        var organizationOptionsService = testScope.ServiceProvider.GetRequiredService<IOrganizationOptionsService>();

        var options = await organizationOptionsService.GetOptionsAsync();

        var scopedOption = Assert.Single(options);
        Assert.Equal(ids.AllowedRetailOrganizationId, scopedOption.OrganizationId);
        Assert.True(scopedOption.IsActive);
    }

    [Fact]
    public async Task SystemAdmin_OrganizationOptionsIncludeAllActiveOrganizationsAsync()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        var ids = await ResolveFixtureIdsAsync(factory.Services);

        await using var testScope = factory.Services.CreateAsyncScope();
        SetHttpContext(
            testScope.ServiceProvider,
            ids.SystemAdminUserId,
            ids.DisallowedRetailOrganizationId,
            ids.DisallowedRetailOrganizationKey,
            "SystemAdmin");
        var organizationOptionsService = testScope.ServiceProvider.GetRequiredService<IOrganizationOptionsService>();

        var options = await organizationOptionsService.GetOptionsAsync();

        Assert.Contains(options, option => option.OrganizationId == ids.InternalOrganizationId);
        Assert.Contains(options, option => option.OrganizationId == ids.AllowedRetailOrganizationId);
        Assert.Contains(options, option => option.OrganizationId == ids.DisallowedRetailOrganizationId && option.IsActive);
    }

    [Fact]
    public async Task DualMembershipCustomer_OrganizationOptionsStayLimitedToMembershipsAsync()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        var ids = await ResolveFixtureIdsAsync(factory.Services);

        await using var testScope = factory.Services.CreateAsyncScope();
        SetHttpContext(
            testScope.ServiceProvider,
            ids.DualMembershipUserId,
            ids.DualMembershipRetailOrganizationId,
            ids.DualMembershipRetailOrganizationKey,
            "Customer");
        var organizationOptionsService = testScope.ServiceProvider.GetRequiredService<IOrganizationOptionsService>();

        var options = await organizationOptionsService.GetOptionsAsync();

        Assert.Equal(2, options.Count);
        Assert.Contains(options, option => option.OrganizationId == ids.InternalOrganizationId);
        Assert.Contains(options, option => option.OrganizationId == ids.DualMembershipRetailOrganizationId && option.IsActive);
        Assert.DoesNotContain(options, option => option.OrganizationId == ids.DisallowedRetailOrganizationId);
    }

    [Fact]
    public async Task OrganizationOptions_WithInvalidUserId_ThrowsUnauthorizedAccessAsync()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        var ids = await ResolveFixtureIdsAsync(factory.Services);

        await using var testScope = factory.Services.CreateAsyncScope();
        SetHttpContext(
            testScope.ServiceProvider,
            "not-an-integer",
            ids.AllowedRetailOrganizationId,
            ids.AllowedRetailOrganizationKey,
            "BackofficeUserAdmin");
        var organizationOptionsService = testScope.ServiceProvider.GetRequiredService<IOrganizationOptionsService>();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => organizationOptionsService.GetOptionsAsync());
    }

    [Fact]
    public async Task RegionalUserAdmin_CanRevokeScopedUserAdminInsideActiveOrganizationAsync()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        var ids = await ResolveFixtureIdsAsync(factory.Services);
        await ResetScopedAdminAssignmentAsync(factory.Services, ids.AllowedRetailTargetMembershipId);

        await using var grantScope = factory.Services.CreateAsyncScope();
        SetHttpContext(
            grantScope.ServiceProvider,
            ids.RegionalSalesRepUserId,
            ids.AllowedRetailOrganizationId,
            ids.AllowedRetailOrganizationKey,
            "BackofficeUserAdmin");
        var adminUserScopeService = grantScope.ServiceProvider.GetRequiredService<IAdminUserScopeService>();

        var grantedScope = await adminUserScopeService.GrantScopedUserAdminAsync(ids.AllowedRetailTargetMembershipId);

        Assert.Contains(
            grantedScope.Users,
            user => user.OrganizationMembershipId == ids.AllowedRetailTargetMembershipId && user.HasScopedUserAdmin);

        var revokedScope = await adminUserScopeService.RevokeScopedUserAdminAsync(ids.AllowedRetailTargetMembershipId);

        Assert.Contains(
            revokedScope.Users,
            user => user.OrganizationMembershipId == ids.AllowedRetailTargetMembershipId && !user.HasScopedUserAdmin);

        await using var verifyScope = factory.Services.CreateAsyncScope();
        var securityDb = verifyScope.ServiceProvider.GetRequiredService<DabblerSecurityDbContext>();
        var adminRoleId = await securityDb.AppRoles
            .Where(role => role.Name == "BackofficeUserAdmin")
            .Select(role => role.Id)
            .SingleAsync();

        Assert.False(await securityDb.AppUserRoles.AnyAsync(
            assignment => assignment.OrganizationMembershipId == ids.AllowedRetailTargetMembershipId
                && assignment.RoleId == adminRoleId
                && assignment.DeletedAt == null));
    }

    [Fact]
    public async Task RegionalUserAdmin_CannotRevokeScopedUserAdminOutsideActiveOrganizationAsync()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        var ids = await ResolveFixtureIdsAsync(factory.Services);

        await using var testScope = factory.Services.CreateAsyncScope();
        SetHttpContext(
            testScope.ServiceProvider,
            ids.RegionalSalesRepUserId,
            ids.AllowedRetailOrganizationId,
            ids.AllowedRetailOrganizationKey,
            "BackofficeUserAdmin");
        var adminUserScopeService = testScope.ServiceProvider.GetRequiredService<IAdminUserScopeService>();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            adminUserScopeService.RevokeScopedUserAdminAsync(ids.DisallowedRetailTargetMembershipId));
    }

    private static async Task<FixtureIds> ResolveFixtureIdsAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var appDb = scope.ServiceProvider.GetRequiredService<AcmeCoffeeDbContext>();
        var securityDb = scope.ServiceProvider.GetRequiredService<DabblerSecurityDbContext>();

        var customers = await appDb.Customers
            .AsNoTracking()
            .OrderBy(customer => customer.Id)
            .Take(2)
            .ToListAsync();

        var allowedCustomer = customers[0];
        var disallowedCustomer = customers[1];

        var internalOrganization = await EnsureOrganizationAsync(
            securityDb,
            "internal",
            "Internal Operations",
            isInternal: true);
        var allowedOrganization = await EnsureOrganizationAsync(
            securityDb,
            $"customer-{allowedCustomer.Id}",
            $"Guardrail Retail {allowedCustomer.Id:000}",
            isInternal: false);
        var disallowedOrganization = await EnsureOrganizationAsync(
            securityDb,
            $"customer-{disallowedCustomer.Id}",
            $"Guardrail Retail {disallowedCustomer.Id:000}",
            isInternal: false);

        var systemAdminRole = await EnsureRoleAsync(securityDb, "SystemAdmin", inheritsFromRoleName: "BackofficeUserAdmin");
        var scopedAdminRole = await EnsureRoleAsync(securityDb, "BackofficeUserAdmin", inheritsFromRoleName: "BackofficeUser");
        var backofficeRole = await EnsureRoleAsync(securityDb, "BackofficeUser", inheritsFromRoleName: "Customer");
        var customerRole = await EnsureRoleAsync(securityDb, "Customer");

        var systemAdminUser = await EnsureUserAsync(securityDb, GuardrailSystemAdminEmail, "API Guardrail System Admin");
        var regionalAdminUser = await EnsureUserAsync(securityDb, GuardrailRegionalAdminEmail, "API Guardrail Regional Admin");
        var dualUser = await EnsureUserAsync(securityDb, GuardrailDualUserEmail, "API Guardrail Dual User");
        var allowedTargetUser = await EnsureUserAsync(securityDb, GuardrailAllowedTargetEmail, "API Guardrail Allowed Target");
        var disallowedTargetUser = await EnsureUserAsync(securityDb, GuardrailDisallowedTargetEmail, "API Guardrail Disallowed Target");

        await securityDb.SaveChangesAsync();

        var systemAdminInternalMembership = await EnsureMembershipAsync(securityDb, systemAdminUser.Id, internalOrganization.Id, isDefault: true);
        var regionalAllowedMembership = await EnsureMembershipAsync(securityDb, regionalAdminUser.Id, allowedOrganization.Id, isDefault: true);
        var dualInternalMembership = await EnsureMembershipAsync(securityDb, dualUser.Id, internalOrganization.Id, isDefault: true);
        var dualRetailMembership = await EnsureMembershipAsync(securityDb, dualUser.Id, allowedOrganization.Id, isDefault: false);
        var allowedTargetMembership = await EnsureMembershipAsync(securityDb, allowedTargetUser.Id, allowedOrganization.Id, isDefault: true);
        var disallowedTargetMembership = await EnsureMembershipAsync(securityDb, disallowedTargetUser.Id, disallowedOrganization.Id, isDefault: true);

        await securityDb.SaveChangesAsync();

        await EnsureRoleAssignmentAsync(securityDb, systemAdminUser.Id, systemAdminRole.Id, organizationMembershipId: null, assignedBy: systemAdminUser.Id);
        await EnsureRoleAssignmentAsync(securityDb, regionalAdminUser.Id, scopedAdminRole.Id, regionalAllowedMembership.Id, systemAdminUser.Id);
        await EnsureRoleAssignmentAsync(securityDb, dualUser.Id, backofficeRole.Id, dualInternalMembership.Id, systemAdminUser.Id);
        await EnsureRoleAssignmentAsync(securityDb, dualUser.Id, customerRole.Id, dualRetailMembership.Id, systemAdminUser.Id);
        await EnsureRoleAssignmentAsync(securityDb, allowedTargetUser.Id, customerRole.Id, allowedTargetMembership.Id, systemAdminUser.Id);
        await EnsureRoleAssignmentAsync(securityDb, disallowedTargetUser.Id, customerRole.Id, disallowedTargetMembership.Id, systemAdminUser.Id);

        await securityDb.SaveChangesAsync();

        return new FixtureIds(
            systemAdminUser.Id,
            regionalAdminUser.Id,
            dualUser.Id,
            internalOrganization.Id,
            allowedOrganization.Id,
            allowedOrganization.Key,
            allowedTargetMembership.Id,
            disallowedOrganization.Id,
            disallowedOrganization.Key,
            disallowedTargetMembership.Id,
            dualRetailMembership.OrganizationId,
            allowedCustomer.Id,
            dualRetailMembership.Organization!.Key);
    }

    private static async Task ResetScopedAdminAssignmentAsync(IServiceProvider services, int organizationMembershipId)
    {
        await using var scope = services.CreateAsyncScope();
        var securityDb = scope.ServiceProvider.GetRequiredService<DabblerSecurityDbContext>();
        var adminRoleId = await securityDb.AppRoles
            .Where(role => role.Name == "BackofficeUserAdmin")
            .Select(role => role.Id)
            .SingleAsync();

        var assignments = await securityDb.AppUserRoles
            .Where(assignment => assignment.OrganizationMembershipId == organizationMembershipId
                && assignment.RoleId == adminRoleId
                && assignment.DeletedAt == null)
            .ToListAsync();

        foreach (var assignment in assignments)
        {
            assignment.DeletedAt = DateTime.UtcNow;
            assignment.DeletedBy = 1;
        }

        await securityDb.SaveChangesAsync();
    }

    private static void SetHttpContext(
        IServiceProvider services,
        int userId,
        int organizationId,
        string organizationKey,
        params string[] roleNames)
        => SetHttpContext(services, userId.ToString(), organizationId, organizationKey, roleNames);

    private static void SetHttpContext(
        IServiceProvider services,
        string userId,
        int organizationId,
        string organizationKey,
        params string[] roleNames)
    {
        var accessor = services.GetRequiredService<IHttpContextAccessor>();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(Dabbler.Security.Services.UserSessionContextConstants.ActiveOrganizationIdClaimType, organizationId.ToString()),
            new(Dabbler.Security.Services.UserSessionContextConstants.ActiveOrganizationKeyClaimType, organizationKey)
        };

        claims.AddRange(roleNames.Select(roleName => new Claim(ClaimTypes.Role, roleName)));

        accessor.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
        };
    }

    private static async Task<AppOrganization> EnsureOrganizationAsync(
        DabblerSecurityDbContext securityDb,
        string key,
        string name,
        bool isInternal)
    {
        var organization = await securityDb.AppOrganizations
            .FirstOrDefaultAsync(candidate => candidate.Key == key);

        if (organization is null)
        {
            organization = new AppOrganization
            {
                Key = key,
                Name = name,
                IsInternal = isInternal,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            securityDb.AppOrganizations.Add(organization);
        }
        else
        {
            organization.Name = name;
            organization.IsInternal = isInternal;
            organization.IsActive = true;
        }

        return organization;
    }

    private static async Task<AppRole> EnsureRoleAsync(
        DabblerSecurityDbContext securityDb,
        string name,
        string? inheritsFromRoleName = null)
    {
        var role = await securityDb.AppRoles
            .FirstOrDefaultAsync(candidate => candidate.Name == name);

        if (role is null)
        {
            role = new AppRole
            {
                Name = name,
                Description = $"Guardrail role {name}",
                InheritsFromRoleName = inheritsFromRoleName,
                IsSystemRole = true,
                CreatedAt = DateTime.UtcNow
            };
            securityDb.AppRoles.Add(role);
        }
        else
        {
            role.InheritsFromRoleName = inheritsFromRoleName;
            role.DeletedAt = null;
        }

        return role;
    }

    private static async Task<AppUser> EnsureUserAsync(
        DabblerSecurityDbContext securityDb,
        string email,
        string displayName)
    {
        var user = await securityDb.AppUsers
            .FirstOrDefaultAsync(candidate => candidate.Email == email);

        if (user is null)
        {
            user = new AppUser
            {
                Email = email,
                DisplayName = displayName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            securityDb.AppUsers.Add(user);
        }
        else
        {
            user.DisplayName = displayName;
            user.IsActive = true;
            user.DeletedAt = null;
        }

        return user;
    }

    private static async Task<AppUserOrganizationMembership> EnsureMembershipAsync(
        DabblerSecurityDbContext securityDb,
        int userId,
        int organizationId,
        bool isDefault)
    {
        var membership = await securityDb.AppUserOrganizationMemberships
            .FirstOrDefaultAsync(candidate => candidate.UserId == userId && candidate.OrganizationId == organizationId);

        if (membership is null)
        {
            membership = new AppUserOrganizationMembership
            {
                UserId = userId,
                OrganizationId = organizationId,
                IsDefault = isDefault,
                IsActive = true,
                JoinedAt = DateTime.UtcNow
            };
            securityDb.AppUserOrganizationMemberships.Add(membership);
        }
        else
        {
            membership.IsDefault = isDefault;
            membership.IsActive = true;
            membership.DeletedAt = null;
        }

        return membership;
    }

    private static async Task EnsureRoleAssignmentAsync(
        DabblerSecurityDbContext securityDb,
        int userId,
        int roleId,
        int? organizationMembershipId,
        int assignedBy)
    {
        var assignment = await securityDb.AppUserRoles
            .FirstOrDefaultAsync(candidate => candidate.UserId == userId
                && candidate.RoleId == roleId
                && candidate.OrganizationMembershipId == organizationMembershipId);

        if (assignment is null)
        {
            securityDb.AppUserRoles.Add(new AppUserRole
            {
                UserId = userId,
                RoleId = roleId,
                OrganizationMembershipId = organizationMembershipId,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = assignedBy
            });
            return;
        }

        assignment.DeletedAt = null;
        assignment.DeletedBy = null;
        assignment.AssignedAt = DateTime.UtcNow;
        assignment.AssignedBy = assignedBy;
    }

    private sealed record FixtureIds(
        int SystemAdminUserId,
        int RegionalSalesRepUserId,
        int DualMembershipUserId,
        int InternalOrganizationId,
        int AllowedRetailOrganizationId,
        string AllowedRetailOrganizationKey,
        int AllowedRetailTargetMembershipId,
        int DisallowedRetailOrganizationId,
        string DisallowedRetailOrganizationKey,
        int DisallowedRetailTargetMembershipId,
        int DualMembershipRetailOrganizationId,
        int DualMembershipRetailCustomerId,
        string DualMembershipRetailOrganizationKey);
}
