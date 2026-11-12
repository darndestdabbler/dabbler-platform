# Change Log -- Rearchitect Role Administration: Security Dashboard Rebuild (Set B)

This session set replaces the seven-tab, grant-based Security Dashboard with a five-tab, restriction-based surface wired against the baked-in role model and restriction types delivered by Set A (`rearchitect-role-administration-foundations`). It lands the data-bound restriction enforcement seam, the organization-scope EF query filter, and the users/role-assignment workflow, and ends with a Role Documentation tab, a full UAT checklist, and a dashboard-wide Playwright sweep. Set B is the second of four planned sets (A -> **B** -> D -> C).

### Session 1 of 8: Flesh out specification
- **Summary:** Expanded the stub spec into the full eight-session plan. Every session now carries Steps, Creates, Touches, Ends-with criteria, Progress keys, and risk notes. Cross-referenced the roadmap at `C:\Users\denmi\.claude\plans\ok-i-did-some-abstract-blossom.md` and the architecture doc at `docs/session-sets/rearchitecting-role-administration/rearchitecting-role-administration.md`.
- **Verification:** Cross-provider verification (Anthropic reviewing Gemini-drafted content) on 2026-04-19 confirmed coherence, completeness against Set A's follow-up list, and session sequencing sanity.

### Session 2 of 8: Stub 5-tab dashboard + delete 12 legacy admin components
- **Summary:** Rewrote `DabblerSecurityDashboard.razor` from seven `MudTabPanel` entries into five empty stubs: Role Master Details, Default Masks, Organization Master Details, Users and Role Assignment, Role Documentation. Per-tab role gating uses `BakedInRoles` constants and consults `IRoleSwitcher` + `IBakedInRoleProvider`. Added tab-strip arrow-key navigation (ArrowLeft/ArrowRight wrap, Home, End). Deleted `DabblerPagePermissionMatrix`, `DabblerEndpointPermissionMatrix`, `DabblerEntityPermissionMatrix`, `DabblerFieldPermissionEditor`, `DabblerControlPermissionTab`, `DabblerControlPermissionEditor`, `DabblerRowFilterTab`, `DabblerRowFilterEditor`, `DabblerRoleManager`, `DenyAllowSwitch`, `InheritedLegend`, and `RoleFormDialog`.
- **Key Decision:** Progressive `[Obsolete]` deletion only. EF entities and configuration types still referenced by non-UI consumers (`EfCoreSecurityStore`, handler `Requirements`) were kept; they'll cascade out naturally as Sets D and C retire those consumers.
- **Verification:** Cross-provider verification on 2026-04-19. Build + non-Playwright tests green; Playwright seven-tab tests rewritten as five-tab smoke tests.

### Session 3 of 8: Role Master Details tab + 4 sub-tabs
- **Summary:** Built `RoleMasterDetailsTab.razor`, `RoleMasterGrid.razor`, `RoleDetailsSection.razor`, `BakedInPrivilegesPanel.razor`, and `AddUserTypeRoleDialog.razor` under `Components/Admin/Role/`. Added four inline-editable sub-tabs: `ColumnRestrictionsSubTab`, `RowRestrictionsSubTab`, `UiRestrictionsSubTab`, `MaskExemptionsSubTab`. Baked-in rows render greyed with a "baked-in" chip; user-type rows show all four sub-tabs; baked-in rows show only Mask Exemptions plus the read-only privileges panel. Extended `ResourceRegistry` with a `MetadataRegistry` facade that exposes tables and columns for cascading dropdowns. Seeded a `Customer.SocialSecurityNumber` `DefaultMaskRule` in both `security-config.json` files so Session 3's authority-rule tests had real data before Session 4 shipped the Default Masks tab.
- **Key Decision:** `RestrictionKind` columns render as `MudToggleGroup<T>` (ViewOnly | Hidden; Disabled | Hidden) rather than `MudSwitch` -- follows the lesson promoted to Conventions after `security-dashboard-polish` Session 2 that MudSwitch is binary and doesn't fit tri-state restriction semantics.
- **Verification:** Cross-provider verification on 2026-04-19. Two existing Playwright guardrail tests that asserted "SystemAdmin cannot see a control" were relaxed per the promoted hard-asserted-disabled-state convention.

