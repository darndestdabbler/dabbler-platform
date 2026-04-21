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
    private const string JuserEmail     = "juser@acmecoffee.com";

    private readonly PlaywrightFixture _fixture;

    public SecurityDashboardUatFollowupCoverageTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    // Finding #2 (Blocker) — real-login flow (Session 2 re-remediation).
    // Drives the sequence the human UAT reviewer actually performs: open
    // /login, enter juser@acmecoffee.com, click "Send sign-in link",
    // navigate to /dev/email-inbox, grab the Link cell of the most recent
    // row, follow that href, land authenticated. Asserts the Admin drawer
    // group is visible and the Security Dashboard shows only Role
    // Documentation. The prior Session 1 test used SeededSignInAsync with
    // a roles=User override, which reproduced the end state but NOT the
    // path — so the regression against real login was invisible to
    // automation. After Session 2's seed change, juser is a DB-backed user
    // whose user-roles.json assignment is the baked-in User role; that
    // identity is resolvable through the standard magic-link pipeline.
    [Fact]
    public async Task SecurityNav_BakedInUserPersona_RealLoginFlow_NavVisibleRoleDocOnlyAsync()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            // Clear the dev inbox so the "top-most" row is unambiguously
            // our freshly-issued magic link, not a leftover from a prior
            // test.
            await _fixture.ClearMagicLinksAsync();

            // Step 1: open the login screen.
            await page.GotoAsync(
                $"{_fixture.BaseUrl}/login",
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await Assertions
                .Expect(page.GetByTestId("login-page"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            // Step 2: enter juser's email and request a magic link.
            await page.Locator("input[type='email']").FillAsync(JuserEmail);
            await page.GetByRole(AriaRole.Button, new() { Name = "Send sign-in link" }).ClickAsync();

            // Step 3: the login screen confirms the code was sent (step 2 of
            // the login state machine). This proves the send-magic-link
            // request succeeded before we look for the email.
            await Assertions
                .Expect(page.GetByText($"We sent a sign-in code to"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            // Step 4: open /dev/email-inbox in a fresh page. Running in the
            // same page would lose the login state we just set up if
            // navigation interrupts the step-2 timer; a separate page
            // better mirrors what the human does (second browser tab).
            var inboxPage = await _fixture.CreatePageAsync();
            try
            {
                await inboxPage.GotoAsync(
                    $"{_fixture.BaseUrl}/dev/email-inbox",
                    new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

                // Step 5: grab the top-most DATA row's Link cell and read
                // its href. Scope inside `tbody` so the header `<tr>` (which
                // also carries `class="mud-table-row"` in MudBlazor's DOM)
                // doesn't steal the `.First` match. The MudLink renders as
                // an <a target="_blank"> — instead of clicking it (which
                // would pop up a new tab), read the href and navigate the
                // primary page to it, which mirrors "follow the link" with
                // the browser's single tab.
                var firstRow = inboxPage.Locator("tbody tr.mud-table-row").First;
                await Assertions
                    .Expect(firstRow)
                    .ToBeVisibleAsync(new() { Timeout = 15_000 });
                var linkCell = firstRow.Locator("td[data-label='Link']");
                await Assertions
                    .Expect(linkCell)
                    .ToBeVisibleAsync(new() { Timeout = 5_000 });

                // Step 4 / V1 Playwright-native assertion that the Link
                // cell contains a followable link: the first <a> inside
                // the cell has an href that points at the magic-link
                // landing URL (http(s)://.../login?token=...).
                var linkAnchor = linkCell.Locator("a").First;
                await Assertions
                    .Expect(linkAnchor)
                    .ToHaveAttributeAsync("href", new System.Text.RegularExpressions.Regex(@"^https?://.+/login\?token="));

                var magicHref = await linkAnchor.GetAttributeAsync("href");
                await page.GotoAsync(magicHref!, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            }
            finally
            {
                await _fixture.ClosePageAsync(inboxPage);
            }

            // Step 6: the human lands on an authenticated app route. The
            // Admin drawer group must be visible for a user whose only
            // role is the baked-in User (Role Documentation read access).
            await Assertions
                .Expect(page.GetByRole(AriaRole.Button, new() { Name = "Admin" }))
                .ToBeVisibleAsync(new() { Timeout = 15_000 });

            // Step 7: navigate via the Admin drawer group to Security
            // Dashboard and confirm only Role Documentation is visible.
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
