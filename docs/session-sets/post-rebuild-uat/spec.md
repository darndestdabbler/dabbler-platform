# Post-Rebuild Human UAT Spec

> **Purpose:** Provide a targeted human-UAT checklist for the AcmeCoffeeOnline
> application following the MudBlazor-native rebuild (8 sessions) and
> comprehensive E2E workflow coverage (5 sessions, 63 new Playwright tests).
> **Created:** 2026-04-16
> **Session Set:** `docs/session-sets/post-rebuild-uat/`
> **Prerequisite:** `mudblazor-native-rebuild` and `e2e-workflow-coverage`
> session sets complete.
> **Workflow:** Single-session checklist generation followed by human execution.

---

## Context

The application has been through two major session sets:

1. **MudBlazor-native rebuild** (8 sessions): Replaced 20+ custom wrapper
   components with direct MudBlazor usage. Preserved the QBE filter model,
   expression engine, and data providers.

2. **E2E workflow coverage** (5 sessions): Added 63 Playwright tests covering
   navigation, CRUD, validation, reports, admin, multi-step workflows, and
   edge cases. All 63 new tests pass; 7 pre-existing failures remain.

The prior full-app UAT (pre-rebuild) was abandoned early because the human
found too many broken controls. That feedback drove the MudBlazor rebuild and
the comprehensive E2E testing effort.

## What This UAT Covers

Human UAT is the confidence and judgment layer after automation has proved
basic operability. This checklist focuses on:

- **Visual quality and polish** - layout, spacing, alignment, readable text
- **Clarity and coherence** - labels, headings, feedback messages, error copy
- **Workflow feel** - does the navigation feel natural? do transitions feel
  smooth? does the app feel trustworthy?
- **Accessibility basics** - contrast, focus visibility, keyboard operability
- **Presentation-readiness** - is this demo-worthy?

This checklist intentionally avoids re-testing what the 63 E2E tests already
prove (data persistence, API verification, route reachability). It asks the
human to judge things automation cannot score.

## How To Execute

1. Start the application:
   ```bash
   dotnet build Dabbler.Platform.sln
   dotnet run --project src/Apps/AcmeCoffeeOnline.Api --launch-profile http
   dotnet run --project src/Apps/AcmeCoffeeOnline.Web --launch-profile http
   ```

2. Open the checklist in the UAT editor:
   **https://darndestdabbler.github.io/uat-checklist-editor/**

3. Load `docs/session-sets/post-rebuild-uat/post-rebuild-uat-checklist.json`

4. Sign in using the browser DevTools console. The seeded-session endpoint
   requires the `X-Test-Auth-Secret` header, so bare URLs will not work.

   Open `http://localhost:5010/login`, then open the DevTools console (F12)
   and paste this reusable helper:

   ```javascript
   window.seededSessionSignIn = async (email) => {
     const response = await fetch(
       `/test-auth/seeded-session?email=${encodeURIComponent(email)}`,
       {
         method: 'GET',
         headers: { 'X-Test-Auth-Secret': 'acme-local-seeded-session' },
         credentials: 'include'
       }
     );
     if (!response.ok) throw new Error(`Sign-in failed: ${response.status}`);
     location.href = '/';
   };
   ```

   Then sign in as any test user:

   ```javascript
   await window.seededSessionSignIn('admin@acmecoffee.com');       // SystemAdmin
   await window.seededSessionSignIn('jdoe@acmecoffee.com');        // BackofficeUser
   await window.seededSessionSignIn('james.smith@example.com');    // Customer
   await window.seededSessionSignIn('casey.hart@acmecoffee.com');  // Dual membership
   await window.seededSessionSignIn('sloane.reyes@acmecoffee.com');// Regional sales rep
   ```

   To sign out between personas:

   ```javascript
   await fetch('/test-auth/sign-out', {
     method: 'GET',
     headers: { 'X-Test-Auth-Secret': 'acme-local-seeded-session' },
     credentials: 'include'
   });
   location.href = '/login';
   ```

5. Work through each checklist item. Mark Pass/Fail and add feedback.

6. Save the completed checklist JSON back to the same path.

## Session Set Configuration

```yaml
requiresUAT: false
requiresE2E: false
```

## Sessions
### Session 1 of 1: Generate UAT Checklist and Hand Off

**Steps:**
1. Generate the UAT checklist JSON against the checklist-editor schema.
2. Verify the JSON loads correctly in the checklist editor.
3. Hand off to the human for execution.

**Creates:** `post-rebuild-uat-checklist.json`
**Ends with:** Checklist ready for human execution

## Helper Usage

The preferred developer UX is the in-app **Dev** menu in the AcmeCoffeeOnline
app bar (visible only when `Authentication:SeededSession:Enabled=true` in
Development or Test). It exposes Quick Sign-In, Session Info, and Magic Links
without leaving the browser.

The PowerShell helpers remain for headless or CI scenarios. They live under
`scripts/dev-tools/` — see `scripts/dev-tools/README.md` for details.

Current session snapshot after an organization switch:

```powershell
.\scripts\dev-tools\Get-SessionInfo.ps1
```

Clear any prior magic links for the customer test user:

```powershell
.\scripts\dev-tools\Get-MagicLinks.ps1 -Email james.smith@example.com -Clear
```

Read the newly captured magic link after requesting sign-in:

```powershell
.\scripts\dev-tools\Get-MagicLinks.ps1 -Email james.smith@example.com
```