### Session 4 of 8: Default Masks tab + sensitive-name safety net
- **Summary:** Built `DefaultMasksTab.razor`, `DefaultMasksGrid.razor`, and `SensitiveNameSafetyNet.razor` under `Components/Admin/Masks/`. The grid is an inline-editable `{ Table, Column, MaskFormat }` form with cascading metadata-registry dropdowns. The safety net is a collapsible panel that flags every column in the registry whose name matches `/ssn|taxid|ein|dob|password|ccnum|salary/i` but is not yet declared in Default Masks; each flagged row has a one-click "Add to Default Masks" button. Cross-tab reactivity is wired through `AuthorizationStateNotifier` so a save in Role Master Details' Mask Exemptions sub-tab rescans the safety net in the same circuit.
- **Verification:** Cross-provider verification on 2026-04-19. bUnit tests cover add, remove, and sibling-tab reactivity paths.

### Session 5 of 8: Data-bound control restriction enforcement seam
- **Summary:** Added `IRestrictionBinder` + `RestrictionBinder` under `src/Libs/Dabbler.Security.Blazor/Components/Restriction/`, along with `RestrictedFormField.razor` (MudForm child wrapper) and `RestrictedGridColumn.razor` (MudDataGrid column wrapper). The binder resolves (entity, column, active user, active role) to one of: pass-through, disabled/read-only (`ViewOnly`), not-rendered (`Hidden`), or masked. Reference-integrated into `ProductList.razor` and `ProductDetail.razor` -- a role with `ColumnRestriction.Hidden` on `Product.UnitPrice` loses the grid column and form field; `ViewOnly` makes both read-only. The masked-rendering path is stubbed with a `data-masked="true"` placeholder so Set D can replace it.
- **Key Decision:** Restrictions react to in-circuit role switches without a page reload, using the existing `AuthorizationStateNotifier` plumbing.
- **Verification:** Cross-provider verification on 2026-04-19. bUnit + Playwright `RestrictionEnforcementTests` exercise Hidden and ViewOnly end-to-end.

### Session 6 of 8: Organization Master Details tab + EF org-scope query filter
- **Summary:** Built `OrganizationMasterDetailsTab.razor`, `OrganizationMasterGrid.razor`, and `OrganizationDetailsForm.razor` under `Components/Admin/Organization/`. Added `src/Libs/Dabbler.Security/Services/OrganizationScopeFilter.cs` and applied it globally in `AcmeCoffeeDbContext.OnModelCreating` to every transactional `DbSet`. The filter escapes for users whose active baked-in role carries `CanSeeAllOrganizations` (SystemAdmin, CrossOrgUserAdmin, CrossOrgMonitor). Removed duplicated per-service org filtering that the global filter now handles.
- **Key Decision:** The filter reads from a scoped `IOrganizationContextAccessor` rather than `IHttpContextAccessor` so the same code works in unit tests, integration tests, and the Blazor Server circuit.
- **Verification:** Cross-provider verification on 2026-04-19. Integration tests confirm cross-org escape for the three privileged roles and that standard user-type roles see only their assigned org's data.

### Session 7 of 8: Users and Role Assignment tab
- **Summary:** Built `UsersAndRoleAssignmentTab.razor`, `UsersMasterGrid.razor`, and `UserRoleAssignmentDetails.razor` under `Components/Admin/Users/`. Added `RoleAssignmentAuthority` (Dabbler.Security) and `IAdminUserQueryService` to centralize "who can assign which role to whom" logic and the user-list projection. UserAdmin sees only users in their assigned orgs; CrossOrgUserAdmin sees all users and can assign any role except SystemAdmin, and can self-assign CrossOrgUserAdmin for coverage; SystemAdmin can assign any role. `user-roles.json` shape unchanged -- the rewrite is UI + authority-service only.
- **Key Decision:** Three rounds of verifier-driven fixes were applied before the session was signed off. The final pass added defensive bUnit checks and reworked the AssignAsync call path so expired/archived role assignments never round-trip through the save.
- **Verification:** Cross-provider verification rounds 1-3 on 2026-04-19 all reached VERIFIED. Playwright `UsersAndRoleAssignmentWorkflowTests` covers the CrossOrgUserAdmin-elevates-a-user flow end-to-end.

