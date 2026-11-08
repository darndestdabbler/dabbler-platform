# Dabbler.Security.Blazor

> Part of the Dabbler Platform. See
> [`docs/platform-overview.md`](../../../docs/platform-overview.md) for
> the platform-level view of how this library fits in.

The Blazor Server UI surface for
[`Dabbler.Security`](../Dabbler.Security/README.md): secured-rendering
components, the security-administration dashboard, role and
organization switchers, restriction-aware grid and form binders, and
the authentication pages (login, MFA, external-login callback).

## Audience

Blazor Server teams already adopting `Dabbler.Security` who want the
matching UI surface — admin pages, dashboard, role-aware navigation,
declarative security wrappers — without reimplementing them. The
package is the consumer-facing complement to `Dabbler.Security`'s
service-level model.

## Prerequisites and stack constraints

- Blazor Server
- MudBlazor 8.x
- .NET 10 (current LTS)
- Reference to [`Dabbler.Security`](../Dabbler.Security/README.md)
  (this library extends its service-level model into UI)
- Reference to [`Dabbler.Components`](../Dabbler.Components/README.md)
  (for the underlying grid + form primitives)

## Install

> **Adoption note:**
> The recommended adoption model for internal projects is **integrated source** under
> `src/Libs/Dabbler.Security.Blazor/`. NuGet packages are available;
> to install the package:
> `dotnet add package Dabbler.Security.Blazor --prerelease`

## Minimal registration

```csharp
builder.Services.AddDabblerSecurityBlazor();
```

This registers MudBlazor services, cascading authentication state,
restriction-aware renderers, and a `DelegatingHandler` for passing user
identity to downstream API calls. It complements
`AddDabblerSecurity(...)` from the service-level library.

## Smallest working example

A page that mounts the central security dashboard with role-aware
tabs:

```razor
@page "/admin/security"

<DabblerSecurityDashboard />
```

A typical wrapper around a privileged control:

```razor
<DabblerSecured PageKey="Dashboard" ControlKey="ExportButton">
    <MudButton OnClick="@ExportAsync">Export</MudButton>
</DabblerSecured>
```

A layout that exposes role switching for multi-role users:

```razor
@* MainLayout.razor *@
<MudAppBar>
    <DabblerRoleSwitcher />
</MudAppBar>
```

## Supported components

### Authentication pages

| Component | Route | Description |
|-----------|-------|-------------|
| `LoginPage` | `/login` | Magic-link login with optional external OIDC/OAuth entry points. |
| `MfaPage` | `/mfa` | TOTP setup and verification when product policy enables MFA. |

### Admin pages

| Component | Route | Description |
|-----------|-------|-------------|
| `UserListPage` | `/admin/users` | List and manage application users. |
| `UserEditPage` | `/admin/users/{id}` | Edit user details and metadata. |
| `RoleListPage` | `/admin/roles` | View and manage security roles. |
| `RoleAssignmentPage` | `/admin/roles/assign` | Assign and remove roles from users. |
| `AuditLogPage` | `/admin/audit` | View a chronological audit trail of security events. |

### Core authorization-UI components

| Component | Description |
|-----------|-------------|
| `DabblerSecurityNav` | Navigation menu item for admin pages; automatically appears for authorized users (gated by baked-in role). |
| `DabblerSecured` | Declarative wrapper enforcing page, field, or control permissions on its child content. |
| `DabblerRoleSwitcher` | Dropdown for users with multiple roles to switch their active role. |

### Dashboard

| Component | Description |
|-----------|-------------|
| `DabblerSecurityDashboard` | Central admin panel exposing five tabs: Role Master Details, Default Masks, Organization Master Details, Users and Role Assignment, Role Documentation. Each tab is gated by the active baked-in role. |
| `RestrictedFormField` / `RestrictedGridColumn` | Render fields and grid columns subject to the active role's `ColumnRestriction` rules and `DefaultMask` + `MaskExemption` resolution. |

## Current contract notes

- The retained supported contract is the runtime authorization story:
  secured rendering, role switching, security navigation, permission
  editing, and row filters.
- The admin / dashboard pages are part of the supported core — the
  demo app and `tests/Dabbler.Security.Blazor.Tests/` exercise them
  directly.
- Broader external-login and MFA breadth remains available, but should
  be treated as product / policy configuration rather than the default
  reusable platform story.

## Related packages

- [`Dabbler.Security`](../Dabbler.Security/README.md) — service-level
  authorization model (restrictions, masking, baked-in roles,
  authentication). This library cannot function without it.
- [`Dabbler.Components`](../Dabbler.Components/README.md) — the CRUD
  primitives that `RestrictedFormField` / `RestrictedGridColumn` wrap.
- [`Dabbler.Testing.Blazor`](../Dabbler.Testing.Blazor/README.md) —
  watermark instrumentation for Playwright E2E tests of admin /
  dashboard pages.

## Warnings and caveats

- **Disabled-state and hidden-control tests should sign in as the role
  whose product definition mandates that absence**, not a privileged
  role whose product direction might widen later. Asserting
  "SystemAdmin cannot see X" is brittle; assert under the user-type
  role whose `UiRestriction` actually defines the absence. See
  `docs/planning/lessons-learned.md` → *Hard-Asserted Disabled-State
  Tests Become Regressions When The Feature Gets Enabled*.
- **`MudDataGrid` `data-testid` lands on the `<input>`, not the wrapper**
  (a MudBlazor 8.x convention). Test selectors should account for this.
- **`MudTextField ReadOnly="true"` does NOT set the HTML `readonly`
  attribute** in MudBlazor 8.x; tests asserting read-only state must
  use behavior-level proxies (e.g., toggle-button text).
- **License: MIT** (see [LICENSE](https://licenses.nuget.org/MIT)).

## If this library stops being maintained

This package is MIT-licensed, and its source is in
[`src/Libs/Dabbler.Security.Blazor`](https://github.com/darndestdabbler/dabbler-platform/tree/master/src/Libs/Dabbler.Security.Blazor).
If it stops being maintained, copy that source into your application,
replace the package reference with a project reference to your copy, and
own it from there. Keep the MIT license notice with the copied code.
