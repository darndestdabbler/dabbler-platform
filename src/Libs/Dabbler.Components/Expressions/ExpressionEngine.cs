namespace Dabbler.Components.Expressions;

/// <summary>
/// Public API for evaluating report expressions.
/// </summary>
/// <remarks>
/// <para>
/// The expression engine supports the following expression syntax:
/// </para>
/// <list type="bullet">
///   <item><description>Field references: <c>[FieldName]</c></description></item>
///   <item><description>Parameter references: <c>{@ParamName}</c></description></item>
///   <item><description>Named expression references: <c>{=ExprName}</c></description></item>
///   <item><description>Built-in values: <c>{Today}</c>, <c>{Now}</c>, <c>{PageNumber}</c>, <c>{PageCount}</c></description></item>
///   <item><description>Aggregate functions: <c>Sum([Field])</c>, <c>Count(*)</c>, <c>Avg([Field])</c>, <c>Min([Field])</c>, <c>Max([Field])</c></description></item>
///   <item><description>Conditionals: <c>IIf(condition, trueValue, falseValue)</c></description></item>
///   <item><description>Arithmetic operators: <c>+</c>, <c>-</c>, <c>*</c>, <c>/</c></description></item>
///   <item><description>Comparison operators: <c>=</c>, <c>&lt;&gt;</c>, <c>&lt;</c>, <c>&gt;</c>, <c>&lt;=</c>, <c>&gt;=</c></description></item>
///   <item><description>String concatenation: <c>+</c> when either operand is a string</description></item>
///   <item><description>String literals: <c>"text"</c></description></item>
///   <item><description>Numeric literals: <c>123</c>, <c>123.45</c></description></item>
///   <item><description>Boolean literals: <c>true</c>, <c>false</c></description></item>
/// </list>
/// <example>
/// <code>
/// var context = new MyEvaluationContext(currentRow, parameters, expressions);
/// var engine = new ExpressionEngine(context);
///
/// // Simple field reference
/// var name = engine.Evaluate("[FirstName]");
///
/// // Arithmetic with named expressions
/// var total = engine.Evaluate("{=Subtotal} + {=Tax} + [ShippingFee]");
///
/// // Conditional
/// var label = engine.Evaluate("IIf([TaxStatusID] = 1, \"Taxable\", \"Exempt\")");
///
/// // Aggregate with scope
/// var grandTotal = engine.Evaluate("Sum([OrderTotal], \"report\")");
///
/// // String concatenation with built-in
/// var footer = engine.Evaluate("\"Page \" + {PageNumber} + \" of \" + {PageCount}");
/// </code>
/// </example>
/// </remarks>
public sealed class ExpressionEngine
{
    private readonly ExpressionEvaluator _evaluator;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpressionEngine"/> class.
    /// </summary>
    /// <param name="context">The evaluation context providing field, parameter, and aggregate values.</param>
    public ExpressionEngine(IEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _evaluator = new ExpressionEvaluator(context);
    }

    /// <summary>
    /// Evaluates the given expression string and returns the result.
    /// </summary>
    /// <param name="expression">The expression text to evaluate.</param>
    /// <returns>The evaluated result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="expression"/> is <c>null</c>.</exception>
    /// <exception cref="ExpressionEvaluationException">The expression is invalid or cannot be evaluated.</exception>
    public object? Evaluate(string expression)
    {
        return _evaluator.Evaluate(expression);
    }

    /// <summary>
    /// Tries to evaluate the given expression string.
    /// </summary>
    /// <param name="expression">The expression text to evaluate.</param>
    /// <param name="result">The evaluation result if successful.</param>
    /// <param name="error">The error message if evaluation fails.</param>
    /// <returns><c>true</c> if evaluation succeeded; otherwise <c>false</c>.</returns>
    public bool TryEvaluate(string expression, out object? result, out string? error)
    {
        ArgumentNullException.ThrowIfNull(expression);

        try
        {
            result = _evaluator.Evaluate(expression);
            error = null;
            return true;
        }
        catch (ExpressionEvaluationException ex)
        {
            result = null;
            error = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Validates that the given expression can be parsed without evaluating it.
    /// </summary>
    /// <param name="expression">The expression text to validate.</param>
    /// <returns><c>true</c> if the expression is syntactically valid.</returns>
    public static bool IsValid(string expression)
    {
        return IsValid(expression, out _);
    }

    /// <summary>
    /// Validates that the given expression can be parsed without evaluating it.
    /// </summary>
    /// <param name="expression">The expression text to validate.</param>
    /// <param name="error">The error message if parsing fails.</param>
    /// <returns><c>true</c> if the expression is syntactically valid.</returns>
    public static bool IsValid(string expression, out string? error)
    {
        ArgumentNullException.ThrowIfNull(expression);

        try
        {
            var lexer = new ExpressionLexer(expression);
            var tokens = lexer.Tokenize();
            var parser = new ExpressionParser(tokens);
            parser.Parse();
            error = null;
            return true;
        }
        catch (ExpressionEvaluationException ex)
        {
            error = ex.Message;
            return false;
        }
    }
}
