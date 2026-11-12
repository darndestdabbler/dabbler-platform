using System.Linq.Expressions;
using System.Reflection;
using Dabbler.Security.Configuration;
using Microsoft.Extensions.Logging;

namespace Dabbler.Security.Services;

/// <summary>
/// Resolves row-level filter expressions from <see cref="SecurityConfigurationManager"/>.
/// <para>
/// Supports simple equality expressions of the form:
/// <list type="bullet">
///   <item><c>PropertyName == 'StringValue'</c> (string literal, single-quoted)</item>
///   <item><c>PropertyName == 42</c> (integer literal)</item>
///   <item><c>PropertyName == @CurrentUserId</c> (token resolved to the current user's ID)</item>
/// </list>
/// </para>
/// <para>
/// Returns <c>x => true</c> when no filter is configured for the given entity and role.
/// Returns <c>x => false</c> (deny-all) when a filter is configured but cannot be parsed.
/// </para>
/// </summary>
public class RowFilterService : IRowFilterService
{
    private readonly SecurityConfigurationManager _manager;
    private readonly ILogger<RowFilterService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RowFilterService"/> class.
    /// </summary>
    /// <param name="manager">The security configuration manager.</param>
    /// <param name="logger">The logger for this service.</param>
    public RowFilterService(SecurityConfigurationManager manager, ILogger<RowFilterService> logger)
    {
        _manager = manager;
        _logger = logger;
    }

    /// <summary>
    /// Gets a LINQ filter expression for a given entity, role, and current user.
    /// </summary>
    /// <typeparam name="T">The entity type to apply the filter to.</typeparam>
    /// <param name="entityKey">The key identifying the entity in the security configuration.</param>
    /// <param name="roleName">The name of the role for which to retrieve the filter.</param>
    /// <param name="currentUserId">The ID of the current user, used to resolve the <c>@CurrentUserId</c> token.</param>
    /// <returns>
    /// An <see cref="Expression{TDelegate}"/> that can be used in a LINQ Where clause.
    /// Returns a passthrough filter (<c>x => true</c>) if no specific filter is found.
    /// Returns a deny-all filter (<c>x => false</c>) if a configured filter fails to parse.
    /// </returns>
    public Expression<Func<T, bool>> GetFilter<T>(string entityKey, string roleName, int? currentUserId = null)
    {
        var config = _manager.Current;

        var rowFilter = config.RowFilters.FirstOrDefault(f =>
            string.Equals(f.EntityKey, entityKey, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(f.RoleName, roleName, StringComparison.OrdinalIgnoreCase));

        if (rowFilter == null || string.IsNullOrWhiteSpace(rowFilter.FilterExpression))
            return Passthrough<T>();

        var result = TryBuildExpression<T>(rowFilter.FilterExpression, currentUserId);
        if (result == null)
        {
            _logger.LogWarning(
                "Failed to parse row filter expression '{Expression}' for entity {Entity}, role {Role}. Denying access.",
                rowFilter.FilterExpression, entityKey, roleName);
            return DenyAll<T>();
        }

        return result;
    }

    private static Expression<Func<T, bool>> Passthrough<T>() => _ => true;

    private static Expression<Func<T, bool>> DenyAll<T>() => _ => false;

    private static Expression<Func<T, bool>>? TryBuildExpression<T>(string filterExpression, int? currentUserId)
    {
        // Supports: "PropertyName == 'value'", "PropertyName == 42", or "PropertyName == @CurrentUserId"
        var parts = filterExpression.Split("==", 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
            return null;

        var propertyName = parts[0].Trim();
        var rawValue = parts[1].Trim();

        var type = typeof(T);
        var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (property == null)
            return null;

        var param = Expression.Parameter(type, "x");
        var propAccess = Expression.Property(param, property);

        Expression constant;
        try
        {
            if (rawValue.StartsWith('\'') && rawValue.EndsWith('\''))
            {
                var stringValue = rawValue[1..^1];
                constant = Expression.Constant(stringValue, typeof(string));
            }
            else if (rawValue.Equals("@CurrentUserId", StringComparison.OrdinalIgnoreCase))
            {
                if (currentUserId == null)
                    return null;
                constant = Expression.Constant(currentUserId.Value);
            }
            else if (int.TryParse(rawValue, out var intValue))
            {
                constant = Expression.Constant(intValue);
            }
            else
            {
                return null;
            }

            // Convert constant type if needed (e.g. int constant on a long property)
            if (constant.Type != propAccess.Type)
                constant = Expression.Convert(constant, propAccess.Type);

            var body = Expression.Equal(propAccess, constant);
            return Expression.Lambda<Func<T, bool>>(body, param);
        }
        catch
        {
            return null;
        }
    }
}
