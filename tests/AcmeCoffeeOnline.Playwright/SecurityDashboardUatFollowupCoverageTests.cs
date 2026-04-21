using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;

namespace AcmeCoffeeOnline.Playwright;

/// <summary>
/// Per-UAT-item coverage for the <c>security-dashboard-uat-followup-fixes</c>
/// session set. Each test mirrors one finding from the
/// <c>security-dashboard-uat-remediation-review</c> disposition so the matching
/// UAT checklist item has a Playwright test that drives the same steps with the
/// same seeded parameters.
/// </summary>
[Collection(PlaywrightCollection.Name)]
public sealed class SecurityDashboardUatFollowupCoverageTests
{
    private const string AdminEmail     = "admin@acmecoffee.com";
    private const string JdoeEmail      = "jdoe@acmecoffee.com";

    private readonly PlaywrightFixture _fixture;

    public SecurityDashboardUatFollowupCoverageTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    // Finding #2 (Blocker): A UAT reviewer who picks the "Jordan Ellis —
    // User (baked-in role override)" entry from the dev quick-sign-in
    // drop-down must see the Admin drawer group and the Security Dashboard
    // nav entry, with only the Role Documentation tab accessible inside.
    // Before the follow-up fix the dev drop-down had no persona that
    // signed the reviewer in under a baked-in role — every existing
    // persona was a user-type role with a `UiRestriction(/admin/security,
    // *, Hidden)` that kept the Admin drawer hidden. The fix adds a
    // roles-override path to the dev-quick-signin helper so a persona can
    // declare `RolesOverride = "User"` without requiring a DB-seeded user.
    // This test drives the same parameters the reviewer now has available
    // (jdoe email + roles=User override).
    [Fact]
    public async Task SecurityNav_BakedInUserPersona_NavVisible_RoleDocOnlyAsync()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, JdoeEmail, roles: "User");

            // Step 2 verification 1: the Admin drawer group is present in the
            // left navigation. Assert explicitly before navigating so a
            // regression that hides the drawer surfaces with a clear failure
            // rather than bubbling up as a missing-nav-entry timeout later.
            await Assertions
                .Expect(page.GetByRole(AriaRole.Button, new() { Name = "Admin" }))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

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

    // Finding #11 (Blocker): After a successful create the results shell
    // must auto-re-expand so the refreshed grid is visible without the
    // user having to click the shell header. The human reviewer reported
    // "new organization is not visible in the grid" because the prior
    // Session 7 flow left the shell collapsed and the single re-expand
    // click was easy to miss. This test asserts the new row surfaces
    // without any shell-header click after Save.
    [Fact]
    public async Task OrgMasterDetails_CreateOrganization_AutoReexpandsShellAsync()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, AdminEmail, roles: "SystemAdmin");

            await _fixture.NavigateViaDrawerGroupAsync(
                page, "Admin", "Security Dashboard", ".*/admin/security$");

            await page.GetByRole(AriaRole.Tab, new() { Name = "Organization Master Details" })
                .ClickAsync();

            await Assertions
                .Expect(page.GetByTestId("organization-master-grid"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            var marker = Guid.NewGuid().ToString("N")[..8];
            var newKey = $"customer-followup-{marker}";
            var newName = $"Followup {marker}";

            await page.GetByTestId("organization-grid-add-button").ClickAsync();

            // Step 5 / V1: the collapsed shell summary is visible AND reads
            // "New organization". The text-content check protects against a
            // regression where the summary renders but with a wrong label.
            var creatingSummary = page.GetByTestId("organization-master-details-shell-summary");
            await Assertions.Expect(creatingSummary).ToBeVisibleAsync(new() { Timeout = 10_000 });
            await Assertions.Expect(creatingSummary).ToHaveTextAsync("New organization");

            await page.GetByTestId("organization-key-field").Locator("input").FillAsync(newKey);
            await page.GetByTestId("organization-name-field").Locator("input").FillAsync(newName);
            await page.GetByTestId("organization-save-button").ClickAsync();

            // Key assertion for finding #11: no shell-header click below.
            // The results shell must auto-re-expand once persistence succeeds
            // so the organization-master-grid element is visible AND the new
            // row shows up inside it. Scope the row-text lookups to the grid
            // container so we aren't accidentally matching a summary blurb
            // or snackbar that happens to echo the key/name elsewhere on the
            // page.
            var gridContainer = page.GetByTestId("organization-master-grid-container");
            await Assertions
                .Expect(page.GetByTestId("organization-master-grid"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
            await Assertions
                .Expect(gridContainer.Locator($"text={newKey}").First)
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
            await Assertions
                .Expect(gridContainer.Locator($"text={newName}").First)
                .ToBeVisibleAsync(new() { Timeout = 5_000 });
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    // Finding #14 (Minor): the Unit Price column on /products renders the
    // currency symbol from the active thread's culture. Linux containers
    // start with POSIX locale so .NET falls back to InvariantCulture's
    // "¤" symbol unless the app pins a culture explicitly. This test
    // asserts the formatted price carries the US dollar sign — the
    // culture pin lives in Web Program.cs.
    [Fact]
    public async Task Products_BackofficeUser_UnitPriceColumn_ShowsDollarSymbolAsync()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, JdoeEmail, roles: "BackofficeUser");

            await page.GotoAsync(
                $"{_fixture.BaseUrl}/products",
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await PlaywrightFixture.WaitForGridDataAsync(page);

            // Step 3 / V1: the "Unit Price" column header is visible in the
            // grid's `.mud-table-head` region. Matches the checklist's
            // verification text exactly so the coverage-review gate can
            // mechanically pair them.
            await Assertions
                .Expect(page.Locator(".mud-table-head").GetByText("Unit Price"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            // Step 3 / V2 and V3: the first data cell under the Unit Price
            // column. The C2 format produces e.g. "$4.99" when culture is
            // en-US and "¤4.99" when culture is invariant, so a `$`
            // substring check is a reliable signal for the fix.
            var firstUnitPriceCell = page
                .Locator("td[data-label='Unit Price']")
                .First;
            await Assertions
                .Expect(firstUnitPriceCell)
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
            await Assertions
                .Expect(firstUnitPriceCell)
                .ToContainTextAsync("$");
            await Assertions
                .Expect(firstUnitPriceCell)
                .Not.ToContainTextAsync("¤");
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    // Finding #49 (Minor): clicking a cell in the FIRST column of a
    // master grid must collapse the shell on the first click. The prior
    // `SelectedItemChanged`-based row-select path meant the first click
    // could silently set cell focus without reselecting the row, which
    // the reviewer experienced as "two clicks to collapse". The fix
    // switches each master grid to `RowClick`, which fires on every row
    // click regardless of the SelectedItem state. Exercising
    // RoleMasterGrid is representative because it's the most populated
    // grid and carries the canonical first-column MudText+MudChip
    // cell content the human clicked.
    [Fact]
    public async Task RoleMasterGrid_FirstColumnCellClick_CollapsesShellOnFirstClickAsync()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, AdminEmail, roles: "SystemAdmin");
            await page.GotoAsync(
                $"{_fixture.BaseUrl}/admin/security",
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

            await page.GetByRole(AriaRole.Tab, new() { Name = "Role Master Details" }).ClickAsync();

            // Shell starts expanded.
            await Assertions
                .Expect(page.GetByTestId("role-master-grid"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            // Click directly on the first-column cell content (MudText
            // inside the CellTemplate's wrapper div) rather than on
            // `xpath=ancestor::tr[1]`. The `role-row-{canonicalName}`
            // data-testid lives on the first-column CellTemplate's inner
            // <div> — see RoleMasterGrid.razor — so this locator is
            // cell-scoped, not row-scoped, despite the historical name.
            // Before the fix, this click path required two clicks — first
            // to set cell focus, second to select the row and collapse the
            // shell. After the fix, RowClick fires on the first click and
            // the shell collapses immediately.
            await page.GetByTestId("role-row-BackofficeUser").ClickAsync();

            // Step 4 / V1: the shell collapses on the first click —
            // role-master-details-shell-summary becomes visible.
            await Assertions
                .Expect(page.GetByTestId("role-master-details-shell-summary"))
                .ToBeVisibleAsync(new() { Timeout = 5_000 });

            // Step 4 / V2: the role-details-section is visible below the
            // collapsed summary — row-select drives both collapse and
            // details panel render on the same click.
            await Assertions
                .Expect(page.GetByTestId("role-details-section"))
                .ToBeVisibleAsync(new() { Timeout = 5_000 });
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }
}
