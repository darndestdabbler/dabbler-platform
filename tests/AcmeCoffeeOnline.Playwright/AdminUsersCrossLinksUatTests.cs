using System.Text.RegularExpressions;
using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace AcmeCoffeeOnline.Playwright;

/// <summary>
/// E2E coverage for <c>admin-users-cross-links</c> UAT-3 (and the supporting
/// UAT-1 / UAT-2 paths) using the SAME magic-link sign-in flow a human
/// follows in the live app — NOT <c>SeededSignInAsync</c>. This is the
/// regression-test backstop for the 2026-05-11 ClaimsPrincipal-overload
/// refactor: it would have caught the IHttpContextAccessor-is-null and
/// CrossOrgMonitor-wins-over-SystemAdmin bugs before human UAT discovered
/// them.
///
/// Pattern matches <c>SensitiveFieldMaskingUatCoverageTests</c>: real
/// <c>/login</c> → <c>/dev/email-inbox</c> → magic-link follow, then UI
/// navigation via drawer + tab clicks.
/// </summary>
[Collection(PlaywrightCollection.Name)]
public sealed class AdminUsersCrossLinksUatTests
{
    private const string AdminEmail = "admin@acmecoffee.com";

    private readonly PlaywrightFixture _fixture;
    private readonly ITestOutputHelper _output;

    public AdminUsersCrossLinksUatTests(PlaywrightFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task Uat3_SystemAdmin_OpensRoleDropdown_UserAdminExcluded_OtherRolesPresent()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await SignInAsAdminViaMagicLinkAsync(page);

            // Navigate to Security Dashboard via the UI drawer (not direct URL),
            // matching what the human does.
            await page.GetByRole(AriaRole.Button, new() { Name = "Toggle Admin" }).ClickAsync();
            await page.GetByRole(AriaRole.Link, new() { Name = "Security Dashboard" }).ClickAsync();
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(@"/admin/security"));

            // Click the USERS AND ROLE ASSIGNMENT tab.
            var usersTab = page.GetByRole(AriaRole.Tab)
                .Filter(new() { HasTextRegex = new Regex("USERS AND ROLE ASSIGNMENT", RegexOptions.IgnoreCase) })
                .First;
            await Assertions.Expect(usersTab).ToBeVisibleAsync(new() { Timeout = 10_000 });
            await usersTab.ClickAsync();

            // The user grid loads asynchronously. Wait for the user-count caption.
            var captionLocator = page.Locator("[data-testid='users-master-grid-container']")
                .GetByText(new Regex(@"\d+ of \d+ users"));
            await Assertions.Expect(captionLocator).ToBeVisibleAsync(new() { Timeout = 15_000 });
            // The caption renders "0 of 0 users" before the first load returns; wait past it.
            await Assertions.Expect(captionLocator.First).Not.ToHaveTextAsync(new Regex(@"^0 of 0 users"), new() { Timeout = 15_000 });
            var caption = await captionLocator.First.InnerTextAsync();
            _output.WriteLine($"User-count caption: {caption}");
            Assert.False(caption.StartsWith("0 of"),
                "Grid must not be empty after SystemAdmin signs in — empty grid means " +
                "GetUsersAsync's actor context resolution is broken. Caption: " + caption);

            // Click a user row. Pick a customer (jane.smith etc.) — they hold
            // user-type roles via OrganizationMembership but no admin-class roles,
            // which is the canonical case for "show 7 assignable roles."
            var firstUserRow = page.Locator("[data-testid='users-master-grid'] .mud-table-row")
                .Filter(new() { HasTextRegex = new Regex("@") })  // skip the header row
                .First;
            await firstUserRow.ClickAsync();

            // The detail panel renders below. The "Assign role" dropdown must be
            // present (NOT the no-actor-role alert, NOT the no-assignable-roles alert).
            var noActorRoleAlert = page.Locator("[data-testid='user-role-assignment-no-actor-role']");
            var noAssignableRolesAlert = page.Locator("[data-testid='user-role-assignment-no-assignable-roles']");
            var roleSelect = page.Locator("[data-testid='user-role-assignment-role-select']");

            await Assertions.Expect(roleSelect).ToBeVisibleAsync(new() { Timeout = 10_000 });
            Assert.Equal(0, await noActorRoleAlert.CountAsync());
            Assert.Equal(0, await noAssignableRolesAlert.CountAsync());

            // Open the dropdown and verify the option set.
            await roleSelect.ClickAsync();
            await page.WaitForTimeoutAsync(500); // MudSelect popover animation

            var options = page.Locator("[data-testid^='user-role-assignment-role-option-']");
            var optionCount = await options.CountAsync();
            var optionLabels = new List<string>(optionCount);
            for (var i = 0; i < optionCount; i++)
            {
                var label = await options.Nth(i).InnerTextAsync();
                optionLabels.Add(label.Trim().Replace("\n", " "));
            }