### Session 8 of 8: Role Documentation tab + UAT checklist + change log + closeout
- **Summary:** Built `RoleDocumentationTab.razor` under `Components/Admin/Documentation/`. Renders `MudExpansionPanels` sourced from `IBakedInRoleProvider` plus the deployment's `BakedInRoleAliasOverrides`, with a capabilities list generated from each role's `BakedInRolePrivileges` flags and two summary panels (user-type default privileges; Default-Masks + Mask-Exemption semantics). Wrote `rearchitect-role-administration-dashboard-uat-checklist.json` against the UAT checklist editor schema -- 34 review items across eight functional areas (Shell and Access, Role Master Details, Default Masks, Organization Master Details, Users and Role Assignment, Role Documentation, Data-bound Restriction Enforcement, Cross-Cutting Quality) with concrete seeded test data (admin@acmecoffee.com SystemAdmin; asmith@acmecoffee.com CrossOrgUserAdmin id 3; jdoe/mwilson/kbrown/tjones@acmecoffee.com BackofficeUser; Customer.SocialSecurityNumber default mask; Product.UnitPrice restriction). Added `tests/AcmeCoffeeOnline.Playwright/SecurityDashboardFullSweepTests.cs` -- per-role tab-gating matrix across all five baked-in roles, arrow-key navigation with Home/End wrap, Role Documentation rendering (all five role panels + two summary panels), and the safety-net Add-to-Default-Masks flow.
- **Key Decision:** Fixed a pre-existing failing test, `WorkflowAssetSmokeTests.AiRouterConfig_LoadsNestedPromptTemplates`, which referenced `docs/workflow-auth-observability.md`. That file was deleted in an earlier repository-cleanup commit without updating the test. Dropped the stale assertions and removed the file from the required-files list -- a mechanical correction within session scope to keep the final build+test gate green.
- **Verification:** Cross-provider verification on 2026-04-19.

## Files Created

**Documentation tab**
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Documentation/RoleDocumentationTab.razor`

**Role Master Details**
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Role/RoleMasterDetailsTab.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Role/RoleMasterGrid.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Role/RoleDetailsSection.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Role/ColumnRestrictionsSubTab.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Role/RowRestrictionsSubTab.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Role/UiRestrictionsSubTab.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Role/MaskExemptionsSubTab.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Role/BakedInPrivilegesPanel.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Role/AddUserTypeRoleDialog.razor`

**Default Masks**
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Masks/DefaultMasksTab.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Masks/DefaultMasksGrid.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Masks/SensitiveNameSafetyNet.razor`

**Organization**
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Organization/OrganizationMasterDetailsTab.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Organization/OrganizationMasterGrid.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Organization/OrganizationDetailsForm.razor`
- `src/Libs/Dabbler.Security/Services/OrganizationScopeFilter.cs`

