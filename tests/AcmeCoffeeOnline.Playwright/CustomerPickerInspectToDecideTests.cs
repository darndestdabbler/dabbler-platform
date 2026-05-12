using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;

namespace AcmeCoffeeOnline.Playwright;

[Collection(PlaywrightCollection.Name)]
public sealed class CustomerPickerInspectToDecideTests
{
    private readonly PlaywrightFixture _fixture;

    public CustomerPickerInspectToDecideTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task InspectToDecide_ToggleFlow_MakesFieldsEditableAsync()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, "admin@acmecoffee.com");
            await page.GotoAsync($"{_fixture.BaseUrl}/orders/new", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

            // The 'Inspect' button is only enabled after a customer is picked.
            await page.GetByTestId("pick-customer-button").ClickAsync();
            var firstCustomerRowInPicker = page.Locator("[data-testid='customer-picker-grid'] .mud-table-body tr").First;
            await Assertions.Expect(firstCustomerRowInPicker).ToBeVisibleAsync();
            await firstCustomerRowInPicker.GetByTestId("customer-picker-dialog-row-use").ClickAsync();
            await Assertions.Expect(page.GetByTestId("inspect-customer-button")).ToBeEnabledAsync();

            // Now, start the inspect-to-decide workflow.
            await page.GetByTestId("inspect-customer-button").ClickAsync();
            await Assertions.Expect(page.GetByTestId("customer-picker-dialog")).ToBeVisibleAsync();

            // Select the first customer in the grid to load their details.
            var firstCustomerRowInDialog = page.Locator("[data-testid='customer-picker-grid'] .mud-table-body tr").First;
            await firstCustomerRowInDialog.Locator("td").First.ClickAsync();

            // Verify the 'Edit record' toggle is now visible in the target card.
            var editToggle = page.GetByTestId("customer-picker-dialog-edit-toggle");
            await Assertions.Expect(editToggle).ToBeVisibleAsync();

            // Verify fields are read-only initially.
            var firstNameInput = page.GetByTestId("customer-picker-first-name").Locator("input");
            var primaryButton = page.GetByTestId("customer-picker-dialog-primary-button");
            await Assertions.Expect(firstNameInput).Not.ToBeEditableAsync();
            await Assertions.Expect(primaryButton).ToHaveTextAsync("Use");

            // Click the toggle to enable editing.
            await editToggle.ClickAsync();

            // Verify fields are now editable.
            await Assertions.Expect(firstNameInput).ToBeEditableAsync();

            // Dirty the form and verify the primary button text changes.
            await PlaywrightFixture.FillFormFieldAsync(page, "customer-picker-first-name", "Edited Name");
            await Assertions.Expect(primaryButton).ToHaveTextAsync("Save and Use");
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }
}
