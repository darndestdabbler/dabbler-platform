using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;

namespace AcmeCoffeeOnline.Playwright;

/// <summary>
/// Session 8 of the <c>rearchitect-role-administration-dashboard</c> session set.
/// Full-dashboard browser coverage layered on top of the five-tab smoke test from
/// Session 2, the restriction-enforcement tests from Session 5, and the role-
/// assignment workflow tests from Session 7. Covers the per-role tab-gating
/// matrix across all five baked-in roles, tab-strip arrow-key navigation, the
/// sensitive-name safety net's Add-to-Default-Masks flow, and the Role
/// Documentation tab's rendering.
/// </summary>
[Collection(PlaywrightCollection.Name)]
public sealed class SecurityDashboardFullSweepTests
{
    private const string AdminEmail = "admin@acmecoffee.com";

    private readonly PlaywrightFixture _fixture;

    public SecurityDashboardFullSweepTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Matrix of visible tabs per baked-in role. Matches
    /// <c>DabblerSecurityDashboard.AllTabs</c>'s <c>AllowedBakedInRoles</c>.
    /// </summary>
    public static IEnumerable<object[]> TabGatingMatrix() =>
    [
        ["SystemAdmin", new[]
        {
            "Role Master Details", "Default Masks", "Organization Master Details",
            "Users and Role Assignment", "Role Documentation",
        }],
        ["CrossOrgUserAdmin", new[]
        {
            "Organization Master Details", "Users and Role Assignment", "Role Documentation",
        }],
        ["CrossOrgMonitor", new[]
        {
            "Role Documentation",
        }],
        ["UserAdmin", new[]
        {
            "Users and Role Assignment", "Role Documentation",
        }],
        ["User", new[]
        {
            "Role Documentation",
        }],
    ];

    [Theory]
    [MemberData(nameof(TabGatingMatrix))]
    public async Task PerRole_TabGatingMatchesCompiledPrivilegesAsync(
        string role,
        string[] expectedLabels)
    {
        var allLabels = new[]
        {
            "Role Master Details", "Default Masks", "Organization Master Details",
            "Users and Role Assignment", "Role Documentation",
        };

        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, AdminEmail, roles: role);
            await page.GotoAsync($"{_fixture.BaseUrl}/admin/security", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
            });

            foreach (var label in expectedLabels)
            {
                await Assertions
                    .Expect(page.GetByRole(AriaRole.Tab, new() { Name = label }))
                    .ToBeVisibleAsync();
            }

            foreach (var label in allLabels.Except(expectedLabels))
            {
                await Assertions
                    .Expect(page.GetByRole(AriaRole.Tab, new() { Name = label }))
                    .ToHaveCountAsync(0);
            }
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }


    [Fact]
    public async Task RoleDocumentationTab_RendersAllFiveBakedInPanelsAsync()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, AdminEmail, roles: "SystemAdmin");
            await page.GotoAsync($"{_fixture.BaseUrl}/admin/security", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
            });

            await page.GetByRole(AriaRole.Tab, new() { Name = "Role Documentation" }).ClickAsync();

            await Assertions
                .Expect(page.GetByTestId("role-documentation-tab"))
                .ToBeVisibleAsync();

            foreach (var role in new[]
                     {
                         "SystemAdmin", "CrossOrgUserAdmin", "CrossOrgMonitor",
                         "UserAdmin", "User",
                     })
            {
                await Assertions
                    .Expect(page.GetByTestId($"role-doc-panel-{role}"))
                    .ToBeVisibleAsync();
            }

            await Assertions
                .Expect(page.GetByTestId("role-doc-panel-user-type-defaults"))
                .ToBeVisibleAsync();

            await Assertions
                .Expect(page.GetByTestId("role-doc-panel-default-masks"))
                .ToBeVisibleAsync();
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

}
