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
| `DabblerSecurityDashboard` | Central admin panel. Under the restriction-based model, it exposes five tabs: Role Master Details, Default Masks, Organization Master Details, Users and Role Assignment, and Role Documentation. Each tab is gated by the active baked-in role. |
| `RestrictedFormField` / `RestrictedGridColumn` | Render fields and grid columns subject to the active role's `ColumnRestriction` rules and any `DefaultMask` + `MaskExemption` resolution. |

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
- [MudBlazor](https://www.nuget.org/packages/MudBlazor/) (8.x)
- [Microsoft.AspNetCore.Components.Web](https://www.nuget.org/packages/Microsoft.AspNetCore.Components.Web/)

## License

This package is licensed under the [MIT License](https://opensource.org/licenses/MIT).
