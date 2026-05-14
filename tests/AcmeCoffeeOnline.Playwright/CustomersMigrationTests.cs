using System.Text.RegularExpressions;
using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;

namespace AcmeCoffeeOnline.Playwright;

[Collection(PlaywrightCollection.Name)]
public sealed class CustomersMigrationTests
{
    private static readonly string RunId = Guid.NewGuid().ToString("N")[..8];

    private readonly PlaywrightFixture _fixture;

    public CustomersMigrationTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task SignInAndNavigateToCustomersAsync(IPage page)
    {
        await _fixture.SeededSignInAsync(page, "admin@acmecoffee.com");
        await page.GotoAsync($"{_fixture.BaseUrl}/dashboard", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });
        await _fixture.NavigateViaDrawerAsync(page, "Retail Businesses", ".*/customers$");
    }

    [Fact]
    public async Task CustomerFilter_QueryString_Persists_And_Restores_On_ReloadAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await SignInAndNavigateToCustomersAsync(page);
            await PlaywrightFixture.WaitForGridDataAsync(page);

            // Enter a last-name filter using the DeclarativeFilterPanel
            var lastNameInput = page.GetByLabel("Last Name");
            await lastNameInput.FillAsync("Smith");
            await page.WaitForTimeoutAsync(400); // debounce

            // Verify URL contains the filter value (DeclarativeFilterPanel in ServerSide mode)
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(@"LastName=Smith", RegexOptions.IgnoreCase));

            // Reload with the filter URL and verify the panel restores it
            var filteredUrl = page.Url;
            await page.GotoAsync(filteredUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await PlaywrightFixture.WaitForGridDataAsync(page);

            await Assertions.Expect(lastNameInput).ToHaveValueAsync("Smith");
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task CustomerDetail_DirtyAwareActionRow_SaveAndUndo_WorksAsync()
    {
        var page = await _fixture.CreatePageAsync();
        var userId = await _fixture.GetSeededUserIdAsync("admin@acmecoffee.com");
        CustomerSnapshot? originalSnapshot = null;
        int? customerId = null;

        try
        {
            await SignInAndNavigateToCustomersAsync(page);
            await PlaywrightFixture.WaitForGridDataAsync(page);
            await PlaywrightFixture.ClickGridRowAsync(page, 0);
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(@".*/customers/\d+$"));

            var urlMatch = Regex.Match(page.Url, @"/customers/(\d+)$");
            customerId = int.Parse(urlMatch.Groups[1].Value);
            originalSnapshot = await _fixture.GetCustomerByIdAsync(userId, customerId.Value);

            // Edit phone — Save should become enabled
            var editedPhone = $"555-{RunId}";
            await PlaywrightFixture.FillFormFieldAsync(page, "field-phone", editedPhone);
            await PlaywrightFixture.AssertSaveEnabledAsync(page);

            // Save and verify clean state
            await PlaywrightFixture.ClickSaveAsync(page);
            await PlaywrightFixture.AssertSaveDisabledAsync(page);

            // Edit again and Undo — value should revert to what was saved
            var phoneInput = await PlaywrightFixture.GetFormInputAsync(page, "field-phone");
            await PlaywrightFixture.FillFormFieldAsync(page, "field-phone", "000-0000");
            await PlaywrightFixture.AssertSaveEnabledAsync(page);
            await PlaywrightFixture.ClickUndoAsync(page);

            await Assertions.Expect(phoneInput).ToHaveValueAsync(editedPhone);
            await PlaywrightFixture.AssertSaveDisabledAsync(page);
        }
        finally
        {
            // Restore original phone via API
            if (originalSnapshot is not null && customerId.HasValue)
            {
                try
                {
                    originalSnapshot.Phone = originalSnapshot.Phone; // no-op; update restores original
                    await _fixture.UpdateCustomerAsync(userId, originalSnapshot);
                }
                catch { /* best-effort cleanup */ }
            }

            await _fixture.ClosePageAsync(page);
        }
    }
}
