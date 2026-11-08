# Security Dashboard UAT Spec

> **Purpose:** Provide a targeted human-UAT checklist for the AcmeCoffeeOnline
> Security Dashboard at `/admin/security`, now that all seven tabs (Roles,
> Page Access, Entity Access, Endpoint Access, Control Permissions, Row
> Filters, User Assignments) are wired to the Dabbler.Security registration
> API and populated with real seed data.
> **Created:** 2026-04-17
> **Session Set:** `docs/session-sets/security-dashboard-uat/`
> **Prerequisite:** `security-dashboard-wiring` session set complete. All
> dashboard tabs render with real data and runtime enforcement is verified by
> 63 automated tests.
> **Workflow:** Single-session checklist generation followed by human
> execution.

---

## Context

After the `security-dashboard-wiring` session set, the Security Dashboard is
now populated with:

- **4 roles** with inheritance (Customer → BackofficeUser →
  BackofficeUserAdmin → SystemAdmin)
- **7 page permissions** covering the full AcmeCoffeeOnline app
- **5 entities** (Product, Customer, Order, OrderItem, OrderNote) with CRUD
  matrices and at least one field permission (Product.UnitPrice)
- **21 endpoint permissions** covering the API surface
- **5 control permissions** (CreateProductButton, EditPriceButton,
  DeleteCustomerButton, DeleteOrderButton, UpdateStatusButton)
- **1 row filter** (Customer role sees only their own orders)
- **User Role Assignments** populated from the EF user list provider

The human reviewer says the dashboard "looks really nice." This UAT is a
targeted confidence pass to judge visual polish, clarity, and the feel of
interacting with each tab — the things automation cannot score.

## What This UAT Covers

- **Visual quality** — tab strip, header, spacing, alignment, readable text
- **Information clarity** — labels, column headings, tooltips, status text,
  empty states, role-inheritance cues
- **Editing feel** — do toggles, dropdowns, and save/discard actions feel
  trustworthy? Is feedback prompt and readable?
- **Navigation coherence** — switching tabs preserves state predictably; deep
  content is reachable and scrollable
- **Presentation-readiness** — would this demo well to a stakeholder?

This checklist intentionally does not re-test the 63 automated enforcement
tests already proving that runtime changes persist and are honored. It asks
the human to judge polish, coherence, and edge cases of the dashboard UI
itself.

## How To Execute

1. Start the application:
   ```bash
   dotnet build Dabbler.Platform.sln
   dotnet run --project src/Apps/AcmeCoffeeOnline.Api --launch-profile http
   dotnet run --project src/Apps/AcmeCoffeeOnline.Web --launch-profile http
   ```

2. Open the checklist in the UAT editor:
   **https://darndestdabbler.github.io/uat-checklist-editor/**

3. Load
   `docs/session-sets/security-dashboard-uat/security-dashboard-uat-checklist.json`

4. Sign in as **`admin@acmecoffee.com`** (SystemAdmin). The Security
   Dashboard is restricted to SystemAdmin by the seeded page permission, so
   most checklist items are executed under that identity.

   Use the Dev Quick Sign-In dropdown on the login page or the DevTools
   helper documented in the `post-rebuild-uat` spec. A second sign-in as a
   non-admin (e.g. `jdoe@acmecoffee.com`) is needed to confirm that
   `/admin/security` remains denied to non-SystemAdmin roles.

5. Navigate to `/admin/security` and work tab-by-tab through the checklist.
   Mark Pass/Fail and add feedback. Use the "Other" rows per subarea for
   anything the predefined items do not cover.

6. Save the completed checklist JSON back to the same path.

## Session Set Configuration

```yaml
requiresUAT: false
requiresE2E: false
```

## Sessions
### Session 1 of 1: Generate UAT Checklist and Hand Off

**Steps:**
1. Read `docs/planning/project-guidance.md` and
   `docs/planning/lessons-learned.md`.
2. Generate the UAT checklist JSON against the checklist-editor schema with
   functional areas covering each dashboard tab plus dashboard-wide concerns
   (shell, access control, cross-tab behavior).
3. Verify the JSON validates against `checklist-schema.json` and loads
   correctly in the checklist editor.
4. Hand off to the human for execution.

**Creates:** `security-dashboard-uat-checklist.json`
**Ends with:** Checklist ready for human execution
**Progress keys:** `session-1/checklist-generation`,
`session-1/schema-validation`, `session-1/hand-off`

## Helper Usage

The in-app **Dev** menu (AcmeCoffeeOnline app bar, Dev/Test only) surfaces
Quick Sign-In, Session Info, and Magic Links without leaving the browser and
is the preferred UX. The PowerShell helpers below remain for headless or CI
scenarios. See `scripts/dev-tools/README.md` for details.

Current session snapshot after a persona change:

```powershell
.\scripts\dev-tools\Get-SessionInfo.ps1
```

Inspect the persisted configuration (to compare against what the dashboard
displays):

```bash
cat src/Apps/AcmeCoffeeOnline.Web/security-config.json
```

## Remediation Flow

If the human UAT finds issues, follow the same pattern used in
`post-rebuild-uat`:

1. Capture each failure as a distinct entry in a follow-up session-set spec
   (likely `security-dashboard-polish` or similar), not in this checklist.
2. Log remediation sessions against that follow-up set so this UAT remains
   the authoritative record of what the human saw.
