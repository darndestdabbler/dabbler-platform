using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;

namespace AcmeCoffeeOnline.Playwright;

[Collection(PlaywrightCollection.Name)]
public sealed class CustomerBatchEntryWorkflowTests
{
    private readonly PlaywrightFixture _fixture;
    private static readonly string RunId = Guid.NewGuid().ToString("N")[..8];

    public CustomerBatchEntryWorkflowTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task BatchEntry_PageLoads_NoExpansionChromeAsync()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, "admin@acmecoffee.com");
            await page.GotoAsync($"{_fixture.BaseUrl}/customers/batch-entry", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            await Assertions.Expect(page.GetByTestId("batch-entry-title")).ToBeVisibleAsync();

            // ShowGrid=false — expansion panel chrome must not exist
            await Assertions.Expect(page.GetByTestId("batch-entry-shell-header")).Not.ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("batch-entry-shell-body")).Not.ToBeVisibleAsync();

            // Full-width details paper must be visible
            await Assertions.Expect(page.GetByTestId("batch-entry-shell-details")).ToBeVisibleAsync();
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task BatchEntry_Save_IncrementsCounterAndClearsFormAsync()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, "admin@acmecoffee.com");
            await page.GotoAsync($"{_fixture.BaseUrl}/customers/batch-entry", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            var email = $"batchentry-{RunId}-s2@test.local";

            await PlaywrightFixture.FillFormFieldAsync(page, "batch-first-name", "Batch");
            await PlaywrightFixture.FillFormFieldAsync(page, "batch-last-name", "UserOne");
            await PlaywrightFixture.FillFormFieldAsync(page, "batch-email", email);

            await page.GetByTestId("batch-entry-shell-save-button").ClickAsync();

            // Success snackbar
            await Assertions.Expect(page.Locator(".mud-snackbar")).ToContainTextAsync("Customer added.");

            // Session counter incremented
            await Assertions.Expect(page.GetByTestId("batch-entry-shell-session-summary"))
                .ToContainTextAsync("Records added this session: 1");

            // Form cleared
            var emailInput = await PlaywrightFixture.GetFormInputAsync(page, "batch-email");
            await Assertions.Expect(emailInput).ToHaveValueAsync(string.Empty);
            var firstNameInput = await PlaywrightFixture.GetFormInputAsync(page, "batch-first-name");
            await Assertions.Expect(firstNameInput).ToHaveValueAsync(string.Empty);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task BatchEntry_TwoRecords_BothPersistedInDbAsync()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            var userId = await _fixture.GetSeededUserIdAsync("admin@acmecoffee.com");
            await _fixture.SeededSignInAsync(page, "admin@acmecoffee.com");
            await page.GotoAsync($"{_fixture.BaseUrl}/customers/batch-entry", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            var email1 = $"batchentry-{RunId}-s3a@test.local";
            var email2 = $"batchentry-{RunId}-s3b@test.local";

            // Add first customer
            await PlaywrightFixture.FillFormFieldAsync(page, "batch-first-name", "Batch");
            await PlaywrightFixture.FillFormFieldAsync(page, "batch-last-name", "UserOne");
            await PlaywrightFixture.FillFormFieldAsync(page, "batch-email", email1);
            await page.GetByTestId("batch-entry-shell-save-button").ClickAsync();
            await Assertions.Expect(page.GetByTestId("batch-entry-shell-session-summary"))
                .ToContainTextAsync("Records added this session: 1");

            // Add second customer
            await PlaywrightFixture.FillFormFieldAsync(page, "batch-first-name", "Batch");
            await PlaywrightFixture.FillFormFieldAsync(page, "batch-last-name", "UserTwo");
            await PlaywrightFixture.FillFormFieldAsync(page, "batch-email", email2);
            await page.GetByTestId("batch-entry-shell-save-button").ClickAsync();
            await Assertions.Expect(page.GetByTestId("batch-entry-shell-session-summary"))
                .ToContainTextAsync("Records added this session: 2");

            // Verify both persisted in DB via API
            var customers = await _fixture.GetCustomersAsync(userId, 1, 1000);
            Assert.Contains(customers, c => c.Email == email1);
            Assert.Contains(customers, c => c.Email == email2);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }
}
