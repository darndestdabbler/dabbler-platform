namespace Dabbler.Reports.Core.Expressions;

/// <summary>
/// Exception thrown when an expression cannot be evaluated.
/// </summary>
public class ExpressionEvaluationException : Exception
{
    /// <summary>Gets the expression text that caused the error, if available.</summary>
    public string? Expression { get; }

    /// <summary>Initializes a new instance with the specified error message.</summary>
    /// <param name="message">A description of the evaluation error.</param>
    public ExpressionEvaluationException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance with the specified error message and expression text.</summary>
    /// <param name="message">A description of the evaluation error.</param>
    /// <param name="expression">The expression text that caused the error.</param>
    public ExpressionEvaluationException(string message, string? expression)
        : base(message)
    {
        Expression = expression;
    }

    /// <summary>Initializes a new instance with the specified error message and inner exception.</summary>
    /// <param name="message">A description of the evaluation error.</param>
    /// <param name="innerException">The exception that caused this error.</param>
    public ExpressionEvaluationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Initializes a new instance with the specified error message, expression text, and inner exception.</summary>
    /// <param name="message">A description of the evaluation error.</param>
    /// <param name="expression">The expression text that caused the error.</param>
    /// <param name="innerException">The exception that caused this error.</param>
    public ExpressionEvaluationException(string message, string? expression, Exception innerException)
        : base(message, innerException)
    {
        Expression = expression;
    }
}
