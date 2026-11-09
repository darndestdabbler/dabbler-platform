using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;

namespace AcmeCoffeeOnline.Playwright;

[Collection(PlaywrightCollection.Name)]
public sealed class RoleMasterDetailsRegressionTests
{
    private readonly PlaywrightFixture _fixture;
    private static readonly string RunId = Guid.NewGuid().ToString("N")[..8];

    public RoleMasterDetailsRegressionTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task RoleMasterDetails_SelectRow_CollapsesShellAsync()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, "admin@acmecoffee.com");
            await page.GotoAsync($"{_fixture.BaseUrl}/admin/security", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            // Shell starts expanded; body should be visible
            await Assertions.Expect(page.GetByTestId("role-master-details-shell-body"))
                .ToBeVisibleAsync();

            // Click the BackofficeUser row (a user-type role guaranteed to be in seed data)
            await page.GetByTestId("role-row-BackofficeUser").ClickAsync();

            // Shell collapses: body no longer visible
            await Assertions.Expect(page.GetByTestId("role-master-details-shell-body"))
                .Not.ToBeVisibleAsync();

            // Collapsed summary appears showing the selected role's canonical name
            await Assertions.Expect(page.GetByTestId("role-master-details-shell-summary"))
                .ToContainTextAsync("BackofficeUser");
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task RoleMasterDetails_AddUserTypeRoleButton_OpensDialogAsync()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, "admin@acmecoffee.com");
            await page.GotoAsync($"{_fixture.BaseUrl}/admin/security", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            // Click the ADD USER-TYPE ROLE button (catalog testId: role-master-add-button)
            await page.GetByTestId("role-master-add-button").ClickAsync();

            // AddUserTypeRoleDialog opens — confirm via its known testids
            await Assertions.Expect(page.GetByTestId("add-role-dialog-name"))
                .ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("add-role-dialog-alias"))
                .ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("add-role-dialog-description"))
                .ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("add-role-dialog-confirm"))
                .ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("add-role-dialog-cancel"))
                .ToBeVisibleAsync();

            // Cancel to avoid polluting state
            await page.GetByTestId("add-role-dialog-cancel").ClickAsync();
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task RoleMasterDetails_EditDisplayAlias_PersistsAsync()
    {
        var page = await _fixture.CreatePageAsync();
        var originalAlias = "Backoffice User"; // seeded default
        var newAlias = $"UAT-Edit-{RunId}";
        var aliasTestId = "role-row-BackofficeUser-alias";

        try
        {
            await _fixture.SeededSignInAsync(page, "admin@acmecoffee.com");
            await page.GotoAsync($"{_fixture.BaseUrl}/admin/security", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            // Read current value first so we restore exactly what was there
            var aliasInput = await PlaywrightFixture.GetFormInputAsync(page, aliasTestId);
            var currentValue = await aliasInput.InputValueAsync();
            if (!string.IsNullOrWhiteSpace(currentValue)) originalAlias = currentValue;

            // Edit the display alias and blur so MudTextField fires ValueChanged
            await aliasInput.FillAsync(newAlias);
            await aliasInput.BlurAsync();

            // Snackbar confirmation
            await Assertions.Expect(page.Locator(".mud-snackbar").First)
                .ToContainTextAsync("Display alias updated.");

            // Reload and verify persistence
            await page.GotoAsync($"{_fixture.BaseUrl}/admin/security", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            var reloadedInput = await PlaywrightFixture.GetFormInputAsync(page, aliasTestId);
            await Assertions.Expect(reloadedInput).ToHaveValueAsync(newAlias);

            // Restore original to keep subsequent tests deterministic
            await reloadedInput.FillAsync(originalAlias);
            await reloadedInput.BlurAsync();
            await Assertions.Expect(page.Locator(".mud-snackbar").First)
                .ToContainTextAsync("Display alias updated.");
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }
}
