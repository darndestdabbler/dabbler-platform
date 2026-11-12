// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Dabbler.Components.Utilities;

/// <summary>
/// Provides cached, expression-compiled property accessors for fast reflection-based property reads.
/// </summary>
/// <remarks>
/// <para>
/// Supports both single-level property names (e.g., <c>"Name"</c>) and dot-notation paths
/// (e.g., <c>"Address.City"</c>) for nested property access.
/// </para>
/// <para>
/// Compiled delegates are cached per (Type, PropertyPath) pair for optimal performance
/// on repeated reads.
/// </para>
/// </remarks>
public static class PropertyAccessor
{
    private static readonly ConcurrentDictionary<(Type Type, string Path), Func<object, object?>> _getterCache = new();
    private static readonly ConcurrentDictionary<(Type Type, string Path), Action<object, object?>> _setterCache = new();

    /// <summary>
    /// Gets the value of the specified property or property path on the given item.
    /// </summary>
    /// <param name="item">The object to read the property from.</param>
    /// <param name="propertyPath">
    /// The property name or dot-notation path to read (e.g., <c>"Name"</c> or <c>"Address.City"</c>).
    /// </param>
    /// <returns>
    /// The property value, or <c>null</c> if any segment in the path evaluates to <c>null</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="item"/> or <paramref name="propertyPath"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when a property segment does not exist on the resolved type.
    /// </exception>
    public static object? GetValue(object item, string propertyPath)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(propertyPath);

        var key = (item.GetType(), propertyPath);
        var getter = _getterCache.GetOrAdd(key, static k => CompileGetter(k.Type, k.Path));
        return getter(item);
    }

    /// <summary>
    /// Sets the value of the specified property on the given item.
    /// </summary>
    /// <param name="item">The object to set the property on.</param>
    /// <param name="propertyName">
    /// The property name to set. Dot-notation paths are not supported for writes.
    /// </param>
    /// <param name="value">The value to set.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="item"/> or <paramref name="propertyName"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the property does not exist or is not writable.
    /// </exception>
    public static void SetValue(object item, string propertyName, object? value)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(propertyName);

        var key = (item.GetType(), propertyName);
        var setter = _setterCache.GetOrAdd(key, static k => CompileSetter(k.Type, k.Path));
        setter(item, value);
    }

    /// <summary>
    /// Gets the <see cref="PropertyInfo"/> for a single-level property name on the given type.
    /// Returns <c>null</c> if not found or not writable.
    /// </summary>
    public static PropertyInfo? GetWritableProperty(Type type, string propertyName)
    {
        var prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        return prop is { CanWrite: true } ? prop : null;
    }

    private static Func<object, object?> CompileGetter(Type type, string propertyPath)
    {
        var segments = propertyPath.Split('.');

        // Build: (object obj) => (object?)((TType)obj).Prop1?.Prop2?.Prop3
        var parameter = Expression.Parameter(typeof(object), "obj");
        Expression current = Expression.Convert(parameter, type);
        var currentType = type;

        foreach (var segment in segments)
        {
            var property = currentType.GetProperty(segment, BindingFlags.Public | BindingFlags.Instance)
                ?? throw new ArgumentException(
                    $"Property '{segment}' not found on type '{currentType.FullName}' (full path: '{propertyPath}').",
                    nameof(propertyPath));

            current = Expression.Property(current, property);
            currentType = property.PropertyType;
        }

        // For nested paths with reference types, insert null checks.
        // For single-level paths, just box and return.
        if (segments.Length > 1)
        {
            current = BuildNullSafeChain(parameter, type, segments);
        }

        var boxed = Expression.Convert(current, typeof(object));
        return Expression.Lambda<Func<object, object?>>(boxed, parameter).Compile();
    }

    /// <summary>
    /// Builds a null-safe property chain: returns null if any intermediate segment is null.
    /// </summary>
    private static Expression BuildNullSafeChain(ParameterExpression parameter, Type rootType, string[] segments)
    {
        // Start with the cast to the root type
        Expression current = Expression.Convert(parameter, rootType);
        var currentType = rootType;

        for (var i = 0; i < segments.Length; i++)
        {
            var property = currentType.GetProperty(segments[i], BindingFlags.Public | BindingFlags.Instance)!;
            current = Expression.Property(current, property);
            currentType = property.PropertyType;

            // Insert null check for intermediate reference-type segments (not the final one)
            if (i < segments.Length - 1 && !currentType.IsValueType)
            {
                // If current is null, short-circuit to null
                var nullConst = Expression.Constant(null, typeof(object));
                var isNull = Expression.Equal(current, Expression.Constant(null, currentType));

                // Build the rest of the chain recursively
                var remainingSegments = segments[(i + 1)..];
                var rest = BuildContinuation(current, currentType, remainingSegments);
                var boxedRest = Expression.Convert(rest, typeof(object));

                return Expression.Condition(isNull, nullConst, boxedRest);
            }
        }

        return current;
    }

    private static Expression BuildContinuation(Expression source, Type sourceType, string[] segments)
    {
        Expression current = source;
        var currentType = sourceType;

        for (var i = 0; i < segments.Length; i++)
        {
            var property = currentType.GetProperty(segments[i], BindingFlags.Public | BindingFlags.Instance)!;
            current = Expression.Property(current, property);
            currentType = property.PropertyType;

            if (i < segments.Length - 1 && !currentType.IsValueType)
            {
                var nullConst = Expression.Constant(null, typeof(object));
                var isNull = Expression.Equal(current, Expression.Constant(null, currentType));
                var rest = BuildContinuation(current, currentType, segments[(i + 1)..]);
                var boxedRest = Expression.Convert(rest, typeof(object));
                return Expression.Condition(isNull, nullConst, boxedRest);
            }
        }

        return current;
    }

    private static Action<object, object?> CompileSetter(Type type, string propertyName)
    {
        var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new ArgumentException(
                $"Property '{propertyName}' not found on type '{type.FullName}'.",
                nameof(propertyName));

        if (!property.CanWrite)
            throw new ArgumentException(
                $"Property '{propertyName}' on type '{type.FullName}' is not writable.",
                nameof(propertyName));

        // Build: (object obj, object? val) => ((TType)obj).Prop = (TProp)val
        var objParam = Expression.Parameter(typeof(object), "obj");
        var valParam = Expression.Parameter(typeof(object), "val");

        var castObj = Expression.Convert(objParam, type);
        var castVal = Expression.Convert(valParam, property.PropertyType);
        var assign = Expression.Assign(Expression.Property(castObj, property), castVal);

        return Expression.Lambda<Action<object, object?>>(assign, objParam, valParam).Compile();
    }
}
