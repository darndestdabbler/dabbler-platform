using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;

namespace AcmeCoffeeOnline.Playwright;

/// <summary>
/// Browser-level coverage for the Users and Role Assignment tab added in
/// Session 7 of the <c>rearchitect-role-administration-dashboard</c> session
/// set. CrossOrgUserAdmin signs in, locates a seeded target user with no
/// prior role assignment, and elevates them to <c>BackofficeUser</c>. The
/// assignment must flow through <see cref="IUserRoleStore"/> and surface in
/// the grid's role chips as well as the details surface's current-roles
/// list.
/// </summary>
[Collection(PlaywrightCollection.Name)]
public sealed class UsersAndRoleAssignmentWorkflowTests
{
    private const string ActorEmail = "admin@acmecoffee.com";
    private const string TargetEmail = "robert.williams@example.com";
    private const string NewRole = "BackofficeUser";

    private readonly PlaywrightFixture _fixture;

    public UsersAndRoleAssignmentWorkflowTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CrossOrgUserAdmin_AssignsBackofficeUser_ToSeededCustomerAsync()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            // CrossOrgUserAdmin role-override: admin@acmecoffee.com is an
            // existing SystemAdmin in seed data, but the test-auth endpoint
            // supports role overrides so we can exercise the
            // CrossOrgUserAdmin authority rules without having to seed a
            // new account.
            await _fixture.SeededSignInAsync(page, ActorEmail, roles: "CrossOrgUserAdmin");

            await page.GotoAsync($"{_fixture.BaseUrl}/admin/security", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
            });

            // Activate the Users tab (CrossOrgUserAdmin sees
            // Organization Master Details, Users and Role Assignment,
            // Role Documentation — Users is the middle tab).
            await page.GetByRole(AriaRole.Tab, new() { Name = "Users and Role Assignment" }).ClickAsync();

            await Assertions
                .Expect(page.GetByTestId("users-master-grid"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            // Resolve the target user's seeded id so we can use the stable
            // data-testid selectors rather than matching by email (which may
            // appear in other columns).
            var targetUserId = await _fixture.GetSeededUserIdAsync(TargetEmail);

            // Click the row for the target customer to select them.
            var targetRow = page.Locator(
                $"[data-testid='users-row-{targetUserId}-roles'], [data-testid='users-row-{targetUserId}-no-roles']");
            await Assertions.Expect(targetRow.First).ToBeVisibleAsync(new() { Timeout = 10_000 });

            // MudDataGrid row selection is triggered by clicking the row. The
            // chip container lives inside that row, so scrolling up to the
            // row element and clicking achieves selection.
            var row = targetRow.First.Locator("xpath=ancestor::tr[1]");
            await row.ClickAsync();

            await Assertions
                .Expect(page.GetByTestId("user-role-assignment-heading"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            // Open the MudSelect dropdown and pick BackofficeUser. The
            // MudSelect renders as a button in the DOM; open it, then the
            // options render in a popover.
            await page.GetByTestId("user-role-assignment-role-select").ClickAsync();
            await page.GetByTestId($"user-role-assignment-role-option-{NewRole}").ClickAsync();

            await page.GetByTestId("user-role-assignment-assign-button").ClickAsync();

            // The grid refresh flows through SelectedUserChanged and the
            // details surface re-renders against the freshly-projected row.
            // Assert the new role chip appears in the current-roles section.
            await Assertions
                .Expect(page.GetByTestId($"user-role-assignment-current-{NewRole}"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            // The grid row itself also now carries the new chip.
            await Assertions
                .Expect(page.GetByTestId($"users-row-{targetUserId}-role-{NewRole}"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
        }
        finally
        {
            // Tidy up the assignment so the test is idempotent and future
            // test runs start from the same baseline.
            try
            {
                await page.GetByTestId($"user-role-assignment-current-{NewRole}").Locator("button.mud-chip-close-button").ClickAsync();
            }
            catch
            {
                // Best-effort cleanup; do not fail the test on teardown.
            }

            await _fixture.ClosePageAsync(page);
        }
    }
}
