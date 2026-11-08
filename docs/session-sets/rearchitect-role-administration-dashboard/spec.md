# Rearchitect Role Administration — Security Dashboard Rebuild

> **Purpose:** Rebuild the AcmeCoffeeOnline Security Dashboard at
> `/admin/security` from scratch as a five-tab layout wired to the
> restriction-based model introduced by Set A. Delete the seven
> grant-based legacy tabs and their supporting components. Introduce
> the data-bound enforcement seam that Sets D and C will extend.
> **Created:** 2026-04-19 *(stub)*, fleshed out 2026-04-19 (Session 1
> of this set).
> **Session Set:** `docs/session-sets/rearchitect-role-administration-dashboard/`
> **Roadmap:** `C:\Users\denmi\.claude\plans\ok-i-did-some-abstract-blossom.md`
> **Architecture doc:**
> [../rearchitecting-role-administration/rearchitecting-role-administration.md](../rearchitecting-role-administration/rearchitecting-role-administration.md)
> **Prerequisite:** Set A
> (`rearchitect-role-administration-foundations`) complete; its
> [change-log](../rearchitect-role-administration-foundations/change-log.md)
> confirms the restriction types, baked-in role surface, and seed
> migration all shipped.
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Context

This is Session Set **B** of the four-set rearchitecture roadmap
(A → **B** → D → C). Set A rebuilt the authorization data model and
handlers; Set B rebuilds the *surface* that a SystemAdmin uses to
configure that model. After Set B, the Security Dashboard renders
entirely against restriction-based types and is ready for Set D
(sensitive-field masking + audit) and Set C (transactional system
columns) to extend.

### Confirmation that Set A delivered what Set B needs

Cross-checked against
`docs/session-sets/rearchitect-role-administration-foundations/change-log.md`
(exit criteria all met on 2026-04-19):

- Restriction types ship:
  [`ColumnRestriction`](../../../src/Libs/Dabbler.Security/Configuration/ColumnRestriction.cs),
  [`RowRestriction`](../../../src/Libs/Dabbler.Security/Configuration/RowRestriction.cs),
  [`UiRestriction`](../../../src/Libs/Dabbler.Security/Configuration/UiRestriction.cs),
  [`DefaultMaskRule`](../../../src/Libs/Dabbler.Security/Configuration/DefaultMaskRule.cs),
  [`MaskExemption`](../../../src/Libs/Dabbler.Security/Configuration/MaskExemption.cs),
  [`OrganizationSwitching`](../../../src/Libs/Dabbler.Security/Configuration/OrganizationSwitching.cs).
- Baked-in role surface ships:
  [`BakedInRoles`](../../../src/Libs/Dabbler.Security/Authorization/BakedInRoles.cs),
  [`BakedInRolePrivileges`](../../../src/Libs/Dabbler.Security/Authorization/BakedInRolePrivileges.cs),
  [`BakedInRoleDefinition`](../../../src/Libs/Dabbler.Security/Authorization/BakedInRoleDefinition.cs),
  [`IBakedInRoleProvider`](../../../src/Libs/Dabbler.Security/Authorization/IBakedInRoleProvider.cs),
  [`PlatformBakedInRoleProvider`](../../../src/Libs/Dabbler.Security/Authorization/PlatformBakedInRoleProvider.cs).
- `RoleDefinition.DisplayAlias` present;
  `SecurityConfiguration.BakedInRoleAliasOverrides` present;
  `RoleDefinition.InheritsFrom` deleted; `AllowedRoles` / `DeniedRoles`
  absent from seed.
- Row-restriction runtime ships:
  [`IRowRestrictionBroker`](../../../src/Libs/Dabbler.Security/Services/IRowRestrictionBroker.cs),
  [`RowRestrictionBroker`](../../../src/Libs/Dabbler.Security/Services/RowRestrictionBroker.cs),
  [`RowRestrictionExpressionBuilder`](../../../src/Libs/Dabbler.Security/Services/RowRestrictionExpressionBuilder.cs).
- New handlers in place: `PageAuthorizationHandler`,
  `EndpointAuthorizationHandler`, `ControlAuthorizationHandler`;
  `EntityAuthorizationHandler` and `FieldAuthorizationHandler`
  deleted.
- Seed files (`security-config.json`, `user-roles.json`) migrated;
  `BackofficeUserAdmin` → baked-in `CrossOrgUserAdmin` complete.
- Sample integration: `OrderService` consults
  `IRowRestrictionBroker`.

### What Set A explicitly deferred to Set B (or later)

From the Set A change-log's Follow-up section:

1. **Rebuild the Security Dashboard against the new model.** Core
   scope of this set.
2. **EF Core query filter provider for organization scoping.** This
   sits naturally in the Organization Master Details session (see
   Session 6 below), because the tab needs the filter to render
   cross-org vs. single-org views correctly.
3. **Physical deletion of legacy `[Obsolete]` configuration types.**
   Set A's Follow-up section literally says "(Set C)" for this
   item, but Set C's scope (per the roadmap) is transactional
   system columns, not security cleanup. **This spec treats the
   Set A change-log's "(Set C)" tag as a correction candidate and
   reassigns the deletion to Set B, progressively, alongside the
   UI consumers.** Rationale: the consumers that keep
   `PagePermission` / `EntityPermission` / `FieldPermission` /
   `ControlPermission` / `RowFilter` / `RoleSwitchingPermission`
   alive today are the legacy matrix components this set deletes,
   plus `EfCoreSecurityStore`, `AppPagePermission` /
   `AppFieldPermission` / `AppControlPermission` / `AppRowFilter`
   entities, `RoleInheritanceHelper`, and a few `Requirements`
   types. Deletion belongs where the consumers go away — here —
   not in Set C. If the human prefers Set A's original assignment,
   revert this section and add a placeholder session to Set C; the
   human has an explicit opportunity to weigh in during Session 8's
   Step 9 reorganization review, and Set A's change-log can be
   amended there (never rewritten — only moved) if accepted.
   Progressive `[Obsolete]`-type deletion lands alongside the tabs
   that consume them in Sessions 2–8 of this set, tracked per
   session.

### What this set does NOT do

- **Sensitive-field masking rendering + audit.** The dashboard ships
  a Default Masks tab (declarative) and a Mask Exemptions sub-tab
  (declarative) in this set; the actual *rendering* of masked values
  and audit trail wiring is Set D. Until Set D, rows in these tabs
  edit and persist but do not change any rendered data.
