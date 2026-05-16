// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using AcmeCoffeeOnline.Shared.DTOs;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace AcmeCoffeeOnline.Api.Infrastructure;

/// <summary>
/// Extension methods for applying dynamic grid filtering and sorting to an
/// <see cref="IQueryable{T}"/> using LINQ expression trees that EF Core can translate to SQL.
/// </summary>
public static class QueryableExtensions
{
    private static readonly MethodInfo _stringContains =
        typeof(string).GetMethod("Contains", [typeof(string)])!;
    private static readonly MethodInfo _stringStartsWith =
        typeof(string).GetMethod("StartsWith", [typeof(string)])!;
    private static readonly MethodInfo _stringEndsWith =
        typeof(string).GetMethod("EndsWith", [typeof(string)])!;

    /// <summary>
    /// Applies a <see cref="GridCompositeFilter"/> to the query as a LINQ <c>Where</c> predicate.
    /// Returns the original query unchanged when <paramref name="filter"/> is null or empty.
    /// </summary>
    public static IQueryable<T> ApplyGridCompositeFilter<T>(
        this IQueryable<T> query,
        GridCompositeFilter? filter)
    {
        if (filter is null) return query;

        var param = Expression.Parameter(typeof(T), "x");
        var body = BuildCompositeExpression(param, filter);
        if (body is null) return query;

        var lambda = Expression.Lambda<Func<T, bool>>(body, param);
        return query.Where(lambda);
    }

    /// <summary>
    /// Applies a list of <see cref="GridQuerySort"/> descriptors using
    /// <c>OrderBy</c> for the first and <c>ThenBy</c> for subsequent sorts.
    /// Returns the original query when <paramref name="sorts"/> is empty.
    /// </summary>
    public static IQueryable<T> ApplyGridQuerySorts<T>(
        this IQueryable<T> query,
        IReadOnlyList<GridQuerySort> sorts)
    {
        if (sorts.Count == 0) return query;

        bool first = true;
        foreach (var sort in sorts)
        {
            var param = Expression.Parameter(typeof(T), "x");
            var prop = BuildPropertyAccess(param, sort.PropertyName);
            var keySelector = Expression.Lambda(prop, param);

            var method = first
                ? sort.Descending ? "OrderByDescending" : "OrderBy"
                : sort.Descending ? "ThenByDescending" : "ThenBy";
            first = false;

            var call = Expression.Call(
                typeof(Queryable),
                method,
                [typeof(T), prop.Type],
                query.Expression,
                Expression.Quote(keySelector));

            query = query.Provider.CreateQuery<T>(call);
        }

        return query;
    }

    // ── Private helpers ──────────────────────────────────────────────────

    private static Expression? BuildCompositeExpression(
        ParameterExpression param,
        GridCompositeFilter composite)
    {
        var exprs = composite.Filters
            .Select(f => BuildLeafExpression(param, f))
            .Concat(composite.Children.Select(c => BuildCompositeExpression(param, c)))
            .Where(e => e is not null)
            .ToList();

        if (exprs.Count == 0) return null;

        var combine = composite.Logic == GridFilterLogic.And
            ? (Func<Expression, Expression, Expression>)Expression.AndAlso
            : Expression.OrElse;

        return exprs.Cast<Expression>().Aggregate(combine);
    }

