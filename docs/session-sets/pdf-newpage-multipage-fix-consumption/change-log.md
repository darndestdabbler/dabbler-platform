# pdf-newpage-multipage-fix-consumption

## Summary

This session set integrates DabblerPdf 1.1.1 into dabbler-platform, enhances PDF export testing with rendering-aware assertions, and adds Playwright-driven end-to-end validation for multi-page exports. Finding 2 has been re-opened and correctly closed.

## Session 1 of 1: Upgrade, caller coverage, Playwright coverage, close Finding 2

- `dabbler-platform` now consumes DabblerPdf 1.1.1 via `Directory.Packages.props`.
- `tests/Dabbler.Reports.Blazor.Tests/CallerLevelPdfExportTests.cs` now uses PdfPig for rendering-aware per-physical-page assertions and checks that physical page count equals composed logical page count for a multi-page export.
- `tests/AcmeCoffeeOnline.Playwright/ReportsWorkflowTests.cs` now drives the real Orders by Retail Business export button, captures the downloaded PDF, asserts physical page 1 contains the title and `Page 1 of`, and compares PdfPig page count to the logical page count surfaced by `DabblerReportPage`.
- `src/Libs/Dabbler.Reports.Blazor/DabblerReportPage.razor` now exposes a stable test hook for the report page count used by the Playwright assertion.

## Files Changed

- `Directory.Packages.props`
- `src/Libs/Dabbler.Reports.Blazor/DabblerReportPage.razor`
- `tests/Dabbler.Reports.Blazor.Tests/Dabbler.Reports.Blazor.Tests.csproj`
- `tests/Dabbler.Reports.Blazor.Tests/CallerLevelPdfExportTests.cs`
- `tests/AcmeCoffeeOnline.Playwright/AcmeCoffeeOnline.Playwright.csproj`
- `tests/AcmeCoffeeOnline.Playwright/ReportsWorkflowTests.cs`

## Verification

VERIFIED.

Validation passed for:

- `dotnet build Dabbler.Platform.sln`
- `dotnet test tests/Dabbler.Reports.Blazor.Tests/Dabbler.Reports.Blazor.Tests.csproj --filter FullyQualifiedName~CallerLevelPdfExportTests`
- `PLAYWRIGHT_BASE_URL=http://localhost:5020 PLAYWRIGHT_API_BASE_URL=http://localhost:5021 dotnet test tests/AcmeCoffeeOnline.Playwright/AcmeCoffeeOnline.Playwright.csproj --filter FullyQualifiedName~OrdersByCustomer_PdfExport_DownloadMatchesRenderedPageCountAsync`
- `PLAYWRIGHT_BASE_URL=http://localhost:5020 PLAYWRIGHT_API_BASE_URL=http://localhost:5021 dotnet test Dabbler.Platform.sln --filter FullyQualifiedName~PdfExport`

Full-solution and full-Playwright runs both failed only on the unrelated pre-existing test `SecurityDashboardUatFollowupCoverageTests.SecurityNav_BakedInUserPersona_RealLoginFlow_NavVisibleRoleDocOnlyAsync` waiting for an Admin button.

## Finding 2 Re-opened And Corrected

The earlier `pdf-info-metadata-uat-remediation` disposition calling the issue a stale runtime was incorrect. The 2026-04-22 field report disproved that closure. The corrected closure is the new rendering-aware PdfPig assertion proving visible page-1 text rather than mere string presence in compressed PDF bytes.
