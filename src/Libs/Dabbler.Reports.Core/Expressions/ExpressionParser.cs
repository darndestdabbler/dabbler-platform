namespace Dabbler.Reports.Core.Expressions;

/// <summary>
/// Recursive descent parser that converts a token list into an expression AST.
/// </summary>
/// <remarks>
/// <para>Grammar (precedence from lowest to highest):</para>
/// <code>
/// expression     = comparison
/// comparison     = additive (("=" | "&lt;&gt;" | "&lt;" | "&gt;" | "&lt;=" | "&gt;=") additive)?
/// additive       = multiplicative (("+" | "-") multiplicative)*
/// multiplicative = unary (("*" | "/") unary)*
/// unary          = ("-") unary | primary
/// primary        = "(" expression ")"
///                | functionCall
///                | fieldRef | paramRef | namedExprRef | builtinRef
///                | stringLiteral | numberLiteral | booleanLiteral
/// functionCall   = identifier "(" argList ")"
/// argList        = expression ("," expression)* | "*"
/// </code>
/// </remarks>
internal sealed class ExpressionParser
{
    private readonly List<ExpressionToken> _tokens;
    private int _pos;

    public ExpressionParser(List<ExpressionToken> tokens)
    {
        _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        _pos = 0;
    }

    /// <summary>
    /// Parses the token list into an AST.
    /// </summary>
    public ExpressionNode Parse()
    {
        var node = ParseExpression();

        if (Current.Type != ExpressionTokenType.End)
            throw new ExpressionEvaluationException(
                $"Unexpected token '{Current.Value}' at position {Current.Position}.");

        return node;
    }

    private ExpressionToken Current => _pos < _tokens.Count
        ? _tokens[_pos]
        : new ExpressionToken(ExpressionTokenType.End, "", -1);

    private ExpressionToken Advance()
    {
        var token = Current;
        _pos++;
        return token;
    }

    private ExpressionToken Expect(ExpressionTokenType type)
    {
        if (Current.Type != type)
            throw new ExpressionEvaluationException(
                $"Expected {type} but found {Current.Type} ('{Current.Value}') at position {Current.Position}.");
        return Advance();
    }

    private ExpressionNode ParseExpression()
    {
        return ParseComparison();
    }

    private ExpressionNode ParseComparison()
    {
        var left = ParseAdditive();

        if (IsComparisonOperator(Current.Type))
        {
            var op = ToComparisonOp(Advance().Type);
            var right = ParseAdditive();
            return new BinaryOpNode(left, op, right);
        }

        return left;
    }

    private ExpressionNode ParseAdditive()
    {
        var left = ParseMultiplicative();

        while (Current.Type == ExpressionTokenType.Plus || Current.Type == ExpressionTokenType.Minus)
        {
            var op = Current.Type == ExpressionTokenType.Plus ? BinaryOperator.Add : BinaryOperator.Subtract;
            Advance();
            var right = ParseMultiplicative();
            left = new BinaryOpNode(left, op, right);
        }

        return left;
    }

    private ExpressionNode ParseMultiplicative()
    {
        var left = ParseUnary();

        while (Current.Type == ExpressionTokenType.Star || Current.Type == ExpressionTokenType.Slash)
        {
            var op = Current.Type == ExpressionTokenType.Star ? BinaryOperator.Multiply : BinaryOperator.Divide;
            Advance();
            var right = ParseUnary();
            left = new BinaryOpNode(left, op, right);
        }

        return left;
    }

    private ExpressionNode ParseUnary()
    {
        if (Current.Type == ExpressionTokenType.Minus)
        {
            Advance();
            var operand = ParseUnary();
            return new UnaryOpNode(UnaryOperator.Negate, operand);
        }

        return ParsePrimary();
    }

