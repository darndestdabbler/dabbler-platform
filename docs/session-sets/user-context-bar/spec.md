# User Context Bar Spec

> **Purpose:** Replace the AcmeCoffeeOnline app-bar User/Role/Org cluster
> (active-org label, Switch-Org button, DabblerRoleSwitcher, user-icon menu)
> with a single sentence — "{UserName} as {Role} for {Organization}" — where
> each name is a hyperlink whose enabled state reflects the user's
> capabilities.
> **Created:** 2026-04-17
> **Session Set:** `docs/session-sets/user-context-bar/`
> **Prerequisite:** `post-rebuild-uat` remediation complete;
> `security-dashboard-wiring` complete. This work was deferred during
> post-rebuild-uat and tracked as an open UX issue ever since.
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Problem Statement

The app bar today renders four disjoint controls that together occupy
significant horizontal space and duplicate information:

- An "Active org" label with the org name
- A Switch-Org `MudMenu` button (shown when 2+ orgs or SystemAdmin)
- A `DabblerRoleSwitcher` component (shown when role switching is allowed)
- A user-icon `MudMenu` dropdown with display name, org, roles, Sign Out

The human reviewer called this "awkward" and "unprofessional." The preferred
design is a single sentence:

> **Casey Hart** as **System Admin** for **Internal Operations**

- UserName link is always enabled; clicking it opens a Sign Out confirmation
  dialog.
- Role link is enabled only when the user's role allows role switching
  (configured via `RoleSwitching.AllowedRoles` in `security-config.json` —
  today SystemAdmin and BackofficeUserAdmin).
- Org link is enabled only when the user has 2+ org memberships.
- `DabblerRoleSwitcher` stays in the Dabbler.Security.Blazor library for
  other consumers; it is removed from AcmeCoffeeOnline.Web's `MainLayout`.

## Architecture Context

- Current cluster lives in
  `src/Apps/AcmeCoffeeOnline.Web/Components/Layout/MainLayout.razor`
  (lines 14-85).
- Session/identity data is loaded by `ShellSessionStateLoader` and
  `OrganizationOptionsApiClient` (both under
  `src/Apps/AcmeCoffeeOnline.Web/Services/`).
- Role switching uses `IRoleSwitcher` / `SessionRoleSwitcher`
  (`src/Libs/Dabbler.Security/Services/`).
- Org switching uses the existing redirect flow
  `/auth/switch-organization?organizationId={id}&returnUrl={url}`.
- Tests that must remain green:
  - Playwright `MultiOrganizationWorkflowTests` (selectors may need updates)
  - bUnit `DabblerRoleSwitcherTests` (library test; not affected by removal
    from this app)
  - bUnit `ShellSessionStateLoaderTests`
  - `MudBlazorRebuildConformanceTests`
  - `WebGuardrailTests`

---

## Session Plan

### Session 1 of 3: Build `UserContextBar` + Sign-Out dialog

**Steps:**
1. Read `docs/planning/guiding-principles.md` and
   `docs/planning/lessons-learned.md`.
2. Confirm data shape via `ShellSessionStateLoader`, `IRoleSwitcher` /
   `SessionRoleSwitcher`, and the `RoleSwitching.AllowedRoles` path in
   `security-config.json`.
3. Create `src/Apps/AcmeCoffeeOnline.Web/Components/Shared/UserContextBar.razor`
   (+ code-behind). Render the three-link sentence; compute enable/disable
   per link; expose `data-testid` hooks for `user-context-user`,
   `user-context-role`, `user-context-org`; preserve the legacy
   `data-testid="active-organization-label"` on the org-link span so
   Playwright keeps its selector.
4. Create `SignOutConfirmationDialog.razor` (`MudDialog`). Confirm navigates
   `/auth/logout` with `forceLoad: true`.
5. Role-link click opens a `MudMenu` of allowed roles; calls
   `IRoleSwitcher.SwitchRoleAsync`. Org-link click opens a `MudMenu` of
   org options reusing the existing switch-organization redirect.
6. Add bUnit tests
   `tests/AcmeCoffeeOnline.Web.Tests/Components/UserContextBarTests.cs`:
   enable/disable matrix (SystemAdmin, single-role/single-org Customer,
   multi-org user, role-switchable non-admin), sign-out dialog opens,
   data-testids present.
7. `dotnet build` and `dotnet test`.

**Creates:** `UserContextBar.razor`, `UserContextBar.razor.cs`,
`SignOutConfirmationDialog.razor`, `UserContextBarTests.cs`
**Touches:** nothing in production yet (component is not referenced).
**Ends with:** Component + dialog compile; bUnit tests pass; `MainLayout`
unchanged.
**Progress keys:** `session-1/read-principles`,
`session-1/confirm-data-shape`, `session-1/usercontextbar-component`,
`session-1/signout-dialog`, `session-1/role-and-org-menus`,
`session-1/bunit-tests`, `session-1/build-and-test`

