# Refresh response for `src/Libs/Dabbler.Security.Blazor/README.md`

- Model: gemini-pro
- Input tokens: 1827
- Output tokens: 933
- Cost: $0.0116

---

```markdown
# Dabbler.Security.Blazor

Dabbler.Security.Blazor provides UI components for runtime authorization in Blazor Server applications. It enables a declarative, permission-based model for:

*   Securing pages, UI controls, and data fields (`DabblerSecured`).
*   Role-aware navigation menus (`DabblerSecurityNav`).
*   Switching between active roles for multi-role users (`DabblerRoleSwitcher`).
*   A comprehensive security dashboard for managing users, roles, and permissions.

While components for authentication workflows (e.g., magic link, MFA) are available, the core library contract focuses on this runtime authorization surface. The admin pages and dashboard are fully supported and exercised by the `Dabbler.Security.Blazor.Tests` suite.

## Quick Start

Register the necessary services in your `Program.cs`:

```csharp
builder.Services.AddDabblerSecurityBlazor();
```

This extension method registers MudBlazor services, cascading authentication state, and a `DelegatingHandler` for passing the user's identity to downstream API calls.

## Components

### Core Components

| Component | Description |
|---|---|
| `DabblerSecured` | A wrapper component that declaratively enforces permissions on any content, automatically handling visibility based on the user's active role. |
| `DabblerSecurityNav` | A navigation menu item that provides a link to admin pages and only appears for authorized users. |
| `DabblerRoleSwitcher` | A dropdown that allows users with multiple roles to switch their active role for the current session. |

### Admin & Security Pages

| Component | Route | Description |
|---|---|---|
| `DabblerSecurityDashboard` | `/admin/security` | A dashboard for security administration. It includes tabs for managing roles, default data masks, organizations, user-role assignments, and role documentation. Access to each tab is controlled by the user's active role. |
| `UserListPage` | `/admin/users` | Lists and manages application users. |
| `UserEditPage` | `/admin/users/{id}` | Edits user details and metadata. |
| `RoleListPage` | `/admin/roles` | Views and manages security roles. |
| `RoleAssignmentPage` | `/admin/roles/assign` | Assigns and removes roles from users. |
| `AuditLogPage` | `/admin/audit` | Displays a chronological audit trail of security-related events. |

### Authentication Pages

These components support common authentication patterns but may require product-specific policy configuration.

| Component | Route | Description |
|---|---|---|
| `LoginPage` | `/login` | Magic link login with optional external OIDC/OAuth entry points. |
| `MfaPage` | `/mfa` | TOTP setup and verification. |

## Usage

### Securing UI with `DabblerSecured`

Wrap any content that requires role-based permissions. The component handles visibility based on the current user's role and the permissions defined for the given keys.

```razor
<DabblerSecured PageKey="Dashboard" ControlKey="ExportButton">
    <MudButton Color="Color.Primary">Export</MudButton>
</DabblerSecured>
```

### Adding the Security Dashboard

Add the central security dashboard to any page, typically an admin-only route.

```razor
@page "/admin/security"
@attribute [Authorize(Roles = "SystemAdmin")]

<DabblerSecurityDashboard />
```

### Adding the Role Switcher

Place the role switcher in a shared layout or navigation bar. It automatically renders a dropdown for users assigned to more than one role.

```razor
<DabblerRoleSwitcher />
```

## Dependencies

- [Dabbler.Security](https://www.nuget.org/packages/Dabbler.Security/)
- [MudBlazor](https://www.nuget.org/packages/MudBlazor/) (>= 7.15.0)
- [Microsoft.AspNetCore.Components.Web](https://www.nuget.org/packages/Microsoft.AspNetCore.Components.Web/)

## License

This package is licensed under the [MIT License](https://opensource.org/licenses/MIT).
```

---
**Verification (gpt-5-4-mini, openai):** ISSUES_FOUND

**Issue 1** [? / ?]: S FOUND

The revision is broadly accurate, but it is not fully complete and includes one potentially misleading example:

1. **Missing public component**
   - The original doc included `DabblerUserRoleManager` as a supported admin dashboard component. The revised version drops it entirely.
   - If that component is still shipped, this is a consumer-facing omission.

2. **Potentially misleading authorization example**
   - The usage example adds `@attribute [Authorize(Roles = "SystemAdmin")]`.
   - The platform truth does not establish that `SystemAdmin` is the required access model for `DabblerSecurityDashboard`, and the package supports multiple baked-in roles plus JSON-configured roles. This example may overstate a specific policy as if it were the canonical requirement.

3. **Completeness could be tighter around the supported surface**
   - The original doc explicitly framed the package around runtime authorization, secured rendering, role-aware navigation, runtime permission editing, and the admin/dashboard pages that are exercised by tests.
   - The rewrite covers most of that, but it no longer clearly preserves the distinction between the core reusable surface and the broader authentication workflows, nor does it mention the row-filter / permission-editing angle as directly as the source did.

I would refresh this before shipping, mainly to restore the omitted component and remove or generalize the role-specific authorization example.
