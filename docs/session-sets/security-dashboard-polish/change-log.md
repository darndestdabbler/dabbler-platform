# Change Log — Security Dashboard Polish

> **Status:** Complete
> **Completed:** 2026-04-18
> **Sessions:** 8 of 8
> **Session Set:** `docs/session-sets/security-dashboard-polish/`

## Summary

This session set executed the UX-only remediation items from the
`security-dashboard-uat` human pass. It unified the Allow/Deny control
on Page and Endpoint matrices, added CRUD and state icons, introduced
inherited-vs-explicit visualization, added a User Assignments
unassigned-users toggle, corrected the access-denied flow (including a
narrowing of `UseStatusCodePagesWithReExecute` so 401/403 no longer
surface as 404), and added arrow-key navigation to the tab strip plus
an inline duplicate-role-name validation demo. The three behavioral
UAT items were intentionally deferred to
`security-dashboard-behavioral-fixes`.

## Per-session changes

### Session 1: Access-denied flow
Made `PageAuthorizationHandler` call `context.Fail()` on every denial
and narrowed `UseStatusCodePagesWithReExecute` in `Program.cs` to
genuine 404s only. Introduced `Components/Shared/AccessDeniedCard.razor`
as the centered access-denied card, wired it into the Blazor
`<NotAuthorized>` fragment in `Routes.razor`, and kept
`AccessDenied.razor` using the same card so the `/access-denied`
redirect path still shows the new UX.

### Session 2: Page Access matrix — switch + layout
Replaced the two-checkbox Allow/Deny pattern in
`DabblerPagePermissionMatrix.razor` with a new shared
`DenyAllowSwitch.razor` component (chosen as a `MudToggleGroup` to
preserve the Allow / Deny / Unset tri-state). Wired
`deny-allow-header.svg`, widened cards, centered the switch column,
and added a bUnit invariant test for the mutual-exclusion rule.

### Session 3: Endpoint Access matrix — same pattern
Reused `DenyAllowSwitch.razor` and `deny-allow-header.svg` in
`DabblerEndpointPermissionMatrix.razor`. Extended `SecurityConfiguration`
and endpoint persistence to carry an explicit `Denied` list alongside
`Allowed`, and added a bUnit invariant test parallel to Session 2.

### Session 4: Entity Access matrix — CRUD icons, no scroll
Replaced the "Create / Read / Update / Delete" text headers in
`DabblerEntityPermissionMatrix.razor` with the four 45° CRUD SVGs
(`create-header.svg`, `read-header.svg`, `update-header.svg`,
`delete-header.svg`) while preserving accessible names. Centered the
checkboxes, removed horizontal scroll, and added a bUnit accessibility
test for the column headers.

### Session 5: Control Permissions — state icons
Rendered `enabled.svg`, `disabled.svg`, and `hidden.svg` alongside the
text labels inside the `MudSelect` items in
`DabblerControlPermissionTab.razor` and
`DabblerControlPermissionEditor.razor`. Added a bUnit test that the
option template includes both icon and text.

### Session 6: Inherited vs. explicit visualization
Added italic/dimmed styling and `MudTooltip` `inherited from
<ParentRoleName>` to inherited cells across
`DabblerPagePermissionMatrix`, `DabblerEndpointPermissionMatrix`, and
`DabblerEntityPermissionMatrix`. Introduced a small shared legend card
above each matrix and a bUnit test asserting the inherited css class
and tooltip target.

### Session 7: User Assignments — unassigned filter toggle
Added a "Show unassigned users" `MudSwitch` to
`DabblerUserRoleManager.razor`, default off. Client-side filtering
keeps the full `IUserListProvider` result in state so toggling on is
instant and the seed is untouched. bUnit test proves the switch
toggles visibility without a re-fetch.

### Session 8: Tab-strip keyboard nav + error-path demo
Wrapped the tab strip in `DabblerSecurityDashboard.razor` with an
`@onkeydown` handler that moves `ActivePanelIndex` for ArrowLeft,
ArrowRight, Home, and End. Added an inline `MudAlert` duplicate-role
error path: `RoleFormDialog.razor` now accepts `ExistingRoleNames`,
validates via the pure `IsDuplicateRoleName` helper, and shows the
alert without closing the dialog; `DabblerRoleManager.razor` passes
the current role names in both the Add and Edit dialog flows. Two
bUnit tests cover the keyboard handler and the duplicate-name
validation path.

## What this set did NOT change

- All existing Playwright and non-Playwright tests were preserved.
- Role hierarchy and role/user seed data remained untouched.
- The `DabblerPageAuthorize` attribute's success semantics were
  preserved; only the failure path surface changed.
- The `SecurityConfiguration` JSON schema was preserved outside of
  Session 3's explicit `Denied` list for endpoint permissions.
- The three behavioral UAT items (repro'd against real data) remain
  out of scope — they are tracked in
  `security-dashboard-behavioral-fixes`.

## Follow-ups

- **Human UAT re-run required.** Please re-run these rows in
  `docs/session-sets/security-dashboard-uat/security-dashboard-uat-checklist.json`:
  Shell and Access — Role-Based Access (both rows); Tab Strip — Layout
  and Labels (keyboard); Page Access Tab — Matrix Layout, Matrix
  Editing, Inherited vs Explicit; Entity Access Tab — Entity List and
  CRUD Matrix; Endpoint Access Tab — Endpoint List; Control
  Permissions Tab — State Semantics; User Assignments Tab — User
  List; Cross-Cutting Quality — Feedback and Errors, Keyboard and
  Accessibility.
- If any row remains `Passes: false` after re-run, capture the
  feedback in a follow-up spec. Do not reopen the original UAT JSON.
- **Pre-existing, unrelated test failure:**
  `AcmeCoffeeOnline.Web.Tests.WorkflowAssetSmokeTests.AiRouterConfig_LoadsNestedPromptTemplates`
  fails because it references `docs/workflow-auth-observability.md`,
  which was removed in commit `8730107` (Repository cleanup). Failure
  is present on master before Session 8 changes. Not in scope here;
  should be resolved in a separate workflow-assets session.
- **Session 8 deferred items** (from cross-provider verification,
  round 1): MudTabs built-in key handling conflict is a defensive
  concern that is not observed in MudBlazor 8.15; `Save()` being
  synchronous void is fine while `MudDialog.Close` is synchronous;
  browser-level arrow-key E2E and stale-element re-querying in the
  dialog test are reasonable hardening work outside this set's scope.