            _output.WriteLine($"Role-select options ({optionCount}):");
            foreach (var label in optionLabels)
            {
                _output.WriteLine($"  - {label}");
            }

            Assert.True(optionCount > 0,
                $"dropdown must show at least one assignable role for SystemAdmin actor. Got: {optionCount}");

            // UserAdmin must be excluded per ExcludeUserAdminAssignment=true (default).
            // Filter out CrossOrgUserAdmin (which contains "UserAdmin" as a substring).
            var hasUserAdmin = optionLabels.Any(l =>
                l.Contains("UserAdmin", StringComparison.Ordinal)
                && !l.Contains("CrossOrgUserAdmin", StringComparison.Ordinal));
            Assert.False(hasUserAdmin,
                "UserAdmin must be excluded from the dashboard role dropdown. " +
                "Options: " + string.Join(", ", optionLabels));

            // Baked-in roles SystemAdmin / CrossOrgUserAdmin must be present.
            Assert.Contains(optionLabels, l => l.Contains("SystemAdmin", StringComparison.Ordinal));
            Assert.Contains(optionLabels, l => l.Contains("CrossOrgUserAdmin", StringComparison.Ordinal));
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task Uat1_PageHeading_RenamedToManageOrganizationAdmins()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await SignInAsAdminViaMagicLinkAsync(page);

            await page.GetByRole(AriaRole.Button, new() { Name = "Toggle Admin" }).ClickAsync();
            await page.GetByRole(AriaRole.Link, new() { Name = "User Management" }).ClickAsync();
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(@"/admin/users"));

            // The h4 page heading must read "Manage Organization Admins" (not "User Administration").
            await Assertions
                .Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Manage Organization Admins" }))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            // The dashboard cross-link banner must be visible.
            await Assertions
                .Expect(page.GetByTestId("user-admin-dashboard-banner"))
                .ToBeVisibleAsync(new() { Timeout = 5_000 });

            // The banner's link must point to /admin/security.
            var bannerLink = page.GetByTestId("user-admin-dashboard-banner-link");
            await Assertions
                .Expect(bannerLink)
                .ToHaveAttributeAsync("href", new Regex(@"^/admin/security(?:\?|$)"));
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task Uat2_BannerClickThrough_LandsOnSecurityDashboard()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await SignInAsAdminViaMagicLinkAsync(page);

            await page.GetByRole(AriaRole.Button, new() { Name = "Toggle Admin" }).ClickAsync();
            await page.GetByRole(AriaRole.Link, new() { Name = "User Management" }).ClickAsync();

            // Click the banner link.
            await page.GetByTestId("user-admin-dashboard-banner-link").ClickAsync();

            // Must land on the Security Dashboard.
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(@"/admin/security"));

            // The USERS AND ROLE ASSIGNMENT tab must be clickable.
            await page.GetByRole(AriaRole.Tab)
                .Filter(new() { HasTextRegex = new Regex("USERS AND ROLE ASSIGNMENT", RegexOptions.IgnoreCase) })
                .First
                .ClickAsync();

            await Assertions
                .Expect(page.Locator("[data-testid='users-master-grid-container']"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Magic-link sign-in helper — mirrors the human's flow exactly.
    // Adapted from SensitiveFieldMaskingUatCoverageTests.
    // ─────────────────────────────────────────────────────────────────────────
    private async Task SignInAsAdminViaMagicLinkAsync(IPage page)
    {
        await _fixture.ClearMagicLinksAsync();

        await page.GotoAsync(
            $"{_fixture.BaseUrl}/login",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await Assertions.Expect(page.GetByLabel("Email")).ToBeVisibleAsync(new() { Timeout = 10_000 });

        await page.GetByLabel("Email").FillAsync(AdminEmail);
        await page.GetByRole(AriaRole.Button, new() { Name = "Send sign-in link" }).ClickAsync();
        await Assertions.Expect(page.GetByText("We sent a sign-in code to"))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });

        var inboxPage = await _fixture.CreatePageAsync();
        try
        {
            await inboxPage.GotoAsync(
                $"{_fixture.BaseUrl}/dev/email-inbox",
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

            var firstRow = inboxPage.Locator("tbody tr.mud-table-row").First;
            await Assertions.Expect(firstRow).ToBeVisibleAsync(new() { Timeout = 15_000 });
            var linkAnchor = firstRow.Locator("td[data-label='Link'] a").First;
            await Assertions.Expect(linkAnchor)
                .ToHaveAttributeAsync("href", new Regex(@"^https?://.+/login\?token="));

            var magicHref = await linkAnchor.GetAttributeAsync("href");
            await page.GotoAsync(magicHref!,
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        }
        finally
        {
            await _fixture.ClosePageAsync(inboxPage);
        }

        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Toggle Admin" }))
            .ToBeVisibleAsync(new() { Timeout = 15_000 });
    }
}
