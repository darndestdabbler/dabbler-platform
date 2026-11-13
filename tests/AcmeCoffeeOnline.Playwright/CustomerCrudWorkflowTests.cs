using System.Text.RegularExpressions;
using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;

namespace AcmeCoffeeOnline.Playwright;

[Collection(PlaywrightCollection.Name)]
public sealed class CustomerCrudWorkflowTests
{
    private readonly PlaywrightFixture _fixture;

    public CustomerCrudWorkflowTests(PlaywrightFixture fixture)
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

    private static int ExtractCustomerIdFromUrl(string url)
    {
        var match = Regex.Match(url, @"/customers/(\d+)$");
        Assert.True(match.Success, $"Expected URL to match /customers/{{id}}, got {url}");
        return int.Parse(match.Groups[1].Value);
    }

    [Fact]
    public async Task CustomersList_LoadsWithDataAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await SignInAndNavigateToCustomersAsync(page);
            await PlaywrightFixture.WaitForGridDataAsync(page);

            // Verify column headers
            var headerRow = page.Locator(".mud-table-head");
            await Assertions.Expect(headerRow.GetByText("Primary Contact First Name")).ToBeVisibleAsync();
            await Assertions.Expect(headerRow.GetByText("Primary Contact Last Name")).ToBeVisibleAsync();
            await Assertions.Expect(headerRow.GetByText("Account Email")).ToBeVisibleAsync();

            // Verify data rows exist
            var rowCount = await PlaywrightFixture.GetGridRowCountAsync(page);
            Assert.True(rowCount > 0, "Customers grid should have at least one data row.");

            // Verify status text
            await Assertions.Expect(page.GetByText(new Regex(@"\d+ records"))).ToBeVisibleAsync();
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task CustomerDetail_LoadsExistingRecordAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            var userId = await _fixture.GetSeededUserIdAsync("admin@acmecoffee.com");
            await SignInAndNavigateToCustomersAsync(page);
            await PlaywrightFixture.WaitForGridDataAsync(page);

            await PlaywrightFixture.ClickGridRowAsync(page, 0);

            await Assertions.Expect(page).ToHaveURLAsync(new Regex(@".*/customers/\d+$"));
            await Assertions.Expect(page.GetByTestId("customer-detail-title")).ToBeVisibleAsync();

            var customerId = ExtractCustomerIdFromUrl(page.Url);
            var customer = await _fixture.GetCustomerByIdAsync(userId, customerId);

            // Verify key fields are populated
            var firstNameInput = await PlaywrightFixture.GetFormInputAsync(page, "field-first-name");
            await Assertions.Expect(firstNameInput).ToHaveValueAsync(customer.FirstName);

            var lastNameInput = await PlaywrightFixture.GetFormInputAsync(page, "field-last-name");
            await Assertions.Expect(lastNameInput).ToHaveValueAsync(customer.LastName);

            var emailInput = await PlaywrightFixture.GetFormInputAsync(page, "field-email");
            await Assertions.Expect(emailInput).ToHaveValueAsync(customer.Email);

            // Verify save is disabled (no unsaved changes)
            await PlaywrightFixture.AssertSaveDisabledAsync(page);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task CustomerDetail_EditAndSave_PersistsViaApiAsync()
    {
        var page = await _fixture.CreatePageAsync();
        var userId = await _fixture.GetSeededUserIdAsync("admin@acmecoffee.com");
        CustomerSnapshot? original = null;
        int? customerId = null;

        try
        {
            await SignInAndNavigateToCustomersAsync(page);
            await PlaywrightFixture.WaitForGridDataAsync(page);

            await PlaywrightFixture.ClickGridRowAsync(page, 0);

            customerId = ExtractCustomerIdFromUrl(page.Url);
            original = await _fixture.GetCustomerByIdAsync(userId, customerId.Value);

            // Edit the phone field (safe, non-required field)
            var editedPhone = $"555-{DateTime.UtcNow:mmssfff}";
            await PlaywrightFixture.FillFormFieldAsync(page, "field-phone", editedPhone);

            await PlaywrightFixture.AssertSaveEnabledAsync(page);

            // Save
            await PlaywrightFixture.ClickSaveAsync(page);

            // Verify save returns to clean state
            await PlaywrightFixture.AssertSaveDisabledAsync(page);

            // Verify via API
            var updated = await _fixture.GetCustomerByIdAsync(userId, customerId.Value);
            Assert.Equal(editedPhone, updated.Phone);
        }
        finally
        {
            if (original is not null && customerId.HasValue)
            {
                await _fixture.UpdateCustomerAsync(userId, original);
            }

            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task CustomerDetail_EditAndUndo_RevertsFieldsAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await SignInAndNavigateToCustomersAsync(page);
            await PlaywrightFixture.WaitForGridDataAsync(page);

            await PlaywrightFixture.ClickGridRowAsync(page, 0);

            // Record original first name
            var firstNameInput = await PlaywrightFixture.GetFormInputAsync(page, "field-first-name");
            var originalFirstName = await firstNameInput.InputValueAsync();

            // Edit the first name
            await PlaywrightFixture.FillFormFieldAsync(page, "field-first-name", originalFirstName + " Modified");
            await PlaywrightFixture.AssertSaveEnabledAsync(page);

            // Click Undo
            await PlaywrightFixture.ClickUndoAsync(page);

            // Verify fields reverted and save is disabled
            await Assertions.Expect(firstNameInput).ToHaveValueAsync(originalFirstName);
            await PlaywrightFixture.AssertSaveDisabledAsync(page);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task CustomerDetail_BackButton_ReturnsToCustomersListAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await SignInAndNavigateToCustomersAsync(page);
            await PlaywrightFixture.WaitForGridDataAsync(page);

            await PlaywrightFixture.ClickGridRowAsync(page, 0);
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(@".*/customers/\d+$"));

            // Click back button
            var backButton = page.Locator("button[aria-label='Back to retail businesses']");
            await Assertions.Expect(backButton).ToBeVisibleAsync();
            await backButton.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await Assertions.Expect(page).ToHaveURLAsync(new Regex(@".*/customers$"));
            await PlaywrightFixture.WaitForGridDataAsync(page);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }
}
