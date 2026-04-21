using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;

namespace AcmeCoffeeOnline.Playwright;

/// <summary>
/// Isolated home for the ProductAdministration tests that have proven flaky
/// under the main collection's cross-collection parallelism. Running under
/// <see cref="FlakyPlaywrightCollection"/> disables parallelism with the main
/// "playwright" collection, giving these tests uncontended access to the
/// Kestrel + Blazor circuits + SQL Server while they run.
///
/// Each test here MUST carry a TODO pointing at a tracked root-cause issue so
/// this class does not become a dumping ground. Flipping these back to the
/// main collection once their races are resolved is the desired end state.
/// </summary>
[Collection(FlakyPlaywrightCollection.Name)]
public sealed class ProductAdministrationWorkflowFlakyTests
{
    private readonly PlaywrightFixture _fixture;

    public ProductAdministrationWorkflowFlakyTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    // TODO(tracked): this test passes reliably in isolation but fails under
    // full-suite parallelism even at a 15s snackbar-visibility timeout. Root
    // cause is concurrent MudBlazor toast queuing against the same circuit
    // under load. A focused investigation belongs in a post-Set D cleanup
    // session. Until then the isolated run covers it without blocking the
    // main suite's parallel profile.
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
                .ToBeVisibleAsync(new() { Timeout = 15_000 });

            var afterProduct = await _fixture.GetProductByIdAsync(userId!, productId.Value);
            Assert.Equal(expectedAvailability, afterProduct.IsAvailable);

            await PlaywrightFixture.AssertLayoutSpacingAsync(page);
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
}
