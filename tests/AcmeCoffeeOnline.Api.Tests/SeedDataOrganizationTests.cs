using AcmeCoffeeOnline.Api.Data;
using AcmeCoffeeOnline.Shared.Models;
using Dabbler.Security.Entities;

namespace AcmeCoffeeOnline.Api.Tests;

public sealed class SeedDataOrganizationTests
{
    [Fact]
    public void GetOrganizations_IncludesConfiguredInternalOrganization_AndRetailBusinessOrganizations()
    {
        var customers = BuildCustomersWithIds();

        var organizations = SeedData.GetOrganizations(customers, "internal", "Internal Operations");

        Assert.Contains(organizations, organization => organization.IsInternal && organization.Key == "internal");
        Assert.Equal(customers.Count + 1, organizations.Count);
        Assert.Contains(organizations, organization => organization.Key == "customer-1");
    }

    [Fact]
    public void GetOrganizationMemberships_SeedsRegionalSalesRep_AndDualMembershipUser()
    {
        var customers = BuildCustomersWithIds();
        var users = BuildUsersWithIds(customers);
        var organizations = BuildOrganizationsWithIds(customers);

        var memberships = SeedData.GetOrganizationMemberships(users, organizations, customers);

        var regionalSalesRep = users.Single(user => user.Email == SeedData.RegionalSalesRepEmail);
        var dualMembershipUser = users.Single(user => user.Email == SeedData.DualMembershipUserEmail);

        Assert.Equal(4, memberships.Count(membership => membership.UserId == regionalSalesRep.Id));
        Assert.Equal(2, memberships.Count(membership => membership.UserId == dualMembershipUser.Id));
        Assert.Single(memberships, membership => membership.UserId == regionalSalesRep.Id && membership.IsDefault);
    }

    [Fact]
    public void GetUserRoleAssignments_KeepsSystemAdminGlobal_AndScopesRegionalSalesRepAdminAssignments()
    {
        var customers = BuildCustomersWithIds();
        var users = BuildUsersWithIds(customers);
        var organizations = BuildOrganizationsWithIds(customers);
        var roles = BuildRolesWithIds();
        var memberships = SeedData.GetOrganizationMemberships(users, organizations, customers);
        AssignIds(memberships);

        var assignments = SeedData.GetUserRoleAssignments(users, roles, memberships);

        var systemAdmin = users.Single(user => user.Email == "admin@acmecoffee.com");
        var regionalSalesRep = users.Single(user => user.Email == SeedData.RegionalSalesRepEmail);
        var adminRole = roles.Single(role => role.Name == "BackofficeUserAdmin");

        Assert.Contains(assignments, assignment => assignment.UserId == systemAdmin.Id && assignment.OrganizationMembershipId == null);
        Assert.Equal(
            3,
            assignments.Count(assignment =>
                assignment.UserId == regionalSalesRep.Id
                && assignment.RoleId == adminRole.Id
                && assignment.OrganizationMembershipId.HasValue));
    }

    private static List<Customer> BuildCustomersWithIds()
    {
        var customers = SeedData.GetCustomers();
        AssignIds(customers);
        return customers;
    }

    private static List<AppUser> BuildUsersWithIds(List<Customer> customers)
    {
        var users = SeedData.GetSecurityUsers(customers);
        AssignIds(users);
        return users;
    }

    private static List<AppOrganization> BuildOrganizationsWithIds(List<Customer> customers)
    {
        var organizations = SeedData.GetOrganizations(customers, "internal", "Internal Operations");
        AssignIds(organizations);
        return organizations;
    }

    private static List<AppRole> BuildRolesWithIds()
    {
        var roles = SeedData.GetSecurityRoles();
        AssignIds(roles);
        return roles;
    }

    private static void AssignIds<T>(IReadOnlyList<T> items) where T : class
    {
        for (var index = 0; index < items.Count; index++)
        {
            typeof(T).GetProperty("Id")!.SetValue(items[index], index + 1);
        }
    }
}
