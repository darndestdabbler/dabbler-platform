using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;

namespace AcmeCoffeeOnline.Playwright;

/// <summary>
/// Browser-level smoke coverage for the Compliance Review tab introduced in
/// Session 4 of the <c>sensitive-field-masking-audit</c> session set. Drives
/// the same path a CrossOrgMonitor / SystemAdmin would: sign in, navigate
/// to the Security Dashboard via the drawer, click the Compliance Review tab,
/// confirm the grid scroll region renders.
/// </summary>
[Collection(PlaywrightCollection.Name)]
public sealed class ComplianceReviewSmokeTests
{
    private const string AdminEmail = "admin@acmecoffee.com";

    private readonly PlaywrightFixture _fixture;

    public ComplianceReviewSmokeTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SystemAdmin_ReachesComplianceReviewViaDrawer_GridRendersAsync()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, AdminEmail, roles: "SystemAdmin");

            await _fixture.NavigateViaDrawerGroupAsync(
                page, "Admin", "Security Dashboard", ".*/admin/security$");

            await page.GetByRole(AriaRole.Tab, new() { Name = "Compliance Review" })
                .ClickAsync();

            await Assertions
                .Expect(page.GetByTestId("compliance-review-tab"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            await Assertions
                .Expect(page.GetByTestId("compliance-review-grid-scroll-region"))
                .ToBeVisibleAsync();

            await Assertions
                .Expect(page.GetByTestId("compliance-review-export-button"))
                .ToBeVisibleAsync();
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task CrossOrgMonitor_SeesComplianceReviewTabAsync()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, AdminEmail, roles: "CrossOrgMonitor");
            await page.GotoAsync($"{_fixture.BaseUrl}/admin/security", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
            });

            await Assertions
                .Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Compliance Review" }))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }
}
