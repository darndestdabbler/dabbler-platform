```markdown
# Changelog - Admin Users Cross-Links Session Set

## Summary

This release introduces changes to the user administration experience, primarily focusing on clarifying the distinction between "User Admin" and other roles, and improving navigation between related administration pages. The `/admin/users` page has been rebranded to "Manage Organization Admins" to reflect its specific purpose of managing User Admin assignments within an organization context. Cross-links and banners have been added to guide users to the appropriate interface for full role assignment or specific User Admin management.

## Changes

### Core Features

*   **Renamed `/admin/users` page:** The page title, `h4` heading, and description text for `/admin/users` are now "Manage Organization Admins".
    *   File: `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Admin/UserManagement.razor`
*   **Enhanced `UserRoleAssignmentDetails` component:**
    *   Added `[Parameter] bool ExcludeUserAdminAssignment { get; set; } = true;` to `UserRoleAssignmentDetails`.
    *   When `ExcludeUserAdminAssignment` is `true` (default):
        *   Filters "UserAdmin" from the assignable-role dropdown.
        *   Renders an assigned "UserAdmin" chip as read-only.
        *   Includes defense-in-depth guards in `AssignAsync` and `RemoveRoleAsync` to prevent UserAdmin assignment/removal.
    *   File: `src/Libs/Dabbler.Security.Blazor/Components/Admin/Users/UserRoleAssignmentDetails.razor`
*   **Organization-context-aware cross-linking:**
    *   In `UserRoleAssignmentDetails`, when `ExcludeUserAdminAssignment` is `true` and a user with organizations is selected, a prompt and per-organization links are displayed: "To grant or revoke UserAdmin access for this user, use Manage Organization Admins: [Org Name] →".
    *   Links navigate to `/auth/switch-organization?organizationId={id}&returnUrl=/admin/users`.
    *   File: `src/Libs/Dabbler.Security.Blazor/Components/Admin/Users/UserRoleAssignmentDetails.razor`
*   **Dashboard banner on `/admin/users`:**
    *   A banner is now displayed on the `/admin/users` page: "Looking for full role assignment? Open the Security Dashboard → Users and Role Assignment tab".
    *   Visible to users with roles `SystemAdmin`, `CrossOrgUserAdmin`, or `UserAdmin`.
    *   Uses `AuthenticationStateProvider` for Blazor interactivity.
    *   File: `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Admin/UserManagement.razor`
*   **Refined Authorization Gating:**
    *   The cross-link to `/admin/users` is shown when the actor's active role is `SystemAdmin` or `UserAdmin`.
    *   The dashboard banner is shown when the actor's role is `SystemAdmin`, `CrossOrgUserAdmin`, or `UserAdmin`.
    *   Both visibility states are cached on component initialization.

### Bug Fixes

*   **UAT Runner `REPO_ROOT` path:** Corrected `uat_runner/runner.py`'s `REPO_ROOT` calculation from `parents[2]` to `parents[1]` due to repository restructuring.

## Tests

*   **`UserManagementPageTests`:** Added 9 new tests, including 5 theory cases for Dashboard Banner visibility based on actor role.
*   **`UserRoleAssignmentDetailsTests`:** Added 15 new tests, including 5 specifically for `ExcludeUserAdminAssignment` behavior.
*   **UAT Checklist:** Updated `docs/session-sets/admin-users-cross-links/admin-users-cross-links-uat-checklist.json` with 8 DSL steps. All automated UATs passed.

## Known Limitations

*   Human User Acceptance Testing (UAT) is pending for this feature set.
```