using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;

namespace AcmeCoffeeOnline.Playwright;

/// <summary>
/// 1:1 Playwright coverage for the
/// <c>sensitive-field-masking-audit-uat-checklist.json</c> UAT checklist.
/// All six UAT items are exercised in a single [Fact] to guarantee sequential
/// execution and prevent ordering-related state issues.
///
/// Six items (covered in sequence):
///   UAT-1  SSN renders masked for a user-type role with no exemption
///   UAT-2  SystemAdmin grants a mask exemption for the BackofficeUser role
///   UAT-3  BackofficeUser with active exemption sees SSN unmasked
///   UAT-4  CrossOrgMonitor sees ExemptionGranted + FieldUnmaskedRead in Compliance Review
///   UAT-5  Row expander renders the correlation cluster section
///   UAT-6  Export CSV button triggers a download
///
/// Design notes:
/// - Baked-in roles (SystemAdmin, CrossOrgUserAdmin, CrossOrgMonitor) bypass DefaultMasks;
///   UAT-1/3 use BackofficeUser (a user-type role that CAN access /customers and IS
///   subject to DefaultMask rules).
/// - Only SystemAdmin sees the Role Master Details tab (where mask exemptions are managed);
///   CrossOrgUserAdmin does NOT — confirmed by SecurityDashboardFullSweepTests.
/// - The E2ETestReference for all checklist items points to
///   SensitiveFieldMasking_FullUatScenarioAsync (single method, all items).
/// - Cleanup at the end removes the BackofficeUser exemption so the suite is idempotent.
/// </summary>
[Collection(PlaywrightCollection.Name)]
public sealed class SensitiveFieldMaskingUatCoverageTests
{
    private const string AdminEmail        = "admin@acmecoffee.com";

    // Shared test-data values matching the checklist parameters.
    private const string EntityKey         = "Customer";
    private const string ColumnKey         = "SocialSecurityNumber";
    // BackofficeUser: user-type role with /customers access, subject to DefaultMasks.
    private const string BackofficeUserRole = "BackofficeUser";

    private readonly PlaywrightFixture _fixture;

    public SensitiveFieldMaskingUatCoverageTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Directly clears any BackofficeUser mask exemption from the web app's
    /// security-config.json so each test run starts from a known clean state.
    /// More reliable than UI-based cleanup; the web server reloads the config on
    /// the next SecurityConfigurationManager.EnsureLoadedAsync() call.
    /// </summary>
    private static void ClearBackofficeUserExemptionFromFile()
    {
        // Locate the web project's security-config.json relative to the repo root.
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !dir.EnumerateFiles("Dabbler.Platform.sln").Any())
            dir = dir.Parent;

        if (dir is null) return;
        var configPath = Path.Combine(
            dir.FullName,
            "src", "Apps", "AcmeCoffeeOnline.Web", "security-config.json");
        if (!File.Exists(configPath)) return;

        var json = File.ReadAllText(configPath);
        var doc = System.Text.Json.JsonDocument.Parse(json);
        using var ms = new System.IO.MemoryStream();
        using var writer = new System.Text.Json.Utf8JsonWriter(
            ms, new System.Text.Json.JsonWriterOptions { Indented = true });