    private static Expression? BuildLeafExpression(
        ParameterExpression param,
        GridQueryFilter filter)
    {
        try
        {
            var member = BuildPropertyAccess(param, filter.PropertyName);
            var memberType = member.Type;
            var underlyingType = Nullable.GetUnderlyingType(memberType) ?? memberType;
            // Materialise the filter value from JsonElement if needed (POST [FromBody] deserialization)
            var rawValue = ExtractFilterValue(filter.Value, underlyingType);
            // nullConst is only valid for reference types and nullable value types
            var canBeNull = !memberType.IsValueType || Nullable.GetUnderlyingType(memberType) is not null;
            var nullConst = canBeNull ? Expression.Constant(null, memberType) : null;

            switch (filter.Operator)
            {
                case "Equals":
                    if (rawValue is null)
                        return nullConst is null ? null : Expression.Equal(member, nullConst);
                    var eqConst = BuildTypedConstant(rawValue, memberType, underlyingType);
                    return Expression.Equal(member, eqConst);

                case "GreaterThan":
                case "LessThan":
                case "GreaterThanOrEqual":
                case "LessThanOrEqual":
                    if (rawValue is null) return null;
                    var cmpConst = BuildTypedConstant(rawValue, memberType, underlyingType);
                    return filter.Operator switch
                    {
                        "GreaterThan" => Expression.GreaterThan(member, cmpConst),
                        "LessThan" => Expression.LessThan(member, cmpConst),
                        "GreaterThanOrEqual" => Expression.GreaterThanOrEqual(member, cmpConst),
                        _ => Expression.LessThanOrEqual(member, cmpConst),
                    };

                case "Contains":
                case "StartsWith":
                case "EndsWith":
                    if (memberType != typeof(string) || rawValue is not string strVal)
                        return null;
                    var strConst = Expression.Constant(strVal);
                    var strMethod = filter.Operator switch
                    {
                        "Contains" => _stringContains,
                        "StartsWith" => _stringStartsWith,
                        _ => _stringEndsWith,
                    };
                    // string is a reference type so nullConst is always non-null here
                    var notNull = Expression.NotEqual(member, nullConst!);
                    var strCall = Expression.Call(member, strMethod, strConst);
                    return Expression.AndAlso(notNull, strCall);

                case "IsEmpty":
                    if (nullConst is null) return null; // non-nullable value types cannot be empty
                    if (memberType == typeof(string))
                        return Expression.OrElse(
                            Expression.Equal(member, nullConst),
                            Expression.Equal(member, Expression.Constant(string.Empty)));
                    return Expression.Equal(member, nullConst);

                case "IsNotEmpty":
                    if (nullConst is null) return null; // non-nullable value types are always non-empty
                    if (memberType == typeof(string))
                        return Expression.AndAlso(
                            Expression.NotEqual(member, nullConst),
                            Expression.NotEqual(member, Expression.Constant(string.Empty)));
                    return Expression.NotEqual(member, nullConst);

                default:
                    return null; // "In" and unknown operators — deferred / skip
            }
        }
        catch
        {
            // Invalid property name or type conversion — skip filter silently
            return null;
        }
    }

    private static MemberExpression BuildPropertyAccess(Expression root, string propertyPath) =>
        (MemberExpression)propertyPath
            .Split('.')
            .Aggregate(root, Expression.Property);

    /// <summary>
    /// Normalises a filter value to a CLR object suitable for expression tree building.
    /// When the value arrives from a POST [FromBody] JSON payload, System.Text.Json
    /// deserialises <c>object?</c> properties as <see cref="JsonElement"/>; this method
    /// unwraps the element to a typed CLR value so the caller can use it directly.
    /// </summary>
    private static object? ExtractFilterValue(object? raw, Type targetType)
    {
        if (raw is not JsonElement je) return raw;

        return je.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.String => je.GetString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number =>
                targetType == typeof(int) || targetType == typeof(int?) ? je.GetInt32() :
                targetType == typeof(long) || targetType == typeof(long?) ? je.GetInt64() :
                targetType == typeof(double) || targetType == typeof(double?) ? je.GetDouble() :
                targetType == typeof(float) || targetType == typeof(float?) ? (object)je.GetSingle() :
                (object)je.GetDecimal(),  // default numeric → decimal covers most DTO properties
            _ => null,
        };
    }

    /// <summary>
    /// Builds a typed constant expression compatible with the member expression type.
    /// For nullable value types, lifts the non-nullable constant to the nullable type via
    /// <see cref="Expression.Convert"/> so that comparison operators work correctly.
    /// </summary>
    private static Expression BuildTypedConstant(object? value, Type memberType, Type underlyingType)
    {
        var converted = Convert.ChangeType(value, underlyingType);
        var constant = Expression.Constant(converted, underlyingType);
        // Lift to nullable when the member is e.g. decimal? or int?
        return memberType != underlyingType
            ? Expression.Convert(constant, memberType)
            : constant;
    }
}
