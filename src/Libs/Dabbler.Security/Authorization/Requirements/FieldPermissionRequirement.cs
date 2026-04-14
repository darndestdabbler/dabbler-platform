using Microsoft.AspNetCore.Authorization;

namespace Dabbler.Security.Authorization.Requirements;

/// <summary>
/// Represents the requirement that a user must have permission to access a specific field within an entity.
/// </summary>
public sealed class FieldPermissionRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Gets the unique key identifying the entity to which the field belongs.
    /// </summary>
    public string EntityKey { get; }

    /// <summary>
    /// Gets the unique key identifying the field within the entity.
    /// </summary>
    public string FieldKey { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FieldPermissionRequirement"/> class.
    /// </summary>
    /// <param name="entityKey">The unique key of the entity containing the field.</param>
    /// <param name="fieldKey">The unique key of the field.</param>
    public FieldPermissionRequirement(string entityKey, string fieldKey)
    {
        EntityKey = entityKey;
        FieldKey = fieldKey;
    }
}
