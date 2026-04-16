using System.Text.RegularExpressions;
using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;

namespace AcmeCoffeeOnline.Playwright;

[Collection(PlaywrightCollection.Name)]
public sealed class MultiOrganizationWorkflowTests
{
    private const string DualMembershipEmail = "casey.hart@acmecoffee.com";
    private const string RegionalSalesRepEmail = "sloane.reyes@acmecoffee.com";

    private readonly PlaywrightFixture _fixture;

    public MultiOrganizationWorkflowTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task E2E_WF_02_DualMembershipUser_SwitchesShellScopeAndLosesInternalRoutesAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await _fixture.SeededSignInAsync(page, DualMembershipEmail);

            var initialSession = await _fixture.GetSessionInfoAsync(page);
            Assert.Equal(DualMembershipEmail, initialSession.Email);
            Assert.False(string.IsNullOrWhiteSpace(initialSession.UserId));
            Assert.Equal("internal", initialSession.ActiveOrganizationKey);
            Assert.Contains("BackofficeUser", initialSession.Roles ?? []);

            var retailOrganization = initialSession.AvailableOrganizations?
                .SingleOrDefault(organization =>
                    !organization.IsInternal
                    && (organization.Roles ?? []).Contains("Customer", StringComparer.Ordinal))
                ?? throw new Xunit.Sdk.XunitException("The dual-membership user did not expose a retail organization.");

