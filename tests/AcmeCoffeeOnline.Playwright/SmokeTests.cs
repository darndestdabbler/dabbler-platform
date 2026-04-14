using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;
using Xunit.Sdk;

namespace AcmeCoffeeOnline.Playwright;

[Collection(PlaywrightCollection.Name)]
public sealed class SmokeTests
{
    private readonly PlaywrightFixture _fixture;

    public SmokeTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task HomePage_ShowsApiConnectivityAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await _fixture.SeededSignInAsync(page, "jdoe@acmecoffee.com");

            await page.GotoAsync(_fixture.BaseUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            await Assertions.Expect(page.GetByTestId("main-shell"))
                .ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("link[data-dabbler-asset='dabbler-shell.css']"))
                .ToHaveCountAsync(1);
            await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Welcome to AcmeCoffeeOnline" }))
                .ToBeVisibleAsync();
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task LoginPage_RendersAnonymousAuthShellAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await page.GotoAsync($"{_fixture.BaseUrl}/login", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            await Assertions.Expect(page.GetByTestId("auth-shell"))
                .ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("login-page"))
                .ToBeVisibleAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Sign In" }))
                .ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("main-shell"))
                .ToHaveCountAsync(0);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task MagicLinkFlow_SignsInCustomerAndLoadsOrdersAsync()
    {
        const string email = "james.smith@example.com";
        var page = await _fixture.CreatePageAsync();

        try
        {
            await _fixture.ClearMagicLinksAsync(email);

            await page.GotoAsync($"{_fixture.BaseUrl}/login", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            await page.GetByLabel("Email address").FillAsync(email);
            await page.GetByRole(AriaRole.Button, new() { Name = "Send sign-in link" }).ClickAsync();

            await Assertions.Expect(page.GetByText("We sent a sign-in code")).ToBeVisibleAsync();

            var magicLink = await _fixture.WaitForMagicLinkAsync(email);
            if (string.IsNullOrWhiteSpace(magicLink.LinkUrl))
            {
                throw new XunitException("Magic link payload did not include a link URL.");
            }

            Assert.Equal("MagicLink", magicLink.EmailType);

            await page.GotoAsync(magicLink.LinkUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            await Assertions.Expect(page.GetByTestId("main-shell")).ToBeVisibleAsync();

            await page.GotoAsync($"{_fixture.BaseUrl}/orders", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            await Assertions.Expect(page.GetByText("Orders")).ToBeVisibleAsync();

            var session = await _fixture.GetSessionInfoAsync(page);
            Assert.Contains("Customer", session.Roles ?? []);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task SeededSession_EnforcesPermissionsAndCustomerOrderScopingAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await _fixture.SeededSignInAsync(page, "james.smith@example.com");

            await page.GotoAsync($"{_fixture.BaseUrl}/customers", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            await Assertions.Expect(page.GetByTestId("access-denied-page")).ToBeVisibleAsync();

            var customerSession = await _fixture.GetSessionInfoAsync(page);
            Assert.Equal("james.smith@example.com", customerSession.Email);
            Assert.False(string.IsNullOrWhiteSpace(customerSession.UserId));

            using (var ordersPayload = await _fixture.GetApiJsonAsync(
                "api/orders?page=1&pageSize=5",
                customerSession.UserId!))
            {
                var items = ordersPayload.RootElement.GetProperty("items").EnumerateArray().ToArray();
                Assert.NotEmpty(items);
                Assert.All(items, item =>
                    Assert.Equal("James Smith", item.GetProperty("customerName").GetString()));
            }

            await _fixture.SignOutAsync(page);
            await _fixture.SeededSignInAsync(page, "jdoe@acmecoffee.com");

            await page.GotoAsync($"{_fixture.BaseUrl}/customers", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            await Assertions.Expect(page.GetByText("Customers")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("access-denied-page")).ToHaveCountAsync(0);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task ProductAdministration_TogglesAvailabilityAndPersistsToBackendAsync()
    {
        var page = await _fixture.CreatePageAsync();
        const string productName = "Blonde Roast - Decaf - Espresso";
        int? productId = null;
        bool? wasAvailable = null;
        string? userId = null;

        try
        {
            await _fixture.SeededSignInAsync(page, "admin@acmecoffee.com");

            var session = await _fixture.GetSessionInfoAsync(page);
            userId = session.UserId;
            Assert.False(string.IsNullOrWhiteSpace(userId));
            Assert.Contains("SystemAdmin", session.Roles ?? []);

            using var beforePayload = await _fixture.GetApiJsonAsync(
                $"api/products?page=1&pageSize=10&searchTerm={Uri.EscapeDataString(productName)}",
                userId!);

            var beforeProduct = beforePayload.RootElement
                .GetProperty("items")
                .EnumerateArray()
                .FirstOrDefault(item => item.GetProperty("name").GetString() == productName);

            Assert.Equal(productName, beforeProduct.GetProperty("name").GetString());

            productId = beforeProduct.GetProperty("id").GetInt32();
            wasAvailable = beforeProduct.GetProperty("isAvailable").GetBoolean();

            await _fixture.PatchApiAsync($"api/products/{productId}/availability", userId!);

            using var afterPayload = await _fixture.GetApiJsonAsync(
                $"api/products?page=1&pageSize=10&searchTerm={Uri.EscapeDataString(productName)}",
                userId!);

            var afterProduct = afterPayload.RootElement
                .GetProperty("items")
                .EnumerateArray()
                .First(item => item.GetProperty("id").GetInt32() == productId);

            Assert.Equal(productName, afterProduct.GetProperty("name").GetString());
            Assert.NotEqual(wasAvailable, afterProduct.GetProperty("isAvailable").GetBoolean());
        }
        finally
        {
            if (productId.HasValue && wasAvailable.HasValue && !string.IsNullOrWhiteSpace(userId))
            {
                using var finalPayload = await _fixture.GetApiJsonAsync(
                    $"api/products?page=1&pageSize=10&searchTerm={Uri.EscapeDataString(productName)}",
                    userId);

                var currentProduct = finalPayload.RootElement
                    .GetProperty("items")
                    .EnumerateArray()
                    .FirstOrDefault(item => item.GetProperty("id").GetInt32() == productId.Value);

                if (currentProduct.ValueKind != System.Text.Json.JsonValueKind.Undefined
                    && currentProduct.GetProperty("isAvailable").GetBoolean() != wasAvailable.Value)
                {
                    await _fixture.PatchApiAsync($"api/products/{productId.Value}/availability", userId);
                }
            }

            await _fixture.ClosePageAsync(page);
        }
    }
}
