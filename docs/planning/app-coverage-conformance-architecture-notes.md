# App Coverage And Conformance Architecture Notes

> **Purpose:** Capture the Session 2 seam cleanup delivered for
> `docs/session-sets/app-coverage-conformance/` so later proof work can target
> stable app-facing boundaries.
> **Created:** 2026-04-15
> **Source Inputs:** `docs/planning/app-coverage-conformance-review-ledger.md`,
> `docs/planning/app-coverage-conformance-seam-refactor-plan.md`, and the
> updated app, API, and test sources under `src/Apps/` and `tests/`

---

## Session 2 Delivery Snapshot

Session 2 focused on the highest-priority architecture-conformance gaps from
the Session 1 ledger:

- the user-administration page no longer reads or mutates security data
  directly from the web tier
- the main shell no longer queries organization options from
  `DabblerSecurityDbContext` inside the layout
- the order-detail page now delegates record, item, note, and workflow state
  coordination to a dedicated app-facing state object

These changes keep the distributor app behavior intact while making the app
easier to prove at the intended API, unit, and bUnit layers.

## User Administration Boundary

`src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Admin/UserManagement.razor`
now consumes `AdminUserApiClient` exclusively for:

- active-scope loading
- scoped user-admin grants
- scoped user-admin revocations

The web page remains responsible for rendering and Snackbar feedback, but the
API remains the only owner of the user-admin scope rules already centralized in
`src/Apps/AcmeCoffeeOnline.Api/Services/AdminUserScopeService.cs`.

This restores the REST-service-layer rule from
`docs/planning/user-functionality-notes.md` and gives Session 3 a clear page
boundary for focused UI proof without duplicating rule-heavy behavior in
Blazor.

## Shell Organization Boundary

The shell organization selector now flows through two explicit seams:

- `src/Apps/AcmeCoffeeOnline.Api/Services/OrganizationOptionsService.cs`
- `src/Apps/AcmeCoffeeOnline.Web/Services/ShellSessionStateLoader.cs`

`OrganizationOptionsService` resolves the visible organization set from the API
side using the authenticated session context:

- system administrators receive all active organizations with any membership
  role hints preserved when available
- non-system-admin users receive only their own organization memberships

`MainLayout.razor` now limits itself to shell rendering and app wiring. It
loads user/session display state through `ShellSessionStateLoader` and no
longer owns direct security-database reads.

## Order Detail Seam

`src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Orders/OrderDetail.razor` now
uses `src/Apps/AcmeCoffeeOnline.Web/Services/OrderDetailState.cs` for:

- initial order and note loading
- create, cancel, and status-transition coordination
- order-item add/remove behavior and total recalculation
- note refresh and deletion behavior

The Razor component still owns UI-only concerns such as button rendering,
picker visibility, popup invocation, and color or icon choices. The stateful
workflow logic now lives behind an app-facing seam that Session 3 can test more
directly without driving the whole page.

## Proof Follow-On

Session 3 should target these seams directly:

- API/integration proof for `OrganizationOptionsService` and the existing admin
  scope service
- bUnit proof for `UserManagement.razor` and shell state rendering against
  injected clients or loader seams
- unit or light component proof for `OrderDetailState` and the thinner order
  detail page wiring
