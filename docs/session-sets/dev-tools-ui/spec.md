# Dev Tools UI Spec

> **Purpose:** Retire the UX of running PowerShell scripts against dev-only
> `/test-auth/*` endpoints by surfacing the same capabilities (Quick
> Sign-In, Session Info, Magic Links) in a persistent in-app Dev menu,
> gated on `Authentication.SeededSession.Enabled`. Relocate the existing
> Quick Sign-In from `Login.razor` so personas are reachable post-login.
> **Created:** 2026-04-17
> **Session Set:** `docs/session-sets/dev-tools-ui/`
> **Prerequisite:** `user-context-bar` session set complete (so `MainLayout`
> has settled with the new sentence component; the Dev menu will be
> inserted adjacent to it).
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Problem Statement

Developer affordances for the seeded-session test-auth surface are
fragmented:

- **Quick Sign-In** is on `Login.razor` only — unreachable after sign-in, so
  persona-hopping during UAT requires a logout round-trip.
- **Session inspection** requires running
  `scripts/full-app-uat/Get-SessionInfo.ps1` in a terminal.
- **Magic-link inspection** requires running
  `scripts/full-app-uat/Get-MagicLinks.ps1` in a terminal.

Both PowerShell scripts are thin wrappers around endpoints that the Blazor
app could call directly. They are useful for CI / headless scenarios but
should not be the primary developer UX. The folder name
`scripts/full-app-uat/` is also misleading — these are dev diagnostics, not
UAT flows.

## Goals

1. A persistent "Dev" menu in the AcmeCoffeeOnline app bar, visible only
   when `Authentication.SeededSession.Enabled` is true, that opens three
   actions: Quick Sign-In, Session Info, Magic Links.
2. All three actions call the existing `/test-auth/*` endpoints and match
   the server-side gate exactly.
3. PowerShell scripts are kept for headless use but relocated to
   `scripts/dev-tools/` with a short README steering developers to the
   in-app menu.
4. The Dev Quick Sign-In block is removed from `Login.razor`; the dialog
   version is the single source.

## Architecture Context

- Endpoints (all in `src/Apps/AcmeCoffeeOnline.Web/Program.cs`,
  ~lines 325-571):
  - `GET /test-auth/seeded-session?email=X` — sign in as persona
  - `GET /test-auth/session-info` — current session JSON
  - `GET /test-auth/magic-links[?email=X][&clear=true]` — dev email inbox
  - `GET /test-auth/switch-organization` — org switch (already used)
  - `GET /test-auth/sign-out`
- All endpoints require the `X-Test-Auth-Secret` header. Gate options live
  in `src/Apps/AcmeCoffeeOnline.Web/Configuration/AuthenticationOptions.cs`
  (`AppAuthenticationOptions.SeededSession`) with a validator enforcing
  Development/Test environments only.
- Current Dev Quick Sign-In: `Login.razor` lines 173-217 and code-behind at
  458-504. JS interop lives at `wwwroot/dev-quick-signin.js` and will be
  kept.
- Magic links are in-memory via
  `src/Libs/Dabbler.Security/Services/DevEmailInbox.cs`. No DB storage; no
  new endpoint needed.

---

## Session Plan

### Session 1 of 2: Build DevToolsMenu + three dialogs + TestAuthClient

**Steps:**
1. Read `docs/planning/guiding-principles.md` and
   `docs/planning/lessons-learned.md`.
2. Confirm server gate in `Program.cs` and option shape in
   `AuthenticationOptions.cs`. Decide how the shared secret reaches the
   client: preferred approach is a server-side `DelegatingHandler` that
   attaches the header — the secret must not be embedded in static assets.
3. Create `src/Apps/AcmeCoffeeOnline.Web/Services/DevTools/TestAuthClient.cs` —
   a typed `HttpClient` wrapping:
   - `GetSessionInfoAsync()`
   - `GetMagicLinksAsync(string? emailFilter)`
   - `ClearMagicLinksAsync()`
   - `SignInAsPersonaAsync(string email)`
   Register in `Program.cs` only when `SeededSession.Enabled`.
