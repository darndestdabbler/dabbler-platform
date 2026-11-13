using System.Linq.Expressions;

namespace Dabbler.Security.Services;

/// <summary>
/// Returns a compiled LINQ predicate that enforces the row-level filter configured for a
/// given entity and role.
/// </summary>
/// <remarks>
/// Returns a pass-through predicate (<c>x => true</c>) when no filter is configured.
/// </remarks>
public interface IRowFilterService
{
    /// <summary>
    /// Gets a filter expression for a given entity type and role.
    /// </summary>
    /// <typeparam name="T">The entity type to be filtered.</typeparam>
    /// <param name="entityKey">The key identifying the entity configuration.</param>
    /// <param name="roleName">The role name to look up the filter for.</param>
    /// <param name="currentUserId">
    /// The ID of the currently authenticated user. This is required if the configured filter expression
    /// contains the <c>@CurrentUserId</c> token.
    /// </param>
    /// <returns>
    /// An <see cref="Expression{TDelegate}"/> of type <see cref="Func{T, TResult}"/> that, when applied to an
    /// <see cref="IQueryable{T}"/>, restricts rows to those matching the configured filter.
    /// Returns <c>x => true</c> when no filter is configured or if a required
    /// <paramref name="currentUserId"/> is not provided.
    /// </returns>
    Expression<Func<T, bool>> GetFilter<T>(string entityKey, string roleName, int? currentUserId = null);
}
