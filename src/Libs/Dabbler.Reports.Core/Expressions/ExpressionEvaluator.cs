using System.Globalization;

namespace Dabbler.Reports.Core.Expressions;

/// <summary>
/// Evaluates a parsed expression AST against an <see cref="IEvaluationContext"/>.
/// </summary>
/// <remarks>
/// Handles field references, parameter references, named expressions (with
/// circular-reference detection), built-in values, aggregate functions,
/// IIf conditionals, arithmetic, string concatenation, and comparisons.
/// </remarks>
public class ExpressionEvaluator
{
    private readonly IEvaluationContext _context;
    private readonly HashSet<string> _evaluatingExpressions = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpressionEvaluator"/> class.
    /// </summary>
    /// <param name="context">The evaluation context providing field, parameter, and aggregate values.</param>
    public ExpressionEvaluator(IEvaluationContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Evaluates the given expression string and returns the result.
    /// </summary>
    /// <param name="expression">The expression text to evaluate.</param>
    /// <returns>The evaluated result.</returns>
    /// <exception cref="ExpressionEvaluationException">The expression is invalid or cannot be evaluated.</exception>
    public object? Evaluate(string expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        if (string.IsNullOrWhiteSpace(expression))
            return null;

        try
        {
            var lexer = new ExpressionLexer(expression);
            var tokens = lexer.Tokenize();
            var parser = new ExpressionParser(tokens);
            var ast = parser.Parse();
            return EvaluateNode(ast);
        }
        catch (ExpressionEvaluationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ExpressionEvaluationException(
                $"Failed to evaluate expression: {expression}", expression, ex);
        }
    }

    internal object? EvaluateNode(ExpressionNode node)
    {
        return node switch
        {
            LiteralNode literal => literal.Value,
            FieldRefNode fieldRef => _context.GetFieldValue(fieldRef.FieldName),
            ParamRefNode paramRef => _context.GetParameterValue(paramRef.ParamName),
            BuiltinRefNode builtinRef => _context.GetBuiltinValue(builtinRef.Name),
            NamedExprRefNode namedRef => EvaluateNamedExpression(namedRef),
            BinaryOpNode binary => EvaluateBinary(binary),
            UnaryOpNode unary => EvaluateUnary(unary),
            FunctionCallNode func => EvaluateFunction(func),
            CountStarNode countStar => _context.ComputeAggregate(AggregateFunction.Count, null, countStar.Scope),
            _ => throw new ExpressionEvaluationException($"Unknown AST node type: {node.GetType().Name}")
        };
    }

    private object? EvaluateNamedExpression(NamedExprRefNode namedRef)
    {
        string name = namedRef.ExpressionName;

        if (!_evaluatingExpressions.Add(name))
            throw new ExpressionEvaluationException(
                $"Circular reference detected in named expression '{name}'. " +
                $"Chain: {string.Join(" -> ", _evaluatingExpressions)} -> {name}");

        try
        {
            string exprText = _context.GetNamedExpression(name);
            return Evaluate(exprText);
        }
        finally
        {
            _evaluatingExpressions.Remove(name);
        }
    }

    private object? EvaluateBinary(BinaryOpNode binary)
    {
        var left = EvaluateNode(binary.Left);
        var right = EvaluateNode(binary.Right);

        // String concatenation: if either side is a string and op is Add
        if (binary.Operator == BinaryOperator.Add && (left is string || right is string))
        {
            return ConvertToString(left) + ConvertToString(right);
        }

        return binary.Operator switch
        {
            BinaryOperator.Add => ArithmeticOp(left, right, (a, b) => a + b),
            BinaryOperator.Subtract => ArithmeticOp(left, right, (a, b) => a - b),
            BinaryOperator.Multiply => ArithmeticOp(left, right, (a, b) => a * b),
            BinaryOperator.Divide => ArithmeticOp(left, right, (a, b) =>
            {
                if (b == 0m) throw new ExpressionEvaluationException("Division by zero.");
                return a / b;
            }),
            BinaryOperator.Equal => CompareValues(left, right) == 0,
            BinaryOperator.NotEqual => CompareValues(left, right) != 0,
            BinaryOperator.LessThan => CompareValues(left, right) < 0,
            BinaryOperator.GreaterThan => CompareValues(left, right) > 0,
            BinaryOperator.LessOrEqual => CompareValues(left, right) <= 0,
            BinaryOperator.GreaterOrEqual => CompareValues(left, right) >= 0,
            _ => throw new ExpressionEvaluationException($"Unknown operator: {binary.Operator}")
        };
    }

    private object? EvaluateUnary(UnaryOpNode unary)
    {
        var operand = EvaluateNode(unary.Operand);

        if (unary.Operator == UnaryOperator.Negate)
        {
            var d = ConvertToDecimal(operand);
            return -d;
        }

        throw new ExpressionEvaluationException($"Unknown unary operator: {unary.Operator}");
    }

    private object? EvaluateFunction(FunctionCallNode func)
    {
        string name = func.FunctionName;

        // IIf(condition, trueValue, falseValue)
        if (name.Equals("IIf", StringComparison.OrdinalIgnoreCase))
        {
            if (func.Arguments.Count != 3)
                throw new ExpressionEvaluationException(
                    $"IIf requires exactly 3 arguments, got {func.Arguments.Count}.");

            var condition = EvaluateNode(func.Arguments[0]);
            bool isTrue = ConvertToBoolean(condition);
            // Only evaluate the branch we need
            return isTrue
                ? EvaluateNode(func.Arguments[1])
                : EvaluateNode(func.Arguments[2]);
        }

        // Aggregate functions: Sum, Count, Avg, Min, Max
        if (TryParseAggregateFunction(name, out var aggFunc))
        {
            return EvaluateAggregate(aggFunc, func);
        }

        throw new ExpressionEvaluationException($"Unknown function: '{name}'.");
    }

    private object? EvaluateAggregate(AggregateFunction aggFunc, FunctionCallNode func)
    {
        if (func.Arguments.Count < 1 || func.Arguments.Count > 2)
            throw new ExpressionEvaluationException(
                $"{func.FunctionName} requires 1-2 arguments, got {func.Arguments.Count}.");

        string? innerExpr = ReconstructExpression(func.Arguments[0]);
        string? scope = null;

        if (func.Arguments.Count == 2)
        {
            var scopeVal = EvaluateNode(func.Arguments[1]);
            scope = scopeVal?.ToString();
        }

        return _context.ComputeAggregate(aggFunc, innerExpr, scope);
    }

    /// <summary>
    /// Reconstructs an expression string from an AST node, used for passing
    /// inner expressions to aggregate computation.
    /// </summary>
    private static string ReconstructExpression(ExpressionNode node)
    {
        return node switch
        {
            FieldRefNode f => $"[{f.FieldName}]",
            NamedExprRefNode n => $"{{={n.ExpressionName}}}",
            ParamRefNode p => $"{{@{p.ParamName}}}",
            BuiltinRefNode b => $"{{{b.Name}}}",
            LiteralNode lit => lit.Value switch
            {
                string s => $"\"{s}\"",
                decimal d => d.ToString(CultureInfo.InvariantCulture),
                int i => i.ToString(CultureInfo.InvariantCulture),
                long l => l.ToString(CultureInfo.InvariantCulture),
                bool bv => bv ? "true" : "false",
                null => "null",
                _ => lit.Value.ToString() ?? "null"
            },
            BinaryOpNode bin => $"({ReconstructExpression(bin.Left)} {OperatorToString(bin.Operator)} {ReconstructExpression(bin.Right)})",
            UnaryOpNode un => $"(-{ReconstructExpression(un.Operand)})",
            FunctionCallNode fn => $"{fn.FunctionName}({string.Join(", ", fn.Arguments.Select(ReconstructExpression))})",
            CountStarNode cs => cs.Scope != null ? $"Count(*, \"{cs.Scope}\")" : "Count(*)",
            _ => throw new ExpressionEvaluationException($"Cannot reconstruct expression from {node.GetType().Name}")
        };
    }

    private static string OperatorToString(BinaryOperator op)
    {
        return op switch
        {
            BinaryOperator.Add => "+",
            BinaryOperator.Subtract => "-",
            BinaryOperator.Multiply => "*",
            BinaryOperator.Divide => "/",
            BinaryOperator.Equal => "=",
            BinaryOperator.NotEqual => "<>",
            BinaryOperator.LessThan => "<",
            BinaryOperator.GreaterThan => ">",
            BinaryOperator.LessOrEqual => "<=",
            BinaryOperator.GreaterOrEqual => ">=",
            _ => throw new ExpressionEvaluationException($"Unknown operator: {op}")
        };
    }

    private static decimal ArithmeticOp(object? left, object? right, Func<decimal, decimal, decimal> op)
    {
        var l = ConvertToDecimal(left);
        var r = ConvertToDecimal(right);
        return op(l, r);
    }

    private static decimal ConvertToDecimal(object? value)
    {
        if (value is null)
            return 0m;

        return value switch
        {
            decimal d => d,
            int i => i,
            long l => l,
            double d => (decimal)d,
            float f => (decimal)f,
            short s => s,
            byte b => b,
            bool bo => bo ? 1m : 0m,
            string s when decimal.TryParse(s, CultureInfo.InvariantCulture, out decimal parsed) => parsed,
            _ => throw new ExpressionEvaluationException(
                $"Cannot convert '{value}' (type: {value.GetType().Name}) to a number.")
        };
    }

    private static bool ConvertToBoolean(object? value)
    {
        if (value is null)
            return false;

        return value switch
        {
            bool b => b,
            int i => i != 0,
            long l => l != 0,
            decimal d => d != 0m,
            double d => d != 0.0,
            string s => !string.IsNullOrEmpty(s) && !s.Equals("false", StringComparison.OrdinalIgnoreCase),
            _ => true
        };
    }

    private static int CompareValues(object? left, object? right)
    {
        // Null handling
        if (left is null && right is null) return 0;
        if (left is null) return -1;
        if (right is null) return 1;

        // Same types
        if (left is string ls && right is string rs)
            return string.Compare(ls, rs, StringComparison.OrdinalIgnoreCase);

        if (left is bool lb && right is bool rb)
            return lb.CompareTo(rb);

        // Numeric comparison
        try
        {
            var l = ConvertToDecimal(left);
            var r = ConvertToDecimal(right);
            return l.CompareTo(r);
        }
        catch (ExpressionEvaluationException)
        {
            // Fall back to string comparison
            return string.Compare(
                ConvertToString(left),
                ConvertToString(right),
                StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string ConvertToString(object? value)
    {
        if (value is null)
            return string.Empty;

        if (value is decimal d)
            return d.ToString(CultureInfo.InvariantCulture);

        if (value is DateTime dt)
            return dt.ToString("d", CultureInfo.CurrentCulture);

        return value.ToString() ?? string.Empty;
    }

    private static bool TryParseAggregateFunction(string name, out AggregateFunction func)
    {
        if (name.Equals("Sum", StringComparison.OrdinalIgnoreCase))
        { func = AggregateFunction.Sum; return true; }
        if (name.Equals("Count", StringComparison.OrdinalIgnoreCase))
        { func = AggregateFunction.Count; return true; }
        if (name.Equals("Avg", StringComparison.OrdinalIgnoreCase))
        { func = AggregateFunction.Avg; return true; }
        if (name.Equals("Min", StringComparison.OrdinalIgnoreCase))
        { func = AggregateFunction.Min; return true; }
        if (name.Equals("Max", StringComparison.OrdinalIgnoreCase))
        { func = AggregateFunction.Max; return true; }

        func = default;
        return false;
    }
}
