namespace Dabbler.Security.Registration;

/// <summary>
/// Fluent builder used inside <c>DabblerSecurityOptions.RegisterEntities()</c>
/// to declare the entities the host application wants to protect.
/// </summary>
public sealed class EntityRegistrar
{
    private readonly List<RegisteredEntity> _entities = [];

    /// <summary>
    /// Registers an entity whose fields can be protected.
    /// </summary>
    /// <param name="key">A unique key for the entity.</param>
    /// <param name="displayName">The name of the entity to display in the admin UI.</param>
    /// <param name="configureFields">An optional action to configure the fields for this entity.</param>
    /// <returns>The same <see cref="EntityRegistrar"/> instance for chaining.</returns>
    public EntityRegistrar Add(string key, string displayName, Action<FieldRegistrar>? configureFields = null)
    {
        var fieldRegistrar = new FieldRegistrar();
        configureFields?.Invoke(fieldRegistrar);
        _entities.Add(new RegisteredEntity
        {
            Key = key,
            DisplayName = displayName,
            Fields = fieldRegistrar.Build()
        });
        return this;
    }

    internal IReadOnlyList<RegisteredEntity> Build() => _entities.AsReadOnly();
}

/// <summary>
/// Fluent builder for declaring the fields of a registered entity.
/// </summary>
public sealed class FieldRegistrar
{
    private readonly List<RegisteredField> _fields = [];

    /// <summary>
    /// Registers a field for the parent entity.
    /// </summary>
    /// <param name="key">A unique key for the field within the entity.</param>
    /// <param name="displayName">The name of the field to display in the admin UI.</param>
    /// <returns>The same <see cref="FieldRegistrar"/> instance for chaining.</returns>
    public FieldRegistrar Add(string key, string displayName)
    {
        _fields.Add(new RegisteredField { Key = key, DisplayName = displayName });
        return this;
    }

    internal IReadOnlyList<RegisteredField> Build() => _fields.AsReadOnly();
}
