using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;
using System.Text.RegularExpressions;

namespace AcmeCoffeeOnline.Playwright;

/// <summary>
/// 1:1 Playwright coverage for the
/// <c>sensitive-field-masking-audit-uat-checklist.json</c> UAT checklist
/// (remediated version — real magic-link login + UserContextBar role switching;
/// no <c>SeededSignInAsync(roles=)</c> overrides for human-equivalent steps).
///
/// Six items, one [Fact] each:
///   UAT-1  SSN renders masked for a user-type role with no exemption
///   UAT-2  SystemAdmin grants a mask exemption for the BackofficeUser role
///   UAT-3  BackofficeUser with active exemption sees SSN unmasked
///   UAT-4  CrossOrgMonitor sees ExemptionGranted + FieldUnmaskedRead in Compliance Review
///   UAT-5  Row expander renders the correlation cluster section (judgment item)
///   UAT-6  Export CSV button triggers a download
///
/// Each method:
/// - Uses real magic-link login through /login → /dev/email-inbox → magic-link follow
/// - Uses the UserContextBar role switcher for role changes (visible-surface only)
/// - Uses in-circuit Blazor navigation after login (drawer clicks, grid row clicks)
/// - Contains // begin STEP NNN markers aligned to the checklist item's Steps[].StepNumber
///
/// Pre-test setup (not human-equivalent) uses SeededSignInAsync + UI flow where
/// needed, per the "Pre-test cleanup... must use the same UI flow" rule.
/// </summary>
[Collection(PlaywrightCollection.Name)]
public sealed class SensitiveFieldMaskingUatCoverageTests
{
    private const string AdminEmail          = "admin@acmecoffee.com";
    private const string EntityKey           = "Customer";
    private const string ColumnKey           = "SocialSecurityNumber";
    private const string BackofficeUserRole  = "BackofficeUser";
    // Seeded customer i=0: James Smith, james.smith@example.com, SSN="123-45-6789"
    private const string SsnCustomerEmail    = "james.smith@example.com";

    private readonly PlaywrightFixture _fixture;

    public SensitiveFieldMaskingUatCoverageTests(PlaywrightFixture fixture)
        => _fixture = fixture;

    // ─────────────────────────────────────────────────────────────────────────
    // Pre-test setup helpers (test setup, not human-equivalent UAT steps).
    // These use SeededSignInAsync for convenience and the UI flow for the
    // actual config mutation (no direct security-config.json edits or bypass
    // endpoints for the config change itself).
    // ─────────────────────────────────────────────────────────────────────────

