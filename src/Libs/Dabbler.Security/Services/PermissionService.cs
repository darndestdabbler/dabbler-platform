using Dabbler.Security.Configuration;
using Dabbler.Security.Data;
using Microsoft.EntityFrameworkCore;

namespace Dabbler.Security.Services;

/// <summary>
/// Defines a service for checking user permissions against entities, fields, and pages.
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Checks if a user has a specific type of permission for a given entity.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="entityName">The name of the entity (e.g., "Customer", "Order").</param>
    /// <param name="permissionType">The type of permission to check (e.g., "Read", "Create", "Update", "Delete").</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains <c>true</c> if the user has the permission; otherwise, <c>false</c>.</returns>
    Task<bool> HasPermissionAsync(int userId, string entityName, string permissionType);

    /// <summary>
    /// Gets the access level for a specific field within an entity for a given user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="entityName">The name of the entity.</param>
    /// <param name="fieldName">The name of the field.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="FieldAccessLevel"/> for the specified field.</returns>
    Task<FieldAccessLevel> GetFieldAccessAsync(int userId, string entityName, string fieldName);

    /// <summary>
    /// Checks if a user is allowed to access a specific page route.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="pageRoute">The route of the page to check (e.g., "/admin/users").</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains <c>true</c> if the user can access the page; otherwise, <c>false</c>.</returns>
    Task<bool> CanAccessPageAsync(int userId, string pageRoute);

    /// <summary>
    /// Modifies a data transfer object (DTO) by nullifying properties the user does not have permission to see (i.e., fields marked as 'Hidden').
    /// </summary>
    /// <typeparam name="T">The type of the DTO.</typeparam>
    /// <param name="dto">The DTO instance to process.</param>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="entityName">The name of the entity corresponding to the DTO.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the modified DTO.</returns>
    Task<T> ApplyFieldPermissionsAsync<T>(T dto, int userId, string entityName) where T : class;

    /// <summary>
    /// Gets a list of all entity names for which the user has a specific type of permission.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="permissionType">The type of permission to check (e.g., "Read", "Create").</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a read-only list of accessible entity names, sorted alphabetically.</returns>
    Task<IReadOnlyList<string>> GetAccessibleEntitiesAsync(int userId, string permissionType);
}

/// <summary>
/// Implements <see cref="IPermissionService"/> to check user permissions using a database context.
/// </summary>
/// <param name="db">The database context for security-related data.</param>
public class PermissionService(DabblerSecurityDbContext db) : IPermissionService
{
    /// <inheritdoc/>
    public async Task<bool> HasPermissionAsync(int userId, string entityName, string permissionType)
    {
        return await db.AppUserRoles
            .Where(ur => ur.UserId == userId)
            .Join(
                db.AppRolePermissions.Where(rp => rp.EntityName == entityName && rp.PermissionType == permissionType),
                ur => ur.RoleId,
                rp => rp.RoleId,
                (_, _) => 1)
            .AnyAsync();
    }

    /// <inheritdoc/>
    public async Task<FieldAccessLevel> GetFieldAccessAsync(int userId, string entityName, string fieldName)
    {
        var level = await db.AppUserRoles
            .Where(ur => ur.UserId == userId)
            .Join(
                db.AppFieldPermissions.Where(fp => fp.EntityName == entityName && fp.FieldName == fieldName),
                ur => ur.RoleId,
                fp => fp.RoleId,
                (_, fp) => fp.AccessLevel)
            .ToListAsync();

        if (level.Contains("Editable") || level.Contains("ReadWrite")) return FieldAccessLevel.Editable;
        if (level.Contains("ReadOnly")) return FieldAccessLevel.ReadOnly;
        return FieldAccessLevel.Hidden;
    }

    /// <inheritdoc/>
    public async Task<bool> CanAccessPageAsync(int userId, string pageRoute)
    {
        return await db.AppUserRoles
            .Where(ur => ur.UserId == userId)
            .Join(
                db.AppPagePermissions.Where(pp => pp.PageRoute == pageRoute && pp.Allowed),
                ur => ur.RoleId,
                pp => pp.RoleId,
                (_, _) => 1)
            .AnyAsync();
    }

    /// <inheritdoc/>
    public async Task<T> ApplyFieldPermissionsAsync<T>(T dto, int userId, string entityName) where T : class
    {
        var roleIds = await db.AppUserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        var fieldPermissions = await db.AppFieldPermissions
            .Where(fp => fp.EntityName == entityName && roleIds.Contains(fp.RoleId))
            .ToListAsync();

        // Group by field name, take the most permissive access level across roles
        var effectiveLevels = fieldPermissions
            .GroupBy(fp => fp.FieldName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Any(fp => fp.AccessLevel is "Editable" or "ReadWrite") ? FieldAccessLevel.Editable
                   : g.Any(fp => fp.AccessLevel == "ReadOnly") ? FieldAccessLevel.ReadOnly
                   : FieldAccessLevel.Hidden,
                StringComparer.OrdinalIgnoreCase);

        foreach (var prop in typeof(T).GetProperties())
        {
            if (effectiveLevels.TryGetValue(prop.Name, out var level) && level == FieldAccessLevel.Hidden && prop.CanWrite)
            {
                prop.SetValue(dto, null);
            }
        }

        return dto;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> GetAccessibleEntitiesAsync(int userId, string permissionType)
    {
        return await db.AppUserRoles
            .Where(ur => ur.UserId == userId)
            .Join(
                db.AppRolePermissions.Where(rp => rp.PermissionType == permissionType),
                ur => ur.RoleId,
                rp => rp.RoleId,
                (_, rp) => rp.EntityName)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();
    }
}
