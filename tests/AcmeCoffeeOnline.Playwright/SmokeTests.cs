using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;
using System.Text.RegularExpressions;
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
            await Assertions.Expect(page.GetByTestId("routed-page-content")
                    .GetByRole(AriaRole.Heading, new() { Name = "Welcome to AcmeCoffeeDistributors" }))
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
    public async Task MagicLinkFlow_SignsInRetailBusinessUserAndLoadsOrdersAsync()
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

            await Assertions.Expect(page)
                .ToHaveURLAsync(new Regex(".*/orders$"));
            await Assertions.Expect(page.GetByTestId("access-denied-page"))
                .ToHaveCountAsync(0);

            var session = await _fixture.GetSessionInfoAsync(page);
            Assert.Contains("Customer", session.Roles ?? []);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task SeededSession_EnforcesPermissionsAndRetailBusinessOrderScopingAsync()
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

            var retailBusinessSession = await _fixture.GetSessionInfoAsync(page);
            Assert.Equal("james.smith@example.com", retailBusinessSession.Email);
            Assert.False(string.IsNullOrWhiteSpace(retailBusinessSession.UserId));

            using (var ordersPayload = await _fixture.GetApiJsonAsync(
                "api/orders?page=1&pageSize=5",
                retailBusinessSession.UserId!))
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

            await Assertions.Expect(page)
                .ToHaveURLAsync(new Regex(".*/customers$"));
            await Assertions.Expect(page.GetByTestId("access-denied-page")).ToHaveCountAsync(0);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task AdminNavigation_UsesFocusedProductsEntryAndActualRolePermissionsAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await _fixture.SeededSignInAsync(page, "kbrown@acmecoffee.com");

            await page.GotoAsync(_fixture.BaseUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            await Assertions.Expect(page.GetByText("Catalog", new() { Exact = true }))
                .ToHaveCountAsync(0);

            await page.GotoAsync($"{_fixture.BaseUrl}/products", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(".*/products$"));
            await Assertions.Expect(page.GetByTestId("access-denied-page")).ToHaveCountAsync(0);

            await page.GotoAsync($"{_fixture.BaseUrl}/products/catalog", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(".*/products/catalog$"));
            await Assertions.Expect(page.GetByTestId("access-denied-page")).ToHaveCountAsync(0);

            await page.GotoAsync($"{_fixture.BaseUrl}/admin/users", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(".*/admin/users$"));
            await Assertions.Expect(page.GetByTestId("access-denied-page")).ToHaveCountAsync(0);

            await page.GotoAsync($"{_fixture.BaseUrl}/admin/products", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });
            await Assertions.Expect(page.GetByTestId("access-denied-page")).ToBeVisibleAsync();

            await page.GotoAsync($"{_fixture.BaseUrl}/admin/security", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });
            await Assertions.Expect(page.GetByTestId("access-denied-page")).ToBeVisibleAsync();

            await _fixture.SignOutAsync(page);
            await _fixture.SeededSignInAsync(page, "admin@acmecoffee.com");

            await page.GotoAsync($"{_fixture.BaseUrl}/admin/products", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(".*/admin/products$"));
            await Assertions.Expect(page.GetByTestId("access-denied-page")).ToHaveCountAsync(0);

            await page.GotoAsync($"{_fixture.BaseUrl}/admin/security", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(".*/admin/security$"));
            await Assertions.Expect(page.GetByTestId("access-denied-page")).ToHaveCountAsync(0);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

}
