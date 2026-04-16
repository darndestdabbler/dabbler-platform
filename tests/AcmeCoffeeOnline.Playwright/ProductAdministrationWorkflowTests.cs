using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;

namespace AcmeCoffeeOnline.Playwright;

[Collection(PlaywrightCollection.Name)]
public sealed class ProductAdministrationWorkflowTests
{
    private readonly PlaywrightFixture _fixture;

    public ProductAdministrationWorkflowTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task E2E_WF_04_SystemAdmin_TogglesProductAvailabilityThroughBrowserAndRestoresStateAsync()
    {
        const string productName = "Blonde Roast - Decaf - Espresso";
        var page = await _fixture.CreatePageAsync();
        string? userId = null;
        int? productId = null;
        bool? originalAvailability = null;

        try
        {
            await _fixture.SeededSignInAsync(page, "admin@acmecoffee.com");

            var session = await _fixture.GetSessionInfoAsync(page);
            userId = session.UserId;

            Assert.False(string.IsNullOrWhiteSpace(userId));
            Assert.Contains("SystemAdmin", session.Roles ?? []);

            var beforeProduct = await _fixture.GetProductByExactNameAsync(userId!, productName);
            productId = beforeProduct.Id;
            originalAvailability = beforeProduct.IsAvailable;

            await _fixture.NavigateToAdminProductsAsync(page);
            var routedContent = page.GetByTestId("routed-page-content");

            await routedContent.GetByPlaceholder("Search products...").FillAsync(productName);

            var availabilitySwitch = routedContent.GetByTestId($"product-availability-{productId}");

            await Assertions.Expect(availabilitySwitch).ToBeVisibleAsync();
            Assert.Equal(originalAvailability.Value, await availabilitySwitch.IsCheckedAsync());

            await availabilitySwitch.ClickAsync();

            var expectedAvailability = !originalAvailability.Value;
            if (expectedAvailability)
            {
                await Assertions.Expect(availabilitySwitch).ToBeCheckedAsync();
            }
            else
            {
                await Assertions.Expect(availabilitySwitch).Not.ToBeCheckedAsync();
            }
            await Assertions.Expect(page.GetByText(
                    $"{productName} is now {(expectedAvailability ? "available" : "unavailable")}.",
                    new PageGetByTextOptions { Exact = true }))
                .ToBeVisibleAsync();

            var afterProduct = await _fixture.GetProductByIdAsync(userId!, productId.Value);
            Assert.Equal(expectedAvailability, afterProduct.IsAvailable);
        }
        finally
        {
            if (productId.HasValue && originalAvailability.HasValue && !string.IsNullOrWhiteSpace(userId))
            {
                await _fixture.RestoreProductAvailabilityAsync(userId, productId.Value, originalAvailability.Value);
            }

            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task E2E_WF_05_SystemAdmin_AddsProductAfterKeyboardAndValidationChecksAsync()
    {
        var page = await _fixture.CreatePageAsync();
        var productName = $"Playwright Automation Product {DateTime.UtcNow:yyyyMMddHHmmssfff}";
        var archivedNamePrefix = $"Archived Playwright Product {DateTime.UtcNow:yyyyMMddHHmmssfff}";
        string? userId = null;
        int? createdProductId = null;

        try
        {
            await _fixture.SeededSignInAsync(page, "admin@acmecoffee.com");

            var session = await _fixture.GetSessionInfoAsync(page);
            userId = session.UserId;

            Assert.False(string.IsNullOrWhiteSpace(userId));
            Assert.Contains("SystemAdmin", session.Roles ?? []);

            await _fixture.NavigateToAdminProductsAsync(page);
            var routedContent = page.GetByTestId("routed-page-content");

            await routedContent.GetByRole(AriaRole.Button, new() { Name = "Add Product" }).ClickAsync();

            var dialog = page.Locator("[role='dialog']");
            await Assertions.Expect(dialog).ToBeVisibleAsync();

            var nameField = page.GetByTestId("product-name-input");
            var roastLevelField = dialog.GetByLabel("Roast Level");
            var unitPriceField = dialog.GetByLabel("Unit Price");
            var descriptionField = dialog.GetByLabel("Description");
            var imageUrlField = dialog.GetByLabel("Image URL");
            var createButton = dialog.GetByRole(AriaRole.Button, new() { Name = "Create" });

            await Assertions.Expect(createButton).ToBeDisabledAsync();

            await TabUntilFocusedFieldAsync(page, "Name", maxTabs: 3);
            await TabUntilFocusedFieldAsync(page, "Roast Level", maxTabs: 2);

            await unitPriceField.FillAsync("0");
            await nameField.ClickAsync();
            await Assertions.Expect(createButton).ToBeDisabledAsync();

            await nameField.FillAsync(productName);
            await unitPriceField.FillAsync("12.34");
            await descriptionField.FillAsync("Representative Playwright automation product.");
            await imageUrlField.FillAsync("https://example.com/playwright-product.png");

            await Assertions.Expect(createButton).ToBeEnabledAsync();
            await createButton.ClickAsync();
            try
            {
                await Assertions.Expect(dialog).Not.ToBeVisibleAsync(new() { Timeout = 2000 });
            }
            catch
            {
                var dialogText = await dialog.InnerTextAsync();
                var snackbars = await page.Locator(".mud-snackbar").AllInnerTextsAsync();
                throw new Xunit.Sdk.XunitException(
                    $"Product dialog stayed open after Create. Dialog text:{Environment.NewLine}{dialogText}{Environment.NewLine}{Environment.NewLine}Snackbars:{Environment.NewLine}{string.Join(Environment.NewLine, snackbars)}");
            }

            var createdProduct = await WaitForProductByExactNameAsync(userId!, productName);
            createdProductId = createdProduct.Id;

            Assert.Equal(productName, createdProduct.Name);
            Assert.Equal(12.34m, createdProduct.UnitPrice);
            Assert.True(createdProduct.IsAvailable);

            await routedContent.GetByPlaceholder("Search products...").FillAsync(productName);
            await Assertions.Expect(routedContent.GetByText(productName, new LocatorGetByTextOptions { Exact = true }))
                .ToBeVisibleAsync();
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(userId))
            {
                if (!createdProductId.HasValue)
                {
                    var createdProduct = await _fixture.TryGetProductByExactNameAsync(userId, productName);
                    createdProductId = createdProduct?.Id;
                }

                if (createdProductId.HasValue)
                {
                    await _fixture.ArchiveProductByIdAsync(userId, createdProductId.Value, archivedNamePrefix);
                }
            }

            await _fixture.ClosePageAsync(page);
        }
    }

    private static async Task<string> GetFocusedFieldLabelAsync(IPage page)
    {
        return await page.EvaluateAsync<string>(
            "() => document.activeElement?.closest('.mud-input-control')?.querySelector('label')?.textContent?.trim() ?? ''");
    }

    private async Task<ProductSnapshot> WaitForProductByExactNameAsync(string userId, string productName)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var product = await _fixture.TryGetProductByExactNameAsync(userId, productName);
            if (product is not null)
            {
                return product;
            }

            await Task.Delay(250);
        }

        throw new Xunit.Sdk.XunitException($"Could not find product '{productName}' through the API.");
    }

    private static async Task TabUntilFocusedFieldAsync(IPage page, string expectedLabel, int maxTabs)
    {
        for (var attempt = 0; attempt < maxTabs; attempt++)
        {
            await page.Keyboard.PressAsync("Tab");
            if (string.Equals(await GetFocusedFieldLabelAsync(page), expectedLabel, StringComparison.Ordinal))
            {
                return;
            }
        }

        Assert.Equal(expectedLabel, await GetFocusedFieldLabelAsync(page));
    }
}
