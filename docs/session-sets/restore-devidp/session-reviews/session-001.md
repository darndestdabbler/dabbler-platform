# Verification Round 1

# Session 1 Verification: `restore-devidp`

## Verdict: ✅ PASS

Session 1 meets all spec requirements. The DevIdP is correctly restored, builds clean, is wired into the solution, and serves valid OIDC metadata. Two advisory notes for Session 2 follow.

---

## Checklist

| Spec Requirement | Status | Evidence |
|---|---|---|
| Recover all files from `8730107^` | ✅ | 14 files restored across 6 subdirectories; no `Migrations/` expected (uses `EnsureCreatedAsync`) |
| Place under `src/Libs/Dabbler.Security.DevIdp/` | ✅ | All paths correct |
| `RootNamespace` correct without override | ✅ | SDK-default `Dabbler.Security.DevIdp` matches all `namespace` declarations |
| Add to `Dabbler.Platform.sln` | ✅ | `dotnet sln add` ran |
| `dotnet build` clean | ✅ | 0 errors; 184 warnings are pre-existing CS0618 from other code |
| Binds to `https://localhost:5100` | ✅ | `launchSettings.json` profile `"https"` → `applicationUrl: "https://localhost:5100"` |
| OIDC metadata serves correctly | ✅ | issuer, endpoints, JWKS URI, scopes all confirmed via `curl` |
| Tests pass | ✅ | 709 passed, 0 failed |

---

## File-by-File Review

### `Dabbler.Security.DevIdp.csproj`
- `Sdk="Microsoft.NET.Sdk.Web"` — correct for a standalone host.
- `net10.0` — consistent with the rest of the solution.
- Four `<PackageReference>` entries with no `Version` attributes — correctly delegates to `Directory.Packages.props` (spec confirms OpenIddict 7.4.0 is already there).
- No stale project references or leftover `samples/` paths.

### `Program.cs`
- Service registration order is correct: EF → Identity → OpenIddict → Razor Pages + Controllers.
- `UseOpenIddict()` on the `DbContextOptionsBuilder` — required for OpenIddict EF Core integration. ✅
- Auth code flow + PKCE only — matches the `acmecoffee-dev` client's `Permissions.GrantTypes.AuthorizationCode`. ✅
- `AddDevelopmentEncryptionCertificate()` + `AddDevelopmentSigningCertificate()` — appropriate for dev; auto-persisted by OpenIddict in the data store. ✅
- `DisableAccessTokenEncryption()` — dev-only convenience, consistent with dev signing cert. ✅
- `DisableTransportSecurityRequirement()` gated behind `IsDevelopment()`. ✅
- Startup seed: `EnsureCreatedAsync()` then `SeedData.SeedAsync()` in a scoped block. ✅
- Middleware order: `UseStaticFiles → UseRouting → UseAuthentication → UseAuthorization → MapRazorPages + MapControllers`. Correct.

### `Data/SeedData.cs`
- Roles seeded include all roles the AcmeCoffee backoffice expects (`SystemAdmin`, `BackofficeUserAdmin`, `BackofficeUser`). ✅
- 8 users with deterministic GUIDs and `EmailConfirmed = true`. Each stores a `"name"` claim for display. ✅
- **`acmecoffee-dev` client** registered with:
  - `ClientType = Confidential`, `ConsentType = Implicit` — correct for server-side Web app. ✅
  - `RedirectUris = { "https://localhost:7002/signin-EntraId" }` — callback path `/signin-EntraId` implies the Web app's OIDC scheme is named `EntraId` with an explicit `CallbackPath`. (Verified in Session 2.)
  - Permissions cover auth endpoint, token endpoint, auth-code grant, `code` response type, email/profile/roles scopes. ✅
- `SeedClientAsync` does `UpdateAsync` for existing clients — safe for repeated `dotnet run` cycles. ✅
- `roles` custom scope created in `SeedScopesAsync`. ✅

### `Controllers/AuthorizationController.cs`
- `/connect/authorize`: authenticates against Identity cookie → redirects to `/Login` if unauthenticated → builds claims via `EntraIdClaimsHandler` → signs in with OpenIddict server scheme. Standard OpenIddict pattern. ✅
- `/connect/token`: handles auth-code exchange only → rebuilds identity from DB for fresh claims. ✅
- `[IgnoreAntiforgeryToken]` on both — required because requests come from OIDC middleware, not Razor forms. ✅

### `Claims/EntraIdClaimsHandler.cs`
- Emits standard OIDC claims (`sub`, `name`, `email`, `preferred_username`) plus Entra ID claims (`oid`, `tid`, `roles`). ✅
- `GetDestinations` routes claims to access/identity tokens based on granted scopes. `oid`/`tid` always go to both tokens. `SecurityStamp` is excluded. ✅
- `SetDestinations(GetDestinations)` called after `SetScopes` — ordering is correct (OpenIddict needs scopes set before destination evaluation). ✅

### `Pages/Login.cshtml` + `Login.cshtml.cs`
- Standard email/password form posting to `OnPostAsync`.
- Uses `SignInManager.PasswordSignInAsync` — matches the `AddIdentity<IdentityUser, IdentityRole>()` registration. ✅
- `ReturnUrl` preserved as hidden field and validated with `Url.IsLocalUrl()` — no open-redirect. ✅
- `Layout = "_Layout"` resolves to `Pages/_Layout.cshtml` (same directory). ✅

### `Pages/_Layout.cshtml` + `_ViewImports.cshtml`
- Layout uses inline `<style>` — no `wwwroot/` dependency needed (the `UseStaticFiles()` call is harmless). ✅
- `_ViewImports.cshtml` sets `@namespace Dabbler.Security.DevIdp.Pages` — matches `LoginModel` namespace. ✅

### `Properties/launchSettings.json`
- Single profile `"https"` at `https://localhost:5100` with `ASPNETCORE_ENVIRONMENT=Development`. ✅
- Rename from original profile name to `"https"` matches the spec's helper command. ✅

### `appsettings.json` + `appsettings.Development.json`
- Connection string `Data Source=devidp.db` — file-based SQLite, zero-config. ✅
- `DevIdp:TenantId` = `"00000000-0000-0000-0000-000000000001"` — deterministic, matches `DevIdpOptions` default. ✅
- Dev config adds `OpenIddict: Debug` logging — helpful for Session 2 troubleshooting. ✅

---

## Advisory Notes for Session 2

### 1. Verify `acmecoffee-dev` redirect URI matches Web app

The registered redirect URI is `https://localhost:7002/signin-EntraId`. Session 2 must confirm:
- The Web app's HTTPS port is `7002`.
- The OIDC scheme's `CallbackPath` is set to `/signin-EntraId` (not the default `/signin-oidc`).

If either doesn't match, OpenIddict will reject the authorization request with `invalid_request` (redirect_uri mismatch). Fix is trivial: update the seed data or the Web app config.

### 2. `openid` scope not in client permissions (benign)

The `acmecoffee-dev` client's permission list includes `scp:email`, `scp:profile`, `scp:roles` but not `scp:openid`. This is **not a bug** — OpenIddict exempts the `openid` scope from client permission checks for authorization-code flows per the OIDC spec. Noting for awareness only; no action needed.

---

## Scope Boundaries Respected

The following are confirmed **not done** and correctly deferred to Session 2:
- `ShowSsoButton` remains `false` in `appsettings.Development.json`
- Safe-fail try/catch in `Program.cs` still present
- No Playwright test added
- No `docker-compose.app.yml` changes
- No end-to-end OIDC roundtrip exercised from the Web app
