# Restore Dev IdP Spec

> **Purpose:** Restore the OpenIddict-based `Dabbler.Security.DevIdp` so the
> "Sign in with your organization account" button completes real OIDC in local
> development and Test environments.
> **Created:** 2026-04-17
> **Session Set:** `docs/session-sets/restore-devidp/`
> **Workflow:** Two sessions — restore + wire up.

---

## Context

The AcmeCoffeeOnline Web app has always had a "Sign in with your organization
account" button on the login page that performs an OIDC challenge against
`https://localhost:5100`. That endpoint used to be served by an OpenIddict
project named **`Dabbler.Security.DevIdp`** under
`samples/Dabbler.Security.Sample/Dabbler.Security.DevIdp/`.

The `samples/` folder, including the Dev IdP, was deleted in commit
`8730107` ("Repository cleanup"). The `DabblerSecurity:Oidc` configuration
keys that point at `https://localhost:5100` remain in
`appsettings.Development.json`, so the OIDC scheme still registers but has
nothing to talk to. Before the `signin-unblock` change this meant clicking
the SSO button raised `HttpRequestException` at OIDC metadata discovery.

The immediately-preceding `signin-unblock` change:

- Restored Dev Quick Sign-In on the login page (bypasses OIDC entirely).
- Added a `ShowSsoButton` flag on `AppAuthenticationOptions`, defaulting to
  `false`. The SSO button is hidden until this flag is flipped.
- Wrapped `/auth/challenge/{scheme}` in a try/catch that redirects to
  `/login?error=sso-unavailable` instead of crashing, as a belt-and-suspenders
  guard for any direct URL hit.

This session set completes the work by restoring the DevIdp project itself
so SSO can be turned back on in Dev and Test.

## Prerequisites

- `signin-unblock` change is merged (so the current local sign-in path
  works and users are not blocked).
- OpenIddict 7.4.0 package references already exist in
  `Directory.Packages.props:26–27`. No new package additions needed.
- `DabblerSecurity:Oidc` keys in `appsettings.Development.json` remain
  pointed at `https://localhost:5100`.

## What This Session Set Covers

- **Project restoration.** Pull `Dabbler.Security.DevIdp` source back from
  commit `8730107^`. Relocate it under `src/Libs/Dabbler.Security.DevIdp/`
  (matches the current source layout — the library-adjacent folder, not a
  `samples/` area).
- **Solution wire-up.** Re-add the project to `Dabbler.Platform.sln`.
  Verify it builds clean alongside everything else.
- **Runtime wire-up.** Verify the restored IdP binds to
  `https://localhost:5100`, has the correct client registration for
  `acmecoffee-dev`, and issues tokens that the Web app's OIDC middleware
  accepts.
- **Enable the SSO button.** Flip `ShowSsoButton` to `true` in
  `appsettings.Development.json` (and Test config, if present). Remove the
  safe-fail redirect in `Program.cs` once SSO is confirmed working.
- **Playwright coverage.** Add an end-to-end Playwright test that exercises
  the full SSO happy path from the login page through OIDC to the dashboard.
- **Docker (if applicable).** If `docker/docker-compose.yml` runs the Web
  and API containers in a Dev-like stack, add the DevIdp container so the
  browser-driven flows in Docker can use SSO too.

## What This Session Set Does NOT Cover

- Production Entra ID configuration. The DevIdp is for Dev and Test only.
- Re-organization of the `Dabbler.Security` library surface itself.
- Any new features in the DevIdp beyond what commit `8730107^` contained.

## Session Plan

### Session 1 of 2: Restore project and verify it builds and runs

**Steps:**

1. Read `docs/planning/project-guidance.md` and
   `docs/planning/lessons-learned.md`.
2. Use `git show 8730107^:samples/Dabbler.Security.Sample/Dabbler.Security.DevIdp/...`
   to recover every file that was in the DevIdp project (`.csproj`, all
   `.cs`, any `appsettings*.json`, `Program.cs`, `Migrations/`, cert helpers,
   and any adjacent assets).
3. Relocate the recovered files into `src/Libs/Dabbler.Security.DevIdp/`.
   Update the csproj `RootNamespace`, project references, and asset
   references as needed for the new path.
4. Re-add the project to `Dabbler.Platform.sln` via `dotnet sln add`.
5. Confirm `dotnet build Dabbler.Platform.sln` is clean.
6. Launch the DevIdp locally and confirm it serves
   `https://localhost:5100/.well-known/openid-configuration`.

**Creates:** `src/Libs/Dabbler.Security.DevIdp/` (all files)
**Touches:** `Dabbler.Platform.sln`
**Ends with:** DevIdp builds clean and serves OIDC metadata.
**Progress keys:** `session-1/restore-source`, `session-1/solution-wireup`,
`session-1/build-and-run`

### Session 2 of 2: Wire up SSO, enable in Dev/Test, add Playwright coverage

**Steps:**

1. Read `docs/planning/project-guidance.md` and
   `docs/planning/lessons-learned.md`.
2. Launch DevIdp alongside Web and API locally. Exercise the OIDC code flow
   end-to-end from `/auth/challenge/EntraId` through external callback to the
   dashboard. Capture evidence (screenshots or a short script log).
3. Flip `Authentication:ShowSsoButton` to `true` in
   `appsettings.Development.json` (and `Test` config if present). Verify the
   button appears on `/login` and completes sign-in.
4. Remove the safe-fail try/catch from the `/auth/challenge/{scheme}` handler
   in `Program.cs`, and remove the `"sso-unavailable"` case from the Login
   page's `_queryError` mapping. These were belt-and-suspenders for the
   absent DevIdp and are no longer needed.
5. Add a Playwright test under `tests/AcmeCoffeeOnline.Playwright/` that:
   - Visits `/login`.
   - Clicks "Sign in with your organization account".
   - Completes the OIDC form on the DevIdp with a seeded dev user.
   - Lands on the dashboard as the expected user.
6. If `docker/docker-compose.yml` exists and runs the Dev stack, add a
   `devidp` service binding the same port so Playwright-in-Docker also gets
   a live SSO flow.
7. Run `dotnet test Dabbler.Platform.sln` and the Playwright suite; confirm
   all green with the new test added.

**Creates:** New Playwright test file
**Touches:** `appsettings.Development.json`,
`src/Apps/AcmeCoffeeOnline.Web/Program.cs`,
`src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Auth/Login.razor`,
possibly `docker/docker-compose.yml`
**Ends with:** SSO works end-to-end in Dev; Playwright covers the happy path.
**Progress keys:** `session-2/oidc-roundtrip`, `session-2/enable-sso`,
`session-2/retire-safe-fail`, `session-2/playwright-coverage`,
`session-2/docker` (if applicable)

## Helper Usage

- Local run commands:
  ```bash
  dotnet run --project src/Libs/Dabbler.Security.DevIdp --launch-profile https
  dotnet run --project src/Apps/AcmeCoffeeOnline.Api --launch-profile http
  dotnet run --project src/Apps/AcmeCoffeeOnline.Web --launch-profile http
  ```
- OIDC discovery check:
  ```
  curl -k https://localhost:5100/.well-known/openid-configuration
  ```

## Remediation Flow

If the restored project has drifted from what current OpenIddict 7.4.0 or
the Web app expects (e.g., new required options, migration-schema changes),
capture each blocker in the session's activity log, fix it inline if
possible, and surface any durable lessons via
`docs/planning/lessons-learned.md`.
