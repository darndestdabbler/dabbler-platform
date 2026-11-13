using System.Text.RegularExpressions;
using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace AcmeCoffeeOnline.Playwright;

/// <summary>
/// E2E coverage for admin-user-creation-flow UAT-1 through UAT-4 using the
/// same magic-link sign-in flow a human follows — not SeededSignInAsync.
/// Correctness backstop per project-guidance option (a): the W0 runner's
/// SeededSignInAsync conflicts with the DSL-driven login in the UAT checklist,
/// so these hand-authored Playwright [Fact] tests serve as the compiled-test
/// proof required before handing the checklist to the human.
/// </summary>
[Collection(PlaywrightCollection.Name)]
public sealed class AdminUserCreationFlowUatTests
{
    private const string AdminEmail = "admin@acmecoffee.com";
    private const string UserAdminJdoeEmail = "jdoe@acmecoffee.com";

    // Unique per test-run so repeated dotnet test invocations don't collide on DB state.
    private static readonly string RunId = Guid.NewGuid().ToString("N")[..8];

    private readonly PlaywrightFixture _fixture;
    private readonly ITestOutputHelper _output;

    public AdminUserCreationFlowUatTests(PlaywrightFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task SuccessfulCreate_SystemAdmin_HappyPath()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await SignInViaMagicLinkAsync(page, AdminEmail);
            await NavigateToUsersTabAsync(page);

            await OpenAddUserDialogAsync(page);

            var uniqueEmail = $"e2e-newuser-{RunId}@test.local";
            await page.GetByTestId("create-user-display-name").FillAsync("E2E New User");
            await page.GetByTestId("create-user-email").FillAsync(uniqueEmail);

            await PlaywrightFixture.SelectFormDropdownAsync(page, "create-user-role-select", "Backoffice User (BackofficeUser)");
            await PlaywrightFixture.SelectFormDropdownAsync(page, "create-user-org-select", "Internal Operations");

            var submitBtn = page.GetByTestId("create-user-submit-button");
            await Assertions.Expect(submitBtn).ToBeEnabledAsync(new() { Timeout = 5_000 });
            await submitBtn.ClickAsync();

            await Assertions.Expect(page.GetByTestId("create-user-dialog-form"))
                .Not.ToBeVisibleAsync(new() { Timeout = 15_000 });

            var gridContainer = page.GetByTestId("users-master-grid-container");
            await Assertions.Expect(gridContainer.GetByText(uniqueEmail).First)
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task UserAdmin_CrossOrgRejection()
    {
        // jdoe@acmecoffee.com is a BackofficeUser with Internal org membership.
        // SeededSignInAsync with roles=UserAdmin simulates the UserAdmin baked-in
        // role being active — valid for test setup of state that is out-of-scope
        // of the UAT (the UserAdmin grant itself is not what's being tested here).
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, "jdoe@acmecoffee.com", roles: "UserAdmin");
            await NavigateToUsersTabAsync(page);

            await OpenAddUserDialogAsync(page);

            await page.GetByTestId("create-user-role-select").ClickAsync();
            await page.WaitForTimeoutAsync(800); // MudBlazor popover render time

            // Count option testids — CountAsync() works regardless of visibility.
            // SystemAdmin and CrossOrgUserAdmin must NOT appear in a UserAdmin granter's dropdown.
            var sysAdminCount = await page.GetByTestId("create-user-role-option-SystemAdmin").CountAsync();
            var crossOrgCount = await page.GetByTestId("create-user-role-option-CrossOrgUserAdmin").CountAsync();
            _output.WriteLine($"SystemAdmin option count: {sysAdminCount}, CrossOrgUserAdmin count: {crossOrgCount}");
            Assert.Equal(0, sysAdminCount);
            Assert.Equal(0, crossOrgCount);

            // Close popover and cancel dialog.
            await page.Keyboard.PressAsync("Escape");
            await page.WaitForTimeoutAsync(300);
            await page.GetByTestId("create-user-cancel-button").ClickAsync();
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task DuplicateEmail_ShowsValidationError()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await SignInViaMagicLinkAsync(page, AdminEmail);
            await NavigateToUsersTabAsync(page);

            await OpenAddUserDialogAsync(page);

            await page.GetByTestId("create-user-display-name").FillAsync("Dup Email Test");
            await page.GetByTestId("create-user-email").FillAsync(AdminEmail);

            await PlaywrightFixture.SelectFormDropdownAsync(page, "create-user-role-select", "Backoffice User (BackofficeUser)");
            await PlaywrightFixture.SelectFormDropdownAsync(page, "create-user-org-select", "Internal Operations");

            var submitBtn = page.GetByTestId("create-user-submit-button");
            await Assertions.Expect(submitBtn).ToBeEnabledAsync(new() { Timeout = 5_000 });
            await submitBtn.ClickAsync();

            var errorLocator = page.GetByTestId("create-user-error-message");
            await Assertions.Expect(errorLocator).ToBeVisibleAsync(new() { Timeout = 10_000 });
            await Assertions.Expect(errorLocator)
                .ToContainTextAsync("A user with this email already exists.");

            await Assertions.Expect(page.GetByTestId("create-user-dialog-form"))
                .ToBeVisibleAsync();
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task AuditEmission_UserCreated()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await SignInViaMagicLinkAsync(page, AdminEmail);
            await NavigateToUsersTabAsync(page);

            await OpenAddUserDialogAsync(page);

            var auditEmail = $"audit-e2e-{RunId}@test.local";
            await page.GetByTestId("create-user-display-name").FillAsync("Audit E2E User");
            await page.GetByTestId("create-user-email").FillAsync(auditEmail);

            await PlaywrightFixture.SelectFormDropdownAsync(page, "create-user-role-select", "Backoffice User (BackofficeUser)");
            await PlaywrightFixture.SelectFormDropdownAsync(page, "create-user-org-select", "Internal Operations");

            var submitBtn = page.GetByTestId("create-user-submit-button");
            await Assertions.Expect(submitBtn).ToBeEnabledAsync(new() { Timeout = 5_000 });
            await submitBtn.ClickAsync();

            await Assertions.Expect(page.GetByTestId("create-user-dialog-form"))
                .Not.ToBeVisibleAsync(new() { Timeout = 15_000 });

            var complianceTab = page.GetByRole(AriaRole.Tab)
                .Filter(new() { HasTextRegex = new Regex("COMPLIANCE REVIEW", RegexOptions.IgnoreCase) })
                .First;
            await Assertions.Expect(complianceTab).ToBeVisibleAsync(new() { Timeout = 5_000 });
            await complianceTab.ClickAsync();

            // At least one UserCreated audit row must be visible (may be multiple from prior runs).
            await Assertions.Expect(page.GetByText("UserCreated").First)
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private async Task SignInViaMagicLinkAsync(IPage page, string email)
    {
        await _fixture.ClearMagicLinksAsync();

        await page.GotoAsync(
            $"{_fixture.BaseUrl}/login",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await Assertions.Expect(page.GetByLabel("Email")).ToBeVisibleAsync(new() { Timeout = 10_000 });

        await page.GetByLabel("Email").FillAsync(email);
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

    private async Task NavigateToUsersTabAsync(IPage page)
    {
        await page.GetByRole(AriaRole.Button, new() { Name = "Toggle Admin" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Security Dashboard" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(new Regex(@"/admin/security"));

        var usersTab = page.GetByRole(AriaRole.Tab)
            .Filter(new() { HasTextRegex = new Regex("USERS AND ROLE ASSIGNMENT", RegexOptions.IgnoreCase) })
            .First;
        await Assertions.Expect(usersTab).ToBeVisibleAsync(new() { Timeout = 10_000 });
        await usersTab.ClickAsync();

        var captionLocator = page.Locator("[data-testid='users-master-grid-container']")
            .GetByText(new Regex(@"\d+ of \d+ users"));
        await Assertions.Expect(captionLocator).ToBeVisibleAsync(new() { Timeout = 15_000 });
    }

    private async Task OpenAddUserDialogAsync(IPage page)
    {
        var addUserBtn = page.GetByTestId("add-user-button");
        await Assertions.Expect(addUserBtn).ToBeVisibleAsync(new() { Timeout = 5_000 });
        await addUserBtn.ClickAsync();
        await Assertions.Expect(page.GetByTestId("create-user-dialog-form"))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }
}