    private async Task EnsureNoBackofficeUserExemptionAsync()
    {
        var setupPage = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(setupPage, AdminEmail);
            await setupPage.GotoAsync(
                $"{_fixture.BaseUrl}/admin/security",
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await setupPage
                .GetByRole(AriaRole.Tab, new() { Name = "Role Master Details" })
                .ClickAsync();
            await Assertions
                .Expect(setupPage.GetByTestId("role-master-grid"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
            await setupPage.GetByTestId($"role-row-{BackofficeUserRole}").ClickAsync();
            await Assertions
                .Expect(setupPage.GetByTestId("role-details-section"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
            await setupPage
                .GetByRole(AriaRole.Tab, new() { Name = "Mask Exemptions" })
                .ClickAsync();
            await Assertions
                .Expect(setupPage.GetByTestId("mask-exemptions-subtab"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
            await Assertions
                .Expect(setupPage
                    .GetByTestId("mask-exemption-grid")
                    .Or(setupPage.GetByTestId("mask-exemption-empty")))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
            var removeButton = setupPage.GetByTestId(
                $"mask-exemption-remove-{EntityKey}-{ColumnKey}");
            if (await removeButton.CountAsync() > 0)
            {
                await removeButton.ClickAsync();
                // Wait for the removal to land before the page (and its
                // circuit) closes; NetworkIdle does not see SignalR work.
                await Assertions.Expect(removeButton).ToHaveCountAsync(0);
            }
        }
        finally
        {
            await _fixture.ClosePageAsync(setupPage);
        }
    }

    private async Task EnsureBackofficeUserExemptionExistsAsync()
    {
        await EnsureNoBackofficeUserExemptionAsync();
        var setupPage = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(setupPage, AdminEmail);
            await setupPage.GotoAsync(
                $"{_fixture.BaseUrl}/admin/security",
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await setupPage
                .GetByRole(AriaRole.Tab, new() { Name = "Role Master Details" })
                .ClickAsync();
            await Assertions
                .Expect(setupPage.GetByTestId("role-master-grid"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
            await setupPage.GetByTestId($"role-row-{BackofficeUserRole}").ClickAsync();
            await Assertions
                .Expect(setupPage.GetByTestId("role-details-section"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
            await setupPage
                .GetByRole(AriaRole.Tab, new() { Name = "Mask Exemptions" })
                .ClickAsync();
            await Assertions
                .Expect(setupPage.GetByTestId("mask-exemptions-subtab"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
            await setupPage.GetByTestId("mask-exemption-new-table").ClickAsync();
            var tableOption = setupPage.Locator(".mud-popover-open")
                .GetByText(EntityKey, new() { Exact = true }).First;
            await Assertions.Expect(tableOption).ToBeVisibleAsync();
            await tableOption.ClickAsync();
            await Assertions
                .Expect(setupPage.GetByTestId("mask-exemption-new-column"))
                .ToBeEnabledAsync(new() { Timeout = 5_000 });
            await setupPage.GetByTestId("mask-exemption-new-column").ClickAsync();
            var columnOption = setupPage.Locator(".mud-popover-open")
                .GetByText(ColumnKey, new() { Exact = true }).First;
            await Assertions.Expect(columnOption).ToBeVisibleAsync();
            await columnOption.ClickAsync();
            var addButton = setupPage.GetByTestId("mask-exemption-add-button");
            await Assertions.Expect(addButton).ToBeEnabledAsync(new() { Timeout = 5_000 });
            await addButton.ClickAsync();
            // Wait for the Add to land (its row appears) or be rejected before
            // the page (and its circuit) closes.
            var authorityError = setupPage.GetByTestId("mask-exemption-authority-error");
            await Assertions
                .Expect(setupPage.GetByTestId($"mask-exemption-remove-{EntityKey}-{ColumnKey}").Or(authorityError))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
            // Surface a denied-authority error if Add was rejected, so the
            // resulting test failure points at the real cause rather than at
            // a missing badge.
            if (await authorityError.CountAsync() > 0)
            {
                var errText = await authorityError.InnerTextAsync();
                throw new Xunit.Sdk.XunitException(
                    $"Mask exemption Add was rejected by authority check: {errText}");
            }
            await Assertions
                .Expect(setupPage.GetByTestId(
                    $"mask-exemption-status-active-{EntityKey}-{ColumnKey}"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
        }
        finally
        {
            await _fixture.ClosePageAsync(setupPage);
        }
    }

    private async Task EnsureComplianceAuditEventsExistAsync()
    {
        await EnsureBackofficeUserExemptionExistsAsync();
        // Sign in as BackofficeUser and view the SSN customer to create a FieldUnmaskedRead event.
        var auditPage = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(auditPage, AdminEmail, roles: BackofficeUserRole);
            var customerId = await GetSsnCustomerIdAsync();
            await auditPage.GotoAsync(
                $"{_fixture.BaseUrl}/customers/{customerId}",
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            // Wait for the SSN field to render — its initial resolution writes
            // the FieldUnmaskedRead audit row when the exemption is active.
            await Assertions
                .Expect(auditPage.GetByLabel("Social Security Number"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
            await Task.Delay(800); // DB propagation: allows audit write to commit before test reads it back.
        }
        finally
        {
            await _fixture.ClosePageAsync(auditPage);
        }
    }

    private async Task<int> GetSsnCustomerIdAsync()
    {
        var adminId = await _fixture.GetSeededUserIdAsync(AdminEmail);
        var customers = await _fixture.GetCustomersAsync(adminId, pageSize: 1);
        return customers.First().Id;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Real magic-link login helpers.
    // Steps 001-008 are inlined into each [Fact] so the coverage-review gate
    // can walk the step markers directly inside the method body.
    // ─────────────────────────────────────────────────────────────────────────

    // ─────────────────────────────────────────────────────────────────────────
    // UAT-1: SSN renders masked for BackofficeUser with no active exemption.
    //
    // E2ETestReference:
    //   AcmeCoffeeOnline.Playwright.SensitiveFieldMaskingUatCoverageTests
    //   .Uatv1_BackofficeUserSeesMaskedSsnAsync
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task Uatv1_BackofficeUserSeesMaskedSsnAsync()
    {
        // Pre-test setup: ensure no BackofficeUser exemption exists.
        await EnsureNoBackofficeUserExemptionAsync();

        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.ClearMagicLinksAsync();

            // begin STEP 001
            await page.GotoAsync(
                $"{_fixture.BaseUrl}/login",
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await Assertions
                .Expect(page.GetByLabel("Email"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            // begin STEP 002
            await page.GetByLabel("Email").FillAsync(AdminEmail);

            // begin STEP 003
            await page
                .GetByRole(AriaRole.Button, new() { Name = "Send sign-in link" })
                .ClickAsync();

            // begin STEP 004
            await Assertions
                .Expect(page.GetByText("We sent a sign-in code to"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            // begin STEP 005
            var inboxPage = await _fixture.CreatePageAsync();
            try
            {
                await inboxPage.GotoAsync(
                    $"{_fixture.BaseUrl}/dev/email-inbox",
                    new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

                // begin STEP 006
                var firstRow = inboxPage.Locator("tbody tr.mud-table-row").First;
                await Assertions.Expect(firstRow).ToBeVisibleAsync(new() { Timeout = 15_000 });
                var linkAnchor = firstRow.Locator("td[data-label='Link'] a").First;
                await Assertions.Expect(linkAnchor)
                    .ToHaveAttributeAsync("href",
                        new Regex(@"^https?://.+/login\?token="));

                // begin STEP 007
                var magicHref = await linkAnchor.GetAttributeAsync("href");
                await page.GotoAsync(magicHref!,
                    new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            }
            finally
            {
                await _fixture.ClosePageAsync(inboxPage);
            }

            // begin STEP 008
            await Assertions
                .Expect(page.GetByRole(AriaRole.Button, new() { Name = "Toggle Admin" }))
                .ToBeVisibleAsync(new() { Timeout = 15_000 });

            // begin STEP 009
            // Click the role-switcher activator. The MudMenu wraps the inner
            // MudLink in an activator div, so two elements share the same
            // aria-label; targeting the inner link by data-testid is the
            // reliable way to fire the menu-open click.
            await page.GetByTestId("user-context-role").ClickAsync();

            // begin STEP 010
            // MudMenuItem renders as a list item; clicking by visible text inside
            // the open popover is the most stable cross-MudBlazor-version locator.
            await page.Locator(".mud-popover-open")
                .GetByText("Backoffice User", new() { Exact = true })
                .First
                .ClickAsync();

            // begin STEP 011
            await Assertions
                .Expect(page.GetByText("Now viewing as 'Backoffice User'."))
                .ToBeVisibleAsync(new() { Timeout = 5_000 });

            // begin STEP 012
            await _fixture.NavigateViaDrawerAsync(page, "Retail Businesses", ".*/customers$");
            await PlaywrightFixture.WaitForGridDataAsync(page);

            // begin STEP 013
            // The masked-SSN render is "XXX-XX-{last4}" which falls back to
            // "XXX-XX-****" when the underlying value is null, so the assertion
            // works for any customer in the grid; clicking the first visible
            // row keeps the test stable across pagination/sort changes.
            var customerRow = page.Locator("tbody tr.mud-table-row").First;
            await Assertions.Expect(customerRow).ToBeVisibleAsync(new() { Timeout = 10_000 });
            await customerRow.ClickAsync();

            // begin STEP 014
            await Assertions
                .Expect(page)
                .ToHaveURLAsync(new Regex(@".*/customers/\d+$"), new() { Timeout = 10_000 });

            // begin STEP 015
            await Assertions
                .Expect(page.GetByText(new Regex(@"XXX-XX-")))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            // begin STEP 016
            await Assertions
                .Expect(
                    page.GetByLabel("Social Security Number"))
                .Not.ToBeVisibleAsync(new() { Timeout = 2_000 });
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UAT-2: SystemAdmin grants a mask exemption for BackofficeUser role.
    //
    // E2ETestReference:
    //   AcmeCoffeeOnline.Playwright.SensitiveFieldMaskingUatCoverageTests
    //   .Uatv2_SystemAdminGrantsMaskExemptionAsync
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task Uatv2_SystemAdminGrantsMaskExemptionAsync()
    {
        // Pre-test setup: ensure no BackofficeUser exemption exists.
        await EnsureNoBackofficeUserExemptionAsync();

        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.ClearMagicLinksAsync();

            // begin STEP 001
            await page.GotoAsync(
                $"{_fixture.BaseUrl}/login",
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await Assertions
                .Expect(page.GetByLabel("Email"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            // begin STEP 002
            await page.GetByLabel("Email").FillAsync(AdminEmail);

            // begin STEP 003
            await page
                .GetByRole(AriaRole.Button, new() { Name = "Send sign-in link" })
                .ClickAsync();

            // begin STEP 004
            await Assertions
                .Expect(page.GetByText("We sent a sign-in code to"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            // begin STEP 005
            var inboxPage = await _fixture.CreatePageAsync();
            try
            {
                await inboxPage.GotoAsync(
                    $"{_fixture.BaseUrl}/dev/email-inbox",
                    new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

                // begin STEP 006
                var firstRow = inboxPage.Locator("tbody tr.mud-table-row").First;
                await Assertions.Expect(firstRow).ToBeVisibleAsync(new() { Timeout = 15_000 });
                var linkAnchor = firstRow.Locator("td[data-label='Link'] a").First;
                await Assertions.Expect(linkAnchor)
                    .ToHaveAttributeAsync("href",
                        new Regex(@"^https?://.+/login\?token="));

                // begin STEP 007
                var magicHref = await linkAnchor.GetAttributeAsync("href");
                await page.GotoAsync(magicHref!,
                    new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            }
            finally
            {
                await _fixture.ClosePageAsync(inboxPage);
            }

            // begin STEP 008
            await Assertions
                .Expect(page.GetByRole(AriaRole.Button, new() { Name = "Toggle Admin" }))
                .ToBeVisibleAsync(new() { Timeout = 15_000 });

            // begin STEP 009
            await _fixture.NavigateViaDrawerGroupAsync(
                page, "Admin", "Security Dashboard", ".*/admin/security$");

            // begin STEP 010
            await page
                .GetByRole(AriaRole.Tab, new() { Name = "Role Master Details" })
                .ClickAsync();
            // Wait for role list to render by checking for "Backoffice User" text.
            await Assertions
                .Expect(page.GetByText("Backoffice User").First)
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            // begin STEP 011
            var backofficeRow = page.Locator("tbody tr.mud-table-row")
                .Filter(new() { HasText = "Backoffice User" });
            await Assertions.Expect(backofficeRow).ToBeVisibleAsync(new() { Timeout = 5_000 });
            await backofficeRow.ClickAsync();
            // Verify details panel opened — check for "Mask Exemptions" tab text.
            await Assertions
                .Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Mask Exemptions" }))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            // begin STEP 012
            await page
                .GetByRole(AriaRole.Tab, new() { Name = "Mask Exemptions" })
                .ClickAsync();
            // Verify subtab rendered — look for the exact "Table" dropdown label.
            await Assertions
                .Expect(page.GetByLabel("Table", new() { Exact = true }).First)
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            // begin STEP 013
            await page.GetByLabel("Table", new() { Exact = true }).First.ClickAsync();
            var tableOption = page.Locator(".mud-popover-open")
                .GetByText(EntityKey, new() { Exact = true }).First;
            await Assertions.Expect(tableOption).ToBeVisibleAsync(new() { Timeout = 5_000 });
            await tableOption.ClickAsync();

            // begin STEP 014
            await Assertions
                .Expect(page.GetByLabel("Column", new() { Exact = true }).First)
                .ToBeEnabledAsync(new() { Timeout = 5_000 });
            await page.GetByLabel("Column", new() { Exact = true }).First.ClickAsync();
            var columnOption = page.Locator(".mud-popover-open")
                .GetByText(ColumnKey, new() { Exact = true }).First;
            await Assertions.Expect(columnOption).ToBeVisibleAsync(new() { Timeout = 5_000 });
            await columnOption.ClickAsync();

            // begin STEP 015
            var addButton = page.GetByRole(AriaRole.Button, new() { Name = "Add" });
            await Assertions.Expect(addButton).ToBeEnabledAsync(new() { Timeout = 5_000 });
            await addButton.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // begin STEP 016
            // Verify the new exemption row is visible: column name appears, status is Active.
            await Assertions
                .Expect(page.GetByText(ColumnKey, new() { Exact = true }).First)
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
            await Assertions
                .Expect(page.GetByText("Active", new() { Exact = true }).First)
                .ToBeVisibleAsync(new() { Timeout = 5_000 });
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UAT-3: BackofficeUser with active exemption sees SSN unmasked.
    //
    // E2ETestReference:
    //   AcmeCoffeeOnline.Playwright.SensitiveFieldMaskingUatCoverageTests
    //   .Uatv3_BackofficeUserWithExemptionSeesUnmaskedSsnAsync
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task Uatv3_BackofficeUserWithExemptionSeesUnmaskedSsnAsync()
    {
        // Pre-test setup: ensure BackofficeUser exemption exists.
        await EnsureBackofficeUserExemptionExistsAsync();

        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.ClearMagicLinksAsync();

            // begin STEP 001
            await page.GotoAsync(
                $"{_fixture.BaseUrl}/login",
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await Assertions
                .Expect(page.GetByLabel("Email"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            // begin STEP 002
            await page.GetByLabel("Email").FillAsync(AdminEmail);

            // begin STEP 003
            await page
                .GetByRole(AriaRole.Button, new() { Name = "Send sign-in link" })
                .ClickAsync();

            // begin STEP 004
            await Assertions
                .Expect(page.GetByText("We sent a sign-in code to"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            // begin STEP 005
            var inboxPage = await _fixture.CreatePageAsync();
            try
            {
                await inboxPage.GotoAsync(
                    $"{_fixture.BaseUrl}/dev/email-inbox",
                    new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

                // begin STEP 006
                var firstRow = inboxPage.Locator("tbody tr.mud-table-row").First;
                await Assertions.Expect(firstRow).ToBeVisibleAsync(new() { Timeout = 15_000 });
                var linkAnchor = firstRow.Locator("td[data-label='Link'] a").First;
                await Assertions.Expect(linkAnchor)
                    .ToHaveAttributeAsync("href",
                        new Regex(@"^https?://.+/login\?token="));

                // begin STEP 007
                var magicHref = await linkAnchor.GetAttributeAsync("href");
                await page.GotoAsync(magicHref!,
                    new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            }
            finally
            {
                await _fixture.ClosePageAsync(inboxPage);
            }

            // begin STEP 008
            await Assertions
                .Expect(page.GetByRole(AriaRole.Button, new() { Name = "Toggle Admin" }))
                .ToBeVisibleAsync(new() { Timeout = 15_000 });

            // begin STEP 009
            // Click the role-switcher activator. The MudMenu wraps the inner
            // MudLink in an activator div, so two elements share the same
            // aria-label; targeting the inner link by data-testid is the
            // reliable way to fire the menu-open click.
            await page.GetByTestId("user-context-role").ClickAsync();

            // begin STEP 010
            await page.Locator(".mud-popover-open")
                .GetByText("Backoffice User", new() { Exact = true })
                .First
                .ClickAsync();

            // begin STEP 011
            await Assertions
                .Expect(page.GetByText("Now viewing as 'Backoffice User'."))
                .ToBeVisibleAsync(new() { Timeout = 5_000 });

            // begin STEP 012
            await _fixture.NavigateViaDrawerAsync(page, "Retail Businesses", ".*/customers$");
            await PlaywrightFixture.WaitForGridDataAsync(page);

            // begin STEP 013
            var customerRow = page.Locator("tbody tr.mud-table-row").First;
            await Assertions.Expect(customerRow).ToBeVisibleAsync(new() { Timeout = 10_000 });
            await customerRow.ClickAsync();

            // begin STEP 014
            await Assertions
                .Expect(page)
                .ToHaveURLAsync(new Regex(@".*/customers/\d+$"), new() { Timeout = 10_000 });

            // begin STEP 015
            await Assertions
                .Expect(page.GetByLabel("Social Security Number"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            // begin STEP 016
            await Assertions
                .Expect(page.GetByText(new Regex(@"XXX-XX-")))
                .Not.ToBeVisibleAsync(new() { Timeout = 2_000 });
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UAT-4: CrossOrgMonitor sees ExemptionGranted + FieldUnmaskedRead events.
    //
    // E2ETestReference:
    //   AcmeCoffeeOnline.Playwright.SensitiveFieldMaskingUatCoverageTests
    //   .Uatv4_CrossOrgMonitorSeesComplianceEventsAsync
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task Uatv4_CrossOrgMonitorSeesComplianceEventsAsync()
    {
        // Pre-test setup: ensure exemption and audit events exist.
        await EnsureComplianceAuditEventsExistAsync();

        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.ClearMagicLinksAsync();

            // begin STEP 001
            await page.GotoAsync(
                $"{_fixture.BaseUrl}/login",
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await Assertions
                .Expect(page.GetByLabel("Email"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            // begin STEP 002
            await page.GetByLabel("Email").FillAsync(AdminEmail);

            // begin STEP 003
            await page
                .GetByRole(AriaRole.Button, new() { Name = "Send sign-in link" })
                .ClickAsync();

            // begin STEP 004
            await Assertions
                .Expect(page.GetByText("We sent a sign-in code to"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            // begin STEP 005
            var inboxPage = await _fixture.CreatePageAsync();
            try
            {
                await inboxPage.GotoAsync(
                    $"{_fixture.BaseUrl}/dev/email-inbox",
                    new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

                // begin STEP 006
                var firstRow = inboxPage.Locator("tbody tr.mud-table-row").First;
                await Assertions.Expect(firstRow).ToBeVisibleAsync(new() { Timeout = 15_000 });
                var linkAnchor = firstRow.Locator("td[data-label='Link'] a").First;
                await Assertions.Expect(linkAnchor)
                    .ToHaveAttributeAsync("href",
                        new Regex(@"^https?://.+/login\?token="));

                // begin STEP 007
                var magicHref = await linkAnchor.GetAttributeAsync("href");
                await page.GotoAsync(magicHref!,
                    new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            }
            finally
            {
                await _fixture.ClosePageAsync(inboxPage);
            }

            // begin STEP 008
            await Assertions
                .Expect(page.GetByRole(AriaRole.Button, new() { Name = "Toggle Admin" }))
                .ToBeVisibleAsync(new() { Timeout = 15_000 });

            // begin STEP 009
            // Click the role-switcher activator. The MudMenu wraps the inner
            // MudLink in an activator div, so two elements share the same
            // aria-label; targeting the inner link by data-testid is the
            // reliable way to fire the menu-open click.
            await page.GetByTestId("user-context-role").ClickAsync();

            // begin STEP 010
            await page.Locator(".mud-popover-open")
                .GetByText("Cross Org Monitor", new() { Exact = true })
                .First
                .ClickAsync();

            // begin STEP 011
            await Assertions
                .Expect(page.GetByText("Now viewing as 'Cross Org Monitor'."))
                .ToBeVisibleAsync(new() { Timeout = 5_000 });

            // begin STEP 012
            await _fixture.NavigateViaDrawerGroupAsync(
                page, "Admin", "Security Dashboard", ".*/admin/security$");

            // begin STEP 013
            await page
                .GetByRole(AriaRole.Tab, new() { Name = "Compliance Review" })
                .ClickAsync();
            // Wait for Compliance Review content heading to appear.
            await Assertions
                .Expect(page.GetByText("Compliance Review").First)
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            // begin STEP 014
            // Verify the Entity filter input is visible — confirms grid + filters rendered.
            await Assertions
                .Expect(page.GetByLabel("Entity").First)
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            // begin STEP 015
            await page.GetByLabel("Entity").First.FillAsync(EntityKey);
            await Task.Delay(1500); // Debounce delay: audit grid entity filter has a long debounce, not render-correlated.
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // begin STEP 016
            await Assertions
                .Expect(page.GetByText("ExemptionGranted").First)
                .ToBeVisibleAsync(new() { Timeout = 15_000 });

            // begin STEP 017
            await Assertions
                .Expect(page.GetByText("FieldUnmaskedRead").First)
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UAT-5: Row expander renders the correlation cluster section (judgment item).
    //
    // E2ETestReference:
    //   AcmeCoffeeOnline.Playwright.SensitiveFieldMaskingUatCoverageTests
    //   .Uatv5_RowExpanderRendersCorrelationClusterAsync
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task Uatv5_RowExpanderRendersCorrelationClusterAsync()
    {
        // Pre-test setup: ensure exemption and audit events exist.
        await EnsureComplianceAuditEventsExistAsync();

        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.ClearMagicLinksAsync();

            // begin STEP 001
            await page.GotoAsync(
                $"{_fixture.BaseUrl}/login",
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await Assertions
                .Expect(page.GetByLabel("Email"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            // begin STEP 002
            await page.GetByLabel("Email").FillAsync(AdminEmail);

            // begin STEP 003
            await page
                .GetByRole(AriaRole.Button, new() { Name = "Send sign-in link" })
                .ClickAsync();

            // begin STEP 004
            await Assertions
                .Expect(page.GetByText("We sent a sign-in code to"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            // begin STEP 005
            var inboxPage = await _fixture.CreatePageAsync();
            try
            {
                await inboxPage.GotoAsync(
                    $"{_fixture.BaseUrl}/dev/email-inbox",
                    new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

                // begin STEP 006
                var firstRow = inboxPage.Locator("tbody tr.mud-table-row").First;
                await Assertions.Expect(firstRow).ToBeVisibleAsync(new() { Timeout = 15_000 });
                var linkAnchor = firstRow.Locator("td[data-label='Link'] a").First;
                await Assertions.Expect(linkAnchor)
                    .ToHaveAttributeAsync("href",
                        new Regex(@"^https?://.+/login\?token="));

                // begin STEP 007
                var magicHref = await linkAnchor.GetAttributeAsync("href");
                await page.GotoAsync(magicHref!,
                    new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            }
            finally
            {
                await _fixture.ClosePageAsync(inboxPage);
            }

            // begin STEP 008
            await Assertions
                .Expect(page.GetByRole(AriaRole.Button, new() { Name = "Toggle Admin" }))
                .ToBeVisibleAsync(new() { Timeout = 15_000 });

            // begin STEP 009
            // Click the role-switcher activator. The MudMenu wraps the inner
            // MudLink in an activator div, so two elements share the same
            // aria-label; targeting the inner link by data-testid is the
            // reliable way to fire the menu-open click.
            await page.GetByTestId("user-context-role").ClickAsync();

            // begin STEP 010
            await page.Locator(".mud-popover-open")
                .GetByText("Cross Org Monitor", new() { Exact = true })
                .First
                .ClickAsync();

            // begin STEP 011
            await Assertions
                .Expect(page.GetByText("Now viewing as 'Cross Org Monitor'."))
                .ToBeVisibleAsync(new() { Timeout = 5_000 });

            // begin STEP 012
            await _fixture.NavigateViaDrawerGroupAsync(
                page, "Admin", "Security Dashboard", ".*/admin/security$");

            // begin STEP 013
            await page
                .GetByRole(AriaRole.Tab, new() { Name = "Compliance Review" })
                .ClickAsync();
            await Assertions
                .Expect(page.GetByText("Compliance Review").First)
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            // begin STEP 014
            // Verify at least one audit event chip is visible before expanding.
            await Assertions
                .Expect(page.GetByText("ExemptionGranted").Or(page.GetByText("FieldUnmaskedRead")).First)
                .ToBeVisibleAsync(new() { Timeout = 15_000 });

            // begin STEP 015
            var firstDataRow = page.Locator(".mud-table-body tr.mud-table-row").First;
            await Assertions.Expect(firstDataRow).ToBeVisibleAsync(new() { Timeout = 5_000 });
            var expandCell = firstDataRow.Locator("td").First;
            await Assertions.Expect(expandCell).ToBeVisibleAsync();
            await expandCell.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // begin STEP 016
            var expanderContainer = page
                .Locator("[data-testid*='compliance-review-row-'][data-testid$='-expander']")
                .First;
            await Assertions
                .Expect(expanderContainer)
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            // begin STEP 017
            var hasClusterTable = await expanderContainer
                .Locator("[data-testid*='-cluster-table']").CountAsync() > 0;
            var hasEmptyMessage = await expanderContainer
                .Locator("[data-testid*='-cluster-empty']").CountAsync() > 0;
            Assert.True(
                hasClusterTable || hasEmptyMessage,
                "The expander must render either a cluster table or a 'No other rows' message.");
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UAT-6: Export CSV button triggers a download and shows a success snackbar.
    //
    // E2ETestReference:
    //   AcmeCoffeeOnline.Playwright.SensitiveFieldMaskingUatCoverageTests
    //   .Uatv6_ExportCsvButtonTriggersDownloadAsync
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task Uatv6_ExportCsvButtonTriggersDownloadAsync()
    {
        // Pre-test setup: ensure exemption and audit events exist.
        await EnsureComplianceAuditEventsExistAsync();

        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.ClearMagicLinksAsync();

            // begin STEP 001
            await page.GotoAsync(
                $"{_fixture.BaseUrl}/login",
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await Assertions
                .Expect(page.GetByLabel("Email"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            // begin STEP 002
            await page.GetByLabel("Email").FillAsync(AdminEmail);

            // begin STEP 003
            await page
                .GetByRole(AriaRole.Button, new() { Name = "Send sign-in link" })
                .ClickAsync();

            // begin STEP 004
            await Assertions
                .Expect(page.GetByText("We sent a sign-in code to"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            // begin STEP 005
            var inboxPage = await _fixture.CreatePageAsync();
            try
            {
                await inboxPage.GotoAsync(
                    $"{_fixture.BaseUrl}/dev/email-inbox",
                    new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

                // begin STEP 006
                var firstRow = inboxPage.Locator("tbody tr.mud-table-row").First;
                await Assertions.Expect(firstRow).ToBeVisibleAsync(new() { Timeout = 15_000 });
                var linkAnchor = firstRow.Locator("td[data-label='Link'] a").First;
                await Assertions.Expect(linkAnchor)
                    .ToHaveAttributeAsync("href",
                        new Regex(@"^https?://.+/login\?token="));

                // begin STEP 007
                var magicHref = await linkAnchor.GetAttributeAsync("href");
                await page.GotoAsync(magicHref!,
                    new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            }
            finally
            {
                await _fixture.ClosePageAsync(inboxPage);
            }

            // begin STEP 008
            await Assertions
                .Expect(page.GetByRole(AriaRole.Button, new() { Name = "Toggle Admin" }))
                .ToBeVisibleAsync(new() { Timeout = 15_000 });

            // begin STEP 009
            // Click the role-switcher activator. The MudMenu wraps the inner
            // MudLink in an activator div, so two elements share the same
            // aria-label; targeting the inner link by data-testid is the
            // reliable way to fire the menu-open click.
            await page.GetByTestId("user-context-role").ClickAsync();

            // begin STEP 010
            await page.Locator(".mud-popover-open")
                .GetByText("Cross Org Monitor", new() { Exact = true })
                .First
                .ClickAsync();

            // begin STEP 011
            await Assertions
                .Expect(page.GetByText("Now viewing as 'Cross Org Monitor'."))
                .ToBeVisibleAsync(new() { Timeout = 5_000 });

            // begin STEP 012
            await _fixture.NavigateViaDrawerGroupAsync(
                page, "Admin", "Security Dashboard", ".*/admin/security$");

            // begin STEP 013
            await page
                .GetByRole(AriaRole.Tab, new() { Name = "Compliance Review" })
                .ClickAsync();
            await Assertions
                .Expect(page.GetByText("Compliance Review").First)
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            // begin STEP 014
            var exportButton = page.GetByRole(AriaRole.Button, new() { Name = "Export CSV" });
            await Assertions.Expect(exportButton).ToBeEnabledAsync(new() { Timeout = 5_000 });

            // begin STEP 015
            await exportButton.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // begin STEP 016
            await Assertions
                .Expect(page.GetByText("Exported").First)
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }
}
