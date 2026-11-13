namespace Dabbler.Security.Authorization;

/// <summary>
/// Compiled privilege surface for a baked-in role. Authorization handlers
/// consult the concrete subclass for the user's baked-in role before any
/// JSON-configured restriction lookup. Applications cannot alter these
/// values: they are the immutable contract that makes baked-in roles
/// different from user-type roles.
///
/// Every flag defaults to <c>false</c>. Concrete subclasses override only
/// the flags that apply, keeping each role's capability set visibly
/// self-describing.
/// </summary>
public abstract class BakedInRolePrivileges
{
    /// <summary>SystemAdmin short-circuit. When <c>true</c> handlers allow any requirement.</summary>
    public virtual bool HasAllPrivileges => false;

    /// <summary>
    /// <c>true</c> for <see cref="BakedInRoles.User"/>. The other four baked-in roles
    /// are non-user-type (administrative or monitoring roles) and have a different
    /// assignment contract.
    /// </summary>
    public virtual bool IsUserType => false;

    /// <summary>
    /// CrossOrgMonitor only. Write and configuration handlers fail for read-only
    /// baked-in roles even if no explicit restriction is configured.
    /// </summary>
    public virtual bool IsReadOnly => false;

    /// <summary>SystemAdmin only — can create, edit, or archive user-type roles.</summary>
    public virtual bool CanManageRoles => false;

    /// <summary>SystemAdmin only — can configure user-type role restrictions and defaults.</summary>
    public virtual bool CanConfigureRolePrivileges => false;

    /// <summary>SystemAdmin or CrossOrgUserAdmin — can create, edit, or archive organizations.</summary>
    public virtual bool CanManageOrganizations => false;

    /// <summary>SystemAdmin, CrossOrgUserAdmin, or UserAdmin — can assign any role to a user in their scope.</summary>
    public virtual bool CanAssignUsersToRoles => false;

    /// <summary>
    /// SystemAdmin, CrossOrgUserAdmin, or CrossOrgMonitor — bypasses the per-organization
    /// EF global query filter so they can view and report across organizations.
    /// </summary>
    public virtual bool CanSeeAllOrganizations => false;

    /// <summary>SystemAdmin only — can assign <see cref="BakedInRoles.CrossOrgUserAdmin"/> to a user.</summary>
    public virtual bool CanAssignCrossOrgUserAdmin => false;

    /// <summary>SystemAdmin or CrossOrgUserAdmin — can assign <see cref="BakedInRoles.CrossOrgMonitor"/>.</summary>
    public virtual bool CanAssignCrossOrgMonitor => false;

    /// <summary>
    /// CrossOrgUserAdmin only — may self-assign the role so vacation/illness coverage can
    /// be granted without waiting for SystemAdmin.
    /// </summary>
    public virtual bool CanSelfAssignOwnRole => false;

    /// <summary>
    /// SystemAdmin or CrossOrgUserAdmin — may grant MaskExemption rows. Authority rules
    /// for which target roles are permitted, and whether an expiry is required, live in
    /// the Default Masks / Mask Exemptions configuration model, not here.
    /// </summary>
    public virtual bool CanGrantMaskExemptions => false;
}

/// <summary>
/// Privileges for <see cref="BakedInRoles.SystemAdmin"/>. Has all platform privileges.
/// The only role that can create, edit, or delete roles and configure privileges.
/// </summary>
public sealed class SystemAdminPrivileges : BakedInRolePrivileges
{
    /// <summary>Singleton instance used by handlers and the default baked-in role provider.</summary>
    public static SystemAdminPrivileges Instance { get; } = new();
    private SystemAdminPrivileges() { }

    /// <inheritdoc />
    public override bool HasAllPrivileges           => true;
    /// <inheritdoc />
    public override bool CanManageRoles             => true;
    /// <inheritdoc />
    public override bool CanConfigureRolePrivileges => true;
    /// <inheritdoc />
    public override bool CanManageOrganizations     => true;
    /// <inheritdoc />
    public override bool CanAssignUsersToRoles      => true;
    /// <inheritdoc />
    public override bool CanSeeAllOrganizations     => true;
    /// <inheritdoc />
    public override bool CanAssignCrossOrgUserAdmin => true;
    /// <inheritdoc />
    public override bool CanAssignCrossOrgMonitor   => true;
    /// <inheritdoc />
    public override bool CanGrantMaskExemptions     => true;
}

/// <summary>
/// Privileges for <see cref="BakedInRoles.CrossOrgUserAdmin"/>. A cross-organization
/// UserAdmin plus organization management; can self-assign to cover absences and
/// can grant Mask Exemptions subject to the separation-of-duties rules.
/// </summary>
public sealed class CrossOrgUserAdminPrivileges : BakedInRolePrivileges
{
    /// <summary>Singleton instance used by handlers and the default baked-in role provider.</summary>
    public static CrossOrgUserAdminPrivileges Instance { get; } = new();
    private CrossOrgUserAdminPrivileges() { }

    /// <inheritdoc />
    public override bool CanManageOrganizations   => true;
    /// <inheritdoc />
    public override bool CanAssignUsersToRoles    => true;
    /// <inheritdoc />
    public override bool CanSeeAllOrganizations   => true;
    /// <inheritdoc />
    public override bool CanAssignCrossOrgMonitor => true;
    /// <inheritdoc />
    public override bool CanSelfAssignOwnRole     => true;
    /// <inheritdoc />
    public override bool CanGrantMaskExemptions   => true;
}

/// <summary>
/// Privileges for <see cref="BakedInRoles.CrossOrgMonitor"/>. Read-only cross-organization
/// visibility. No create/update/delete, no role administration, no role assignment.
/// </summary>
public sealed class CrossOrgMonitorPrivileges : BakedInRolePrivileges
{
    /// <summary>Singleton instance used by handlers and the default baked-in role provider.</summary>
    public static CrossOrgMonitorPrivileges Instance { get; } = new();
    private CrossOrgMonitorPrivileges() { }

    /// <inheritdoc />
    public override bool IsReadOnly             => true;
    /// <inheritdoc />
    public override bool CanSeeAllOrganizations => true;
}

/// <summary>
/// Privileges for <see cref="BakedInRoles.UserAdmin"/>. Delegated user administration
/// within the organizations the user is assigned to. Can assign any role (including
/// user-type roles they do not themselves hold) but cannot manage roles or organizations.
/// </summary>
public sealed class UserAdminPrivileges : BakedInRolePrivileges
{
    /// <summary>Singleton instance used by handlers and the default baked-in role provider.</summary>
    public static UserAdminPrivileges Instance { get; } = new();
    private UserAdminPrivileges() { }

    /// <inheritdoc />
    public override bool CanAssignUsersToRoles => true;
}

/// <summary>
/// Privileges for <see cref="BakedInRoles.User"/>. The only baked-in user-type role.
/// Default-permissive CRUD on transactional data within the user's organization(s),
/// shaped entirely by any <c>ColumnRestriction</c>, <c>RowRestriction</c>, or
/// <c>UiRestriction</c> configured for this role.
/// </summary>
public sealed class UserPrivileges : BakedInRolePrivileges
{
    /// <summary>Singleton instance used by handlers and the default baked-in role provider.</summary>
    public static UserPrivileges Instance { get; } = new();
    private UserPrivileges() { }

    /// <inheritdoc />
    public override bool IsUserType => true;
}
