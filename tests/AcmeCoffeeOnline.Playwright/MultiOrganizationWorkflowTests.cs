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

    // E2E_WF_02 (DualMembershipUser_SwitchesShellScope...) lives in
    // MultiOrganizationWorkflowFlakyTests.cs under FlakyPlaywrightCollection
    // because the post-switch shell re-render is flaky under full-suite
    // parallelism. Keep the isolated-run invocation until the root cause is
    // addressed in a focused session.

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

            // Navigate to admin users via drawer group
            await _fixture.NavigateViaDrawerGroupAsync(page, "Admin", "User Management", ".*/admin/users$");

            await Assertions.Expect(page.GetByTestId("user-admin-scope-warning"))
                .ToBeVisibleAsync();

            // Switch to first managed organization
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

            // Switch to second organization and verify scoping
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

            // Verify layout on admin users page
            await PlaywrightFixture.AssertLayoutSpacingAsync(page);
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
