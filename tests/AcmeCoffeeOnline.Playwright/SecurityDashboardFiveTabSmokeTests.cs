using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;

namespace AcmeCoffeeOnline.Playwright;

/// <summary>
/// Browser-level smoke coverage for the Security Dashboard. Originally
/// authored for the five-tab layout stubbed in Session 2 of
/// <c>rearchitect-role-administration-dashboard</c>; expanded in Session 4
/// of <c>sensitive-field-masking-audit</c> to assert the appended sixth tab
/// (Compliance Review). Confirms that SystemAdmin sees every tab header and
/// that a narrower baked-in role (UserAdmin) only sees the tabs its compiled
/// privileges permit.
/// </summary>
[Collection(PlaywrightCollection.Name)]
public sealed class SecurityDashboardFiveTabSmokeTests
{
    private const string AdminEmail = "admin@acmecoffee.com";

    private static readonly string[] AllTabLabels =
    [
        "Role Master Details",
        "Default Masks",
        "Organization Master Details",
        "Users and Role Assignment",
        "Role Documentation",
        "Compliance Review",
    ];

    private static readonly string[] UserAdminVisibleTabs =
    [
        "Users and Role Assignment",
        "Role Documentation",
    ];

    private static readonly string[] UserAdminHiddenTabs =
    [
        "Role Master Details",
        "Default Masks",
        "Organization Master Details",
        "Compliance Review",
    ];

    private readonly PlaywrightFixture _fixture;

    public SecurityDashboardFiveTabSmokeTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SystemAdmin_SeesAllSixTabsAsync()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, AdminEmail, roles: "SystemAdmin");
            await page.GotoAsync($"{_fixture.BaseUrl}/admin/security", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
            });

            foreach (var label in AllTabLabels)
            {
                await Assertions
                    .Expect(page.GetByRole(AriaRole.Tab, new() { Name = label }))
                    .ToBeVisibleAsync();
            }
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task UserAdmin_SeesOnlyPermittedTabsAsync()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, AdminEmail, roles: "UserAdmin");
            await page.GotoAsync($"{_fixture.BaseUrl}/admin/security", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
            });

            foreach (var label in UserAdminVisibleTabs)
            {
                await Assertions
                    .Expect(page.GetByRole(AriaRole.Tab, new() { Name = label }))
                    .ToBeVisibleAsync();
            }

            foreach (var label in UserAdminHiddenTabs)
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
}
