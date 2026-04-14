using Microsoft.AspNetCore.Authorization;

namespace Dabbler.Security.Authorization.Requirements;

/// <summary>
/// Represents the requirement that a user must have permission to perform a specific operation on an entity.
/// </summary>
public sealed class EntityPermissionRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Gets the unique key identifying the entity.
    /// </summary>
    public string EntityKey { get; }

    /// <summary>
    /// Gets the operation being attempted.
    /// </summary>
    /// <value>A string representing the operation, such as "Create", "Read", "Update", or "Delete".</value>
    public string Operation { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityPermissionRequirement"/> class.
    /// </summary>
    /// <param name="entityKey">The unique key of the entity.</param>
    /// <param name="operation">The operation being performed.</param>
    public EntityPermissionRequirement(string entityKey, string operation)
    {
        EntityKey = entityKey;
        Operation = operation;
    }
}
