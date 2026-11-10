namespace Dabbler.Security.Configuration;

/// <summary>
/// The root aggregate for runtime authorization configuration. Held in-memory by
/// <see cref="SecurityConfigurationManager"/> and persisted by an
/// <c>ISecurityConfigurationStore</c>.
/// </summary>
/// <remarks>
/// <para>
/// The restriction-based model added in Session 2 of the
/// <c>rearchitect-role-administration-foundations</c> set introduces the four
/// canonical top-level fields — <see cref="UserTypeRoles"/>,
/// <see cref="OrganizationSwitching"/>, <see cref="DefaultMasks"/>, and
/// <see cref="MaskExemptions"/> — plus <see cref="BakedInRoleAliasOverrides"/>
/// carried forward from Session 1.
/// </para>
/// <para>
/// The legacy grant-based fields (<see cref="Roles"/>, <see cref="PagePermissions"/>,
/// etc.) are marked <see cref="ObsoleteAttribute"/> and remain on the type for
/// build compatibility with the current handlers, Blazor admin components, and EF
/// store. Session 3 of the same session set rewrites the authorization handlers to
/// read from the new fields, at which point the legacy fields can be physically
/// removed. Spec step 8 called for deletion in Session 2; the deletion was
/// deferred to keep <c>dotnet build</c> green because the handler rewrite has not
/// yet landed.
/// </para>
/// </remarks>
public sealed class SecurityConfiguration
{
    /// <summary>
    /// App-defined user-type roles. Each carries its own column, row, and UI
    /// restriction lists. Baked-in roles are not stored here — they live as
    /// code constants plus an <c>IBakedInRoleProvider</c> DI hook.
    /// </summary>
    public List<RoleDefinition> UserTypeRoles { get; set; } = [];

    /// <summary>
    /// Per-deployment configuration governing which additional roles may see the
    /// organization switcher UI. See <see cref="Configuration.OrganizationSwitching"/>.
    /// </summary>
    public OrganizationSwitching OrganizationSwitching { get; set; } = new();

    /// <summary>
    /// Platform-level sensitive-column declarations. Every role sees these columns
    /// rendered through their <see cref="DefaultMaskRule.MaskFormat"/> unless an
    /// active <see cref="MaskExemption"/> targets the same entity + column for
    /// the user's active role.
    /// </summary>
    public List<DefaultMaskRule> DefaultMasks { get; set; } = [];

    /// <summary>
    /// Per-role exemptions against the <see cref="DefaultMasks"/> list. Exemptions
    /// may target user-type roles or baked-in roles; authority and expiry rules
    /// are enforced when the exemption is granted (wired in Session Set D).
    /// </summary>
    public List<MaskExemption> MaskExemptions { get; set; } = [];

    /// <summary>
    /// Per-deployment display-alias overrides for baked-in roles. The key is
    /// the canonical baked-in role name (e.g., <c>SystemAdmin</c>); the value
    /// is the label shown in the dashboard, role switcher, and audit UI.
    /// </summary>
    /// <remarks>
    /// Baked-in role canonical names and compiled privileges are immutable.
    /// Only the visible label is overridable, and the default comes from
    /// <c>BakedInRoleDefinition.DefaultDisplayAlias</c> when no override is
    /// present.
    /// </remarks>
    public Dictionary<string, string> BakedInRoleAliasOverrides { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    // -----------------------------------------------------------------------
    // Legacy grant-based fields — scheduled for deletion in Session 3 of the
    // rearchitect-role-administration-foundations set, alongside the handler
    // rewrite. Kept here temporarily so the existing handlers, Blazor admin
    // components, and EF store continue to compile between Session 2 and
    // Session 3.
    // -----------------------------------------------------------------------

    /// <summary>
    /// Legacy: all defined roles (baked-in + user-type) as a flat list.
    /// Prefer <see cref="UserTypeRoles"/>; baked-in roles move to code.
    /// </summary>
    [Obsolete("Replaced by UserTypeRoles for app-defined roles. Baked-in roles now live as code constants under Dabbler.Security.Authorization.BakedInRoles. Scheduled for removal in Session 3 of rearchitect-role-administration-foundations.")]
    public List<RoleDefinition> Roles { get; set; } = [];

    /// <summary>
    /// Legacy: page-level grant entries (AllowedRoles / DeniedRoles).
    /// Replaced by per-role <see cref="UiRestriction"/>.
    /// </summary>
    [Obsolete("Replaced by UiRestriction on each user-type RoleDefinition. Scheduled for removal in Session 3 of rearchitect-role-administration-foundations.")]
    public List<PagePermission> PagePermissions { get; set; } = [];

    /// <summary>
    /// Legacy: entity CRUD grant entries.
    /// User-type roles are now default-permissive; absence of a
    /// <see cref="ColumnRestriction"/> means full access.
    /// </summary>
    [Obsolete("Replaced by default-permissive CRUD plus ColumnRestriction. Scheduled for removal in Session 3 of rearchitect-role-administration-foundations.")]
    public List<EntityPermission> EntityPermissions { get; set; } = [];

    /// <summary>
    /// Legacy: field-level access grants. Replaced by per-role
    /// <see cref="ColumnRestriction"/> (see-only / hidden) plus platform-level
    /// <see cref="DefaultMasks"/> and per-role <see cref="MaskExemptions"/>.
    /// </summary>
    [Obsolete("Replaced by ColumnRestriction plus DefaultMasks / MaskExemptions. Scheduled for removal in Session 3 of rearchitect-role-administration-foundations.")]
    public List<FieldPermission> FieldPermissions { get; set; } = [];

    /// <summary>
    /// Legacy: endpoint-level grant entries. Replaced by baked-in role
    /// privileges plus per-role <see cref="UiRestriction"/>.
    /// </summary>
    [Obsolete("Replaced by baked-in role privileges plus UiRestriction. Scheduled for removal in Session 3 of rearchitect-role-administration-foundations.")]
    public List<EndpointPermission> EndpointPermissions { get; set; } = [];

    /// <summary>
    /// Legacy: UI control state grants. Replaced by per-role
    /// <see cref="UiRestriction"/>.
    /// </summary>
    [Obsolete("Replaced by UiRestriction on each user-type RoleDefinition. Scheduled for removal in Session 3 of rearchitect-role-administration-foundations.")]
    public List<ControlPermission> ControlPermissions { get; set; } = [];

    /// <summary>
    /// Legacy: per-role row filters. Replaced by per-role
    /// <see cref="RowRestriction"/> on the role's <see cref="RoleDefinition"/>.
    /// </summary>
    [Obsolete("Replaced by RowRestriction on each user-type RoleDefinition. Scheduled for removal in Session 3 of rearchitect-role-administration-foundations.")]
    public List<RowFilter> RowFilters { get; set; } = [];

    /// <summary>
    /// Legacy: role-switching permission. Role-switching privileges now come from
    /// baked-in role compiled rules; organization-switching config moved to
    /// <see cref="OrganizationSwitching"/>.
    /// </summary>
    [Obsolete("Replaced by OrganizationSwitching (org switching) plus baked-in role privileges (role switching). Scheduled for removal in Session 3 of rearchitect-role-administration-foundations.")]
    public RoleSwitchingPermission RoleSwitching { get; set; } = new();
}
