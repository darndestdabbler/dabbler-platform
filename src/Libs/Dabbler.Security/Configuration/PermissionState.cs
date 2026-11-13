namespace Dabbler.Security.Configuration;

/// <summary>
/// Tri-state decision a role holds for a given resource. Mutual exclusion is
/// enforced at the data-model level: a role can only be in one state for any
/// one resource, preventing the "both Allow and Deny checked" defect the UAT
/// flagged on the checkbox-based matrix.
/// </summary>
public enum PermissionState
{
    /// <summary>Role has no opinion; falls through to other roles or defaults.</summary>
    Unset = 0,

    /// <summary>Role explicitly grants access.</summary>
    Allow = 1,

    /// <summary>Role explicitly denies access. Deny takes precedence over Allow when a user holds both an allowed and a denied role.</summary>
    Deny = 2,
}
