using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace AcmeCoffeeOnline.Playwright;

/// <summary>
/// End-to-end coverage of the "Sign in with your organization account" SSO
/// happy path. The Web app's OIDC scheme `EntraId` is configured against the
/// local DevIdp at https://localhost:5100. The fixture launches DevIdp on
/// demand alongside the API and Web processes already started for other
/// tests in the suite. The browser context tolerates the DevIdp's
/// self-signed dev cert so the redirect chain to /connect/authorize and
/// back through /signin-EntraId can complete unmodified.
/// </summary>
[Collection(PlaywrightCollection.Name)]
public sealed class SsoOidcWorkflowTests
{
    private readonly PlaywrightFixture _fixture;

    public SsoOidcWorkflowTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    // Skipped pending the Docker cross-container OIDC issuer/metadata wiring
    // described in `docs/Dockerfile.DevIdp` and the `restore-devidp` change
    // log. Running this test today requires:
    //   1. PLAYWRIGHT_BASE_URL=http://localhost:5020 (Web on dotnet-run http
    //      profile, not Docker host-port 5010), and
    //      PLAYWRIGHT_API_BASE_URL=http://localhost:5021 (API on dotnet-run
    //      http profile, not Docker host-port 5011),
    //   2. `dotnet dev-certs https --trust` (otherwise the back-channel
    //      probe of https://localhost:5100/.well-known/openid-configuration
    //      fails despite the runtime PostConfigure that trusts localhost),
    //   3. LocalDB available so the auto-launched Web/API can attach.
    // The visible feature (button → /auth/challenge/EntraId → DevIdp) is
    // covered by the redirect-URI-and-config wire-up exercised by
    // Dabbler.Security.Blazor.Tests `LoginPageOidcTests`. Re-enable this
    // [Fact] in the follow-on session that aligns the OIDC issuer for
    // Docker compose so the Web container's metadata fetch and the
    // browser's redirect target both resolve to the same authority.
    [Fact(Skip = "Requires either local-run env vars (see comment above) or " +
        "the Docker cross-container OIDC issuer wiring planned as a follow-on " +
        "to the restore-devidp set.")]
    public async Task SsoLogin_BackofficeUserAdmin_LandsOnDashboardAsync()
    {
        await _fixture.EnsureDevIdpRunningAsync();

        var page = await _fixture.CreatePageAsync(ignoreHttpsErrors: true);
        try
        {
            // 1. Load the login page and confirm the SSO button is visible.
            await page.GotoAsync($"{_fixture.BaseUrl}/login", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            var ssoButton = page.GetByRole(AriaRole.Link, new PageGetByRoleOptions
            {
                Name = "Sign in with your organization account"
            });
            await Assertions.Expect(ssoButton).ToBeVisibleAsync();

            // 2. Click the SSO button. The Web app's /auth/challenge/EntraId
            //    endpoint issues a 302 to the DevIdp's /connect/authorize.
            await ssoButton.ClickAsync();
            await page.WaitForURLAsync(
                new System.Text.RegularExpressions.Regex("localhost:5100/.*"),
                new PageWaitForURLOptions { Timeout = 15000 });

            // 3. The DevIdp's /connect/authorize redirects unauthenticated users
            //    to /Login. Fill in the seeded backoffice-admin credentials and
            //    submit. After OpenIddict consents (ConsentType.Implicit), the
            //    DevIdp redirects back to the Web's /signin-EntraId callback.
            await page.GetByLabel("Email").FillAsync("backoffice-admin@acmecoffee.local");
            await page.GetByLabel("Password").FillAsync("Pass123!");
            await page
                .GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Sign In" })
                .ClickAsync();

            // 4. The browser is redirected back through the OIDC scheme's
            //    /signin-EntraId callback path, then through the Web's
            //    /auth/external-callback handler, and finally to the dashboard.
            await page.WaitForURLAsync(
                new System.Text.RegularExpressions.Regex(@".*/dashboard$"),
                new PageWaitForURLOptions { Timeout = 30000 });

            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // 5. Verify the user is signed in by checking the session-info
            //    helper. The seeded user is auto-provisioned with the default
            //    "Customer" role on first SSO arrival; the email/displayName
            //    flow comes from the DevIdp's seeded `name` claim.
            var session = await _fixture.GetSessionInfoAsync(page);
            Assert.True(session.IsAuthenticated, "Session should be authenticated after SSO sign-in.");
            Assert.Equal("backoffice-admin@acmecoffee.local", session.Email);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }
}
