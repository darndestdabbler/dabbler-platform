namespace Dabbler.Reports.Core.Expressions;

/// <summary>
/// Provides values for expression evaluation: field values, parameters,
/// named expressions, built-in values, and aggregate computation.
/// </summary>
public interface IEvaluationContext
{
    /// <summary>
    /// Gets the value of a field from the current data row.
    /// </summary>
    /// <param name="fieldName">The field name.</param>
    /// <returns>The field value, or null if the field has no value.</returns>
    /// <exception cref="ExpressionEvaluationException">The field does not exist.</exception>
    object? GetFieldValue(string fieldName);

    /// <summary>
    /// Gets the value of a report parameter.
    /// </summary>
    /// <param name="paramName">The parameter name.</param>
    /// <returns>The parameter value.</returns>
    /// <exception cref="ExpressionEvaluationException">The parameter does not exist.</exception>
    object? GetParameterValue(string paramName);

    /// <summary>
    /// Gets the expression text for a named expression.
    /// </summary>
    /// <param name="expressionName">The expression name.</param>
    /// <returns>The expression text.</returns>
    /// <exception cref="ExpressionEvaluationException">The expression does not exist.</exception>
    string GetNamedExpression(string expressionName);

    /// <summary>
    /// Gets a built-in value ({Today}, {Now}, {PageNumber}, {PageCount}).
    /// </summary>
    /// <param name="name">The built-in name.</param>
    /// <returns>The built-in value.</returns>
    /// <exception cref="ExpressionEvaluationException">The built-in is not recognized.</exception>
    object? GetBuiltinValue(string name);

    /// <summary>
    /// Computes an aggregate function over the current scope.
    /// </summary>
    /// <param name="function">The aggregate function.</param>
    /// <param name="expression">The expression to aggregate (null for Count(*)).</param>
    /// <param name="scope">The scope name (null for current group, "report" for report-level).</param>
    /// <returns>The aggregate result.</returns>
    object? ComputeAggregate(AggregateFunction function, string? expression, string? scope);
}
