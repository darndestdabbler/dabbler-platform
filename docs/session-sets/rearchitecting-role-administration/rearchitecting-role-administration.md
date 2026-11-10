# Rearchitecting Role Administration

> **Purpose:** Both simplify and extend how role is administered.
> **Status:** Informs the multi-session-set roadmap at
> `C:\Users\denmi\.claude\plans\ok-i-did-some-abstract-blossom.md`.
> **Created:** 2026-04-19 — updated same day to incorporate the
> decisions from pre-flight design review (see "Decisions confirmed
> during design review" at the bottom).

---

## Baked-In Roles

Every application has the following five baked-in roles. They are
defined in code (constants + compiled privilege rules) and cannot
be altered. An `IBakedInRoleProvider` DI contract allows a
consuming application to register **additional** non-user-type
roles at startup with their own compiled privilege rules; this
keeps the platform simple today while leaving room for future
extension.

Every role — baked-in and app-defined — carries an optional
`DisplayAlias` that SystemAdmin can set per deployment. The
canonical `Name` is the immutable code identifier; the alias is
what users see in dashboards, role-switcher menus, and audit
logs.

1. SystemAdmin
    - This role has all privileges in the application.
    - This is the only role that can create, edit, or delete roles.
    - This is the only role that can configure role privileges and
      restrictions for user-type roles.
    - This role can create, edit, and archive organization entities.
    - This is the only role that can assign users to the
      CrossOrgUserAdmin role **initially** (see CrossOrgUserAdmin
      for coverage assignments).
    - This is a ***non-user-type role***.
        - Users assigned to a non-user-type role cannot be assigned
          to another role, except for UserAdmin roles (which can be
          assigned to another role for a different organization) or
          for the narrow purpose of holding a `MaskExemption` on an
          unmask-capable auxiliary role (see "Sensitive-Field
          Masking" below).
2. CrossOrgUserAdmin
    - This is a special role for a user who is a UserAdmin (see
      below) in all defined organizations, as well as a user who
      can provide operational assistance to another organization
      (enter or update data on their behalf).
    - This role is typically not used when there is just one
      organization defined (e.g., Internal Organization).
    - This role can create, edit, and archive organization entities.
    - This role cannot create, edit, or delete roles.
    - This role cannot configure role privileges for user-type
      roles.
    - This role **can assign the CrossOrgUserAdmin role to another
      person** (so the role can be covered during vacation or
      illness).
    - This role and the SystemAdmin role are the only two roles
      that can assign users to a CrossOrgMonitor role.
    - This role is also responsible for granting mask exemptions
      that target SystemAdmin, CrossOrgMonitor, or
      CrossOrgUserAdmin (the last being self-granted only). All
      such exemptions must be time-bound.
    - This is a non-user-type role.
3. CrossOrgMonitor
    - This is a special role for a user who can view cross-
      organization data and run cross-organization reports.
    - This role is typically not used when there is just one
      organization defined (e.g., Internal Organization).
    - This role cannot create, edit, or delete roles.
    - This role cannot configure role privileges.
    - This role cannot assign users to roles.
    - The role does not have create, update, or delete privileges
      on any data.
    - This is a non-user-type role.
4. UserAdmin
    - This role is defined so that user administration (role
      assignment) can be delegated by the SystemAdmin to business
      users.
    - This role cannot create, edit, or delete roles.
    - This role cannot configure role privileges.
    - This role cannot see records for organizations to which they
      are not assigned and with the org active.
    - This role can assign any role to any person in one of their
      organizations.
    - This role has no other data or UI restrictions.
    - This is a non-user-type role.
5. User
    - This role is the main role that performs CRUD operations on
      transactional data.
    - This role is the only baked-in ***user-type*** role.
        - User-type roles cannot create, edit, or delete roles.
        - User-type roles cannot configure role privileges.
        - User-type roles cannot see records for organizations to
          which they are not assigned and with the org active.
        - User-type roles cannot assign roles to any person.
        - By default, user-type roles have no other data or UI
          restrictions.

The SystemAdmin, CrossOrgUserAdmin, CrossOrgMonitor, and UserAdmin
roles cannot be altered in their compiled privilege rules. They DO
appear in the new Role Master Details grid (greyed, with a
"baked-in" chip) so that their `DisplayAlias` and any
`MaskExemption` targeting them have a visible home; other
sub-tabs (Column / Row / UI Restrictions) are not shown for
baked-in rows.

Sometimes the above roles are insufficient because different users
have different roles in an application. In such an application,
the User role can be supplemented by defining other ***user-type***
roles, for example:

- One or more reviewer-approver roles
- A role that can see and edit sensitive personally identifiable
  information (e.g., social security number) via `MaskExemption`
  grants

---

## Multi-Role Support

Additional complexity occurs when users are assigned to multiple
user-type roles within the same organization. When this happens,
role privileges have to be resolved using rules. Although it
places more work on system administrators to configure roles, it
is easier to assign users to only one role within each
organization.

As a compromise, this security framework allows users to be
assigned to more than one role in an organization, but users must
explicitly switch their role when they want to operate within a
particular role in the application. This role-switching
architecture allows the following:

- `RoleId` is a baked-in system column for transactional entities
  (see the `transactional-system-columns` session set).
- Role is part of the audit trail for records.
- No multi-role resolution processes (e.g., greatest-privilege
  rule) are required.

---

## Multi-Organization (Limited Multi-Tenant) Support

As stated elsewhere, the application should have support for
multiple organizations. Multi-organization support has several
characteristics:

1. By default, applications have a single organization (e.g.,
   Internal Organization). This models the situation that most
   typifies Microsoft Access database solutions.
2. Typically, a user would be assigned to a single organization;
   however, a SystemAdmin, CrossOrgUserAdmin, or UserAdmin
   assigned to organization X could additionally assign the user
   to organization X.
3. For all roles other than SystemAdmin and CrossOrgUserAdmin,
   roles are always organization-specific. A user who is assigned
   to one role for one organization may be assigned to a different
   role in a different organization.
4. To provide multi-organization (limited multi-tenant) support,
   all transactional entities in the data model need to have
   `OrganizationId` as a standard, system attribute/column. A
   global EF query filter restricts visibility to the active
   organization except for users with SystemAdmin,
   CrossOrgUserAdmin, or CrossOrgMonitor roles.
5. Aside from users with a SystemAdmin, CrossOrgUserAdmin, or
   CrossOrgMonitor role, a user who is assigned to multiple
   organizations must explicitly switch their organization when
   working within the UI for another organization.
    - A UserAdmin must switch their organization when assigning
      other users to that organization and assigning roles within
      that organization.
    - A User (or other user-type role) must switch their
      organization in order to create, edit, or delete (if
      allowed) data for that organization.

---

## Typical Role Use Cases and User-Type Roles

It is anticipated that most applications will have very simplistic
role differentiation needs. In addition to having one or more
users with SystemAdmin privileges and one or more users with
UserAdmin privileges, the typical application will need the rest
of the users to have a User role, as defined above.

As mentioned above, in some cases a single user-type role (User)
will be insufficient. In those cases, the SystemAdmin role may
define additional user-type roles. In all cases, the default set
of privileges for a user-type role will be the same set of
privileges as the defaults for a User role. As such, these
defaults are simply called **user-type default privileges**.

---

## Role Restriction Approach

Because all non-user-type roles have predefined privileges and all
user-type roles by default have the most permissive CRUD and UI
privileges within an organization, this greatly simplifies role
configuration for an application. In particular, for any given
user-type role, the SystemAdmin only needs to apply one or more
explicit restrictions on permissions.

For any specified user-type role, the following explicit
restrictions are available:

1. ***Data column restrictions*** For any specified table and
   column, an explicit restriction means that
    - the defined user-type role either
        - cannot see and cannot edit, or
        - can see but cannot edit
    - all relevant data-bound controls on all pages respect this
      column restriction
        - when the restriction is *cannot see and cannot edit*,
          associated data-bound controls are hidden (and disabled)
        - when the restriction is *see but cannot edit*, associated
          data-bound controls are merely disabled
    - Note: field masking is **not** a column-restriction kind.
      Masking is modelled separately as platform-level Default
      Masks plus per-role Mask Exemptions (see next section).
2. ***Data row restrictions*** For any specified table, an
   explicit restriction means that
    - the defined user-type role can only see records matching a
      specified filter
    - the filter is automatically applied to all relevant grids
      and pages with data models
3. ***Non-data-related UI restrictions*** Additional restrictions
   can be applied to a user-type role, including:
    - the role cannot see a specified page. In such a situation,
        - associated menu items are not visible
        - when the user attempts to navigate directly to a page,
          the user receives a 403 (*Forbidden*) response (with
          appropriate UI styling)
    - specified controls on a page are either disabled or hidden

---

## Sensitive-Field Masking

Masking is modelled separately from restrictions because its
shape is inverted: masks are a **platform-wide default** with
**per-role exceptions**, not a per-role rule with a platform
default. This keeps the security-by-default property: declare a
column sensitive once, and every role sees masked until explicitly
exempted.

### Platform-level Default Masks

The SystemAdmin declares `{ Table, Column, MaskFormat }` rows in
the Default Masks tab. `MaskFormat` supports tokens like `{last4}`
and `{firstChar}`; a blank format renders `********`. The tab
includes a **sensitive-name safety net** that lists any column in
the metadata registry whose name matches common sensitive-column
patterns (ssn, taxid, ein, dob, password, ccnum, salary) but is
not yet in the Default Masks list, with a one-click "Add to
Default Masks" button.

### Per-role Mask Exemptions

Each `MaskExemption` carries
`{ EntityKey, ColumnKey, TargetRoleName, GrantedBy, GrantedAt,
ExpiresAt? }`. A null `ExpiresAt` is permanent; a non-null one is
time-bound. Exemptions live in a top-level `MaskExemptions` list
in `SecurityConfiguration` so they can target both user-type and
non-user-type roles.

Authorization rules (separation of duties):

- Exemptions targeting **user-type roles** or **UserAdmin**: can
  be granted by SystemAdmin OR CrossOrgUserAdmin, with or without
  `ExpiresAt`.
- Exemptions targeting **SystemAdmin** or **CrossOrgMonitor**: can
  be granted only by CrossOrgUserAdmin and **must** have a
  non-null `ExpiresAt`.
- Exemptions targeting **CrossOrgUserAdmin**: self-granted only
  (same user), **must** have a non-null `ExpiresAt`. This gives
  that role bounded self-service unmasking for legitimate review
  work while keeping it monitored in the audit log.

### Null-data rule

When a column is default-masked and the active role has no active
exemption, the mask renders **regardless of whether the stored
value is null**. Showing "empty" for a sensitive-when-present
field would itself leak information (e.g., presence of an HIV
diagnosis date). The rendered control also applies a distinct
"masked" visual treatment (dark background in light mode, lighter
background in dark mode — or a subtle shield/lock adornment; the
exact styling is pinned during implementation).

### Audit

Both the role switch that activates an exemption and any
subsequently rendered unmasked read are written to an
`AppSensitiveAccessLog` temporal entity with event kinds
`RoleSwitch | FieldUnmaskedRead | ExemptionGranted | ExemptionExpired`.
Unmasked reads of non-sensitive columns produce no audit traffic.
Reads within one grid render share a `CorrelationId` so one
page produces one audit row per record, not one per column.

---

## Recommended UI implementation

The security dashboard has **five** top-level tabs. Each
master-detail grid (Role Master Details, Default Masks,
Organization Master Details, Users and Role Assignment, and the
optional Compliance Review audit viewer from Set D) sits under a
`FilterPanel<T>` reusing the existing platform QBE filter model.

1. **Role Master Details Tab**
    - Only present for SystemAdmin.
    - Allows for
        - Searching (via `FilterPanel<T>`) for existing roles by
          canonical name, alias, or description
        - Defining new user-type roles
        - Archiving user-type roles
        - Inline-editing each role's `DisplayAlias` (including for
          baked-in roles, which are otherwise non-editable)
    - Baked-in roles appear greyed with a "baked-in" chip so that
      their alias and any targeting `MaskExemption` have a visible
      home. Add/Archive actions are enabled only on user-type rows.
    - In the Details section, the set of sub-tabs depends on the
      selected row:
        - **User-type roles** show four sub-tabs: Data column
          restrictions, Data row restrictions, Other UI
          restrictions, and Mask Exemptions.
        - **Baked-in roles** show only the Mask Exemptions sub-tab
          plus a read-only panel summarizing the role's compiled
          privileges.
    - Each sub-tab is an inline-editable grid:
        - **Data column restrictions**
            - First grid column has Table dropdown
            - Second grid column has Column dropdown (cascading
              filter from Table dropdown selection)
            - Third grid column has a MudSwitch that can toggle
              between View Only and Hidden
                - Note: we do not need an Allow option because
                  when there is no entry in this grid, it is
                  allowed.
        - **Data row restrictions**
            - First grid column has Table dropdown
            - Second grid column has filter expression
        - **Other UI restrictions**
            - First grid column has Page dropdown
            - Second grid column has Control dropdown (cascading
              filter from Page dropdown selection). Note that the
              first item in the list is "Entire Page"
            - Third grid column has a MudSwitch that can toggle
              between Disabled Only and Hidden
                - Note: we do not need an Allow option because
                  when there is no entry in this grid, it is
                  allowed.
        - **Mask Exemptions**
            - Columns: Table, Column, Granted By, Granted At,
              Expires At (editable, optional), Status
            - Source data is the platform's Default Masks list —
              you can only exempt a column that's declared
              sensitive
            - The grid enforces the authorization rules from the
              Sensitive-Field Masking section above; expired rows
              render struck-through with a "Renew" action
2. **Default Masks Tab**
    - SystemAdmin-only.
    - `FilterPanel<T>` over table name, column name, and mask
      format.
    - Inline-editable grid of `{ Table, Column, MaskFormat }`.
    - Collapsible "sensitive-name safety net" panel below the
      grid listing undeclared columns whose names match common
      sensitive patterns.
3. **Organization Master Details Tab**
    - Where SystemAdmin and CrossOrgUserAdmin roles can create and
      edit organization data.
    - Provides `FilterPanel<T>` at the top, then the master grid,
      then details at the bottom.
4. **Users and Role Assignment**
    - Where SystemAdmin, CrossOrgUserAdmin, and UserAdmin roles
      assign persons to roles within an organization.
    - Provides `FilterPanel<T>` at the top, then the master grid,
      then details at the bottom.
5. **Role Documentation**
    - This provides detailed information on the non-user-type
      roles, as well as the defaults for user-type roles, and the
      Default-Masks / Mask-Exemption semantics.

An optional **6th top-level tab** (or peer
`/admin/sensitive-access-log` page) hosts the Compliance Review
audit viewer; placement is decided during Set D Session 1.

---

## Decisions confirmed during design review (2026-04-19)

These are recorded here for traceability; the authoritative
roadmap is in the plan file
`C:\Users\denmi\.claude\plans\ok-i-did-some-abstract-blossom.md`.

1. Baked-in roles live as code constants plus an
   `IBakedInRoleProvider` DI hook; user-type roles stay JSON.
2. Canonical role names are `SystemAdmin`, `CrossOrgUserAdmin`,
   `CrossOrgMonitor`, `UserAdmin`, `User`. `CrossOrgUserAdmin` can
   self-assign the role for vacation/illness coverage.
3. AcmeCoffee migration: `BackofficeUserAdmin` →
   `CrossOrgUserAdmin`; `BackofficeUser` and `Customer` stay as
   app-defined user-type roles, differentiated by org assignment
   and row restrictions, distinguished by `DisplayAlias`.
4. Old files (grant-based matrix components,
   `RoleInheritanceHelper`, `DenyAllowSwitch`) are deleted
   progressively across Sets A and B.
5. Sensitive-field masking uses platform Default Masks + per-role
   Mask Exemptions with optional `ExpiresAt` and strict authority
   rules (separation of duties).
6. Masks render even when data is null; masked controls get a
   distinct visual treatment.
7. Master-detail grids reuse `FilterPanel<T>` from the existing
   platform filter library.
8. The four session sets execute in order
   A (foundations) → B (dashboard) → D (masking + audit) →
   C (system columns on transactional entities).