**Users and Role Assignment**
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Users/UsersAndRoleAssignmentTab.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Users/UsersMasterGrid.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/Users/UserRoleAssignmentDetails.razor`
- `src/Libs/Dabbler.Security/Services/RoleAssignmentAuthority.cs`
- `src/Libs/Dabbler.Security/Services/IAdminUserQueryService.cs`

**Restriction enforcement seam**
- `src/Libs/Dabbler.Security.Blazor/Components/Restriction/IRestrictionBinder.cs`
- `src/Libs/Dabbler.Security.Blazor/Components/Restriction/RestrictionBinder.cs`
- `src/Libs/Dabbler.Security.Blazor/Components/Restriction/RestrictedFormField.razor`
- `src/Libs/Dabbler.Security.Blazor/Components/Restriction/RestrictedGridColumn.razor`

**Registration**
- `src/Libs/Dabbler.Security/Registration/MetadataRegistry.cs`

**Tests**
- `tests/Dabbler.Security.Blazor.Tests/Components/Admin/Role/...` (sub-tab and grid coverage)
- `tests/Dabbler.Security.Blazor.Tests/Components/Admin/Masks/...` (grid + safety net)
- `tests/Dabbler.Security.Blazor.Tests/Components/Admin/Organization/...`
- `tests/Dabbler.Security.Blazor.Tests/Components/Admin/Users/...`
- `tests/Dabbler.Security.Blazor.Tests/Components/Admin/Documentation/RoleDocumentationTabTests.cs`
- `tests/Dabbler.Security.Blazor.Tests/Components/Restriction/...`
- `tests/AcmeCoffeeOnline.Playwright/SecurityDashboardFiveTabSmokeTests.cs`
- `tests/AcmeCoffeeOnline.Playwright/RestrictionEnforcementTests.cs`
- `tests/AcmeCoffeeOnline.Playwright/UsersAndRoleAssignmentWorkflowTests.cs`
- `tests/AcmeCoffeeOnline.Playwright/SecurityDashboardFullSweepTests.cs`

**Docs**
- `docs/session-sets/rearchitect-role-administration-dashboard/spec.md` (fleshed in Session 1)
- `docs/session-sets/rearchitect-role-administration-dashboard/rearchitect-role-administration-dashboard-uat-checklist.json`
- `docs/session-sets/rearchitect-role-administration-dashboard/change-log.md`

## Files Modified or Deleted

**Modified**
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/DabblerSecurityDashboard.razor` (7 tabs -> 5 tabs, per-tab gating)
- `src/Libs/Dabbler.Security/Registration/ResourceRegistry.cs`
- `src/Apps/AcmeCoffeeOnline.Web/AcmeCoffeeDbContext.cs` (global query filter registration)
- `src/Apps/AcmeCoffeeOnline.Web/security-config.json`, `src/Apps/AcmeCoffeeOnline.Api/security-config.json` (seed DefaultMaskRule)
- `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Products/ProductList.razor`
- `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Products/ProductDetail.razor`
- `src/Libs/Dabbler.Security/Data/DabblerSecurityDbContext.cs`, `ModelBuilderExtensions.cs`, `Persistence/EfCoreSecurityStore.cs` (lines tied to deleted EF entities)
- `tests/AcmeCoffeeOnline.Web.Tests/WorkflowAssetSmokeTests.cs` (Session 8 pre-existing-failure fix)

**Deleted (legacy admin components; Session 2)**
- `DabblerPagePermissionMatrix.razor`
- `DabblerEndpointPermissionMatrix.razor`
- `DabblerEntityPermissionMatrix.razor`
- `DabblerFieldPermissionEditor.razor`
- `DabblerControlPermissionTab.razor`
- `DabblerControlPermissionEditor.razor`
- `DabblerRowFilterTab.razor`
- `DabblerRowFilterEditor.razor`
- `DabblerRoleManager.razor`
- `DenyAllowSwitch.razor`
- `InheritedLegend.razor`
- `RoleFormDialog.razor`

## Follow-up

- **Set D (sensitive-field masking rendering + audit).** The Set B masks tab declares mask rules, the enforcement seam flags masked columns through a `data-masked` placeholder hook, and Mask Exemptions persist. What is still owed: the actual masked-value rendering path (replacing the `********` placeholder with `{last4}`/`{firstChar}` format output), the `AppSensitiveAccessLog` temporal entity, and the compliance-review audit viewer (either a sixth tab or a peer `/admin/sensitive-access-log` page).
- **Set C (transactional system columns).** `OrganizationId` and `RoleId` need to become system columns on every transactional entity so multi-role writes carry unambiguous audit trails. Set B relies on the active-role context available at render time; persisted evidence lives in Set C.
- **`[Obsolete]` legacy type cleanup is partial.** ~145 compile warnings remain across `SecurityConfiguration.EntityPermissions` / `PagePermissions` / `ControlPermissions` / `FieldPermissions` / `EndpointPermissions` / `RowFilters` / `RoleSwitching`, the matching EF entities, and a few consumer tests in `DabblerRoleSwitcherTests`. Expected to finish with Set D or Set C as their rewrites retire the last consumers; no new production code writes these types.
- **Final Playwright sweep readiness.** The dashboard has browser-level coverage for tab gating, arrow-key navigation, restriction enforcement on the reference product integration, role-assignment workflow, and the Role Documentation tab. A human UAT run is waiting on the checklist at `docs/session-sets/rearchitect-role-administration-dashboard/rearchitect-role-administration-dashboard-uat-checklist.json`; open it with the editor at https://darndestdabbler.github.io/uat-checklist-editor/.