---

### Session 2 of 3: Wire into MainLayout, retire old controls

**Steps:**
1. Read `docs/planning/guiding-principles.md`,
   `docs/planning/lessons-learned.md`, and the Session 1 review.
2. In `MainLayout.razor` remove lines ~27-84 (active-org summary, Switch-Org
   menu, `DabblerRoleSwitcher`, user-icon menu, surrounding `MudDivider`s).
   Insert `<UserContextBar />` after `<MudSpacer />`.
3. Drop now-unused fields and methods (`_activeOrganizationKey`,
   `SwitchOrganizationAsync`, the `Dabbler.Security.Blazor.Components`
   using directive). Keep `_isAdmin`/`_isSystemAdmin` — nav drawer still
   uses them.
4. Update `MudBlazorRebuildConformanceTests` if structural counts shift.
   Update `MultiOrganizationWorkflowTests` selectors if they click the
   "Switch org" button text — click the org link instead.
5. `dotnet build`, `dotnet test`, run the full Playwright suite locally.

**Creates:** none.
**Touches:** `MainLayout.razor`, conformance test, Playwright multi-org test.
**Ends with:** One-line user context in the app bar; all 75 Playwright and
517+ unit tests green.
**Progress keys:** `session-2/maintlayout-swap`,
`session-2/cleanup-codebehind`, `session-2/preserve-testids`,
`session-2/update-conformance`, `session-2/playwright`,
`session-2/build-and-test`

---

### Session 3 of 3: Accessibility and narrow-width polish

**Steps:**
1. Read principles and Session 2 review.
2. Keyboard focus order through the three links; aria-labels so screen
   readers read the whole sentence coherently ("Casey Hart, sign out.
   System Admin, switch role. Internal Operations, switch organization.").
3. Narrow-viewport behavior (tablet width): decide between wrap and
   truncation with tooltip.
4. Verify contrast against the app bar's `AppbarText=#424242` theme token.
5. Refresh any reference screenshot under `docs/` that pictured the old
   cluster.
6. `dotnet build`, `dotnet test`, Playwright.

**Creates:** none (or screenshot updates only).
**Touches:** `UserContextBar.razor`, possibly theme overrides.
**Ends with:** Dashboard is keyboard-operable and presents well at tablet
widths.
**Progress keys:** `session-3/a11y`, `session-3/responsive`,
`session-3/screenshots`, `session-3/build-and-test`

---

## First-Implementation Sketch

```razor
@* src/Apps/AcmeCoffeeOnline.Web/Components/Shared/UserContextBar.razor *@
<div data-testid="user-context-bar" class="d-flex align-center">
    <MudLink OnClick="OnUserClickAsync" Color="Color.Inherit"
             data-testid="user-context-user">@_userName</MudLink>
    <span class="mx-1">as</span>
    <MudLink OnClick="OnRoleClickAsync" Color="Color.Inherit"
             Disabled="!_canSwitchRole"
             data-testid="user-context-role">@_activeRole</MudLink>
    <span class="mx-1">for</span>
    <MudLink OnClick="OnOrgClickAsync" Color="Color.Inherit"
             Disabled="!_canSwitchOrg"
             data-testid="active-organization-label">@_activeOrgName</MudLink>
    <MudMenu @ref="_roleMenu" PositionAtCursor="true">
        @foreach (var r in _allowedRoles)
        { <MudMenuItem OnClick="@(() => SwitchRoleAsync(r))">@r</MudMenuItem> }
    </MudMenu>
    <MudMenu @ref="_orgMenu" PositionAtCursor="true">
        @foreach (var o in _orgOptions.Where(o => !o.IsActive))
        { <MudMenuItem OnClick="@(() => SwitchOrgAsync(o.OrganizationId))">@o.OrganizationName</MudMenuItem> }
    </MudMenu>
</div>
```

## What This Set Preserves

| Existing Asset | Status |
|---|---|
| `DabblerRoleSwitcher` in Dabbler.Security.Blazor | Preserved; only removed from this app's MainLayout |
| `DabblerRoleSwitcherTests` | Must stay green |
| `/auth/switch-organization` redirect flow | Reused verbatim |
| `ShellSessionStateLoader`, `IRoleSwitcher` | Reused |
| `data-testid="active-organization-label"` hook | Moved onto the org link span |
| All 75 Playwright tests and 517+ unit tests | Must stay green |

## Risk Notes

- **Preserved test hooks.** Keep `data-testid="active-organization-label"`
  on the org-link span. If a Playwright test clicks the "Switch org" button
  text, change it to click the org link.
- **Sign-out confirmation is new UX.** Any test that clicks a logout link
  directly must drive the confirm dialog.
- **Deferred polish.** Avatar-icon hybrid and role-menu-vs-dialog are
  Session 3 concerns only if they surface during UAT.
- **Dabbler library deprecation.** Out of scope. The library component
  stays for other consumers.
