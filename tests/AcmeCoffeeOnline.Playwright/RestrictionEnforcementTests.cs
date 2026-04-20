using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;

namespace AcmeCoffeeOnline.Playwright;

/// <summary>
/// Browser-level coverage for the data-bound restriction enforcement seam
/// introduced in Session 5 of the
/// <c>rearchitect-role-administration-dashboard</c> session set. The
/// Product UnitPrice field/column is the reference integration: Customer
/// sees it ViewOnly, WarehouseUser sees it Hidden, baked-in SystemAdmin
/// sees it editable.
/// </summary>
[Collection(PlaywrightCollection.Name)]
public sealed class RestrictionEnforcementTests
{
    private const string AdminEmail = "admin@acmecoffee.com";
    private const string CustomerEmail = "customer@acmecoffee.com";

    private readonly PlaywrightFixture _fixture;

    public RestrictionEnforcementTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<int> GetFirstProductIdAsync()
    {
        var systemAdminId = await _fixture.GetSeededUserIdAsync(AdminEmail);
        using var payload = await _fixture.GetApiJsonAsync(
            "api/products?page=1&pageSize=1",
            systemAdminId);
        var first = payload.RootElement.GetProperty("items").EnumerateArray().First();
        return first.GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task SystemAdmin_SeesUnitPriceColumnAndEditableFieldAsync()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, AdminEmail, roles: "SystemAdmin");

            await page.GotoAsync($"{_fixture.BaseUrl}/products", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
            });
            await PlaywrightFixture.WaitForGridDataAsync(page);

            await Assertions
                .Expect(page.Locator(".mud-table-head").GetByText("Unit Price"))
                .ToBeVisibleAsync();

            var productId = await GetFirstProductIdAsync();
            await page.GotoAsync($"{_fixture.BaseUrl}/products/{productId}", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
            });

            var unitPriceField = page.GetByTestId("field-unit-price");
            await Assertions.Expect(unitPriceField).ToBeVisibleAsync();
            await Assertions.Expect(unitPriceField).Not.ToHaveAttributeAsync("readonly", string.Empty);
            await Assertions.Expect(unitPriceField).ToBeEnabledAsync();
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    // Finding 10.2 resolution (security-dashboard-uat-remediation Session 5):
    // The Session 4 triage skipped this test on the theory that Customer-role
    // endpoint-auth under Docker was the blocker. The real root cause was
    // narrower — customer@acmecoffee.com was never in SeedData, so BuildAsync
    // found no AppUser row and the downstream api call returned 401. Session 5
    // seeds customer@acmecoffee.com as a dedicated Customer persona tied to
    // the first retail-business organization, which is what
    // UserSessionContextService.TryResolveCustomerId needs to yield a
    // non-null ActiveCustomerId for the "CustomerId" claim the row
    // restriction keys on.
    [Fact]
    public async Task CustomerRole_SeesUnitPriceFieldButReadOnlyAsync()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, CustomerEmail, roles: "Customer");

            var productId = await GetFirstProductIdAsync();
            await page.GotoAsync($"{_fixture.BaseUrl}/products/{productId}", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
            });

            var unitPriceField = page.GetByTestId("field-unit-price");
            await Assertions.Expect(unitPriceField).ToBeVisibleAsync();

            // Customer's seed restriction is Product.UnitPrice = ViewOnly,
            // so the wrapper passes ReadOnly to the MudNumericField. The
            // input element renders as disabled+readonly so the cursor
            // can't focus it.
            await Assertions.Expect(unitPriceField).ToBeDisabledAsync();
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task WarehouseUserRole_DoesNotSeeUnitPriceFieldOrColumnAsync()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, AdminEmail, roles: "WarehouseUser");

            await page.GotoAsync($"{_fixture.BaseUrl}/products", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
            });
            await PlaywrightFixture.WaitForGridDataAsync(page);

            await Assertions
                .Expect(page.Locator(".mud-table-head").GetByText("Unit Price"))
                .ToHaveCountAsync(0);

            var productId = await GetFirstProductIdAsync();
            await page.GotoAsync($"{_fixture.BaseUrl}/products/{productId}", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
            });

            await Assertions
                .Expect(page.GetByTestId("field-unit-price"))
                .ToHaveCountAsync(0);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }
}
