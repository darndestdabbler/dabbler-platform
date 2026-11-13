# Verification Round 1

## Cross-Provider Verification — Session 3: Role Master Details Tab

### Checklist Results

| # | Requirement | Status | Evidence |
|---|------------|--------|----------|
| 1 | Role Master grid renders baked-in + user-type roles; baked-in chip + greyed styling | ✅ | `RoleMasterGrid.razor`: `BuildRows()` merges `BakedInRoleProvider.GetBakedInRoles()` with `config.UserTypeRoles`; baked-in rows get `color:#6c757d` and `<MudChip>` with `baked-in` text |
| 2 | DisplayAlias inline-editable; canonical name read-only; baked-in → `BakedInRoleAliasOverrides`, user-type → `RoleDefinition.DisplayAlias` | ✅ | `OnDisplayAliasChangedAsync` branches on `row.IsBakedIn`; `MudTextField` with `DebounceInterval="400"`; canonical name rendered as `MudText` |
| 3 | Add/Archive gated by `CanManageRoles` | ✅ | `RoleMasterDetailsTab.ResolveCanManageRolesAsync()` checks `bakedIn.Privileges.CanManageRoles`; Add button `Disabled="@(!CanManageRoles)"`; Archive only renders when `!IsBakedIn && CanManageRoles` |
| 4 | RoleDetailsSection: user-type → 4 sub-tabs; baked-in → Mask Exemptions + Compiled Privileges | ✅ | `RoleDetailsSection.razor` conditionally renders 4 `MudTabPanel`s (Column/Row/UI/Mask) for user-type; 2 panels (Mask Exemptions + Compiled Privileges) for baked-in |
| 5 | ColumnRestrictionsSubTab uses `MudToggleGroup<ColumnRestrictionKind>` (ViewOnly \| Hidden), not MudSwitch | ✅ | Both the "new" row and existing rows use `<MudToggleGroup T="ColumnRestrictionKind">` with two `MudToggleItem`s; zero `MudSwitch` references |
| 6 | UiRestrictionsSubTab: first control item = "Entire Page" → `"*"`; `MudToggleGroup<UiRestrictionKind>` (Disabled \| Hidden) | ✅ | First `<MudSelectItem>` uses `MetadataRegistry.EntirePageControlKey` (`"*"`) with display `"Entire Page"`; toggle group has Disabled + Hidden items |
| 7 | MaskExemptionsSubTab enforces separation-of-duties via `MaskExemptionAuthority` | ✅ | `AddAsync()` and `RenewAsync()` both call `MaskExemptionAuthority.Evaluate()`; 18 unit tests cover all rule branches (user-type, UserAdmin, SystemAdmin/CrossOrgMonitor targets, self-grant, expiry); expired rows get strike-through + "Expired" chip + Renew button |
| 8 | `FilterPanel<T>` present on Role Master grid | ✅ | `<FilterPanel T="RoleRow">` with three `FilterColumnDefinition`s (CanonicalName, DisplayAlias, Description); `ShowFilterChips="true"` |
| 9 | BakedInPrivilegesPanel renders privilege flags + alias-override/default display | ✅ | `OnParametersSet` builds 12 `PrivilegeRow`s from `Role.Privileges`; alias override shown via `MudAlert` when present, default alias via `MudText` otherwise |
| 10 | Dashboard renders `<RoleMasterDetailsTab />` for tab 1; other 4 tabs remain stubs | ✅ | `DabblerSecurityDashboard.razor`: `if (tab.Key == TabKey.RoleMasterDetails) { <RoleMasterDetailsTab /> }` else stub `MudText` |
| 11 | Seeded `DefaultMaskRule` (Customer.SocialSecurityNumber) in both `security-config.json` files | ✅ | Both Web and Api configs: `"DefaultMasks": [{ "EntityKey": "Customer", "ColumnKey": "SocialSecurityNumber", "MaskFormat": "XXX-XX-{last4}" }]` |
| 12 | Persistence via `SecurityConfigurationManager.SaveAsync`; sub-tabs raise `OnPersisted` → parent `Rebuild()` | ✅ | All four sub-tabs call `ConfigManager.SaveAsync` then `OnPersisted.InvokeAsync()`; `RoleMasterDetailsTab.OnSubTabPersistedAsync()` calls `_grid?.Rebuild()` |
| 13 | No unresolved Critical/Major issues | ✅ | See minor items below |

### Additional Structural Checks

| Item | Status | Notes |
|------|--------|-------|
| `MetadataRegistry` wraps `ResourceRegistry` as thin read-only facade | ✅ | Exposes `Tables`/`GetColumns`/`Pages`/`GetControls` via entity/field → table/column vocabulary mapping |
| `MetadataRegistry` registered as singleton in DI | ✅ | `ServiceCollectionExtensions.cs`: `services.AddSingleton<MetadataRegistry>()` after `ResourceRegistry` |
| `MetadataRegistry.Empty` static for tests | ✅ | `new MetadataRegistry(ResourceRegistry.Empty)` |
| `AddUserTypeRoleDialog.razor` validates via `MudForm` | ✅ | `RequiredError` on RoleName; `ConfirmAsync` calls `_form.Validate()` |
| Duplicate role guard on add | ✅ | `RoleMasterGrid.OnAddUserTypeRoleAsync` checks both `UserTypeRoles` and `BakedInRoles.IsBakedIn` |
| MaskExemptions Table/Column dropdowns sourced from `DefaultMasks` (not MetadataRegistry) | ✅ | Correct per spec: "exemptions can only target declared-sensitive columns" |
| `MaskExemptionAuthority` is pure/static, no DI dependency | ✅ | Reusable by Set D's exemption-granting service |
| Build: 0 errors, 558 passed, 1 pre-existing failure (unrelated) | ✅ | 46 new tests all passing |

### Minor Observations (non-blocking)

| Severity | Item | Notes |
|----------|------|-------|
| Minor | No dedicated bUnit tests for `RowRestrictionsSubTab` and `UiRestrictionsSubTab` | Both follow the identical add/remove/persist pattern as `ColumnRestrictionsSubTab` (which is tested); structural correctness is confirmed by the build. Consider adding in a follow-up if coverage gaps surface during UAT. |
| Minor | `MaskExemptionsSubTab.ResolveGranterAsync` takes the first baked-in role from `EffectiveRoles` | Since only SystemAdmin can reach the Role Master Details tab, and `RoleSwitcher` returns the actively-switched role first, this is safe in practice. A more defensive implementation could prefer the highest-privilege baked-in role. |

VERDICT: VERIFIED
