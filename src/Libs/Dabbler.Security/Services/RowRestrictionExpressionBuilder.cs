using System.Linq.Expressions;
using System.Reflection;

namespace Dabbler.Security.Services;

/// <summary>
/// Parses a <see cref="Configuration.RowRestriction.FilterExpression"/> into a
/// LINQ <see cref="Expression{TDelegate}"/> of type <see cref="Func{T, TResult}"/>
/// suitable for <c>IQueryable.Where</c>.
/// </summary>
/// <remarks>
/// <para>
/// Grammar (ported verbatim from the legacy <c>RowFilterService</c> that this
/// helper replaces):
/// </para>
/// <list type="bullet">
///   <item><c>PropertyName == 'StringValue'</c> (single-quoted string literal)</item>
///   <item><c>PropertyName == 42</c> (integer literal)</item>
///   <item>
///     <c>PropertyName == @TokenName</c> where <c>TokenName</c> is matched
///     case-insensitively against the <c>tokens</c> dictionary supplied by
///     the caller (typically <c>@CurrentUserId</c> and
///     <c>@CurrentUserCustomerId</c>).
///   </item>
/// </list>
/// <para>
/// Returns <c>null</c> when the expression cannot be parsed, when the named
/// property does not exist on <typeparamref name="T"/>, or when a required
/// token has no value in <paramref name="tokens"/>. Callers decide whether a
/// <c>null</c> result means deny-all (fail-closed, matching the prior service)
/// or passthrough.
/// </para>
/// </remarks>
public static class RowRestrictionExpressionBuilder
{
    /// <summary>
    /// Attempts to build a LINQ filter expression for the given filter string
    /// and token dictionary.
    /// </summary>
    /// <typeparam name="T">The entity type to apply the filter to.</typeparam>
    /// <param name="filterExpression">The raw expression from a
    /// <c>RowRestriction.FilterExpression</c>.</param>
    /// <param name="tokens">
    /// Case-insensitive dictionary of token names (without the leading <c>@</c>)
    /// to runtime values. Supply <c>null</c> for a token that has no value for
    /// the current user — the builder returns <c>null</c> so the caller can
    /// fail closed.
    /// </param>
    /// <returns>
    /// The compiled-ready <see cref="Expression{TDelegate}"/>, or <c>null</c>
    /// when the input cannot be parsed.
    /// </returns>
    public static Expression<Func<T, bool>>? TryBuild<T>(
        string filterExpression,
        IReadOnlyDictionary<string, object?>? tokens)
    {
        if (string.IsNullOrWhiteSpace(filterExpression))
        {
            return null;
        }

        var parts = filterExpression.Split("==", 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            return null;
        }

        var propertyName = parts[0].Trim();
        var rawValue = parts[1].Trim();

        var type = typeof(T);
        var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (property is null)
        {
            return null;
        }

        var param = Expression.Parameter(type, "x");
        var propAccess = Expression.Property(param, property);

        Expression constant;
        try
        {
            if (rawValue.Length >= 2 && rawValue.StartsWith('\'') && rawValue.EndsWith('\''))
            {
                var stringValue = rawValue[1..^1];
                constant = Expression.Constant(stringValue, typeof(string));
            }
            else if (rawValue.StartsWith('@'))
            {
                var tokenName = rawValue[1..];
                if (tokens is null || !TryResolveToken(tokens, tokenName, out var tokenValue) || tokenValue is null)
                {
                    return null;
                }

                constant = Expression.Constant(tokenValue, tokenValue.GetType());
            }
            else if (int.TryParse(rawValue, out var intValue))
            {
                constant = Expression.Constant(intValue);
            }
            else
            {
                return null;
            }

            if (constant.Type != propAccess.Type)
            {
                constant = Expression.Convert(constant, propAccess.Type);
            }

            var body = Expression.Equal(propAccess, constant);
            return Expression.Lambda<Func<T, bool>>(body, param);
        }
        catch
        {
            return null;
        }
    }

    private static bool TryResolveToken(
        IReadOnlyDictionary<string, object?> tokens,
        string tokenName,
        out object? value)
    {
        foreach (var kvp in tokens)
        {
            if (string.Equals(kvp.Key, tokenName, StringComparison.OrdinalIgnoreCase))
            {
                value = kvp.Value;
                return true;
            }
        }

        value = null;
        return false;
    }
}