            await page.GotoAsync($"{_fixture.BaseUrl}/customers", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            await Assertions.Expect(page)
                .ToHaveURLAsync(new Regex(".*/customers$"));
            await Assertions.Expect(page.GetByTestId("access-denied-page"))
                .ToHaveCountAsync(0);

            await _fixture.SwitchOrganizationFromShellAsync(page, retailOrganization.OrganizationName);

            await Assertions.Expect(page.GetByTestId("active-organization-label"))
                .ToHaveTextAsync(retailOrganization.OrganizationName);

            var switchedSession = await _fixture.GetSessionInfoAsync(page);
            Assert.Equal(retailOrganization.OrganizationId.ToString(), switchedSession.ActiveOrganizationId);
            Assert.Contains("Customer", switchedSession.Roles ?? []);
            Assert.DoesNotContain("BackofficeUser", switchedSession.Roles ?? []);

            await page.GotoAsync($"{_fixture.BaseUrl}/orders", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            await Assertions.Expect(page)
                .ToHaveURLAsync(new Regex(".*/orders$"));
            await Assertions.Expect(page.GetByTestId("access-denied-page"))
                .ToHaveCountAsync(0);

            await page.GotoAsync($"{_fixture.BaseUrl}/admin/users", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            await Assertions.Expect(page.GetByTestId("access-denied-page"))
                .ToBeVisibleAsync();
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task E2E_WF_01_RegionalSalesRep_ManagesScopedUserAdminWithoutLeakingAcrossOrganizationsAsync()
    {
        var page = await _fixture.CreatePageAsync();
        string? actorUserId = null;
        int? firstManagedOrganizationId = null;
        int? targetMembershipId = null;
        bool? originalHasScopedUserAdmin = null;

        try
        {
            await _fixture.SeededSignInAsync(page, RegionalSalesRepEmail);

            var initialSession = await _fixture.GetSessionInfoAsync(page);
            actorUserId = initialSession.UserId;

            Assert.False(string.IsNullOrWhiteSpace(actorUserId));
            Assert.Equal("internal", initialSession.ActiveOrganizationKey);
            Assert.Contains("BackofficeUser", initialSession.Roles ?? []);

            var managedOrganizations = initialSession.AvailableOrganizations?
                .Where(organization =>
                    !organization.IsInternal
                    && (organization.Roles ?? []).Contains("BackofficeUserAdmin", StringComparer.Ordinal))
                .ToArray()
                ?? [];

            Assert.True(
                managedOrganizations.Length >= 2,
                "The regional sales rep needs at least two managed retail organizations for WF-01.");

            var firstManagedOrganization = managedOrganizations[0];
            var secondManagedOrganization = managedOrganizations[1];
            firstManagedOrganizationId = firstManagedOrganization.OrganizationId;

            await page.GotoAsync($"{_fixture.BaseUrl}/admin/users", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            await Assertions.Expect(page)
                .ToHaveURLAsync(new Regex(".*/admin/users$"));
            await Assertions.Expect(page.GetByTestId("user-admin-scope-warning"))
                .ToBeVisibleAsync();

            await _fixture.SwitchOrganizationFromShellAsync(page, firstManagedOrganization.OrganizationName);

            await page.GotoAsync($"{_fixture.BaseUrl}/admin/users", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            await Assertions.Expect(page.GetByTestId("admin-scope-banner"))
                .ToContainTextAsync(firstManagedOrganization.OrganizationName);

            var beforeScope = await _fixture.GetAdminUserScopeAsync(actorUserId!, firstManagedOrganization.OrganizationId);
            var targetUser = beforeScope.Users.FirstOrDefault(user => user.CanGrantUserAdmin)
                ?? beforeScope.Users.FirstOrDefault(user => user.CanRevokeUserAdmin)
                ?? throw new Xunit.Sdk.XunitException("Could not find a scoped user-admin action target in the managed organization.");

            targetMembershipId = targetUser.OrganizationMembershipId;
            originalHasScopedUserAdmin = targetUser.HasScopedUserAdmin;

            var actionTestId = originalHasScopedUserAdmin.Value
                ? $"revoke-useradmin-{targetMembershipId.Value}"
                : $"grant-useradmin-{targetMembershipId.Value}";
            var expectedSnackbar = originalHasScopedUserAdmin.Value
                ? $"Removed UserAdmin for {targetUser.DisplayName} in the active organization."
                : $"Granted UserAdmin for {targetUser.DisplayName} in the active organization.";

            await Assertions.Expect(page.GetByTestId(actionTestId))
                .ToBeVisibleAsync();
            await page.GetByTestId(actionTestId).ClickAsync();

            await Assertions.Expect(page.GetByText(expectedSnackbar, new PageGetByTextOptions { Exact = true }))
                .ToBeVisibleAsync();

            var afterScope = await _fixture.GetAdminUserScopeAsync(actorUserId!, firstManagedOrganization.OrganizationId);
            var updatedTarget = afterScope.Users.Single(user => user.OrganizationMembershipId == targetMembershipId.Value);
            Assert.Equal(!originalHasScopedUserAdmin.Value, updatedTarget.HasScopedUserAdmin);

            await _fixture.SwitchOrganizationFromShellAsync(page, secondManagedOrganization.OrganizationName);

            await page.GotoAsync($"{_fixture.BaseUrl}/admin/users", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            await Assertions.Expect(page.GetByTestId("admin-scope-banner"))
                .ToContainTextAsync(secondManagedOrganization.OrganizationName);
            await Assertions.Expect(page.GetByTestId(actionTestId))
                .ToHaveCountAsync(0);

            var secondScope = await _fixture.GetAdminUserScopeAsync(actorUserId!, secondManagedOrganization.OrganizationId);
            Assert.DoesNotContain(secondScope.Users, user => user.OrganizationMembershipId == targetMembershipId.Value);
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(actorUserId)
                && firstManagedOrganizationId.HasValue
                && targetMembershipId.HasValue
                && originalHasScopedUserAdmin.HasValue)
            {
                try
                {
                    var cleanupScope = await _fixture.GetAdminUserScopeAsync(actorUserId, firstManagedOrganizationId.Value);
                    var cleanupTarget = cleanupScope.Users.FirstOrDefault(user => user.OrganizationMembershipId == targetMembershipId.Value);

                    if (cleanupTarget is not null && cleanupTarget.HasScopedUserAdmin != originalHasScopedUserAdmin.Value)
                    {
                        if (originalHasScopedUserAdmin.Value)
                        {
                            await _fixture.GrantScopedUserAdminAsync(
                                actorUserId,
                                firstManagedOrganizationId.Value,
                                targetMembershipId.Value);
                        }
                        else
                        {
                            await _fixture.RevokeScopedUserAdminAsync(
                                actorUserId,
                                firstManagedOrganizationId.Value,
                                targetMembershipId.Value);
                        }
                    }
                }
                catch
                {
                }
            }

            await _fixture.ClosePageAsync(page);
        }
    }
}
