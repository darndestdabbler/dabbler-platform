using Dabbler.Components.Data.Metadata;

namespace AcmeCoffeeOnline.Web.Data;

/// <summary>
/// Wraps an <see cref="IEntityMetadataProvider"/> and filters navigable entities
/// based on the current user's roles. Customers see only Products and Orders;
/// BackofficeUser+ sees all entities.
/// </summary>
public class RoleFilteredMetadataProvider : IEntityMetadataProvider
{
    private readonly IEntityMetadataProvider _inner;
    private readonly HashSet<string> _roles;

    private static readonly HashSet<string> BackofficeRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "BackofficeUser", "BackofficeUserAdmin", "SystemAdmin"
    };

    private static readonly HashSet<string> CustomerVisibleEntities = new(StringComparer.OrdinalIgnoreCase)
    {
        "Product", "Order"
    };

    public RoleFilteredMetadataProvider(IEntityMetadataProvider inner, IEnumerable<string> userRoles)
    {
        _inner = inner;
        _roles = new HashSet<string>(userRoles, StringComparer.OrdinalIgnoreCase);
    }

    public EntityMetadata? GetEntityMetadata(string entityName) => _inner.GetEntityMetadata(entityName);

    public IReadOnlyList<EntityMetadata> GetAllEntities() => _inner.GetAllEntities();

    public IReadOnlyList<EntityMetadata> GetNavigableEntities()
    {
        var all = _inner.GetNavigableEntities();

        if (_roles.Overlaps(BackofficeRoles))
            return all;

        return all.Where(e => CustomerVisibleEntities.Contains(e.EntityName)).ToList();
    }
}
