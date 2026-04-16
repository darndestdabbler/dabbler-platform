using System.Text.RegularExpressions;
using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;

namespace AcmeCoffeeOnline.Playwright;

[Collection(PlaywrightCollection.Name)]
public sealed class ReportsWorkflowTests
{
    private readonly PlaywrightFixture _fixture;

    public ReportsWorkflowTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task SignInAndLoadShellAsync(IPage page)
    {
        await _fixture.SeededSignInAsync(page, "admin@acmecoffee.com");
        await page.GotoAsync($"{_fixture.BaseUrl}/dashboard", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });
        await Assertions.Expect(page.GetByTestId("main-shell")).ToBeVisibleAsync();
    }

    /// <summary>
    /// Fills a MudDatePicker date field by removing the readonly attribute,
    /// filling the text value, and dispatching a change event so Blazor Server
    /// processes the date parse.
    /// </summary>
    private static async Task FillDatePickerAsync(IPage page, string label, string dateText)
    {
        var input = page.GetByLabel(label).First;
        await input.EvaluateAsync("el => el.readOnly = false");
        await input.FillAsync(dateText);
        await input.EvaluateAsync("el => el.dispatchEvent(new Event('change', { bubbles: true }))");
        await Task.Delay(300);
    }

    private static async Task FillReportDatesAsync(IPage page)
    {
        await FillDatePickerAsync(page, "Start Date", "01/01/2025");
        await FillDatePickerAsync(page, "End Date", "12/31/2025");
    }

    private static async Task ClickRunReportAsync(IPage page)
    {
        var runButton = page.GetByRole(AriaRole.Button, new() { Name = "Run Report" });
        await Assertions.Expect(runButton).ToBeEnabledAsync(new() { Timeout = 5000 });
        await runButton.ClickAsync();
    }

    private static async Task WaitForReportOutputAsync(IPage page)
    {
        // Wait for the empty state message to disappear, meaning the report rendered
        await Assertions.Expect(page.GetByText("Set parameters and click"))
            .ToHaveCountAsync(0, new() { Timeout = 30000 });
    }

    [Fact]
    public async Task OrdersByCustomer_NavigateAndRunReport_OutputRendersAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await SignInAndLoadShellAsync(page);

            await _fixture.NavigateViaDrawerGroupAsync(
                page, "Reports", "Orders by Retail Business", ".*/reports/orders-by-customer$");

            // Verify report page loaded — scope to routed content to avoid drawer match
            var routedContent = page.GetByTestId("routed-page-content");
            await Assertions.Expect(routedContent.GetByText("Orders by Retail Business").First)
                .ToBeVisibleAsync();

            // Verify empty state before running
            await Assertions.Expect(page.GetByText("Set parameters and click")).ToBeVisibleAsync();

            // Fill required date parameters and run
            await FillReportDatesAsync(page);
            await ClickRunReportAsync(page);
            await WaitForReportOutputAsync(page);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task OrdersByCustomer_PdfExport_TriggersDownloadAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await SignInAndLoadShellAsync(page);

            await _fixture.NavigateViaDrawerGroupAsync(
                page, "Reports", "Orders by Retail Business", ".*/reports/orders-by-customer$");

            // PDF button should be disabled before report runs
            var pdfButton = page.GetByRole(AriaRole.Button, new() { Name = "Export to PDF" });
            await Assertions.Expect(pdfButton).ToBeDisabledAsync();

            // Fill dates and run report
            await FillReportDatesAsync(page);
            await ClickRunReportAsync(page);
            await WaitForReportOutputAsync(page);

            // PDF button should now be enabled
            await Assertions.Expect(pdfButton).ToBeEnabledAsync();

            // Click PDF export and verify download starts
            var downloadTask = page.WaitForDownloadAsync();
            await pdfButton.ClickAsync();
            var download = await downloadTask;

            Assert.Contains("orders-by-customer", download.SuggestedFilename, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task SalesByProduct_NavigateAndRunReport_OutputRendersAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await SignInAndLoadShellAsync(page);

            await _fixture.NavigateViaDrawerGroupAsync(
                page, "Reports", "Sales by Product", ".*/reports/sales-by-product$");

            // Verify report page loaded — scope to routed content
            var routedContent = page.GetByTestId("routed-page-content");
            await Assertions.Expect(routedContent.GetByText("Sales by Product").First)
                .ToBeVisibleAsync();

            // Fill required date parameters and run
            await FillReportDatesAsync(page);
            await ClickRunReportAsync(page);
            await WaitForReportOutputAsync(page);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task SalesByProduct_PdfExport_TriggersDownloadAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await SignInAndLoadShellAsync(page);

            await _fixture.NavigateViaDrawerGroupAsync(
                page, "Reports", "Sales by Product", ".*/reports/sales-by-product$");

            // PDF button should be disabled before report runs
            var pdfButton = page.GetByRole(AriaRole.Button, new() { Name = "Export to PDF" });
            await Assertions.Expect(pdfButton).ToBeDisabledAsync();

            // Fill dates and run report
            await FillReportDatesAsync(page);
            await ClickRunReportAsync(page);
            await WaitForReportOutputAsync(page);

            // PDF button should be enabled after report runs
            await Assertions.Expect(pdfButton).ToBeEnabledAsync();

            // Click PDF export and verify download starts
            var downloadTask = page.WaitForDownloadAsync();
            await pdfButton.ClickAsync();
            var download = await downloadTask;

            Assert.Contains("sales-by-product", download.SuggestedFilename, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task ReportValidation_MissingDates_RunButtonDisabledAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await SignInAndLoadShellAsync(page);

            await _fixture.NavigateViaDrawerGroupAsync(
                page, "Reports", "Orders by Retail Business", ".*/reports/orders-by-customer$");

            // Without filling required date parameters, Run Report should be disabled
            var runButton = page.GetByRole(AriaRole.Button, new() { Name = "Run Report" });
            await Assertions.Expect(runButton).ToBeVisibleAsync();
            await Assertions.Expect(runButton).ToBeDisabledAsync();

            // Fill only the start date — end date still missing
            await FillDatePickerAsync(page, "Start Date", "01/01/2025");

            // Run button should still be disabled with only one required field filled
            await Assertions.Expect(runButton).ToBeDisabledAsync();

            // Now fill end date too
            await FillDatePickerAsync(page, "End Date", "12/31/2025");

            // Run button should now be enabled
            await Assertions.Expect(runButton).ToBeEnabledAsync();
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }
}