4. Create components under
   `src/Apps/AcmeCoffeeOnline.Web/Components/Shared/DevTools/`:
   - `DevToolsMenu.razor` — renders nothing when the gate is off; otherwise
     a `MudMenu` with "Dev" label + `BugReport` icon and three items.
   - `QuickSignInDialog.razor` — ports the persona list from `Login.razor`
     and reuses `wwwroot/dev-quick-signin.js` for the sign-in call.
   - `SessionInfoDialog.razor` — fetches session info, pretty-prints JSON
     in a scrollable `<pre>`; Refresh button.
   - `MagicLinksDialog.razor` — `MudTable` (Email, Subject/Preview, Sent,
     Actions). Per-row Copy-Link (via
     `navigator.clipboard.writeText` JS interop) and Open. Optional
     email-filter input; Clear-Inbox button hitting `?clear=true`.
5. Insert `<DevToolsMenu />` in `MainLayout.razor` just before
   `<UserContextBar />` so it lives in the app bar with consistent
   placement.
6. Tests:
   - bUnit `DevToolsMenuTests`: hidden when gate off; three items visible
     when on; each item opens its dialog.
   - bUnit `MagicLinksDialogTests`: renders rows from a stubbed
     `TestAuthClient`; filter input narrows rows; Copy-Link triggers JS
     interop.
   - Integration (`WebApplicationFactory`): `/test-auth/session-info` and
     `/test-auth/magic-links` respond with the header and return the
     shapes the UI consumes. Guards the contract.
7. `dotnet build`, `dotnet test`.

**Creates:**
- `Components/Shared/DevTools/DevToolsMenu.razor`
- `Components/Shared/DevTools/QuickSignInDialog.razor`
- `Components/Shared/DevTools/SessionInfoDialog.razor`
- `Components/Shared/DevTools/MagicLinksDialog.razor`
- `Services/DevTools/TestAuthClient.cs`
- `tests/AcmeCoffeeOnline.Web.Tests/Components/DevTools/*`

**Touches:**
- `src/Apps/AcmeCoffeeOnline.Web/Components/Layout/MainLayout.razor`
- `src/Apps/AcmeCoffeeOnline.Web/Program.cs` (conditional registration)

**Ends with:** Dev menu visible in Dev/Test envs, hidden in Prod; three
dialogs functional; tests green.
**Progress keys:** `session-1/confirm-gating`, `session-1/testauthclient`,
`session-1/devtoolsmenu`, `session-1/quicksignin-dialog`,
`session-1/sessioninfo-dialog`, `session-1/magiclinks-dialog`,
`session-1/mainlayout-wire`, `session-1/tests`,
`session-1/build-and-test`

---

### Session 2 of 2: Relocate scripts, remove Login dev block, doc refresh

**Steps:**
1. Read principles and Session 1 review.
2. Move `scripts/full-app-uat/Get-MagicLinks.ps1` and
   `Get-SessionInfo.ps1` to **`scripts/dev-tools/`**. The in-app "Dev
   Tools" naming matches; `scripts/full-app-uat/` stays focused on UAT
   flows.
3. Remove the Dev Quick Sign-In block from `Login.razor` lines 173-217 and
   the code-behind at 458-504. Keep `wwwroot/dev-quick-signin.js` — the
   dialog calls it.
4. Update `docs/session-sets/post-rebuild-uat/spec.md` Helper Usage section
   to point at `scripts/dev-tools/` and the in-app menu.
5. Grep for `full-app-uat/Get-MagicLinks` or `Get-SessionInfo` references
   in docs, READMEs, and `.github/workflows/`; update all.
6. Add `scripts/dev-tools/README.md` noting the in-app menu is the
   preferred UX and the scripts are headless / CI fallbacks.
