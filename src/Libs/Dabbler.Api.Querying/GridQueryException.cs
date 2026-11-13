// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Api.Querying;

/// <summary>
/// Thrown by <see cref="QueryableExtensions"/> when a filter or sort cannot be applied as
/// asked: an unknown property, an unknown operator, an operator the property's type does
/// not support, or a value that cannot be converted to the property's type. The query is
/// never run with the condition left out.
/// </summary>
/// <remarks>
/// Derives from <see cref="ArgumentException"/>: the caller's request is what is wrong, and
/// an API answers it with <c>400 Bad Request</c>.
/// </remarks>
public sealed class GridQueryException : ArgumentException
{
    /// <summary>Initializes a new instance of the <see cref="GridQueryException"/> class.</summary>
    /// <param name="propertyName">The property the filter or sort names.</param>
    /// <param name="operator">The filter operator, or <c>null</c> for a sort.</param>
    /// <param name="reason">Why the condition cannot be applied.</param>
    /// <param name="innerException">The exception that caused this one, if any.</param>
    public GridQueryException(
        string propertyName,
        string? @operator,
        string reason,
        Exception? innerException = null)
        : base(
            @operator is null
                ? $"Cannot sort by property '{propertyName}': {reason}."
                : $"Cannot apply operator '{@operator}' to property '{propertyName}': {reason}.",
            innerException)
    {
        PropertyName = propertyName;
        Operator = @operator;
    }

    /// <summary>The property the filter or sort names.</summary>
    public string PropertyName { get; }

    /// <summary>The filter operator, or <c>null</c> when the sort is what failed.</summary>
    public string? Operator { get; }
}
