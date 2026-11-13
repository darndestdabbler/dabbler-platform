# Dev Tools UI Change Log

This work retired the PowerShell-based developer workflow for seeded-session authentication by creating a persistent, in-app "Dev" menu. The new UI, gated on `Authentication.SeededSession.Enabled`, centralizes Quick Sign-In, Session Info, and Magic Link inspection, while relocating the original Quick Sign-In UI from the login page to a reusable dialog accessible post-login.

## Session 1 Outcome

- A new `DevToolsMenu` component was added to the main layout's app bar, conditionally rendered in Development/Test environments.
- The menu opens three new dialog components: `QuickSignInDialog` (for persona switching), `SessionInfoDialog` (for viewing session state as JSON), and `MagicLinksDialog` (for inspecting the in-memory dev email inbox).
- A `TestAuthClient` service was created to provide data to the dialogs by calling internal services directly, deviating from the spec's `HttpClient` proposal to simplify Blazor Server state management.
- Nineteen new unit and integration tests were added to cover the new components, service logic, and endpoint contracts.

## Session 2 Outcome

- The developer Quick Sign-In UI was removed from `Login.razor`, making the new `QuickSignInDialog` the single source of truth.
- The `Get-MagicLinks.ps1` and `Get-SessionInfo.ps1` PowerShell scripts were moved from `scripts/full-app-uat/` to `scripts/dev-tools/`.
- A `README.md` was added to `scripts/dev-tools/`, directing developers to the in-app menu as the primary workflow and positioning the scripts as headless/CI alternatives.
- All documentation and specs referencing the old script paths were updated.

## Files Created / Moved / Removed / Modified

*   **Created**
    *   `src/Apps/AcmeCoffeeOnline.Web/Components/Shared/DevTools/DevToolsMenu.razor`
    *   `src/Apps/AcmeCoffeeOnline.Web/Components/Shared/DevTools/QuickSignInDialog.razor`
    *   `src/Apps/AcmeCoffeeOnline.Web/Components/Shared/DevTools/SessionInfoDialog.razor`
    *   `src/Apps/AcmeCoffeeOnline.Web/Components/Shared/DevTools/MagicLinksDialog.razor`
    *   `src/Apps/AcmeCoffeeOnline.Web/Services/DevTools/TestAuthClient.cs`
    *   `tests/AcmeCoffeeOnline.Web.Tests/Components/DevTools/*` (4 test files)
    *   `scripts/dev-tools/README.md`
*   **Moved**
    *   `scripts/full-app-uat/Get-MagicLinks.ps1` → `scripts/dev-tools/Get-MagicLinks.ps1`
    *   `scripts/full-app-uat/Get-SessionInfo.ps1` → `scripts/dev-tools/Get-SessionInfo.ps1`
*   **Removed**
    *   `scripts/full-app-uat/` (directory)
    *   Dev Quick Sign-In Razor and C# code from `Login.razor`
*   **Modified**
    *   `src/Apps/AcmeCoffeeOnline.Web/Components/Layout/MainLayout.razor`
    *   `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Auth/Login.razor`
    *   `src/Apps/AcmeCoffeeOnline.Web/Program.cs`
    *   `docs/session-sets/post-rebuild-uat/spec.md`
    *   `docs/session-sets/security-dashboard-uat/spec.md`
    *   `docs/session-sets/packaging-and-template-readiness/spec.md`

## Verification Results

- **Session 1:** Initial review found a critical issue (shared secret sent to browser JS), which was deferred with rationale (dev-only feature preserving an existing pattern). Other major and minor issues were fixed or deferred. The session passed verification after remediation.
- **Session 2:** Passed verification on the first attempt. All spec steps were fulfilled, and the full Playwright suite (75/75 tests) passed, confirming no regressions from the `Login.razor` modifications.

## Deferred Items

- Sourcing the Quick Sign-In persona list from a configuration file instead of a hardcoded list.
- Adding a visual environment badge (e.g., yellow for Dev) to the Dev menu button.
- Implementing a keyboard shortcut to open the Dev menu.
- Adding bUnit tests for the `MudMenu` click-to-open-dialog behavior, due to a testing library limitation. This is covered by E2E tests.

## Known Issues

- The `QuickSignInDialog` sends the `X-Test-Auth-Secret` to the browser's JavaScript runtime to make an HTTP request, which is the same pattern used by the now-removed `Login.razor` implementation. This is an accepted risk for a developer-only feature gated from production environments.