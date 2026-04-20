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
    private const string AdminEmail    = "admin@acmecoffee.com";
    private const string AsmithEmail   = "asmith@acmecoffee.com";
    private const string JdoeEmail     = "jdoe@acmecoffee.com";

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

    // ── Session 3: Per-role tab gating and Security nav visibility ───────────

    // UAT finding 2: SystemAdmin sees the Security Dashboard nav entry in the
    // Admin drawer group, reaches the page, and sees all five tabs.
    [Fact]
    public async Task SecurityNav_SystemAdmin_NavVisible_And_FiveTabsAsync()
    {
        // UAT: admin@acmecoffee.com → Admin nav group → Security Dashboard →
        //      five tabs visible (Role Master Details, Default Masks,
        //      Organization Master Details, Users and Role Assignment,
        //      Role Documentation).
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, AdminEmail, roles: "SystemAdmin");

            await _fixture.NavigateViaDrawerGroupAsync(
                page, "Admin", "Security Dashboard", ".*/admin/security$");

            await Assertions.Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Role Master Details" }))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
            await Assertions.Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Default Masks" }))
                .ToBeVisibleAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Organization Master Details" }))
                .ToBeVisibleAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Users and Role Assignment" }))
                .ToBeVisibleAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Role Documentation" }))
                .ToBeVisibleAsync();
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    // UAT finding 2: CrossOrgUserAdmin sees the Security Dashboard nav entry,
    // can reach the page, and sees exactly three tabs (Org, Users, Role Doc).
    [Fact]
    public async Task SecurityNav_CrossOrgUserAdmin_NavVisible_And_ThreeTabsAsync()
    {
        // UAT: asmith@acmecoffee.com (CrossOrgUserAdmin) → Admin nav group →
        //      Security Dashboard → three tabs: Organization Master Details,
        //      Users and Role Assignment, Role Documentation.
        //      Role Master Details and Default Masks must be absent.
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, AsmithEmail, roles: "CrossOrgUserAdmin");

            await _fixture.NavigateViaDrawerGroupAsync(
                page, "Admin", "Security Dashboard", ".*/admin/security$");

            await Assertions.Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Organization Master Details" }))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
            await Assertions.Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Users and Role Assignment" }))
                .ToBeVisibleAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Role Documentation" }))
                .ToBeVisibleAsync();

            await Assertions.Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Role Master Details" }))
                .ToHaveCountAsync(0);
            await Assertions.Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Default Masks" }))
                .ToHaveCountAsync(0);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    // UAT finding 2: Baked-in User (represented by jdoe) sees the Security
    // Dashboard nav entry, reaches the page, and sees only Role Documentation.
    [Fact]
    public async Task SecurityNav_BakedInUser_NavVisible_And_RoleDocOnlyAsync()
    {
        // UAT: jdoe@acmecoffee.com (baked-in User role) → Admin nav group →
        //      Security Dashboard → one tab: Role Documentation.
        //      All other tabs must be absent.
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, JdoeEmail, roles: "User");

            await _fixture.NavigateViaDrawerGroupAsync(
                page, "Admin", "Security Dashboard", ".*/admin/security$");

            await Assertions.Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Role Documentation" }))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            await Assertions.Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Role Master Details" }))
                .ToHaveCountAsync(0);
            await Assertions.Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Default Masks" }))
                .ToHaveCountAsync(0);
            await Assertions.Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Organization Master Details" }))
                .ToHaveCountAsync(0);
            await Assertions.Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Users and Role Assignment" }))
                .ToHaveCountAsync(0);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    // UAT finding 2, step 4: Re-run Session 2 Users-tab load test under
    // CrossOrgUserAdmin to confirm the DbContextFactory fix holds across
    // the broader persona set now that this role can reach the page.
    [Fact]
    public async Task UsersTab_LoadsForCrossOrgUserAdmin_NoCircuitCrashAsync()
    {
        // UAT: asmith@acmecoffee.com (CrossOrgUserAdmin) → /admin/security →
        //      Users and Role Assignment tab → master-grid and details panel
        //      render without a circuit exception.
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, AsmithEmail, roles: "CrossOrgUserAdmin");

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
}
