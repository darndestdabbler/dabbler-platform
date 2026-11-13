namespace Dabbler.Security.Authorization;

/// <summary>
/// Platform-level description of a baked-in role: canonical immutable name,
/// default display alias (overridable per deployment via
/// <c>SecurityConfiguration.BakedInRoleAliasOverrides</c>), human-readable
/// description, and the compiled privilege profile that handlers consult.
///
/// Baked-in roles are supplied by <see cref="IBakedInRoleProvider"/>. The
/// platform ships the five canonical roles; applications may register
/// additional <b>non-user-type</b> baked-in roles through the same contract.
/// </summary>
/// <param name="Name">
/// The canonical immutable role name (e.g., <c>SystemAdmin</c>). This is the
/// identifier used in claims, audit records, and handler logic.
/// </param>
/// <param name="DefaultDisplayAlias">
/// The platform-default alias shown in UI surfaces when no per-deployment
/// alias override is configured.
/// </param>
/// <param name="Description">
/// Short human-readable description of the role's purpose, surfaced by the
/// Role Documentation tab in the security dashboard.
/// </param>
/// <param name="Privileges">
/// The compiled privilege profile consulted by authorization handlers before
/// any JSON-configured restriction lookup.
/// </param>
public sealed record BakedInRoleDefinition(
    string Name,
    string DefaultDisplayAlias,
    string Description,
    BakedInRolePrivileges Privileges);
