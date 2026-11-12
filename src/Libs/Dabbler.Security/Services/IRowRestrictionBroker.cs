using System.Linq.Expressions;

namespace Dabbler.Security.Services;

/// <summary>
/// Resolves the effective row-level restriction that should be applied when
/// the current user queries the named entity. Data providers call this during
/// query construction (typically inside <c>IQueryable.Where</c>) to narrow
/// results to what the active role is permitted to see.
/// </summary>
/// <remarks>
/// <para>
/// Replaces the legacy <c>IRowFilterService</c>. The broker is aware of the
/// current user's claims and active role (via <c>IHttpContextAccessor</c> and
/// <c>IRoleSwitcher</c>), so callers no longer thread role names and user
/// identifiers through every query call.
/// </para>
/// <para>
/// Baked-in roles have compiled-in row-scoping semantics and are governed by
/// their privilege profiles, not by configured restrictions. When the active
/// user has any baked-in role, the broker returns <c>null</c> — baked-in
/// privileges are enforced elsewhere.
/// </para>
/// </remarks>
public interface IRowRestrictionBroker
{
    /// <summary>
    /// Returns the effective row filter for <typeparamref name="T"/>, or
    /// <c>null</c> when no restriction applies.
    /// </summary>
    /// <typeparam name="T">The entity type to be filtered.</typeparam>
    /// <param name="entityKey">
    /// The logical entity key as declared on
    /// <see cref="Configuration.RowRestriction.EntityKey"/>. Matched
    /// case-insensitively.
    /// </param>
    /// <returns>
    /// An <see cref="Expression{TDelegate}"/> of type
    /// <see cref="Func{T, TResult}"/> suitable for
    /// <c>IQueryable.Where</c>, or <c>null</c> when the current user's active
    /// role set carries no matching restriction for <paramref name="entityKey"/>.
    /// </returns>
    Expression<Func<T, bool>>? GetRestriction<T>(string entityKey);
}