        writer.WriteStartObject();
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            if (prop.Name == "MaskExemptions")
            {
                writer.WriteStartArray("MaskExemptions");
                foreach (var ex in prop.Value.EnumerateArray())
                {
                    if (ex.TryGetProperty("TargetRoleName", out var tn)
                        && string.Equals(tn.GetString(), BackofficeUserRole,
                            StringComparison.OrdinalIgnoreCase))
                        continue; // skip BackofficeUser exemption
                    ex.WriteTo(writer);
                }
                writer.WriteEndArray();
            }
            else
            {
                prop.WriteTo(writer);
            }
        }
        writer.WriteEndObject();
        writer.Flush();

        File.WriteAllBytes(configPath, ms.ToArray());
    }

    /// <summary>Returns the ID of the first customer — the one seeded with SSN "123-45-6789".</summary>
    private async Task<int> GetSsnCustomerIdAsync()
    {
        var adminId = await _fixture.GetSeededUserIdAsync(AdminEmail);
        var customers = await _fixture.GetCustomersAsync(adminId, pageSize: 1);
        return customers.First().Id;
    }

    /// <summary>
    /// Full end-to-end UAT scenario for the sensitive-field-masking-audit session set.
    /// Covers checklist items UAT-1 through UAT-6 in sequential order.
    /// E2ETestReference for all checklist Review items:
    /// AcmeCoffeeOnline.Playwright.SensitiveFieldMaskingUatCoverageTests.SensitiveFieldMasking_FullUatScenarioAsync
    /// </summary>
    [Fact]
    public async Task SensitiveFieldMasking_FullUatScenarioAsync()
    {
        var customerId = await GetSsnCustomerIdAsync();

        // ── Pre-test cleanup: remove any leftover BackofficeUser exemption from prior runs ─
        {
            var setupPage = await _fixture.CreatePageAsync();
            try
            {
                await _fixture.SeededSignInAsync(setupPage, AdminEmail, roles: "SystemAdmin");
                await setupPage.GotoAsync(
                    $"{_fixture.BaseUrl}/admin/security",
                    new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
                await setupPage
                    .GetByRole(AriaRole.Tab, new() { Name = "Role Master Details" })
                    .ClickAsync();
                await Assertions
                    .Expect(setupPage.GetByTestId("role-master-grid"))
                    .ToBeVisibleAsync(new() { Timeout = 10_000 });
                var priorRow = setupPage
                    .GetByTestId($"role-row-{BackofficeUserRole}")
                    .Locator("xpath=ancestor::tr[1]");
                await Assertions.Expect(priorRow).ToBeVisibleAsync(new() { Timeout = 10_000 });
                await priorRow.ClickAsync();
                await setupPage
                    .GetByRole(AriaRole.Tab, new() { Name = "Mask Exemptions" })
                    .ClickAsync();
                await Assertions
                    .Expect(setupPage.GetByTestId("mask-exemptions-subtab"))
                    .ToBeVisibleAsync(new() { Timeout = 10_000 });
                // Wait for EITHER the exemption grid (if exemptions exist) or the "no exemptions"
                // empty message to render. Blazor Server renders asynchronously via SignalR so
                // a simple delay is insufficient; polling on the expected elements is required.
                await Assertions
                    .Expect(
                        setupPage.GetByTestId("mask-exemption-grid")
                            .Or(setupPage.GetByTestId("mask-exemption-empty")))
                    .ToBeVisibleAsync(new() { Timeout = 10_000 });
                var priorRemove = setupPage.GetByTestId(
                    $"mask-exemption-remove-{EntityKey}-{ColumnKey}");
                if (await priorRemove.CountAsync() > 0)
                {
                    await priorRemove.ClickAsync();
                    await setupPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
                }
            }
            finally
            {
                await _fixture.ClosePageAsync(setupPage);
            }
        }

        // ── UAT-1: SSN renders masked for a user-type role with no exemption ──────────────
        // Step 1: Sign in as admin@acmecoffee.com with role BackofficeUser.
        // Step 2: Navigate to the customer detail page for the first customer.
        // Step 3: Verify the SSN field wrapper (data-testid="field-ssn-wrapper") is visible.
        // Step 4: Verify the masked span (data-testid="restricted-field-masked-Customer-SocialSecurityNumber")
        //         is visible and its text starts with "XXX-XX-" (the configured mask format).
        {
            var page = await _fixture.CreatePageAsync();
            try
            {
                await _fixture.SeededSignInAsync(page, AdminEmail, roles: BackofficeUserRole);
                await page.GotoAsync(
                    $"{_fixture.BaseUrl}/customers/{customerId}",
                    new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

                await Assertions
                    .Expect(page.GetByTestId("field-ssn-wrapper"))
                    .ToBeVisibleAsync(new() { Timeout = 10_000 });

                var maskedSpan = page.GetByTestId(
                    $"restricted-field-masked-{EntityKey}-{ColumnKey}");
                await Assertions.Expect(maskedSpan).ToBeVisibleAsync(new() { Timeout = 10_000 });
                await Assertions.Expect(maskedSpan).ToContainTextAsync("XXX-XX-");
            }
            finally
            {
                await _fixture.ClosePageAsync(page);
            }
        }

        // ── UAT-2: SystemAdmin grants a mask exemption for the BackofficeUser role ─────────
        // Step 1: Sign in as admin@acmecoffee.com with role SystemAdmin.
        // Step 2: Navigate to the Security Dashboard (/admin/security).
        // Step 3: Click the "Role Master Details" tab.
        // Step 4: Click the row for the "BackofficeUser" role.
        // Step 5: Click the "Mask Exemptions" sub-tab.
        // Step 6: Select "Customer" in the Table dropdown.
        // Step 7: Wait for Column dropdown to be enabled, then select "SocialSecurityNumber".
        // Step 8: Click the Add button.
        // Step 9: Verify the active-status badge is visible.
        {
            var page = await _fixture.CreatePageAsync();
            try
            {
                await _fixture.SeededSignInAsync(page, AdminEmail, roles: "SystemAdmin");
                await page.GotoAsync(
                    $"{_fixture.BaseUrl}/admin/security",
                    new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

                await page.GetByRole(AriaRole.Tab, new() { Name = "Role Master Details" }).ClickAsync();
                await Assertions
                    .Expect(page.GetByTestId("role-master-grid"))
                    .ToBeVisibleAsync(new() { Timeout = 10_000 });

                var roleRow = page
                    .GetByTestId($"role-row-{BackofficeUserRole}")
                    .Locator("xpath=ancestor::tr[1]");
                await Assertions.Expect(roleRow).ToBeVisibleAsync(new() { Timeout = 10_000 });
                await roleRow.ClickAsync();
                await Assertions
                    .Expect(page.GetByTestId("role-details-section"))
                    .ToBeVisibleAsync(new() { Timeout = 10_000 });

                await page.GetByRole(AriaRole.Tab, new() { Name = "Mask Exemptions" }).ClickAsync();
                await Assertions
                    .Expect(page.GetByTestId("mask-exemptions-subtab"))
                    .ToBeVisibleAsync(new() { Timeout = 10_000 });

                await page.GetByTestId("mask-exemption-new-table").ClickAsync();
                var tableOption = page.Locator(".mud-popover-open")
                    .GetByText(EntityKey, new() { Exact = true }).First;
                await Assertions.Expect(tableOption).ToBeVisibleAsync();
                await tableOption.ClickAsync();
                await Task.Delay(300);

                await Assertions
                    .Expect(page.GetByTestId("mask-exemption-new-column"))
                    .ToBeEnabledAsync(new() { Timeout = 5_000 });
                await page.GetByTestId("mask-exemption-new-column").ClickAsync();
                var columnOption = page.Locator(".mud-popover-open")
                    .GetByText(ColumnKey, new() { Exact = true }).First;
                await Assertions.Expect(columnOption).ToBeVisibleAsync();
                await columnOption.ClickAsync();
                await Task.Delay(300);

                var addButton = page.GetByTestId("mask-exemption-add-button");
                await Assertions.Expect(addButton).ToBeEnabledAsync(new() { Timeout = 5_000 });
                await addButton.ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                await Assertions
                    .Expect(page.GetByTestId(
                        $"mask-exemption-status-active-{EntityKey}-{ColumnKey}"))
                    .ToBeVisibleAsync(new() { Timeout = 10_000 });
            }
            finally
            {
                await _fixture.ClosePageAsync(page);
            }
        }

        // ── UAT-3: BackofficeUser with active exemption sees SSN unmasked ─────────────────
        // Step 1: Sign in as admin@acmecoffee.com with role BackofficeUser.
        // Step 2: Navigate to the customer detail page for the first customer.
        // Step 3: Verify the SSN field wrapper (data-testid="field-ssn-wrapper") is visible.
        // Step 4: Verify the unmasked text field (data-testid="field-ssn") is visible.
        {
            var page = await _fixture.CreatePageAsync();
            try
            {
                await _fixture.SeededSignInAsync(page, AdminEmail, roles: BackofficeUserRole);
                await page.GotoAsync(
                    $"{_fixture.BaseUrl}/customers/{customerId}",
                    new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

                await Assertions
                    .Expect(page.GetByTestId("field-ssn-wrapper"))
                    .ToBeVisibleAsync(new() { Timeout = 10_000 });

                await Assertions
                    .Expect(page.GetByTestId("field-ssn"))
                    .ToBeVisibleAsync(new() { Timeout = 10_000 });
            }
            finally
            {
                await _fixture.ClosePageAsync(page);
            }
        }

        // ── UAT-4: CrossOrgMonitor sees ExemptionGranted + FieldUnmaskedRead events ────────
        // Step 1: Sign in as admin@acmecoffee.com with role CrossOrgMonitor.
        // Step 2: Navigate to the Security Dashboard (/admin/security).
        // Step 3: Click the "Compliance Review" tab.
        // Step 4: Verify the compliance-review-tab container is visible.
        // Step 5: Verify the grid scroll region is visible.
        // Step 6: Type "Customer" in the Entity filter field to scope results.
        // Step 7: Verify at least one ExemptionGranted event chip is visible.
        // Step 8: Verify at least one FieldUnmaskedRead event chip is visible.
        {
            var page = await _fixture.CreatePageAsync();
            try
            {
                await _fixture.SeededSignInAsync(page, AdminEmail, roles: "CrossOrgMonitor");
                await page.GotoAsync(
                    $"{_fixture.BaseUrl}/admin/security",
                    new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

                await page.GetByRole(AriaRole.Tab, new() { Name = "Compliance Review" }).ClickAsync();
                await Assertions
                    .Expect(page.GetByTestId("compliance-review-tab"))
                    .ToBeVisibleAsync(new() { Timeout = 10_000 });
                await Assertions
                    .Expect(page.GetByTestId("compliance-review-grid-scroll-region"))
                    .ToBeVisibleAsync();

                var entityFilter = page.GetByLabel("Entity").First;
                await Assertions.Expect(entityFilter).ToBeVisibleAsync(new() { Timeout = 5_000 });
                await entityFilter.FillAsync(EntityKey);
                await Task.Delay(1500);
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                await Assertions
                    .Expect(page.Locator("[data-testid*='event-chip']")
                        .Filter(new() { HasText = "ExemptionGranted" })
                        .First)
                    .ToBeVisibleAsync(new() { Timeout = 15_000 });

                await Assertions
                    .Expect(page.Locator("[data-testid*='event-chip']")
                        .Filter(new() { HasText = "FieldUnmaskedRead" })
                        .First)
                    .ToBeVisibleAsync(new() { Timeout = 10_000 });
            }
            finally
            {
                await _fixture.ClosePageAsync(page);
            }
        }

        // ── UAT-5: Row expander renders the correlation cluster section (judgment item) ────
        // Step 1: Sign in as admin@acmecoffee.com with role CrossOrgMonitor.
        // Step 2: Navigate to /admin/security and click the "Compliance Review" tab.
        // Step 3: Wait for at least one event chip to appear in the grid.
        // Step 4: Click the expand icon button on the first data row.
        // Step 5: Verify the expander container is visible.
        // Step 6: Verify either a cluster table or "No other rows" message renders.
        {
            var page = await _fixture.CreatePageAsync();
            try
            {
                await _fixture.SeededSignInAsync(page, AdminEmail, roles: "CrossOrgMonitor");
                await page.GotoAsync(
                    $"{_fixture.BaseUrl}/admin/security",
                    new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
                await page.GetByRole(AriaRole.Tab, new() { Name = "Compliance Review" }).ClickAsync();
                await Assertions
                    .Expect(page.GetByTestId("compliance-review-tab"))
                    .ToBeVisibleAsync(new() { Timeout = 10_000 });

                await Assertions
                    .Expect(page.Locator("[data-testid*='event-chip']").First)
                    .ToBeVisibleAsync(new() { Timeout = 15_000 });

                // MudDataGrid's expand toggle is in the first cell of each data row.
                // Click the first cell (the toggle/expand cell) to open ChildRowContent.
                var firstDataRow = page.Locator(".mud-table-body tr.mud-table-row").First;
                await Assertions.Expect(firstDataRow).ToBeVisibleAsync(new() { Timeout = 5_000 });
                // Click the first table cell in the row (which is the expand-toggle cell).
                var expandCell = firstDataRow.Locator("td").First;
                await Assertions.Expect(expandCell).ToBeVisibleAsync();
                await expandCell.ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                var expanderContainer = page
                    .Locator("[data-testid*='compliance-review-row-'][data-testid$='-expander']")
                    .First;
                await Assertions
                    .Expect(expanderContainer)
                    .ToBeVisibleAsync(new() { Timeout = 10_000 });

                var hasClusterTable  = await expanderContainer
                    .Locator("[data-testid*='-cluster-table']").CountAsync() > 0;
                var hasEmptyMessage  = await expanderContainer
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

        // ── UAT-6: Export CSV button triggers a download and shows a success snackbar ──────
        // Step 1: Sign in as admin@acmecoffee.com with role CrossOrgMonitor.
        // Step 2: Navigate to /admin/security and click the "Compliance Review" tab.
        // Step 3: Verify the Export CSV button (data-testid="compliance-review-export-button") is enabled.
        // Step 4: Click the Export CSV button.
        // Step 5: Verify a snackbar containing "Exported" appears.
        //
        // Cleanup: remove the BackofficeUser exemption so subsequent runs are idempotent.
        {
            var page = await _fixture.CreatePageAsync();
            try
            {
                await _fixture.SeededSignInAsync(page, AdminEmail, roles: "CrossOrgMonitor");
                await page.GotoAsync(
                    $"{_fixture.BaseUrl}/admin/security",
                    new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
                await page.GetByRole(AriaRole.Tab, new() { Name = "Compliance Review" }).ClickAsync();
                await Assertions
                    .Expect(page.GetByTestId("compliance-review-tab"))
                    .ToBeVisibleAsync(new() { Timeout = 10_000 });

                var exportButton = page.GetByTestId("compliance-review-export-button");
                await Assertions.Expect(exportButton).ToBeEnabledAsync();
                await exportButton.ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                await Assertions
                    .Expect(page.Locator(".mud-snackbar")
                        .Filter(new() { HasText = "Exported" })
                        .First)
                    .ToBeVisibleAsync(new() { Timeout = 10_000 });
            }
            finally
            {
                await _fixture.ClosePageAsync(page);
            }
        }

        // ── Cleanup: remove BackofficeUser exemption ──────────────────────────────────────
        var cleanupPage = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(cleanupPage, AdminEmail, roles: "SystemAdmin");
            await cleanupPage.GotoAsync(
                $"{_fixture.BaseUrl}/admin/security",
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await cleanupPage
                .GetByRole(AriaRole.Tab, new() { Name = "Role Master Details" })
                .ClickAsync();
            await Assertions
                .Expect(cleanupPage.GetByTestId("role-master-grid"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            var roleRow = cleanupPage
                .GetByTestId($"role-row-{BackofficeUserRole}")
                .Locator("xpath=ancestor::tr[1]");
            if (await roleRow.CountAsync() > 0)
            {
                await roleRow.ClickAsync();
                await cleanupPage
                    .GetByRole(AriaRole.Tab, new() { Name = "Mask Exemptions" })
                    .ClickAsync();
                var removeButton = cleanupPage.GetByTestId(
                    $"mask-exemption-remove-{EntityKey}-{ColumnKey}");
                if (await removeButton.CountAsync() > 0)
                {
                    await removeButton.ClickAsync();
                    await cleanupPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
                }
            }
        }
        catch
        {
            // Best-effort cleanup.
        }
        finally
        {
            await _fixture.ClosePageAsync(cleanupPage);
        }
    }
}