7. Manual smoke: log in; Dev → Session Info (JSON matches
   `Get-SessionInfo.ps1` output); Dev → Magic Links (rows appear,
   Copy-Link works, Clear empties); Dev → Quick Sign-In (re-login as
   Customer).
8. `dotnet build`, `dotnet test`, Playwright suite.

**Creates:** `scripts/dev-tools/README.md`
**Touches:**
- Moved scripts
- `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Auth/Login.razor`
- `docs/session-sets/post-rebuild-uat/spec.md`
- Any other doc refs surfaced by grep
- `.github/workflows/*.yml` if they reference old paths

**Ends with:** Login page slimmed down; scripts in their new home; Dev
menu is the canonical developer surface.
**Progress keys:** `session-2/move-scripts`,
`session-2/remove-login-block`, `session-2/update-docs`,
`session-2/grep-refs`, `session-2/readme`, `session-2/manual-smoke`,
`session-2/build-and-test`

---

## First-Implementation Sketch

```razor
@* src/Apps/AcmeCoffeeOnline.Web/Components/Shared/DevTools/DevToolsMenu.razor *@
@inject IOptions<AppAuthenticationOptions> AuthOptions
@inject IDialogService DialogService

@if (AuthOptions.Value.SeededSession.Enabled)
{
    <MudMenu Icon="@Icons.Material.Filled.BugReport" Color="Color.Warning"
             Dense="true" Label="Dev" data-testid="dev-tools-menu"
             AnchorOrigin="Origin.BottomRight"
             TransformOrigin="Origin.TopRight">
        <MudMenuItem Icon="@Icons.Material.Filled.Login"
                     OnClick="@(() => Open<QuickSignInDialog>("Quick Sign-In"))"
                     data-testid="dev-tools-quick-signin">Quick Sign-In</MudMenuItem>
        <MudMenuItem Icon="@Icons.Material.Filled.Info"
                     OnClick="@(() => Open<SessionInfoDialog>("Session Info"))"
                     data-testid="dev-tools-session-info">Session Info</MudMenuItem>
        <MudMenuItem Icon="@Icons.Material.Filled.Email"
                     OnClick="@(() => Open<MagicLinksDialog>("Magic Links"))"
                     data-testid="dev-tools-magic-links">Magic Links</MudMenuItem>
    </MudMenu>
}

@code {
    private Task Open<TDialog>(string title) where TDialog : ComponentBase =>
        DialogService.ShowAsync<TDialog>(title,
            new DialogOptions { FullWidth = true, MaxWidth = MaxWidth.Medium });
}
```

## What This Set Preserves

| Existing Asset | Status |
|---|---|
| `/test-auth/*` endpoints | Unchanged |
| `DevEmailInbox` in-memory store | Unchanged |
| `wwwroot/dev-quick-signin.js` | Kept; caller moves to the new dialog |
| `AppAuthenticationOptions.SeededSession` gate + validator | Unchanged |
| All 75 Playwright tests and 517+ unit tests | Must stay green |
| PowerShell scripts | Moved, not deleted |

## Risk Notes

- **Gate parity.** The UI and server must read the same
  `SeededSession.Enabled` value. The shared secret lives server-side only;
  attach it via a server-side `DelegatingHandler`. Do not ship the secret
  to static assets or prod bundles.
- **Login-page selector drift.** Any test that targets
  `data-testid="dev-quick-signin-*"` on `/login` breaks in Session 2.
  Inventory during Session 1 and update in Session 2.
- **`dev-quick-signin.js` reuse.** Only its caller moves; confirm the
  `window.*` shape still works from dialog scope.
- **Script move paths.** Grep `.github/workflows/*.yml` and any dev docs
  for `scripts/full-app-uat/` hardcoded paths.
- **Deferred.** Environment-badge color on the Dev button (yellow Dev,
  red Test), persona sourcing from `security-config.json`, keyboard
  shortcut (`Ctrl+Shift+D`).