- **Transactional system columns (`OrganizationId`, `RoleId` on
  transactional entities).** That is Set C.
- **Anything outside `/admin/security`.** No changes to order,
  product, customer pages except where required to wire the
  data-bound restriction enforcement seam in Session 5.

## Goals

1. Delete all grant-based dashboard components and their Obsolete
   configuration/handler siblings (progressively, per session).
2. Replace the seven-tab layout with the
   [architecture doc's five top-level tabs](../rearchitecting-role-administration/rearchitecting-role-administration.md#recommended-ui-implementation):
   Role Master Details, Default Masks, Organization Master Details,
   Users and Role Assignment, Role Documentation.
3. Authorization-gate each tab per the baked-in role rules:
   - Role Master Details — **SystemAdmin only**.
   - Default Masks — **SystemAdmin only**.
   - Organization Master Details — **SystemAdmin + CrossOrgUserAdmin**.
   - Users and Role Assignment — **SystemAdmin + CrossOrgUserAdmin +
     UserAdmin** (UserAdmin's visible data is scoped by their assigned
     orgs; SystemAdmin/CrossOrgUserAdmin see all).
   - Role Documentation — all roles that can reach `/admin/security`
     (read-only; primarily educational).
4. Every master-detail grid in the dashboard sits under a reusable
   `FilterPanel<T>` (the same platform QBE filter already used by
   Orders/Products/Customers) per project-guidance.md → Conventions →
   Filter UI.
5. Role Master Details surfaces ALL roles (baked-in greyed with a
   "baked-in" chip). The details area below the grid conditionally
   renders:
   - **User-type row selected** → four sub-tabs: Data Column
     Restrictions, Data Row Restrictions, Other UI Restrictions,
     Mask Exemptions.
   - **Baked-in row selected** → Mask Exemptions sub-tab + a
     read-only panel summarizing compiled privileges.
6. Default Masks tab includes a **sensitive-name safety net** that
   flags columns in the metadata registry whose names match common
   sensitive patterns (ssn, taxid, ein, dob, password, ccnum, salary)
   but are not yet declared.
7. A **data-bound control enforcement seam** lands in this set:
   a shared helper that ties the active user-type role's Column and
   UI restrictions (and the masked-rendering hook) to any `MudForm`
   field or `MudDataGrid` column bound to a registered
   table+column. Set D fills in the mask-rendering path behind the
   hook; this set delivers the seam itself plus Hidden/ViewOnly
   behavior.
8. UAT checklist `rearchitect-role-administration-dashboard-uat-checklist.json`
   ready for human execution.
9. All build + non-Playwright test gates stay green at the end of
   every session (Playwright tests may flex in response to the
   rebuild — budgeted per session).

## Architecture Context

### Files deleted progressively across this set

From
[`src/Libs/Dabbler.Security.Blazor/Components/Admin/`](../../../src/Libs/Dabbler.Security.Blazor/Components/Admin):

- `DabblerPagePermissionMatrix.razor`
- `DabblerEndpointPermissionMatrix.razor`
- `DabblerEntityPermissionMatrix.razor`
- `DabblerFieldPermissionEditor.razor`
- `DabblerControlPermissionTab.razor`
- `DabblerControlPermissionEditor.razor`
- `DabblerRowFilterTab.razor`
- `DabblerRowFilterEditor.razor`
- `DabblerRoleManager.razor` (replaced by the new Role Master Details
  component; `DabblerUserRoleManager.razor` stays and is adapted)
- `DenyAllowSwitch.razor`
- `InheritedLegend.razor`
- `RoleFormDialog.razor` (if it only serves legacy paths — confirm
  during Session 2)

From `src/Libs/Dabbler.Security/`:

- `Configuration/PagePermission.cs`, `EntityPermission.cs`,
  `FieldPermission.cs`, `ControlPermission.cs`,
  `EndpointPermission.cs`, `RowFilter.cs`,
  `RoleSwitchingPermission.cs` — all currently `[Obsolete]`. Each
  deletion must be paired with removing the (by then dead) EF entity
  (`AppPagePermission`, `AppFieldPermission`, `AppControlPermission`,
  `AppRowFilter`), the matching `Requirements/*Requirement.cs`, and
  the lines in `EfCoreSecurityStore`, `DabblerSecurityDbContext`, and
  `ModelBuilderExtensions`.
- `Authorization/RoleInheritanceHelper.cs` — zero production callers
  after Set A; delete once the last test using it is rewritten.
- `Authorization/PermissionResolver.cs` — if it only serves legacy
  handlers (confirm during Session 2).

### Files added

- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Role/`
  folder with the new Role Master Details tree
  (`RoleMasterDetailsTab.razor`, `RoleMasterGrid.razor`,
  `RoleDetailsSection.razor`, `ColumnRestrictionsSubTab.razor`,
  `RowRestrictionsSubTab.razor`, `UiRestrictionsSubTab.razor`,
  `MaskExemptionsSubTab.razor`, `BakedInPrivilegesPanel.razor`).
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Masks/`
  folder (`DefaultMasksTab.razor`, `DefaultMasksGrid.razor`,
  `SensitiveNameSafetyNet.razor`).
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Organization/`
  folder (`OrganizationMasterDetailsTab.razor`,
  `OrganizationMasterGrid.razor`, `OrganizationDetailsForm.razor`).
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Users/`
  folder (`UsersAndRoleAssignmentTab.razor`, `UsersMasterGrid.razor`,
  `UserRoleAssignmentDetails.razor`).
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Documentation/`
  folder (`RoleDocumentationTab.razor`).
- `src/Libs/Dabbler.Security.Blazor/Components/Restriction/`
  folder for the shared data-bound enforcement seam
  (`RestrictedFormField.razor`, `RestrictedGridColumn.razor`,
  `IRestrictionBinder`, `RestrictionBinder.cs`).
- `src/Libs/Dabbler.Security/Registration/MetadataRegistry.cs` (or
  extend the existing
  [`ResourceRegistry`](../../../src/Libs/Dabbler.Security/Registration/ResourceRegistry.cs))
  to expose tables and columns in addition to pages and controls.
- `src/Libs/Dabbler.Security/Services/OrganizationScopeFilter.cs`
  (EF query filter provider consumed in Session 6).
- Tests under `tests/Dabbler.Security.Blazor.Tests/Admin/**/*.cs`
  and `tests/AcmeCoffeeOnline.Web.Tests/Admin/**/*.cs`.

### Files touched

- [`src/Libs/Dabbler.Security.Blazor/Components/Admin/DabblerSecurityDashboard.razor`](../../../src/Libs/Dabbler.Security.Blazor/Components/Admin/DabblerSecurityDashboard.razor)
  — rewritten from 7 tabs to 5 in Session 2; tab-by-tab contents
  filled in Sessions 3–8.
- [`DabblerUserRoleManager.razor`](../../../src/Libs/Dabbler.Security.Blazor/Components/Admin/DabblerUserRoleManager.razor)
  — adapted into the new Users and Role Assignment tab in Session 7.
- `DabblerRoleSwitcher.razor` — verify it already renders
  `DisplayAlias`; touch if needed.
- `DabblerSecurityDbContext.cs`, `ModelBuilderExtensions.cs`,
  `EfCoreSecurityStore.cs` — shrunk as the legacy EF entities and
  configuration types are deleted.
- `src/Apps/AcmeCoffeeOnline.Web/AcmeCoffeeDbContext.cs` — gains the
  organization-scope query filter in Session 6.
- `src/Apps/AcmeCoffeeOnline.Web/security-config.json` —
  `user-roles.json` — if Session 7 changes the user-role-assignment
  shape to surface assignments correctly.
- Any consumers of deleted components: `DabblerSecured.razor`,
  `DabblerSecurityNav.razor`, any places that wire the old matrix
  components.

### Reuse

- `FilterPanel<T>` — already used in
  [`OrderList.razor`](../../../src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Orders/OrderList.razor),
  [`ProductList.razor`](../../../src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Products/ProductList.razor),
  [`CustomerList.razor`](../../../src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Customers/CustomerList.razor).
  Identical pattern applies to every master-detail grid in this set.
- `IRoleSwitcher` / `SessionRoleSwitcher` — no changes required.
- `IBakedInRoleProvider` — consulted for the "greyed baked-in rows +
  baked-in chip" rendering in Role Master Details.
- `IRowRestrictionBroker` — already wired in `OrderService`; Session
  5's enforcement seam uses it for grid column restriction.
- `AppOrganization`, `AppUserOrganizationMembership` — backed the
  Organization Master Details + Users tabs.
- `ResourceRegistry` — the existing registry of pages/controls gets
  extended (or accompanied by a `MetadataRegistry`) for tables and
  columns so the cascading dropdowns in the restriction sub-tabs
  work.

## Decisions confirmed with the human (do not re-litigate)

From the roadmap's "Confirmed decisions" section and the architecture
doc's "Decisions confirmed during design review (2026-04-19)"
section:

1. Five top-level tabs (not four — Default Masks is a peer, not a
   sub-tab).
2. All master-detail grids use `FilterPanel<T>`.
3. `DisplayAlias` is inline-editable everywhere a role appears,
   including for baked-in roles (canonical `Name` remains
   immutable).
4. Baked-in roles appear in Role Master Details, greyed with a
   "baked-in" chip, with only the Mask Exemptions sub-tab + a
   read-only privileges panel.
5. `ColumnRestriction` is binary (`ViewOnly` | `Hidden`) — masked is
   a platform-level declaration, not a column-restriction kind.
6. Default Masks includes the sensitive-name safety net (ssn, taxid,
   ein, dob, password, ccnum, salary patterns).
7. Mask Exemptions authority and `ExpiresAt` rules (separation of
   duties) are enforced in the UI even though full rendering wires
   in Set D.
8. Legacy files are deleted progressively within this set, not in a
   final sweep. Each session's log captures its removals.

## Session Set Configuration

```yaml
requiresUAT: false
requiresE2E: false
```

## Sessions
Eight sessions total. **Session 1 (this one) fleshes out the spec
itself** so the human can review the full plan before implementation
begins. Sessions 2–8 execute the seven-item dashboard rebuild from
the roadmap.

Expect Session 5 (the data-bound enforcement seam) to be the
largest; if it overruns, the Default Masks tab's safety-net UX or
the Mask Exemptions sub-tab's authority gating may slip from
Session 4 or Session 3 respectively to a Session 5b follow-up.
Session 8 absorbs the standard closeout (UAT checklist, change log,
Step 9 reorganization review).

### Session 1 of 8: Flesh out this spec

**Steps:**
1. Read planning docs (`project-guidance.md`, `lessons-learned.md`).
2. Read the roadmap
   (`C:\Users\denmi\.claude\plans\ok-i-did-some-abstract-blossom.md`)
   and the architecture doc
   (`docs/session-sets/rearchitecting-role-administration/rearchitecting-role-administration.md`).
3. Read Set A's change-log to confirm deliverables against Set B's
   needs; verify the six-field restriction surface (ColumnRestriction,
   RowRestriction, UiRestriction, DefaultMaskRule, MaskExemption,
   OrganizationSwitching), the baked-in role surface, the row-
   restriction broker, and the migrated seed files are all present
   on master.
4. Inventory current-state files: enumerate the legacy admin
   components still living under
   `src/Libs/Dabbler.Security.Blazor/Components/Admin/` and the
   `[Obsolete]` configuration types still living under
   `src/Libs/Dabbler.Security/Configuration/`. Record them in the
   spec's "Files deleted progressively" section so per-session
   deletion lists are concrete.
5. Draft the full spec with per-session steps, Creates/Touches
   lists, Progress keys, Ends-with criteria, and explicit risk
   notes. Cover all seven roadmap items (Sessions 2–8).
6. Initialize `activity-log.json` with `totalSessions: 8`.
7. Log each step above.
8. Build and test (baseline confirmation — nothing in this session
   should have changed code).
9. Cross-provider verification of the spec itself (verifier reads
   the fleshed spec + the roadmap + architecture doc + Set A
   change-log and checks for coherence, missing pieces, and
   infeasible sequencing).
10. Commit: spec.md + activity-log.json.

**Creates:**
- `docs/session-sets/rearchitect-role-administration-dashboard/activity-log.json`

**Touches:**
- `docs/session-sets/rearchitect-role-administration-dashboard/spec.md`
  (stub → fleshed).

**Ends with:** the spec below (Sessions 2–8) matches what the human
agrees to implement; activity-log initialized; build+test green
(baseline).

**Progress keys:** `session-1/read-planning`, `session-1/read-roadmap`,
`session-1/read-set-a-change-log`, `session-1/inventory-legacy-files`,
`session-1/draft-spec`, `session-1/init-activity-log`,
`session-1/build-and-test`, `session-1/verification`,
`session-1/commit`.

---

### Session 2 of 8: Delete seven legacy tabs and stub the five-tab layout with authorization gating

**Steps:**
1. Read planning docs.
2. Delete `DabblerSecurityDashboard.razor`'s existing seven
   `MudTabPanel` entries and replace with five empty stubs: Role
   Master Details, Default Masks, Organization Master Details,
   Users and Role Assignment, Role Documentation. Keep the
   arrow-key tab navigation from the previous dashboard (it's
   already a convention — see `HandleTabKeyDown` +
   `NextPanelIndex`); update `TabCount` from 7 to 5.
3. Wire per-tab authorization using the baked-in role constants
   from
   [`BakedInRoles`](../../../src/Libs/Dabbler.Security/Authorization/BakedInRoles.cs)
   and the active role from `IRoleSwitcher` /
   `UserSessionContextService`. Tabs the active role cannot see are
   not rendered.
4. Delete the legacy components now orphaned:
   `DabblerPagePermissionMatrix.razor`,
   `DabblerEndpointPermissionMatrix.razor`,
   `DabblerEntityPermissionMatrix.razor`,
   `DabblerFieldPermissionEditor.razor`,
   `DabblerControlPermissionTab.razor`,
   `DabblerControlPermissionEditor.razor`,
   `DabblerRowFilterTab.razor`,
   `DabblerRowFilterEditor.razor`,
   `DabblerRoleManager.razor`,
   `DenyAllowSwitch.razor`, `InheritedLegend.razor`, and
   `RoleFormDialog.razor` if it serves only legacy paths. Each
   deletion paired with removing any references from
   `DabblerSecurityNav.razor` or siblings.
5. Delete the now-orphaned `[Obsolete]` configuration types whose
   last UI consumer just went away (expected:
   `PagePermission.cs`, `EntityPermission.cs`,
   `FieldPermission.cs`, `ControlPermission.cs`,
   `EndpointPermission.cs`, `RowFilter.cs`,
   `RoleSwitchingPermission.cs`). If any still compiles against
   live code (`EfCoreSecurityStore`, EF entities, handlers
   `Requirements`), keep them until the dependent file is itself
   removed. Record in the session log which deletions landed and
   which stayed.
6. Delete matching EF entities
   (`AppPagePermission.cs`, `AppFieldPermission.cs`,
   `AppControlPermission.cs`, `AppRowFilter.cs`) and their lines
   in
   [`DabblerSecurityDbContext`](../../../src/Libs/Dabbler.Security/Data/DabblerSecurityDbContext.cs)
   and
   [`ModelBuilderExtensions`](../../../src/Libs/Dabbler.Security/Data/ModelBuilderExtensions.cs),
   plus the matching store logic in
   [`EfCoreSecurityStore`](../../../src/Libs/Dabbler.Security/Persistence/EfCoreSecurityStore.cs)
   — **only** the lines that no longer have a consumer after
   step 5.
7. Delete `RoleInheritanceHelper.cs` and its tests if zero callers
   remain after the above. (Expected: zero callers.)
7a. Confirm the status of
    [`PermissionResolver.cs`](../../../src/Libs/Dabbler.Security/Authorization/PermissionResolver.cs):
    grep its references after Steps 4–7 land. If the legacy
    handlers and matrix components were its only consumers (the
    most likely outcome), delete it in this session. If any
    non-legacy consumer still uses it, retain it and note the
    surviving consumer in the session log for a follow-up session
    to address. Either way, the file's fate is recorded here and
    does not carry forward as an orphan deletion target.
8. Update `DabblerSecurityNav.razor` so it points at the new
   dashboard route; delete any nav entries that led to the old
   tabs individually if they existed.
9. Build and test. Expect failures in any bUnit/component test
   that referenced the deleted components — delete or rewrite the
   obsolete tests (do not try to keep them alive).
10. Replace any Playwright test targeting the seven-tab dashboard
    with a five-tab smoke test: sign in as SystemAdmin and assert
    all five tab headers render; sign in as a role with narrower
    access (e.g., UserAdmin) and assert only the permitted tabs
    render. Delete or update the legacy tests that asserted the
    old tab set.
11. Cross-provider verification.
12. Commit: all deletions + the stubbed dashboard, as one change.

**Creates:** none (this session is primarily deletion + stubbing).

**Touches:**
- `DabblerSecurityDashboard.razor` (seven tabs → five stubs).
- `DabblerSecurityNav.razor`.
- `DabblerSecurityDbContext.cs`, `ModelBuilderExtensions.cs`,
  `EfCoreSecurityStore.cs` (only the lines matching the deleted
  entities).

**Deletes:** the twelve-ish legacy admin components listed above,
the orphaned `[Obsolete]` configuration types, matching EF
entities, `RoleInheritanceHelper.cs` if clear, and all tests that
hardcoded the deleted types.

**Ends with:** dashboard route renders five empty tabs with correct
per-tab role gating; no references to the seven deleted tabs
remain; build + non-Playwright tests green. The `[Obsolete]`
type deletions are best-effort — as many as can cleanly cascade
alongside the UI removals land here; any type whose non-UI
consumers (EF store, handler Requirements, API endpoints,
background services) still reference it survives to the next
session that removes that consumer. The session log enumerates
what actually landed vs. what stayed.

**Progress keys:** `session-2/read-planning`,
`session-2/stub-five-tabs`, `session-2/tab-authorization`,
`session-2/delete-legacy-components`,
`session-2/delete-obsolete-config-types`,
`session-2/delete-ef-entities`,
`session-2/delete-role-inheritance-helper`,
`session-2/delete-permission-resolver`,
`session-2/update-nav`, `session-2/build-and-test`,
`session-2/playwright-five-tab-smoke`,
`session-2/verification`, `session-2/commit`.

---

### Session 3 of 8: Role Master Details tab

**Steps:**
1. Read planning docs.
2. Extend
   [`ResourceRegistry`](../../../src/Libs/Dabbler.Security/Registration/ResourceRegistry.cs)
   (or add `MetadataRegistry`) to expose tables and columns in
   addition to pages and controls. The registry is the source for
   the cascading dropdowns in the restriction sub-tabs.
3. Build `RoleMasterDetailsTab.razor`:
   - `FilterPanel<RoleFilterModel>` at the top searching across
     canonical name, alias, and description.
   - `MudDataGrid<RoleRow>` showing ALL roles. `RoleRow`
     includes `{ CanonicalName, DisplayAlias, Description,
     IsBakedIn }`. The grid is sourced by combining the baked-in
     roles from `IBakedInRoleProvider` with
     `SecurityConfiguration.UserTypeRoles`.
   - Baked-in rows render greyed with a "baked-in"
     `MudChip`. Add/Archive actions are enabled only on
     user-type rows and only when the active role is
     SystemAdmin. `DisplayAlias` is inline-editable for every
     row (debounced save; canonical `Name` stays read-only).
   - Currency/date column styling conventions where relevant.
4. Build `RoleDetailsSection.razor` that renders below the grid
   when a row is selected:
   - **User-type selected:** four sub-tab panels (Data Column
     Restrictions, Data Row Restrictions, Other UI Restrictions,
     Mask Exemptions).
   - **Baked-in selected:** Mask Exemptions sub-tab +
     `BakedInPrivilegesPanel.razor` (read-only summary sourced
     from `BakedInRolePrivileges`).
5. Build `ColumnRestrictionsSubTab.razor`: inline-editable grid of
   `{ Table, Column, RestrictionKind }`.
   Table and Column are cascading `MudSelect`s sourced from the
   metadata registry. `RestrictionKind` renders as a
   `MudToggleGroup<ColumnRestrictionKind>` (`ViewOnly` | `Hidden`);
   no Allow option (absence of row means Allow). Lesson learned
   from `security-dashboard-polish` Session 2: `MudSwitch` is
   binary — use `MudToggleGroup` for multi-state semantics.
6. Build `RowRestrictionsSubTab.razor`: inline-editable grid of
   `{ Table, FilterExpression }`. Filter expression uses the
   existing expression-language string (the foundations set's
   `RowRestrictionExpressionBuilder` validates on save).
7. Build `UiRestrictionsSubTab.razor`: inline-editable grid of
   `{ Page, Control, RestrictionKind }`. Control dropdown's first
   item is "Entire Page" (maps to `ControlKey = "*"`).
   `RestrictionKind` is `MudToggleGroup<UiRestrictionKind>`
   (`Disabled` | `Hidden`).
8. Build `MaskExemptionsSubTab.razor`: inline-editable grid of
   `{ Table, Column, GrantedBy, GrantedAt, ExpiresAt, Status }`.
   Table/Column cascading-sourced from
   `SecurityConfiguration.DefaultMasks` (so exemptions can only
   target declared-sensitive columns). `ExpiresAt` is an editable
   optional `MudDatePicker`. Status column renders
   `Active` | `Expired` based on `ExpiresAt > now`. Grant
   authority rules (see architecture doc §Per-role Mask
   Exemptions) enforced here: row validation fails if the active
   role isn't authorized to grant that (target-role, ExpiresAt?)
   combination. Expired rows render struck-through with a
   "Renew" action.
9. **Seed at least one `DefaultMaskRule` in
   `src/Apps/AcmeCoffeeOnline.Web/security-config.json`** (and the
   API copy) so Session 3's Mask Exemptions sub-tab cascading
   dropdowns have real source data before Session 4 ships the
   Default Masks *tab* that normally manages this list. Suggested
   seed: `Customer.SocialSecurityNumber` with
   `MaskFormat = "XXX-XX-{last4}"`. This lets Session 3's
   authority-rule tests target a real exemption row and avoids an
   empty-dropdown testing gap.
10. Write bUnit interaction tests — including for
    `BakedInPrivilegesPanel.razor` (read-only rendering +
    alias-override display): filter, add, edit, delete, baked-in
    vs. user-type row rendering, baked-in privileges panel
    content, authority-rule enforcement on Mask Exemptions.
11. Build and test.
12. Cross-provider verification.
13. Commit.

**Creates:**
- `Components/Admin/Role/RoleMasterDetailsTab.razor`
  (+ `RoleMasterGrid`, `RoleDetailsSection`,
  `ColumnRestrictionsSubTab`, `RowRestrictionsSubTab`,
  `UiRestrictionsSubTab`, `MaskExemptionsSubTab`,
  `BakedInPrivilegesPanel`).
- `Registration/MetadataRegistry.cs` (or extend
  `ResourceRegistry.cs`).
- `tests/Dabbler.Security.Blazor.Tests/Admin/Role/` tree.

**Touches:**
- `DabblerSecurityDashboard.razor` (fill the Role Master Details
  tab stub).
- `ResourceRegistry.cs` if that's where the metadata extension
  lands.
- `src/Apps/AcmeCoffeeOnline.Web/security-config.json`,
  `src/Apps/AcmeCoffeeOnline.Api/security-config.json` (seed
  DefaultMaskRule row per Step 9).

**Ends with:** Role Master Details tab renders all roles with
correct user-type vs. baked-in split; every sub-tab edits and
persists; authority rules enforced on Mask Exemptions against at
least one seeded Default Mask row; `FilterPanel<T>` present;
bUnit tests green. The Mask Exemptions sub-tab is structurally
complete and operates against the seeded row; richer interaction
coverage (add-many, filter-across-many) lands in Session 4 after
the Default Masks tab exists.

**Progress keys:** `session-3/read-planning`,
`session-3/metadata-registry`, `session-3/role-master-grid`,
`session-3/role-details-section`,
`session-3/column-restrictions-subtab`,
`session-3/row-restrictions-subtab`,
`session-3/ui-restrictions-subtab`,
`session-3/mask-exemptions-subtab`,
`session-3/mask-exemption-authority`,
`session-3/seed-default-mask-rule`,
`session-3/bunit-tests`, `session-3/build-and-test`,
`session-3/verification`, `session-3/commit`.

---

### Session 4 of 8: Default Masks tab + sensitive-name safety net

**Steps:**
1. Read planning docs.
2. Build `DefaultMasksTab.razor` with:
   - `FilterPanel<DefaultMaskFilterModel>` searching across table
     name, column name, and mask format.
   - `DefaultMasksGrid` (inline-editable `MudDataGrid`) of
     `{ Table, Column, MaskFormat }`. Table/Column are cascading
     `MudSelect`s from the metadata registry. `MaskFormat` is a
     free-text string that supports tokens `{last4}`,
     `{firstChar}`; a blank value renders `********`. Defer
     actual rendering to Set D; this session writes and persists
     rows only.
3. Build `SensitiveNameSafetyNet.razor`: a collapsible `MudCollapse`
   panel below the grid listing every column in the metadata
   registry whose name matches regex
   `/ssn|taxid|ein|dob|password|ccnum|salary/i` but is NOT yet in
   the Default Masks list. Each row has an "Add to Default Masks"
   button that prepends a new row to the grid with MaskFormat
   blank.
4. Validate that only SystemAdmin can reach the tab (redundant with
   Session 2's gating but re-check that Mask-Exemption cascading
   dropdowns in Role Master Details react within the same
   circuit when Default Masks changes — push through a
   notification event if needed).
5. Write bUnit tests for the safety-net flow and filter
   interaction.
6. Build and test.
7. Cross-provider verification.
8. Commit.

**Creates:**
- `Components/Admin/Masks/DefaultMasksTab.razor`
  (+ `DefaultMasksGrid`, `SensitiveNameSafetyNet`).
- `tests/Dabbler.Security.Blazor.Tests/Admin/Masks/` tree.

**Touches:**
- `DabblerSecurityDashboard.razor` (fill the Default Masks tab
  stub).
- Possibly `MetadataRegistry.cs` to expose a column-names query
  for the safety-net regex scan.

**Ends with:** Default Masks tab renders, edits, persists; safety
net shows undeclared sensitive-pattern columns; Mask Exemptions
sub-tab in Role Master Details sees newly declared masks in the
same circuit without a reload.

**Progress keys:** `session-4/read-planning`,
`session-4/default-masks-grid`,
`session-4/sensitive-name-safety-net`,
`session-4/cross-tab-notification`, `session-4/bunit-tests`,
`session-4/build-and-test`, `session-4/verification`,
`session-4/commit`.

---

### Session 5 of 8: Data-bound control restriction enforcement seam

**Steps:**
1. Read planning docs.
2. Design and implement `IRestrictionBinder` in
   `src/Libs/Dabbler.Security.Blazor/Components/Restriction/`.
   The binder resolves, for a given (entity, column, active user,
   active role) tuple:
   - Is there a `ColumnRestriction` in effect? If so, is it
     `Hidden` or `ViewOnly`?
   - Is the column in `DefaultMasks` and does the active role lack
     an active `MaskExemption`? If so, flag for masked rendering.
     (Actual mask *rendering* stays in Set D; this session only
     surfaces the boolean + the format string.)
3. Implement `RestrictedFormField.razor` — a wrapper around a
   MudForm child that reads the binder result and:
   - `Hidden` → returns `null` (does not render).
   - `ViewOnly` → renders the child with `Disabled=true` /
     `ReadOnly=true` (MudForm supports both; pick per control).
   - Masked + no exemption → stubs the Set D hook (renders
     placeholder `********` for now with a `data-masked="true"`
     attribute so Set D can replace the rendering).
   - Otherwise → pass-through.
4. Implement `RestrictedGridColumn.razor` — analogous wrapper for
   `MudDataGrid`'s `PropertyColumn` / `TemplateColumn`:
   - `Hidden` → the column is not added.
   - `ViewOnly` → the column renders as read-only (no editor).
   - Masked → renders the placeholder text through the Set D
     hook.
5. Adapt one form and one grid in AcmeCoffeeOnline as a reference
   integration. Candidates: `ProductDetail.razor` (`UnitPrice`
   field has an existing field-level reference in the legacy
   model) and `ProductList.razor`'s `UnitPrice` column. Confirm
   the `ViewOnly` and `Hidden` paths work end-to-end.
6. Integration tests: a user-type role with a `ColumnRestriction`
   for `Product.UnitPrice = ViewOnly` cannot edit the field but
   can see it; `Hidden` makes the field and the grid column
   disappear.
7. Test that the restriction reacts to an in-circuit role switch
   (state-has-changed pattern).
8. **Playwright coverage for the reference integration.** Drive
   the browser: sign in as a user-type role with a seeded
   `ColumnRestriction.Hidden` on `Product.UnitPrice`, open the
   product list, and assert the grid column is absent; open a
   product detail and assert the form field is absent. Repeat
   with `ViewOnly` and assert the column renders read-only and
   the form field is `Disabled` / `ReadOnly`. Use the existing
   `SeededSignInAsync` fixture helper pattern from
   `AcmeCoffeeOnline.Playwright`.
9. Build and test.
10. Cross-provider verification.
11. Commit.

**Creates:**
- `Components/Restriction/IRestrictionBinder.cs`,
  `RestrictionBinder.cs`, `RestrictedFormField.razor`,
  `RestrictedGridColumn.razor`.
- `tests/Dabbler.Security.Blazor.Tests/Restriction/` tree.
- `tests/AcmeCoffeeOnline.Web.Tests/Restriction/` tree.

**Touches:**
- `ProductDetail.razor`, `ProductList.razor` (reference
  integration).
- Possibly `ServiceCollectionExtensions.cs` to register the
  binder.

**Risks:** this is the largest session. If the reference
integration reveals API gaps (e.g., `MudDataGrid` column sets
don't rebuild without a full repaint on restriction change), the
reference scope shrinks to a form-only proof and the grid path is
parked until a Session 5b follow-up. Flag in the session log.

**Ends with:** restriction-aware form fields and grid columns work
end-to-end on at least one reference entity; Hidden/ViewOnly
both exercised; masked placeholder in place for Set D to fill in.

**Progress keys:** `session-5/read-planning`,
`session-5/binder-design`, `session-5/restricted-form-field`,
`session-5/restricted-grid-column`,
`session-5/reference-integration`, `session-5/integration-tests`,
`session-5/role-switch-reactivity`,
`session-5/playwright-restriction-coverage`,
`session-5/build-and-test`,
`session-5/verification`, `session-5/commit`.

---

### Session 6 of 8: Organization Master Details tab + EF org-scope query filter

**Steps:**
1. Read planning docs.
2. Build `OrganizationMasterDetailsTab.razor`:
   - `FilterPanel<OrganizationFilterModel>` over
     `{ Key, Name, IsInternal, IsActive }`.
   - `MudDataGrid<AppOrganization>` bound to the existing
     temporal
     [`AppOrganization`](../../../src/Libs/Dabbler.Security/Entities/AppOrganization.cs)
     entity via `DabblerSecurityDbContext`.
   - Details form below for create/edit/archive.
3. Add
   `src/Libs/Dabbler.Security/Services/OrganizationScopeFilter.cs`
   — the EF Core global query filter provider Set A deferred.
   Apply it in `AcmeCoffeeDbContext.OnModelCreating` for every
   transactional `DbSet`. Escape the filter for users with
   SystemAdmin, CrossOrgUserAdmin, or CrossOrgMonitor.
4. Integration tests: records tagged with `OrganizationId = A`
   are invisible to a user whose active org is B (unless the user
   is one of the three cross-org roles).
5. Build and test (expect existing cross-org tests to need small
   updates).
6. Cross-provider verification.
7. Commit.

**Creates:**
- `Components/Admin/Organization/OrganizationMasterDetailsTab.razor`
  (+ children).
- `Services/OrganizationScopeFilter.cs`.
- `tests/AcmeCoffeeOnline.Api.Tests/OrganizationScopeFilterTests.cs`
  or similar.

**Touches:**
- `DabblerSecurityDashboard.razor`.
- `AcmeCoffeeDbContext.cs` (global query filter registration).
- Any service layer that already manually org-filters and now
  becomes redundant (remove duplicated code per
  project-guidance.md → Conventions → don't keep
  mixed-responsibility methods).

**Ends with:** Organization Master Details tab renders and edits;
org-scope filter active globally; tests confirm cross-org escape
for the three cross-org roles.

**Progress keys:** `session-6/read-planning`,
`session-6/organization-master-grid`,
`session-6/organization-details-form`,
`session-6/org-scope-filter`,
`session-6/remove-manual-filters`, `session-6/integration-tests`,
`session-6/build-and-test`, `session-6/verification`,
`session-6/commit`.

---

### Session 7 of 8: Users and Role Assignment tab

**Steps:**
1. Read planning docs.
2. Build `UsersAndRoleAssignmentTab.razor`:
   - `FilterPanel<UserFilterModel>` over
     `{ Name, Email, ActiveRole, ActiveOrganization }`.
   - `MudDataGrid<UserRow>` bound to
     [`IUserListProvider`](../../../src/Libs/Dabbler.Security/Services/IUserListProvider.cs)
     and
     [`AppUserOrganizationMembership`](../../../src/Libs/Dabbler.Security/Entities/AppUserOrganizationMembership.cs).
   - Below, a role-assignment details surface adapted from the
     existing
     [`DabblerUserRoleManager.razor`](../../../src/Libs/Dabbler.Security.Blazor/Components/Admin/DabblerUserRoleManager.razor)
     that already works for the wiring set.
3. Authorization gating beyond the tab-level check:
   - UserAdmin sees only users in their assigned orgs.
   - CrossOrgUserAdmin sees all users; can assign any role
     *except* SystemAdmin. Can self-assign CrossOrgUserAdmin
     (vacation coverage).
   - SystemAdmin sees all users; can assign any role. Can assign
     CrossOrgUserAdmin initially (architecture doc §1).
4. Rewire the tab to use the new `UserTypeRoles` list + baked-in
   provider; the old grant-based role list is gone.
5. If needed, update `security-config.json` / `user-roles.json`
   shape so newly-assigned roles round-trip correctly.
6. Integration + bUnit tests.
7. **Playwright coverage for the role-assignment workflow.** Sign
   in as CrossOrgUserAdmin and elevate a seeded user to
   `BackofficeUser`; assert the save succeeds and the target user,
   on their next seeded sign-in, sees the affordances the new
   role grants (reuse the pattern from
   `security-dashboard-behavioral-fixes` Session 3's
   SystemAdmin-elevation test).
8. Build and test.
9. Cross-provider verification.
10. Commit.

**Creates:**
- `Components/Admin/Users/UsersAndRoleAssignmentTab.razor`
  (+ `UsersMasterGrid`, `UserRoleAssignmentDetails`).
- `tests/Dabbler.Security.Blazor.Tests/Admin/Users/` tree.

**Touches:**
- `DabblerSecurityDashboard.razor`.
- `DabblerUserRoleManager.razor` (adapted or retired in favor of
  `UserRoleAssignmentDetails`; decide during the session).
- Possibly `security-config.json` / `user-roles.json`.

**Ends with:** Users and Role Assignment tab renders, gates, and
persists per role; every role-assignment rule from the
architecture doc enforced in both UI and API.

**Progress keys:** `session-7/read-planning`,
`session-7/users-master-grid`,
`session-7/user-role-assignment-details`,
`session-7/role-authority-rules`, `session-7/config-shape-update`,
`session-7/integration-tests`,
`session-7/playwright-role-assignment`,
`session-7/build-and-test`,
`session-7/verification`, `session-7/commit`.

---

### Session 8 of 8: Role Documentation tab + UAT checklist + change log + Step 9 reorganization

**Steps:**
1. Read planning docs.
2. Build `RoleDocumentationTab.razor` — static content sourced
   from the architecture doc (baked-in role summaries, user-type
   default privileges, Default-Masks + Mask-Exemption semantics).
   Render with `MudExpansionPanels` per role for navigability.
   No data binding; no edit.
3. Write
   `rearchitect-role-administration-dashboard-uat-checklist.json`
   against the
   [checklist-editor schema](https://github.com/darndestdabbler/uat-checklist-editor/blob/main/checklist-schema.json)
   with functional areas covering each tab + Shell and Access +
   cross-cutting quality. Include concrete seeded test data (users,
   entity names, mask patterns) in every `HumanAction` that
   searches, filters, or selects. Validate against the schema.
4. **Playwright sweep for the full dashboard.** Add tests for
   tab-strip arrow-key navigation (lesson: tab-strip arrow-key
   nav was added in `security-dashboard-polish` Session 6 and
   must survive the rebuild), the per-role tab gating matrix
   across SystemAdmin / CrossOrgUserAdmin / UserAdmin / User,
   the sensitive-name safety net's "Add to Default Masks"
   one-click flow, and the Role Documentation tab's rendering.
   Combined with Sessions 2/5/7's Playwright additions, the
   five-tab dashboard now has full browser-level coverage.
5. Generate `change-log.md` per the workflow spec. Include the
   eight-session summary, all files created/modified/deleted, and
   follow-up items for Sets D and C.
6. Build and test (final green gate).
7. Cross-provider verification.
8. Cost report + commit + push.
9. Pushover notification.
10. Step 9 reorganization review of `project-guidance.md` +
    `lessons-learned.md` (post-notify per the workflow rule).

**Creates:**
- `Components/Admin/Documentation/RoleDocumentationTab.razor`.
- `rearchitect-role-administration-dashboard-uat-checklist.json`.
- `change-log.md`.

**Touches:**
- `DabblerSecurityDashboard.razor` (final tab fill).
- `docs/planning/project-guidance.md`,
  `docs/planning/lessons-learned.md` — only if Step 9 accepts any
  reorganization proposals; committed separately.

**Ends with:** change-log.md closes the set; UAT checklist ready
for human; Step 9 review complete (whatever the outcome).

**Progress keys:** `session-8/read-planning`,
`session-8/role-documentation-tab`, `session-8/uat-checklist`,
`session-8/playwright-full-dashboard-sweep`,
`session-8/change-log`, `session-8/build-and-test`,
`session-8/verification`, `session-8/cost-report`,
`session-8/commit`, `session-8/pushover`,
`session-8/reorganization-review`.

---

## Exit Criteria (whole set)

- `/admin/security` renders five tabs; zero references to the
  legacy seven-tab layout remain in code or tests.
- All `[Obsolete]` configuration types from Set A are deleted;
  their EF entities and `Requirements/*Requirement.cs` siblings
  are gone.
- `RoleInheritanceHelper.cs` is deleted.
- Every master-detail grid uses `FilterPanel<T>`.
- Role Master Details shows baked-in roles greyed with the
  "baked-in" chip; user-type rows edit restrictions across four
  sub-tabs; baked-in rows show Mask Exemptions + the privileges
  panel.
- Default Masks tab edits + persists; sensitive-name safety net
  surfaces undeclared matches; cross-tab reactivity to Role
  Master Details' Mask Exemptions works within one circuit.
- Data-bound control enforcement works in at least one reference
  integration for both `Hidden` and `ViewOnly`; masked placeholder
  hook in place for Set D.
- Organization Master Details renders; EF org-scope query filter
  applied globally with correct escape for SystemAdmin /
  CrossOrgUserAdmin / CrossOrgMonitor.
- Users and Role Assignment persists role changes under all
  relevant admin roles with the correct authority rules.
- Role Documentation renders static role content from the
  architecture doc.
- UAT checklist ready for human review.
- Build + all non-Playwright tests green; Playwright coverage for
  the new dashboard added (tab navigation, per-role gating,
  restriction grids, safety net, reference-integration field
  behavior).

## Risk Notes

- **Session 5 size.** The enforcement-seam session may exceed
  one conversation. Reference-integration scope collapses to a
  form-only proof if `MudDataGrid` column reconfiguration
  requires a broader UI-level change — move the grid path to a
  5b follow-up rather than blowing session boundaries. (Lesson:
  `lessons-learned.md` → "Mark `[Obsolete]` Instead Of Deleting"
  endorses the similar pattern.)
- **Session 6 global EF query filter blast radius.** Adding
  `HasQueryFilter` to every transactional `DbSet` will affect
  every integration test that touches one of those entities —
  including the 20+ tests Set A Sessions 4–5 added, the
  `OrderService` tests, Customer/Product tests, and API
  controller tests. A filter that reads from
  `IHttpContextAccessor` or a scoped `UserSessionContextService`
  will throw or return empty rows in test contexts that don't
  stage an active-org claim. Budget time in Session 6 to update
  test fixtures (an `IOrganizationContextAccessor` test double
  or a `TestAuthSessionBuilder` helper that seeds the active
  org) before flipping the filter on, and run tests *with* a
  user context fixture before declaring the session green.
  Baked-in role escape (SystemAdmin / CrossOrgUserAdmin /
  CrossOrgMonitor) must still be verified with a test context
  that identifies the user as one of those roles.
- **Deletion cascades.** Every `[Obsolete]` type deletion must
  pair with EF entity + store + handler-requirement removal in
  the same commit; otherwise `dotnet build` fails or tests
  reference orphaned types. Each session's "delete-*" progress
  keys enumerate the cascade.
- **Cross-provider verification prompt scope.** Every session's
  verification prompt must disclose related files it does not
  include. This set touches 25+ files across Security, Security.Blazor,
  and AcmeCoffeeOnline.Web; the prompt stays explicit about
  what's out-of-scope so the verifier doesn't hallucinate
  "missing" code. (Lesson: promoted 2026-04-18 to Conventions →
  Workflow Expectations.)
- **bUnit coverage discipline.** Each tab + sub-tab needs
  interaction-contract coverage, not markup-presence coverage
  (lesson: project-guidance.md → Conventions → Testing
  Conventions). Add `data-testid` attributes eagerly so
  Playwright can reuse the selectors in Session 8's UAT prep.
- **Disabled-state vs. role-choice tests.** Any "the X button is
  not rendered for role Y" test must sign in as a role whose
  product definition genuinely lacks that affordance, not as a
  privileged role that happens to be gated today (lesson:
  promoted 2026-04-19 to Conventions → Testing Conventions).
- **Playwright churn.** Several existing Playwright tests target
  the seven-tab dashboard. Session 2 rewrites or deletes those
  tests; the new dashboard gains its own Playwright coverage by
  Session 8 at the latest. Partial Playwright breakage is
  expected between Sessions 2 and 8; non-Playwright suites stay
  green every session.
- **Role-switch reactivity.** Sessions 3, 5, and 7 all exercise
  restriction / privilege evaluation that can change mid-circuit
  when the user role-switches. Use the existing
  `IAuthorizationStateNotifier` / `AuthorizationStateNotifier`
  plumbing; verify in bUnit that a switch triggers re-evaluation
  without a full page reload.

## Notes for the verifier

This fleshed-out spec is Session 1's only deliverable. It does
not change executable code, restriction types, or seed data.
The verifier should check the spec for:

1. Coherence with the roadmap and architecture doc.
2. Completeness against Set A's Follow-up list (dashboard rebuild,
   EF query filter, `[Obsolete]` deletion).
3. Session sequencing sanity (no session depends on a later
   session's outputs).
4. Every session has Steps, Creates, Touches, Ends with, and
   Progress keys.
5. Risk notes connect to existing lessons-learned entries where
   applicable.
