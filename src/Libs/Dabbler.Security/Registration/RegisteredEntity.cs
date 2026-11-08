namespace Dabbler.Security.Registration;

/// <summary>
/// Describes an entity that has been registered with the resource registry.
/// Each entity may declare the fields that can have per-role visibility configured.
/// </summary>
public sealed class RegisteredEntity
{
    /// <summary>
    /// Gets the unique key for the entity.
    /// </summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>
    /// Gets the name of the entity for display in the admin UI.
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the list of fields registered for this entity.
    /// </summary>
    public IReadOnlyList<RegisteredField> Fields { get; init; } = [];
}

/// <summary>
/// A single field on a registered entity.
/// </summary>
public sealed class RegisteredField
{
    /// <summary>
    /// Gets the unique key for the field within its parent entity.
    /// </summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>
    /// Gets the name of the field for display in the admin UI.
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;
}
