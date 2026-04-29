using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;

namespace AcmeCoffeeOnline.Playwright;

/// <summary>
/// Isolated home for ProductAdministration tests that have proven flaky under
/// full-suite parallelism due to the cross-test cumulative-state issue
/// documented in
/// <c>docs/session-sets/playwright-sync-watermark-and-playbook/cumulative-flake-investigation.md</c>.
/// See <see cref="FlakyPlaywrightCollection"/> for the isolation rationale.
/// </summary>
[Collection(FlakyPlaywrightCollection.Name)]
public sealed class ProductAdministrationWorkflowFlakyTests
{
    private readonly PlaywrightFixture _fixture;

    public ProductAdministrationWorkflowFlakyTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    // TODO(drain-flaky): pass-alone-fail-in-suite Tab-focus race (NOT the same
    // root cause as E2E_WF_04's was).
    //
    // Session 2 observation: TabUntilFocusedFieldAsync expected "Name", got
    // "Grind Type" after ~120 prior tests; isolated re-run after fresh Web
    // container: 5/5 PASS.
    //
    // Session 5 (2026-04-29) update: investigated and ruled out as a member of
    // the E2E_WF_04 family. The diagnostic-first stress run on E2E_WF_04 found
    // its root cause was a click-during-search-rerender race — fixed by
    // wrapping ProductManagement.OnSearchAsync in TrackAction("search-products")
    // and adding WaitForQuietAsync after the test's search FillAsync (the same
    // watermark mechanism used after the toggle click). After that fix:
    // 59/60 PASS for E2E_WF_04 across three 20-iteration batches.
    //
    // E2E_WF_05 doesn't exercise the search before the failing Tab loop — it
    // clicks "Add Product" to open the dialog and then tabs. The race here is
    // between the dialog-open render (driven by ShowNewProductDialogAsync,
    // which is NOT TrackAction-wrapped) and the first Keyboard.Press("Tab")
    // call. The next-session candidate fix is to wrap the dialog-open click
    // path in TrackAction and have the test WaitForQuietAsync before tabbing.
    // That would extend iteration 2's pattern to the dialog-open transition.
    //
    // Held in FlakyPlaywrightCollection because the failure mode is real but
    // unreproducible in isolation — drain in a follow-up set after the
    // dialog-open watermark fix lands and the full suite stays clean.
    // Do not investigate piecemeal in another session set.
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

            // Navigate to admin products via the drawer nav group
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
            // MudNumericField @bind-Value commits on the browser's `change`
            // event, which Playwright's FillAsync + Locator.BlurAsync do NOT
            // synthesise (FillAsync fires only `input`; BlurAsync fires only
            // `blur`). A real Tab keypress moves focus and triggers `change`,
            // committing the bound C# decimal. Without this, the rendered
            // field shows "12.34" but the bound value stays at the default
            // 0.01, so Create saves that instead.
            await unitPriceField.ClickAsync();
            await page.Keyboard.PressAsync("Control+A");
            await page.Keyboard.PressAsync("Delete");
            await unitPriceField.PressSequentiallyAsync("12.34");
            await page.Keyboard.PressAsync("Tab");
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
