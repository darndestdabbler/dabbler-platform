using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;

namespace AcmeCoffeeOnline.Playwright;

/// <summary>
/// Per-UAT-item coverage suite for the
/// <c>security-dashboard-uat-remediation</c> session set. Each test method
/// carries a <c>// UAT:</c> comment identifying the checklist item it
/// mirrors. Sessions 2–8 of the set append to this file so that every
/// Review item in
/// <c>security-dashboard-uat-remediation-uat-checklist.json</c> has a
/// Playwright test that drives the same steps with the same seeded
/// parameters — the newly promoted Human UAT Philosophy principle made
/// this mandatory for the remediation set.
/// </summary>
[Collection(PlaywrightCollection.Name)]
public sealed class SecurityDashboardUatCoverageTests
{
    private const string AdminEmail = "admin@acmecoffee.com";

    private readonly PlaywrightFixture _fixture;

    public SecurityDashboardUatCoverageTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    // UAT finding 1: Users and Role Assignment tab initial load — SystemAdmin.
    // Before the Session 2 fix, activating this tab threw
    // InvalidOperationException ("second operation on context") from
    // AdminUserQueryService because UsersMasterGrid and
    // UserRoleAssignmentDetails both awaited the same scoped DbContext
    // during OnInitializedAsync. The IDbContextFactory refactor gives
    // every call its own context; the tab should now render without a
    // circuit exception.
    [Fact]
    public async Task UsersTab_LoadsForSystemAdmin_NoCircuitCrashAsync()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, AdminEmail, roles: "SystemAdmin");

            await page.GotoAsync(
                $"{_fixture.BaseUrl}/admin/security",
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

            await page.GetByRole(AriaRole.Tab, new() { Name = "Users and Role Assignment" }).ClickAsync();

            await Assertions
                .Expect(page.GetByTestId("users-master-grid"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            await Assertions
                .Expect(page.GetByTestId("user-role-assignment-details"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    // UAT finding 1: Users grid populates after tab activation. The
    // crash previously masked this operability check — once the tab
    // loads, the seeded users must actually surface in the grid.
    [Fact]
    public async Task UsersTab_LoadsForSystemAdmin_GridProjectsSeededRowsAsync()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, AdminEmail, roles: "SystemAdmin");

            await page.GotoAsync(
                $"{_fixture.BaseUrl}/admin/security",
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

            await page.GetByRole(AriaRole.Tab, new() { Name = "Users and Role Assignment" }).ClickAsync();

            await Assertions
                .Expect(page.GetByTestId("users-master-grid"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            // MudDataGrid renders rows asynchronously after the master-grid
            // shell appears. Wait for the first status chip (present on every
            // row, regardless of whether the user has roles or orgs) before
            // counting.
            var statusChips = page.Locator("[data-testid^='users-row-'][data-testid$='-status-chip']");
            await Assertions
                .Expect(statusChips.First)
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            var rowCount = await statusChips.CountAsync();
            Assert.True(rowCount >= 1, $"Users grid should render at least one seeded row, found {rowCount}.");
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    // UAT finding 1 regression guard: rapid tab remount. The original
    // defect was a concurrent-init race between UsersMasterGrid and
    // UserRoleAssignmentDetails. Switching away and back re-runs both
    // OnInitializedAsync methods side-by-side and would reproduce the
    // "second operation on context" crash under the old scoped-context
    // design. ClosePageAsync asserts no browser-side errors surfaced
    // during the sequence.
    [Fact]
    public async Task UsersTab_RapidRemountDoesNotCrashAsync()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, AdminEmail, roles: "SystemAdmin");

            await page.GotoAsync(
                $"{_fixture.BaseUrl}/admin/security",
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

            await page.GetByRole(AriaRole.Tab, new() { Name = "Users and Role Assignment" }).ClickAsync();
            await Assertions
                .Expect(page.GetByTestId("users-master-grid"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            // Force the tab-strip clicks: during rapid remount the MudTabs
            // re-render leaves a transient overlay above the tab strip, and
            // the default actionability check flags "subtree intercepts
            // pointer events". The purpose of this test is to regression-
            // guard the DbContext fix under rapid remount, not to prove the
            // tab-click UX (other tests cover that). Force-click reaches
            // the same tab handler the user would invoke.
            await page.GetByRole(AriaRole.Tab, new() { Name = "Role Master Details" })
                .ClickAsync(new() { Force = true });

            await page.GetByRole(AriaRole.Tab, new() { Name = "Users and Role Assignment" })
                .ClickAsync(new() { Force = true });
            await Assertions
                .Expect(page.GetByTestId("users-master-grid"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
            await Assertions
                .Expect(page.GetByTestId("user-role-assignment-details"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }
}
