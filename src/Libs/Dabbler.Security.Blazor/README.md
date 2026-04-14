# Dabbler.Security.Blazor

Dabbler.Security.Blazor provides the notes-driven runtime authorization UI for
Blazor Server applications. The supported core surface centers on secured
rendering, role-aware navigation, runtime permission editing, and the security
dashboard/admin pages that the app and dedicated tests already prove.

Authentication pages for magic link login, MFA, and external-login workflows
remain available, but that broader identity breadth is product- and
policy-shaped rather than the main reusable-surface contract this repo is
currently simplifying.

## Quick Start

Register the necessary services in your `Program.cs`:

```csharp
builder.Services.AddDabblerSecurityBlazor();
```

This registers MudBlazor services, cascading authentication state, and a `DelegatingHandler` for passing user identity to downstream API calls.

## Components

### Authentication Pages

| Component | Route | Description |
|---|---|---|
| `LoginPage` | `/login` | Magic link login with optional external OIDC/OAuth entry points. |
| `MfaPage` | `/mfa` | TOTP setup and verification when a product policy enables MFA. |

### Admin Pages

| Component | Route | Description |
|---|---|---|
| `UserListPage` | `/admin/users` | List and manage application users. |
| `UserEditPage` | `/admin/users/{id}` | Edit user details and metadata. |
| `RoleListPage` | `/admin/roles` | View and manage security roles. |
| `RoleAssignmentPage` | `/admin/roles/assign` | Assign and remove roles from users. |
| `AuditLogPage` | `/admin/audit` | View a chronological audit trail of security events. |

### Supported Core Components

| Component | Description |
|---|---|
| `DabblerSecurityNav` | Navigation menu item for admin pages; automatically appears for authorized users. |
| `DabblerSecured` | Wrapper component that declaratively enforces page, field, and control permissions. |
| `DabblerRoleSwitcher` | Dropdown for users with multiple roles to switch their active role. |

### Admin Dashboard Components

| Component | Description |
|---|---|
| `DabblerSecurityDashboard` | Central admin panel with a tabbed interface for all permission management. |
| `DabblerRoleManager` | Create, edit, and delete roles with inheritance configuration. |
| `DabblerUserRoleManager` | Manage user-to-role assignments. |
| `DabblerPagePermissionMatrix` | Matrix UI for configuring page-level access by role. |
| `DabblerEntityPermissionMatrix` | Matrix UI for configuring entity CRUD permissions by role. |
| `DabblerFieldPermissionEditor` | Configure field-level access (Editable, ReadOnly, Hidden) by role. |
| `DabblerEndpointPermissionMatrix` | Matrix UI for configuring API endpoint access by role. |
| `DabblerControlPermissionEditor` | Configure UI control states (Enabled, Disabled, Hidden) by role. |
| `DabblerControlPermissionTab` | Tabbed interface for control permission editing within the dashboard. |
| `DabblerRowFilterEditor` | Configure per-role data scoping filters for row-level security. |
| `DabblerRowFilterTab` | Tabbed interface for row filter editing within the dashboard. |

## Current Contract Notes

- The retained supported contract is the runtime authorization story:
  secured rendering, role switching, security navigation, permission editing,
  and row filters.
- The admin/dashboard pages remain part of the supported core because the app
  and dedicated `tests/Dabbler.Security.Blazor.Tests/` suite exercise them
  directly.
- Broader external-login and MFA breadth remains available, but should be
  treated as product/policy configuration rather than the default reusable
  platform story.

## Admin Dashboard Setup

Add the central security dashboard to any page:

```razor
@page "/admin/security"

<DabblerSecurityDashboard />
```

## DabblerSecured Component Usage

Wrap any content that requires role-based permissions. The component automatically handles visibility and state based on the current user's role:

```razor
<DabblerSecured PageKey="Dashboard" ControlKey="ExportButton">
    <MudButton>Export</MudButton>
</DabblerSecured>
```

## Role Switcher Setup

Add the role switcher to your layout or navigation bar. It renders a dropdown for users assigned more than one role:

```razor
<DabblerRoleSwitcher />
```

## Dependencies

- [Dabbler.Security](https://www.nuget.org/packages/Dabbler.Security/) -- Core package
- [MudBlazor](https://www.nuget.org/packages/MudBlazor/) (>= 7.15.0)
- [Microsoft.AspNetCore.Components.Web](https://www.nuget.org/packages/Microsoft.AspNetCore.Components.Web/)

## License

This package is licensed under the [MIT License](https://opensource.org/licenses/MIT).
