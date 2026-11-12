// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace Dabbler.Api.Querying;

/// <summary>
/// Extension methods for applying dynamic grid filtering and sorting to an
/// <see cref="IQueryable{T}"/> using LINQ expression trees that EF Core can translate to SQL.
/// </summary>
/// <remarks>
/// A condition that cannot be applied as asked throws <see cref="GridQueryException"/>; none is
/// left out of the query.
/// </remarks>
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
    /// <exception cref="GridQueryException">
    /// A filter names an unknown property or operator, an operator the property's type does not
    /// support, or a value that cannot be converted to the property's type.
    /// </exception>
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
    /// <exception cref="GridQueryException">A sort names an unknown property.</exception>
    public static IQueryable<T> ApplyGridQuerySorts<T>(
        this IQueryable<T> query,
        IReadOnlyList<GridQuerySort> sorts)
    {
        if (sorts.Count == 0) return query;

        bool first = true;
        foreach (var sort in sorts)
        {
            var param = Expression.Parameter(typeof(T), "x");
            var prop = BuildPropertyAccess(param, sort.PropertyName, @operator: null);
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
            .Concat(composite.Children
                .Select(c => BuildCompositeExpression(param, c))
                .OfType<Expression>())
            .ToList();

        if (exprs.Count == 0) return null;

        var combine = composite.Logic == GridFilterLogic.And
            ? (Func<Expression, Expression, Expression>)Expression.AndAlso
            : Expression.OrElse;

        return exprs.Aggregate(combine);
    }

    private static Expression BuildLeafExpression(
        ParameterExpression param,
        GridQueryFilter filter)
    {
        var member = BuildPropertyAccess(param, filter.PropertyName, filter.Operator);
        var memberType = member.Type;
        var underlyingType = Nullable.GetUnderlyingType(memberType) ?? memberType;
        var canBeNull = !memberType.IsValueType || memberType != underlyingType;
        // Only read where the property can hold null: string operators and IsEmpty/IsNotEmpty.
        var nullConst = canBeNull ? Expression.Constant(null, memberType) : null;

        switch (filter.Operator)
        {
            case "Equals":
                return Expression.Equal(member, TypedConstant(filter, filter.Value, memberType, underlyingType));

            case "GreaterThan":
            case "LessThan":
            case "GreaterThanOrEqual":
            case "LessThanOrEqual":
                var cmpConst = TypedConstant(filter, RequireValue(filter), memberType, underlyingType);
                var cmpType = filter.Operator switch
                {
                    "GreaterThan" => ExpressionType.GreaterThan,
                    "LessThan" => ExpressionType.LessThan,
                    "GreaterThanOrEqual" => ExpressionType.GreaterThanOrEqual,
                    _ => ExpressionType.LessThanOrEqual,
                };
                try
                {
                    return Expression.MakeBinary(cmpType, member, cmpConst);
                }
                catch (InvalidOperationException ex)
                {
                    throw Unsupported(filter, memberType, ex);
                }

            case "Contains":
            case "StartsWith":
            case "EndsWith":
                if (memberType != typeof(string)) throw Unsupported(filter, memberType);
                var strConst = TypedConstant(filter, RequireValue(filter), memberType, underlyingType);
                var strMethod = filter.Operator switch
                {
                    "Contains" => _stringContains,
                    "StartsWith" => _stringStartsWith,
                    _ => _stringEndsWith,
                };
                return Expression.AndAlso(
                    Expression.NotEqual(member, nullConst!),
                    Expression.Call(member, strMethod, strConst));

            case "IsEmpty":
                if (!canBeNull) throw Unsupported(filter, memberType);
                if (memberType == typeof(string))
                    return Expression.OrElse(
                        Expression.Equal(member, nullConst!),
                        Expression.Equal(member, Expression.Constant(string.Empty)));
                return Expression.Equal(member, nullConst!);

            case "IsNotEmpty":
                if (!canBeNull) throw Unsupported(filter, memberType);
                if (memberType == typeof(string))
                    return Expression.AndAlso(
                        Expression.NotEqual(member, nullConst!),
                        Expression.NotEqual(member, Expression.Constant(string.Empty)));
                return Expression.NotEqual(member, nullConst!);

            case "In":
                var items = filter.Value switch
                {
                    JsonElement { ValueKind: JsonValueKind.Array } je => je.EnumerateArray().Cast<object?>().ToList(),
                    Array array => array.Cast<object?>().ToList(),
                    _ => throw new GridQueryException(filter.PropertyName, filter.Operator, "the value must be an array"),
                };
                var typed = Array.CreateInstance(memberType, items.Count);
                for (var i = 0; i < items.Count; i++)
                    typed.SetValue(ConvertOrNull(filter, items[i], memberType, underlyingType), i);
                return Expression.Call(
                    typeof(Enumerable),
                    nameof(Enumerable.Contains),
                    [memberType],
                    Expression.Constant(typed),
                    member);

            default:
                throw new GridQueryException(filter.PropertyName, filter.Operator, "the operator is not known");
        }
    }

    private static MemberExpression BuildPropertyAccess(Expression root, string propertyPath, string? @operator)
    {
        try
        {
            return (MemberExpression)propertyPath
                .Split('.')
                .Aggregate(root, Expression.Property);
        }
        catch (ArgumentException ex)
        {
            throw new GridQueryException(
                propertyPath, @operator, $"{root.Type.Name} has no such property", ex);
        }
    }

    private static object RequireValue(GridQueryFilter filter) =>
        filter.Value is null or JsonElement { ValueKind: JsonValueKind.Null }
            ? throw new GridQueryException(filter.PropertyName, filter.Operator, "the operator needs a value")
            : filter.Value;

    private static GridQueryException Unsupported(GridQueryFilter filter, Type memberType, Exception? inner = null) =>
        new(filter.PropertyName, filter.Operator, $"the operator does not apply to a {memberType.Name} property", inner);

    private static ConstantExpression TypedConstant(
        GridQueryFilter filter, object? value, Type memberType, Type underlyingType) =>
        Expression.Constant(ConvertOrNull(filter, value, memberType, underlyingType), memberType);

    /// <summary>
    /// Converts a filter value to the property's type. <c>null</c> (or a JSON null) is kept
    /// only for a property that can hold it.
    /// </summary>
    private static object? ConvertOrNull(
        GridQueryFilter filter, object? value, Type memberType, Type underlyingType)
    {
        if (value is not null and not JsonElement { ValueKind: JsonValueKind.Null })
            return ConvertValue(filter, value, underlyingType);

        if (memberType.IsValueType && memberType == underlyingType)
            throw new GridQueryException(
                filter.PropertyName, filter.Operator, $"a null value does not apply to a {memberType.Name} property");
        return null;
    }

    /// <summary>
    /// Converts a value to <paramref name="type"/> with the invariant culture. A
    /// <see cref="JsonElement"/> (what System.Text.Json gives an <c>object?</c> property of a
    /// POST body) and a string are parsed; ISO 8601 is the date and time form.
    /// </summary>
    private static object ConvertValue(GridQueryFilter filter, object value, Type type)
    {
        if (type.IsInstanceOfType(value)) return value;

        try
        {
            var converted = value switch
            {
                JsonElement je => ConvertText(JsonText(je), type),
                string text => ConvertText(text, type),
                DateTime dt when type == typeof(DateOnly) => DateOnly.FromDateTime(dt),
                DateTime dt when type == typeof(DateTimeOffset) => new DateTimeOffset(dt),
                _ when type.IsEnum => Enum.ToObject(type, value),
                _ => Convert.ChangeType(value, type, CultureInfo.InvariantCulture),
            };
            if (type.IsEnum && !Enum.IsDefined(type, converted))
                throw new FormatException($"{value} is not a member of {type.Name}");
            return converted;
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException or ArgumentException)
        {
            throw new GridQueryException(
                filter.PropertyName, filter.Operator, $"the value '{value}' cannot be converted to {type.Name}", ex);
        }
    }

    private static string JsonText(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString()!,
        JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => element.GetRawText(),
        _ => throw new FormatException($"a JSON {element.ValueKind} is not a single value"),
    };

    private static object ConvertText(string text, Type type)
    {
        var culture = CultureInfo.InvariantCulture;

        if (type == typeof(string)) return text;
        if (type.IsEnum) return Enum.Parse(type, text, ignoreCase: true);
        if (type == typeof(Guid)) return Guid.Parse(text);
        if (type == typeof(DateOnly))
            return DateOnly.TryParse(text, culture, DateTimeStyles.None, out var date)
                ? date
                : DateOnly.FromDateTime(DateTime.Parse(text, culture, DateTimeStyles.RoundtripKind));
        if (type == typeof(TimeOnly)) return TimeOnly.Parse(text, culture);
        if (type == typeof(DateTime)) return DateTime.Parse(text, culture, DateTimeStyles.RoundtripKind);
        if (type == typeof(DateTimeOffset)) return DateTimeOffset.Parse(text, culture, DateTimeStyles.AssumeUniversal);
        return Convert.ChangeType(text, type, culture);
    }
}