    private ExpressionNode ParsePrimary()
    {
        var token = Current;

        switch (token.Type)
        {
            case ExpressionTokenType.LeftParen:
                Advance();
                var expr = ParseExpression();
                Expect(ExpressionTokenType.RightParen);
                return expr;

            case ExpressionTokenType.Number:
                Advance();
                return ParseNumberLiteral(token.Value);

            case ExpressionTokenType.String:
                Advance();
                return new LiteralNode(token.Value);

            case ExpressionTokenType.True:
                Advance();
                return new LiteralNode(true);

            case ExpressionTokenType.False:
                Advance();
                return new LiteralNode(false);

            case ExpressionTokenType.FieldRef:
                Advance();
                return new FieldRefNode(token.Value);

            case ExpressionTokenType.ParamRef:
                Advance();
                return new ParamRefNode(token.Value);

            case ExpressionTokenType.NamedExprRef:
                Advance();
                return new NamedExprRefNode(token.Value);

            case ExpressionTokenType.BuiltinRef:
                Advance();
                return new BuiltinRefNode(token.Value);

            case ExpressionTokenType.Identifier:
                return ParseIdentifierOrFunctionCall();

            default:
                throw new ExpressionEvaluationException(
                    $"Unexpected token '{token.Value}' at position {token.Position}.");
        }
    }

    private ExpressionNode ParseIdentifierOrFunctionCall()
    {
        var nameToken = Advance();
        string name = nameToken.Value;

        // If not followed by '(', it's an error — bare identifiers are not valid
        // (fields use [], params use {@}, etc.)
        if (Current.Type != ExpressionTokenType.LeftParen)
            throw new ExpressionEvaluationException(
                $"Unexpected identifier '{name}' at position {nameToken.Position}. " +
                "Use [FieldName] for fields, {@ParamName} for parameters, or {=ExprName} for named expressions.");

        Advance(); // skip (

        // Special case: Count(*)
        if (name.Equals("Count", StringComparison.OrdinalIgnoreCase) && Current.Type == ExpressionTokenType.Star)
        {
            Advance(); // skip *

            string? scope = null;
            if (Current.Type == ExpressionTokenType.Comma)
            {
                Advance(); // skip ,
                var scopeToken = Expect(ExpressionTokenType.String);
                scope = scopeToken.Value;
            }

            Expect(ExpressionTokenType.RightParen);
            return new CountStarNode(scope);
        }

        // Parse argument list
        var args = new List<ExpressionNode>();
        if (Current.Type != ExpressionTokenType.RightParen)
        {
            args.Add(ParseExpression());
            while (Current.Type == ExpressionTokenType.Comma)
            {
                Advance(); // skip ,
                args.Add(ParseExpression());
            }
        }

        Expect(ExpressionTokenType.RightParen);
        return new FunctionCallNode(name, args);
    }

    private static ExpressionNode ParseNumberLiteral(string value)
    {
        if (value.Contains('.'))
        {
            if (decimal.TryParse(value, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out decimal d))
                return new LiteralNode(d);
        }
        else
        {
            if (int.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out int i))
                return new LiteralNode(i);

            if (long.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out long l))
                return new LiteralNode(l);

            if (decimal.TryParse(value, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out decimal d))
                return new LiteralNode(d);
        }

        throw new ExpressionEvaluationException($"Invalid number literal: '{value}'.");
    }

    private static bool IsComparisonOperator(ExpressionTokenType type)
    {
        return type == ExpressionTokenType.Equals
            || type == ExpressionTokenType.NotEquals
            || type == ExpressionTokenType.LessThan
            || type == ExpressionTokenType.GreaterThan
            || type == ExpressionTokenType.LessOrEqual
            || type == ExpressionTokenType.GreaterOrEqual;
    }

    private static BinaryOperator ToComparisonOp(ExpressionTokenType type)
    {
        return type switch
        {
            ExpressionTokenType.Equals => BinaryOperator.Equal,
            ExpressionTokenType.NotEquals => BinaryOperator.NotEqual,
            ExpressionTokenType.LessThan => BinaryOperator.LessThan,
            ExpressionTokenType.GreaterThan => BinaryOperator.GreaterThan,
            ExpressionTokenType.LessOrEqual => BinaryOperator.LessOrEqual,
            ExpressionTokenType.GreaterOrEqual => BinaryOperator.GreaterOrEqual,
            _ => throw new ExpressionEvaluationException($"Not a comparison operator: {type}")
        };
    }
}
