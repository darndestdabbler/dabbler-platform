# Change Log — admin-user-creation-flow

## Session 1

- Fleshed out specification for admin user creation flow, locking design decisions:
  - MudDialog modal triggered from Users and Role Assignment tab
  - `BackofficeUserCreationService` in `Dabbler.Security` (single EF transaction: AppUser + membership + role + audit + IsProvisioned flip)
  - Email uniqueness enforced via existing unique index (`IX_AppUsers_Email`) + `DbUpdateException` catch (no TOCTOU race)
  - `IsProvisioned` flag on `AppUser` (default `true` for existing users; admin INSERT opts in to `false`, transaction flips to `true` as final step)
  - `SensitiveAccessEventKind.UserCreated` audit emitted **inside** the EF transaction before IsProvisioned flip
  - Library scope: dialog in `Dabbler.Security.Blazor`, service in `Dabbler.Security`, controller in `AcmeCoffeeOnline.Api`
- Two rounds of cross-provider verification; 6 design issues fixed (Critical: audit ordering; Major: IsProvisioned default; Minor: email collision UX, UAT-5 CrossOrgMonitor scenario, controller authorization policy name, magic-link post-commit failure mitigation)
- No code changes this session

## Session 2

**Database:**
- Added `IsProvisioned` column (`bool`, default `true`) to `AppUser` via EF migration `20260511171133_AddAppUserIsProvisioned`

**Audit:**
- Added `SensitiveAccessEventKind.UserCreated = 5` to `AppSensitiveAccessLog.cs`

**Service layer:**
- Implemented `BackofficeUserCreationService` + `IBackofficeUserCreationService` + `CreateBackofficeUserRequest` + `EmailAlreadyExistsException` in `Dabbler.Security/Services/`
- Service executes a single EF transaction: insert `AppUser (IsProvisioned=false)` → insert `AppUserOrganizationMembership` → insert `AppUserRole` + emit `UserCreated` audit row + set `IsProvisioned=true` → commit
- Added `IsProvisioned` guard to `MagicLinkService.IssueAsync` (throws `InvalidOperationException` for unprovisioned users)
- Registered `IBackofficeUserCreationService` in `ServiceCollectionExtensions`

**API + client:**
- Added `POST /api/admin/users` to `AdminUsersController` (authorization gate: `{SystemAdmin, CrossOrgUserAdmin, UserAdmin}`; delegates to service; 201 via `CreatedAtAction`; 409 for duplicate email; 403 for authority denial)
- Added `CreateBackofficeUserDto` / `BackofficeUserCreationResponseDto` to `AcmeCoffeeOnline.Shared/DTOs/`
- Added `CreateBackofficeUserAsync` to `AdminUserApiClient`

**Testing:**
- 5 unit tests in `BackofficeUserCreationServiceTests.cs`: happy path (SystemAdmin), UserAdmin cross-org rejection, CrossOrgMonitor denial, duplicate email, magic-link post-commit failure non-fatal

**Verification:**
- Two rounds of cross-provider verification; 6 issues fixed including server-side `baseVerifyUrl` security fix, sanitized `MagicLinkError` in API response, `CreatedAtAction` for 201, and `EmailAlreadyExistsException` base class

## Session 3

**Query contract:**
- Added `GetGrantableOrganizationsAsync(ClaimsPrincipal, CancellationToken)` to `IAdminUserQueryService` + `AdminUserQueryService`: SystemAdmin/CrossOrgUserAdmin see all active orgs; UserAdmin sees their assigned orgs; CrossOrgMonitor returns empty

**UI components:**
- Created `CreateBackofficeUserDialog.razor` in `Dabbler.Security.Blazor/Components/Admin/Users/`:
  - `MudDialog` + `MudForm` with DisplayName, Email, InitialRole (dropdown), InitialOrganization (dropdown)
  - Role dropdown filtered by `RoleAssignmentAuthority.Evaluate`; re-filters reactively when organization selection changes (`OnOrgSelectedAsync`)
  - On submit: validates form, calls `IBackofficeUserCreationService.CreateAsync`, raises `OnCreated` callback, closes dialog; inline error display for `EmailAlreadyExistsException` / `UnauthorizedAccessException`
- Added "Add User" `MudButton` to `UsersAndRoleAssignmentTab.razor` (visible to SystemAdmin, CrossOrgUserAdmin, UserAdmin; opens dialog via `IDialogService.ShowAsync`; refreshes grid on successful close)

**Testing:**
- 4 bUnit tests in `CreateBackofficeUserDialogTests.cs`: empty render, submit disabled while invalid, auth filter excludes non-assignable roles, successful submit calls service + raises `OnCreated`
- UAT checklist: `admin-user-creation-flow-uat-checklist.json` (4 journeys: happy path, UserAdmin role dropdown restriction, duplicate email validation, audit emission)
- 4 Playwright `[Fact]` tests in `AdminUserCreationFlowUatTests.cs` (green): happy path with grid refresh, UserAdmin role filter, duplicate email error, UserCreated audit row in Compliance Review

**Tooling:**
- Re-crawled component catalog (`docs/component-catalog/index.json`) after implementing new UI controls
- Added 5 suppression entries to `tools/uat-suppressions.json` covering the W2 runner DSL-driven-login conflict and un-cataloged dialog controls

**Verification:**
- Two rounds of cross-provider verification; 1 issue fixed (org-reactive role filtering in `CreateBackofficeUserDialog.BuildAssignableRoles`)
