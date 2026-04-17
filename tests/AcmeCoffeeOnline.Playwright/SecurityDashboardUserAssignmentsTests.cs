using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;

namespace AcmeCoffeeOnline.Playwright;

/// <summary>
/// E2E coverage for the User Assignments tab of the Security Dashboard.
/// Targets the UAT finding: "I tried to elevate Jordan Ellis to SystemAdmin,
/// and that did not work." Verifies that an admin can add SystemAdmin to a user
/// through the Dabbler user-role manager, and that the target user's next seeded
/// sign-in reflects the new role in their session claims.
/// </summary>
[Collection(PlaywrightCollection.Name)]
public sealed class SecurityDashboardUserAssignmentsTests
{
    private const string AdminEmail = "admin@acmecoffee.com";
    private const string TargetUserEmail = "jdoe@acmecoffee.com";
    private const string TargetUserDisplayName = "Jordan Ellis";
    private const string RoleToAssign = "SystemAdmin";

    private readonly PlaywrightFixture _fixture;

    public SecurityDashboardUserAssignmentsTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SystemAdmin_ElevatesJordanViaUserAssignmentsTab_ElevationReflectsInTargetUserSessionAsync()
    {
        var adminPage = await _fixture.CreatePageAsync();
        var targetUserPage = await _fixture.CreatePageAsync();

        var elevated = false;
        try
        {
            // --- Pre-test cleanup: if a previous failed run left SystemAdmin on
            // Jordan, strip it before starting so the baseline assertion below is
            // reliable. IUserRoleStore persists across runs (user-roles.json), so
            // shared state is a real risk otherwise. ---
            await _fixture.SeededSignInAsync(adminPage, AdminEmail);
            await EnsureTargetUserDoesNotHaveRoleAsync(adminPage);
            await _fixture.SignOutAsync(adminPage);

            // --- Baseline: confirm the target user does not have SystemAdmin yet. ---
            await _fixture.SeededSignInAsync(targetUserPage, TargetUserEmail);
            var baselineSession = await _fixture.GetSessionInfoAsync(targetUserPage);
            Assert.True(baselineSession.IsAuthenticated, "Seeded sign-in as Jordan failed.");
            Assert.False(
                baselineSession.Roles?.Contains(RoleToAssign) ?? false,
                $"Expected Jordan Ellis to NOT have {RoleToAssign} before the admin elevates the account.");
            await _fixture.SignOutAsync(targetUserPage);

            // --- Admin signs in and opens the User Assignments tab. ---
            await _fixture.SeededSignInAsync(adminPage, AdminEmail);
            await adminPage.GotoAsync($"{_fixture.BaseUrl}/admin/security", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            await adminPage
                .GetByRole(AriaRole.Tab, new() { Name = "User Assignments" })
                .ClickAsync();
            await Assertions.Expect(adminPage.GetByText("User Role Assignments"))
                .ToBeVisibleAsync();

            // Target row for Jordan Ellis.
            var jordanRow = adminPage
                .Locator("tr", new() { Has = adminPage.GetByText(TargetUserDisplayName, new() { Exact = true }) })
                .First;
            await Assertions.Expect(jordanRow).ToBeVisibleAsync();

            // --- Open the role select, pick SystemAdmin, click add. ---
            var selectTrigger = jordanRow.Locator(
                $"[aria-label=\"Select a role to add to {TargetUserDisplayName}\"]");
            await selectTrigger.ClickAsync();

            var option = adminPage
                .Locator(".mud-popover-open")
                .GetByText(RoleToAssign, new() { Exact = true })
                .First;
            await Assertions.Expect(option).ToBeVisibleAsync();
            await option.ClickAsync();

            var addButton = jordanRow.Locator(
                $"button[aria-label=\"Add selected role to {TargetUserDisplayName}\"]");
            await Assertions.Expect(addButton).ToBeEnabledAsync();
            await addButton.ClickAsync();
            elevated = true;

            // Chip should appear in the current-roles column of Jordan's row.
            var systemAdminChip = jordanRow
                .Locator(".mud-chip")
                .GetByText(RoleToAssign, new() { Exact = true })
                .First;
            await Assertions.Expect(systemAdminChip).ToBeVisibleAsync();

            // --- Target user signs back in: claims should now include SystemAdmin. ---
            await _fixture.SeededSignInAsync(targetUserPage, TargetUserEmail);
            var elevatedSession = await _fixture.GetSessionInfoAsync(targetUserPage);
            Assert.Contains(RoleToAssign, elevatedSession.Roles ?? []);
        }
        finally
        {
            if (elevated)
            {
                // Clean up: remove the SystemAdmin role so later tests start from the
                // baseline. The MudChip renders an OnClose button; clicking it drives
                // RemoveRoleAsync through the IUserRoleStore.
                try
                {
                    var jordanRow = adminPage
                        .Locator("tr", new() { Has = adminPage.GetByText(TargetUserDisplayName, new() { Exact = true }) })
                        .First;
                    var chipCloseButton = jordanRow
                        .Locator(".mud-chip", new() { HasText = RoleToAssign })
                        .Locator("button")
                        .First;
                    if (await chipCloseButton.CountAsync() > 0)
                    {
                        await chipCloseButton.ClickAsync();
                        await Assertions.Expect(
                            jordanRow
                                .Locator(".mud-chip")
                                .GetByText(RoleToAssign, new() { Exact = true }))
                            .ToHaveCountAsync(0);
                    }
                }
                catch
                {
                    // Cleanup best-effort — surface the original failure, not the cleanup.
                }
            }

            await _fixture.ClosePageAsync(adminPage);
            await _fixture.ClosePageAsync(targetUserPage);
        }
    }

    /// <summary>
    /// Navigate admin to the User Assignments tab and, if Jordan's row has a
    /// <see cref="RoleToAssign"/> chip, click its close button to remove it.
    /// No-op when the role is already absent. Used to make the test idempotent
    /// across re-runs, since IUserRoleStore persists outside the test process.
    /// </summary>
    private async Task EnsureTargetUserDoesNotHaveRoleAsync(IPage adminPage)
    {
        await adminPage.GotoAsync($"{_fixture.BaseUrl}/admin/security", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });

        await adminPage
            .GetByRole(AriaRole.Tab, new() { Name = "User Assignments" })
            .ClickAsync();
        await Assertions.Expect(adminPage.GetByText("User Role Assignments"))
            .ToBeVisibleAsync();

        var jordanRow = adminPage
            .Locator("tr", new() { Has = adminPage.GetByText(TargetUserDisplayName, new() { Exact = true }) })
            .First;
        await Assertions.Expect(jordanRow).ToBeVisibleAsync();

        var existingChip = jordanRow.Locator(".mud-chip", new() { HasText = RoleToAssign });
        if (await existingChip.CountAsync() == 0)
        {
            return;
        }

        var closeButton = existingChip.Locator("button").First;
        await closeButton.ClickAsync();
        await Assertions.Expect(
            jordanRow.Locator(".mud-chip").GetByText(RoleToAssign, new() { Exact = true }))
            .ToHaveCountAsync(0);
    }
}
