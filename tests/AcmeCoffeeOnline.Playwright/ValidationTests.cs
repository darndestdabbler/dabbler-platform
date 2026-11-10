using System.Text.RegularExpressions;
using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;

namespace AcmeCoffeeOnline.Playwright;

[Collection(PlaywrightCollection.Name)]
public sealed class ValidationTests
{
    private readonly PlaywrightFixture _fixture;

    public ValidationTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    private static int ExtractProductIdFromUrl(string url)
    {
        var match = Regex.Match(url, @"/products/(\d+)$");
        Assert.True(match.Success, $"Expected URL to match /products/{{id}}, got {url}");
        return int.Parse(match.Groups[1].Value);
    }

    private async Task SignInAndNavigateToProductDetailAsync(IPage page)
    {
        await _fixture.SeededSignInAsync(page, "admin@acmecoffee.com");
        await page.GotoAsync($"{_fixture.BaseUrl}/dashboard", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });
        await _fixture.NavigateViaDrawerAsync(page, "Products", ".*/products$");
        await PlaywrightFixture.WaitForGridDataAsync(page);
        await PlaywrightFixture.ClickGridRowAsync(page, 0);
        await Assertions.Expect(page).ToHaveURLAsync(new Regex(@".*/products/\d+$"));
    }

    private async Task SignInAndNavigateToCustomerDetailAsync(IPage page)
    {
        await _fixture.SeededSignInAsync(page, "admin@acmecoffee.com");
        await page.GotoAsync($"{_fixture.BaseUrl}/dashboard", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });
        await _fixture.NavigateViaDrawerAsync(page, "Retail Businesses", ".*/customers$");
        await PlaywrightFixture.WaitForGridDataAsync(page);
        await PlaywrightFixture.ClickGridRowAsync(page, 0);
        await Assertions.Expect(page).ToHaveURLAsync(new Regex(@".*/customers/\d+$"));
    }

    [Fact]
    public async Task ProductDetail_NameRequired_ShowsValidationErrorAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await SignInAndNavigateToProductDetailAsync(page);

            // Clear the required Name field
            await PlaywrightFixture.ClearFormFieldAsync(page, "field-name");
            await PlaywrightFixture.AssertSaveEnabledAsync(page);

            // Attempt to save — validation should prevent it
            await page.GetByTestId("save-button").ClickAsync();

            // Verify the required-field error message is visible
            await Assertions.Expect(page.GetByText("Product name is required"))
                .ToBeVisibleAsync(new() { Timeout = 3000 });
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task ProductDetail_UnitPriceBoundary_EnforcesMinimumAsync()
    {
        var page = await _fixture.CreatePageAsync();
        var userId = await _fixture.GetSeededUserIdAsync("admin@acmecoffee.com");
        ProductSnapshot? original = null;
        int? productId = null;

        try
        {
            await SignInAndNavigateToProductDetailAsync(page);
            productId = ExtractProductIdFromUrl(page.Url);
            original = await _fixture.GetProductByIdAsync(userId, productId.Value);

            // Set price to 0 (below min 0.01) — MudNumericField enforces the
            // boundary by clamping or validation
            await PlaywrightFixture.FillFormFieldAsync(page, "field-unit-price", "0");

            // Also edit name to trigger dirty state (numeric fields don't call
            // the OnFieldChanged callback directly)
            await PlaywrightFixture.FillFormFieldAsync(page, "field-name", original.Name + " boundary");

            // Click save and wait for it to land (Save renders disabled again
            // only once the PUT has returned), so neither the read below nor
            // the restore in finally races the UI's save.
            await PlaywrightFixture.ClickSaveAsync(page);
            await PlaywrightFixture.AssertSaveDisabledAsync(page);

            // Verify the saved price respects the minimum boundary
            var afterProduct = await _fixture.GetProductByIdAsync(userId, productId.Value);
            Assert.True(afterProduct.UnitPrice >= 0.01m,
                $"Unit price should be at least 0.01 after min boundary enforcement, got {afterProduct.UnitPrice}");
        }
        finally
        {
            if (original is not null && productId.HasValue)
            {
                await _fixture.UpdateProductAsync(userId, original);
            }

            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task CustomerDetail_FirstNameRequired_ShowsValidationErrorAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await SignInAndNavigateToCustomerDetailAsync(page);

            // Clear the required First Name field
            await PlaywrightFixture.ClearFormFieldAsync(page, "field-first-name");
            await PlaywrightFixture.AssertSaveEnabledAsync(page);

            // Attempt to save
            await page.GetByTestId("save-button").ClickAsync();

            // Verify the required-field error message is visible
            await Assertions.Expect(page.GetByText("First name is required"))
                .ToBeVisibleAsync(new() { Timeout = 3000 });
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task CustomerDetail_LastNameRequired_ShowsValidationErrorAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await SignInAndNavigateToCustomerDetailAsync(page);

            // Clear the required Last Name field
            await PlaywrightFixture.ClearFormFieldAsync(page, "field-last-name");
            await PlaywrightFixture.AssertSaveEnabledAsync(page);

            // Attempt to save
            await page.GetByTestId("save-button").ClickAsync();

            await Assertions.Expect(page.GetByText("Last name is required"))
                .ToBeVisibleAsync(new() { Timeout = 3000 });
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task CustomerDetail_EmailRequired_ShowsValidationErrorAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await SignInAndNavigateToCustomerDetailAsync(page);

            // Clear the required Email field
            await PlaywrightFixture.ClearFormFieldAsync(page, "field-email");
            await PlaywrightFixture.AssertSaveEnabledAsync(page);

            // Attempt to save
            await page.GetByTestId("save-button").ClickAsync();

            await Assertions.Expect(page.GetByText("Email is required"))
                .ToBeVisibleAsync(new() { Timeout = 3000 });
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }
}
