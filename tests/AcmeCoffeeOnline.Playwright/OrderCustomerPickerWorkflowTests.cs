using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;

namespace AcmeCoffeeOnline.Playwright;

[Collection(PlaywrightCollection.Name)]
public sealed class OrderCustomerPickerWorkflowTests
{
    private readonly PlaywrightFixture _fixture;
    private static readonly string RunId = Guid.NewGuid().ToString("N")[..8];

    public OrderCustomerPickerWorkflowTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Picker_PickExisting_PopulatesParentFieldAsync()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, "admin@acmecoffee.com");
            await page.GotoAsync($"{_fixture.BaseUrl}/orders/new", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            await page.GetByTestId("pick-customer-button").ClickAsync();
            await Assertions.Expect(page.GetByTestId("customer-picker-dialog")).ToBeVisibleAsync();

            // Wait for the grid to load data and use the first visible row
            var firstRow = page.Locator("[data-testid='customer-picker-grid'] .mud-table-body tr").First;
            await Assertions.Expect(firstRow).ToBeVisibleAsync();

            // Read the row's name cells before clicking (cell order: LastName, FirstName, Email, Use)
            var lastName = (await firstRow.Locator("td").Nth(0).InnerTextAsync()).Trim();
            var firstName = (await firstRow.Locator("td").Nth(1).InnerTextAsync()).Trim();
            var expectedName = $"{firstName} {lastName}";

            // Click the row-wise "Use" button to link without saving
            await firstRow.GetByTestId("customer-picker-dialog-row-use").ClickAsync();

            // Dialog closes
            await Assertions.Expect(page.GetByTestId("customer-picker-dialog")).Not.ToBeVisibleAsync();

            // Parent OrderDetail customer field shows the picked name
            var customerNameInput = await PlaywrightFixture.GetFormInputAsync(page, "field-customer-name");
            await Assertions.Expect(customerNameInput).ToHaveValueAsync(expectedName);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task Picker_AddNew_SavesAndPopulatesParentFieldAsync()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            var userId = await _fixture.GetSeededUserIdAsync("admin@acmecoffee.com");
            await _fixture.SeededSignInAsync(page, "admin@acmecoffee.com");
            await page.GotoAsync($"{_fixture.BaseUrl}/orders/new", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            await page.GetByTestId("pick-customer-button").ClickAsync();
            await Assertions.Expect(page.GetByTestId("customer-picker-dialog")).ToBeVisibleAsync();

            var email = $"picker-add-{RunId}@test.local";
            const string firstName = "Picker";
            const string lastName = "AddNew";

            await PlaywrightFixture.FillFormFieldAsync(page, "customer-picker-first-name", firstName);
            await PlaywrightFixture.FillFormFieldAsync(page, "customer-picker-last-name", lastName);
            await PlaywrightFixture.FillFormFieldAsync(page, "customer-picker-email", email);

            await page.GetByTestId("customer-picker-dialog-primary-button").ClickAsync();

            await Assertions.Expect(page.GetByTestId("customer-picker-dialog")).Not.ToBeVisibleAsync();

            var customerNameInput = await PlaywrightFixture.GetFormInputAsync(page, "field-customer-name");
            await Assertions.Expect(customerNameInput).ToHaveValueAsync($"{firstName} {lastName}");

            // Verify the new customer exists in the DB via the API
            var customers = await _fixture.GetCustomersAsync(userId, 1, 1000);
            var newCustomer = Assert.Single(customers, c => c.Email == email);
            Assert.Equal(firstName, newCustomer.FirstName);
            Assert.Equal(lastName, newCustomer.LastName);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task Picker_DirtyState_FlipsPrimaryButtonLabelAsync()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, "admin@acmecoffee.com");
            await page.GotoAsync($"{_fixture.BaseUrl}/orders/new", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            await page.GetByTestId("pick-customer-button").ClickAsync();
            await Assertions.Expect(page.GetByTestId("customer-picker-dialog")).ToBeVisibleAsync();

            var firstRow = page.Locator("[data-testid='customer-picker-grid'] .mud-table-body tr").First;
            await Assertions.Expect(firstRow).ToBeVisibleAsync();

            // Click anywhere on the row (not the Use icon button) to select it
            await firstRow.Locator("td").Nth(0).ClickAsync();

            var primaryButton = page.GetByTestId("customer-picker-dialog-primary-button");
            await Assertions.Expect(primaryButton).ToHaveTextAsync("Use");

            // Dirty the form by editing first-name
            await PlaywrightFixture.FillFormFieldAsync(page, "customer-picker-first-name", "Dirty Edit");

            await Assertions.Expect(primaryButton).ToHaveTextAsync("Save and Use");
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }
}
